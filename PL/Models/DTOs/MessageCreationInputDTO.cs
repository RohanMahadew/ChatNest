using System.ComponentModel.DataAnnotations;

namespace PL.Models.DTOs
{
    public class MessageCreationInputDTO
    {
        [Required(ErrorMessage = "SenderId is required")]
        public int SenderId { get; set; }


        [Required(ErrorMessage = "ChannelId is required")]
        public int ChannelId { get; set; }


        [Required(ErrorMessage = "Content is required")]
        [Length(1, 500, ErrorMessage = "Content must be between 1 and 500 characters")]
        public string Content { get; set; } = string.Empty;
    }
}
