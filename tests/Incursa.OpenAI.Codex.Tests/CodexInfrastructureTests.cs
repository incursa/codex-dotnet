using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CodexInfrastructureTests
{
    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ConfigSerialization_FlattensNestedOverrides()
    {
        CodexConfigObject config = new()
        {
            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
            {
                ["sandbox"] = new CodexConfigObject
                {
                    Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                    {
                        ["workspace_write"] = new CodexConfigObject
                        {
                            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
                            {
                                ["network_access"] = new CodexConfigBooleanValue(false),
                            },
                        },
                    },
                },
                ["empty"] = new CodexConfigObject(),
                ["service"] = new CodexConfigStringValue("alpha"),
                ["count"] = new CodexConfigNumberValue(1.5),
                ["feature_enabled"] = new CodexConfigBooleanValue(true),
            },
        };

        IReadOnlyList<string> overrides = CodexConfigSerialization.FlattenConfigOverrides(config);

        Assert.Contains("sandbox.workspace_write.network_access=false", overrides);
        Assert.Contains("empty={}", overrides);
        Assert.Contains("service=\"alpha\"", overrides);
        Assert.Contains("count=1.5", overrides);
        Assert.Contains("feature_enabled=true", overrides);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [CoverageType(RequirementCoverageType.Negative)]
    public void ConfigSerialization_RejectsNonFiniteNumbers()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            CodexConfigSerialization.ToTomlLiteral(new CodexConfigNumberValue(double.NaN), "root.value"));

        Assert.Contains("finite number", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ConfigSerialization_ReturnsEmptyResultsForNullConfig()
    {
        Assert.Empty(CodexConfigSerialization.FlattenConfigOverrides(null));
        Assert.Empty(CodexProtocol.BuildConfigPayload(null));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ConfigSerialization_PreservesWhitespaceKeysInJsonPayload()
    {
        CodexConfigObject config = new()
        {
            Values = new Dictionary<string, CodexConfigValue>(StringComparer.Ordinal)
            {
                ["display name"] = new CodexConfigBooleanValue(true),
            },
        };

        JsonObject payload = CodexProtocol.BuildConfigPayload(config);

        Assert.True(payload["display name"]!.GetValue<bool>());
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0234")]
    [CoverageType(RequirementCoverageType.Positive)]
    public void ConfigSerialization_FormatsJsonNodesAsTomlLiterals()
    {
        JsonObject value = new()
        {
            ["display name"] = "alpha",
            ["enabled"] = true,
            ["count"] = 2.5,
            ["nested"] = new JsonObject
            {
                ["inner value"] = false,
            },
            ["tags"] = new JsonArray
            {
                "one",
                2,
                true,
                new JsonArray
                {
                    "deep",
                },
            },
        };

        string literal = CodexConfigSerialization.ToTomlLiteral(value, "root.value");

        Assert.Equal(
            "{\"display name\" = \"alpha\", enabled = true, count = 2.5, nested = {\"inner value\" = false}, tags = [\"one\", 2, true, [\"deep\"]]}",
            literal);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0245")]
    public async Task OutputSchemaFile_CreatesAndDeletesTempDirectory()
    {
        JsonObject schema = new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["name"] = new JsonObject { ["type"] = "string" },
            },
        };

        string? directoryPath;
        string? filePath;

        await using (CodexOutputSchemaFile schemaFile = await CodexOutputSchemaFile.CreateAsync(schema, CancellationToken.None))
        {
            directoryPath = schemaFile.DirectoryPath;
            filePath = schemaFile.FilePath;

            Assert.False(string.IsNullOrWhiteSpace(directoryPath));
            Assert.False(string.IsNullOrWhiteSpace(filePath));
            Assert.True(Directory.Exists(directoryPath));
            Assert.True(File.Exists(filePath));
        }

        Assert.NotNull(directoryPath);
        Assert.NotNull(filePath);
        Assert.False(Directory.Exists(directoryPath));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0236")]
    public void ExecutableResolver_BuildEnvironment_UsesExplicitMapWithoutAmbientLeak()
    {
        CodexClientOptions options = new()
        {
            Environment = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["CUSTOM_VAR"] = "custom-value",
            },
            ApiKey = "secret-key",
        };

        IReadOnlyDictionary<string, string> env = CodexExecutableResolver.BuildEnvironment(options);

        Assert.Equal("custom-value", env["CUSTOM_VAR"]);
        Assert.Equal("secret-key", env["CODEX_API_KEY"]);
        Assert.Equal("codex_sdk_dotnet", env["CODEX_INTERNAL_ORIGINATOR_OVERRIDE"]);
        Assert.DoesNotContain("PATH", env.Keys);
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0236")]
    public void ExecutableResolver_BuildEnvironment_CopiesAmbientEnvironmentWhenNoExplicitMap()
    {
        const string ambientKey = "CODEX_TEST_AMBIENT_ENV";
        string? originalValue = Environment.GetEnvironmentVariable(ambientKey);

        try
        {
            Environment.SetEnvironmentVariable(ambientKey, "ambient-value");

            CodexClientOptions options = new()
            {
                ApiKey = "secret-key",
            };

            IReadOnlyDictionary<string, string> env = CodexExecutableResolver.BuildEnvironment(options);

            Assert.Equal("ambient-value", env[ambientKey]);
            Assert.Equal("secret-key", env["CODEX_API_KEY"]);
            Assert.Equal("codex_sdk_dotnet", env["CODEX_INTERNAL_ORIGINATOR_OVERRIDE"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ambientKey, originalValue);
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-STRUCTURE-0286")]
    public void ExecutableResolver_UsesExplicitOverridePath()
    {
        string executablePath = Path.GetTempFileName();

        try
        {
            CodexClientOptions options = new()
            {
                CodexPathOverride = executablePath,
            };

            Assert.Equal(executablePath, CodexExecutableResolver.Resolve(options));
            Assert.True(CodexExecutableResolver.IsAvailable(options));
        }
        finally
        {
            if (File.Exists(executablePath))
            {
                File.Delete(executablePath);
            }
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-STRUCTURE-0286")]
    public void ExecutableResolver_FindsExecutableOnPath()
    {
        string? originalPath = Environment.GetEnvironmentVariable("PATH");
        string tempDirectory = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        string executableName = OperatingSystem.IsWindows() ? "codex.exe" : "codex";
        string executablePath = Path.Combine(tempDirectory, executableName);
        File.WriteAllText(executablePath, string.Empty);

        try
        {
            Environment.SetEnvironmentVariable("PATH", tempDirectory);

            string resolved = CodexExecutableResolver.Resolve(new CodexClientOptions());

            Assert.Equal(executablePath, resolved);
            Assert.True(CodexExecutableResolver.IsAvailable(new CodexClientOptions()));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);

            if (File.Exists(executablePath))
            {
                File.Delete(executablePath);
            }

            if (Directory.Exists(tempDirectory))
            {
                DeleteDirectoryWithRetry(tempDirectory);
            }
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-STRUCTURE-0286")]
    public void ExecutableResolver_ThrowsWhenExecutableCannotBeFound()
    {
        string? originalPath = Environment.GetEnvironmentVariable("PATH");

        try
        {
            Environment.SetEnvironmentVariable("PATH", string.Empty);

            FileNotFoundException exception = Assert.Throws<FileNotFoundException>(() =>
                CodexExecutableResolver.Resolve(new CodexClientOptions()));

            Assert.Contains("Unable to locate the Codex executable", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-STRUCTURE-0286")]
    public void ExecutableResolver_IsAvailable_RejectsNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() => CodexExecutableResolver.IsAvailable(null!));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-STRUCTURE-0286")]
    public void ExecutableResolver_IsAvailable_ReturnsFalseForMissingOverridePath()
    {
        string missingPath = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"), "missing-codex.exe");

        CodexClientOptions options = new()
        {
            CodexPathOverride = missingPath,
        };

        Assert.False(CodexExecutableResolver.IsAvailable(options));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-STRUCTURE-0286")]
    public void ExecutableResolver_TreatsRelativeOverrideWithSeparatorAsPath()
    {
        string originalCurrentDirectory = Environment.CurrentDirectory;
        string tempDirectory = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"));
        string nestedDirectory = Path.Combine(tempDirectory, "nested");
        string executableName = OperatingSystem.IsWindows() ? "codex.exe" : "codex";
        string relativePath = Path.Combine("nested", executableName);

        Directory.CreateDirectory(nestedDirectory);
        File.WriteAllText(Path.Combine(nestedDirectory, executableName), string.Empty);

        try
        {
            Environment.CurrentDirectory = tempDirectory;

            CodexClientOptions options = new()
            {
                CodexPathOverride = relativePath,
            };

            Assert.Equal(relativePath, CodexExecutableResolver.Resolve(options));
            Assert.True(CodexExecutableResolver.IsAvailable(options));
        }
        finally
        {
            Environment.CurrentDirectory = originalCurrentDirectory;

            if (Directory.Exists(tempDirectory))
            {
                DeleteDirectoryWithRetry(tempDirectory);
            }
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0236")]
    public async Task ProcessLauncher_StartAsync_TracksProcessLifecycle()
    {
        ProcessCodexProcessLauncher launcher = new();
        string tempDirectory = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        string fileName;
        IReadOnlyList<string> arguments;
        if (OperatingSystem.IsWindows())
        {
            fileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            arguments = ["/c", "timeout /t 2 /nobreak >nul"];
        }
        else
        {
            fileName = "/bin/sh";
            arguments = ["-c", "sleep 2"];
        }

        try
        {
            ICodexProcess process = await launcher.StartAsync(
                new CodexProcessStartInfo(
                    fileName,
                    arguments,
                    tempDirectory,
                    new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["CODEX_TEST_FLAG"] = "1",
                    }),
                CancellationToken.None);

            Assert.NotNull(process.ProcessId);
            Assert.False(process.HasExited);
            Assert.NotNull(process.StandardInput);
            Assert.NotNull(process.StandardOutput);
            Assert.NotNull(process.StandardError);

            await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            Assert.True(process.HasExited);
            Assert.Null(process.ProcessId);

            process.Kill();
            Assert.True(process.HasExited);

            await process.DisposeAsync();
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                DeleteDirectoryWithRetry(tempDirectory);
            }
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0236")]
    public async Task ProcessLauncher_StartAsync_UsesWorkingDirectory()
    {
        ProcessCodexProcessLauncher launcher = new();
        string tempDirectory = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        string fileName;
        IReadOnlyList<string> arguments;
        if (OperatingSystem.IsWindows())
        {
            fileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            arguments = ["/c", "cd"];
        }
        else
        {
            fileName = "/bin/sh";
            arguments = ["-c", "pwd"];
        }

        try
        {
            ICodexProcess process = await launcher.StartAsync(
                new CodexProcessStartInfo(
                    fileName,
                    arguments,
                    tempDirectory,
                    new Dictionary<string, string>(StringComparer.Ordinal)),
                CancellationToken.None);

            string? output = await process.StandardOutput.ReadLineAsync();
            await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            Assert.Equal(tempDirectory, output?.Trim());
            await process.DisposeAsync();
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                DeleteDirectoryWithRetry(tempDirectory);
            }
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0236")]
    public async Task ProcessLauncher_StartAsync_IgnoresWhitespaceWorkingDirectory()
    {
        ProcessCodexProcessLauncher launcher = new();
        string currentDirectory = Environment.CurrentDirectory;

        string fileName;
        IReadOnlyList<string> arguments;
        if (OperatingSystem.IsWindows())
        {
            fileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            arguments = ["/c", "cd"];
        }
        else
        {
            fileName = "/bin/sh";
            arguments = ["-c", "pwd"];
        }

        ICodexProcess process = await launcher.StartAsync(
            new CodexProcessStartInfo(
                fileName,
                arguments,
                "   ",
                new Dictionary<string, string>(StringComparer.Ordinal)),
            CancellationToken.None);

        string? output = await process.StandardOutput.ReadLineAsync();
        await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

        Assert.Equal(currentDirectory, output?.Trim());
        await process.DisposeAsync();
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0238")]
    [CoverageType(RequirementCoverageType.Positive)]
    public async Task ProcessLauncher_StartAsync_PinsRedirectedTextPipesToUtf8()
    {
        ProcessCodexProcessLauncher launcher = new();
        string tempDirectory = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        string fileName;
        IReadOnlyList<string> arguments;
        if (OperatingSystem.IsWindows())
        {
            fileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            arguments = ["/c", "echo hello"];
        }
        else
        {
            fileName = "/bin/sh";
            arguments = ["-c", "printf 'hello\\n'"];
        }

        try
        {
            await using ICodexProcess process = await launcher.StartAsync(
                new CodexProcessStartInfo(
                    fileName,
                    arguments,
                    tempDirectory,
                    new Dictionary<string, string>(StringComparer.Ordinal)),
                CancellationToken.None);

            _ = await process.StandardOutput.ReadLineAsync();
            await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            StreamReader stdout = Assert.IsType<StreamReader>(process.StandardOutput);
            StreamReader stderr = Assert.IsType<StreamReader>(process.StandardError);
            StreamWriter stdin = Assert.IsType<StreamWriter>(process.StandardInput);

            Assert.Equal(Encoding.UTF8.CodePage, stdout.CurrentEncoding.CodePage);
            Assert.Equal(Encoding.UTF8.CodePage, stderr.CurrentEncoding.CodePage);
            Assert.Equal(Encoding.UTF8.CodePage, stdin.Encoding.CodePage);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                DeleteDirectoryWithRetry(tempDirectory);
            }
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task ProcessCodexProcess_KillTerminatesLiveProcess()
    {
        ProcessCodexProcessLauncher launcher = new();
        string tempDirectory = Path.Combine(Path.GetTempPath(), "codex-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        string fileName;
        IReadOnlyList<string> arguments;
        if (OperatingSystem.IsWindows())
        {
            fileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            arguments = ["/c", "timeout /t 20 /nobreak >nul"];
        }
        else
        {
            fileName = "/bin/sh";
            arguments = ["-c", "sleep 20"];
        }

        try
        {
            ICodexProcess process = await launcher.StartAsync(
                new CodexProcessStartInfo(
                    fileName,
                    arguments,
                    tempDirectory,
                    new Dictionary<string, string>(StringComparer.Ordinal)),
                CancellationToken.None);

            process.Kill();
            await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            Assert.True(process.HasExited);
            Assert.Null(process.ProcessId);

            await process.DisposeAsync();
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                DeleteDirectoryWithRetry(tempDirectory);
            }
        }
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    [CoverageType(RequirementCoverageType.Edge)]
    public async Task ProcessCodexProcess_DisposeAsync_TerminatesLiveProcess()
    {
        ProcessCodexProcessLauncher launcher = new();
        string workingDirectory = Path.GetTempPath();

        string fileName;
        IReadOnlyList<string> arguments;
        if (OperatingSystem.IsWindows())
        {
            fileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            arguments = ["/c", "timeout /t 20 /nobreak >nul"];
        }
        else
        {
            fileName = "/bin/sh";
            arguments = ["-c", "sleep 20"];
        }

        ICodexProcess process = await launcher.StartAsync(
            new CodexProcessStartInfo(
                fileName,
                arguments,
                workingDirectory,
                new Dictionary<string, string>(StringComparer.Ordinal)),
            CancellationToken.None);

        int processId = process.ProcessId ?? throw new InvalidOperationException("Expected a live process id.");
        await process.DisposeAsync();

        try
        {
            using Process liveProcess = Process.GetProcessById(processId);
            Assert.True(liveProcess.WaitForExit(5000));
            Assert.True(liveProcess.HasExited);
        }
        catch (ArgumentException)
        {
            // The process has already exited and no longer exists in the OS process table.
        }
    }
    private static void DeleteDirectoryWithRetry(string directoryPath)
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    return;
                }

                Directory.Delete(directoryPath, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 9)
            {
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 9)
            {
                Thread.Sleep(100);
            }
        }
    }
}
