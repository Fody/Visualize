using System.Collections;

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