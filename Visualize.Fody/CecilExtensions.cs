using Mono.Cecil;
using Mono.Cecil.Rocks;

public static class CecilExtensions
{
    public static void RemoveFodyAttributes(this ICustomAttributeProvider definition)
    {
        var customAttributes = definition.CustomAttributes;

        var attributes = customAttributes.Where(_ => _.AttributeType.Namespace == "Visualize").ToList();

        foreach (var attribute in attributes)
        {
            customAttributes.Remove(attribute);
        }
    }

    public static IEnumerable<MethodDefinition> MethodsWithBody(this TypeDefinition type)
    {
        return type.Methods.Where(_ => _.Body != null);
    }

    public static IEnumerable<PropertyDefinition> ConcreteProperties(this TypeDefinition type)
    {
        return type.Properties.Where(_ => (_.GetMethod == null || !_.GetMethod.IsAbstract) && (_.SetMethod == null || !_.SetMethod.IsAbstract));
    }

    static MethodReference CloneMethodWithDeclaringType(MethodDefinition methodDef, TypeReference declaringTypeRef)
    {
        if (!declaringTypeRef.IsGenericInstance || methodDef == null)
        {
            return methodDef;
        }

        var methodRef = new MethodReference(methodDef.Name, methodDef.ReturnType, declaringTypeRef)
        {
            CallingConvention = methodDef.CallingConvention,
            HasThis = methodDef.HasThis,
            ExplicitThis = methodDef.ExplicitThis
        };

        foreach (var paramDef in methodDef.Parameters)
        {
            methodRef.Parameters.Add(new(paramDef.Name, paramDef.Attributes, paramDef.ParameterType));
        }

        foreach (var genParamDef in methodDef.GenericParameters)
        {
            methodRef.GenericParameters.Add(new(genParamDef.Name, methodRef));
        }

        return methodRef;
    }

    public static MethodReference ReferenceMethod(this TypeReference typeRef, Func<MethodDefinition, bool> methodSelector)
    {
        return CloneMethodWithDeclaringType(typeRef.Resolve().Methods.FirstOrDefault(methodSelector), typeRef);
    }

    public static MethodReference ReferenceMethod(this TypeReference typeRef, string methodName)
    {
        return ReferenceMethod(typeRef, m => m.Name == methodName);
    }

    public static IEnumerable<MethodDefinition> Constructors(this TypeDefinition type)
    {
        return type.Methods.Where(m => m.IsConstructor);
    }

    public static MethodReference MakeHostInstanceGeneric(this MethodReference self, params TypeReference[] args)
    {
        var reference = new MethodReference(self.Name, self.ReturnType, self.DeclaringType.MakeGenericInstanceType(args))
        {
            HasThis = self.HasThis,
            ExplicitThis = self.ExplicitThis,
            CallingConvention = self.CallingConvention
        };

        foreach (var parameter in self.Parameters)
        {
            reference.Parameters.Add(new(parameter.ParameterType));
        }

        foreach (var genericParam in self.GenericParameters)
        {
            reference.GenericParameters.Add(new(genericParam.Name, reference));
        }

        return reference;
    }

    public static bool IsRefType(this TypeReference arg)
    {
        if (arg.IsValueType)
        {
            return false;
        }

        if (arg is ByReferenceType byReferenceType && byReferenceType.ElementType.IsValueType)
        {
            return false;
        }

        if (arg is PointerType pointerType && pointerType.ElementType.IsValueType)
        {
            return false;
        }

        return true;
    }
}