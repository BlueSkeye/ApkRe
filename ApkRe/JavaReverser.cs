using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;

using com.rackham.ApkHandler;
using com.rackham.ApkHandler.API;
using com.rackham.ApkHandler.Dex;
using com.rackham.ApkRe.AST;
using com.rackham.ApkRe.ByteCode;
using com.rackham.ApkRe.CFG;
using com.rackham.ApkRe.Tree;

namespace com.rackham.ApkRe
{
    public class JavaReverser
    {
        #region CONSTRUCTORS
        public JavaReverser(JavaReversingContext context, DexFile input)
        {
            if (null == context) { throw new ArgumentNullException(); }
            if (null == input) { throw new REException(); }
            _context = context;
            _input = input;
            _objectResolver = (IResolver)input;
            _treeHandler = new SourceCodeTreeHandler(context.BaseSourceCodeDirectory);
            EnsureStorage();
            return;
        }
        #endregion

        #region PROPERTIES
        public static bool CfgDebuggingAvailable
        {
            get
            {
#if DBGCFG
                return true;
#else
                return false;
#endif
            }
        }
#endregion

#region METHODS
        /// <summary>Build the class header source code. This encompass :
        /// - The class package definition.
        /// - The import definitions
        /// - The class definition including the type it extends and the interfaces it
        ///   implements.</summary>
        /// <param name="definition">Class definition.</param>
        /// <param name="namespaceByImportedType"></param>
        /// <returns>A <see cref="StringBuilder"/> holding the source code.</returns>
        private StringBuilder BuildClassHeaderSourceCode(IClass definition,
            Dictionary<string, string> namespaceByImportedType)
        {
            StringBuilder resultBuilder = new StringBuilder();
            string thisClassNamespace;
            string thisClassCanonicName =
                com.rackham.ApkHandler.Helpers.GetCanonicTypeName(definition.Name, out thisClassNamespace);

            // Resolve implemented interfaces namespaces.
            List<string> simpleInterfaceNames = new List<string>();
            string simpleBaseClassName = GetCanonicTypeName(definition.SuperClass.Name,
                namespaceByImportedType);

            foreach (string fullInterfaceName in definition.EnumerateImplementedInterfaces()) {
                string simpleInterfaceName = GetCanonicTypeName(fullInterfaceName, namespaceByImportedType);
                simpleInterfaceNames.Add(simpleInterfaceName);
            }

            resultBuilder.AppendFormat("package {0};\n", thisClassNamespace);
            resultBuilder.Append("\n");
            foreach (KeyValuePair<string, string> pair in namespaceByImportedType) {
                // Types from the same namespace than the reversed class are automatically
                // imported. No needs to emit an import directive.
                if (pair.Value == thisClassNamespace) { continue; }
                resultBuilder.AppendFormat(string.Format("import {0}.{1};\n", pair.Value, pair.Key));
            }
            resultBuilder.Append(Helpers.GetModifiersSourceCode(definition.Access));
            if (definition.IsEnumeration) { resultBuilder.Append("enum "); }
            else if (definition.IsInterface) { resultBuilder.Append("interface "); }
            else { resultBuilder.Append("class "); }
            resultBuilder.AppendFormat(" {0} ", thisClassCanonicName);
            if (!definition.IsInterface) { resultBuilder.AppendFormat("extends {0}", simpleBaseClassName); }
            foreach (string interfaceName in simpleInterfaceNames) {
                resultBuilder.AppendFormat(", {0}", interfaceName);
            }
            resultBuilder.AppendLine();
            resultBuilder.AppendLine("{");
            return resultBuilder;
        }

        private string BuildFieldDefinition(IField field,
            Dictionary<string, string> namespaceByImportedType)
        {
            string canonicFieldType =
                GetCanonicTypeName(field.Class.Name, namespaceByImportedType);
            return string.Format("{0} {1} {2};",
                Helpers.GetModifiersSourceCode(field.AccessFlags), canonicFieldType,
                field.Name);
        }

        /// <summary>Decode instructions for the given method.</summary>
        /// <param name="method">The method to be decoded.</param>
        /// <param name="objectResolver">An implementation of an object resolver to be
        /// used for retrieving classes and constant strings referenced from the bytecode.
        /// </param>
        /// <returns>An array indexed by the offset within method byte code and storing
        /// the instruction starting at this offset (if any).</returns>
        private static DalvikInstruction[] BuildInstructionList(IMethod method,
            IResolver objectResolver)
        {
            DalvikInstruction[] result = new DalvikInstruction[method.ByteCodeSize];
            byte[] byteCode = method.GetByteCode();
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
            Console.WriteLine(
                "Decoding '{0}' method bytecode on {1} bytes starting at 0x{2:X8}.",
                Helpers.BuildMethodDeclarationString(method), byteCode.Length,
                method.ByteCodeRawAddress);
            
            while (0 < pendingInstructionsOffset.Count) {
                uint opCodeIndex = pendingInstructionsOffset[0];
                pendingInstructionsOffset.RemoveAt(0);
                bool fallInSequence = true;

                while (fallInSequence) {
                    // Avoid targeting twice a single instruction.
                    if (null != result[opCodeIndex]) { break; }
                    DalvikInstruction newNode = OpCodeDecoder.Decode(byteCode,
                        method.ByteCodeRawAddress, objectResolver, result,
                        ref opCodeIndex);

                    // Analyse possible targets after this instruction and augment
                    // pending instructions list accordingly.
                    fallInSequence = newNode.ContinueInSequence;
                    uint[] otherTargetMethodOffsets = newNode.AdditionalTargetMethodOffsets;
                    if (null != otherTargetMethodOffsets) {
                        for(int index = 0; index < otherTargetMethodOffsets.Length; index++) {
                            uint targetOffset = otherTargetMethodOffsets[index];

                            if (targetOffset >= byteCode.Length) { throw new ApplicationException(); }
                            if (!pendingInstructionsOffset.Contains(targetOffset)
                                && (null == result[targetOffset])) {
                                pendingInstructionsOffset.Add(targetOffset);
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>Build source code for a single method.</summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private string BuildSourceCode(IMethod method, AstNode methodRootNode,
            Dictionary<string, string> namespaceByImportedType)
        {
            byte[] byteCode = method.GetByteCode();
            if (null == byteCode) { return ""; }
            StringBuilder resultBuilder = new StringBuilder();
            int opCodeIndex = 0;
            SortedList<uint, uint> exclusions = new SortedList<uint, uint>();

            GenerateMethodHeader(method, resultBuilder, namespaceByImportedType);
            throw new NotImplementedException();
            //SourceCodeWalkerContext sourceCodeWalkerContext = new SourceCodeWalkerContext(resultBuilder);
            //methodRootNode.Walk(SourceCodeWalker, WalkMode.TransitBeforeAndAfter,
            //    sourceCodeWalkerContext);
            //foreach(InstructionAstNode scannedNode in methodRootNode.WalkLeaf<InstructionAstNode>()) {
            //    resultBuilder.AppendLine(scannedNode.AssemblyCode);
            //}
            //IEnumerator<ITryBlock> tryBlocksEnumerator = method.EnumerateTryBlocks().GetEnumerator();
            //ITryBlock nextGuardedBlock =
            //    tryBlocksEnumerator.MoveNext() ? tryBlocksEnumerator.Current : null;
            //Stack<ITryBlock> partialTryBlocks = new Stack<ITryBlock>();
            //while (byteCode.Length > opCodeIndex)
            //{
            //    // Check for next guarded block beginning.
            //    while ((null != nextGuardedBlock)
            //        && ((sizeof(ushort) * nextGuardedBlock.StartAddress) == opCodeIndex))
            //    {
            //        // The current block is starting at current decoder position.
            //        resultBuilder.Append("try { \n");
            //        partialTryBlocks.Push(nextGuardedBlock);
            //        // Acquire next guarded block if any and loop again. This is because this
            //        // new guarded block may share the same start offset than the one we just
            //        // handled.
            //        nextGuardedBlock =
            //            tryBlocksEnumerator.MoveNext() ? tryBlocksEnumerator.Current : null;
            //    }

            //    // Decode current instruction.
            //    resultBuilder.Append(OpCodeDecoder.Decode(null, byteCode, method.ByteCodeRawAddress,
            //        _objectResolver, exclusions, ref opCodeIndex));

            //    // Handle partial guarded block termination.
            //    while (true)
            //    {
            //        if (0 == partialTryBlocks.Count) { break; }
            //        ITryBlock nextClosingBlock = partialTryBlocks.Peek();
            //        int lastCoveredOpCodeIndex = (int)(sizeof(ushort) *
            //            (nextClosingBlock.StartAddress + nextClosingBlock.InstructionsCount - 1));
            //        if (lastCoveredOpCodeIndex >= opCodeIndex) { break; }
            //        // This test for code robustness. Should never be true.
            //        if (lastCoveredOpCodeIndex != (opCodeIndex - sizeof(ushort)))
            //        {
            //            throw new ApplicationException();
            //        }
            //        // Current block terminates here.
            //        nextClosingBlock = partialTryBlocks.Pop();
            //    }
            //}
            //if (0 != partialTryBlocks.Count) { throw new REException(); }
            //if (byteCode.Length != opCodeIndex) { throw new REException(); }
            //if (0 < exclusions.Count) { throw new REException(); }
            resultBuilder.Append("}");
            return resultBuilder.ToString();
        }

        private void EnsureStorage()
        {
            if (!IsolatedStorageFile.IsEnabled) { return; }
            try { _cache = IsolatedStorageFile.GetUserStoreForAssembly(); }
            catch { return; }
        }

        private void GenerateMethodHeader(IMethod method, StringBuilder into,
            Dictionary<string, string> namespaceByImportedType)
        {
            into.Append(Helpers.GetModifiersSourceCode(method.AccessFlags));
            string canonicReturnType =
                GetCanonicTypeName(method.Prototype.ReturnType, namespaceByImportedType);
            into.Append(canonicReturnType + " " + method.Name + " (");
            int parameterIndex = 0;
            int parametersCount = (null == method.Prototype.ParametersType)
                ? 0
                : method.Prototype.ParametersType.Count;
            for(int index = 0; index < parametersCount; index++) {
                string fullParameterType = method.Prototype.ParametersType[index];
                string canonicParameterType =
                    GetCanonicTypeName(fullParameterType, namespaceByImportedType);

                if (0 < index) { into.Append(", "); }
                into.AppendFormat("{0} p{1}", canonicParameterType, parameterIndex++);
            }
            into.AppendLine(")");
            into.AppendLine("{");
            return;
        }

        /// <summary>Starting from the given Dalvik full type name, translate it to a Java
        /// syntax type name. Also attempt to remove the result namespace taking care to
        /// resolve homonimy conflicts.</summary>
        /// <param name="fullDalvikTypeName"></param>
        /// <param name="namespaceByImportedType">A dictionary of import namespaces keyed
        /// by the imported class name.</param>
        /// <returns></returns>
        private string GetCanonicTypeName(string fullDalvikTypeName,
            Dictionary<string, string> namespaceByImportedType)
        {
            string alreadyRegisteredNamespace;
            string candidateNamespace;
            string simpleClassName =
                com.rackham.ApkHandler.Helpers.GetCanonicTypeName(fullDalvikTypeName, out candidateNamespace);

            if (null == candidateNamespace) { return simpleClassName; }
            if (!namespaceByImportedType.TryGetValue(simpleClassName, out alreadyRegisteredNamespace)) {
                namespaceByImportedType[simpleClassName] = candidateNamespace;
                return simpleClassName;
            }
            if (alreadyRegisteredNamespace == candidateNamespace) { return simpleClassName; }
            return fullDalvikTypeName;
        }

        /// <summary>The main method for this class. Will reverse every class in turn.
        /// </summary>
        public void Reverse()
        {
            int reversedClassesCount = 0;
            int totalReversedMethodsCount = 0;

            List<IClass> pending = new List<IClass>(_input.EnumerateClasses());
            while (0 < pending.Count) {
                IClass scanned = pending[0];
                pending.RemoveAt(0);
                if (!scanned.IsSuperClassResolved && !pending.Contains(scanned)) {
                    FileInfo resolveTo =
                        _context.ResolveClassNameToFile(scanned.SuperClassName);
                    if (null == resolveTo) {
                        Console.WriteLine("Failed to reverse class '{0}' to any in context file.",
                            scanned.SuperClassName);
                        // Load file now.
                        // throw new NotImplementedException();
                    }
                }
            }
            int j = 1;

            Console.WriteLine("Building classes hierarchy -----------------");
            InheritanceHierarchyBuilder inheritanceBuilder =
                new InheritanceHierarchyBuilder(_context);
            if (!inheritanceBuilder.Build(_input.EnumerateClasses())) {
                return;
            }

            foreach (IClass item in _input.EnumerateClasses()) {
                int reversedMethodsCount;
                Console.WriteLine("Reversing class '{0}' ----------------------------------",
                    item.FullName);
                ReverseClass(item, out reversedMethodsCount);
                totalReversedMethodsCount += reversedMethodsCount;
                reversedClassesCount++;
            }
            Console.WriteLine("Reversed {0} methods spaning {1} classes.",
                totalReversedMethodsCount, reversedClassesCount);
            return;
        }

        /// <summary>Reverse a single class and output result in a file named from
        /// the class name and located in the appropriate directory reflecting the
        /// class package name.</summary>
        /// <param name="item">The class to be reversed.</param>
        /// <param name="reversedMethodsCount">On return this parameter is updated
        /// with the count of methods reversed within this class.</param>
        private void ReverseClass(IClass item, out int reversedMethodsCount)
        {
            FileInfo targetFile = _treeHandler.GetClassFileName(item.Name);
            List<string> headerSourceCode = new List<string>();
            List<string> methodsSourceCode = new List<string>();
            StringBuilder methodsSourceCodeBuilder = new StringBuilder();
            StringBuilder fieldsSourceCodeBuilder = new StringBuilder();
            Dictionary<string, string> namespaceByImportedType = new Dictionary<string,string>();

            reversedMethodsCount = 0;
            if (!item.IsAbstract) {
                foreach (IField field in item.EnumerateFields()) {
                    fieldsSourceCodeBuilder.AppendLine(
                        BuildFieldDefinition(field, namespaceByImportedType));
                }
#if DBGCFG
                bool debugClassCfg = item.IsAnnotatedWith(CfgDebugAnnotation.Id);
#endif
                foreach (IMethod method in item.EnumerateMethods()) {
#if DBGCFG
                    bool debugMethodCfg = debugClassCfg || method.IsAnnotatedWith(CfgDebugAnnotation.Id);
#endif
                    reversedMethodsCount++;
                    // Extract all instructions from the method byte code.
                    DalvikInstruction[] sparseInstructions = BuildInstructionList(method, _objectResolver);
                    // Construct a first version of the CFG graph.
                    CfgNode methodRootCfgNode = CfgBuilder.BuildBasicTree(method, sparseInstructions
#if DBGCFG
                        , debugMethodCfg
#endif
                        );
                    // Ensure each catch block from each try block starts on a block boundary.
                    // Eventually create additional block to enforce the constraint.
                    foreach (ITryBlock scannedBlock in method.EnumerateTryBlocks()) {
                        foreach (IGuardHandler scannedHandler in scannedBlock.EnumerateHandlers()) {
                            CfgBuilder.EnsureBlockBoundary(methodRootCfgNode, scannedHandler.HandlerMethodOffset);
                        }
                    }
                    // Detect cycles in CFG graph.
                    List<CircuitDefinition> circuits =
                        new DirectedGraphCycleFinder(methodRootCfgNode).Resolve();

                    if (0 < circuits.Count) {
                        int circuitId = 1;
                        foreach (CircuitDefinition circuit in circuits) {
                            Console.WriteLine("CIRCUIT #{0} ------------------", circuitId++);
                            circuit.Print();
                        }
                        int i = 1;
                    }
                    // Go on now with the AST tree. This is where we perform the real job.
                    //AstNode methodRootAstNode =
                    //    AstBuilder.BuildTree(method, sparseInstructions, methodRootCfgNode, _objectResolver);
                    
                    //// Here we are we a basic CFG graph and an AST tree that is composed only
                    //// of disassembled instruction. Time to augment the AST.
                    //AstBuilder.CreateTryCatch(method, methodRootAstNode, methodRootCfgNode);
                    //methodsSourceCodeBuilder.Append(
                    //    BuildSourceCode(method, methodRootAstNode, namespaceByImportedType));
                    //methodsSourceCodeBuilder.AppendLine();
                }
            }

            FileStream stream = null;

            try {
                stream = File.Open(targetFile.FullName, FileMode.Create, FileAccess.ReadWrite,
                    FileShare.ReadWrite);

                using (StreamWriter writer = new StreamWriter(stream)) {
                    writer.Write(BuildClassHeaderSourceCode(item, namespaceByImportedType).ToString());
                    writer.WriteLine("// FIELDS -----------------");
                    writer.Write(fieldsSourceCodeBuilder.ToString());
                    writer.WriteLine("// METHODS -----------------");
                    writer.Write(methodsSourceCodeBuilder.ToString());
                    // Class closing curly braces.
                    writer.WriteLine("}");
                }
            }
            finally { if (null != stream) { stream.Close(); } }
        }

        ///// <summary>A walk delegate that is used to produce the source code for a method.</summary>
        ///// <param name="node">Currently walked code.</param>
        ///// <param name="traversal">Which way we entered the walked node.</param>
        ///// <param name="context">Walk context.</param>
        ///// <returns></returns>
        //private WalkContinuation SourceCodeWalker(NodeBase<AstNode> node, WalkTraversal traversal,
        //    object context)
        //{
        //    SourceCodeWalkerContext walkerContext = (SourceCodeWalkerContext)context;
        //    DalvikInstruction instructionNode = node as DalvikInstruction;

        //    // Most of the time we will encounter this kind of node until we come close to
        //    // a source reconstruction algorithm.
        //    if (null != instructionNode) {
        //        walkerContext.Builder.Append(instructionNode.AssemblyCode);
        //        return WalkContinuation.Normal;
        //    }
        //    TryNode tryNode = node as TryNode;

        //    if (null != tryNode) {
        //        switch (traversal) {
        //            case WalkTraversal.BeforeTransit:
        //                walkerContext.Builder.AppendLine("try {");
        //                break;
        //            case WalkTraversal.AfterTransit:
        //                walkerContext.Builder.AppendLine("}");
        //                break;
        //            default:
        //                throw new ApplicationException();
        //        }
        //    }

        //    return WalkContinuation.Normal;
        //}
        #endregion

        #region FIELDS
        private IsolatedStorageFile _cache;
        private JavaReversingContext _context;
        private DexFile _input;
        private IResolver _objectResolver;
        private SourceCodeTreeHandler _treeHandler;
        #endregion

        #region INNER CLASSES
        private class SourceCodeWalkerContext
        {
            #region CONSTRUCTORS
            internal SourceCodeWalkerContext(StringBuilder builder)
            {
                Builder = builder;
                return;
            }
            #endregion

            #region PROPERTIES
            internal StringBuilder Builder { get; private set; }
            #endregion
        }
        #endregion
    }
}
