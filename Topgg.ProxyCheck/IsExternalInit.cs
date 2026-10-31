#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    // Lets record structs compile for netstandard2.0, which predates init accessors.
    internal static class IsExternalInit
    {
    }
}
#endif
