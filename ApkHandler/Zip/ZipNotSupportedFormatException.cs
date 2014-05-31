using System;

namespace com.rackham.ApkHandler.Zip
{
    public class ZipNotSupportedFormatException : ApplicationException
    {
        internal ZipNotSupportedFormatException()
        {
            return;
        }

        internal ZipNotSupportedFormatException(string message)
            : base(message)
        {
            return;
        }
    }
}