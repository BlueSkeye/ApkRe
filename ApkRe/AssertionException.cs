using System;

namespace com.rackham.ApkRe
{
    internal class AssertionException : ApplicationException
    {
        internal AssertionException()
        {
            return;
        }

        internal AssertionException(string message)
            : base(message)
        {
            return;
        }
    }
}
