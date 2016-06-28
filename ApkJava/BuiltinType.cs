using System.Collections.Generic;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class BuiltinType : JavaTypeDefinition, IJavaType
    {
        static BuiltinType()
        {
            _builtinTypes = new BuiltinType[] {
                new BuiltinType("byte"),
                new BuiltinType("short"),
                new BuiltinType("int"),
                new BuiltinType("long"),
                new BuiltinType("float"),
                new BuiltinType("double"),
                new BuiltinType("char"),
                new BuiltinType("boolean"),
                new BuiltinType("void")
            };
        }

        private BuiltinType(string name)
            : base(JavaTypeDefinition.NamingspaceItem.Root, name)
        {
            base.Name = name;
        }

        public override bool IsBuiltin
        {
            get { return true; }
        }

        internal static IEnumerable<BuiltinType> EnumerateAll()
        {
            foreach(BuiltinType item in _builtinTypes) { yield return item; }
        }

        private static BuiltinType[] _builtinTypes;
    }
}
