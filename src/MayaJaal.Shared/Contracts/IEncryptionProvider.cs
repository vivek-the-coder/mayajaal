namespace MayaJaal.Shared.Contracts;

/// <summary>
/// AES-GCM encryption primitives. WrapKey blob format is owned by <see cref="IKeyManager"/>.
/// </summary>
public interface IEncryptionProvider
{
    byte[] Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> key, out byte[] iv, out byte[] tag);
    byte[] Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> tag);
    void EncryptFile(string inputPath, string outputPath, ReadOnlySpan<byte> key, out byte[] iv, out byte[] tag);
    void DecryptFile(string inputPath, string outputPath, ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> tag);
    byte[] GenerateKey(int sizeBytes = 32);
    byte[] GenerateSalt(int sizeBytes = 16);
    byte[] GenerateIv(int sizeBytes = 12);
}
