using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class DebuggerDisplayInjectorTests
    {
        [Test]
        public void EnumShouldNotGetDebuggerDisplay()
        {
            var asm = AssemblyDefinition.ReadAssembly(AssemblyWeaver.AfterAssemblyPath,
                                                      new ReaderParameters(ReadingMode.Deferred));
            var simpleEnumType = asm.MainModule.GetType("AssemblyToProcess.SimpleEnum");
            var fullName = typeof(DebuggerDisplayAttribute).FullName;
            Assert.IsFalse(simpleEnumType.CustomAttributes.Any(t => t.AttributeType.FullName == fullName), 
                           "Enums should not get decorated with '{0}'.", 
                           typeof(DebuggerDisplayAttribute).Name);
        }
    }
}
