using System.Collections.Generic;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class ClassDefinition : BaseClassDefinition, IClass
    {
        #region CONSTRUCTORS
        public ClassDefinition(string className)
            : base(className)
        {
            return;
        }
        #endregion

        #region PROPERTIES
        public List<Annotation> Annotations { get; set; }

        internal List<Method> DirectMethods { get; private set; }

        internal List<Field> InstanceFields { get; private set; }

        public override bool IsExternal
        {
            get { return false; }
        }

        public string Filename { get; set; }

        internal List<Field> StaticFields { get; private set; }

        internal List<Method> VirtualMethods { get; private set; }
        #endregion

        #region METHODS
        public void AddDirectMethod(Method method)
        {
            if (null == DirectMethods) { DirectMethods = new List<Method>(); }
            DirectMethods.Add(method);
            return;
        }

        public void AddInstanceField(Field field)
        {
            if (null == InstanceFields) { InstanceFields = new List<Field>(); }
            InstanceFields.Add(field);
            return;
        }

        public void AddStaticField(Field field)
        {
            if (null == StaticFields) { StaticFields = new List<Field>(); }
            StaticFields.Add(field);
            return;
        }

        public void AddVirtualMethod(Method method)
        {
            if (null == VirtualMethods) { VirtualMethods = new List<Method>(); }
            VirtualMethods.Add(method);
            return;
        }

        public override IEnumerable<IField> EnumerateFields()
        {
            if (null != StaticFields) {
                foreach (IField item in StaticFields) { yield return item; }
            }
            if (null != InstanceFields) {
                foreach (IField item in InstanceFields) { yield return item; }
            }
            yield break;
        }

        public override IEnumerable<IMethod> EnumerateMethods()
        {
            if (null != VirtualMethods) {
                foreach (IMethod item in VirtualMethods) { yield return item; }
            }
            if (null != DirectMethods) {
                foreach (IMethod item in DirectMethods) { yield return item; }
            }
            yield break;
        }

        public override IMethod FindMethod(string fullName)
        {
            foreach (Method candidate in this.EnumerateMethods()) {
                if (JavaHelpers.BuildMethodDeclarationString(candidate) == fullName) {
                    return candidate;
                }
            }
            return null;
        }
        #endregion
    }
}
