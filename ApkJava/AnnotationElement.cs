
namespace com.rackham.ApkJava
{
    public class AnnotationElement
    {
        #region CONSTRUCTORS
        public AnnotationElement(string memberName, object value)
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
