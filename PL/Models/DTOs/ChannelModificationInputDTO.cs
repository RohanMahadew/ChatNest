using System.ComponentModel.DataAnnotations;

namespace PL.Models.DTOs
{
    public class ChannelModificationInputDTO
    {
        [Required(ErrorMessage = "ChannelId is required")]
        public int ChannelId { get; set; }

        [Required(ErrorMessage = "isPrivate is required")]
        public bool IsPrivate { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [Length(1, 50, ErrorMessage = "Name must be between 1 and 50 characters")]
        public string Name { get; set; } = String.Empty;
    }
}
