using System;
using System.Collections;
using System.Collections.Generic;

[Visualize.DebuggerEnumerableType]
public class GenericClassWithICollection<T> : ICollection<T>
{
    public IEnumerator<T> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item)
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public bool Contains(T item)
    {
        throw new NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    public int Count => 10;

    public bool IsReadOnly => true;

    public bool Remove(T item)
    {
        throw new NotImplementedException();
    }
}