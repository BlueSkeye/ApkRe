using System.Collections.Generic;

namespace com.rackham.ApkJava
{
    public class Annotation
    {
        #region CONSTRUCTORS
        public Annotation(string className, List<AnnotationElement> elements)
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
