using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static class Decompiler
{
    static string exePath;

    static Decompiler()
    {
        var windowsSdk = Environment.ExpandEnvironmentVariables(@"%programfiles(x86)%\Microsoft SDKs\Windows\");
        exePath = Directory.EnumerateFiles(windowsSdk, "ildasm.exe", SearchOption.AllDirectories)
            .OrderByDescending(x =>
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(x);
                return new Version(fileVersionInfo.FileMajorPart, fileVersionInfo.FileMinorPart, fileVersionInfo.FileBuildPart);
            })
            .FirstOrDefault();
        if (exePath == null)
        {
            throw new Exception("Could not find path to ildasm.exe");
        }
    }

    public static string Decompile(string assemblyPath, string identifier = "")
    {
        if (!string.IsNullOrEmpty(identifier))
            identifier = "/item:" + identifier;

        using (var process = Process.Start(new ProcessStartInfo(exePath, $"\"{assemblyPath}\" /text /linenum {identifier}")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            var projectFolder = Path.GetFullPath(Path.GetDirectoryName(assemblyPath) + "\\..\\..\\..").Replace("\\", "\\\\") + "\\\\";
            var lprojectFolder = $"{char.ToLower(projectFolder[0])}{projectFolder.Substring(1)}";

            process.WaitForExit(10000);

            return string.Join(Environment.NewLine, Regex.Split(process.StandardOutput.ReadToEnd(), Environment.NewLine)
                    .Where(l => !l.StartsWith("// ") && !string.IsNullOrEmpty(l))
                    .Select(l => l.Replace(projectFolder, ""))
                    .Select(l => l.Replace(lprojectFolder, ""))
                    .ToList());
        }
    }

}