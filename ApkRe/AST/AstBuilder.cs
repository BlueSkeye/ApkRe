using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;
using com.rackham.ApkRe.ByteCode;
using com.rackham.ApkRe.CFG;
using com.rackham.ApkRe.Tree;

namespace com.rackham.ApkRe.AST
{
    internal static class AstBuilder
    {
        #region METHODS
        /// <summary>Build an AST tree for the given method.</summary>
        /// <param name="method">The method being disassembled.</param>
        /// <param name="sparseInstructions">A sparse array of dalvik instructions.
        /// Index in the array is the offset of the first byte of the indexed instruction.
        /// Most of the array is made of null references.</param>
        /// <param name="objectResolver">An implementation of the resolver that allow for
        /// retrieval of constant strings and types.</param>
        /// <returns></returns>
        internal static AstNode BuildTree(IMethod method, DalvikInstruction[] sparseInstructions,
            CfgNode methodRootCfgNode, IResolver objectResolver)
        {
            Console.WriteLine("Building '{0}' method ASTree. Method starting at 0x{2:X8}.",
                method.Name, method.ByteCodeRawAddress);
            AstNode rootNode = new AstNode(method);
            byte[] byteCode = method.GetByteCode();
            uint methodBaseAddress = method.ByteCodeRawAddress;
            uint opCodeIndex = 0;
            bool[] coveredWord = new bool[byteCode.Length / 2];
            SortedList<uint, uint> exclusions = new SortedList<uint, uint>();
            List<uint> pendingInstructionsOffset = new List<uint>();

            // Add entry point.
            pendingInstructionsOffset.Add(0);
            // As well as catch blocks from the exception because they aren't
            // referenced from normal code.
            foreach (ITryBlock tryBlock in method.EnumerateTryBlocks()) {
                foreach (IGuardHandler handler in tryBlock.EnumerateHandlers()) {
                    uint addedOffset = handler.HandlerMethodOffset;
                    // For debugging purpose. Should never occur.
                    if (addedOffset >= byteCode.Length) { throw new ApplicationException(); }
                    pendingInstructionsOffset.Add(addedOffset);
                }
            }

            while (0 < pendingInstructionsOffset.Count) {
                opCodeIndex = pendingInstructionsOffset[0];
                Console.WriteLine("OpCode at {0}", opCodeIndex);
                pendingInstructionsOffset.RemoveAt(0);
                bool fallInSequence = true;

                while (fallInSequence) {
                    throw new NotImplementedException();
                    //// Avoid targeting twice a single instruction.
                    //if (coveredWord[opCodeIndex / 2]) { break; }
                    //DalvikInstruction newNode = OpCodeDecoder.Decode(byteCode,
                    //    method.ByteCodeRawAddress, objectResolver, coveredWord,
                    //    ref opCodeIndex);
                    //fallInSequence = newNode.ContinueInSequence;
                    //// if (newNode.AssemblyCode.StartsWith("// 0002E8BC :")) { int l = 1; }
                    //uint[] otherTargetMethodOffsets = newNode.AdditionalTargetMethodOffsets;
                    //if (null != otherTargetMethodOffsets) {
                    //    for(int index = 0; index < otherTargetMethodOffsets.Length; index++) {
                    //        uint targetOffset = otherTargetMethodOffsets[index];

                    //        if (targetOffset >= byteCode.Length) { throw new ApplicationException(); }
                    //        if (!pendingInstructionsOffset.Contains(targetOffset)
                    //            && !coveredWord[targetOffset / 2]) {
                    //            pendingInstructionsOffset.Add(targetOffset);
                    //        }
                    //    }
                    //}
                }
            }

            // TODO : This is for debugging purpose. Not sure at all it will stand for
            // obfuscated code.
            for (int index = 0; index < coveredWord.Length; index++)
            {
                if (!coveredWord[index]) { throw new ApplicationException(); }
            }
            rootNode.Dump();
            return rootNode;
        }

        /// <summary>Add some node to the AST tree for try / catch blocks defined by
        /// the method.</summary>
        /// <param name="method">Method which try/catch blocks are to be added to the
        /// tree.</param>
        /// <param name="astRootNode">AST tree.</param>
        /// <param name="cfgRootNode">CFG tree. The tree may be modified due to new
        /// boudaries discovered with try/catch blocks.</param>
        internal static void CreateTryCatch(IMethod method, AstNode astRootNode,
            CfgNode cfgRootNode)
        {
            // First of all we need to sort try blocks for inclusion. Due to the Dalvik
            // constraint on try items definition we are assured to encounter a wrapping
            // block before a wrapped one.
            TryBlockHierarchyNode rootNode = null;
            // TODO - TEST - find test case with embedded try blocks.
            foreach (ITryBlock addedBlock in method.EnumerateTryBlocks()) {
                if (null == rootNode) { rootNode = new TryBlockHierarchyNode(); }
                rootNode.Walk(TryNodeInsertHandler, WalkMode.TransitBeforeAndAfter,
                    new TryInsertContext(addedBlock));
            }
            if (null == rootNode) { return; }
            rootNode.Walk(TryNodeHandler, WalkMode.SonsThenFather,
                new TryWalkContext(method, astRootNode, cfgRootNode));
            return;
        }

        /// <summary>A walk handler that will consume <see cref="TryBlockHierarchyNode"/>
        /// nodes and create associated nodes in the AST tree as well as update the CFG
        /// tree.</summary>
        /// <param name="node"></param>
        /// <param name="traversal"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static WalkContinuation TryNodeHandler(NodeBase<TryBlockHierarchyNode> node,
            WalkTraversal traversal, object rawContext)
        {
            if (null == node) { return WalkContinuation.Terminate; }
            // Skip root node that doesn't hold an associated ITryBlock.
            if (node.IsRootNode) { return WalkContinuation.Normal; }
            TryWalkContext context = (TryWalkContext)rawContext;
            ITryBlock block = ((TryBlockHierarchyNode)node).TryBlock;
            uint firstOffset = block.MethodStartOffset;
            uint lastOffset = block.MethodStartOffset + block.BlockSize - 1;

            Console.WriteLine("Handling try block [{0:X8}-{1:X8}]", firstOffset, lastOffset);
            List<NodeBase<AstNode>> wrapedNodes = new List<NodeBase<AstNode>>();
            context.AstRoot.Walk(delegate(NodeBase<AstNode> walkedAstNode, WalkTraversal astTreeTraversal,
                object astTreeContext)
                {
                    AstNode astNode = (AstNode)walkedAstNode;
                    if (null == astNode) { return WalkContinuation.Terminate; }
                    // Root node is a special case. It should never be considered as a covering
                    // node.
                    if (astNode.IsRootNode) { return WalkContinuation.Normal; }
                    if (astNode.IsAreaCovering(block.MethodStartOffset, block.BlockSize)) {
                        wrapedNodes.Add(walkedAstNode);
                        // Take care not to include sons.
                        return WalkContinuation.SkipSons;
                    }
                    return WalkContinuation.Normal;
                },
                WalkMode.FatherThenSons);
            // Here the wrappedNodes list is a set of AstNodes that are covered by the
            // try block of interest.
            TryNode tryNode = TryNode.Create(wrapedNodes);
            // Go on with catch associated blocks. Eventually adjust CFG block hosting the
            // catch handler first instruction so that the CFG block starts with this
            // instruction.
            foreach (IGuardHandler scannedHandler in block.EnumerateHandlers()) {
                throw new NotImplementedException();
                //CatchNode catchNode = CatchNode.Create(tryNode);
                //AstNode firstCatchInstruction = catchNode.Sons[0];
                //BlockNode hostingNode = context.CfgRoot.FindCoveringNode(
                //    firstCatchInstruction.MethodRelativeOffset, firstCatchInstruction.BlockSize);
                //if (null == hostingNode) { throw new REException(); }
                //if (hostingNode.FirstCoveredOffset != firstCatchInstruction.MethodRelativeOffset) {
                //    hostingNode = hostingNode.Split(firstCatchInstruction.MethodRelativeOffset);
                //}
            }
            return WalkContinuation.Normal;
        }

        /// <summary>A walk handler delegate that will build a tree of <see cref="TryBlockHierarchyNode"/>.
        /// The resulting tree will later be used to create new node in the AST tree and
        /// to augment the CFG tree.</summary>
        /// <param name="node">The walked node.</param>
        /// <param name="traversal">The way we are traversing the walked node.</param>
        /// <param name="rawContext">The walk context.</param>
        /// <returns></returns>
        private static WalkContinuation TryNodeInsertHandler(NodeBase<TryBlockHierarchyNode> node,
            WalkTraversal traversal, object rawContext)
        {
            if (null == node) { throw new ApplicationException(); }
            TryInsertContext context = (TryInsertContext)rawContext;
            if (traversal == WalkTraversal.AfterTransit) {
                if (node.IsRootNode || context.InsertOnAfterTransit) {
                    node.AddSon(new TryBlockHierarchyNode(context.ToInsert));
                    return WalkContinuation.Terminate;
                }
            }
            if (node.IsRootNode) {
                if (WalkTraversal.CurrentNode == traversal) {
                    // The root node is the one and only in the tree. Directly insert.
                    node.AddSon(new TryBlockHierarchyNode(context.ToInsert));
                    return WalkContinuation.Terminate;
                }
                // Otherwise skip processing root node because no ITryBlock is bound to
                // it and the Embedded test would fail.
                return WalkContinuation.Normal;
            }
            if (!((TryBlockHierarchyNode)node).IsEmbedded(context.ToInsert)) {
                return WalkContinuation.SkipSons;
            }
            // TODO - TEST - Find test case where we reach this point.
            context.InsertOnAfterTransit = true;
            return WalkContinuation.Normal;
        }
        #endregion

        #region INNER CLASSES
        private class TryBlockHierarchyNode : NodeBase<TryBlockHierarchyNode>
        {
            #region CONSTRUCTORS
            internal TryBlockHierarchyNode()
            {
                return;
            }

            internal TryBlockHierarchyNode(ITryBlock tryBlock)
            {
                TryBlock = tryBlock;
                return;
            }
            #endregion

            #region PROPERTIES
            internal ITryBlock TryBlock { get; set; }
            #endregion

            #region METHODS
            internal bool IsEmbedded(ITryBlock candidate)
            {
                if (this.TryBlock.MethodStartOffset > candidate.MethodStartOffset) {
                    return false;
                }
                uint endOffset = this.TryBlock.MethodStartOffset + this.TryBlock.BlockSize - 1;
                if (endOffset < candidate.MethodStartOffset) { return false; }
                if (endOffset >= candidate.MethodStartOffset) { return true; }
                throw new ApplicationException();
            }
            #endregion
        }

        private class TryInsertContext
        {
            internal TryInsertContext(ITryBlock toInsert)
            {
                ToInsert = toInsert;
                return;
            }

            internal bool InsertOnAfterTransit { get; set; }
            internal ITryBlock ToInsert { get; private set; }
        }

        private class TryWalkContext
        {
            internal TryWalkContext(IMethod method, AstNode astRootNode, CfgNode cfgRootNode)
            {
                AstRoot = astRootNode;
                CfgRoot = cfgRootNode;
                Method = method;
                return;
            }

            internal AstNode AstRoot { get; set; }
            internal CfgNode CfgRoot { get; set; }
            internal IMethod Method { get; set; }
        }
        #endregion
    }
}
