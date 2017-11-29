using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

public static class DebuggerDisplayInjector
{
    class DisplayAttributeOrderComparer : IComparer<MemberReference>
    {
        readonly IComparer<string> stringComparer = Comparer<string>.Default;

        public int Compare(MemberReference x, MemberReference y)
        {
            var xorder = DisplayOrder(x);
            var yorder = DisplayOrder(y);

            if (xorder < yorder)
                return -1;
            if (xorder > yorder)
                return 1;
            return stringComparer.Compare(x.Name, y.Name);
        }
    }

    public static void AddDebuggerDisplayAttributes(ModuleDefinition moduleDefinition, TypeDefinition type)
    {
        if (type.IsEnum || type.CustomAttributes.Any(c => c.AttributeType.Name == "CompilerGeneratedAttribute" || c.AttributeType.Name == "DebuggerDisplayAttribute"))
            return;

        var fields = type.Fields
            .Where(f => f.IsPublic && !f.HasConstant && CanPrint(f.FieldType))
            .Cast<MemberReference>();
        var props = type.Properties
            .Where(p =>
                p.GetMethod != null &&
                p.GetMethod.IsPublic &&
                !p.GetMethod.HasParameters &&
                CanPrint(p.PropertyType))
            .Cast<MemberReference>();

        var displayBits = fields.Concat(props)
            .OrderBy(m => m, new DisplayAttributeOrderComparer())
            .ToList();

        if (!displayBits.Any())
            return;

        AddSimpleDebuggerDisplayAttribute(moduleDefinition, type);

        if (type.Methods.Any(m => m.Name == "DebuggerDisplay" && m.Parameters.Count == 0))
            return;

        var debuggerDisplayMethod = new MethodDefinition("DebuggerDisplay", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName, moduleDefinition.TypeSystem.String);
        var body = debuggerDisplayMethod.Body;

        var arrayVar = new VariableDefinition(moduleDefinition.TypeSystem.Object.MakeArrayType());
        body.Variables.Add(arrayVar);

        body.SimplifyMacros();
        body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, string.Join(" | ", displayBits.Select((m, i) => $"{DisplayName(m)} = \"{{{i}}}\""))));
        body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, displayBits.Count));
        body.Instructions.Add(Instruction.Create(OpCodes.Newarr, moduleDefinition.TypeSystem.Object));
        body.Instructions.Add(Instruction.Create(OpCodes.Stloc, arrayVar));

        for (var i = 0; i < displayBits.Count; i++)
        {
            body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, arrayVar));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4, i));
            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

            if (displayBits[i] is FieldDefinition field)
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Ldfld, field));
                if (!field.FieldType.IsRefType())
                    body.Instructions.Add(Instruction.Create(OpCodes.Box, field.FieldType));
            }

            if (displayBits[i] is PropertyDefinition property)
            {
                body.Instructions.Add(Instruction.Create(OpCodes.Call, property.GetMethod));
                if (!property.PropertyType.IsRefType())
                    body.Instructions.Add(Instruction.Create(OpCodes.Box, property.PropertyType));
            }

            body.Instructions.Add(Instruction.Create(OpCodes.Stelem_Ref));
        }

        body.Instructions.Add(Instruction.Create(OpCodes.Ldloc, arrayVar));
        body.Instructions.Add(Instruction.Create(OpCodes.Call, ReferenceFinder.StringFormat));
        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        body.InitLocals = true;
        body.OptimizeMacros();

        type.Methods.Add(debuggerDisplayMethod);
    }

    static void AddSimpleDebuggerDisplayAttribute(ModuleDefinition moduleDefinition, TypeDefinition type)
    {
        var debuggerDisplay = new CustomAttribute(ReferenceFinder.DebuggerDisplayAttributeCtor);
        debuggerDisplay.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, "{DebuggerDisplay(),nq}"));
        type.CustomAttributes.Add(debuggerDisplay);
    }

    static string DisplayName(MemberReference member)
    {
        if (!(member is ICustomAttributeProvider customAttributeProvider))
            return member.Name;

        var display = customAttributeProvider.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.DisplayAttribute");
        if (display == null)
            return member.Name;

        if (display.Properties.Any(p => p.Name == "Name"))
            return display.Properties.First(p => p.Name == "Name").Argument.Value.ToString();

        return member.Name;
    }

    static int DisplayOrder(MemberReference member)
    {
        var customAttributeProvider = member as ICustomAttributeProvider;

        var display = customAttributeProvider?.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.DisplayAttribute");
        if (display == null)
            return 0;

        if (display.Properties.Any(p => p.Name == "Order"))
            return (int)display.Properties.First(p => p.Name == "Order").Argument.Value;

        return 0;
    }

    readonly static HashSet<string> basicNames = new HashSet<string>
    {
        typeof(short).Name,
        typeof(ushort).Name,
        typeof(int).Name,
        typeof(uint).Name,
        typeof(long).Name,
        typeof(ulong).Name,
        typeof(float).Name,
        typeof(double).Name,
        typeof(bool).Name,
        typeof(byte).Name,
        typeof(sbyte).Name,
        typeof(char).Name,
        typeof(string).Name,
    };

    static bool CanPrint(TypeReference typeReference)
    {
        if (basicNames.Contains(typeReference.Name))
            return true;

        if (typeReference.IsArray)
            return false;

        var typeDefinition = typeReference.Resolve();

        if (typeDefinition.IsEnum)
            return true;

        if (typeReference.IsGenericInstance && typeReference.Name == "Nullable`1")
        {
            var genericType = (GenericInstanceType)typeReference;
            return basicNames.Contains(genericType.GenericArguments[0].Name);
        }

        return false;
    }
}