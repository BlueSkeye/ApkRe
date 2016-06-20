using System;
using System.Security.Cryptography;

namespace com.rackham.ApkHandler.API
{
    /// <summary>An annotation is a piece of information that can be
    /// attached to various kind of objects. These annotations are not
    /// to be confused with those that are embedded in the DEX file
    /// itself.</summary>
    public interface IAnnotation
    {
        Oid Id { get; }
        bool IsSealed { get; }
        object Value { get; set; }

        void Seal();
    }
}
