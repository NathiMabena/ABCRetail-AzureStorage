using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.AzureStorage.Models
{
    public class CustomerEntity : ITableEntity, IValidatableObject
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        public string Phone { get; set; } = string.Empty;

        // Enforces the exact expected digit count per country code — catches
        // short/garbage numbers like "+2722" that [Required] alone would miss.
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var error = PhoneCountryCodes.Validate(Phone);
            if (error != null)
            {
                yield return new ValidationResult(error, new[] { nameof(Phone) });
            }
        }
    }
}