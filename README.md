[![Chat on Gitter](https://img.shields.io/gitter/room/fody/fody.svg?style=flat)](https://gitter.im/Fody/Fody)
[![NuGet Status](http://img.shields.io/nuget/v/Visualize.Fody.svg?style=flat)](https://www.nuget.org/packages/Visualize.Fody/)


## This is an add-in for [Fody](https://github.com/Fody/Fody/) 

![Visualize Icon - An eye](https://raw.github.com/Fody/Visualize/master/Icons/package_icon.png)

Adds debugger attributes to help visualize objects.


## The nuget package

https://nuget.org/packages/Visualize.Fody/

    PM> Install-Package Visualize.Fody

### Your Code

    public class Example1
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    public class Example2 : IEnumerable<int>
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

### What gets compiled

    [DebuggerDisplay("Name = {Name} | Number = {Number}")]
    public class Example1
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }

    [DebuggerTypeProxy(typeof(<Example2>Proxy))]
    public class Example2 : IEnumerable<int>
    {
        private sealed class <Example2>Proxy
        {
            private readonly Example2 original;

            public <Example2>Proxy(Example2 original)
            {
                this.original = original;
            }

            public int[] Items
            {
                get { return new List<int>(original).ToArray(); }
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            return Enumerable.Range(0, 10).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


## Links

  * [DebuggerDisplayAttribute](http://msdn.microsoft.com/en-us/library/system.diagnostics.debuggerdisplayattribute.aspx) on MSDN
  * [DebuggerTypeProxyAttribute](http://msdn.microsoft.com/en-us/library/system.diagnostics.debuggertypeproxyattribute.aspx) on MSDN
  * [Enhancing Debugging with the Debugger Display Attributes](http://msdn.microsoft.com/en-us/library/ms228992.aspx)


## Contributors

  * [Cameron MacFarland](https://github.com/distantcam)


## Icon

[Eye](http://thenounproject.com/noun/eye/#icon-No7467) designed by [Nicolas Ramallo](http://thenounproject.com/nicografico) from The Noun Project
