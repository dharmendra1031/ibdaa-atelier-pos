namespace IbdaaAtelier.Web.Models.Masters;

public static class PhoneNumbers
{
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        if (value.Any(c => c is not (>= '0' and <= '9' or ' ' or '-' or '(' or ')' or '+')))
            return null;

        var digits = new string(value.Where(c => c is >= '0' and <= '9').ToArray());

        // Kuwaiti country code is optional for local eight-digit numbers.
        if (digits.Length == 11 && digits.StartsWith("965", StringComparison.Ordinal))
            digits = digits[3..];

        return digits.Length is >= 8 and <= 15 ? digits : null;
    }
}
