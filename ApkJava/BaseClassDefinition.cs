﻿using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public abstract class BaseClassDefinition : JavaTypeDefinition, IClass
    {
        #region CONSTRUCTORS
        public BaseClassDefinition(string fullName)
            : base(fullName)
        {
            return;
        }
        #endregion

        #region PROPERTIES
        public virtual AccessFlags Access { get; set; }

        public override bool IsBuiltin
        {
            get { return false; }
        }

        //public string CanonicName
        //{
        //    get
        //    {
        //        if (null == _fullName) {
        //            StringBuilder builder = new StringBuilder();
        //            builder.Append(Name);
        //            for (IJavaType superClass = SuperClass; null != superClass; superClass = superClass.SuperType) {
        //                builder.Insert(0, "::");
        //                builder.Insert(0, superClass.Name);
        //            }
        //            _fullName = builder.ToString();
        //        }
        //        return _fullName;
        //    }
        //}

        public bool IsAbstract
        {
            get { return (0 != (Access & AccessFlags.Abstract)); }
        }

        public bool IsEnumeration
        {
            get { return (0 != (Access & AccessFlags.Enumeration)); }
        }

        public abstract bool IsExternal { get; }

        public bool IsInterface
        {
            get { return (0 != (Access & AccessFlags.Interface)); }
        }

        public bool IsSuperClassResolved
        {
            get { return (null != _superClass); }
        }

        INamespace IClass.Namespace
        {
            get { return base.Namespace; }
        }

        public IClass OuterClass
        {
            get { return _outerClass; }
        }

        public IJavaType SuperClass
        {
            get
            {
                if (null != _superClassName) { throw new InvalidOperationException(); }
                return _superClass;
            }
        }

        // TODO : Document the algorithm.
        public string SuperClassName
        {
            get
            {
                if (null != _superClassName) { return _superClassName; }
                if (null != _superClass) {
                    if (object.ReferenceEquals(_superClass, this)) { return null; }
                    return _superClass.Name;
                }
                throw new InvalidOperationException();
            }
        }
        #endregion

        #region METHODS
        public virtual IEnumerable<IField> EnumerateFields()
        {
            if (null == _fields) { yield break; }
            foreach(IField item in _fields) { yield return item; }
            yield break;
        }

        public IEnumerable<string> EnumerateImplementedInterfaces()
        {
            if (null != _implementedInterfaces) {
                foreach (string result in _implementedInterfaces) { yield return result; }
            }
            yield break;
        }

        public virtual IEnumerable<IAnnotatableMethod> EnumerateMethods()
        {
            if (null != _methods) {
                foreach (IAnnotatableMethod result in _methods) { yield return result; }
            }
            yield break;
        }

        public virtual IMethod FindMethod(string fullName)
        {
            throw new NotImplementedException();
        }

        public void RegisterField(IField field)
        {
            if (null == field) { throw new ArgumentNullException(); }
            if (!object.ReferenceEquals(this, field.FieldType)) {
                throw new ArgumentException();
            }
            lock (this) {
                if (null == _fields) { _fields = new List<IField>(); }
                if (_fields.Contains(field)) { throw new InvalidOperationException(); }
                _fields.Add(field);
            }
        }

        public virtual void SetBaseClass(string name)
        {
            if (null != _superClass) { throw new InvalidOperationException(); }
            if (null != _superClassName) { throw new InvalidOperationException(); }
            if (string.IsNullOrEmpty(name)) { throw new ArgumentNullException(); }
            if (ObjectClassName == name) { _superClass = this; }
            if ("ArrayAdapter" == name) { int i = 1; }
            _superClassName = name;
            return;
        }

        public virtual void SetBaseClass(IJavaType value)
        {
            if (null != _superClass) { throw new InvalidOperationException(); }
            if (null == value) { throw new ArgumentNullException(); }
            _superClass = value;
            // Consider checking that super class name matches the name
            // of the value.
            _superClassName = null;
            return;
        }

        public virtual void SetImplementedInterfaces(List<string> value)
        {
            if (null != _implementedInterfaces) { throw new InvalidOperationException(); }
            if (null == value) { throw new ArgumentNullException(); }
            _implementedInterfaces = new List<string>(value);
            return;
        }
        #endregion

        #region FIELDS
        internal const string ObjectClassName = "Ljava/lang/Object;";
        private List<IField> _fields;
        private string _fullName;
        private List<string> _implementedInterfaces;
        private List<IMethod> _methods;
        private IClass _outerClass;
        private IJavaType _superClass;
        private string _superClassName;
        #endregion
    }
}
