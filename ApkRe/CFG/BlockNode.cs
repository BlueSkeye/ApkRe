using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkRe.AST;
using com.rackham.ApkRe.ByteCode;

namespace com.rackham.ApkRe.CFG
{
    internal class BlockNode : CfgNode
    {
        #region CONSTRUCTORS
#if DBGCFG
        internal BlockNode(bool debugCfg = false)
            : base(debugCfg)
#else
        internal BlockNode()
#endif
        {
            return;
        }
        #endregion

        #region PROPERTIES
        internal override string AdditionalInfo
        {
            get
            {
                uint size;
                uint offset = GetBaseOffsetAndSize(out size);
                return string.Format("0x{0:X6}-0x{1:X6}", offset, offset + size - 1);
            }
        }

        internal uint FirstCoveredOffset
        {
            get { return _instructions[0].MethodRelativeOffset; }
        }
        #endregion

        #region METHODS
        /// <summary>Bind the given instruction to this block. The instruction must either
        /// be the first one to be bound to the block or be at offset immediately after the
        /// last already bound instruction from the block.</summary>
        /// <param name="instruction">The instruction to be bound.</param>
        internal void Bind(DalvikInstruction instruction)
        {
            if ((null != Successors) && (0 < Successors.Length)) {
                // We want successorc always to be from the last instruction of the
                // block so once a successor is set on a block binding additional
                // instructions is not possible anymore.
                throw new InvalidOperationException();
            }
#if DBGCFG
            if (DebugEnabled && (null != instruction)) {
                Console.WriteLine("[{0}] += <{1:X4}>", NodeId, instruction.MethodRelativeOffset);
            }
#endif
            if (null == instruction) { throw new ArgumentNullException(); }
            if (null == _instructions) {
                // First instruction to be bound to this block. Easy to handle.
                _instructions = new List<DalvikInstruction>();
                _instructions.Add(instruction);
                _size += instruction.BlockSize;
                return;
            }
            // Prevent double insertion.
            if (_instructions.Contains(instruction)) {
                throw new InvalidOperationException();
            }

            // The new instruction must be adjacent to the last already bound AND be
            // at a greater offset.
            DalvikInstruction lastInstruction = _instructions[_instructions.Count - 1];
            uint expectedOffset = lastInstruction.MethodRelativeOffset + lastInstruction.BlockSize;
            if (expectedOffset != instruction.MethodRelativeOffset) {
                throw new InvalidOperationException();
            }
            _instructions.Add(instruction);
            _size += instruction.BlockSize;
            return;
        }

        /// <summary>Retrieve the method relative offset of the first instruction in this
        /// block as well as the total number of bytes consumed by the block.</summary>
        /// <param name="size">On return this parameter is updated with the total number
        /// of bytes in the block.</param>
        /// <returns>The method relative offset of the first instruction in the block.</returns>
        internal uint GetBaseOffsetAndSize(out uint size)
        {
            size = _size;
            return ((null == _instructions) || (0 == _instructions.Count))
                ? 0
                : _instructions[0].MethodRelativeOffset;
        }

        /// <summary>Check whether the given offset is covered by the current block.</summary>
        /// <param name="offset">Offset to be checked for.</param>
        /// <returns></returns>
        internal bool IsCovering(uint offset, uint size)
        {
            uint blockSize;
            uint baseOffset = GetBaseOffsetAndSize(out blockSize);
            uint coverEnd = offset + size - 1;

            if (offset < baseOffset) { return false; }
            if (coverEnd > (baseOffset + blockSize - 1)) { return false; }
            return true;
        }

        /// <summary>Split the current node in two separate nodes reltively to the given
        /// method relative offset. Existing block is left with its orginal predecessors,
        /// newly create block inherit the successors of the original block and both blocks
        /// (old and new) are linked together.</summary>
        /// <param name="methodOffset">The offset within the owning method of the instruction
        /// that must be migrated to the new node.</param>
        /// <returns>The newly created block. This block is already linked as a successor of
        /// the block that has been split.</returns>
        internal BlockNode Split(uint methodOffset)
        {
            int splitIndex;
#if DBGCFG
            if (DebugEnabled) {
                DalvikInstruction lastInstruction = _instructions[_instructions.Count - 1];
                Console.WriteLine("[{0}] ({1:X4}|{2:X4})/{3:X4}", NodeId,
                    _instructions[0].MethodRelativeOffset,
                    lastInstruction.MethodRelativeOffset + lastInstruction.BlockSize - 1,
                    methodOffset);
            }
#endif
            for (splitIndex = 0; splitIndex < _instructions.Count; splitIndex++) {
                if (_instructions[splitIndex].MethodRelativeOffset == methodOffset) { break; }
            }
            if (splitIndex >= _instructions.Count) { throw new ApplicationException(); }
            int rangeSize = _instructions.Count - splitIndex;
            DalvikInstruction[] movedRange = new DalvikInstruction[rangeSize];
            this._instructions.CopyTo(splitIndex, movedRange, 0, movedRange.Length);
            BlockNode result = new BlockNode(this.DebugEnabled);
            result._instructions = new List<DalvikInstruction>();
            result._instructions.AddRange(movedRange);
            this._instructions.RemoveRange(splitIndex, rangeSize);
            // Also adjust block size for both blocks.
            uint sizeDelta = 0;
            foreach (DalvikInstruction movedIndstruction in result._instructions) {
                sizeDelta += movedIndstruction.BlockSize;
            }
            this._size -= sizeDelta;
            result._size = sizeDelta;
            // We must also transfer existing successors from the splited block to the
            // newly created one.
            base.TransferSuccessors(result);
            // This link MUST occur after successors transfer.
            CfgNode.Link(this, result);
            return result;
        }

        public override string ToString()
        {
            return this.AdditionalInfo;
        }
        #endregion

        #region FIELDS
        private List<DalvikInstruction> _instructions;
        private uint _size;
        #endregion
    }
}
