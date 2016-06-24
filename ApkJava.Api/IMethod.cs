using System.Collections.Generic;

namespace com.rackham.ApkJava.API
{
    public interface IMethod
    {
        #region PROPERTIES
        AccessFlags AccessFlags { get; }
        uint ByteCodeRawAddress { get; }
        uint ByteCodeSize { get; }
        IClass Class { get; }
        string Name { get; }
        IPrototype Prototype { get; }
        #endregion

        #region METHODS
        IEnumerable<ITryBlock> EnumerateTryBlocks();
        byte[] GetByteCode();
        #endregion
    }
}
