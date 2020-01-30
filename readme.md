# <img src="/package_icon.png" height="30px"> Visualize.Fody

[![Chat on Gitter](https://img.shields.io/gitter/room/fody/fody.svg)](https://gitter.im/Fody/Fody)
[![NuGet Status](https://img.shields.io/nuget/v/Visualize.Fody.svg)](https://www.nuget.org/packages/Visualize.Fody/)

Adds debugger attributes to help visualize objects.


### This is an add-in for [Fody](https://github.com/Fody/Home/)

**It is expected that all developers using Fody either [become a Patron on OpenCollective](https://opencollective.com/fody/), or have a [Tidelift Subscription](https://tidelift.com/subscription/pkg/nuget-fody?utm_source=nuget-fody&utm_medium=referral&utm_campaign=enterprise). [See Licensing/Patron FAQ](https://github.com/Fody/Home/blob/master/pages/licensing-patron-faq.md) for more information.**


## Usage

See also [Fody usage](https://github.com/Fody/Home/blob/master/pages/usage.md).


### NuGet installation

Install the [Visualize.Fody NuGet package](https://nuget.org/packages/Visualize.Fody/) and update the [Fody NuGet package](https://nuget.org/packages/Fody/):

```powershell
PM> Install-Package Fody
PM> Install-Package Visualize.Fody
```

The `Install-Package Fody` is required since NuGet always defaults to the oldest, and most buggy, version of any dependency.


### Add to FodyWeavers.xml

Add `<Visualize/>` to [FodyWeavers.xml](https://github.com/Fody/Home/blob/master/pages/usage.md#add-fodyweaversxml)

```xml
<Weavers>
  <Visualize/>
</Weavers>
```


### Your Code

```c#
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
```

### What gets compiled

```c#
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
```


## Links

  * [DebuggerDisplayAttribute](http://msdn.microsoft.com/en-us/library/system.diagnostics.debuggerdisplayattribute.aspx) on MSDN
  * [DebuggerTypeProxyAttribute](http://msdn.microsoft.com/en-us/library/system.diagnostics.debuggertypeproxyattribute.aspx) on MSDN
  * [Enhancing Debugging with the Debugger Display Attributes](http://msdn.microsoft.com/en-us/library/ms228992.aspx)


## Icon

[Eye](https://thenounproject.com/noun/eye/#icon-No7467) designed by [Nicolas Ramallo](https://thenounproject.com/nicografico) from [The Noun Project](https://thenounproject.com).