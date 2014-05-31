using System;

namespace com.rackham.ApkHandler.Zip
{
    public class ZipFormatException : ApplicationException
    {
        internal ZipFormatException()
        {
            return;
        }

        internal ZipFormatException(string message)
            : base(message)
        {
            return;
        }
    }
}
