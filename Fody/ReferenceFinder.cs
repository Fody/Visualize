using System.Linq;
using Mono.Cecil;

public static class ReferenceFinder
{
    public static TypeReference SystemType;
    public static TypeReference DebuggerBrowsableStateType;
    public static MethodReference ListCtor;
    public static MethodReference ListToArray;
    public static MethodReference DebuggerBrowsableAttributeCtor;
    public static MethodReference DebuggerDisplayAttributeCtor;
    public static MethodReference DebuggerTypeProxyAttributeCtor;
    public static MethodReference CompilerGeneratedAttributeCtor;

    public static void FindReferences(IAssemblyResolver assemblyResolver, ModuleDefinition moduleDefinition)
    {
        var baseLib = assemblyResolver.Resolve("mscorlib");
        var baseLibTypes = baseLib.MainModule.Types;

        var winrt = !baseLibTypes.Any(type => type.Name == "Object");
        if (winrt)
        {
            baseLib = assemblyResolver.Resolve("System.Runtime");
            baseLibTypes = baseLib.MainModule.Types;
        }

        var debugLib = !winrt ? baseLib : assemblyResolver.Resolve("System.Diagnostics.Debug");
        var debugLibTypes = debugLib.MainModule.Types;

        var collectionsLib = !winrt ? baseLib : assemblyResolver.Resolve("System.Collections");
        var collectionsLibTypes = collectionsLib.MainModule.Types;

        SystemType = moduleDefinition.Import(baseLibTypes.First(t => t.Name == "Type"));
        DebuggerBrowsableStateType = moduleDefinition.Import(debugLibTypes.First(t => t.Name == "DebuggerBrowsableState"));

        var listType = collectionsLibTypes.First(t => t.Name == "List`1");
        ListCtor = moduleDefinition.Import(listType.Methods.First(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name.StartsWith("IEnumerable")));
        ListToArray = moduleDefinition.Import(listType.Methods.First(m => m.Name == "ToArray"));

        var debuggerBrowsableAttribute = debugLibTypes.First(t => t.Name == "DebuggerBrowsableAttribute");
        DebuggerBrowsableAttributeCtor = moduleDefinition.Import(debuggerBrowsableAttribute.Methods.First(x => x.IsConstructor));

        var debuggerDisplayAttribute = debugLibTypes.First(t => t.Name == "DebuggerDisplayAttribute");
        DebuggerDisplayAttributeCtor = moduleDefinition.Import(debuggerDisplayAttribute.Methods.First(x => x.IsConstructor));

        var debuggerTypeProxyAttribute = debugLibTypes.First(t => t.Name == "DebuggerTypeProxyAttribute");
        DebuggerTypeProxyAttributeCtor = moduleDefinition.Import(debuggerTypeProxyAttribute.Methods.First(x => x.IsConstructor &&
            x.Parameters[0].ParameterType.FullName == "System.Type"));

        var compilerGeneratedAttribute = baseLibTypes.First(t => t.Name == "CompilerGeneratedAttribute");
        CompilerGeneratedAttributeCtor = moduleDefinition.Import(compilerGeneratedAttribute.Methods.First(x => x.IsConstructor));
    }
}