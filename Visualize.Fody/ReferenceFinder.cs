using System;
using System.Linq;
using Fody;
using Mono.Cecil;

public class ReferenceFinder
{
    public TypeReference SystemType;
    public TypeReference DebuggerBrowsableStateType;
    public MethodReference ListCtor;
    public Action<string> LogInfo;
    public MethodReference ListToArray;
    public MethodReference DebuggerBrowsableAttributeCtor;
    public MethodReference DebuggerDisplayAttributeCtor;
    public MethodReference DebuggerTypeProxyAttributeCtor;
    public MethodReference CompilerGeneratedAttributeCtor;
    public MethodReference StringFormat;

    public void FindReferences(BaseModuleWeaver moduleWeaver)
    {
        var moduleDefinition = moduleWeaver.ModuleDefinition;
        SystemType = moduleWeaver.FindTypeDefinition("System.Type");
        DebuggerBrowsableStateType = moduleWeaver.FindTypeDefinition("System.Diagnostics.DebuggerBrowsableState");

        var listType = moduleWeaver.FindTypeDefinition("System.Collections.Generic.List`1");
        ListCtor = moduleDefinition.ImportReference(listType.Methods.First(m => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Name.StartsWith("IEnumerable")));
        ListToArray = moduleDefinition.ImportReference(listType.Methods.First(m => m.Name == "ToArray"));

        var debuggerBrowsableAttribute = moduleWeaver.FindTypeDefinition("System.Diagnostics.DebuggerBrowsableAttribute");
        DebuggerBrowsableAttributeCtor = moduleDefinition.ImportReference(debuggerBrowsableAttribute.Methods.First(x => x.IsConstructor));

        var debuggerDisplayAttribute = moduleWeaver.FindTypeDefinition("System.Diagnostics.DebuggerDisplayAttribute");
        DebuggerDisplayAttributeCtor = moduleDefinition.ImportReference(debuggerDisplayAttribute.Methods.First(x => x.IsConstructor));

        var debuggerTypeProxyAttribute = moduleWeaver.FindTypeDefinition("System.Diagnostics.DebuggerTypeProxyAttribute");
        DebuggerTypeProxyAttributeCtor = moduleDefinition.ImportReference(debuggerTypeProxyAttribute.Methods.First(x => x.IsConstructor &&
            x.Parameters[0].ParameterType.FullName == "System.Type"));

        var compilerGeneratedAttribute = moduleWeaver.FindTypeDefinition("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
        CompilerGeneratedAttributeCtor = moduleDefinition.ImportReference(compilerGeneratedAttribute.Methods.First(x => x.IsConstructor));

        StringFormat = moduleDefinition.ImportReference(moduleWeaver.FindTypeDefinition("System.String").Methods
            .First(m => m.Name == "Format" && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "System.String" && m.Parameters[1].ParameterType.FullName == "System.Object[]"));
    }
}