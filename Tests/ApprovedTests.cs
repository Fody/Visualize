#if(DEBUG)

using System;
using System.Linq;
using ApprovalTests;
using ApprovalTests.Reporters;
using NUnit.Framework;

[TestFixture]
[UseReporter(typeof(DiffReporter))]
public class ApprovedTests
{
    [Test]
    public void ClassWithProperties()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithProperties"));
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
    public void ClassWithExistingAttributes()
    {
        Approvals.Verify(Decompiler.Decompile(AssemblyWeaver.AfterAssemblyPath, "AssemblyToProcess.ClassWithExistingAttributes"));
    }

    //[Test]
    //public void Sample()
    //{
    //    var sampleClassType = AssemblyWeaver.Assembly.GetType("AssemblyToProcess.ClassWithIEnumerable");
    //    var sample = Activator.CreateInstance(sampleClassType);
    //}
}

#endif