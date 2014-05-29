using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.ByteCode
{
    internal class ArrayConstructionInstruction : DalvikInstruction
    {
        #region CONSTRUCTORS
        internal ArrayConstructionInstruction(uint methodOffset, uint size)
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

        #region METHODS
        internal override void SetAdditionalContent(object data)
        {
            byte[] initializationData = data as byte[];

            if (null == initializationData) { throw new ApplicationException(); }
            _initializationData = initializationData;
            return;
        }
        #endregion

        #region FIELDS
        private byte[] _initializationData;
        #endregion
    }
}
