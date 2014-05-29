using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace com.rackham.ApkHandler.Dex
{
    internal class BinaryReaderWithConsistency : BinaryReader
    {
        #region CONSTRUCTORS
        internal BinaryReaderWithConsistency(Stream stream)
            : base(stream)
        {
            return;
        }
        #endregion

        #region METHODS
        internal void Align(int alignement)
        {
            int overshoot = ((int)base.BaseStream.Position % alignement);

            if (0 == overshoot) { return; }
            byte[] trash = new byte[alignement - overshoot];
            base.Read(trash, 0, trash.Length);
            return;
        }

        public override int Read(byte[] buffer, int index, int count)
        {
            int result = base.Read(buffer, index, count);

            lock (this) {
                if (_cheksumEnabled) { /* TODO */ }
                if ((null != _hasher) && (-1 != result)) {
                    _hasher.TransformBlock(buffer, index, result, buffer, index);
                }
            }
            return result;
        }

        internal void EnableChecksuming()
        {
            lock (this) {
                if (_cheksumEnabled) { throw new InvalidOperationException(); }
                _cheksumEnabled = true;
            }
            // TODO.
        }

        internal void EnableHashing()
        {
            lock (this) {
                if (null != _hasher) { throw new InvalidOperationException(); }
                _hasher = (SHA1)SHA1.Create();
            }
            return;
        }
        #endregion

        #region FIELDS
        private bool _cheksumEnabled = false;
        private SHA1 _hasher;
        #endregion
    }
}
