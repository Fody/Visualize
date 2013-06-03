using System;
using System.Linq;
using Mono.Cecil;

public class ModuleWeaver
{
    public Action<string> LogInfo { get; set; }
    public Action<string> LogError { get; set; }
    public ModuleDefinition ModuleDefinition { get; set; }
    public IAssemblyResolver AssemblyResolver { get; set; }
    public string[] DefineConstants { get; set; }

    public ModuleWeaver()
    {
        LogInfo = s => { };
        LogError = s => { };
        DefineConstants = new string[0];
    }

    public void Execute()
    {
        ReferenceFinder.FindReferences(ModuleDefinition);

        foreach (var type in ModuleDefinition.Types)
            DebuggerDisplayInjector.AddDebuggerDisplayAttributes(ModuleDefinition, type);
        foreach (var type in ModuleDefinition.Types.ToList())
            DebuggerTypeProxyInjector.AddDebuggerTypeProxyAttributes(ModuleDefinition, type);

        //RemoveAttributes();
        //RemoveReference();
    }

    //private void RemoveAttributes()
    //{
    //    ModuleDefinition.Assembly.RemoveFodyAttributes();
    //}

    //private void RemoveReference()
    //{
    //    var referenceToRemove = ModuleDefinition.AssemblyReferences.FirstOrDefault(x => x.Name == "Visualize");
    //    if (referenceToRemove == null)
    //    {
    //        LogInfo("\tNo reference to 'Visualize.dll' found. References not modified.");
    //        return;
    //    }

    //    ModuleDefinition.AssemblyReferences.Remove(referenceToRemove);
    //    LogInfo("\tRemoving reference to 'Visualize.dll'.");
    //}
}