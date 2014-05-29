using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.Dex
{
    internal class AnnotationElement
    {
        #region CONSTRUCTORS
        internal AnnotationElement(string memberName, object value)
        {
            MemberName = memberName;
            Value = value;
            return;
        }
        #endregion

        #region PROPERTIES
        internal string MemberName { get; private set; }

        internal object Value { get; private set; }
        #endregion
    }
}
