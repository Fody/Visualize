using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[Visualize.DebuggerEnumerableType]
public class ClassWithICollection : ICollection<int>
{
    public IEnumerator<int> GetEnumerator()
    {
        return Enumerable.Range(0, 10).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(int item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(int item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(int[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public int Count => 10;

    public bool IsReadOnly => true;

    public bool Remove(int item)
    {
        throw new NotImplementedException();
    }
}