using System;

namespace com.rackham.ApkJava.API
{
    public interface INamespace
    {
        bool IsRoot { get; }
        string Name { get; }
        INamespace Parent { get; }
    }
}
