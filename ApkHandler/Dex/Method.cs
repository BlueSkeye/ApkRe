using System;
using System.Collections.Generic;

using com.rackham.ApkJava;
using com.rackham.ApkJava.API;

namespace com.rackham.ApkHandler.Dex
{
    internal class Method : BaseAnnotableObject, IAnnotatable, IMethod, IClassMember
    {
        #region CONSTRUCTORS
        internal Method(string className, string methodName, Prototype methodPrototype)
        {
            ClassName = className;
            // Helpers.GetUndecoratedClassName(className);
            Name = methodName;
            Prototype = methodPrototype;
            DebugId = NextDebugId++;
            return;
        }
        #endregion

        #region PROPERTIES
        public AccessFlags AccessFlags { get; internal set; }

        internal List<Annotation> Annotations { get; set; }

        internal ushort ArgumentsWordsCount { get; set; }

        internal byte[] ByteCode { get; set; }

        public uint ByteCodeSize
        {
            get { return (null == ByteCode) ? 0 : (uint)ByteCode.Length; }
        }

        public uint ByteCodeRawAddress { get; internal set; }

        public IClass Class { get; private set; }

        public string ClassName { get; private set; }

        internal DebugInfo DebugInfo { get; set; }

        internal SortedList<TryBlock.SortKey, TryBlock> GuardedBlocks { get; private set; }

        public string Name { get; private set; }

        public IPrototype Prototype { get; private set; }

        internal ushort RegistersCount { get; set; }

        internal ushort ResultsWordsCount { get; set; }
        #endregion

        #region METHODS
        internal void AddGuardedBlock(TryBlock block)
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

        public void LinkTo(IClass owner)
        {
            if (null != Class) { throw new InvalidOperationException(); }
            Class = owner;
            return;
        }
        #endregion

        #region FIELDS
        internal readonly int DebugId;
        private static int NextDebugId = 1;
        #endregion
    }
}
