using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public static class DebuggerDisplayInjector
{
    public static void AddDebuggerDisplayAttributes(ModuleDefinition moduleDefinition, TypeDefinition type, ReferenceFinder referenceFinder)
    {
        if (type.IsEnum ||
            type.CustomAttributes.Any(c => c.AttributeType.Name is "CompilerGeneratedAttribute" or "DebuggerDisplayAttribute"))
        {
            return;
        }

        var fields = type.Fields
            .Where(f => f.IsPublic &&
                        !f.HasConstant &&
                        !f.IsStatic &&
                        CanPrint(f.FieldType))
            .Cast<MemberReference>();
        var props = type.Properties
            .Where(p =>
                p.GetMethod != null &&
                p.GetMethod.IsPublic &&
                !p.GetMethod.IsStatic &&
                !p.GetMethod.HasParameters &&
                CanPrint(p.PropertyType))
            .Cast<MemberReference>();

        var displayBits = fields.Concat(props)
            .OrderBy(m => m, new DisplayAttributeOrderComparer())
            .ToList();

        if (!displayBits.Any())
        {
            return;
        }

        AddSimpleDebuggerDisplayAttribute(moduleDefinition, type, referenceFinder);

        if (type.Methods.Any(m => m.Name == "DebuggerDisplay" && m.Parameters.Count == 0))
        {
            return;
        }

        var debuggerDisplayMethod = new MethodDefinition("DebuggerDisplay", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName, moduleDefinition.TypeSystem.String);
        var body = debuggerDisplayMethod.Body;

        var arrayVar = new VariableDefinition(moduleDefinition.TypeSystem.Object.MakeArrayType());
        body.Variables.Add(arrayVar);

        body.SimplifyMacros();
        var instructions = body.Instructions;
        instructions.Add(Instruction.Create(OpCodes.Ldstr, string.Join(" | ", displayBits.Select((m, i) => $"{DisplayName(m)} = \"{{{i}}}\""))));
        instructions.Add(Instruction.Create(OpCodes.Ldc_I4, displayBits.Count));
        instructions.Add(Instruction.Create(OpCodes.Newarr, moduleDefinition.TypeSystem.Object));
        instructions.Add(Instruction.Create(OpCodes.Stloc, arrayVar));

        for (var i = 0; i < displayBits.Count; i++)
        {
            instructions.Add(Instruction.Create(OpCodes.Ldloc, arrayVar));
            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

            if (displayBits[i] is FieldDefinition field)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
                if (!field.FieldType.IsRefType())
                {
                    instructions.Add(Instruction.Create(OpCodes.Box, field.FieldType));
                }
            }

            if (displayBits[i] is PropertyDefinition property)
            {
                instructions.Add(Instruction.Create(OpCodes.Call, property.GetMethod));
                if (!property.PropertyType.IsRefType())
                {
                    instructions.Add(Instruction.Create(OpCodes.Box, property.PropertyType));
                }
            }

            instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
        }

        instructions.Add(Instruction.Create(OpCodes.Ldloc, arrayVar));
        instructions.Add(Instruction.Create(OpCodes.Call, referenceFinder.StringFormat));
        instructions.Add(Instruction.Create(OpCodes.Ret));
        body.InitLocals = true;
        body.OptimizeMacros();

        type.Methods.Add(debuggerDisplayMethod);
    }

    static void AddSimpleDebuggerDisplayAttribute(ModuleDefinition moduleDefinition, TypeDefinition type, ReferenceFinder referenceFinder)
    {
        var debuggerDisplay = new CustomAttribute(referenceFinder.DebuggerDisplayAttributeCtor);
        debuggerDisplay.ConstructorArguments.Add(new(moduleDefinition.TypeSystem.String, "{DebuggerDisplay(),nq}"));
        type.CustomAttributes.Add(debuggerDisplay);
    }

    static string DisplayName(MemberReference member)
    {
        if (member is not ICustomAttributeProvider customAttributeProvider)
        {
            return member.Name;
        }

        var display = customAttributeProvider.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.DisplayAttribute");
        if (display == null)
        {
            return member.Name;
        }

        if (display.Properties.Any(p => p.Name == "Name"))
        {
            return display.Properties.First(p => p.Name == "Name").Argument.Value.ToString();
        }

        return member.Name;
    }

    static HashSet<string> basicNames =
    [
        nameof(Int16),
        nameof(UInt16),
        nameof(Int32),
        nameof(UInt32),
        nameof(Int64),
        nameof(UInt64),
        nameof(Single),
        nameof(Double),
        nameof(Boolean),
        nameof(Byte),
        nameof(SByte),
        nameof(Char),
        nameof(String)
    ];

    static bool CanPrint(TypeReference typeReference)
    {
        if (basicNames.Contains(typeReference.Name))
        {
            return true;
        }

        if (typeReference.IsArray)
        {
            return false;
        }

        var typeDefinition = typeReference.Resolve();

        if (typeDefinition.IsEnum)
        {
            return true;
        }

        if (typeReference.IsGenericInstance && typeReference.Name == "Nullable`1")
        {
            var genericType = (GenericInstanceType) typeReference;
            return basicNames.Contains(genericType.GenericArguments[0].Name);
        }

        return false;
    }
}