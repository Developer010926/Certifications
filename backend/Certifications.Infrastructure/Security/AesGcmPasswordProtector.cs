using System.Security.Cryptography;
using System.Text;
using Certifications.Application.Abstractions;
using Certifications.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Certifications.Infrastructure.Security;

internal sealed class AesGcmPasswordProtector : IPasswordProtector
{
    private const string Version = "v1";
    private static readonly byte[] AssociatedData =
        Encoding.UTF8.GetBytes("Certifications.Employee.Password.v1");

    private readonly byte[] _key;

    public AesGcmPasswordProtector(IOptions<SecurityOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.PasswordEncryptionKey);
    }

    public string Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plaintext);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using (var aes = new AesGcm(_key, tag.Length))
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, AssociatedData);
        }

        CryptographicOperations.ZeroMemory(plaintextBytes);
        return string.Join(
            '.',
            Version,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(ciphertext),
            Convert.ToBase64String(tag));
    }

    public string Unprotect(string protectedValue)
    {
        var parts = protectedValue.Split('.');
        if (parts.Length != 4 || parts[0] != Version)
        {
            throw new CryptographicException("The password payload is not supported.");
        }

        var nonce = Convert.FromBase64String(parts[1]);
        var ciphertext = Convert.FromBase64String(parts[2]);
        var tag = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];

        using (var aes = new AesGcm(_key, tag.Length))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData);
        }

        try
        {
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public bool Verify(string plaintext, string protectedValue)
    {
        try
        {
            var expected = Encoding.UTF8.GetBytes(Unprotect(protectedValue));
            var actual = Encoding.UTF8.GetBytes(plaintext);

            try
            {
                return expected.Length == actual.Length
                    && CryptographicOperations.FixedTimeEquals(expected, actual);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(expected);
                CryptographicOperations.ZeroMemory(actual);
            }
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
