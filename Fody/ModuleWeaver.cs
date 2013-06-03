using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public class ModuleWeaver
{
    public Action<string> LogInfo { get; set; }
    public Action<string> LogError { get; set; }
    public ModuleDefinition ModuleDefinition { get; set; }
    public IAssemblyResolver AssemblyResolver { get; set; }
    public string[] DefineConstants { get; set; }

    public ModuleWeaver()
    {
        LogInfo = s => { };
        LogError = s => { };
        DefineConstants = new string[0];
    }

    public void Execute()
    {
        ReferenceFinder.FindReferences(ModuleDefinition);

        AddDebuggerDisplayAttributes(ModuleDefinition.Types);
        AddDebuggerTypeProxyAttributes(ModuleDefinition.Types);

        RemoveAttributes();
        RemoveReference();
    }

    private void AddDebuggerDisplayAttributes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            if (type.CustomAttributes.Any(c => c.AttributeType.Name == "CompilerGeneratedAttribute" || c.AttributeType.Name == "DebuggerDisplayAttribute"))
                continue;

            var fields = type.Fields.Where(f => f.IsPublic).Cast<MemberReference>();
            var props = type.Properties.Where(p => p.GetMethod.IsPublic).Cast<MemberReference>();

            var displayBits = fields.Concat(props)
                .OrderBy(m => m.Name)
                .Select(m => string.Format("{0} = {{{0}}}", m.Name));

            if (displayBits.Any())
            {
                var debuggerDisplay = new CustomAttribute(ReferenceFinder.DebuggerDisplayAttributeCtor);
                debuggerDisplay.ConstructorArguments.Add(new CustomAttributeArgument(ModuleDefinition.TypeSystem.String, string.Join(" | ", displayBits)));
                type.CustomAttributes.Add(debuggerDisplay);
            }
        }
    }

    private void AddDebuggerTypeProxyAttributes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types.ToList())
        {
            if (type.CustomAttributes.Any(c => c.AttributeType.Name == "CompilerGeneratedAttribute" || c.AttributeType.Name == "DebuggerTypeProxyAttribute"))
                continue;

            var ienumerablet = type.Interfaces.FirstOrDefault(i => i.Name == "IEnumerable`1");
            if (ienumerablet != null)
                AddIEnumerableTProxy(type, (GenericInstanceType)ienumerablet);
        }
    }

    private void AddIEnumerableTProxy(TypeDefinition type, GenericInstanceType ienumerablet)
    {
        var proxyType = new TypeDefinition(
            null,
            string.Format("<{0}>Proxy", type.Name),
             TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
             ModuleDefinition.TypeSystem.Object);

        proxyType.CustomAttributes.Add(new CustomAttribute(ReferenceFinder.CompilerGeneratedAttributeCtor));

        var field = new FieldDefinition("original", FieldAttributes.Private | FieldAttributes.InitOnly, type);
        proxyType.Fields.Add(field);

        var method = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            ModuleDefinition.TypeSystem.Void);
        method.Parameters.Add(new ParameterDefinition("original", ParameterAttributes.None, type));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, ModuleDefinition.Import(ModuleDefinition.TypeSystem.Object.Resolve().Constructors().First())));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, field));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        proxyType.Methods.Add(method);

        var itemType = ienumerablet.GenericArguments[0];
        var itemArray = itemType.MakeArrayType();

        var listCtor = ReferenceFinder.ListCtor.MakeHostInstanceGeneric(itemType);
        var listToArray = ReferenceFinder.ListToArray.MakeHostInstanceGeneric(itemType);

        var getMethod = new MethodDefinition("get_Items", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, itemArray);
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, listCtor));
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, listToArray));
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        proxyType.Methods.Add(getMethod);

        var property = new PropertyDefinition("Items", PropertyAttributes.None, itemArray);
        property.GetMethod = getMethod;
        var debuggerBrowsableAttribute = new CustomAttribute(ReferenceFinder.DebuggerBrowsableAttributeCtor);
        debuggerBrowsableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ReferenceFinder.DebuggerBrowsableStateType, DebuggerBrowsableState.RootHidden));
        property.CustomAttributes.Add(debuggerBrowsableAttribute);
        proxyType.Properties.Add(property);

        type.NestedTypes.Add(proxyType);

        var debuggerTypeProxyAttribute = new CustomAttribute(ReferenceFinder.DebuggerTypeProxyAttributeCtor);
        debuggerTypeProxyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ReferenceFinder.SystemType, proxyType));
        type.CustomAttributes.Add(debuggerTypeProxyAttribute);
    }

    private void RemoveAttributes()
    {
        ModuleDefinition.Assembly.RemoveFodyAttributes();
    }

    private void RemoveReference()
    {
        var referenceToRemove = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "Visualize");
        if (referenceToRemove == null)
        {
            LogInfo("\tNo reference to 'Visualize.dll' found. References not modified.");
            return;
        }

        ModuleDefinition.AssemblyReferences.Remove(referenceToRemove);
        LogInfo("\tRemoving reference to 'Visualize.dll'.");
    }
}