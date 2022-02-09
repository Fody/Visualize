using System.Collections;

public class ClassWithIEnumerableNotAttributed : IEnumerable<int>
{
    public IEnumerator<int> GetEnumerator()
    {
        return Enumerable.Range(0, 10).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
