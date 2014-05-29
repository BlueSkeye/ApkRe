﻿using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.ByteCode
{
    internal class ArrayOperationInstruction : DalvikInstruction
    {
        #region CONSTRUCTORS
        internal ArrayOperationInstruction(uint methodOffset, uint size)
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
            get { return true; }
        }
        #endregion
    }
}
