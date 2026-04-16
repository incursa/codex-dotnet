using System.Text;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex.Tests;

// Traceability: REQ-CODEX-SDK-0103.

[Collection("Live Codex")]
public sealed class CodexSampleFlowLiveTests
{
    private const string RedPixelPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAANSURBVBhXY/jPwPAfAAUAAf+mXJtdAAAAAElFTkSuQmCC";

    [LiveCodexFact]
    [Trait("Category", "Integration")]
    [Trait("Requirement", "REQ-CODEX-SDK-0103")]
    [Trait("Requirement", "REQ-CODEX-SDK-API-0211")]
    public async Task AppServerStructuredOutputExample_ReturnsJsonMatchingTheSchema()
    {
        string workDir = await CreateWorkspaceAsync();
        try
        {
            await using CodexClient client = new(LiveCodexIntegration.CreateAppServerClientOptions());
            CodexThread thread = await client.StartThreadAsync(LiveCodexIntegration.CreateThreadOptions(workDir));

            JsonObject schema = new()
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["answer"] = new JsonObject { ["type"] = "string" },
                    ["confidence"] = new JsonObject { ["type"] = "number" },
                },
                ["required"] = new JsonArray("answer", "confidence"),
                ["additionalProperties"] = false,
            };

            CodexRunResult result = await thread.RunAsync(
                "Read sample.txt in the current directory and return only a JSON object with answer equal to the token on line 1 and confidence equal to 1. Do not wrap the JSON in quotes or markdown.",
                new CodexTurnOptions
                {
                    OutputSchema = schema,
                });

            JsonObject response = ParseStructuredOutput(result.FinalResponse);
            Assert.Equal("alpha", response["answer"]!.GetValue<string>());
            Assert.Equal(1.0, response["confidence"]!.GetValue<double>());
        }
        finally
        {
            DeleteDirectory(workDir);
        }
    }

    [LiveCodexFact]
    [Trait("Category", "Integration")]
    [Trait("Requirement", "REQ-CODEX-SDK-0103")]
    [Trait("Requirement", "REQ-CODEX-SDK-TRANSPORT-0233")]
    public async Task AppServerImageInputExample_AcceptsLocalImageInput()
    {
        string workDir = await CreateWorkspaceAsync();
        string imagePath = Path.Combine(workDir, "image.png");
        await File.WriteAllBytesAsync(imagePath, Convert.FromBase64String(RedPixelPngBase64));

        try
        {
            await using CodexClient client = new(LiveCodexIntegration.CreateAppServerClientOptions());
            CodexThread thread = await client.StartThreadAsync(LiveCodexIntegration.CreateThreadOptions(workDir));

            CodexRunResult result = await thread.RunAsync(
                [
                    new CodexTextInput { Text = "Reply with YES if the image input was accepted, otherwise NO." },
                    new CodexLocalImageInput { Path = imagePath },
                ]);

            Assert.Contains("yes", result.FinalResponse ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(workDir);
        }
    }

    [LiveCodexFact]
    [Trait("Category", "Integration")]
    [Trait("Requirement", "REQ-CODEX-SDK-0103")]
    [Trait("Requirement", "REQ-CODEX-SDK-HELPERS-0314")]
    public async Task ExecErrorHandlingExample_DisallowsThreadListingAndStillRunsPrompt()
    {
        string workDir = await CreateWorkspaceAsync();
        try
        {
            await using CodexClient client = new(LiveCodexIntegration.CreateClientOptions());

            CodexCapabilityNotSupportedException exception = await Assert.ThrowsAsync<CodexCapabilityNotSupportedException>(
                () => client.ListThreadsAsync(new CodexThreadListOptions { Limit = 5 }));

            Assert.Equal(nameof(CodexClient.ListThreadsAsync), exception.Operation);
            Assert.Equal(CodexBackendSelection.Exec, exception.BackendSelection);

            CodexThread thread = await client.StartThreadAsync(LiveCodexIntegration.CreateThreadOptions(workDir));
            CodexRunResult result = await thread.RunAsync(
                "Read sample.txt in the current directory and reply with exactly VALUE=<token> for the token on line 1.");

            Assert.Equal("VALUE=alpha", result.FinalResponse);
        }
        finally
        {
            DeleteDirectory(workDir);
        }
    }

    [LiveCodexFact]
    [Trait("Category", "Integration")]
    [Trait("Requirement", "REQ-CODEX-SDK-0103")]
    [Trait("Requirement", "REQ-CODEX-SDK-LIFECYCLE-0296")]
    public async Task TurnControlsExample_EmitsSteeringAndCompletionEvents()
    {
        string workDir = await CreateWorkspaceAsync();
        try
        {
            StringWriter output = new();
            StringWriter error = new();

            int exitCode = await global::ProgramEntry.RunAsync(
                [
                    "--mode",
                    "turn-controls",
                    "--prompt",
                    "Read sample.txt in the current directory and reply with exactly VALUE=<token> for the token on line 1.",
                    "--cwd",
                    workDir,
                    "--interrupt",
                ],
                output,
                error);

            Assert.True(exitCode == 0, $"STDOUT:{Environment.NewLine}{output}STDERR:{Environment.NewLine}{error}");
            Assert.Contains("turn.started", output.ToString());
            Assert.True(
                output.ToString().Contains("turn.completed", StringComparison.Ordinal)
                || output.ToString().Contains("turn.failed", StringComparison.Ordinal),
                $"Expected turn completion output. Actual output:{Environment.NewLine}{output}");
            Assert.Empty(error.ToString());
        }
        finally
        {
            DeleteDirectory(workDir);
        }
    }

    private static async Task<string> CreateWorkspaceAsync()
    {
        string directoryPath = Path.Combine(Path.GetTempPath(), "codex-sample-flow-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);

        string filePath = Path.Combine(directoryPath, "sample.txt");
        string content = string.Join(
            Environment.NewLine,
            "token: alpha",
            "token: beta");

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        return directoryPath;
    }

    private static void DeleteDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static JsonObject ParseStructuredOutput(string? finalResponse)
    {
        Assert.False(string.IsNullOrWhiteSpace(finalResponse));

        JsonNode node = JsonNode.Parse(finalResponse!) ?? throw new InvalidOperationException("Structured output response was empty.");
        if (node is JsonObject response && response["answer"] is JsonValue answerValue && answerValue.TryGetValue<string>(out string? nestedJson) && LooksLikeJsonPayload(nestedJson))
        {
            if (TryParseJsonObject(nestedJson, out JsonObject? nestedResponse))
            {
                return nestedResponse ?? throw new InvalidOperationException("Nested structured output response was empty.");
            }
        }

        if (node is JsonValue value && value.TryGetValue<string>(out string? text) && LooksLikeJsonPayload(text) && TryParseJsonObject(text, out JsonObject? directResponse))
        {
            return directResponse ?? throw new InvalidOperationException("Structured output response was empty.");
        }

        return node as JsonObject ?? throw new InvalidOperationException("Structured output response was not an object.");
    }

    private static bool TryParseJsonObject(string? text, out JsonObject? response)
    {
        response = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        JsonNode? node = JsonNode.Parse(text);
        if (node is JsonObject jsonObject)
        {
            response = jsonObject;
            return true;
        }

        return false;
    }

    private static bool LooksLikeJsonPayload(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        ReadOnlySpan<char> trimmed = text.AsSpan().TrimStart();
        return trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '[');
    }
}
