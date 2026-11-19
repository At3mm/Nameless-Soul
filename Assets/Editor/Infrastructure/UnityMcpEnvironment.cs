#if UNITY_EDITOR_WIN
using System;
using System.IO;
using UnityEditor;

// Unity Hub can retain an old PATH after uv is installed.
[InitializeOnLoad]
internal static class UnityMcpEnvironment
{
    static UnityMcpEnvironment()
    {
        Configure();
        EditorApplication.delayCall += Configure;
    }

    [MenuItem("Tools/Unity MCP/Refresh Environment")]
    private static void Configure()
    {
        // MCP dependencies have Windows wheels for Python 3.12. Letting uv select
        // Python 3.15 triggers source builds (for example PyYAML).
        Environment.SetEnvironmentVariable("UV_PYTHON", "3.12");

        var scripts = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
            "..", "Tools", "UnityMCP"));
        if (!File.Exists(Path.Combine(scripts, "uvx.exe"))) scripts = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Python", "Python312", "Scripts");
        var uvx = Path.Combine(scripts, "uvx.exe");
        if (!File.Exists(uvx)) return;

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (Array.FindIndex(path.Split(';'), entry =>
            string.Equals(entry.TrimEnd('\\'), scripts, StringComparison.OrdinalIgnoreCase)) < 0)
            Environment.SetEnvironmentVariable("PATH", scripts + ";" + path);

        var pythonRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "uv", "python", "cpython-3.12.15-windows-x86_64-none");
        var python = Path.Combine(pythonRoot, "python.exe");
        if (File.Exists(python))
        {
            Environment.SetEnvironmentVariable("UV_PYTHON", python);
            path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (Array.FindIndex(path.Split(';'), entry =>
                string.Equals(entry.TrimEnd('\\'), pythonRoot, StringComparison.OrdinalIgnoreCase)) < 0)
                Environment.SetEnvironmentVariable("PATH", pythonRoot + ";" + path);
        }

        const string preference = "MCPForUnity.UvxPath";
        var current = EditorPrefs.GetString(preference, string.Empty);
        if (string.IsNullOrWhiteSpace(current) || !File.Exists(current) ||
            current.IndexOf("Python312", StringComparison.OrdinalIgnoreCase) >= 0)
            EditorPrefs.SetString(preference, uvx);
    }
}
#endif
