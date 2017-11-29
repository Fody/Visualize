using System.Linq;
using Mono.Cecil;

public class ReferenceFinder
{
    public TypeReference SystemType;
    public TypeReference DebuggerBrowsableStateType;
    public MethodReference ListCtor;
    public MethodReference ListToArray;
    public MethodReference DebuggerBrowsableAttributeCtor;
    public MethodReference DebuggerDisplayAttributeCtor;
    public MethodReference DebuggerTypeProxyAttributeCtor;
    public MethodReference CompilerGeneratedAttributeCtor;
    public MethodReference StringFormat;

    public void FindReferences(IAssemblyResolver assemblyResolver, ModuleDefinition moduleDefinition)
    {
        var baseLib = assemblyResolver.Resolve(new AssemblyNameReference("mscorlib", null));
        var baseLibTypes = baseLib.MainModule.Types;

        var winrt = baseLibTypes.All(type => type.Name != "Object");
        if (winrt)
        {
            baseLib = assemblyResolver.Resolve(new AssemblyNameReference("System.Runtime", null));
            baseLibTypes = baseLib.MainModule.Types;
        }

        var debugLib = !winrt ? baseLib : assemblyResolver.Resolve(new AssemblyNameReference("System.Diagnostics.Debug", null));
        var debugLibTypes = debugLib.MainModule.Types;

        var collectionsLib = !winrt ? baseLib : assemblyResolver.Resolve(new AssemblyNameReference("System.Collections", null));
        var collectionsLibTypes = collectionsLib.MainModule.Types;

        SystemType = moduleDefinition.ImportReference(baseLibTypes.First(t => t.Name == "Type"));
        DebuggerBrowsableStateType = moduleDefinition.ImportReference(debugLibTypes.First(t => t.Name == "DebuggerBrowsableState"));

        var listType = collectionsLibTypes.First(t => t.Name == "List`1");
        ListCtor = moduleDefinition.ImportReference(listType.Methods.First(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name.StartsWith("IEnumerable")));
        ListToArray = moduleDefinition.ImportReference(listType.Methods.First(m => m.Name == "ToArray"));

        var debuggerBrowsableAttribute = debugLibTypes.First(t => t.Name == "DebuggerBrowsableAttribute");
        DebuggerBrowsableAttributeCtor = moduleDefinition.ImportReference(debuggerBrowsableAttribute.Methods.First(x => x.IsConstructor));

        var debuggerDisplayAttribute = debugLibTypes.First(t => t.Name == "DebuggerDisplayAttribute");
        DebuggerDisplayAttributeCtor = moduleDefinition.ImportReference(debuggerDisplayAttribute.Methods.First(x => x.IsConstructor));

        var debuggerTypeProxyAttribute = debugLibTypes.First(t => t.Name == "DebuggerTypeProxyAttribute");
        DebuggerTypeProxyAttributeCtor = moduleDefinition.ImportReference(debuggerTypeProxyAttribute.Methods.First(x => x.IsConstructor &&
            x.Parameters[0].ParameterType.FullName == "System.Type"));

        var compilerGeneratedAttribute = baseLibTypes.First(t => t.Name == "CompilerGeneratedAttribute");
        CompilerGeneratedAttributeCtor = moduleDefinition.ImportReference(compilerGeneratedAttribute.Methods.First(x => x.IsConstructor));

        StringFormat = moduleDefinition.ImportReference(moduleDefinition.TypeSystem.String.Resolve().Methods
            .First(m => m.Name == "Format" && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "System.Object[]"));
    }
}