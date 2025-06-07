using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using CodePilot.Areas.Identity.Data;

namespace CodePilot.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastModified { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}
