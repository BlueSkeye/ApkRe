using com.rackham.ApkJava.API;

namespace com.rackham.ApkHandler.Dex
{
    internal interface IClassMember
    {
        string ClassName { get; }

        void LinkTo(IClass owner);
    }
}
