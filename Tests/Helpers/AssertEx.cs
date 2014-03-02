using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

public class AssertEx
{
    public static void DebuggerDisplayMessage(Type type, string message)
    {
        var fullName = typeof(DebuggerDisplayAttribute).FullName;

        var attribute = type.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == fullName);

        Assert.NotNull(attribute);

        Assert.AreEqual(message, attribute.ConstructorArguments.First().Value.ToString());
    }
}