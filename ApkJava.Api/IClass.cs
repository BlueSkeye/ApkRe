using System;
using System.Collections.Generic;

namespace com.rackham.ApkJava.API
{
    public interface IClass : IJavaType
    {
        #region PROPERTIES
        AccessFlags Access { get; }
        bool IsAbstract { get; }
        bool IsEnumeration { get; }
        bool IsExternal { get; }
        bool IsInterface { get; }
        bool IsSuperClassResolved { get; }
        INamespace Namespace { get; }
        /// <summary>This is the embedding class. This class has it'ts code embedded
        /// in the original class.</summary>
        IClass OuterClass { get; }
        /// <summary>This is the base class. Instances from this class specializes
        /// those from the <see cref="SuperClass"/></summary>
        IJavaType SuperClass { get; }
        string SuperClassName { get; }
        #endregion

        #region METHODS
        IEnumerable<IField> EnumerateFields();
        IEnumerable<IAnnotatableMethod> EnumerateMethods();
        IEnumerable<string> EnumerateImplementedInterfaces();
        IMethod FindMethod(string demangledName);
        #endregion
    }
}
