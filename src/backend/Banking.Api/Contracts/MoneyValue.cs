using System.Globalization;
using System.Text.RegularExpressions;

namespace Banking.Api.Contracts;

public static partial class MoneyValue
{
    public const decimal Maximum = 9999999999999999.99m;

    [GeneratedRegex(@"^[0-9]{1,16}(\.[0-9]{1,2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex AmountPattern();

    public static bool TryParse(string? value, out decimal amount)
    {
        amount = 0m;
        return value is not null && AmountPattern().IsMatch(value)
            && decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount)
            && amount > 0m && amount <= Maximum;
    }

    public static string Format(decimal amount) => amount.ToString("F2", CultureInfo.InvariantCulture);
}
