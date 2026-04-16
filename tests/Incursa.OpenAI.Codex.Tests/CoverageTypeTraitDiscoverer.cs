using Xunit.Abstractions;
using Xunit.Sdk;

namespace Incursa.OpenAI.Codex.Tests;

public sealed class CoverageTypeTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        object? coverageType = traitAttribute.GetConstructorArguments().FirstOrDefault();

        if (coverageType is not RequirementCoverageType typedCoverageType)
        {
            return [];
        }

        return [new("CoverageType", typedCoverageType.ToString())];
    }
}
