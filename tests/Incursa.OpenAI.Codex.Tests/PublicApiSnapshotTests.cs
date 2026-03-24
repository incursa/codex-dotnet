using System.Reflection;
using System.Text;
using Incursa.OpenAI.Codex.Extensions;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class PublicApiSnapshotTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-0101")]
    [Trait("Requirement", "REQ-CODEX-SDK-DI-0269")]
    public void CorePublicApiSnapshot_MatchesShippedBaseline()
    {
        VerifyAssemblySnapshot(
            typeof(CodexClient).Assembly,
            Path.Combine(RepoRoot, "src", "Incursa.OpenAI.Codex", "PublicAPI.Shipped.txt"),
            Path.Combine(RepoRoot, "src", "Incursa.OpenAI.Codex", "PublicAPI.Unshipped.txt"));
    }

    [Fact]
    [Trait("Requirement", "REQ-CODEX-SDK-0101")]
    [Trait("Requirement", "REQ-CODEX-SDK-DI-0269")]
    public void ExtensionsPublicApiSnapshot_MatchesShippedBaseline()
    {
        VerifyAssemblySnapshot(
            typeof(CodexServiceCollectionExtensions).Assembly,
            Path.Combine(RepoRoot, "src", "Incursa.OpenAI.Codex.Extensions", "PublicAPI.Shipped.txt"),
            Path.Combine(RepoRoot, "src", "Incursa.OpenAI.Codex.Extensions", "PublicAPI.Unshipped.txt"));
    }

    private static void VerifyAssemblySnapshot(Assembly assembly, string shippedPath, string unshippedPath)
    {
        string expected = BuildPublicApiSnapshot(assembly);
        EnsureUnshippedFileExists(unshippedPath);

        if (ShouldUpdateBaselines())
        {
            File.WriteAllText(shippedPath, expected, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return;
        }

        string actual = File.Exists(shippedPath)
            ? File.ReadAllText(shippedPath)
            : string.Empty;

        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(actual));
    }

    private static bool ShouldUpdateBaselines()
        => string.Equals(Environment.GetEnvironmentVariable("UPDATE_PUBLIC_API_BASELINES"), "1", StringComparison.Ordinal);

    private static void EnsureUnshippedFileExists(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    private static string NormalizeLineEndings(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string BuildPublicApiSnapshot(Assembly assembly)
    {
        List<string> lines = [];

        foreach (Type type in assembly.GetExportedTypes().OrderBy(static type => type.FullName, StringComparer.Ordinal))
        {
            lines.Add($"T:{FormatType(type)}");
            lines.AddRange(GetMemberLines(type));
        }

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private static IEnumerable<string> GetMemberLines(Type type)
    {
        List<string> lines = [];

        IEnumerable<ConstructorInfo> constructors = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(static ctor => ctor.ToString(), StringComparer.Ordinal);

        foreach (ConstructorInfo constructor in constructors)
        {
            lines.Add($"C:{FormatType(type)}({FormatParameters(constructor.GetParameters())})");
        }

        IEnumerable<PropertyInfo> properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(static property => property.Name, StringComparer.Ordinal);

        foreach (PropertyInfo property in properties)
        {
            string accessors = FormatPropertyAccessors(property);
            lines.Add($"P:{FormatType(type)}.{property.Name}:{FormatType(property.PropertyType)} {accessors}");
        }

        IEnumerable<FieldInfo> fields = type
            .GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(static field => field.Name, StringComparer.Ordinal);

        foreach (FieldInfo field in fields)
        {
            lines.Add($"F:{FormatType(type)}.{field.Name}:{FormatType(field.FieldType)}");
        }

        IEnumerable<MethodInfo> methods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(static method => !IsAccessorMethod(method))
            .OrderBy(static method => method.ToString(), StringComparer.Ordinal);

        foreach (MethodInfo method in methods)
        {
            string methodName = method.IsGenericMethodDefinition
                ? $"{method.Name}<{string.Join(", ", method.GetGenericArguments().Select(static argument => argument.Name))}>"
                : method.Name;

            string signature = $"M:{FormatType(type)}.{methodName}({FormatParameters(method.GetParameters())})";
            lines.Add($"{signature}:{FormatType(method.ReturnType)}");
        }

        IEnumerable<EventInfo> events = type
            .GetEvents(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(static @event => @event.Name, StringComparer.Ordinal);

        foreach (EventInfo @event in events)
        {
            lines.Add($"E:{FormatType(type)}.{@event.Name}:{FormatType(@event.EventHandlerType ?? typeof(void))}");
        }

        return lines;
    }

    private static bool IsAccessorMethod(MethodInfo method)
    {
        if (!method.IsSpecialName)
        {
            return false;
        }

        return method.Name.StartsWith("get_", StringComparison.Ordinal)
            || method.Name.StartsWith("set_", StringComparison.Ordinal)
            || method.Name.StartsWith("add_", StringComparison.Ordinal)
            || method.Name.StartsWith("remove_", StringComparison.Ordinal);
    }

    private static string FormatPropertyAccessors(PropertyInfo property)
    {
        List<string> accessorParts = [];
        if (property.GetMethod is not null && property.GetMethod.IsPublic)
        {
            accessorParts.Add("get;");
        }

        if (property.SetMethod is not null && property.SetMethod.IsPublic)
        {
            accessorParts.Add("set;");
        }

        return string.Join(" ", accessorParts);
    }

    private static string FormatParameters(IReadOnlyList<ParameterInfo> parameters)
    {
        return string.Join(", ", parameters.Select(FormatParameter));
    }

    private static string FormatParameter(ParameterInfo parameter)
    {
        Type parameterType = parameter.ParameterType;
        string prefix = string.Empty;
        if (parameterType.IsByRef)
        {
            parameterType = parameterType.GetElementType()!;
            prefix = parameter.IsOut ? "out " : "ref ";
        }

        if (Attribute.IsDefined(parameter, typeof(ParamArrayAttribute)))
        {
            prefix = "params ";
        }

        string formattedType = FormatType(parameterType);
        if (parameter.IsOptional)
        {
            return $"{prefix}{formattedType} {parameter.Name} = optional";
        }

        return $"{prefix}{formattedType} {parameter.Name}";
    }

    private static string FormatType(Type type)
    {
        if (type.IsByRef)
        {
            return $"{FormatType(type.GetElementType()!)}&";
        }

        if (type.IsPointer)
        {
            return $"{FormatType(type.GetElementType()!)}*";
        }

        if (type.IsArray)
        {
            return $"{FormatType(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
        }

        if (type.IsGenericParameter)
        {
            return type.Name;
        }

        if (type.IsGenericType)
        {
            string? genericTypeName = type.GetGenericTypeDefinition().FullName;
            string baseName = (genericTypeName ?? type.Name).Split('`')[0].Replace('+', '.');
            string arguments = string.Join(", ", type.GetGenericArguments().Select(FormatType));
            return $"{baseName}<{arguments}>";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }
}
