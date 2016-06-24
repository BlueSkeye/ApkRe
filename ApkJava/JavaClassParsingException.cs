using System;

namespace com.rackham.ApkJava
{
    public class JavaClassParsingException : ApplicationException
    {
        internal JavaClassParsingException()
        {
            return;
        }

        internal JavaClassParsingException(string message)
            : base(message)
        {
            return;
        }
    }
}
