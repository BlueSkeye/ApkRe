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
        private void AssertInstructionsSet()
        {
            // TODO
        }
        
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
                _size += instruction.InstructionSize;
                return;
            }
            // Prevent double insertion.
            if (_instructions.Contains(instruction)) {
                throw new InvalidOperationException();
            }

            // The new instruction must be adjacent to the last already bound AND be
            // at a greater offset.
            DalvikInstruction lastInstruction = _instructions[_instructions.Count - 1];
            uint expectedOffset = lastInstruction.MethodRelativeOffset + lastInstruction.InstructionSize;
            if (expectedOffset != instruction.MethodRelativeOffset) {
                throw new InvalidOperationException();
            }
            _instructions.Add(instruction);
            _size += instruction.InstructionSize;
            return;
        }

        /// <summary>For use by the block splitting implementation only.</summary>
        /// <param name="startOffset"></param>
        /// <returns></returns>
        internal List<DalvikInstruction> CaptureInstructions(uint startOffset)
        {
            if (null == _instructions) {
                throw new InvalidOperationException();
            }
            int instructionsCount = this._instructions.Count;
            for(int index = 0; index < instructionsCount; index++) {
                if (_instructions[index].MethodRelativeOffset == startOffset) {
                    int captureCount = instructionsCount - index;
                    List<DalvikInstruction> result = new List<DalvikInstruction>();
                    result.AddRange(this._instructions.GetRange(index, captureCount));
                    return result;
                }
            }
            throw new AssertionException();
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
                    lastInstruction.MethodRelativeOffset + lastInstruction.InstructionSize - 1,
                    methodOffset);
            }
#endif
            // Find index of instruction with method relative offset matching the
            // given method offset.
            for (splitIndex = 0; splitIndex < _instructions.Count; splitIndex++) {
                if (_instructions[splitIndex].MethodRelativeOffset == methodOffset) { break; }
            }
            if (splitIndex >= _instructions.Count) { throw new ArgumentException(); }
            int movedRangeSize = _instructions.Count - splitIndex;
            DalvikInstruction[] movedRange = new DalvikInstruction[movedRangeSize];
            this._instructions.CopyTo(splitIndex, movedRange, 0, movedRange.Length);
            BlockNode result = new BlockNode(this.DebugEnabled);
            result._instructions = new List<DalvikInstruction>(movedRange);
            this._instructions.RemoveRange(splitIndex, movedRangeSize);
            // Also adjust block size for both blocks.
            uint sizeDelta = 0;
            foreach (DalvikInstruction movedIndstruction in result._instructions) {
                sizeDelta += movedIndstruction.InstructionSize;
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

//        /// <summary>This method is intended for use when splitting a block.
//        /// It should not be used otherwise.</summary>
//        /// <param name="to">Block node that will receive the transferred
//        /// instructions.</param>
//        /// <param name="transfered">A collection of instructions that MUST
//        /// already be linked to this instance.</param>
//        internal void TransferInstructions(BlockNode to,
//            IList<DalvikInstruction> transfered)
//        {
//            if (null == to) { throw new ArgumentNullException(); }
//            if (null == transfered) { throw new ArgumentNullException(); }
//            if (0 == transfered.Count) { throw new InvalidOperationException(); }
//            foreach (DalvikInstruction candidate in transfered) {
//                if (null == candidate) { throw new ArgumentException(); }
//            }
//            // Transfer is possible from the head or the tail of the block.
//            bool transferFromHead;

//            if (transfered[0].MethodRelativeOffset == this.FirstCoveredOffset) {
//                transferFromHead = true;
//                if (!to.IsPredecessor(this)) {
//                    throw new InvalidOperationException();
//                }
//            }
//            else {
//                DalvikInstruction lastTransferedInstruction = transfered[transfered.Count - 1];
//                if (lastTransferedInstruction.NextInstructionOffset == (this.FirstCoveredOffset + this._size - 1)) {
//                    transferFromHead = false;
//                    if (!to.IsSuccessor(this)) {
//                        throw new InvalidOperationException();
//                    }
//                }
//                else {
//                    throw new AssertionException(
//                        "Transfer attempt is neither from head nor from tail.");
//                }
//            }
//            // Instructions must be in order with no gap.
//            uint nextExpectedOffset = 0; // Initialize for compiler hapiness.
//            bool firstInstruction = true;
//            uint transferedSize = 0;
//            foreach (DalvikInstruction instruction in transfered) {
//                if (!this._instructions.Contains(instruction)) {
//                    throw new AssertionException(string.Format(
//                        "Illegal attempt to transfer an instruction from block #{0} to block #{1}.",
//                        this.NodeId, to.NodeId));
//                }
//                if (firstInstruction) { firstInstruction = false; }
//                else {
//                    if (nextExpectedOffset != instruction.MethodRelativeOffset) {
//                        throw new AssertionException(
//                            "Attempt to transfer a non contiguous block.");
//                    }
//                }
//                nextExpectedOffset = instruction.NextInstructionOffset;
//                transferedSize += instruction.InstructionSize;
//            }
//            if (transferedSize >= this._size) {
//                throw new InvalidOperationException();
//            }
//            // We are now ready to perform the transfer.
//            if (transferFromHead) {
//                this._instructions.RemoveRange(0, transfered.Count);
//                to._instructions.AddRange(transfered);
//            }
//            else {
//                int startRemovingAt = this._instructions.Count - transfered.Count;
//                this._instructions.RemoveRange(startRemovingAt, transfered.Count);
//                to._instructions.InsertRange(0, transfered);
//            }
//            // No need to adjust the FirstCoveredOffset property.
//            // This is a computed property.
//            this._size -= transferedSize;
//            to._size += transferedSize;
//#if DBGCFG
//            this.AssertInstructionsSet();
//            to.AssertInstructionsSet();
//#endif
//            return;
//        }
        #endregion

        #region FIELDS
        private List<DalvikInstruction> _instructions;
        private uint _size;
        #endregion
    }
}
