using System;
using System.Linq;

namespace Visualize
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class DebuggerEnumerableTypeAttribute : Attribute
    {
    }
}