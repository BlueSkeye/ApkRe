using System.Security.Cryptography;

namespace com.rackham.ApkJava.API
{
    public interface IAnnotatable
    {
        void Annotate(IAnnotation annotation);
        IAnnotation GetAnnotation(Oid id, bool throwIfNotFound = true);
        bool IsAnnotatedWith(Oid id);
    }
}
