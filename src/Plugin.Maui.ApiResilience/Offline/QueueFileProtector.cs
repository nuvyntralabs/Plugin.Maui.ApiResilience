using System.Security.Cryptography;

namespace Plugin.Maui.ApiResilience;

internal static class QueueFileProtector
{
    internal static readonly byte[] Magic = "ARQ1"u8.ToArray();
    const int NonceSize = 12;
    const int TagSize = 16;
    const int KeySize = 32;

    public static bool IsProtected(ReadOnlySpan<byte> payload) =>
        payload.Length >= Magic.Length && payload[..Magic.Length].SequenceEqual(Magic);

    public static byte[] Protect(ReadOnlySpan<byte> plaintext, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length != KeySize)
        {
            throw new CryptographicException("Queue encryption key must be 256 bits.");
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var output = new byte[Magic.Length + NonceSize + TagSize + ciphertext.Length];
        Magic.CopyTo(output, 0);
        nonce.CopyTo(output.AsSpan(Magic.Length));
        tag.CopyTo(output.AsSpan(Magic.Length + NonceSize));
        ciphertext.CopyTo(output.AsSpan(Magic.Length + NonceSize + TagSize));
        return output;
    }

    public static byte[] Unprotect(ReadOnlySpan<byte> payload, byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (!IsProtected(payload))
        {
            throw new CryptographicException("Queue payload is not encrypted.");
        }

        var nonce = payload.Slice(Magic.Length, NonceSize);
        var tag = payload.Slice(Magic.Length + NonceSize, TagSize);
        var ciphertext = payload[(Magic.Length + NonceSize + TagSize)..];
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }

    public static byte[] LoadOrCreateKey(string keyPath)
    {
        if (File.Exists(keyPath))
        {
            var existing = File.ReadAllBytes(keyPath);
            if (existing.Length == KeySize)
            {
                return existing;
            }
        }

        var key = RandomNumberGenerator.GetBytes(KeySize);
        var directory = Path.GetDirectoryName(keyPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(keyPath, key);
        return key;
    }
}
