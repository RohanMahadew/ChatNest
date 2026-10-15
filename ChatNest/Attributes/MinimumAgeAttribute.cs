using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace ChatNest.Attributes
{
    public class MinimumAgeAttribute : ValidationAttribute, IClientModelValidator
    {
        private readonly int _minimumAge;
        // {0} field name {1} age requirement
        private const string DefaultErrorMessage = "You must be at least {1} years old.";

        public MinimumAgeAttribute(int minimumAge) : base(DefaultErrorMessage)
        {
            _minimumAge = minimumAge;
        }

        public override string FormatErrorMessage(string name)
        {
            return string.Format(ErrorMessageString, name, _minimumAge);
        }

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {
            // Let [Required] handle null/empty checks
            if (value == null) return ValidationResult.Success;

            if (value is not DateOnly dateOfBirth)
                throw new ValidationException("MinimumAgeAttribute.cs: The MinimumAgeAttribute can only be applied to DateOnly properties.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dateOfBirth.Year;

            if (dateOfBirth > today.AddYears(-age))
                age--;

            return age >= _minimumAge
                ? ValidationResult.Success
                : new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Client-Side
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-minage", FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
            MergeAttribute(context.Attributes, "data-val-minage-age", _minimumAge.ToString());
        }

        private static bool MergeAttribute(IDictionary<string, string> attributes, string key, string value)
        {
            if (attributes.ContainsKey(key)) return false;
            attributes.Add(key, value);
            return true;
        }
    }
}
