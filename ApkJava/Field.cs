using System;
using System.Collections.Generic;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class Field : IField, IClassMember
    {
        #region CONSTRUCTORS
        public Field(string owningClass, string type, string name)
        {
            ClassName = owningClass;
            Name = name;
            TypeName = type;
            return;
        }
        #endregion

        #region PROPERTIES
        public AccessFlags AccessFlags { get; set; }

        public List<Annotation> Annotations { get; set; }

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
            ((BaseClassDefinition)owner).RegisterField(this);
            return;
        }
        #endregion
    }
}
