using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

public static class DebuggerDisplayInjector
{
    public static void AddDebuggerDisplayAttributes(ModuleDefinition moduleDefinition, TypeDefinition type)
    {
        if (type.CustomAttributes.Any(c => c.AttributeType.Name == "CompilerGeneratedAttribute" || c.AttributeType.Name == "DebuggerDisplayAttribute"))
            return;

        var fields = type.Fields.Where(f => f.IsPublic && CanPrint(f.DeclaringType)).Cast<MemberReference>();
        var props = type.Properties.Where(p => p.GetMethod.IsPublic && CanPrint(p.DeclaringType)).Cast<MemberReference>();

        var displayBits = fields.Concat(props)
            .OrderBy(m => m.Name)
            .Select(m => string.Format("{0} = {{{0}}}", m.Name));

        if (displayBits.Any())
        {
            var debuggerDisplay = new CustomAttribute(ReferenceFinder.DebuggerDisplayAttributeCtor);
            debuggerDisplay.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, string.Join(" | ", displayBits)));
            type.CustomAttributes.Add(debuggerDisplay);
        }
    }

    private readonly static List<string> basicNames = new List<string>
    {
        typeof (short).Name,
        typeof (ushort).Name,
        typeof (int).Name,
        typeof (uint).Name,
        typeof (long).Name,
        typeof (ulong).Name,
        typeof (float).Name,
        typeof (double).Name,
        typeof (bool).Name,
        typeof (byte).Name,
        typeof (sbyte).Name,
        typeof (char).Name,
    };

    private static bool CanPrint(TypeDefinition typeDefinition)
    {
        if (basicNames.Contains(typeDefinition.Name))
            return true;

        if (typeDefinition.IsArray)
            return false;

        if (typeDefinition.IsEnum)
            return true;

        return false;
    }
}