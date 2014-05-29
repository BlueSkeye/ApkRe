using System;
using System.Collections.Generic;
using System.Text;

namespace com.rackham.ApkRe.AST
{
    /// <summary>Nodes of this kind hold byte code that has not yet been
    /// parsed.</summary>
    internal class RawByteCodeAstNode : AstNode
    {
        #region CONSTRUCTORS
        internal RawByteCodeAstNode(AstNode parent, uint methodOffset, byte[] rawCode)
            : base(parent, methodOffset, (uint)rawCode.Length)
        {
            _rawCode = (byte[])rawCode.Clone();
        }
        #endregion

        #region PROPERTIES
        internal byte[] RawCode
        {
            get { return (byte[])_rawCode.Clone(); }
        }
        #endregion

        #region METHODS
        /// <summary>Split this block.</summary>
        /// <param name="atMethodOffset"></param>
        /// <param name="newNodeSize"></param>
        /// <param name="factory"></param>
        /// <returns></returns>
        internal AstNode Split(uint atMethodOffset, uint newNodeSize,
            NodeConstructorDelegate factory)
        {
            if (!IsMethodOffsetOwner(atMethodOffset)) { throw new ApplicationException(); }
            if (!!IsMethodOffsetOwner(atMethodOffset + newNodeSize - 1)) { throw new ApplicationException(); }
            RawByteCodeAstNode beforeNode = null;
            RawByteCodeAstNode afterNode = null;
            int beforeSplitLocalOffset = (int)MethodRelativeOffset;
            int afterSplitLocalOffset = (int)(beforeSplitLocalOffset + newNodeSize);
            byte[] createdNodeContent = new byte[newNodeSize];
            Buffer.BlockCopy(_rawCode, beforeSplitLocalOffset, createdNodeContent,
                0, (int)newNodeSize);
            if (0 != beforeSplitLocalOffset) {
                byte[] beforeNodeContent = new byte[beforeSplitLocalOffset];
                Buffer.BlockCopy(_rawCode, 0, beforeNodeContent, 0, beforeSplitLocalOffset);
                beforeNode = new RawByteCodeAstNode(this.Parent, MethodRelativeOffset, beforeNodeContent);
            }
            if (afterSplitLocalOffset < BlockSize) {
                int afterSize = (int)(BlockSize - afterSplitLocalOffset);
                byte[] afterNodeContent = new byte[afterSize];
                Buffer.BlockCopy(_rawCode, afterSplitLocalOffset, afterNodeContent,
                    0, afterSize);
                afterNode = new RawByteCodeAstNode(this.Parent,
                    (uint)(MethodRelativeOffset + afterSplitLocalOffset), afterNodeContent);
            }
            AstNode createdNode = factory(this.Parent, (uint)(MethodRelativeOffset + beforeSplitLocalOffset),
                createdNodeContent);
            SplitNotify(this, beforeNode, createdNode, afterNode);
            return createdNode;
        }
        #endregion

        #region FIELDS
        private byte[] _rawCode;
        #endregion
    }
}
