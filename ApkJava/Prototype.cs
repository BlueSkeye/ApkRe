using System.Collections.Generic;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class Prototype : IPrototype
    {
        #region CONSTRUCTORS
        public Prototype(IJavaType returnType, string shortDescriptor,
            List<IJavaType> parametersType)
        {
            ReturnType = returnType;
            ShortDescriptor = shortDescriptor;
            ParametersType = parametersType;
            return;
        }
        #endregion

        #region PROPERTIES
        public List<IJavaType> ParametersType { get; private set; }
        public IJavaType ReturnType { get; private set; }
        internal string ShortDescriptor { get; private set; }
        #endregion
    }
}
