using MayaJaal.Shared.Contracts;

namespace MayaJaal.Infrastructure.Cryptography;

/// <summary>
/// Key wrapping using AES-GCM. Wrapped format: [12-byte IV][16-byte tag][ciphertext].
/// </summary>
public sealed class KeyManager : IKeyManager
{
    public const int IvSize = AesGcmEncryption.DefaultIvSize;
    public const int TagSize = AesGcmEncryption.TagSize;
    public const int HeaderSize = IvSize + TagSize;

    private readonly IEncryptionProvider _encryption;
    private readonly Argon2Kdf _kdf;

    public KeyManager(IEncryptionProvider encryption, Argon2Kdf kdf)
    {
        _encryption = encryption;
        _kdf = kdf;
    }

    public byte[] DeriveKey(
        string password,
        ReadOnlySpan<byte> salt,
        int memoryKb = 65536,
        int iterations = 3,
        int parallelism = 1,
        int keyLength = 32)
        => _kdf.DeriveKey(password, salt, memoryKb, iterations, parallelism, keyLength);

    public byte[] WrapKey(ReadOnlySpan<byte> keyToWrap, ReadOnlySpan<byte> wrappingKey)
    {
        if (keyToWrap.IsEmpty)
            throw new ArgumentException("Key to wrap cannot be empty.", nameof(keyToWrap));

        var ciphertext = _encryption.Encrypt(keyToWrap, wrappingKey, out var iv, out var tag);
        var wrapped = new byte[HeaderSize + ciphertext.Length];
        Buffer.BlockCopy(iv, 0, wrapped, 0, IvSize);
        Buffer.BlockCopy(tag, 0, wrapped, IvSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, wrapped, HeaderSize, ciphertext.Length);
        return wrapped;
    }

    public byte[] UnwrapKey(ReadOnlySpan<byte> wrapped, ReadOnlySpan<byte> wrappingKey)
    {
        if (wrapped.Length <= HeaderSize)
            throw new ArgumentException("Wrapped key blob is too short.", nameof(wrapped));

        var iv = wrapped[..IvSize];
        var tag = wrapped.Slice(IvSize, TagSize);
        var ciphertext = wrapped[HeaderSize..];
        return _encryption.Decrypt(ciphertext, wrappingKey, iv, tag);
    }

    public byte[] GenerateMasterKey(int sizeBytes = 32) => _encryption.GenerateKey(sizeBytes);
}
