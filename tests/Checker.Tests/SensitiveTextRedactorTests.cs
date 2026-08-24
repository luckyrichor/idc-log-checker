using IDCLogChecker.Core.ContentAnalysis;
using Xunit;

namespace IDCLogChecker.Tests;

public sealed class SensitiveTextRedactorTests
{
    [Theory]
    [InlineData("password cipher SecretValue")]
    [InlineData("snmp-agent community read PublicValue")]
    [InlineData("authentication-key sha256 KeyValue")]
    [InlineData("secret MySecret")]
    public void CredentialValueIsRemoved(string text)
    {
        var redacted = SensitiveTextRedactor.Redact(text);

        Assert.Contains("***已隐藏***", redacted);
        Assert.DoesNotContain(text.Split(' ')[^1], redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void OrdinaryStatusTextIsPreservedAndLongTextIsBounded()
    {
        Assert.Equal("CPU Usage : 12%", SensitiveTextRedactor.Redact("CPU Usage : 12%"));
        Assert.True(SensitiveTextRedactor.Redact(new string('x', 1000)).Length <= SensitiveTextRedactor.MaximumLength);
    }
}
