using System.Text;
using Anthropometry.Application.Profiles;

namespace Anthropometry.Application.Tests.Profiles;

public sealed class ProfileTransferProtectionTests
{
    private const string Passphrase = "correct horse battery staple";
    private static readonly byte[] CsvPayload = Encoding.UTF8.GetBytes(
        "record_type,format_version,exported_at_utc\nmeta,1,2026-09-20T10:00:00.0000000+00:00\n");

    [Fact]
    public void Protected_payload_round_trips_with_the_same_passphrase()
    {
        var protectedPayload = ProfileTransferProtection.Protect(CsvPayload, Passphrase);

        Assert.True(protectedPayload.IsSuccess);
        var unprotected = ProfileTransferProtection.Unprotect(protectedPayload.Value, Passphrase);

        Assert.True(unprotected.IsSuccess);
        Assert.Equal(CsvPayload, unprotected.Value.CsvContent);
        Assert.False(unprotected.Value.IsLegacyUnprotected);
    }

    [Fact]
    public void Protected_outputs_use_fresh_salt_and_nonce()
    {
        var first = ProfileTransferProtection.Protect(CsvPayload, Passphrase);
        var second = ProfileTransferProtection.Protect(CsvPayload, Passphrase);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value, second.Value);
    }

    [Fact]
    public void Wrong_passphrase_returns_the_generic_authentication_error()
    {
        var protectedPayload = ProfileTransferProtection.Protect(CsvPayload, Passphrase);

        var result = ProfileTransferProtection.Unprotect(protectedPayload.Value, "different passphrase");

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.transfer.authentication.failed", result.Error!.Code);
    }

    [Fact]
    public void Ciphertext_and_authenticated_header_tampering_is_rejected()
    {
        var protectedPayload = ProfileTransferProtection.Protect(CsvPayload, Passphrase).Value;
        var tamperedCiphertext = protectedPayload.ToArray();
        tamperedCiphertext[^1] ^= 0x01;
        var tamperedHeader = protectedPayload.ToArray();
        tamperedHeader[ProfileTransferProtection.Magic.Length] = 0x7F;

        var ciphertextResult = ProfileTransferProtection.Unprotect(tamperedCiphertext, Passphrase);
        var headerResult = ProfileTransferProtection.Unprotect(tamperedHeader, Passphrase);

        Assert.False(ciphertextResult.IsSuccess);
        Assert.Equal("profile.transfer.authentication.failed", ciphertextResult.Error!.Code);
        Assert.False(headerResult.IsSuccess);
    }

    [Fact]
    public void Unsupported_envelope_version_is_rejected()
    {
        var protectedPayload = ProfileTransferProtection.Protect(CsvPayload, Passphrase).Value;
        var unsupported = protectedPayload.ToArray();
        unsupported[ProfileTransferProtection.Magic.Length] = 2;

        var result = ProfileTransferProtection.Unprotect(unsupported, Passphrase);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.transfer.format.unsupported", result.Error!.Code);
    }

    [Fact]
    public void Invalid_and_oversized_inputs_are_rejected()
    {
        var invalid = ProfileTransferProtection.Unprotect([1, 2, 3], Passphrase);
        var oversized = ProfileTransferProtection.Unprotect(
            new byte[ProfileTransferProtection.MaxProtectedFileBytes + 1],
            Passphrase);

        Assert.False(invalid.IsSuccess);
        Assert.Equal("profile.transfer.file.invalid", invalid.Error!.Code);
        Assert.False(oversized.IsSuccess);
        Assert.Equal("profile.transfer.file.invalid", oversized.Error!.Code);
    }

    [Fact]
    public void Protect_rejects_payload_that_would_exceed_the_maximum_envelope_size()
    {
        var payload = new byte[ProfileTransferProtection.MaxProtectedFileBytes];

        var result = ProfileTransferProtection.Protect(payload, Passphrase);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.transfer.file.invalid", result.Error!.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("           ")]
    [InlineData("short")]
    public void Short_or_blank_passphrases_are_rejected(string? passphrase)
    {
        var result = ProfileTransferProtection.ValidatePassphrase(passphrase);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.transfer.passphrase.invalid", result.Error!.Code);
    }

    [Fact]
    public void Previous_plain_csv_is_detected_as_legacy_payload()
    {
        var result = ProfileTransferProtection.Unprotect(CsvPayload, null);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLegacyUnprotected);
        Assert.Equal(CsvPayload, result.Value.CsvContent);
    }

    [Fact]
    public void Previous_plain_csv_with_utf8_bom_is_detected_as_legacy_payload()
    {
        var withBom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(CsvPayload).ToArray();

        var result = ProfileTransferProtection.Unprotect(withBom, null);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLegacyUnprotected);
        Assert.Equal(withBom, result.Value.CsvContent);
    }

    [Fact]
    public void Unrelated_bytes_are_not_accepted_as_a_transfer_file()
    {
        var result = ProfileTransferProtection.Unprotect(
            Encoding.UTF8.GetBytes("not an Anthropometry export"),
            null);

        Assert.False(result.IsSuccess);
        Assert.Equal("profile.transfer.file.invalid", result.Error!.Code);
    }
}
