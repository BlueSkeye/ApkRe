using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using com.rackham.ApkHandler.API;

namespace com.rackham.ApkHandler.Dex
{
    internal class BaseClassDefinition : BaseAnnotableObject, IClass, IAnnotatable
    {
        #region CONSTRUCTORS
        internal BaseClassDefinition(string fullName)
        {
            Name = Helpers.GetUndecoratedClassName(fullName);
        }
        #endregion

        #region PROPERTIES
        public virtual AccessFlags Access { get; internal set; }

        public string FullName
        {
            get
            {
                if (null == _fullName) {
                    StringBuilder builder = new StringBuilder();
                    builder.Append(Name);
                    for(IClass superClass = SuperClass; null != superClass; superClass = superClass.SuperClass) {
                        builder.Insert(0, "::");
                        builder.Insert(0, superClass.Name);
                    }
                    _fullName = builder.ToString();
                }
                return _fullName;
            }
        }

        public bool IsAbstract
        {
            get { return (0 != (Access & AccessFlags.Abstract)); }
        }

        public bool IsEnumeration
        {
            get { return (0 != (Access & AccessFlags.Enumeration)); }
        }

        public bool IsInterface
        {
            get { return (0 != (Access & AccessFlags.Interface)); }
        }

        public string Name { get; private set; }

        public IClass OuterClass
        {
            get { return _outerClass; }
        }

        public IClass SuperClass
        {
            get { return _superClass; }
        }
        #endregion


        #region METHODS
        public virtual IEnumerable<IField> EnumerateFields()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> EnumerateImplementedInterfaces()
        {
            if (null != _implementedInterfaces) {
                foreach (string result in _implementedInterfaces) { yield return result; }
            }
            yield break;
        }

        public virtual IEnumerable<IMethod> EnumerateMethods()
        {
            throw new NotImplementedException();
        }

        public virtual IMethod FindMethod(string fullName)
        {
            throw new NotImplementedException();
        }

        internal virtual void SetBaseClass(IClass value)
        {
            if (null != _superClass) { throw new InvalidOperationException(); }
            if (null == value) { throw new ArgumentNullException(); }
            _superClass = value;
            return;
        }

        internal virtual void SetImplementedInterfaces(List<string> value)
        {
            if (null != _implementedInterfaces) { throw new InvalidOperationException(); }
            if (null == value) { throw new ArgumentNullException(); }
            _implementedInterfaces = new List<string>(value);
            return;
        }
        #endregion

        #region FIELDS
        private string _fullName;
        private List<string> _implementedInterfaces;
        private IClass _outerClass;
        private IClass _superClass;
        #endregion
    }
}
