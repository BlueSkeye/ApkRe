using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public abstract class BaseAnnotableObject : IAnnotatable
    {
        #region METHODS
        public void Annotate(IAnnotation annotation)
        {
            if (null == _annotations) {
                _annotations = new Dictionary<Oid, IAnnotation>();
            }
            else {
                if (_annotations.ContainsKey(annotation.Id)) {
                    throw new InvalidOperationException();
                }
            }
            _annotations.Add(annotation.Id, annotation);
            return;
        }

        public IAnnotation GetAnnotation(Oid id, bool throwIfNotFound = true)
        {
            if (null == id) { throw new ArgumentNullException(); }
            IAnnotation result;
            if (_annotations.TryGetValue(id, out result)) { return result; }
            if (throwIfNotFound) {
                throw new KeyNotFoundException();
            }
            return null;
        }

        public bool IsAnnotatedWith(Oid id)
        {
            if (null == id) { throw new ArgumentNullException(); }
            return (null != _annotations) && (_annotations.ContainsKey(id));
        }
        #endregion

        #region FIELDS
        private Dictionary<Oid, IAnnotation> _annotations;
        #endregion
    }
}
