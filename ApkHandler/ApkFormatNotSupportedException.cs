using System;

namespace com.rackham.ApkHandler
{
    public class ApkFormatNotSupportedException : ApplicationException
    {
        internal ApkFormatNotSupportedException()
        {
            return;
        }

        internal ApkFormatNotSupportedException(string message)
            : base(message)
        {
            return;
        }
    }
}