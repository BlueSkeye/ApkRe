using System;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class ArrayType : JavaTypeDefinition, IJavaType
    {
        public ArrayType(JavaTypeDefinition indexedType)
            : base(JavaTypeDefinition.NamingspaceItem.Root, BuildCanonicalName(indexedType))
        {
            if (null == indexedType) { throw new ArgumentNullException(); }
            _indexedType = indexedType;
            return;
        }

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

        private static string BuildCanonicalName(IJavaType indexedType)
        {
            if (null == indexedType) { throw new ArgumentNullException(); }
            throw new NotImplementedException();
        }

        private JavaTypeDefinition _indexedType;
    }
}
