public class ClassWithIndexor
{
    string[] arr = new string[100];
    public string this[int i]
    {
        // This indexer is very simple, and just returns or sets
        // the corresponding element from the internal array.
        get => arr[i];
        set => arr[i] = value;
    }
}