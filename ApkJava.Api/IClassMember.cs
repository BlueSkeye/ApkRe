
namespace com.rackham.ApkJava.API
{
    public interface IClassMember
    {
        void LinkTo(IJavaType owner);

        IJavaType OwningType { get; }
    }
}
