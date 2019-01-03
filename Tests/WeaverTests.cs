using System.Diagnostics;
using System.Linq;
using Fody;
using Xunit;
#pragma warning disable 618

#if (NET472)
using ApprovalTests;
using ApprovalTests.Namers;
#if(DEBUG)
[UseApprovalSubdirectory("results-debug")]
#else
[UseApprovalSubdirectory("results-release")]
#endif
#endif

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
            $"Enums should not get decorated with '{typeof(DebuggerDisplayAttribute).Name}'.");
    }

    [Fact]
    public void InterfaceShouldNotGetDebuggerDisplay()
    {
        var simpleEnumType = testResult.Assembly.GetType("AnInterface", true);
        var fullName = typeof(DebuggerDisplayAttribute).FullName;
        Assert.False(simpleEnumType.CustomAttributes.Any(t => t.AttributeType.FullName == fullName),
            $"Enums should not get decorated with '{typeof(DebuggerDisplayAttribute).Name}'.");
    }

    [Fact]
    public void ClassWithExistingAttributes()
    {
        var type = testResult.Assembly.GetType("ClassWithExistingAttributes", true);

        AssertEx.DebuggerDisplayMessage(type, "Nothing");
    }

#if (NET472)

    [Fact]
    public void ClassWithProperties()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithProperties"));
    }

    [Fact]
    public void ClassWithConst()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithConst"));
    }

    [Fact]
    public void ClassWithIndexor()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIndexor"));
    }

    [Fact]
    public void ClassWithDataAnnotations()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithDataAnnotations"));
    }

    [Fact]
    public void ClassWithIEnumerable()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIEnumerable"));
    }

    [Fact]
    public void ClassWithICollection()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithICollection"));
    }

    [Fact]
    public void GenericClassWithIEnumerable()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClassWithIEnumerable`1"));
    }

    [Fact]
    public void GenericClassWithICollection()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.GenericClassWithICollection`1"));
    }

    [Fact]
    public void ClassWithIEnumerableNotAttributed()
    {
        Approvals.Verify(Decompiler.Decompile(testResult.AssemblyPath, "AssemblyToProcess.ClassWithIEnumerableNotAttributed"));
    }

#endif
}