using CatshrediasNewsAPI.Services;

namespace CatshrediasNewsAPI.Tests;

public class AuthServicePasswordVersionTests
{
    [Fact]
    public void ComputePasswordVersion_IsDeterministic()
    {
        const string hash = "$2a$11$abcdefghijklmnopqrstuv";
        var v1 = AuthService.ComputePasswordVersion(hash);
        var v2 = AuthService.ComputePasswordVersion(hash);
        Assert.Equal(v1, v2);
        Assert.Equal(64, v1.Length);
    }

    [Fact]
    public void ComputePasswordVersion_DiffersForDifferentHashes()
    {
        var v1 = AuthService.ComputePasswordVersion("$2a$11$hashOneaaaaaaaaaaaaaaa");
        var v2 = AuthService.ComputePasswordVersion("$2a$11$hashTwoaaaaaaaaaaaaaaa");
        Assert.NotEqual(v1, v2);
    }
}
