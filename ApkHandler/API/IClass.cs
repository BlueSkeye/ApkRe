using System;
using System.Collections.Generic;

namespace com.rackham.ApkHandler.API
{
    public interface IClass
    {
        #region PROPERTIES
        string FullName { get; }

        bool IsAbstract { get; }

        bool IsEnumeration { get; }

        bool IsInterface { get; }
        #endregion

        #region METHODS
        AccessFlags Access { get; }

        IEnumerable<IField> EnumerateFields();

        IEnumerable<IMethod> EnumerateMethods();

        IEnumerable<string> EnumerateImplementedInterfaces();

        IClass SuperClass { get; }
        #endregion
    }
}
