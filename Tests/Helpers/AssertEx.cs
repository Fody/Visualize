using System.Diagnostics;

public static class AssertEx
{
    public static async Task DebuggerDisplayMessage(Type type, string message)
    {
        var fullName = typeof(DebuggerDisplayAttribute).FullName;

        var attribute = type.CustomAttributes.FirstOrDefault(_ => _.AttributeType.FullName == fullName);

        await Assert.That(attribute).IsNotNull();

        var value = (string) attribute!.ConstructorArguments.First().Value!;
        await Assert.That(value).IsEqualTo(message);
    }
}
