using System.ComponentModel.DataAnnotations;

namespace ChatNest.Attributes
{
    public class NoSpaceAttribute : ValidationAttribute
    {
        public NoSpaceAttribute() {

        }

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {             
            if (value is string str)
            {
                if (str.Contains(' '))
                {
                    string? failedPropertyName = validationContext.MemberName;
                    return new ValidationResult($"The field {failedPropertyName} cannot contain spaces.");
                }
            }
            return ValidationResult.Success;
        }
    }
}
