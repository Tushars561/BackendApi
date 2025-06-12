using System.Text.Json.Serialization;

namespace WebApplication1.Entity
{
    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool IsComplete { get; set; }
        // Foreign key
        public int UserId { get; set; }

        //Navigation property — Each todo belongs to one user
       [JsonIgnore]
        public User? User { get; set; }
        //public int UserId { get; set; }
        //public User User { get; set; }


    }
}
