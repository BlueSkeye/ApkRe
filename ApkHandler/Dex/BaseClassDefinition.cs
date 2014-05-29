using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;

namespace com.rackham.ApkHandler.Dex
{
    internal class BaseClassDefinition : IClass
    {
        #region CONSTRUCTORS
        internal BaseClassDefinition(string fullName)
        {
            FullName = Helpers.GetUndecoratedClassName(fullName);
        }
        #endregion

        #region PROPERTIES
        public AccessFlags Access { get; internal set; }

        public string FullName { get; private set; }

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

        public IClass SuperClass
        {
            get { return _superClass; }
        }
        #endregion

        #region METHODS
        public IEnumerable<IField> EnumerateFields()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> EnumerateImplementedInterfaces()
        {
            if (null != _implementedInterfaces)
            {
                foreach (string result in _implementedInterfaces) { yield return result; }
            }
            yield break;
        }

        public IEnumerable<IMethod> EnumerateMethods()
        {
            throw new NotImplementedException();
        }

        internal void SetBaseClass(IClass value)
        {
            if (null != _superClass) { throw new InvalidOperationException(); }
            if (null == value) { throw new ArgumentNullException(); }
            _superClass = value;
            return;
        }

        internal void SetImplementedInterfaces(List<string> value)
        {
            if (null != _implementedInterfaces) { throw new InvalidOperationException(); }
            if (null == value) { throw new ArgumentNullException(); }
            _implementedInterfaces = new List<string>(value);
            return;
        }
        #endregion

        #region FIELDS
        private List<string> _implementedInterfaces;
        private IClass _superClass;
        #endregion
    }
}
