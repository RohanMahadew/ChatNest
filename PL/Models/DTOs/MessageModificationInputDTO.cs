using System.ComponentModel.DataAnnotations;

namespace PL.Models.DTOs
{
    public class MessageModificationInputDTO
    {
        [Required(ErrorMessage = "MessageId is required")]
        public int MessageId { get; set; }

        [Required(ErrorMessage = "Content is required")]
        [Length(1, 500, ErrorMessage = "Content must be between 1 and 500 characters")]
        public string Content { get; set; } = string.Empty;
    }
}
