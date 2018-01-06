using System.Collections.Generic;
using System.Linq;
using Fody;

public class ModuleWeaver : BaseModuleWeaver
{
    public override void Execute()
    {
        ReferenceFinder = new ReferenceFinder
        {
            LogInfo = LogInfo
        };
        ReferenceFinder.FindReferences(this);

        foreach (var type in ModuleDefinition.Types)
        {
            DebuggerDisplayInjector.AddDebuggerDisplayAttributes(ModuleDefinition, type, ReferenceFinder);
        }

        foreach (var type in ModuleDefinition.Types.ToList())
        {
            DebuggerTypeProxyInjector.AddDebuggerTypeProxyAttributes(ModuleDefinition, type, ReferenceFinder);
        }

        RemoveAttributes();
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
        yield return "System.Runtime";
        yield return "netstandard";
    }

    public ReferenceFinder ReferenceFinder;

    void RemoveAttributes()
    {
        ModuleDefinition.Assembly.RemoveFodyAttributes();

        foreach (var type in ModuleDefinition.Types)
        {
            type.RemoveFodyAttributes();
        }
    }

    public override bool ShouldCleanReference => true;
}