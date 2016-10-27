using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;
using NUnit.Framework;

public static class Decompiler
{
    public static string Decompile(string assemblyPath, string identifier = "")
    {
        var exePath = GetPathToILDasm();

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

    private static string GetPathToILDasm()
    {
        var path = ToolLocationHelper.GetPathToDotNetFrameworkSdkFile("ildasm.exe", TargetDotNetFrameworkVersion.Version40);
        if (!File.Exists(path))
            Assert.Fail("ILDasm could not be found");
        return path;
    }
}