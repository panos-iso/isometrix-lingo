using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using IsometrixLingo.Models;
using IsometrixLingo.Services;
using Xunit;

namespace IsometrixLingo.Tests.Services;

/// <summary>
/// Tests to ensure export preserves ALL original file content: structure, order, declarations, formatting
/// </summary>
public class ExportPreservationTests : IDisposable
{
    private readonly string _testDir;
    private readonly string _sourceDir;
    private readonly string _exportDir;

    public ExportPreservationTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"lingo_test_{Guid.NewGuid():N}");
        _sourceDir = Path.Combine(_testDir, "source");
        _exportDir = Path.Combine(_testDir, "export");
        
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_exportDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    #region JSON Nested Structure Tests

    [Fact]
    public void ExportNestedJson_PreservesNestedStructure()
    {
        // Arrange: Create a JSON file with NESTED structure
        var originalJson = @"{
  ""config"": {
    ""dateFormat"": ""MM/DD/YYYY"",
    ""locale"": ""en-US""
  },
  ""datetimeFormats"": {
    ""long"": {
      ""day"": ""numeric"",
      ""month"": ""short"",
      ""year"": ""numeric""
    },
    ""short"": {
      ""day"": ""numeric"",
      ""month"": ""short""
    }
  }
}";
        var sourceFile = Path.Combine(_sourceDir, "settings.en.json");
        File.WriteAllText(sourceFile, originalJson);

        // Create translation keys with edited values
        var keys = new List<TranslationKey>
        {
            new TranslationKey 
            { 
                Key = "config.dateFormat",
                Source = new SourceFile("settings", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "DD/MM/YYYY" } } // EDITED
            },
            new TranslationKey 
            { 
                Key = "config.locale",
                Source = new SourceFile("settings", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "en-US" } }
            },
            new TranslationKey 
            { 
                Key = "datetimeFormats.long.day",
                Source = new SourceFile("settings", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "numeric" } }
            },
            new TranslationKey 
            { 
                Key = "datetimeFormats.long.month",
                Source = new SourceFile("settings", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "short" } }
            },
            new TranslationKey 
            { 
                Key = "datetimeFormats.long.year",
                Source = new SourceFile("settings", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "numeric" } }
            },
            new TranslationKey 
            { 
                Key = "datetimeFormats.short.day",
                Source = new SourceFile("settings", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "numeric" } }
            },
            new TranslationKey 
            { 
                Key = "datetimeFormats.short.month",
                Source = new SourceFile("settings", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "short" } }
            }
        };

        var writer = new JsonTranslationFileWriter();

        // Act: Export using copy-then-update
        writer.CopyAndUpdateFiles(keys, _sourceDir, _exportDir, "TestUser", EditMode.Edit);

        // Assert: Verify nested structure is preserved
        var exportedFile = Path.Combine(_exportDir, "settings.en.json");
        Assert.True(File.Exists(exportedFile), "Exported file should exist");

        var exportedContent = File.ReadAllText(exportedFile);
        var exportedJson = JsonNode.Parse(exportedContent) as JsonObject;
        
        Assert.NotNull(exportedJson);
        
        // Verify it's NESTED not FLAT
        Assert.True(exportedJson.ContainsKey("config"), "Should have 'config' as nested object");
        Assert.True(exportedJson.ContainsKey("datetimeFormats"), "Should have 'datetimeFormats' as nested object");
        Assert.False(exportedJson.ContainsKey("config.dateFormat"), "Should NOT have flat 'config.dateFormat' key");
        
        // Verify nested structure
        var config = exportedJson["config"] as JsonObject;
        Assert.NotNull(config);
        Assert.Equal("DD/MM/YYYY", config["dateFormat"]?.GetValue<string>()); // Edited value
        Assert.Equal("en-US", config["locale"]?.GetValue<string>());
        
        var datetimeFormats = exportedJson["datetimeFormats"] as JsonObject;
        Assert.NotNull(datetimeFormats);
        var longFormat = datetimeFormats["long"] as JsonObject;
        Assert.NotNull(longFormat);
        Assert.Equal("numeric", longFormat["day"]?.GetValue<string>());
    }

    [Fact]
    public void ExportMixedJson_PreservesMixedStructure()
    {
        // Arrange: Create a JSON file with BOTH flat and nested keys
        var originalJson = @"{
  ""appName"": ""MyApp"",
  ""version"": ""1.0.0"",
  ""config"": {
    ""timeout"": ""30"",
    ""retries"": ""3""
  },
  ""simpleKey"": ""simpleValue""
}";
        var sourceFile = Path.Combine(_sourceDir, "mixed.en.json");
        File.WriteAllText(sourceFile, originalJson);

        var keys = new List<TranslationKey>
        {
            new TranslationKey 
            { 
                Key = "appName",
                Source = new SourceFile("mixed", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "MyApp EDITED" } } // EDITED
            },
            new TranslationKey 
            { 
                Key = "version",
                Source = new SourceFile("mixed", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "1.0.0" } }
            },
            new TranslationKey 
            { 
                Key = "config.timeout",
                Source = new SourceFile("mixed", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "60" } } // EDITED
            },
            new TranslationKey 
            { 
                Key = "config.retries",
                Source = new SourceFile("mixed", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "3" } }
            },
            new TranslationKey 
            { 
                Key = "simpleKey",
                Source = new SourceFile("mixed", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "simpleValue" } }
            }
        };

        var writer = new JsonTranslationFileWriter();

        // Act
        writer.CopyAndUpdateFiles(keys, _sourceDir, _exportDir, "TestUser", EditMode.Edit);

        // Assert
        var exportedFile = Path.Combine(_exportDir, "mixed.en.json");
        var exportedContent = File.ReadAllText(exportedFile);
        var exportedJson = JsonNode.Parse(exportedContent) as JsonObject;
        
        Assert.NotNull(exportedJson);
        
        // Verify flat keys stay flat
        Assert.Equal("MyApp EDITED", exportedJson["appName"]?.GetValue<string>());
        Assert.Equal("1.0.0", exportedJson["version"]?.GetValue<string>());
        Assert.Equal("simpleValue", exportedJson["simpleKey"]?.GetValue<string>());
        
        // Verify nested keys stay nested
        var config = exportedJson["config"] as JsonObject;
        Assert.NotNull(config);
        Assert.Equal("60", config["timeout"]?.GetValue<string>());
        Assert.Equal("3", config["retries"]?.GetValue<string>());
    }

    #endregion

    #region RESX XML Declaration Tests

    [Fact]
    public void ExportResx_PreservesXmlDeclaration()
    {
        // Arrange: Create RESX with XML declaration
        var originalResx = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Login"" xml:space=""preserve"">
    <value>Login</value>
  </data>
  <data name=""Logout"" xml:space=""preserve"">
    <value>Logout</value>
  </data>
</root>";
        var sourceFile = Path.Combine(_sourceDir, "Strings.resx");
        File.WriteAllText(sourceFile, originalResx);

        var keys = new List<TranslationKey>
        {
            new TranslationKey 
            { 
                Key = "Login",
                Source = new SourceFile("Strings", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> { { "en", "Sign In" } } // EDITED
            },
            new TranslationKey 
            { 
                Key = "Logout",
                Source = new SourceFile("Strings", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> { { "en", "Logout" } }
            }
        };

        var writer = new ResxTranslationFileWriter();

        // Act
        writer.CopyAndUpdateFiles(keys, _sourceDir, _exportDir, "TestUser", EditMode.Edit);

        // Assert
        var exportedFile = Path.Combine(_exportDir, "Strings.resx");
        Assert.True(File.Exists(exportedFile));
        
        var exportedContent = File.ReadAllText(exportedFile);
        
        // Verify XML declaration exists
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", exportedContent);
        
        // Verify content is valid XML with declaration
        var doc = XDocument.Load(exportedFile);
        Assert.NotNull(doc.Declaration);
        Assert.Equal("1.0", doc.Declaration.Version);
        Assert.Equal("utf-8", doc.Declaration.Encoding);
        
        // Verify edited value
        var loginElement = doc.Root.Elements("data").FirstOrDefault(e => e.Attribute("name")?.Value == "Login");
        Assert.NotNull(loginElement);
        Assert.Equal("Sign In", loginElement.Element("value")?.Value);
    }

    #endregion

    #region Key Order Preservation Tests

    [Fact]
    public void ExportJson_PreservesOriginalKeyOrder()
    {
        // Arrange: Create JSON with specific key order
        var originalJson = @"{
  ""zebra"": ""last"",
  ""apple"": ""first"",
  ""mango"": ""middle"",
  ""banana"": ""second""
}";
        var sourceFile = Path.Combine(_sourceDir, "ordered.en.json");
        File.WriteAllText(sourceFile, originalJson);

        var keys = new List<TranslationKey>
        {
            new TranslationKey 
            { 
                Key = "zebra",
                Source = new SourceFile("ordered", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "last EDITED" } }
            },
            new TranslationKey 
            { 
                Key = "apple",
                Source = new SourceFile("ordered", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "first" } }
            },
            new TranslationKey 
            { 
                Key = "mango",
                Source = new SourceFile("ordered", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "middle" } }
            },
            new TranslationKey 
            { 
                Key = "banana",
                Source = new SourceFile("ordered", FileType.Json, null),
                LanguageValues = new Dictionary<string, string> { { "en", "second" } }
            }
        };

        var writer = new JsonTranslationFileWriter();

        // Act
        writer.CopyAndUpdateFiles(keys, _sourceDir, _exportDir, "TestUser", EditMode.Edit);

        // Assert
        var exportedFile = Path.Combine(_exportDir, "ordered.en.json");
        var exportedContent = File.ReadAllText(exportedFile);
        var exportedJson = JsonNode.Parse(exportedContent) as JsonObject;
        
        Assert.NotNull(exportedJson);
        
        // Verify order is preserved (zebra, apple, mango, banana - NOT alphabetical)
        var keysInOrder = exportedJson.Select(kvp => kvp.Key).ToList();
        Assert.Equal("zebra", keysInOrder[0]);
        Assert.Equal("apple", keysInOrder[1]);
        Assert.Equal("mango", keysInOrder[2]);
        Assert.Equal("banana", keysInOrder[3]);
        
        // Verify edited value
        Assert.Equal("last EDITED", exportedJson["zebra"]?.GetValue<string>());
    }

    [Fact]
    public void ExportResx_PreservesOriginalKeyOrder()
    {
        // Arrange: Create RESX with specific key order (not alphabetical)
        var originalResx = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Zebra"" xml:space=""preserve"">
    <value>Last</value>
  </data>
  <data name=""Apple"" xml:space=""preserve"">
    <value>First</value>
  </data>
  <data name=""Mango"" xml:space=""preserve"">
    <value>Middle</value>
  </data>
  <data name=""Banana"" xml:space=""preserve"">
    <value>Second</value>
  </data>
</root>";
        var sourceFile = Path.Combine(_sourceDir, "Ordered.resx");
        File.WriteAllText(sourceFile, originalResx);

        var keys = new List<TranslationKey>
        {
            new TranslationKey 
            { 
                Key = "Zebra",
                Source = new SourceFile("Ordered", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> { { "en", "Last EDITED" } }
            },
            new TranslationKey 
            { 
                Key = "Apple",
                Source = new SourceFile("Ordered", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> { { "en", "First" } }
            },
            new TranslationKey 
            { 
                Key = "Mango",
                Source = new SourceFile("Ordered", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> { { "en", "Middle" } }
            },
            new TranslationKey 
            { 
                Key = "Banana",
                Source = new SourceFile("Ordered", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> { { "en", "Second" } }
            }
        };

        var writer = new ResxTranslationFileWriter();

        // Act
        writer.CopyAndUpdateFiles(keys, _sourceDir, _exportDir, "TestUser", EditMode.Edit);

        // Assert
        var exportedFile = Path.Combine(_exportDir, "Ordered.resx");
        var doc = XDocument.Load(exportedFile);
        
        // Verify order is preserved (Zebra, Apple, Mango, Banana - NOT alphabetical)
        var dataElements = doc.Root.Elements("data").ToList();
        Assert.Equal("Zebra", dataElements[0].Attribute("name")?.Value);
        Assert.Equal("Apple", dataElements[1].Attribute("name")?.Value);
        Assert.Equal("Mango", dataElements[2].Attribute("name")?.Value);
        Assert.Equal("Banana", dataElements[3].Attribute("name")?.Value);
        
        // Verify edited value
        Assert.Equal("Last EDITED", dataElements[0].Element("value")?.Value);
    }

    [Fact]
    public void ExportResx_DifferentLanguageFiles_PreservesPerFileOrder()
    {
        // Arrange: English and Spanish files with DIFFERENT key orders
        var englishResx = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Login"" xml:space=""preserve"">
    <value>Login</value>
  </data>
  <data name=""Logout"" xml:space=""preserve"">
    <value>Logout</value>
  </data>
  <data name=""Welcome"" xml:space=""preserve"">
    <value>Welcome</value>
  </data>
</root>";

        var spanishResx = @"<?xml version=""1.0"" encoding=""utf-8""?>
<root>
  <data name=""Welcome"" xml:space=""preserve"">
    <value>Bienvenido</value>
  </data>
  <data name=""Login"" xml:space=""preserve"">
    <value>Iniciar sesión</value>
  </data>
  <data name=""Logout"" xml:space=""preserve"">
    <value>Cerrar sesión</value>
  </data>
</root>";

        File.WriteAllText(Path.Combine(_sourceDir, "Messages.resx"), englishResx);
        File.WriteAllText(Path.Combine(_sourceDir, "Messages_es.resx"), spanishResx);

        var keys = new List<TranslationKey>
        {
            new TranslationKey 
            { 
                Key = "Login",
                Source = new SourceFile("Messages", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> 
                { 
                    { "en", "Sign In" },  // EDITED
                    { "es", "Iniciar sesión" }
                }
            },
            new TranslationKey 
            { 
                Key = "Logout",
                Source = new SourceFile("Messages", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> 
                { 
                    { "en", "Logout" },
                    { "es", "Cerrar sesión" }
                }
            },
            new TranslationKey 
            { 
                Key = "Welcome",
                Source = new SourceFile("Messages", FileType.Resx, null),
                LanguageValues = new Dictionary<string, string> 
                { 
                    { "en", "Welcome" },
                    { "es", "Bienvenido" }
                }
            }
        };

        var writer = new ResxTranslationFileWriter();

        // Act
        writer.CopyAndUpdateFiles(keys, _sourceDir, _exportDir, "TestUser", EditMode.Edit);

        // Assert English file order
        var englishFile = Path.Combine(_exportDir, "Messages.resx");
        var englishDoc = XDocument.Load(englishFile);
        var englishKeys = englishDoc.Root.Elements("data").Select(e => e.Attribute("name")?.Value).ToList();
        
        // English order should be: Login, Logout, Welcome
        Assert.Equal("Login", englishKeys[0]);
        Assert.Equal("Logout", englishKeys[1]);
        Assert.Equal("Welcome", englishKeys[2]);

        // Assert Spanish file order (DIFFERENT from English)
        var spanishFile = Path.Combine(_exportDir, "Messages_es.resx");
        var spanishDoc = XDocument.Load(spanishFile);
        var spanishKeys = spanishDoc.Root.Elements("data").Select(e => e.Attribute("name")?.Value).ToList();
        
        // Spanish order should be: Welcome, Login, Logout (DIFFERENT!)
        Assert.Equal("Welcome", spanishKeys[0]);
        Assert.Equal("Login", spanishKeys[1]);
        Assert.Equal("Logout", spanishKeys[2]);
    }

    #endregion
}
