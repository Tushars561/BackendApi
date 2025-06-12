using Microsoft.VisualBasic;

namespace WebApplication1.Entity
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        // ✅ Navigation property for one-to-many
        public ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
    }
}
