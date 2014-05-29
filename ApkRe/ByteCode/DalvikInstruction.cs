using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;
namespace com.rackham.ApkRe.ByteCode
{
    internal abstract class DalvikInstruction
    {
        #region CONSTRUCTORS
        protected DalvikInstruction(uint methodOffset, uint size)
        {
            this.MethodRelativeOffset = methodOffset;
            this.BlockSize = size;
            return;
        }
        #endregion

        #region PROPERTIES
        /// <summary>Returns an array (that may be empty or a null reference) with each
        /// item being an offset relative to the owning method of another instruction
        /// targeted by this one. The next instruction in sequence MUST NOT appear in
        /// this list (TODO : see for goto/32).</summary>
        internal abstract uint[] AdditionalTargetMethodOffsets { get; }

        /// <summary>The assembly code for this instruction.</summary>
        internal string AssemblyCode { get; private set; }

        internal uint BlockSize { get; private set; }

        /// <summary>Does this instruction allows for conditional or unconditional
        /// execution of the next instruction.</summary>
        internal abstract bool ContinueInSequence { get; }

        /// <summary>A literal value or an address this instruction uses. This value
        /// may or may not be meaningfull depending on the instruction format identifier.
        /// </summary>
        internal long LiteralOrAddress { get; set; }

        /// <summary>Get the offset within the whole method code of the first byte that
        /// belongs to this node.</summary>
        internal uint MethodRelativeOffset { get; private set; }

        /// <summary>Offset of next instruction within the method.
        /// WARNING : This is a hint only. There is no guaranty that there is effectively
        /// a "next" instruction.</summary>
        internal uint NextInstructionOffset
        {
            get { return MethodRelativeOffset + BlockSize; }
        }

        /// <summary>An array (that may be a null reference) of registers referenced
        /// by this instruction.</summary>
        internal ushort[] Registers { get; set; }
        #endregion

        #region METHODS
        /// <summary>An <see cref="AstInstructionNode"/> factory that recognize every known
        /// subclass type and invoke the appropriate constructor.</summary>
        /// <param name="type">Type of node to be created. This must be one of the known sub
        /// classes.</param>
        /// <param name="parent">Parent node.</param>
        /// <param name="methodRelativeOffset"></param>
        /// <param name="size"></param>
        /// <param name="assemblyCode"></param>
        /// <returns></returns>
        internal static DalvikInstruction Create(Type type, uint methodRelativeOffset, uint size,
            string assemblyCode)
        {
            DalvikInstruction result = null;

            // TODO : Consider using a dictionary of delegates.
            try {
                if (typeof(ArrayConstructionInstruction) == type) {
                    return (result = new ArrayConstructionInstruction(methodRelativeOffset, size));
                }
                if (typeof(ArrayOperationInstruction) == type) {
                    return (result = new ArrayOperationInstruction(methodRelativeOffset, size));
                }
                if (typeof(BinaryOperationInstruction) == type) {
                    return (result = new BinaryOperationInstruction(methodRelativeOffset, size));
                }
                if (typeof(CheckCastInstruction) == type) {
                    return (result = new CheckCastInstruction(methodRelativeOffset, size));
                }
                if (typeof(ComparisonInstruction) == type) {
                    return (result = new ComparisonInstruction(methodRelativeOffset, size));
                }
                if (typeof(ConditionalBranchInstruction) == type) {
                    return (result = new ConditionalBranchInstruction(methodRelativeOffset, size));
                }
                if (typeof(InstanceConstructionInstruction) == type) {
                    return (result = new InstanceConstructionInstruction(methodRelativeOffset, size));
                }
                if (typeof(InstanceFieldInstruction) == type) {
                    return (result = new InstanceFieldInstruction(methodRelativeOffset, size));
                }
                if (typeof(LoadConstantInstruction) == type)
                {
                    return (result = new LoadConstantInstruction(methodRelativeOffset, size));
                }
                if (typeof(MethodCallInstruction) == type) {
                    return (result = new MethodCallInstruction(methodRelativeOffset, size));
                }
                if (typeof(MonitorInstruction) == type) {
                    return (result = new MonitorInstruction(methodRelativeOffset, size));
                }
                if (typeof(MoveInstruction) == type) {
                    return (result = new MoveInstruction(methodRelativeOffset, size));
                }
                if (typeof(NopInstruction) == type) {
                    return (result = new NopInstruction(methodRelativeOffset, size));
                }
                if (typeof(ReturnInstruction) == type) {
                    return (result = new ReturnInstruction(methodRelativeOffset, size));
                }
                if (typeof(StaticFieldInstruction) == type) {
                    return (result = new StaticFieldInstruction(methodRelativeOffset, size));
                }
                if (typeof(SwitchInstruction) == type) {
                    return (result = new SwitchInstruction(methodRelativeOffset, size));
                }
                if (typeof(ThrowInstruction) == type) {
                    return (result = new ThrowInstruction(methodRelativeOffset, size));
                }
                if (typeof(UnaryOperationInstruction) == type) {
                    return (result = new UnaryOperationInstruction(methodRelativeOffset, size));
                }
                if (typeof(UnconditionalBranchInstruction) == type) {
                    return (result = new UnconditionalBranchInstruction(methodRelativeOffset, size));
                }
            }
            finally { if (null != result) { result.AssemblyCode = assemblyCode;} }
            throw new ApplicationException();
        }

        internal virtual void SetAdditionalContent(object data)
        {
            throw new InvalidOperationException();
        }
        #endregion
    }
}
