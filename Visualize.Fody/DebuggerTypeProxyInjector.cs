using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using PropertyAttributes = Mono.Cecil.PropertyAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

public static class DebuggerTypeProxyInjector
{
    static ConstructorInfo genericParameterConstructor;

    static DebuggerTypeProxyInjector()
    {

        var types = new[]
        {
            typeof(int), typeof(GenericParameterType ),typeof( ModuleDefinition )
        };
        genericParameterConstructor = typeof(GenericParameter).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, types, null);
    }

    public static void AddDebuggerTypeProxyAttributes(ModuleDefinition moduleDefinition, TypeDefinition type, ReferenceFinder referenceFinder)
    {
        if (type.CustomAttributes.Any(c => c.AttributeType.Name == "CompilerGeneratedAttribute" || c.AttributeType.Name == "DebuggerTypeProxyAttribute"))
            return;

        if (type.CustomAttributes.All(c => c.AttributeType.Name != "DebuggerEnumerableTypeAttribute"))
            return;

        var collectionT = type.Interfaces.FirstOrDefault(i => i.InterfaceType.Name == "ICollection`1");
        if (collectionT != null)
        {
            AddICollectionTProxy(moduleDefinition, type, (GenericInstanceType)collectionT.InterfaceType, referenceFinder);
            return;
        }

        var enumerableT = type.Interfaces.FirstOrDefault(i => i.InterfaceType.Name == "IEnumerable`1");
        if (enumerableT != null)
        {
            AddIEnumerableTProxy(moduleDefinition, type, (GenericInstanceType)enumerableT.InterfaceType, referenceFinder);
        }
    }

    static void AddICollectionTProxy(ModuleDefinition moduleDefinition, TypeDefinition type, GenericInstanceType collectionT, ReferenceFinder referenceFinder)
    {
        var itemType = collectionT.GenericArguments[0];
        var itemArray = itemType.MakeArrayType();

        var proxyType = CreateProxy(moduleDefinition, type, referenceFinder);
        TypeReference proxyTypeRef = proxyType;
        if (type.HasGenericParameters)
            proxyTypeRef = proxyType.MakeGenericInstanceType(type.GenericParameters.Select(CloneGenericParameter).ToArray());

        var field = proxyType.Fields[0];
        var fieldRef = new FieldReference(field.Name, field.FieldType, proxyTypeRef);

        var countProperty = type.Properties.First(p => p.Name == "Count" || p.Name == "System.Collections.ICollection.Count");
        MethodReference countMethod = countProperty.GetMethod;
        MethodReference copyToMethod = type.Methods.First(p => p.Name == "CopyTo" || p.Name == "System.Collections.ICollection.CopyTo");

        if (type.HasGenericParameters)
        {
            countMethod = countMethod.MakeHostInstanceGeneric(type.GenericParameters.Select(CloneGenericParameter).ToArray());
            copyToMethod = copyToMethod.MakeHostInstanceGeneric(type.GenericParameters.Select(CloneGenericParameter).ToArray());
        }

        var getMethod = new MethodDefinition("get_Items", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, itemArray);
        var getMethodBody = getMethod.Body;

        var localItems = new VariableDefinition(itemArray);
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
        var debuggerBrowsableAttribute = new CustomAttribute(referenceFinder.DebuggerBrowsableAttributeCtor);
        debuggerBrowsableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(referenceFinder.DebuggerBrowsableStateType, DebuggerBrowsableState.RootHidden));
        property.CustomAttributes.Add(debuggerBrowsableAttribute);
        proxyType.Properties.Add(property);

        AddDebuggerTypeProxyAttribute(type, proxyType, referenceFinder);
    }

    static void AddIEnumerableTProxy(ModuleDefinition moduleDefinition, TypeDefinition type, GenericInstanceType enumerableT, ReferenceFinder referenceFinder)
    {
        var itemType = enumerableT.GenericArguments[0];
        var itemArray = itemType.MakeArrayType();

        var proxyType = CreateProxy(moduleDefinition, type, referenceFinder);
        TypeReference proxyTypeRef = proxyType;
        if (type.HasGenericParameters)
            proxyTypeRef = proxyType.MakeGenericInstanceType(type.GenericParameters.Select(CloneGenericParameter).ToArray());

        var field = proxyType.Fields[0];
        var fieldRef = new FieldReference(field.Name, field.FieldType, proxyTypeRef);

        var listCtor = referenceFinder.ListCtor.MakeHostInstanceGeneric(itemType);
        var listToArray = referenceFinder.ListToArray.MakeHostInstanceGeneric(itemType);

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
        var debuggerBrowsableAttribute = new CustomAttribute(referenceFinder.DebuggerBrowsableAttributeCtor);
        debuggerBrowsableAttribute.ConstructorArguments.Add(new CustomAttributeArgument(referenceFinder.DebuggerBrowsableStateType, DebuggerBrowsableState.RootHidden));
        property.CustomAttributes.Add(debuggerBrowsableAttribute);
        proxyType.Properties.Add(property);

        AddDebuggerTypeProxyAttribute(type, proxyType, referenceFinder);
    }

    static GenericParameter CloneGenericParameter(GenericParameter gp)
    {
        return (GenericParameter)genericParameterConstructor.Invoke(new object[] { gp.Position, gp.Type, gp.Module });
    }

    static TypeDefinition CreateProxy(ModuleDefinition moduleDefinition, TypeDefinition type, ReferenceFinder referenceFinder)
    {
        var proxyType = new TypeDefinition(
            null,
            $"<{type.Name.Split('`')[0]}>Proxy",
             TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
             moduleDefinition.TypeSystem.Object);
        TypeReference proxyTypeRef = proxyType;

        type.NestedTypes.Add(proxyType);

        proxyType.CustomAttributes.Add(new CustomAttribute(referenceFinder.CompilerGeneratedAttributeCtor));

        if (type.HasGenericParameters)
        {
            foreach (var genericParameter in type.GenericParameters)
            {
                proxyType.GenericParameters.Add(new GenericParameter(genericParameter.Name, proxyType));
            }

            proxyTypeRef = proxyType.MakeGenericInstanceType(type.GenericParameters.Select(CloneGenericParameter).ToArray());
        }

        var originalType = moduleDefinition.ImportReference(type);
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
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Call, moduleDefinition.ImportReference(moduleDefinition.TypeSystem.Object.Resolve().Constructors().First())));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Stfld, fieldRef));
        method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        proxyType.Methods.Add(method);

        return proxyType;
    }

    static void AddDebuggerTypeProxyAttribute(TypeDefinition type, TypeDefinition proxyType, ReferenceFinder referenceFinder)
    {
        var debuggerTypeProxyAttribute = new CustomAttribute(referenceFinder.DebuggerTypeProxyAttributeCtor);
        debuggerTypeProxyAttribute.ConstructorArguments.Add(new CustomAttributeArgument(referenceFinder.SystemType, proxyType));
        type.CustomAttributes.Add(debuggerTypeProxyAttribute);
    }
}