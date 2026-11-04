using ChatNest.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ChatNest.Models.DTO
{
    public class LoginInputDTO
    {
        [Required (ErrorMessage = "Username is required")]
        [NoSpace (ErrorMessage = "The username cannot contain spaces")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required (ErrorMessage = "Password is required")]
        [NoSpace (ErrorMessage = "The password cannot contain spaces")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}
