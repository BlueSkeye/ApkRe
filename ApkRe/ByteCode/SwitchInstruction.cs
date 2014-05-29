using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.ByteCode
{
    internal class SwitchInstruction : DalvikInstruction
    {
        #region CONSTRUCTORS
        internal SwitchInstruction(uint methodOffset, uint size)
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
                uint[] result = new uint[_targetPerKey.Count];

                _targetPerKey.Values.CopyTo(result, 0);
                return result;
            }
        }

        /// <summary>The switch instruction falls through if there is no match.</summary>
        internal override bool ContinueInSequence
        {
            get { return true; }
        }
        #endregion

        #region METHODS
        /// <summary></summary>
        /// <param name="data">This should be a dictionary of target method related offsets
        /// keyed by the matching switch value. Offsets are expressed as a byte count not a
        /// word count.</param>
        internal override void SetAdditionalContent(object data)
        {
            Dictionary<int, uint> targets = data as Dictionary<int, uint>;

            if (null == targets) { throw new ArgumentException(); }
            _targetPerKey = new Dictionary<int, uint>();
            foreach(KeyValuePair<int, uint> pair in targets) {
                _targetPerKey[pair.Key] = pair.Value;
            }
            return;
        }
        #endregion

        #region FIELDS
        private Dictionary<int, uint> _targetPerKey;
        #endregion
    }
}
