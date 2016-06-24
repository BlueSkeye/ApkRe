
namespace com.rackham.ApkJava.API
{
    public interface IClassMember
    {
        string ClassName { get; }

        void LinkTo(IClass owner);
    }
}
