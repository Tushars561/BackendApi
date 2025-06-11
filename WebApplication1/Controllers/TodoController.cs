using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WebApplication1.Entity;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TodoController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                throw new UnauthorizedAccessException("User ID not found in token.");

            return int.Parse(userIdClaim.Value);
        }


        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetUserTodos()
        {
            int userId = GetUserIdFromToken(); // ✅ Extract from JWT
            var todos = await _context.TodoItems
                                      .Where(t => t.UserId == userId)
                                      .ToListAsync();

            return Ok(todos);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddTodo([FromBody] TodoItem item)
        {
            //if (!ModelState.IsValid)
            //    return BadRequest(ModelState);
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                                .SelectMany(v => v.Errors)
                                .Select(e => e.ErrorMessage)
                                .ToList();

                return BadRequest(new { message = "Validation failed", errors });
            }

            int userId = GetUserIdFromToken(); // ✅ Extract user ID from JWT
            Console.WriteLine("userId: " + userId);
            item.UserId = userId;              // ✅ Set UserId on the TodoItem
            item.User = null;
            _context.TodoItems.Add(item);
            await _context.SaveChangesAsync();

            return Ok(item);
        }
       

       

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, TodoItem updatedTodo)
        {
            var todo = await _context.TodoItems.FindAsync(id);
            if (todo == null)
                return NotFound();

            todo.Title = updatedTodo.Title;
            todo.Description = updatedTodo.Description;
            todo.IsComplete = updatedTodo.IsComplete;

            await _context.SaveChangesAsync();
            return NoContent();
        }

       

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            var todo = await _context.TodoItems.FindAsync(id);
            if (todo == null) return NotFound();

            _context.TodoItems.Remove(todo);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("upload-csv")]
        public async Task<IActionResult> UploadCSV(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("CSV file is required.");

            var todos = new List<TodoItem>();
            int userId = GetUserIdFromToken(); // Your method to extract user ID from JWT

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                bool isFirstLine = true;
                while (!stream.EndOfStream)
                {
                    var line = await stream.ReadLineAsync();
                    if (isFirstLine) { isFirstLine = false; continue; } // Skip header

                    var parts = line.Split(',');
                    if (parts.Length < 3) continue;

                    todos.Add(new TodoItem
                    {
                        Title = parts[0].Trim(),
                        Description = parts[1].Trim(),
                        IsComplete = bool.Parse(parts[2].Trim()),
                        UserId = userId
                    });
                }
            }

            _context.TodoItems.AddRange(todos);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Todos uploaded successfully", count = todos.Count });
        }

        [HttpGet("todo-count")]
        [Authorize]
        public IActionResult GetTodoCount()
        {
            int userId = GetUserIdFromToken();

            var totalCount = _context.TodoItems
                                     .Where(todo => todo.UserId == userId)
                                     .Count();

            var completedCount = _context.TodoItems
                                         .Where(todo => todo.UserId == userId && todo.IsComplete)
                                         .Count();

            var pendingCount = totalCount - completedCount;

            return Ok(new
            {
                userId = userId,
                totalCount = totalCount,
                pendingCount = pendingCount,
                completedCount = completedCount
            });
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<TodoItem>>> SearchByTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return BadRequest("Title query parameter is required.");
            }

            int userId = GetUserIdFromToken();

            var results = await _context.TodoItems
                .Where(t => t.UserId == userId && EF.Functions.Like(t.Title, $"%{title}%"))
                .ToListAsync();

            return Ok(results);
        }

        //[HttpGet]
        //[Authorize]
        //public IActionResult GetTodos(int pageNumber = 1, int pageSize = 10)
        //{
        //    int userId = GetUserIdFromToken(); // Your JWT method

        //    var todos = _context.TodoItems
        //        .Where(todo => todo.UserId == userId)
        //        .Skip((pageNumber - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToList();

        //    var totalCount = _context.TodoItems.Count(todo => todo.UserId == userId);

        //    return Ok(new
        //    {
        //        data = todos,
        //        totalCount = totalCount,
        //        currentPage = pageNumber,
        //        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        //    });
        //}


    }
}
