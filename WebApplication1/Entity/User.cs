using Microsoft.VisualBasic;

namespace WebApplication1.Entity
{
    public class User
    {
        public int Id { get; set; }    
        public String Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        DateFormat DateFormat { get; set; } 
    }
}
