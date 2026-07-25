using Xunit;

namespace AnkiBridge.Infrastructure.IntegrationTests.Common;

internal static class IntegrationTestEnvironment
{
    public static string Require(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"The required environment variable {variableName} is not set.");
        }

        return value.Trim();
    }
}

[AttributeUsage(AttributeTargets.Method)]
internal sealed class EnvironmentVariableFactAttribute : FactAttribute
{
    public EnvironmentVariableFactAttribute(string variableName)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variableName)))
            Skip = $"Set {variableName} to run this live-provider integration test.";
    }
}
