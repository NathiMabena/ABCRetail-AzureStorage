namespace ABCRetail.AzureStorage.Models
{
    // Single source of truth for country code + expected digit length (after the code).
    // Keep this in sync with the country dropdowns in Customers/Create.cshtml and Edit.cshtml.
    public static class PhoneCountryCodes
    {
        public static readonly (string Code, string Name, int Length)[] All =
        {
            ("+27", "South Africa", 9),
            ("+44", "United Kingdom", 10),
            ("+1", "United States", 10),
            ("+234", "Nigeria", 10),
            ("+254", "Kenya", 9),
        };

        // Returns null if valid, or an error message if not.
        public static string? Validate(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "Phone number is required.";
            }

            // Match the longest matching code first (avoids "+1" matching inside "+234")
            var match = All
                .Where(c => phone.StartsWith(c.Code))
                .OrderByDescending(c => c.Code.Length)
                .FirstOrDefault();

            if (match == default)
            {
                return "Phone number must start with a recognised country code.";
            }

            var digits = phone.Substring(match.Code.Length);

            if (digits.Length != match.Length || !digits.All(char.IsDigit))
            {
                return $"Enter exactly {match.Length} digits for {match.Name} ({match.Code}).";
            }

            return null;
        }
    }
}