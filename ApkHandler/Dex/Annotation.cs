using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.Dex
{
    internal class Annotation
    {
        #region CONSTRUCTORS
        internal Annotation(string className, List<AnnotationElement> elements)
        {
            ClassName = className;
            Elements = elements;
            return;
        }
        #endregion

        #region PROPERTIES
        internal string ClassName { get; private set; }

        internal List<AnnotationElement> Elements { get; private set; }
        #endregion
    }
}
