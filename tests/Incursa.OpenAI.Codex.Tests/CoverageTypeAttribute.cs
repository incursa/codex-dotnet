using Xunit.Sdk;

namespace Incursa.OpenAI.Codex.Tests;

[TraitDiscoverer("Incursa.OpenAI.Codex.Tests.CoverageTypeTraitDiscoverer", "Incursa.OpenAI.Codex.Tests")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class CoverageTypeAttribute : Attribute, ITraitAttribute
{
    public CoverageTypeAttribute(RequirementCoverageType coverageType)
    {
        CoverageType = coverageType;
    }

    /// <summary>
    /// Gets the coverage type.
    /// </summary>
    public RequirementCoverageType CoverageType { get; }
}
