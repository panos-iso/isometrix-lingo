using System;
using System.IO;
using TranslationManagementTool.Models;
using TranslationManagementTool.Services;
using Xunit;

namespace TranslationManagementTool.Tests.Services;

public class UserSettingsServiceTests : IDisposable
{
    private readonly string _testSettingsPath;
    private readonly UserSettingsService _service;

    public UserSettingsServiceTests()
    {
        _testSettingsPath = Path.Combine(Path.GetTempPath(), $"test_settings_{Guid.NewGuid()}.json");
        _service = new UserSettingsService(_testSettingsPath);
    }

    [Fact]
    public void IsFirstRun_WhenNoSettingsFile_ReturnsTrue()
    {
        // Act
        var result = _service.IsFirstRun();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Save_CreatesSettingsFile()
    {
        // Arrange
        var settings = new UserSettings { Username = "TestUser" };

        // Act
        _service.Save(settings);

        // Assert
        Assert.True(File.Exists(_testSettingsPath));
    }

    [Fact]
    public void Load_WhenNoFile_ReturnsNull()
    {
        // Act
        var result = _service.Load();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Save_And_Load_PersistsUsername()
    {
        // Arrange
        var settings = new UserSettings { Username = "JohnDoe" };

        // Act
        _service.Save(settings);
        var loaded = _service.Load();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("JohnDoe", loaded.Username);
    }

    [Fact]
    public void IsFirstRun_AfterSave_ReturnsFalse()
    {
        // Arrange
        var settings = new UserSettings { Username = "TestUser" };
        _service.Save(settings);

        // Act
        var result = _service.IsFirstRun();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Load_SetsSettingsFilePath()
    {
        // Arrange
        var settings = new UserSettings { Username = "TestUser" };
        _service.Save(settings);

        // Act
        var loaded = _service.Load();

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(_testSettingsPath, loaded.SettingsFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testSettingsPath))
        {
            File.Delete(_testSettingsPath);
        }
    }
}
