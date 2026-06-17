using NUnit.Framework;
using JOSYN.Commons.Helpers;

namespace JOSYN.Commons.Helpers.Test;

[TestFixture]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class WindowsCredentialTests
{
    [TestCase("svc_job@mydomain.local")]
    [TestCase("job@corp.com")]
    [TestCase("a@b")]
    public void Parse_ValidUpn_Succeeds(string upn)
    {
        var result = WindowsCredential.Parse(upn);
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Value.Upn, Is.EqualTo(upn));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Parse_EmptyOrWhitespace_Fails(string upn)
    {
        var result = WindowsCredential.Parse(upn);
        Assert.That(result.Succeeded, Is.False);
    }

    [TestCase("svc_job")]
    [TestCase("DOMAIN\\user")]
    public void Parse_BareOrBackslashUsername_Fails(string upn)
    {
        // Local accounts and DOMAIN\user format are not accepted (ADR-021).
        var result = WindowsCredential.Parse(upn);
        Assert.That(result.Succeeded, Is.False);
    }

    [Test]
    public void Parse_MultipleAtSigns_Fails()
    {
        var result = WindowsCredential.Parse("a@b@c");
        Assert.That(result.Succeeded, Is.False);
    }

    [TestCase("@domain")]
    [TestCase("user@")]
    public void Parse_EmptyLocalPartOrDomain_Fails(string upn)
    {
        var result = WindowsCredential.Parse(upn);
        Assert.That(result.Succeeded, Is.False);
    }
}
