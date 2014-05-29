using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;

namespace com.rackham.ApkHandler.Dex
{
    internal class Field : IField, IClassMember
    {
        #region CONSTRUCTORS
        internal Field(string owningClass, string type, string name)
        {
            ClassName = owningClass;
            Name = name;
            TypeName = type;
            return;
        }
        #endregion

        #region PROPERTIES
        public AccessFlags AccessFlags { get; internal set; }

        internal List<Annotation> Annotations { get; set; }

        public IClass Class { get; private set; }

        public string ClassName { get; private set; }

        public string Name { get; private set; }

        internal string TypeName { get; private set; }
        #endregion

        #region METHODS
        public void LinkTo(IClass owner)
        {
            if (null != Class) { throw new InvalidOperationException(); }
            Class = owner;
            return;
        }
        #endregion
    }
}
