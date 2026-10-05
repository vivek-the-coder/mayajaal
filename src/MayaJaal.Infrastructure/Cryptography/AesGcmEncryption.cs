using System.Security.Cryptography;
using MayaJaal.Shared.Contracts;

namespace MayaJaal.Infrastructure.Cryptography;

/// <summary>
/// AES-256-GCM encryption. Callers that need wrapped keys should use
/// <see cref="KeyManager"/> which stores [12-byte IV][16-byte tag][ciphertext].
/// </summary>
public sealed class AesGcmEncryption : IEncryptionProvider
{
    public const int DefaultKeySize = 32;
    public const int DefaultIvSize = 12;
    public const int TagSize = 16;

    public byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, out byte[] iv, out byte[] tag)
    {
        ValidateKey(key);
        iv = GenerateIv(DefaultIvSize);
        tag = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];

        using var aes = new AesGcm(key.ToArray(), TagSize);
        aes.Encrypt(iv, plaintext, ciphertext, tag);
        return ciphertext;
    }

    public byte[] Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> tag)
    {
        ValidateKey(key);
        if (iv.Length != DefaultIvSize)
            throw new ArgumentException($"IV must be {DefaultIvSize} bytes.", nameof(iv));
        if (tag.Length != TagSize)
            throw new ArgumentException($"Tag must be {TagSize} bytes.", nameof(tag));

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key.ToArray(), TagSize);
        aes.Decrypt(iv, ciphertext, tag, plaintext);
        return plaintext;
    }

    public void EncryptFile(string inputPath, string outputPath, ReadOnlySpan<byte> key, out byte[] iv, out byte[] tag)
    {
        var plaintext = File.ReadAllBytes(inputPath);
        var ciphertext = Encrypt(plaintext, key, out iv, out tag);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, ciphertext);
        CryptographicOperations.ZeroMemory(plaintext);
    }

    public void DecryptFile(string inputPath, string outputPath, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> tag)
    {
        var ciphertext = File.ReadAllBytes(inputPath);
        var plaintext = Decrypt(ciphertext, key, iv, tag);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllBytes(outputPath, plaintext);
        CryptographicOperations.ZeroMemory(plaintext);
    }

    public byte[] GenerateKey(int sizeBytes = DefaultKeySize)
    {
        if (sizeBytes <= 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        var key = new byte[sizeBytes];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public byte[] GenerateSalt(int sizeBytes = 16)
    {
        if (sizeBytes <= 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        var salt = new byte[sizeBytes];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    public byte[] GenerateIv(int sizeBytes = DefaultIvSize)
    {
        if (sizeBytes <= 0) throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        var iv = new byte[sizeBytes];
        RandomNumberGenerator.Fill(iv);
        return iv;
    }

    private static void ValidateKey(ReadOnlySpan<byte> key)
    {
        if (key.Length is not (16 or 24 or 32))
            throw new ArgumentException("AES key must be 16, 24, or 32 bytes.", nameof(key));
    }
}
