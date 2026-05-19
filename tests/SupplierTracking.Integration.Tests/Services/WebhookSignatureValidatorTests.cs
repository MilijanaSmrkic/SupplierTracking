using System.Security.Cryptography;
using System.Text;
using SupplierTracking.Infrastructure.Services;

namespace SupplierTracking.Integration.Tests.Services;

public class WebhookSignatureValidatorTests
{
    // Access the internal class via its public interface for behaviour testing.
    // We instantiate it directly because it has no dependencies.
    private readonly WebhookSignatureValidator _validator = new();

    private static string ComputeExpectedSignature(string body, string secret)
    {
        var key  = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(key, data);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenSignatureMatches()
    {
        const string body   = """{"orderNumber":"ORD-001","event":"confirmed"}""";
        const string secret = "super-secret-key";
        var signature = ComputeExpectedSignature(body, secret);

        Assert.True(_validator.IsValid(body, signature, secret));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenSignatureIsTampered()
    {
        const string body   = """{"orderNumber":"ORD-001","event":"confirmed"}""";
        const string secret = "super-secret-key";
        var signature = ComputeExpectedSignature(body, secret);
        var tampered  = signature[..^4] + "0000"; // corrupt last 4 hex chars

        Assert.False(_validator.IsValid(body, tampered, secret));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenBodyIsModified()
    {
        const string secret          = "super-secret-key";
        const string originalBody    = """{"orderNumber":"ORD-001","event":"confirmed"}""";
        const string modifiedBody    = """{"orderNumber":"ORD-001","event":"delivered"}""";
        var signature = ComputeExpectedSignature(originalBody, secret);

        Assert.False(_validator.IsValid(modifiedBody, signature, secret));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenSecretIsWrong()
    {
        const string body      = """{"orderNumber":"ORD-001","event":"confirmed"}""";
        var signature = ComputeExpectedSignature(body, "correct-secret");

        Assert.False(_validator.IsValid(body, signature, "wrong-secret"));
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenPrefixIsMissing()
    {
        const string body   = """{"event":"confirmed"}""";
        const string secret = "key";
        // raw hex without "sha256=" prefix
        var key  = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(body);
        var rawHex = Convert.ToHexString(HMACSHA256.HashData(key, data)).ToLowerInvariant();

        Assert.False(_validator.IsValid(body, rawHex, secret));
    }

    [Fact]
    public void IsValid_ShouldBeCaseInsensitive_OnPrefix()
    {
        const string body   = """{"event":"confirmed"}""";
        const string secret = "key";
        var key  = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(body);
        var hash = Convert.ToHexString(HMACSHA256.HashData(key, data)).ToLowerInvariant();
        var signatureUpperPrefix = "SHA256=" + hash;

        Assert.True(_validator.IsValid(body, signatureUpperPrefix, secret));
    }

    [Fact]
    public void Compute_ShouldReturnLowercaseHex()
    {
        const string body   = "hello";
        const string secret = "world";
        var result = _validator.Compute(body, secret);

        Assert.Equal(result, result.ToLowerInvariant());
        Assert.Matches("^[0-9a-f]{64}$", result); // SHA-256 = 32 bytes = 64 hex chars
    }

    [Fact]
    public void Compute_ShouldBeDeterministic()
    {
        const string body   = """{"event":"shipped","trackingCode":"DHL-999"}""";
        const string secret = "deterministic-key";

        var result1 = _validator.Compute(body, secret);
        var result2 = _validator.Compute(body, secret);

        Assert.Equal(result1, result2);
    }
}
