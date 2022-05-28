namespace Microsoft.Extensions.Hosting;

public static class HostEnvironmentExtension
{
    public static bool IsLocalhost(this IHostEnvironment environment)
    {
        return string.Equals(
                             environment.EnvironmentName,
                             "Localhost",
                             StringComparison.OrdinalIgnoreCase);
    }
}