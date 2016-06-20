using System;
using System.Security.Cryptography;

using com.rackham.ApkHandler.API;

namespace com.rackham.ApkHandler
{
    /// <summary>A base implementation for the <see cref="IAnnotation"/>
    /// interface.</summary>
    public class AnnotationBase : IAnnotation
    {
        public AnnotationBase(Oid id, object value)
        {
            if(null == id) { throw new ArgumentNullException(); }
            this.Id = id;
            this.Value = value;
            return;
        }

        public Oid Id { get;  private set; }

        public bool IsSealed { get; private set; }

        public object Value
        {
            get { return _value; }
            set
            {
                if (IsSealed) { throw new InvalidOperationException(); }
                _value = value;
            }
        }

        public void Seal()
        {
            IsSealed = true;
            return;
        }

        private object _value;
    }
}
