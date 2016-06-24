using System.Collections.Generic;

namespace com.rackham.ApkJava.API
{
    public interface ITryBlock
    {
        #region PROPERTIES
        /// <summary>Get block size (in bytes)</summary>
        uint BlockSize { get; }

        uint MethodStartOffset { get; }
        #endregion

        #region METHODS
        IEnumerable<IGuardHandler> EnumerateHandlers();
        #endregion
    }
}
