﻿using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;
using com.rackham.ApkRe.ByteCode;
using com.rackham.ApkRe.AST;

namespace com.rackham.ApkRe.CFG
{
    internal static class CfgBuilder
    {
        #region METHODS
        /// <summary>Make sure the given parameter is not a null reference and
        /// at the same time is an entry node, otherwise throw an exception.</summary>
        /// <param name="entryNode">Candidate entry node.</param>
        private static void AssertEntryNodeParameter(CfgNode entryNode)
        {
            if (null == entryNode) { throw new ArgumentNullException(); }
            if (!entryNode.IsEntryNode) { throw new ArgumentException(); }
            return;
        }

        /// <summary></summary>
        /// <param name="method"></param>
        /// <param name="rootAstNode"></param>
        /// <returns></returns>
        internal static CfgNode BuildBasicTree(IMethod method, DalvikInstruction[] instructions)
        {
            CfgNode result = new CfgNode();

#if DBGCFG
            uint debugAddress =
                // 0x000A198;
                0xFFFFFFFF;
            CfgNode.DebugEnabled = (debugAddress == method.ByteCodeRawAddress);
            if (debugAddress == method.ByteCodeRawAddress) { int i = 1; }
#endif
            CreateBasicBlocks(result, method, instructions);
            EnsureExitNodeUniqueness(result);
#if DBGCFG
            result.DumpGraph();
#endif
            return result;
        }

        /// <summary>Create basic blocks.</summary>
        /// <param name="rootNode">The root node of the graph.</param>
        /// <param name="method">The method which body is involved.</param>
        /// <param name="sparseInstructions">A sparse array of instructions to be analyzed.
        /// Some of the entries in this array are expected to be null references
        /// denoting no instruction starts at that particular offset within the method
        /// bytecode.</param>
        private static void CreateBasicBlocks(CfgNode rootNode, IMethod method,
            DalvikInstruction[] sparseInstructions)
        {
            List<BlockNode> unreachableBlocks = new List<BlockNode>();
            BlockNode currentBlock = null;
            BlockNode[] blocksPerOffset = new BlockNode[method.ByteCodeSize];
            blocksPerOffset[0] = currentBlock;

            // Create basic blocks. These are set of consecutive instructions with
            // each instruction having a single successor that is the next instruction.
            foreach (DalvikInstruction scannedInstruction in sparseInstructions) {
                if (null == scannedInstruction) { continue; }
#if DBGCFG
                if (CfgNode.DebugEnabled) {
                    Console.WriteLine("@{0:X4} {1}", scannedInstruction.MethodRelativeOffset,
                        scannedInstruction.GetType().Name);
                }
#endif
                if (null == currentBlock) {
                    // The previous instruction didn't fail in sequence. Either there is already a
                    // block defined for the current offset or we must create a new one.
                    currentBlock = blocksPerOffset[scannedInstruction.MethodRelativeOffset];
                    if (null == currentBlock) {
                        currentBlock = new BlockNode();
                        blocksPerOffset[scannedInstruction.MethodRelativeOffset] = currentBlock;
                        unreachableBlocks.Add(currentBlock);
#if DBGCFG
                        if (CfgNode.DebugEnabled) { Console.WriteLine("Created unreachable block #{0}", currentBlock.NodeId); }
#endif
                    }
#if DBGCFG
                    else { if (CfgNode.DebugEnabled) {Console.WriteLine("Reusing block #{0}", currentBlock.NodeId); } }
#endif
                }
                else {
                    // Last instruction failed in sequence. However we may have already defined a
                    // block for the current offset.
                    BlockNode alreadyDefined = blocksPerOffset[scannedInstruction.MethodRelativeOffset];

                    if ((null != alreadyDefined) && !object.ReferenceEquals(currentBlock, alreadyDefined)) {
#if DBGCFG
                        if (CfgNode.DebugEnabled) {
                            Console.WriteLine("Linking block #{0} to block #{1} and switching to the later",
                                currentBlock.NodeId, alreadyDefined.NodeId);
                        }
#endif
                        // make the already defined the current block.
                        CfgNode.Link(currentBlock, alreadyDefined);
                        currentBlock = alreadyDefined;
                    }
#if DBGCFG
                    else { if (CfgNode.DebugEnabled) {Console.WriteLine("Continuing with block #{0}", currentBlock.NodeId); } }
#endif
                }
                currentBlock.Bind(scannedInstruction);
                for(uint sizeIndex = 0; sizeIndex < scannedInstruction.BlockSize; sizeIndex++) {
                    int offsetIndex = (int)(scannedInstruction.MethodRelativeOffset + sizeIndex);
                    if ((null != blocksPerOffset[offsetIndex])
                        && !object.ReferenceEquals(blocksPerOffset[offsetIndex], currentBlock)) {
                        throw new ApplicationException();
                    }
                    blocksPerOffset[offsetIndex] = currentBlock;
                }
                // Must create a block for each possible target and link current
                // block to each of those blocks.
                // Scan other targets if any.
                uint[] otherOffsets = scannedInstruction.AdditionalTargetMethodOffsets;
                if (null == otherOffsets) {
                    if (!scannedInstruction.ContinueInSequence) {
                        // Must switch to another block.
                        currentBlock = null;
                    }
                    continue;
                }
                // Having other targets force a block reset AND the next instruction to be in
                // a separate block than the current one, provided the current instruction fails
                // in sequence.
                if (scannedInstruction.ContinueInSequence) {
                    uint nextInstructionOffset = scannedInstruction.MethodRelativeOffset + scannedInstruction.BlockSize;
                    BlockNode nextBlock = blocksPerOffset[nextInstructionOffset];
                    if (null == nextBlock) {
                        nextBlock = new BlockNode();
                        blocksPerOffset[nextInstructionOffset] = nextBlock;
#if DBGCFG
                        if (CfgNode.DebugEnabled) {
                            Console.WriteLine("Created next block #{0} @{1:X4}",
                                nextBlock.NodeId, nextInstructionOffset);
                            Console.WriteLine("Linking block #{0} to block #{1}",
                                currentBlock.NodeId, nextBlock.NodeId);
                        }
#endif
                        // Link current node and next one.
                        CfgNode.Link(currentBlock, nextBlock);
                    }
                }
                for (int index = 0; index < otherOffsets.Length; index++) {
                    uint targetOffset = otherOffsets[index];
                    BlockNode targetBlock = blocksPerOffset[targetOffset];

                    if (null == targetBlock) {
                        // Block doesn't exists yet. Create and register it.
                        targetBlock = new BlockNode();
                        blocksPerOffset[targetOffset] = targetBlock;
#if DBGCFG
                        if (CfgNode.DebugEnabled) {
                            Console.WriteLine("Pre-registering block #{0} @{1:X4}",
                                targetBlock.NodeId, targetOffset);
                            Console.WriteLine("Linking block #{0} to block #{1}",
                                currentBlock.NodeId, targetBlock.NodeId);
                        }
#endif
                        // Link current node and next one.
                        CfgNode.Link(currentBlock, targetBlock);
                        continue;
                    }
                    // The target block already exists albeit it may deserve a split.
                    if (0 == targetOffset) { continue; }
                    BlockNode splitCandidate = targetBlock;

                    try {
                        if (!object.ReferenceEquals(blocksPerOffset[targetOffset - 1], splitCandidate)) {
                            // The split candidate actually starts at target address. No split required.
                            continue;
                        }
                        // Need a split.
                        targetBlock = splitCandidate.Split(targetOffset);
#if DBGCFG
                        if (CfgNode.DebugEnabled) {
                            Console.WriteLine("Spliting block #{0} @{1:X4}. Block #{2} created.",
                                splitCandidate.NodeId, targetOffset, targetBlock.NodeId);
                        }
#endif
                        // Update offest to block mapping for new splited block..
                        for (int splitIndex = (int)targetOffset; splitIndex < blocksPerOffset.Length; splitIndex++) {
                            if (!object.ReferenceEquals(blocksPerOffset[splitIndex], splitCandidate)) { break; }
                            blocksPerOffset[splitIndex] = targetBlock;
                        }
                    }
                    finally {
#if DBGCFG
                        if (CfgNode.DebugEnabled) {
                            try
                            {
                                Console.WriteLine("Linking block #{0} to block #{1}",
                                    currentBlock.NodeId, targetBlock.NodeId);
                            }
                            catch { int i = 1; }
                        }
#endif
                        // Link current node and next one.
                        CfgNode.Link(currentBlock, targetBlock);
                        if (unreachableBlocks.Contains(targetBlock))
                        {
                            unreachableBlocks.Remove(targetBlock);
#if DBGCFG
                            if (CfgNode.DebugEnabled) {
                                Console.WriteLine("Previously unreachable block #{0} now reachable.",
                                    targetBlock.NodeId);
                            }
#endif
                        }
                    }
                }
                currentBlock = null;
            }
#if DBGCFG
            if (CfgNode.DebugEnabled) { Console.WriteLine("Basic blocks creation done."); }
#endif
            // Link allunreachable blocks to the root node.
            foreach (BlockNode scannedBlock in unreachableBlocks) { CfgNode.Link(rootNode, scannedBlock); }
            return;
        }

        /// <summary>Make sure there is a block boundary at the given method offset. If no
        /// split the block hosting the instruction at this offset in two separate blocks.
        /// </summary>
        /// <param name="startNode">Start node for the tree to be considered.</param>
        /// <param name="methodOffset">Offset to be considered.</param>
        internal static void EnsureBlockBoundary(CfgNode startNode, uint methodOffset)
        {
            BlockNode hostingNode = startNode.FindHostingNode(methodOffset, 1);

            if (null == hostingNode) { throw new REException(); }
            if (hostingNode.FirstCoveredOffset == methodOffset) { return; }
            hostingNode.Split(methodOffset);
            return;
        }

        /// <summary>Enumerate all node starting from the given entryNode in no
        /// particular order.</summary>
        /// <param name="entryNode">The entry node from the graph to be walked.</param>
        /// <returns>A node enumerable object.</returns>
        private static IEnumerable<CfgNode> EnumerateAllNodes(CfgNode entryNode)
        {
            AssertEntryNodeParameter(entryNode);
            List<CfgNode> pendingNodes = new List<CfgNode>();
            List<CfgNode> alreadyWalked = new List<CfgNode>();
            pendingNodes.Add(entryNode);

            while (0 < pendingNodes.Count) {
                CfgNode candidate = pendingNodes[0];
                pendingNodes.RemoveAt(0);
                if (alreadyWalked.Contains(candidate)) { continue; }
                yield return candidate;
                alreadyWalked.Add(candidate);
                if (null == candidate.Successors) { continue; }
                foreach (CfgNode targetNode in candidate.Successors) {
                    if (alreadyWalked.Contains(targetNode)) { continue; }
                    if (pendingNodes.Contains(targetNode)) { continue; }
                    pendingNodes.Add(targetNode);
                }
            }
        }

        /// <summary>Make sure there is a single exit node in the graph whose start
        /// node is provided. This may lead to the creation of a new exit node.
        /// </summary>
        /// <param name="entryNode">The graph entry node.</param>
        private static void EnsureExitNodeUniqueness(CfgNode entryNode)
        {
            AssertEntryNodeParameter(entryNode);
            CfgNode addedExitNode = null;
            CfgNode exitNodeCandidate = null;

            foreach(CfgNode candidate in EnumerateAllNodes(entryNode)) {
                if (!candidate.IsExitNode) { continue; }
                if ((null != addedExitNode) && !object.ReferenceEquals(addedExitNode, candidate)) {
                    CfgNode.Link(candidate, addedExitNode);
                    continue;
                }
                if (null == exitNodeCandidate) {
                    exitNodeCandidate = candidate;
                    continue;
                }
                addedExitNode = new CfgNode();
                CfgNode.Link(candidate, addedExitNode);
                CfgNode.Link(exitNodeCandidate, addedExitNode);
                exitNodeCandidate = null;
            }
            return;
        }
        #endregion
    }
}