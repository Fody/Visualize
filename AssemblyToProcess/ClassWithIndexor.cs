namespace AssemblyToProcess
{
    public class ClassWithIndexor
    {
        private string[] arr = new string[100];
        public string this[int i]
        {
            get
            {
                // This indexer is very simple, and just returns or sets 
                // the corresponding element from the internal array. 
                return arr[i];
            }
            set
            {
                arr[i] = value;
            }
        }
    }
}