using System;
using System.Collections.Generic;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public class Method :
        BaseAnnotableObject,
        IAnnotatable,
        IAnnotatableMethod,
        IMethod,
        IClassMember
    {
        #region CONSTRUCTORS
        public Method(IJavaType owningtype, string methodName, Prototype methodPrototype)
        {
            OwningType = owningtype;
            // Helpers.GetUndecoratedClassName(className);
            Name = methodName;
            Prototype = methodPrototype;
            DebugId = NextDebugId++;
            return;
        }
        #endregion

        #region PROPERTIES
        public AccessFlags AccessFlags { get; set; }

        public List<Annotation> Annotations { get; set; }

        public ushort ArgumentsWordsCount { get; set; }

        public byte[] ByteCode { get; set; }

        public uint ByteCodeSize
        {
            get { return (null == ByteCode) ? 0 : (uint)ByteCode.Length; }
        }

        public uint ByteCodeRawAddress { get; set; }

        public IJavaType OwningType { get; private set; }

        public DebugInfo DebugInfo { get; set; }

        public SortedList<TryBlock.SortKey, TryBlock> GuardedBlocks { get; private set; }

        public string Name { get; private set; }

        public IPrototype Prototype { get; private set; }

        public ushort RegistersCount { get; set; }

        public ushort ResultsWordsCount { get; set; }
        #endregion

        #region METHODS
        public void AddGuardedBlock(TryBlock block)
        {
            if (null == GuardedBlocks) { GuardedBlocks = new SortedList<TryBlock.SortKey, TryBlock>(); }
            GuardedBlocks.Add(block.GetSortKey(), block);
            return;
        }

        /// <summary>Enumerate try blocks in order.</summary>
        /// <returns></returns>
        public IEnumerable<ITryBlock> EnumerateTryBlocks()
        {
            if (null != GuardedBlocks) {
                foreach (TryBlock result in GuardedBlocks.Values) { yield return result; }
            }
            yield break;
        }

        public byte[] GetByteCode()
        {
            return (null == ByteCode) ? null : (byte[])ByteCode.Clone();
        }

        public void LinkTo(IJavaType owner)
        {
            if (null != OwningType) { throw new InvalidOperationException(); }
            OwningType = owner;
            return;
        }
        #endregion

        #region FIELDS
        internal readonly int DebugId;
        private static int NextDebugId = 1;
        #endregion
    }
}
