using ChatNest.Attributes;
using System.ComponentModel.DataAnnotations;

namespace ChatNest.Models.DTO
{
    public class RegisterInputDTO
    {
        [Required]
        [EmailAddress]
        [NoSpace]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [NoSpace]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of birth")]
        [MinimumAge(16)]
        public DateOnly? DateOfBirth { get; set; }

        [Required]
        [NoSpace]
        [StringLength(
            100,
            MinimumLength = 6,
            ErrorMessage =
                "The {0} must be at least {2} and at most {1} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [NoSpace]
        [Display(Name = "Confirm password")]
        [Compare(
            nameof(Password),
            ErrorMessage =
                "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
