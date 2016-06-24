
namespace com.rackham.ApkJava.API
{
    public interface IField
    {
        AccessFlags AccessFlags { get; }

        IClass Class { get; }

        string Name { get; }
    }
}
