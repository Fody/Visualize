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

        var types = ModuleDefinition.GetTypes()
            .Where(x => !x.IsInterface)
            .ToList();
        foreach (var type in types)
        {
            DebuggerDisplayInjector.AddDebuggerDisplayAttributes(ModuleDefinition, type, ReferenceFinder);
        }

        foreach (var type in types)
        {
            DebuggerTypeProxyInjector.AddDebuggerTypeProxyAttributes(ModuleDefinition, type, ReferenceFinder);
        }

        ModuleDefinition.Assembly.RemoveFodyAttributes();

        foreach (var type in types)
        {
            type.RemoveFodyAttributes();
        }
    }

    public override IEnumerable<string> GetAssembliesForScanning()
    {
        yield return "mscorlib";
        yield return "System";
        yield return "System.Runtime";
        yield return "netstandard";
    }

    public ReferenceFinder ReferenceFinder;

    public override bool ShouldCleanReference => true;
}