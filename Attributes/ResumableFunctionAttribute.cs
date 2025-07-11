namespace ResumableFunctions.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ResumableFunctionAttribute : Attribute
{
    public int ImplementationVersion { get; }

    public ResumableFunctionAttribute(int version)
    {
        ImplementationVersion = version;
    }
}
