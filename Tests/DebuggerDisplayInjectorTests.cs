using System;
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
    public void ClassWithProperties()
    {
        var type = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.ClassWithProperties", true);

        AssertEx.DebuggerDisplayMessage(type, "Boolean = {Boolean} | Nullable = {Nullable} | Number = {Number} | SimpleEnum = {SimpleEnum} | String = {String}");
    }

    [Test]
    public void ClassWithExistingAttributes()
    {
        var type = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.ClassWithExistingAttributes", true);

        AssertEx.DebuggerDisplayMessage(type, "Nothing");
    }

    [Test]
    public void ClassWithDataAnnotations()
    {
        var type = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.ClassWithDataAnnotations", true);

        AssertEx.DebuggerDisplayMessage(type, "Last Name = {LastName} | First Name = {FirstName}");
    }
}