using System;
using System.Collections.Generic;

namespace com.rackham.ApkHandler.API
{
    public interface IClass : IAnnotatable
    {
        #region PROPERTIES
        AccessFlags Access { get; }
        string FullName { get; }
        bool IsAbstract { get; }
        bool IsEnumeration { get; }
        bool IsInterface { get; }
        string Name { get; }
        /// <summary>This is the embedding class. This class has it'ts code embedded
        /// in the original class.</summary>
        IClass OuterClass { get; }
        /// <summary>This is the base class. Instances from this class specializes
        /// those from the <see cref="SuperClass"/></summary>
        IClass SuperClass { get; }
        #endregion

        #region METHODS
        IEnumerable<IField> EnumerateFields();
        IEnumerable<IMethod> EnumerateMethods();
        IEnumerable<string> EnumerateImplementedInterfaces();
        IMethod FindMethod(string demangledName);
        #endregion
    }
}
