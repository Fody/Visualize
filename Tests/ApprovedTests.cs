using ApprovalTests;
using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using NUnit.Framework;

[UseReporter(typeof(DiffReporter))]
#if(DEBUG)
[UseApprovalSubdirectory("results-debug")]
#else
[UseApprovalSubdirectory("results-release")]
#endif
[TestFixture]
public class ApprovedTests
{
    [Test]
    public void ClassWithProperties()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithProperties"));
    }

    [Test]
    public void ClassWithConst()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithConst"));
    }

    [Test]
    public void ClassWithIndexor()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithIndexor"));
    }

    [Test]
    public void ClassWithDataAnnotations()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithDataAnnotations"));
    }

    [Test]
    public void ClassWithIEnumerable()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithIEnumerable"));
    }

    [Test]
    public void ClassWithICollection()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithICollection"));
    }

    [Test]
    public void GenericClassWithIEnumerable()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.GenericClassWithIEnumerable`1"));
    }

    [Test]
    public void GenericClassWithICollection()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.GenericClassWithICollection`1"));
    }

    [Test]
    public void ClassWithIEnumerableNotAttributed()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithIEnumerableNotAttributed"));
    }

    //[Test]
    //public void Sample()
    //{
    //    var sampleClassType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.ClassWithIEnumerable");
    //    var sample = Activator.CreateInstance(sampleClassType);
    //}
}