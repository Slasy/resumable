namespace ResumableFunctions.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ResumableFunctionAttribute : Attribute
{
    public int ImplementationVersion { get; }
    public string? MethodNameOverride { get; }

    public ResumableFunctionAttribute(int version = 0)
    {
        ImplementationVersion = version;
        MethodNameOverride = null;
    }

    public ResumableFunctionAttribute(string methodNameOverride, int version = 0)
    {
        ImplementationVersion = version;
        MethodNameOverride = methodNameOverride;
    }
}
