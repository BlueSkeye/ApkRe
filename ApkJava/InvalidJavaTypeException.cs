using System;

namespace com.rackham.ApkJava
{
    internal class InvalidJavaTypeException : ApplicationException
    {
        internal InvalidJavaTypeException()
        {
            return;
        }

        internal InvalidJavaTypeException(string message)
            : base(message)
        {
            return;
        }
    }
}
