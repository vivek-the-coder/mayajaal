using System.Text;
using Konscious.Security.Cryptography;
using MayaJaal.Shared.Contracts;

namespace MayaJaal.Infrastructure.Cryptography;

/// <summary>
/// Argon2id password-based key derivation.
/// </summary>
public sealed class Argon2Kdf
{
    private readonly IEncryptionProvider _encryption;

    public Argon2Kdf(IEncryptionProvider encryption)
    {
        _encryption = encryption;
    }

    public byte[] DeriveKey(
        string password,
        ReadOnlySpan<byte> salt,
        int memoryKb = 65536,
        int iterations = 3,
        int parallelism = 1,
        int keyLength = 32)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        if (salt.Length < 8)
            throw new ArgumentException("Salt must be at least 8 bytes.", nameof(salt));
        if (keyLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(keyLength));

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            using var argon2 = new Argon2id(passwordBytes)
            {
                Salt = salt.ToArray(),
                DegreeOfParallelism = Math.Max(1, parallelism),
                MemorySize = Math.Max(8 * Math.Max(1, parallelism), memoryKb),
                Iterations = Math.Max(1, iterations)
            };
            return argon2.GetBytes(keyLength);
        }
        finally
        {
            CryptographicZero(passwordBytes);
        }
    }

    public byte[] GenerateSalt(int sizeBytes = 16) => _encryption.GenerateSalt(sizeBytes);

    private static void CryptographicZero(byte[] buffer)
    {
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(buffer);
    }
}
