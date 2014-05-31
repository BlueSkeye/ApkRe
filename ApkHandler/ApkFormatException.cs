using System;

namespace com.rackham.ApkHandler
{
    public class ApkFormatException : ApplicationException
    {
        internal ApkFormatException()
        {
            return;
        }

        internal ApkFormatException(string message)
            : base(message)
        {
            return;
        }
    }
}
