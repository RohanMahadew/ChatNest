using ChatNest.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ChatNest.Models.DTO
{
    public class RegisterInputDTO
    {
        [Required (ErrorMessage = "Email is required")]
        [EmailAddress (ErrorMessage = "The email address is not valid")]
        [NoSpace (ErrorMessage = "The email address cannot contain spaces")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required (ErrorMessage = "Username is required")]
        [NoSpace (ErrorMessage = "The username cannot contain spaces")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required (ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        [MinimumAge(16)]
        public DateOnly? DateOfBirth { get; set; }

        [Required (ErrorMessage = "Password is required")]
        [NoSpace (ErrorMessage = "The password cannot contain spaces")]
        [StringLength(100,MinimumLength = 6,ErrorMessage ="The {0} must be at least {2} and at most {1} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [NoSpace (ErrorMessage = "The confirmation password cannot contain spaces")]
        [Display(Name = "Confirm password")]
        [Compare(nameof(Password), ErrorMessage ="The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
