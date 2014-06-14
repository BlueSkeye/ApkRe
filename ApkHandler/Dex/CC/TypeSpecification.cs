using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkHandler.Dex.CC
{
    internal class TypeSpecification : ResourceChunkHeader
    {
        #region CONSTRUCTORS
        internal TypeSpecification(byte[] buffer, ref int offset)
            : base(buffer, ref offset)
        {
            Id = buffer[offset++];
            // Skip unused bytes.
            offset += 3;
            // Number of uint32_t entry configuration masks that follow.
            uint entryCount = Helpers.ReadUInt32(buffer, ref offset);
            _resourcesMask = new ResourceConfigurationFlags[entryCount];
            for (int index = 0; index < entryCount; index++) {
                _resourcesMask[index] = (ResourceConfigurationFlags)Helpers.ReadUInt32(buffer, ref offset);
            }
            _types = new List<Type>();
            return;
        }
        #endregion

        #region PROPERTIES
        // The type identifier this chunk is holding. Type IDs start at 1 (corresponding
        // to the value of the type bits in a resource identifier). 0 is invalid.
        internal byte Id { get; private set; }
        #endregion

        #region METHODS
        internal void AddType(Type candidate)
        {
            if (null == candidate) { throw new ArgumentNullException(); }
            _types.Add(candidate);
            return;
        }

        internal IEnumerable<Type> EnumerateTypes()
        {
            foreach (Type result in _types) { yield return result; }
            yield break;
        }

        internal Type GetEntry(uint index)
        {
            return _types[(int)index];
        }

        /// <summary>Check whether the given index as found in a reference is valid.</summary>
        /// <param name="index">Index from a reference.</param>
        /// <returns>true if the index is valid.</returns>
        internal bool IsReferenceIndexValid(uint index)
        {
            return (0 <= index) && (_types[0].Resources.Length > index);
        }
        #endregion

        #region FIELDS
        private ResourceConfigurationFlags[] _resourcesMask;
        private List<Type> _types;
        #endregion
    }
}
