
namespace com.rackham.ApkJava.API
{
    public interface IField
    {
        AccessFlags AccessFlags { get; }
        IJavaType FieldType { get; }
        string Name { get; }
        IJavaType OwningType { get; }
    }
}
