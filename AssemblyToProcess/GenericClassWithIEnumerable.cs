using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyToProcess
{
    [Visualize.DebuggerEnumerableType]
    public class GenericClassWithIEnumerable<T> : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}