using com.rackham.ApkJava.API;

namespace com.rackham.ApkHandler.API
{
    public interface IResolver
    {
        IField ResolveField(ushort index);

        IMethod ResolveMethod(ushort index);

        string ResolveString(ushort index);

        IType ResolveType(ushort index);
    }
}
