using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using EnvironmentInspection.Detectors;
using EnvironmentInspection.Enums;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Volo.Abp.Telemetry.EnvironmentInspection.Detectors;

public class ChromeDetectorTests
{
    private readonly ChromeDetector _detector;

    public ChromeDetectorTests()
    {
        _detector = Substitute.ForPartsOf<ChromeDetector>();
    }

    [Fact]
    public async Task Should_Return_Null_When_Chrome_Not_Found()
    {
        // Arrange
        _detector.WhenForAnyArgs(d => d.ExecuteCommandAsync(default!, default!)).DoNotCallBase();
        _detector.ExecuteCommandAsync(Arg.Any<string>(), Arg.Any<string>()).Returns("119.0.1");

        // Fake the file check to always return false
        var fakePath = Path.Combine("Fake", "Google", "Chrome", "chrome.exe");
        _detector.When(x => File.Exists(Arg.Any<string>())).Do(_ => Task.FromResult(false));

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_SoftwareInfo_When_Chrome_Found_On_Windows()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        // Arrange
        var chromePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Google", "Chrome", "Application", "chrome.exe"
        );

        _detector.When(x => File.Exists(chromePath)).Do(_ => Task.FromResult(true));
        _detector.When(x => x.GetFileVersion(chromePath)).DoNotCallBase();
        _detector.GetFileVersion(chromePath).Returns("119.0.123");

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Chrome");
        result.SoftwareType.ShouldBe(SoftwareType.Browser);
        result.Version.ShouldBe("119.0.123");
    }

    [Fact]
    public async Task Should_Return_SoftwareInfo_When_Chrome_Found_On_Mac()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;

        // Arrange
        var chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
        _detector.When(x => File.Exists(chromePath)).Do(_ => Task.FromResult(true));
        _detector.ExecuteCommandAsync(chromePath, "--version").Returns("Google Chrome 120.0");

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Chrome");
        result.Version.ShouldBe("Google Chrome 120.0");
        result.SoftwareType.ShouldBe(SoftwareType.Browser);
    }

    [Fact]
    public async Task Should_Return_SoftwareInfo_When_Chrome_Found_On_Linux()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

        // Arrange
        var chromePath = "/usr/bin/google-chrome";
        _detector.When(x => File.Exists(chromePath)).Do(_ => Task.FromResult(true));
        _detector.ExecuteCommandAsync(chromePath, "--version").Returns("Google Chrome 121.0");

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Chrome");
        result.Version.ShouldBe("Google Chrome 121.0");
        result.SoftwareType.ShouldBe(SoftwareType.Browser);
    }
}

public class DotnetSdkDetectorTests
{
    private readonly DotnetSdkDetector _detector;

    public DotnetSdkDetectorTests()
    {
        _detector = Substitute.ForPartsOf<DotnetSdkDetector>();
    }

    [Fact]
    public async Task Should_Return_Null_When_DotnetSdk_Is_Not_Installed()
    {
        // Arrange
        _detector.When(x => x.ExecuteCommandAsync("dotnet", "--version"))
            .DoNotCallBase();
        _detector.ExecuteCommandAsync("dotnet", "--version").Returns((string?)null);

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Return_SoftwareInfo_When_DotnetSdk_Is_Installed()
    {
        // Arrange
        const string expectedVersion = "8.0.100";
        _detector.When(x => x.ExecuteCommandAsync("dotnet", "--version"))
            .DoNotCallBase();
        _detector.ExecuteCommandAsync("dotnet", "--version").Returns(Task.FromResult(expectedVersion));

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("DotnetSdk", result!.Name);
        Assert.Equal(expectedVersion, result.Version);
        Assert.Equal(SoftwareType.DotnetSdk, result.SoftwareType);
    }
}


public class AbpStudioDetectorTests
{
    private readonly AbpStudioDetector _detector;

    public AbpStudioDetectorTests()
    {
        _detector = Substitute.ForPartsOf<AbpStudioDetector>();
    }

    [Fact]
    public async Task Should_Return_Null_When_AbpStudio_Files_Not_Found()
    {
        // Arrange
        _detector.WhenForAnyArgs(x => x.GetAbpStudioUiThemeAsync()).Do(x => throw new FileNotFoundException());
        _detector.WhenForAnyArgs(x => x.GetAbpStudioVersionAsync()).Do(x => throw new FileNotFoundException());

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_SoftwareInfo_When_AbpStudio_Files_Exist()
    {
        // Arrange
        _detector.GetAbpStudioUiThemeAsync().Returns(Task.FromResult("dark")!);
        _detector.GetAbpStudioVersionAsync().Returns(Task.FromResult("3.2.1")!);

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        result.ShouldNotBeNull();
        result!.Name.ShouldBe("Abp Studio");
        result.Version.ShouldBe("3.2.1");
        result.UiTheme.ShouldBe("dark");
        result.SoftwareType.ShouldBe(SoftwareType.AbpStudio);
    }
}