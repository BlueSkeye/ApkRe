using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.rackham.ApkHandler.API
{
    public interface IMethod
    {
        #region PROPERTIES
        AccessFlags AccessFlags { get; }

        uint ByteCodeRawAddress { get; }

        uint ByteCodeSize { get; }

        IClass Class { get; }

        string Name { get; }
        #endregion

        #region METHODS
        IEnumerable<ITryBlock> EnumerateTryBlocks();

        byte[] GetByteCode();

        IPrototype Prototype { get; }
        #endregion
    }
}
