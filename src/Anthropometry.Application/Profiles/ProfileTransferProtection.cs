using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Anthropometry.Application.Common;
using Anthropometry.Domain.Common;

namespace Anthropometry.Application.Profiles;

public static class ProfileTransferProtection
{
    public const string Magic = "ANTHROPOMETRY\0";
    public const int MaxProtectedFileBytes = 10 * 1024 * 1024;
    public const int MinimumPassphraseLength = 12;
    public const int CurrentPbkdf2Iterations = 600_000;

    private const byte CurrentEnvelopeVersion = 1;
    private const byte CurrentPayloadVersion = 1;
    private const byte Pbkdf2Sha256Algorithm = 1;
    private const byte Aes256GcmAlgorithm = 1;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int KeySize = 32;
    private const int TagSize = 16;
    private const int MinimumPbkdf2Iterations = 100_000;
    private const int MaximumPbkdf2Iterations = 2_000_000;

    private static readonly byte[] MagicBytes = Encoding.ASCII.GetBytes(Magic);
    private static readonly byte[] LegacyCsvPrefix = Encoding.UTF8.GetBytes("record_type,format_version,");
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];
    private static readonly int HeaderSize = MagicBytes.Length + 4 + sizeof(int) + SaltSize + NonceSize;

    public static Result<byte[]> Protect(byte[] csvContent, string passphrase)
    {
        if (csvContent is null
            || csvContent.Length == 0
            || csvContent.Length > MaxProtectedFileBytes - HeaderSize - TagSize)
        {
            return Failure<byte[]>(ApplicationErrors.ProfileTransferFileInvalid);
        }

        var passphraseValidation = ValidatePassphrase(passphrase);
        if (!passphraseValidation.IsSuccess)
        {
            return Result.Failure<byte[]>(passphraseValidation.Error!);
        }

        var salt = new byte[SaltSize];
        var nonce = new byte[NonceSize];
        var passwordBytes = Encoding.UTF8.GetBytes(passphrase);
        var key = Array.Empty<byte>();
        try
        {
            RandomNumberGenerator.Fill(salt);
            RandomNumberGenerator.Fill(nonce);
            key = DeriveKey(passwordBytes, salt, CurrentPbkdf2Iterations);
            var header = BuildHeader(salt, nonce, CurrentPbkdf2Iterations);
            var ciphertext = new byte[csvContent.Length];
            var tag = new byte[TagSize];
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, csvContent, ciphertext, tag, header);

            var result = new byte[header.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(header, 0, result, 0, header.Length);
            Buffer.BlockCopy(tag, 0, result, header.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, header.Length + tag.Length, ciphertext.Length);
            return Result.Success(result);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(nonce);
        }
    }

    public static Result<ProfileTransferProtectedPayload> Unprotect(byte[] content, string? passphrase)
    {
        if (content is null || content.Length == 0 || content.Length > MaxProtectedFileBytes)
        {
            return Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferFileInvalid);
        }

        if (!StartsWith(content, MagicBytes))
        {
            return IsLegacyCsv(content)
                ? Result.Success(new ProfileTransferProtectedPayload(content.ToArray(), true))
                : Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferFileInvalid);
        }

        if (content.Length < HeaderSize + TagSize + 1)
        {
            return Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferFileInvalid);
        }

        var offset = MagicBytes.Length;
        var envelopeVersion = content[offset++];
        var payloadVersion = content[offset++];
        var kdfAlgorithm = content[offset++];
        var cipherAlgorithm = content[offset++];
        var iterations = BinaryPrimitives.ReadInt32BigEndian(content.AsSpan(offset, sizeof(int)));
        offset += sizeof(int);

        if (envelopeVersion != CurrentEnvelopeVersion
            || payloadVersion != CurrentPayloadVersion
            || kdfAlgorithm != Pbkdf2Sha256Algorithm
            || cipherAlgorithm != Aes256GcmAlgorithm)
        {
            return Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferFormatUnsupported);
        }

        if (iterations is < MinimumPbkdf2Iterations or > MaximumPbkdf2Iterations)
        {
            return Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferFileInvalid);
        }

        var salt = content.AsSpan(offset, SaltSize).ToArray();
        offset += SaltSize;
        var nonce = content.AsSpan(offset, NonceSize).ToArray();
        offset += NonceSize;
        var tag = content.AsSpan(offset, TagSize).ToArray();
        offset += TagSize;
        var ciphertext = content.AsSpan(offset).ToArray();

        if (string.IsNullOrWhiteSpace(passphrase))
        {
            return Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferPasswordRequired);
        }

        var passphraseValidation = ValidatePassphrase(passphrase);
        if (!passphraseValidation.IsSuccess)
        {
            return Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferPassphraseInvalid);
        }

        var passwordBytes = Encoding.UTF8.GetBytes(passphrase);
        var key = Array.Empty<byte>();
        var plaintext = new byte[ciphertext.Length];
        try
        {
            key = DeriveKey(passwordBytes, salt, iterations);
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, content.AsSpan(0, HeaderSize));
            return Result.Success(new ProfileTransferProtectedPayload(plaintext, false));
        }
        catch (AuthenticationTagMismatchException)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            return Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferAuthenticationFailed);
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(plaintext);
            return Failure<ProfileTransferProtectedPayload>(ApplicationErrors.ProfileTransferAuthenticationFailed);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(nonce);
            CryptographicOperations.ZeroMemory(tag);
            CryptographicOperations.ZeroMemory(ciphertext);
        }
    }

    public static Result ValidatePassphrase(string? passphrase)
        => !string.IsNullOrWhiteSpace(passphrase) && passphrase.Trim().Length >= MinimumPassphraseLength
            ? Result.Success()
            : Result.Failure(ApplicationErrors.ProfileTransferPassphraseInvalid);

    private static byte[] BuildHeader(byte[] salt, byte[] nonce, int iterations)
    {
        var header = new byte[HeaderSize];
        MagicBytes.CopyTo(header, 0);
        var offset = MagicBytes.Length;
        header[offset++] = CurrentEnvelopeVersion;
        header[offset++] = CurrentPayloadVersion;
        header[offset++] = Pbkdf2Sha256Algorithm;
        header[offset++] = Aes256GcmAlgorithm;
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(offset, sizeof(int)), iterations);
        offset += sizeof(int);
        salt.CopyTo(header, offset);
        offset += salt.Length;
        nonce.CopyTo(header, offset);
        return header;
    }

    private static byte[] DeriveKey(byte[] passwordBytes, byte[] salt, int iterations)
        => Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, iterations, HashAlgorithmName.SHA256, KeySize);

    private static bool StartsWith(byte[] value, byte[] prefix)
        => StartsWith(value.AsSpan(), prefix);

    private static bool IsLegacyCsv(byte[] content)
    {
        var bytes = content.AsSpan();
        return StartsWith(bytes, LegacyCsvPrefix)
            || (StartsWith(bytes, Utf8Bom) && StartsWith(bytes[Utf8Bom.Length..], LegacyCsvPrefix));
    }

    private static bool StartsWith(ReadOnlySpan<byte> value, ReadOnlySpan<byte> prefix)
        => value.Length >= prefix.Length && value[..prefix.Length].SequenceEqual(prefix);

    private static Result<T> Failure<T>(DomainError error) => Result.Failure<T>(error);
}
