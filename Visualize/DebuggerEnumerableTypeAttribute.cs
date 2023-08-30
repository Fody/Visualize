namespace Visualize;

/// <summary>
/// Marks a type as an enumerable type and hence no DebuggerTypeProxy is added.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DebuggerEnumerableTypeAttribute : Attribute
{
}