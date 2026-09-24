using System.Diagnostics;
using Fody;
using TestResult = Fody.TestResult;
#pragma warning disable CS0618

public class WeaverTests
{
    static TestResult testResult;

    static WeaverTests()
    {
        var weaver = new ModuleWeaver();
        testResult = weaver.ExecuteTestRun("AssemblyToProcess.dll");
    }

    [Test]
    public async Task EnumShouldNotGetDebuggerDisplay()
    {
        var simpleEnumType = testResult.Assembly.GetType("SimpleEnum", true)!;
        var fullName = typeof(DebuggerDisplayAttribute).FullName;
        var hasAttribute = simpleEnumType.CustomAttributes.Any(_ => _.AttributeType.FullName == fullName);
        await Assert.That(hasAttribute)
            .IsFalse()
            .Because($"Enums should not get decorated with '{nameof(DebuggerDisplayAttribute)}'.");
    }

    [Test]
    public async Task InterfaceShouldNotGetDebuggerDisplay()
    {
        var simpleEnumType = testResult.Assembly.GetType("AnInterface", true)!;
        var fullName = typeof(DebuggerDisplayAttribute).FullName;
        var hasAttribute = simpleEnumType.CustomAttributes.Any(_ => _.AttributeType.FullName == fullName);
        await Assert.That(hasAttribute)
            .IsFalse()
            .Because($"Enums should not get decorated with '{nameof(DebuggerDisplayAttribute)}'.");
    }

    [Test]
    public async Task ClassWithExistingAttributes()
    {
        var type = testResult.Assembly.GetType("ClassWithExistingAttributes", true)!;

        await AssertEx.DebuggerDisplayMessage(type, "Nothing");
    }

    [Test]
    public Task ClassWithProperties()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithProperties"));
    }

    [Test]
    public Task ClassWithConst()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithConst"));
    }

    [Test]
    public Task ClassWithIndexor()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIndexor"));
    }

    [Test]
    public Task ClassWithDataAnnotations()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithDataAnnotations"));
    }

    [Test]
    public Task ClassWithIEnumerable()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIEnumerable"));
    }

    [Test]
    public Task ClassWithICollection()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithICollection"));
    }

    [Test]
    public Task GenericClassWithIEnumerable()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClassWithIEnumerable`1"));
    }

    [Test]
    public Task GenericClassWithICollection()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClassWithICollection`1"));
    }

    [Test]
    public Task ClassWithIEnumerableNotAttributed()
    {
        return Verify(Ildasm.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIEnumerableNotAttributed"));
    }
}
