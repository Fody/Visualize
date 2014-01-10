using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AssemblyToProcess
{
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