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

        if (type.CustomAttributes.All(c => c.AttributeType.Name != "DebuggerEnumerableTypeAttribute"))
            return;

        var collectionT = type.Interfaces.FirstOrDefault(i => i.Name == "ICollection`1");
        if (collectionT != null)
        {
            AddICollectionTProxy(moduleDefinition, type, (GenericInstanceType)collectionT);
            return;
        }

        var enumerableT = type.Interfaces.FirstOrDefault(i => i.Name == "IEnumerable`1");
        if (enumerableT != null)
        {
            AddIEnumerableTProxy(moduleDefinition, type, (GenericInstanceType)enumerableT);
        }
    }

    private static void AddICollectionTProxy(ModuleDefinition moduleDefinition, TypeDefinition type, GenericInstanceType collectionT)
    {
        var itemType = collectionT.GenericArguments[0];
        var itemArray = itemType.MakeArrayType();

        var proxyType = CreateProxy(moduleDefinition, type);
        TypeReference proxyTypeRef = proxyType;
        if (type.HasGenericParameters)
            proxyTypeRef = proxyType.MakeGenericInstanceType(type.GenericParameters.Select(gp => new GenericParameter(gp.Position, gp.Type, gp.Module)).ToArray());

        var field = proxyType.Fields[0];
        var fieldRef = new FieldReference(field.Name, field.FieldType, proxyTypeRef);

        var countProperty = type.Properties.First(p => p.Name == "Count" || p.Name == "System.Collections.ICollection.Count");
        MethodReference countMethod = countProperty.GetMethod;
        MethodReference copyToMethod = type.Methods.First(p => p.Name == "CopyTo" || p.Name == "System.Collections.ICollection.CopyTo");

        if (type.HasGenericParameters)
        {
            countMethod = countMethod.MakeHostInstanceGeneric(type.GenericParameters.Select(gp => new GenericParameter(gp.Position, gp.Type, gp.Module)).ToArray());
            copyToMethod = copyToMethod.MakeHostInstanceGeneric(type.GenericParameters.Select(gp => new GenericParameter(gp.Position, gp.Type, gp.Module)).ToArray());
        }

        var getMethod = new MethodDefinition("get_Items", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, itemArray);
        var getMethodBody = getMethod.Body;

        var localItems = new VariableDefinition("items", itemArray);
        getMethodBody.Variables.Add(localItems);

        getMethodBody.SimplifyMacros();
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldRef));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Callvirt, countMethod));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Newarr, itemType));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Stloc, localItems));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldRef));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldloc, localItems));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldloc, localItems));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldlen));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Conv_I4));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Callvirt, copyToMethod));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ldloc, localItems));
        getMethodBody.Instructions.Add(Instruction.Create(OpCodes.Ret));
        getMethodBody.InitLocals = true;
        getMethodBody.OptimizeMacros();

        proxyType.Methods.Add(getMethod);

        var property = new PropertyDefinition("Items", PropertyAttributes.None, itemArray)
                       {
                           GetMethod = getMethod
                       };
        var debuggerBrowsableAttribute = new CustomAttribute(ReferenceFinder.DebuggerBrowsableAttributeCtor);
        debuggerBrowsableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ReferenceFinder.DebuggerBrowsableStateType, DebuggerBrowsableState.RootHidden));
        property.CustomAttributes.Add(debuggerBrowsableAttribute);
        proxyType.Properties.Add(property);

        AddDebuggerTypeProxyAttribute(type, proxyType);
    }

    private static void AddIEnumerableTProxy(ModuleDefinition moduleDefinition, TypeDefinition type, GenericInstanceType enumerableT)
    {
        var itemType = enumerableT.GenericArguments[0];
        var itemArray = itemType.MakeArrayType();

        var proxyType = CreateProxy(moduleDefinition, type);
        TypeReference proxyTypeRef = proxyType;
        if (type.HasGenericParameters)
            proxyTypeRef = proxyType.MakeGenericInstanceType(type.GenericParameters.Select(gp => new GenericParameter(gp.Position, gp.Type, gp.Module)).ToArray());

        var field = proxyType.Fields[0];
        var fieldRef = new FieldReference(field.Name, field.FieldType, proxyTypeRef);

        var listCtor = ReferenceFinder.ListCtor.MakeHostInstanceGeneric(itemType);
        var listToArray = ReferenceFinder.ListToArray.MakeHostInstanceGeneric(itemType);

        var getMethod = new MethodDefinition("get_Items", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, itemArray);
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, fieldRef));
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Newobj, listCtor));
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Call, listToArray));
        getMethod.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        proxyType.Methods.Add(getMethod);

        var property = new PropertyDefinition("Items", PropertyAttributes.None, itemArray)
                       {
                           GetMethod = getMethod
                       };
        var debuggerBrowsableAttribute = new CustomAttribute(ReferenceFinder.DebuggerBrowsableAttributeCtor);
        debuggerBrowsableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ReferenceFinder.DebuggerBrowsableStateType, DebuggerBrowsableState.RootHidden));
        property.CustomAttributes.Add(debuggerBrowsableAttribute);
        proxyType.Properties.Add(property);

        AddDebuggerTypeProxyAttribute(type, proxyType);
    }

    private static TypeDefinition CreateProxy(ModuleDefinition moduleDefinition, TypeDefinition type)
    {
        var proxyType = new TypeDefinition(
            null,
            string.Format("<{0}>Proxy", type.Name.Split('`')[0]),
             TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
             moduleDefinition.TypeSystem.Object);
        TypeReference proxyTypeRef = proxyType;

        type.NestedTypes.Add(proxyType);

        proxyType.CustomAttributes.Add(new CustomAttribute(ReferenceFinder.CompilerGeneratedAttributeCtor));

        if (type.HasGenericParameters)
        {
            foreach (var genericParameter in type.GenericParameters)
            {
                proxyType.GenericParameters.Add(new GenericParameter(genericParameter.Name, proxyType));
            }

            proxyTypeRef = proxyType.MakeGenericInstanceType(type.GenericParameters.Select(gp => new GenericParameter(gp.Position, gp.Type, gp.Module)).ToArray());
        }

        var originalType = moduleDefinition.Import(type);
        if (type.HasGenericParameters)
            originalType = originalType.MakeGenericInstanceType(proxyType.GenericParameters.ToArray());

        var field = new FieldDefinition("original", FieldAttributes.Private | FieldAttributes.InitOnly, originalType);
        proxyType.Fields.Add(field);
        var fieldRef = new FieldReference(field.Name, field.FieldType, proxyTypeRef);

        var method = new MethodDefinition(
            ".ctor",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            moduleDefinition.TypeSystem.Void);
        method.Parameters.Add(new ParameterDefinition("original", ParameterAttributes.None, originalType));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, moduleDefinition.Import(moduleDefinition.TypeSystem.Object.Resolve().Constructors().First())));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, fieldRef));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        proxyType.Methods.Add(method);

        return proxyType;
    }

    private static void AddDebuggerTypeProxyAttribute(TypeDefinition type, TypeDefinition proxyType)
    {
        var debuggerTypeProxyAttribute = new CustomAttribute(ReferenceFinder.DebuggerTypeProxyAttributeCtor);
        debuggerTypeProxyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ReferenceFinder.SystemType, proxyType));
        type.CustomAttributes.Add(debuggerTypeProxyAttribute);
    }
}