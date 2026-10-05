namespace MayaJaal.Shared.Contracts;

/// <summary>
/// Key derivation and wrapping. Wrapped keys use: [12-byte IV][16-byte tag][ciphertext].
/// </summary>
public interface IKeyManager
{
    byte[] DeriveKey(
        string password,
        ReadOnlySpan<byte> salt,
        int memoryKb = 65536,
        int iterations = 3,
        int parallelism = 1,
        int keyLength = 32);

    /// <summary>Returns IV (12) + tag (16) + ciphertext.</summary>
    byte[] WrapKey(ReadOnlySpan<byte> keyToWrap, ReadOnlySpan<byte> wrappingKey);

    /// <summary>Parses IV (12) + tag (16) + ciphertext and returns plaintext key.</summary>
    byte[] UnwrapKey(ReadOnlySpan<byte> wrapped, ReadOnlySpan<byte> wrappingKey);

    byte[] GenerateMasterKey(int sizeBytes = 32);
}
