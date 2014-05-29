using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;

namespace com.rackham.ApkHandler.Dex
{
    internal class Prototype : IPrototype
    {
        #region CONSTRUCTORS
        internal Prototype(string returnType, string shortDescriptor,
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
