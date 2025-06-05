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
      
    }
}
