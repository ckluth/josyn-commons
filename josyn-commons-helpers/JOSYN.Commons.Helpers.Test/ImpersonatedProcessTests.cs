using NUnit.Framework;
using JOSYN.Commons.Helpers;

namespace JOSYN.Commons.Helpers.Test;

[TestFixture]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class ImpersonatedProcessTests
{
    [Test]
    public void Start_NonExistentExe_Fails()
    {
        var credential = WindowsCredential.Parse("svc@domain.local");
        Assert.That(credential.Succeeded, Is.True);

        var result = ImpersonatedProcess.Start(
            @"C:\does\not\exist.exe",
            "some-args",
            "password",
            credential.Value);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }
}
