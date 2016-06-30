using System;
using System.Text;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class ArrayType : JavaTypeDefinition, IJavaType
    {
        #region CONSTRUCTORS
        public ArrayType(JavaTypeDefinition indexedType)
            : base(JavaTypeDefinition.NamingspaceItem.Root, BuildCanonicalName(indexedType))
        {
            if (null == indexedType) { throw new ArgumentNullException(); }
            _indexedType = indexedType;
            JavaTypeDefinition.NamingspaceItem.Root.Register(this);
            return;
        }
        #endregion

        #region PROPERTIES
        public override JavaTypeDefinition IndexedType
        {
            get { return _indexedType; }
        }

        public override bool IsArray
        {
            get { return true; }
        }

        public override bool IsBuiltin
        {
            get { return false; }
        }
        #endregion

        #region METHODS
        private static string BuildCanonicalName(IJavaType indexedType)
        {
            if (null == indexedType) { throw new ArgumentNullException(); }
            StringBuilder builder = new StringBuilder(indexedType.FullyQualifiedBinaryName);
            return builder.Insert(('L' == builder[0]) ? 1 : 0, '[').ToString();
        }
        #endregion

        #region FIELDS
        private JavaTypeDefinition _indexedType;
        #endregion
    }
}
