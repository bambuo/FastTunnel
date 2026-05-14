using System.Security.Cryptography;
using System.Text;

namespace FastTunnel.Api.Services;

public class TotpService
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private const int PeriodSeconds = 30;
    private const int Digits = 6;

    public string GenerateSecret()
    {
        var bytes = new byte[10];
        RandomNumberGenerator.Fill(bytes);
        return Base32Encode(bytes);
    }

    public string GenerateQrCodeUrl(string secret, string accountName, string issuer = "FastTunnel")
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedAccount = Uri.EscapeDataString(accountName);
        return $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={secret}&issuer={encodedIssuer}&digits={Digits}";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (code.Length != Digits || !long.TryParse(code, out _))
            return false;

        var secretBytes = Base32Decode(secret);
        var now = DateTime.UtcNow;
        var counter = (long)(now - UnixEpoch).TotalSeconds / PeriodSeconds;

        for (var i = -1; i <= 1; i++)
        {
            if (GenerateCode(secretBytes, counter + i) == code)
                return true;
        }

        return false;
    }

    private static string GenerateCode(byte[] secretBytes, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBytes);

        var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString().PadLeft(Digits, '0');
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();
        var buffer = 0;
        var bitsRemaining = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsRemaining += 8;

            while (bitsRemaining >= 5)
            {
                bitsRemaining -= 5;
                result.Append(alphabet[(buffer >> bitsRemaining) & 0x1F]);
            }
        }

        if (bitsRemaining > 0)
        {
            result.Append(alphabet[(buffer << (5 - bitsRemaining)) & 0x1F]);
        }

        return result.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        input = input.TrimEnd('=').ToUpperInvariant();

        var result = new List<byte>();
        var buffer = 0;
        var bitsRemaining = 0;

        foreach (var c in input)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0) continue;

            buffer = (buffer << 5) | value;
            bitsRemaining += 5;

            if (bitsRemaining >= 8)
            {
                bitsRemaining -= 8;
                result.Add((byte)((buffer >> bitsRemaining) & 0xFF));
            }
        }

        return result.ToArray();
    }
}
