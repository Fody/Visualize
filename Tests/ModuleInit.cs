
#if NET48

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
sealed class ModuleInitializerAttribute : Attribute
{
}
#endif
public static class ModuleInit
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifierSettings.UniqueForAssemblyConfiguration();
        VerifierSettings.UniqueForRuntime();
    }
}