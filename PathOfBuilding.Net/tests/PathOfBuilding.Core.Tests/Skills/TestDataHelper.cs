namespace PathOfBuilding.Core.Tests.Skills;

internal static class TestDataHelper
{
    /// <summary>
    /// Resolves the path to src/Data/ in the repository root.
    /// Walks up from the test output directory to find the repo root.
    /// </summary>
    public static string GetDataBasePath()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "src", "Data");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException(
            "Could not find src/Data/ directory. " +
            $"Searched upward from {AppContext.BaseDirectory}");
    }

    public static string GetGemsLuaPath() => Path.Combine(GetDataBasePath(), "Gems.lua");
    public static string GetSkillStatMapPath() => Path.Combine(GetDataBasePath(), "SkillStatMap.lua");
    public static string GetSkillsDir() => Path.Combine(GetDataBasePath(), "Skills");
    public static string GetSkillFile(string filename) => Path.Combine(GetSkillsDir(), filename);
}
