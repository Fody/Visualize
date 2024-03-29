using Mono.Cecil;

class DisplayAttributeOrderComparer : IComparer<MemberReference>
{
    IComparer<string> stringComparer = Comparer<string>.Default;

    public int Compare(MemberReference x, MemberReference y)
    {
        var xOrder = DisplayOrder(x);
        var yOrder = DisplayOrder(y);

        if (xOrder < yOrder)
        {
            return -1;
        }

        if (xOrder > yOrder)
        {
            return 1;
        }

        return stringComparer.Compare(x.Name, y.Name);
    }

    static int DisplayOrder(MemberReference member)
    {
        var customAttributeProvider = member as ICustomAttributeProvider;

        var display = customAttributeProvider?.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.DisplayAttribute");
        if (display == null)
        {
            return 0;
        }

        if (display.Properties.Any(p => p.Name == "Order"))
        {
            return (int)display.Properties.First(p => p.Name == "Order").Argument.Value;
        }

        return 0;
    }
}