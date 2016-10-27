using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mono.Cecil;
using NUnit.Framework;

public static class AssemblyWeaver
{
    public static Assembly Assembly;

    static AssemblyWeaver()
    {
        BeforeAssemblyPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\..\AssemblyToProcess\bin\Debug\AssemblyToProcess.dll"));
        BeforeAssemblyPathSymbols = Path.ChangeExtension(BeforeAssemblyPath, "pdb");

#if (!DEBUG)
        BeforeAssemblyPath = BeforeAssemblyPath.Replace("Debug", "Release");
        BeforeAssemblyPathSymbols = BeforeAssemblyPathSymbols.Replace("Debug", "Release");
#endif
        AfterAssemblyPath = BeforeAssemblyPath.Replace(".dll", "2.dll");
        AfterAssemblyPathSymbols = Path.ChangeExtension(AfterAssemblyPath, "pdb");

        File.Copy(BeforeAssemblyPath, AfterAssemblyPath, true);
        File.Copy(BeforeAssemblyPathSymbols, AfterAssemblyPathSymbols, true);

        var assemblyResolver = new MockAssemblyResolver();
        var moduleDefinition = ModuleDefinition.ReadModule(AfterAssemblyPath, new ReaderParameters { ReadSymbols = true });

        var weavingTask = new ModuleWeaver
        {
            ModuleDefinition = moduleDefinition,
            AssemblyResolver = assemblyResolver,
            LogError = LogError,
            LogInfo = LogInfo,
            DefineConstants = new[] { "DEBUG" }, // Always testing the debug weaver
        };

        weavingTask.Execute();
        moduleDefinition.Write(AfterAssemblyPath, new WriterParameters { WriteSymbols = true });

        Assembly = Assembly.LoadFile(AfterAssemblyPath);
    }

    public static string BeforeAssemblyPath;
    public static string AfterAssemblyPath;

    public static string BeforeAssemblyPathSymbols;
    public static string AfterAssemblyPathSymbols;

    private static void LogInfo(string error)
    {
        Infos.Add(error);
    }

    private static void LogError(string error)
    {
        Errors.Add(error);
    }

    public static List<string> Infos = new List<string>();
    public static List<string> Errors = new List<string>();
}