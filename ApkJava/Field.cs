using System;
using System.Collections.Generic;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class Field : IField, IClassMember
    {
        #region CONSTRUCTORS
        public Field(IJavaType owningType, IJavaType type, string name)
        {
            OwningType = owningType;
            Name = name;
            FieldType = type;
            return;
        }
        #endregion

        #region PROPERTIES
        public AccessFlags AccessFlags { get; set; }

        public List<Annotation> Annotations { get; set; }

        public IJavaType FieldType { get; private set; }

        public string Name { get; private set; }

        public IJavaType OwningType { get; private set; }
        #endregion

        #region METHODS
        public void LinkTo(IJavaType owner)
        {
            if (null != FieldType) { throw new InvalidOperationException(); }
            OwningType = owner;
            ((BaseClassDefinition)owner).RegisterField(this);
            return;
        }
        #endregion
    }
}
