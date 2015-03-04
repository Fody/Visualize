using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class DebuggerDisplayInjectorTests
{
    [Test]
    public void EnumShouldNotGetDebuggerDisplay()
    {
        var simpleEnumType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.SimpleEnum", true);
        var fullName = typeof(DebuggerDisplayAttribute).FullName;
        Assert.IsFalse(simpleEnumType.CustomAttributes.Any(t => t.AttributeType.FullName == fullName),
                       "Enums should not get decorated with '{0}'.",
                       typeof(DebuggerDisplayAttribute).Name);
    }

    [Test]
    public void ClassWithExistingAttributes()
    {
        var type = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.ClassWithExistingAttributes", true);

        AssertEx.DebuggerDisplayMessage(type, "Nothing");
    }
}