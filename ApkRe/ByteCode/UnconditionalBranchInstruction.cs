using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.ByteCode
{
    internal class UnconditionalBranchInstruction : DalvikInstruction
    {
        #region CONSTRUCTORS
        internal UnconditionalBranchInstruction(uint methodOffset, uint size)
            : base(methodOffset, size)
        {
            return;
        }
        #endregion

        #region PROPERTIES
        internal override uint[] AdditionalTargetMethodOffsets
        {
            get { return new uint[] { (uint)(this.MethodRelativeOffset + base.LiteralOrAddress) }; }
        }

        internal override bool ContinueInSequence
        {
            get { return false; }
        }
        #endregion
    }
}
