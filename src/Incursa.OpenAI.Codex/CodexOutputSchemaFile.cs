using System.Text.Json;
using System.Text.Json.Nodes;

namespace Incursa.OpenAI.Codex;

internal sealed class CodexOutputSchemaFile : IAsyncDisposable
{
    private CodexOutputSchemaFile(string? directoryPath, string? filePath)
    {
        DirectoryPath = directoryPath;
        FilePath = filePath;
    }

    public string? DirectoryPath { get; }

    public string? FilePath { get; }

    public static async Task<CodexOutputSchemaFile> CreateAsync(JsonNode? schema, CancellationToken cancellationToken)
    {
        if (schema is null)
        {
            return new CodexOutputSchemaFile(directoryPath: null, filePath: null);
        }

        if (schema is not JsonObject jsonObject)
        {
            throw new InvalidOperationException("outputSchema must be a plain JSON object");
        }

        string directoryPath = Path.Combine(Path.GetTempPath(), $"codex-output-schema-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);
        string filePath = Path.Combine(directoryPath, "schema.json");

        try
        {
            string json = jsonObject.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false,
            });

            await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
            return new CodexOutputSchemaFile(directoryPath, filePath);
        }
        catch
        {
            try
            {
                Directory.Delete(directoryPath, recursive: true);
            }
            catch
            {
                // Suppress cleanup failures.
            }

            throw;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (DirectoryPath is null)
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
        catch
        {
            // Suppress cleanup failures.
        }

        return ValueTask.CompletedTask;
    }
}
