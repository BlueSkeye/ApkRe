using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.ByteCode
{
    internal class ThrowInstruction : DalvikInstruction
    {
        #region CONSTRUCTORS
        internal ThrowInstruction(uint methodOffset, uint size)
            : base(methodOffset, size)
        {
            return;
        }
        #endregion

        #region PROPERTIES
        internal override uint[] AdditionalTargetMethodOffsets
        {
            get { return null; }
        }

        internal override bool ContinueInSequence
        {
            get { return false; }
        }
        #endregion
    }
}
