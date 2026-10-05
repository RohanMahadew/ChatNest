using ChatNest.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ChatNest.Models.DTO
{
    public class LoginInputDTO
    {
        [Required]
        [NoSpace]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [NoSpace]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
