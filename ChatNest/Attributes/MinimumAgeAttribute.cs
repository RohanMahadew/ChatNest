using System.ComponentModel.DataAnnotations;

namespace ChatNest.Attributes
{
    public class MinimumAgeAttribute : ValidationAttribute
    {
        private readonly int _minimumAge;

        public MinimumAgeAttribute(int minimumAge)
        {
            _minimumAge = minimumAge;
        }

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {
            if (value is not DateOnly dateOfBirth) 
                throw new ValidationException("MinimumAgeAttribute.cs: The MinimumAgeAttribute can only be applied to DateOnly properties.");

            var today = DateOnly.FromDateTime(DateTime.Today);

            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age >= _minimumAge
                ? ValidationResult.Success
                : new ValidationResult(
                    $"You must be at least {_minimumAge} years old.");
        }
    }
}
