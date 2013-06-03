using System;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public static class DebuggerTypeProxyInjector
{
    public static void AddDebuggerTypeProxyAttributes(ModuleDefinition moduleDefinition, TypeDefinition type)
    {
        if (type.CustomAttributes.Any(c => c.AttributeType.Name == "CompilerGeneratedAttribute" || c.AttributeType.Name == "DebuggerTypeProxyAttribute"))
            return;

        var ienumerablet = type.Interfaces.FirstOrDefault(i => i.Name == "IEnumerable`1");
        if (ienumerablet != null)
            AddIEnumerableTProxy(moduleDefinition, type, (GenericInstanceType)ienumerablet);
    }

    private static void AddIEnumerableTProxy(ModuleDefinition moduleDefinition, TypeDefinition type, GenericInstanceType ienumerablet)
    {
        var proxyType = new TypeDefinition(
            null,
            string.Format("<{0}>Proxy", type.Name),
             TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
             moduleDefinition.TypeSystem.Object);

        proxyType.CustomAttributes.Add(new CustomAttribute(ReferenceFinder.CompilerGeneratedAttributeCtor));

        var field = new FieldDefinition("original", FieldAttributes.Private | FieldAttributes.InitOnly, type);
        proxyType.Fields.Add(field);

        var method = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            moduleDefinition.TypeSystem.Void);
        method.Parameters.Add(new ParameterDefinition("original", ParameterAttributes.None, type));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, moduleDefinition.Import(moduleDefinition.TypeSystem.Object.Resolve().Constructors().First())));
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
}