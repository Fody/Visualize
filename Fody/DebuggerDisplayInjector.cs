using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

public static class DebuggerDisplayInjector
{
    private class DisplayAttributeOrderComparer : IComparer<MemberReference>
    {
        private readonly IComparer<string> stringComparer = Comparer<string>.Default;

        public int Compare(MemberReference x, MemberReference y)
        {
            var xorder = DisplayOrder(x);
            var yorder = DisplayOrder(y);

            if (xorder < yorder)
                return -1;
            else if (xorder > yorder)
                return 1;
            else
                return stringComparer.Compare(x.Name, y.Name);
        }
    }

    public static void AddDebuggerDisplayAttributes(ModuleDefinition moduleDefinition, TypeDefinition type)
    {
        if (type.IsEnum || type.CustomAttributes.Any(c => c.AttributeType.Name == "CompilerGeneratedAttribute" || c.AttributeType.Name == "DebuggerDisplayAttribute"))
            return;

        var fields = type.Fields.Where(f => f.IsPublic && CanPrint(f.FieldType)).Cast<MemberReference>();
        var props = type.Properties.Where(p => p.GetMethod != null && p.GetMethod.IsPublic && CanPrint(p.PropertyType)).Cast<MemberReference>();

        var displayBits = fields.Concat(props)
            .OrderBy(m => m, new DisplayAttributeOrderComparer())
            .Select(m => string.Format("{0} = {{{1}}}", DisplayName(m), m.Name));

        if (displayBits.Any())
        {
            var debuggerDisplay = new CustomAttribute(ReferenceFinder.DebuggerDisplayAttributeCtor);
            debuggerDisplay.ConstructorArguments.Add(new CustomAttributeArgument(moduleDefinition.TypeSystem.String, string.Join(" | ", displayBits)));
            type.CustomAttributes.Add(debuggerDisplay);
        }
    }

    private static string DisplayName(MemberReference member)
    {
        var customAttributeProvider = member as ICustomAttributeProvider;
        if (customAttributeProvider == null)
            return member.Name;

        var display = customAttributeProvider.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.DisplayAttribute");
        if (display == null)
            return member.Name;

        if (display.Properties.Any(p => p.Name == "Name"))
            return display.Properties.First(p => p.Name == "Name").Argument.Value.ToString();

        return member.Name;
    }

    private static int DisplayOrder(MemberReference member)
    {
        var customAttributeProvider = member as ICustomAttributeProvider;
        if (customAttributeProvider == null)
            return int.MaxValue;

        var display = customAttributeProvider.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.DisplayAttribute");
        if (display == null)
            return int.MaxValue;

        if (display.Properties.Any(p => p.Name == "Order"))
            return (int)display.Properties.First(p => p.Name == "Order").Argument.Value;

        return int.MaxValue;
    }

    private readonly static HashSet<string> basicNames = new HashSet<string>
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
        typeof (string).Name,
    };

    private static bool CanPrint(TypeReference typeReference)
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