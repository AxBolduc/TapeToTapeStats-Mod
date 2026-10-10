// UnityEngine.CoreModule exposes a public System.Runtime.CompilerServices.NullableAttribute
// that lacks the constructors the compiler needs for nullable metadata (CS0656). Defining
// the attributes in this assembly makes the compiler use these instead.

namespace System.Runtime.CompilerServices;

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.GenericParameter |
    AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue,
    Inherited = false)]
internal sealed class NullableAttribute : Attribute
{
    public readonly byte[] NullableFlags;

    public NullableAttribute(byte flag)
    {
        NullableFlags = new[] { flag };
    }

    public NullableAttribute(byte[] flags)
    {
        NullableFlags = flags;
    }
}

[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Interface | AttributeTargets.Method |
    AttributeTargets.Struct,
    Inherited = false)]
internal sealed class NullableContextAttribute : Attribute
{
    public readonly byte Flag;

    public NullableContextAttribute(byte flag)
    {
        Flag = flag;
    }
}
