using System.Collections.Generic;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class Prototype : IPrototype
    {
        #region CONSTRUCTORS
        public Prototype(string returnType, string shortDescriptor,
            List<string> parametersType)
        {
            ReturnType = returnType;
            ShortDescriptor = shortDescriptor;
            ParametersType = parametersType;
            return;
        }
        #endregion

        #region PROPERTIES
        public List<string> ParametersType { get; private set; }

        public string ReturnType { get; private set; }

        internal string ShortDescriptor { get; private set; }
        #endregion
    }
}
