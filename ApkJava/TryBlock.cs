using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class TryBlock : ITryBlock
    {
        #region CONSTRUCTORS
        public TryBlock(uint methodStartOffset, ushort blockSize)
        {
            MethodStartOffset = methodStartOffset;
            BlockSize = blockSize;
            return;
        }
        #endregion

        #region PROPERTIES
        public GuardHandlers Handlers { get; set; }

        // WARNING : Should this property be modifiable the GetSortKey method MUST be revised.
        /// <summary>Block size in bytes.</summary>
        public uint BlockSize { get; private set; }

        // WARNING : Should this property be modifiable the GetSortKey method MUST be revised.
        public uint MethodStartOffset { get; private set; }
        #endregion

        #region METHODS
        public IEnumerable<IGuardHandler> EnumerateHandlers()
        {
            bool handlerFound = false;
            if (null != Handlers.CatchClauses) {
                foreach (KeyValuePair<string, uint> handler in Handlers.CatchClauses) {
                    handlerFound = true;
                    yield return new GuardHandler(handler);
                }
            }
            if (0 != Handlers.CatchAllHandlerAddress) {
                handlerFound = true;
                yield return new GuardHandler(Handlers.CatchAllHandlerAddress);
            }
            if (!handlerFound) { throw new ApplicationException(); }
            yield break;
        }

        internal SortKey GetSortKey()
        {
            if (null == _sortKey) { _sortKey = new SortKey(MethodStartOffset, BlockSize); }
            return _sortKey;
        }
        #endregion

        #region FIELDS
        private SortKey _sortKey;
        #endregion

        #region INNER CLASSES
        internal class GuardHandler : IGuardHandler
        {
            internal GuardHandler(KeyValuePair<string, uint> handler)
            {
                CaughtType = handler.Key;
                HandlerMethodOffset = handler.Value;
                return;
            }

            internal GuardHandler(uint methodOffset)
            {
                HandlerMethodOffset = methodOffset;
                return;
            }

            public string CaughtType { get; private set; }

            public uint HandlerMethodOffset { get; private set; }
        }

        public class SortKey : IComparable<SortKey>
        {
            #region CONSTRUCTORS
            internal SortKey(uint startAddress, uint bytesCount)
            {
                _startAddress = startAddress;
                _bytesCount = bytesCount;
                return;
            }
            #endregion

            #region METHODS
            public int CompareTo(SortKey other)
            {
                if (null == other) { return -1; }
                if (other._startAddress > this._startAddress) { return -1; }
                if (other._startAddress < this._startAddress) { return 1; }
                // WARNING : For two keys starting at the same address, the one with the
                // MOST instructions is considered to be LESSER THAN the one with LESS
                // instructions.
                if (other._bytesCount > this._bytesCount) { return 1; }
                if (other._bytesCount < this._bytesCount) { return -1; }
                return 0;
            }
            #endregion

            #region FIELDS
            private uint _bytesCount;
            private uint _startAddress;
            #endregion
        }
        #endregion
    }
}
