using System.Security.Cryptography;
using Certifications.Application.Abstractions;

namespace Certifications.Infrastructure.Security;

internal sealed class CryptographicPasswordGenerator : IPasswordGenerator
{
    private const int PasswordLength = 16;
    private const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string AllCharacters = Letters + Digits;

    public string Generate()
    {
        Span<char> password = stackalloc char[PasswordLength];
        password[0] = Letters[RandomNumberGenerator.GetInt32(Letters.Length)];
        password[1] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];

        for (var index = 2; index < password.Length; index++)
        {
            password[index] = AllCharacters[RandomNumberGenerator.GetInt32(AllCharacters.Length)];
        }

        for (var index = password.Length - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (password[index], password[swapIndex]) = (password[swapIndex], password[index]);
        }

        return new string(password);
    }
}
