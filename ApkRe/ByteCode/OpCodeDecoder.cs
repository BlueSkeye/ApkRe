using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkHandler.API;
using com.rackham.ApkRe.AST;

namespace com.rackham.ApkRe.ByteCode
{
    /// <summary></summary>
    /// <remarks>Initialization code is located in the OpCodeDecoder-Init.cs file.
    /// </remarks>
    internal partial class OpCodeDecoder
    {
        #region CONSTRUCTORS
        static OpCodeDecoder()
        {
            try { InitializeOpCodes(); }
            catch (Exception e) { throw; }
            InitializeMnemonics();
            return;
        }

        private OpCodeDecoder(string formatId, string opCodeName, string argsDefinition,
            Type astNodeType)
        {
            _wordsCount = int.Parse(formatId.Substring(0, 1));
            _formatId = formatId;
            _mnemonic = formatId.Substring(2);
            _opCodeName = opCodeName;
            _argsDefinition = argsDefinition;
            _astNodeType = astNodeType;
            _isWide = !string.IsNullOrEmpty(opCodeName) && opCodeName.Contains("-wide");

            string pattern = _argsDefinition;
            switch (_formatId)
            {
                case "11x":
                    _sourceCodeFormatString = "{0} v{1}";
                    break;
                case "10t":
                case "20t":
                case "30t":
                    _sourceCodeFormatString = "{0} +{1}";
                    break;
                case "10x":
                    _sourceCodeFormatString = "{0}";
                    break;
                case "11n":
                case "21s":
                case "31i":
                case "51l":
                    _sourceCodeFormatString = "{0} v{1}, #+{2}";
                    break;
                case "12x":
                case "22x":
                case "32x":
                    _sourceCodeFormatString = "{0} v{1}, v{2}";
                    break;
                case "20bc":
                    // TODO : Definition is unclear.
                    throw new NotSupportedException();
                case "21c":
                    _argResolver = FindArgumentResolver(ref pattern);
                    _sourceCodeFormatString = "{0} " + pattern.Replace("AA", "{1}").Replace("BBBB", "{2}");
                    break;
                case "21h":
                    _sourceCodeFormatString = "{0} " + _argsDefinition.Replace("AA", "{1}").Replace("BBBB", "{2}");
                    break;
                case "21t":
                case "31t":
                    _sourceCodeFormatString = "{0} v{1}, +{2}";
                    break;
                case "22b":
                case "22s":
                    _sourceCodeFormatString = "{0} v{1}, v{2}, #+{3}";
                    break;
                case "22c":
                case "22cs":
                    _argResolver = FindArgumentResolver(ref pattern);
                    _sourceCodeFormatString = "{0} " + pattern.Replace("A", "{1}").Replace("B", "{2}").Replace("CCCC", "{3}");
                    break;
                case "22t":
                case "23x":
                    _sourceCodeFormatString = "{0} v{1}, v{2}, +{3}";
                    break;
                case "31c":
                    _argResolver = FindArgumentResolver(ref pattern);
                    _sourceCodeFormatString = "{0} " + pattern.Replace("AA", "{1}").Replace("BBBBBBBB", "{2}");
                    break;
                case "3rc":
                case "35c":
                case "35ms":
                case "35mi":
                case "3rms":
                case "3rmi":
                    _argResolver = FindArgumentResolver(ref pattern);
                    pattern = pattern.Replace("BBBB", "{2}");
                    _sourceCodeFormatString = "{0} " + pattern;
                    break;
                default:
                    throw new ApplicationException();
            }
            return;
        }
        #endregion

        #region METHODS
        /// <summary>Decode a single instruction into an <see cref="DalvikInstruction"/>
        /// having the given parent.</summary>
        /// <param name="opCodes">The method body being decoded.</param>
        /// <param name="baseAddress">The base address of the method body. Actually this
        /// is the offset within the dex file where the method body starts.</param>
        /// <param name="objectResolver"></param>
        /// <param name="coveredWord">An array of boolean that tell which of the opCode
        /// word are already disassembled.</param>
        /// <param name="index">The index of the first byte of the instruction being
        /// decoded.</param>
        /// <returns></returns>
        internal static DalvikInstruction Decode(byte[] opCodes, uint baseAddress,
            IResolver objectResolver, DalvikInstruction[] instructions, ref uint index)
        {
            if (null != instructions[index]) { throw new ApplicationException(); }
            uint initialIndexValue = index;
            uint thisAddress = (uint)(baseAddress + index);
            string address = string.Format("// {0:X8} : ", thisAddress);
            byte opCode = opCodes[index];
            byte codeArg = opCodes[index + 1];
            index += 2;
            StringBuilder extraArgBuilder;
            string assemblyCode;
            object additionalContent = null;

            // TODO : With big endian the opcode would be the second byte from the
            // current word.
            OpCodeDecoder decoder = _decoderPerOpCode[opCode];

            // Filter out unused instructions.
            if (null == decoder._opCodeName) { throw new REException(); }
            StringBuilder resultBuilder = new StringBuilder();
            List<object> printArgs = new List<object>();
            int argsCount;
            ushort resolverIndex;
            ushort extraWord;
            uint exclusion;
            long literalOrAddress = 0;
            ushort[] registers = null;
            DalvikInstruction result = null;

            try {
                switch (decoder._formatId)
                {
                    case "10t":
                        // GOTO : special case we compute the target address.
                        literalOrAddress = (int)((sizeof(ushort) * (sbyte)codeArg));
                        printArgs.Add(string.Format("{0:X8}", literalOrAddress));
                        break;
                    case "10x":
                        if (0 != codeArg) { throw new REException(); }
                        break;
                    case "11n":
                        registers = new ushort[] { (ushort)(codeArg & 0x0F) };
                        printArgs.Add(registers[0]);
                        literalOrAddress = ((codeArg & 0xF0) >> 4);
                        printArgs.Add(literalOrAddress);
                        break;
                    case "11x":
                        registers = new ushort[] { (ushort)codeArg };
                        printArgs.Add(registers[0]);
                        break;
                    case "12x":
                        registers = new ushort[] { (ushort)(codeArg & 0x0F), (ushort)((codeArg & 0xF0) >> 4) };
                        printArgs.Add(registers[0]);
                        printArgs.Add(registers[1]);
                        break;
                    case "20bc":
                        // TODO : Definition is unclear.
                        throw new NotSupportedException();
                    case "20t":
                        if (0 != codeArg) { throw new REException(); }
                        // GOTO/16 : special case we compute the target address.
                        literalOrAddress = (uint)((sizeof(ushort) * (short)GetNextDecodedUInt16(opCodes, ref index)));
                        printArgs.Add(string.Format("{0:X8}", literalOrAddress));
                        break;
                    case "21c":
                        resolverIndex = GetNextDecodedUInt16(opCodes, ref index);
                        registers = new ushort[] { (ushort)codeArg };
                        printArgs.Add(registers[0]);
                        // TODO : Must add an additional field in InstructionAstNode for this case.
                        printArgs.Add(decoder._argResolver(objectResolver, resolverIndex));
                        break;
                    case "21h":
                        registers = new ushort[] { (ushort)codeArg };
                        printArgs.Add(registers[0]);
                        literalOrAddress = GetNextDecodedInt16(opCodes, ref index) << (decoder._isWide ? 48 : 16);
                        printArgs.Add(literalOrAddress);
                        break;
                    case "21s":
                        registers = new ushort[] { (ushort)codeArg };
                        printArgs.Add(registers[0]);
                        literalOrAddress = GetNextDecodedInt16(opCodes, ref index);
                        printArgs.Add(literalOrAddress);
                        break;
                    case "21t":
                        registers = new ushort[] { (ushort)codeArg };
                        printArgs.Add(registers[0]);
                        literalOrAddress = GetNextDecodedInt16(opCodes, ref index);
                        printArgs.Add(literalOrAddress);
                        break;
                    case "22b":
                        goto case "23x";
                    case "22c":
                        resolverIndex = GetNextDecodedUInt16(opCodes, ref index);
                        registers = new ushort[] { (ushort)(codeArg & 0x0F), (ushort)((codeArg & 0xF0) >> 4) };
                        printArgs.Add(registers[0]);
                        printArgs.Add(registers[1]);
                        // TODO : Must add an additional field in InstructionAstNode for this case.
                        printArgs.Add(decoder._argResolver(objectResolver, resolverIndex));
                        break;
                    case "22s":
                    case "22t":
                        registers = new ushort[] { (ushort)(codeArg & 0x0F), (ushort)((codeArg & 0xF0) >> 4) };
                        printArgs.Add(registers[0]);
                        printArgs.Add(registers[1]);
                        literalOrAddress = GetNextDecodedInt16(opCodes, ref index);
                        printArgs.Add(literalOrAddress);
                        break;
                    case "22x":
                        registers = new ushort[] { (ushort)codeArg, GetNextDecodedUInt16(opCodes, ref index) };
                        printArgs.Add(registers[0]);
                        printArgs.Add(registers[1]);
                        break;
                    case "23x":
                        extraWord = GetNextDecodedUInt16(opCodes, ref index);
                        registers = new ushort[] { (ushort)codeArg, (ushort)(extraWord & 0x00FF),
                            (ushort)((extraWord & 0xFF00) >> 8) };
                        printArgs.Add(registers[0]);
                        printArgs.Add(registers[1]);
                        printArgs.Add(registers[2]);
                        break;
                    case "30t":
                        // GOTO/32 : special case we compute the target address.
                        literalOrAddress = ((sizeof(ushort) * (int)GetNextDecodedInt32(opCodes, ref index)));
                        printArgs.Add(string.Format("{0:X8}", literalOrAddress));
                        break;
                    case "31i":
                        registers = new ushort[] { (ushort)codeArg };
                        printArgs.Add(registers[0]);
                        literalOrAddress = GetNextDecodedInt32(opCodes, ref index);
                        printArgs.Add(literalOrAddress);
                        break;
                    case "31t":
                        registers = new ushort[] { (ushort)codeArg };
                        printArgs.Add(registers[0]);
                        // fill-array-data, packed-switch, sparse-switch
                        // Special case we compute the target address.
                        literalOrAddress = exclusion = (uint)(thisAddress + (sizeof(ushort) * (short)GetNextDecodedInt32(opCodes, ref index)));
                        additionalContent = DecodeAdditionalContent(opCodes,
                            (uint)(literalOrAddress - baseAddress), instructions,
                            initialIndexValue);
                        printArgs.Add(string.Format("{0:X8}", literalOrAddress));
                        break;
                    case "32x":
                        registers = new ushort[] {
                            GetNextDecodedUInt16(opCodes, ref index),
                            GetNextDecodedUInt16(opCodes, ref index)
                        };
                        printArgs.Add(registers[0]);
                        printArgs.Add(registers[1]);
                        break;
                    case "3rc":
                        literalOrAddress = resolverIndex = GetNextDecodedUInt16(opCodes, ref index);
                        extraWord = GetNextDecodedUInt16(opCodes, ref index);
                        extraArgBuilder = new StringBuilder();
                        extraArgBuilder.AppendFormat("{{v{0} .. v{1}}}", extraWord, (ushort)(extraWord + codeArg - 1));
                        printArgs.Add(extraArgBuilder.ToString());
                        printArgs.Add(decoder._argResolver(objectResolver, resolverIndex));
                        break;
                    case "35c":
                        literalOrAddress = resolverIndex = GetNextDecodedUInt16(opCodes, ref index);
                        argsCount = (codeArg & 0xF0) >> 4;
                        if (0 < argsCount) { registers = new ushort[argsCount]; }
                        extraWord = (0 == argsCount) ? (ushort)0 : GetNextDecodedUInt16(opCodes, ref index);
                        extraArgBuilder = new StringBuilder();
                        switch (argsCount)
                        {
                            case 0:
                                break;
                            case 5:
                                registers[4] = (ushort)((codeArg & 0x0F00) >> 8);
                                extraArgBuilder.Insert(0, " ,v" + registers[4].ToString());
                                goto case 4;
                            case 4:
                                registers[3] = (ushort)((extraWord & 0xF000) >> 12);
                                extraArgBuilder.Insert(0, " ,v" + registers[3].ToString());
                                goto case 3;
                            case 3:
                                registers[2] = (ushort)((extraWord & 0x0F00) >> 8);
                                extraArgBuilder.Insert(0, " ,v" + registers[2].ToString());
                                goto case 2;
                            case 2:
                                registers[1] = (ushort)((extraWord & 0x00F0) >> 4);
                                extraArgBuilder.Insert(0, " ,v" + registers[1].ToString());
                                goto case 1;
                            case 1:
                                registers[0] = (ushort)(extraWord & 0x000F);
                                extraArgBuilder.Insert(0, "v" + registers[0].ToString());
                                break;
                            default:
                                throw new REException();
                        }
                        extraArgBuilder.Insert(0, "{");
                        extraArgBuilder.Append("}");
                        printArgs.Add(extraArgBuilder.ToString());
                        printArgs.Add(decoder._argResolver(objectResolver, resolverIndex));
                        break;
                    case "51l":
                        registers = new ushort[] { (ushort)codeArg };
                        printArgs.Add(registers[0]);
                        literalOrAddress = GetNextDecodedInt64(opCodes, ref index);
                        printArgs.Add(literalOrAddress);
                        break;
                    default:
                        throw new ApplicationException();
                }
                printArgs.Insert(0, decoder._opCodeName);
                try { resultBuilder.AppendFormat(decoder._sourceCodeFormatString, printArgs.ToArray()); }
                catch { throw; }
                assemblyCode = address + resultBuilder.ToString();
                result = DalvikInstruction.Create(decoder._astNodeType, initialIndexValue,
                    (uint)(index - initialIndexValue), assemblyCode);
                result.LiteralOrAddress = literalOrAddress;
                result.Registers = registers;
                if (null != additionalContent) { result.SetAdditionalContent(additionalContent); }
                return result;
            }
            finally { instructions[initialIndexValue] = result; }
        }

        /// <summary>This method decodes additional content associated with instructions
        /// haing a "31t" format identifier.</summary>
        /// <param name="opCodes"></param>
        /// <param name="index"></param>
        /// <param name="baseOffset">Offset within method code of the opcode that is referencing
        /// this additional content. This is meaningfull only for switch instructions in order to
        /// compute targets offset.</param>
        /// <returns>Depending on the kind of additional content this is either :
        /// 1°) an array of bytes to be used for array initialization
        /// 2°) a dictionnary of method related offsets to jump locations keyed by the switch
        /// matching value for packed switch management.</returns>
        private static object DecodeAdditionalContent(byte[] opCodes, uint index,
            DalvikInstruction[] instructions, uint baseOffset)
        {
            uint initialIndexValue = index;

            // Additional content must be aligned on a doubleword boundary. We use a little
            // trick to attempt to discover a nop opCode that may exist prior to the additional
            // content.
            if ((sizeof(ushort) <= index) && (0 == opCodes[index - 1]) && (0 == opCodes[index - 2])) {
                // Found a nop. At least consider this as a covered word.
                // TODO : Consider creating a Nop instruction instance.
                instructions[initialIndexValue] = null;
            }

            byte opCode = opCodes[index++];
            byte codeArg = opCodes[index++];
            // Initial value accounts for opCode and codeArg. Later augmented depending on the
            // kind of codeArg
            uint coveredWordsCount = 1;

            if (0 != opCode) { throw new REException(); }
            uint dataSize;
            int[] keys;
            Dictionary<int, uint> targetMethodOffsets;

            switch (codeArg) {
                case 0x01:
                    dataSize = GetNextDecodedUInt16(opCodes, ref index);
                    targetMethodOffsets = new Dictionary<int,uint>();
                    int firstKey = GetNextDecodedInt32(opCodes, ref index);
                    coveredWordsCount += (sizeof(ushort) + sizeof(int) + (sizeof(uint) * dataSize)) / sizeof(ushort);
                    for (int targetIndex = 0; targetIndex < dataSize; targetIndex++) {
                        // WARNING : The retrieved double word is a word count not a byte count.
                        uint targetMethodOffset = (uint)(baseOffset + (sizeof(ushort) * GetNextDecodedInt32(opCodes, ref index)));
                        targetMethodOffsets[targetIndex + firstKey] = targetMethodOffset;
                    }
                    return targetMethodOffsets;
                case 0x02:
                    dataSize = GetNextDecodedUInt16(opCodes, ref index);
                    keys = new int[dataSize];
                    targetMethodOffsets = new Dictionary<int,uint>();
                    coveredWordsCount += (sizeof(ushort) + ((sizeof(int) + sizeof(uint)) * dataSize)) / sizeof(ushort);
                    for (int targetIndex = 0; targetIndex < dataSize; targetIndex++) {
                        int key = GetNextDecodedInt32(opCodes, ref index);
                        // WARNING : The retrieved double word is a word count not a byte count.
                        uint targetMethodOffset = (uint)(baseOffset + (sizeof(ushort) * (GetNextDecodedUInt32(opCodes, ref index))));
                        targetMethodOffsets[key] = targetMethodOffset;
                    }
                    return targetMethodOffsets;
                case 0x03:
                    ushort elementSize = GetNextDecodedUInt16(opCodes, ref index);
                    uint elementsCount = GetNextDecodedUInt32(opCodes, ref index);
                    uint rawDataSize = elementsCount * elementSize;
                    coveredWordsCount += (sizeof(ushort) + sizeof(uint) + (rawDataSize + 1)) / 2;
                    byte[] initializationData = new byte[(int)rawDataSize];
                    Buffer.BlockCopy(opCodes, (int)index, initializationData, 0, initializationData.Length);
                    return initializationData;
                default:
                    throw new REException();
            }
        }

        private static ArgResolverDelegate FindArgumentResolver(ref string argsDefinition)
        {
            int aroIndex = argsDefinition.IndexOf('@');
            if (-1 == aroIndex) { throw new ApplicationException(); }
            int wordStartIndex;
            for (wordStartIndex = aroIndex - 1; 0 <= wordStartIndex; wordStartIndex--)
            {
                if (!char.IsLetter(argsDefinition[wordStartIndex])) { break; }
            }
            string resolutionKind = argsDefinition.Substring(wordStartIndex + 1, aroIndex - wordStartIndex - 1);
            string patternPrefix = argsDefinition.Substring(0, wordStartIndex);
            argsDefinition = ((patternPrefix.Contains("{")) ? "{1}, " : patternPrefix)
                + argsDefinition.Substring(aroIndex + 1);
            switch (resolutionKind) {
                case "field":
                    return ResolveField;
                case "fieldoff":
                    throw new NotSupportedException();
                case "kind":
                    throw new NotSupportedException();
                case "meth":
                    return ResolveMethod;
                case "string":
                    return ResolveString;
                case "type":
                    return ResolveType;
                default:
                    throw new ApplicationException();
            }
        }

        private static short GetNextDecodedInt16(byte[] opCodes, ref uint index)
        {
            // TODO : Modify this to account for big endian
            return (short)(opCodes[index++] + (opCodes[index++] << 8));
        }

        private static ushort GetNextDecodedUInt16(byte[] opCodes, ref uint index)
        {
            // TODO : Modify this to account for big endian
            return (ushort)(opCodes[index++] + (opCodes[index++] << 8));
        }

        private static int GetNextDecodedInt32(byte[] opCodes, ref uint index)
        {
            // TODO : Modify this to account for big endian
            return (int)(opCodes[index++] + (opCodes[index++] << 8) + (opCodes[index++] << 16) + (opCodes[index++] << 24));
        }

        private static uint GetNextDecodedUInt32(byte[] opCodes, ref uint index)
        {
            // TODO : Modify this to account for big endian
            return (uint)(GetNextDecodedUInt16(opCodes, ref index) + (GetNextDecodedUInt16(opCodes, ref index) << 16));
        }

        private static long GetNextDecodedInt64(byte[] opCodes, ref uint index)
        {
            // TODO : Modify this to account for big endian
            return (long)(opCodes[index++] + (opCodes[index++] << 8) + (opCodes[index++] << 16) + (opCodes[index++] << 24) +
                (opCodes[index++] << 32) + (opCodes[index++] << 40) + (opCodes[index++] << 48) + (opCodes[index++] << 56));
        }

        private static string ResolveField(IResolver objectResolver, ushort index)
        {
            IField field = objectResolver.ResolveField(index);

            if (null == field) { throw new REException(); }
            return field.Class.FullName + "/" + field.Name;
        }

        private static string ResolveMethod(IResolver objectResolver, ushort index)
        {
            IMethod method = objectResolver.ResolveMethod(index);

            if (null == method) { throw new REException(); }
            return method.Class.FullName + "/" + method.Name;
        }

        private static string ResolveString(IResolver objectResolver, ushort index)
        {
            string result = objectResolver.ResolveString(index);

            if (null == result) { throw new REException(); }
            return result;
        }

        private static string ResolveType(IResolver objectResolver, ushort index)
        {
            IType result = objectResolver.ResolveType(index);

            if (null == result) { throw new REException(); }
            return result.FullName;
        }

        private static void UpdateCoverage(bool[] coveredWord, uint initialIndexValue,
            uint coveredWordsCount)
        {
            uint lastCoveredIndex = initialIndexValue + (2 * coveredWordsCount) - 1;
            for (uint coverageIndex = initialIndexValue;
                coverageIndex <= lastCoveredIndex;
                coverageIndex += 2)
            {
                coveredWord[coverageIndex / 2] = true;
            }
            return;
        }
        #endregion

        #region FIELDS
        private static Dictionary<byte, OpCodeDecoder> _decoderPerOpCode =
            new Dictionary<byte, OpCodeDecoder>();
        private static Dictionary<char, int> _perMnemonicBytesCount =
            new Dictionary<char,int>();
        private ArgResolverDelegate _argResolver;
        private string _argsDefinition;
        private Type _astNodeType;
        private string _formatId;
        private bool _isWide;
        private string _sourceCodeFormatString;
        private string _mnemonic;
        private string _opCodeName;
        private int _wordsCount;
        #endregion

        #region INNER CLASSES
        private delegate string ArgResolverDelegate(IResolver objectResolver, ushort index);
        #endregion
    }
}
