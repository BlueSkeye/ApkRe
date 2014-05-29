using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.ByteCode
{
    internal class ConditionalBranchInstruction : DalvikInstruction
    {
        #region CONSTRUCTORS
        internal ConditionalBranchInstruction(uint methodOffset, uint size)
            : base(methodOffset, size)
        {
            return;
        }
        #endregion

        #region PROPERTIES
        /// <summary>Returns an array (that may be empty or a null reference) with each
        /// item being an offset relative to the owning method of another instruction
        /// targeted by this one.</summary>
        internal override uint[] AdditionalTargetMethodOffsets
        {
            get
            {
                return new uint[]
                {
                    // Warning the literal is actually a word count.
                    (uint)(this.MethodRelativeOffset + (sizeof(ushort) * (long)LiteralOrAddress))
                };
            }
        }

        internal override bool ContinueInSequence
        {
            get { return true; }
        }
        #endregion
    }
}
