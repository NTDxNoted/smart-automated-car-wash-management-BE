using System.Text.RegularExpressions;

namespace AutoWash.Application.Common.Validation
{
    // BR-Vehicle: định dạng biển số xe máy/ô tô Việt Nam.
    // Ví dụ hợp lệ: 30F-123.45, 51F12345, 29A-99999, 90A1-12345.
    public static class LicensePlateValidator
    {
        private static readonly Regex Pattern = new(
            @"^[0-9]{2}[A-Z0-9]{1,3}[-.\s]?[0-9]{3,5}(?:[.\s][0-9]{2})?$",
            RegexOptions.Compiled);

        public static bool IsValid(string? licensePlate)
        {
            if (string.IsNullOrWhiteSpace(licensePlate)) return false;
            return Pattern.IsMatch(licensePlate.Trim().ToUpperInvariant());
        }
    }
}
