using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;

namespace com.rackham.ApkHandler.Dex
{
    internal class KnownType : IType
    {
        #region CONSTRUCTORS
        internal KnownType(string fullName)
        {
            FullName = fullName;
            return;
        }
        #endregion

        #region PROPERTIES
        internal IClass Definition { get; private set; }

        public string FullName { get; private set; }
        #endregion

        #region METHODS
        internal void SetDefinition(IClass value)
        {
            if (null != Definition) { throw new InvalidOperationException(); }
            Definition = value;
            return;
        }
        #endregion
    }
}
