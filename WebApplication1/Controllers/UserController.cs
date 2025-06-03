using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Data;
using System.Formats.Asn1;
using System.Globalization;
using WebApplication1.Entity;
using WebApplication1.Service;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {


        private static List<User> users = new List<User>(); // Replace with DB in real use
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public UserController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }
        //private readonly TokenService _tokenService;

        //public UserController(TokenService tokenService)
        //{
        //    _tokenService = tokenService;
        //}

        //private readonly AppDbContext _context;

        //public UserController(AppDbContext context)
        //{
        //    _context = context;
        //}


        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User newUser)
        {
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User updatedUser)
        {
            if (id != updatedUser.Id)
                return BadRequest("User ID mismatch");

            _context.Entry(updatedUser).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent(); // 204
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }


        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<User>>> SearchByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Name query parameter is required.");
            }

            var results = await _context.Users
                .Where(u => EF.Functions.Like(u.Name, $"%{name}%"))
                .ToListAsync();

            return Ok(results);
        }



        [HttpPost("bulk")]
        public async Task<ActionResult> BulkAddUsers(List<User> users)
        {
            if (users == null || !users.Any())
            {
                return BadRequest("User list is empty.");
            }

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"{users.Count} users added successfully." });
        }

        [HttpGet("paged")]
        public async Task<ActionResult<IEnumerable<User>>> GetPagedUsers(int pageNumber = 1, int pageSize = 10)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return BadRequest("pageNumber and pageSize must be greater than zero.");
            }

            var users = await _context.Users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("sorted")]
        public async Task<ActionResult<IEnumerable<User>>> GetUsersSorted(string order = "asc")
        {
            IQueryable<User> query = _context.Users;

            query = order.ToLower() switch
            {
                "desc" => query.OrderByDescending(u => u.Name),
                _ => query.OrderBy(u => u.Name)
            };

            var users = await query.ToListAsync();
            return Ok(users);
        }


        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCsv([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a CSV file.");

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                var users = csv.GetRecords<User>().ToList();

                await _context.Users.AddRangeAsync(users);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{users.Count} users added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to process file: {ex.Message}");
            }
        }
        [HttpPost("SignUp")]
        public async Task<ActionResult<User>> SignUp(User newUser)
        {
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
        }

        [HttpPost("login")]
        public IActionResult Login([FromQuery] string email, [FromQuery] string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return BadRequest("Email and password are required.");

            var user = _context.Users.SingleOrDefault(u => u.Email == email && u.Password == password);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            var token = _tokenService.CreateToken(user);

            return Ok(new
            {
                Token = token,
                User = new { user.Id, user.Name, user.Email }
            });
        }


        public class FileUploadOperation : IOperationFilter
        {
            public void Apply(OpenApiOperation operation, OperationFilterContext context)
            {
                var hasFile = context.MethodInfo.GetParameters()
                    .Any(p => p.ParameterType == typeof(IFormFile));

                if (!hasFile) return;

                operation.RequestBody = new OpenApiRequestBody
                {
                    Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties =
                        {
                            ["file"] = new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            }
                        },
                        Required = { "file" }
                    }
                }
            }
                };
            }
        }
    }
}
