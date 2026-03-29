namespace Medor.Api;

/// <summary>
/// Loads an optional <c>.env</c> file before configuration is built so values apply as environment variables.
/// </summary>
internal static class LocalEnv
{
    /// <summary>
    /// Loads the first existing <c>.env</c> file from candidate paths so DotNetEnv can merge values into configuration.
    /// </summary>
    internal static void LoadOptional()
    {
        foreach (var path in GetCandidateEnvPaths())
        {
            if (!File.Exists(path))
                continue;
            DotNetEnv.Env.Load(path);
            return;
        }
    }

    /// <summary>
    /// Returns ordered paths to try for a local <c>.env</c> (cwd, <c>Medor.Api/.env</c>, project root from bin).
    /// </summary>
    private static IEnumerable<string> GetCandidateEnvPaths()
    {
        var cwd = Directory.GetCurrentDirectory();
        yield return Path.Combine(cwd, ".env");
        yield return Path.Combine(cwd, "Medor.Api", ".env");

        var fromProjectRelativeToBin = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env"));
        yield return fromProjectRelativeToBin;
    }
}
