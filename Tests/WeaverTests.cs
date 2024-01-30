using System.Diagnostics;
using Fody;
#pragma warning disable CS0618

public class WeaverTests
{
    static TestResult testResult;

    static WeaverTests()
    {
        var weavingTask = new ModuleWeaver();
        testResult = weavingTask.ExecuteTestRun("AssemblyToProcess.dll");
    }

    [Fact]
    public void EnumShouldNotGetDebuggerDisplay()
    {
        var simpleEnumType = testResult.Assembly.GetType("SimpleEnum", true);
        var fullName = typeof(DebuggerDisplayAttribute).FullName;
        Assert.False(simpleEnumType.CustomAttributes.Any(t => t.AttributeType.FullName == fullName),
            $"Enums should not get decorated with '{nameof(DebuggerDisplayAttribute)}'.");
    }

    [Fact]
    public void InterfaceShouldNotGetDebuggerDisplay()
    {
        var simpleEnumType = testResult.Assembly.GetType("AnInterface", true);
        var fullName = typeof(DebuggerDisplayAttribute).FullName;
        Assert.False(simpleEnumType.CustomAttributes.Any(t => t.AttributeType.FullName == fullName),
            $"Enums should not get decorated with '{nameof(DebuggerDisplayAttribute)}'.");
    }

    [Fact]
    public void ClassWithExistingAttributes()
    {
        var type = testResult.Assembly.GetType("ClassWithExistingAttributes", true);

        AssertEx.DebuggerDisplayMessage(type, "Nothing");
    }

    [Fact]
    public Task ClassWithProperties()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithProperties"));
    }

    [Fact]
    public Task ClassWithConst()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithConst"));
    }

    [Fact]
    public Task ClassWithIndexor()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIndexor"));
    }

    [Fact]
    public Task ClassWithDataAnnotations()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithDataAnnotations"));
    }

    [Fact]
    public Task ClassWithIEnumerable()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIEnumerable"));
    }

    [Fact]
    public Task ClassWithICollection()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithICollection"));
    }

    [Fact]
    public Task GenericClassWithIEnumerable()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClassWithIEnumerable`1"));
    }

    [Fact]
    public Task GenericClassWithICollection()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClassWithICollection`1"));
    }

    [Fact]
    public Task ClassWithIEnumerableNotAttributed()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIEnumerableNotAttributed"));
    }
}