using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkRe.AST;

namespace com.rackham.ApkRe.ByteCode
{
    /// <summary>This partial class file is used to group static initialization code
    /// and thus preventing noise in the class main source.</summary>
    internal partial class OpCodeDecoder
    {
        private static void InitializeMnemonics()
        {
            _perMnemonicBytesCount['b'] = 8; // immediate signed byte
            _perMnemonicBytesCount['c'] = 16; // or 32 	constant pool index
            _perMnemonicBytesCount['f'] = 16; // interface constants (only used in statically linked formats)
            _perMnemonicBytesCount['h'] = 16; // immediate signed hat (high-order bits of a 32- or 64-bit value; low-order bits are all 0)
            _perMnemonicBytesCount['i'] = 32; // immediate signed int, or 32-bit float
            _perMnemonicBytesCount['l'] = 64; // immediate signed long, or 64-bit double
            _perMnemonicBytesCount['m'] = 16; // method constants (only used in statically linked formats)
            _perMnemonicBytesCount['n'] = 4; // immediate signed nibble
            _perMnemonicBytesCount['s'] = 16; // immediate signed short
            _perMnemonicBytesCount['t'] = 8; // or 16 or 32 	branch target
            _perMnemonicBytesCount['x'] = 0; // no additional data
            return;
        }

        private static void InitializeOpCodes()
        {
            // Waste cycles.
            // Note: Data-bearing pseudo-instructions are tagged with this opcode, in which
            // case the high-order byte of the opcode unit indicates the nature of the data.
            // See "packed-switch-payload Format", "sparse-switch-payload Format", and
            // "fill-array-data-payload Format" below.
            _decoderPerOpCode[0x00] = new OpCodeDecoder("10x", "nop", null, typeof(NopInstruction));
            // Move the contents of one non-object register to another.
            //A: destination register (4 bits)
            //B: source register (4 bits)
            _decoderPerOpCode[0x01] = new OpCodeDecoder("12x", "move", "vA, vB", typeof(MoveInstruction));
            // Move the contents of one non-object register to another.
            // A: destination register (8 bits)
            //B: source register (16 bits)
            _decoderPerOpCode[0x02] = new OpCodeDecoder("22x", "move/from16", "vAA, vBBBB", typeof(MoveInstruction));
            // Move the contents of one non-object register to another.
            //A: destination register (16 bits)
            //B: source register (16 bits)
            _decoderPerOpCode[0x03] = new OpCodeDecoder("32x", "move/16", "vAAAA, vBBBB", typeof(MoveInstruction));
            // Move the contents of one register-pair to another.
            //A: destination register pair (4 bits)
            //B: source register pair (4 bits)
            //Note: It is legal to move from vN to either vN-1 or vN+1, so implementations must arrange
            // for both halves of a register pair to be read before anything is written.
            _decoderPerOpCode[0x04] = new OpCodeDecoder("12x", "move-wide", "vA, vB", typeof(MoveInstruction));
            //Move the contents of one register-pair to another.
            //A: destination register pair (8 bits)
            //B: source register pair (16 bits)
            //Note: Implementation considerations are the same as move-wide, above.
            _decoderPerOpCode[0x05] = new OpCodeDecoder("22x", "move-wide/from16", "vAA, vBBBB", typeof(MoveInstruction));
            // Move the contents of one register-pair to another.
            //A: destination register pair (16 bits)
            //B: source register pair (16 bits)
            //Note: Implementation considerations are the same as move-wide, above.
            _decoderPerOpCode[0x06] = new OpCodeDecoder("32x", "move-wide/16", "vAAAA, vBBBB", typeof(MoveInstruction));
            // Move the contents of one object-bearing register to another.
            //A: destination register (4 bits)
            //B: source register (4 bits)
            _decoderPerOpCode[0x07] = new OpCodeDecoder("12x", "move-object", "vA, vB", typeof(MoveInstruction));
            //Move the contents of one object-bearing register to another.
            //A: destination register (8 bits)
            //B: source register (16 bits)
            _decoderPerOpCode[0x08] = new OpCodeDecoder("22x", "move-object/from16", "vAA, vBBBB", typeof(MoveInstruction));
            // Move the contents of one object-bearing register to another.
            //A: destination register (16 bits)
            //B: source register (16 bits)
            _decoderPerOpCode[0x09] = new OpCodeDecoder("32x", "move-object/16", "vAAAA, vBBBB", typeof(MoveInstruction));
            //A: destination register (8 bits)
            // Move the single-word non-object result of the most recent invoke-kind into the indicated
            // register. This must be done as the instruction immediately after an invoke-kind whose
            // (single-word, non-object) result is not to be ignored; anywhere else is invalid.
            _decoderPerOpCode[0x0A] = new OpCodeDecoder("11x", "move-result", "vAA", typeof(MoveInstruction));
            // Move the double-word result of the most recent invoke-kind into the indicated register
            // pair. This must be done as the instruction immediately after an invoke-kind whose
            // (double-word) result is not to be ignored; anywhere else is invalid.
            //A: destination register pair (8 bits)
            _decoderPerOpCode[0x0B] = new OpCodeDecoder("11x", "move-result-wide", "vAA", typeof(MoveInstruction));
            // Move the object result of the most recent invoke-kind into the indicated register.
            // This must be done as the instruction immediately after an invoke-kind or filled-new-array
            // whose (object) result is not to be ignored; anywhere else is invalid.
            //A: destination register (8 bits)
            _decoderPerOpCode[0x0C] = new OpCodeDecoder("11x", "move-result-object", "vAA", typeof(MoveInstruction));
            // Save a just-caught exception into the given register. This must be the first instruction
            // of any exception handler whose caught exception is not to be ignored, and this instruction
            // must only ever occur as the first instruction of an exception handler; anywhere else is
            // invalid.
            //A: destination register (8 bits)
            _decoderPerOpCode[0x0D] = new OpCodeDecoder("11x", "move-exception", "vAA", typeof(MoveInstruction));
            // Return from a void method.
            _decoderPerOpCode[0x0E] = new OpCodeDecoder("10x", "return-void", null, typeof(ReturnInstruction));
            // Return from a single-width (32-bit) non-object value-returning method.
            //A: return value register (8 bits)
            _decoderPerOpCode[0x0F] = new OpCodeDecoder("11x", "return", "vAA", typeof(ReturnInstruction));
            // Return from a double-width (64-bit) value-returning method.
            //A: return value register-pair (8 bits)
            _decoderPerOpCode[0x10] = new OpCodeDecoder("11x", "return-wide", "vAA", typeof(ReturnInstruction));
            // Return from an object-returning method.
            //A: return value register (8 bits)
            _decoderPerOpCode[0x11] = new OpCodeDecoder("11x", "return-object", "vAA", typeof(ReturnInstruction));
            //Move the given literal value (sign-extended to 32 bits) into the specified register.
            //A: destination register (4 bits)
            //B: signed int (4 bits)
            _decoderPerOpCode[0x12] = new OpCodeDecoder("11n", "const/4", "vA, #+B", typeof(LoadConstantInstruction));
            //Move the given literal value (sign-extended to 32 bits) into the specified register.
            //A: destination register (8 bits)
            //B: signed int (16 bits)
            _decoderPerOpCode[0x13] = new OpCodeDecoder("21s", "const/16", "vAA, #+BBBB", typeof(LoadConstantInstruction));
            // Move the given literal value into the specified register.
            //A: destination register (8 bits)
            //B: arbitrary 32-bit constant
            _decoderPerOpCode[0x14] = new OpCodeDecoder("31i", "const", "vAA, #+BBBBBBBB", typeof(LoadConstantInstruction));
            // Move the given literal value (right-zero-extended to 32 bits) into the specified register.
            //A: destination register (8 bits)
            //B: signed int (16 bits)
            _decoderPerOpCode[0x15] = new OpCodeDecoder("21h", "const/high16", "vAA, #+BBBB0000", typeof(LoadConstantInstruction));
            //Move the given literal value (sign-extended to 64 bits) into the specified register-pair.
            //A: destination register (8 bits)
            //B: signed int (16 bits)
            _decoderPerOpCode[0x16] = new OpCodeDecoder("21s", "const-wide/16", "vAA, #+BBBB", typeof(LoadConstantInstruction));
            //Move the given literal value (sign-extended to 64 bits) into the specified register-pair.
            //A: destination register (8 bits)
            //B: signed int (32 bits)
            _decoderPerOpCode[0x17] = new OpCodeDecoder("31i", "const-wide/32", "vAA, #+BBBBBBBB", typeof(LoadConstantInstruction));
            //Move the given literal value into the specified register-pair.
            //A: destination register (8 bits)
            //B: arbitrary double-width (64-bit) constant
            _decoderPerOpCode[0x18] = new OpCodeDecoder("51l", "const-wide", "vAA, #+BBBBBBBBBBBBBBBB", typeof(LoadConstantInstruction));
            //Move the given literal value (right-zero-extended to 64 bits) into the specified register-pair.
            //A: destination register (8 bits)
            //B: signed int (16 bits)
            _decoderPerOpCode[0x19] = new OpCodeDecoder("21h", "const-wide/high16", "vAA, #+BBBB000000000000", typeof(LoadConstantInstruction));
            //Move a reference to the string specified by the given index into the specified register.
            //A: destination register (8 bits)
            //B: string index
            _decoderPerOpCode[0x1A] = new OpCodeDecoder("21c", "const-string", "vAA, string@BBBB", typeof(LoadConstantInstruction));
            //Move a reference to the string specified by the given index into the specified register.
            //A: destination register (8 bits)
            //B: string index
            _decoderPerOpCode[0x1B] = new OpCodeDecoder("31c", "const-string/jumbo", "vAA, string@BBBBBBBB", typeof(LoadConstantInstruction));
            //Move a reference to the class specified by the given index into the specified register. In the case where the indicated type is primitive, this will store a reference to the primitive type's degenerate class.
            //A: destination register (8 bits)
            //B: type index
            _decoderPerOpCode[0x1C] = new OpCodeDecoder("21c", "const-class", "vAA, type@BBBB", typeof(LoadConstantInstruction));
            //Acquire the monitor for the indicated object.
            //A: reference-bearing register (8 bits)
            _decoderPerOpCode[0x1D] = new OpCodeDecoder("11x", "monitor-enter", "vAA", typeof(MonitorInstruction));
            //A: reference-bearing register (8 bits)
            //Release the monitor for the indicated object.
            //Note: If this instruction needs to throw an exception, it must do so as if the pc has
            // already advanced past the instruction. It may be useful to think of this as the
            // instruction successfully executing (in a sense), and the exception getting thrown after
            // the instruction but before the next one gets a chance to run. This definition makes
            // it possible for a method to use a monitor cleanup catch-all (e.g., finally) block as
            // the monitor cleanup for that block itself, as a way to handle the arbitrary exceptions
            // that might get thrown due to the historical implementation of Thread.stop(), while still
            // managing to have proper monitor hygiene.
            _decoderPerOpCode[0x1E] = new OpCodeDecoder("11x", "monitor-exit", "vAA", typeof(MonitorInstruction));
            //Throw a ClassCastException if the reference in the given register cannot be cast to the
            // indicated type.
            //A: reference-bearing register (8 bits)
            //B: type index (16 bits)
            //Note: Since A must always be a reference (and not a primitive value), this will necessarily
            // fail at runtime (that is, it will throw an exception) if B refers to a primitive type.
            _decoderPerOpCode[0x1F] = new OpCodeDecoder("21c", "check-cast", "vAA, type@BBBB", typeof(CheckCastInstruction));
            // Store in the given destination register 1 if the indicated reference is an instance of the
            // given type, or 0 if not.
            //A: destination register (4 bits)
            //B: reference-bearing register (4 bits)
            //C: type index (16 bits)
            //Note: Since B must always be a reference (and not a primitive value), this will always result
            // in 0 being stored if C refers to a primitive type.
            _decoderPerOpCode[0x20] = new OpCodeDecoder("22c", "instance-of", "vA, vB, type@CCCC", typeof(ComparisonInstruction));
            //Store in the given destination register the length of the indicated array, in entries
            //A: destination register (4 bits)
            //B: array reference-bearing register (4 bits)
            _decoderPerOpCode[0x21] = new OpCodeDecoder("12x", "array-length", "vA, vB", typeof(ArrayOperationInstruction));
            // Construct a new instance of the indicated type, storing a reference to it in the destination.
            // The type must refer to a non-array class.
            //A: destination register (8 bits)
            //B: type index
            _decoderPerOpCode[0x22] = new OpCodeDecoder("21c", "new-instance", "vAA, type@BBBB", typeof(InstanceConstructionInstruction));
            // Construct a new array of the indicated type and size. The type must be an array type.
            //A: destination register (8 bits)
            //B: size register
            //C: type index
            _decoderPerOpCode[0x23] = new OpCodeDecoder("22c", "new-array", "vA, vB, type@CCCC", typeof(ArrayConstructionInstruction));
            // Construct an array of the given type and size, filling it with the supplied contents.
            // The type must be an array type. The array's contents must be single-word (that is,
            // no arrays of long or double, but reference types are acceptable). The constructed
            // instance is stored as a "result" in the same way that the method invocation instructions
            // store their results, so the constructed instance must be moved to a register with an
            // immediately subsequent move-result-object instruction (if it is to be used).
            //A: array size and argument word count (4 bits)
            //B: type index (16 bits)
            //C..G: argument registers (4 bits each)
            _decoderPerOpCode[0x24] = new OpCodeDecoder("35c", "filled-new-array", "{vC, vD, vE, vF, vG}, type@BBBB", typeof(ArrayConstructionInstruction));
            // Construct an array of the given type and size, filling it with the supplied contents.
            // Clarifications and restrictions are the same as filled-new-array, described above.
            //A: array size and argument word count (8 bits)
            //B: type index (16 bits)
            //C: first argument register (16 bits)
            //N = A + C - 1
            _decoderPerOpCode[0x25] = new OpCodeDecoder("3rc", "filled-new-array/range", "{vCCCC .. vNNNN}, type@BBBB", typeof(ArrayConstructionInstruction));
            // Fill the given array with the indicated data. The reference must be to an array of
            // primitives, and the data table must match it in type and must contain no more elements
            // than will fit in the array. That is, the array may be larger than the table, and if so,
            // only the initial elements of the array are set, leaving the remainder alone.
            //(with supplemental data as specified below in "fill-array-data-payload Format")
            //A: array reference (8 bits)
            //B: signed "branch" offset to table data pseudo-instruction (32 bits) 
            _decoderPerOpCode[0x26] = new OpCodeDecoder("31t", "fill-array-data", "vAA, +BBBBBBBB", typeof(ArrayConstructionInstruction));
            //Throw the indicated exception.
            //A: exception-bearing register (8 bits)
            _decoderPerOpCode[0x27] = new OpCodeDecoder("11x", "throw", "vAA", typeof(ThrowInstruction));
            //Unconditionally jump to the indicated instruction.
            //A: signed branch offset (8 bits)
            //Note: The branch offset must not be 0. (A spin loop may be legally constructed either
            // with goto/32 or by including a nop as a target before the branch.)
            // Remark : The branch offset is relative to the current instruction address and
            // is expressed as a signed instruction count (not as a relative bytes count).
            _decoderPerOpCode[0x28] = new OpCodeDecoder("10t", "goto", "AA", typeof(UnconditionalBranchInstruction));
            //Unconditionally jump to the indicated instruction.
            //A: signed branch offset (16 bits)
            //Note: The branch offset must not be 0. (A spin loop may be legally constructed either
            // with goto/32 or by including a nop as a target before the branch.)
            // Remark : The branch offset is relative to the current instruction address and
            // is expressed as a signed instruction count (not as a relative bytes count).
            _decoderPerOpCode[0x29] = new OpCodeDecoder("20t", "goto/16", "+AAAA", typeof(UnconditionalBranchInstruction));
            //Unconditionally jump to the indicated instruction.
            //A: signed branch offset (32 bits)
            // Remark : The branch offset is relative to the current instruction address and
            // is expressed as a signed instruction count (not as a relative bytes count).
            _decoderPerOpCode[0x2A] = new OpCodeDecoder("30t", "goto/32", "+AAAAAAAA", typeof(UnconditionalBranchInstruction));
            // Jump to a new instruction based on the value in the given register, using a table of
            // offsets corresponding to each value in a particular integral range, or fall through
            // to the next instruction if there is no match.
            //(with supplemental data as specified below in "packed-switch-payload Format")
            //A: register to test
            //B: signed "branch" offset to table data pseudo-instruction (32 bits)
            _decoderPerOpCode[0x2B] = new OpCodeDecoder("31t", "packed-switch", "vAA, +BBBBBBBB", typeof(SwitchInstruction));
            //Jump to a new instruction based on the value in the given register, using an ordered
            // table of value-offset pairs, or fall through to the next instruction if there is no
            // match.
            //(with supplemental data as specified below in "sparse-switch-payload Format")
            // A: register to test
            //B: signed "branch" offset to table data pseudo-instruction (32 bits)
            _decoderPerOpCode[0x2C] = new OpCodeDecoder("31t", "sparse-switch", "vAA, +BBBBBBBB", typeof(SwitchInstruction));
            // Perform the indicated floating point or long comparison, setting a to 0 if b == c,
            // 1 if b > c, or -1 if b < c. The "bias" listed for the floating point operations
            // indicates how NaN comparisons are treated: "gt bias" instructions return 1 for NaN
            // comparisons, and "lt bias" instructions return -1.
            //For example, to check to see if floating point x < y it is advisable to use cmpg-float;
            // a result of -1 indicates that the test was true, and the other values indicate it was
            // false either due to a valid comparison or because one of the values was NaN.
            //A: destination register (8 bits)
            //B: first source register or pair
            //C: second source register or pair
            // (lt bias)
            _decoderPerOpCode[0x2D] = new OpCodeDecoder("23x", "cmpl-float", "vAA, vBB, vCC", typeof(ComparisonInstruction));
            // (gt bias)
            _decoderPerOpCode[0x2E] = new OpCodeDecoder("23x", "cmpg-float", "vAA, vBB, vCC", typeof(ComparisonInstruction));
            // (lt bias)
            _decoderPerOpCode[0x2F] = new OpCodeDecoder("23x", "cmpl-double", "vAA, vBB, vCC", typeof(ComparisonInstruction));
            // (gt bias)
            _decoderPerOpCode[0x30] = new OpCodeDecoder("23x", "cmpg-double", "vAA, vBB, vCC", typeof(ComparisonInstruction));
            _decoderPerOpCode[0x31] = new OpCodeDecoder("23x", "cmp-long", "vAA, vBB, vCC", typeof(ComparisonInstruction));
            // Branch to the given destination if the given two registers' values compare as specified.
            //Note: The branch offset must not be 0. (A spin loop may be legally constructed either
            // by branching around a backward goto or by including a nop as a target before the branch.)
            //A: first register to test (4 bits)
            //B: second register to test (4 bits)
            //C: signed branch offset (16 bits)
            _decoderPerOpCode[0x32] = new OpCodeDecoder("22t", "if-eq", "vA, vB, +CCCC", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x33] = new OpCodeDecoder("22t", "if-ne", "vA, vB, +CCCC", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x34] = new OpCodeDecoder("22t", "if-lt", "vA, vB, +CCCC", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x35] = new OpCodeDecoder("22t", "if-ge", "vA, vB, +CCCC", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x36] = new OpCodeDecoder("22t", "if-gt", "vA, vB, +CCCC", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x37] = new OpCodeDecoder("22t", "if-le", "vA, vB, +CCCC", typeof(ConditionalBranchInstruction));
            // Branch to the given destination if the given register's value compares with 0 as specified.
            //Note: The branch offset must not be 0. (A spin loop may be legally constructed either by
            // branching around a backward goto or by including a nop as a target before the branch.)
            //A: register to test (8 bits)
            //B: signed branch offset (16 bits)
            _decoderPerOpCode[0x38] = new OpCodeDecoder("21t", "if-eqz", "vAA, +BBBB", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x39] = new OpCodeDecoder("21t", "if-nez", "vAA, +BBBB", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x3A] = new OpCodeDecoder("21t", "if-ltz", "vAA, +BBBB", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x3B] = new OpCodeDecoder("21t", "if-gez", "vAA, +BBBB", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x3C] = new OpCodeDecoder("21t", "if-gtz", "vAA, +BBBB", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x3D] = new OpCodeDecoder("21t", "if-lez", "vAA, +BBBB", typeof(ConditionalBranchInstruction));
            _decoderPerOpCode[0x3E] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0x3F] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0x40] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0x41] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0x42] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0x43] = new OpCodeDecoder("10x", null, null, null);
            // Perform the identified array operation at the identified index of the given array,
            // loading or storing into the value register.
            // A: value register or pair; may be source or dest (8 bits)
            //B: array register (8 bits)
            //C: index register (8 bits)
            _decoderPerOpCode[0x44] = new OpCodeDecoder("23x", "aget", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x45] = new OpCodeDecoder("23x", "aget-wide", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x46] = new OpCodeDecoder("23x", "aget-object", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x47] = new OpCodeDecoder("23x", "aget-boolean", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x48] = new OpCodeDecoder("23x", "aget-byte", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x49] = new OpCodeDecoder("23x", "aget-char", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x4A] = new OpCodeDecoder("23x", "aget-short", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x4B] = new OpCodeDecoder("23x", "aput", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x4C] = new OpCodeDecoder("23x", "aput-wide", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x4D] = new OpCodeDecoder("23x", "aput-object", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x4E] = new OpCodeDecoder("23x", "aput-boolean", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x4F] = new OpCodeDecoder("23x", "aput-byte", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x50] = new OpCodeDecoder("23x", "aput-char", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            _decoderPerOpCode[0x51] = new OpCodeDecoder("23x", "aput-short", "vAA, vBB, vCC", typeof(ArrayOperationInstruction));
            // Perform the identified object instance field operation with the identified field,
            // loading or storing into the value register.
            //Note: These opcodes are reasonable candidates for static linking, altering the field
            // argument to be a more direct offset.
            //A: value register or pair; may be source or dest (4 bits)
            //B: object register (4 bits)
            //C: instance field reference index (16 bits)
            _decoderPerOpCode[0x52] = new OpCodeDecoder("22c", "iget", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x53] = new OpCodeDecoder("22c", "iget-wide", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x54] = new OpCodeDecoder("22c", "iget-object", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x55] = new OpCodeDecoder("22c", "iget-boolean", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x56] = new OpCodeDecoder("22c", "iget-byte", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x57] = new OpCodeDecoder("22c", "iget-char", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x58] = new OpCodeDecoder("22c", "iget-short", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x59] = new OpCodeDecoder("22c", "iput", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x5A] = new OpCodeDecoder("22c", "iput-wide", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x5B] = new OpCodeDecoder("22c", "iput-object", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x5C] = new OpCodeDecoder("22c", "iput-boolean", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x5D] = new OpCodeDecoder("22c", "iput-byte", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x5E] = new OpCodeDecoder("22c", "iput-char", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            _decoderPerOpCode[0x5F] = new OpCodeDecoder("22c", "iput-short", "vA, vB, field@CCCC", typeof(InstanceFieldInstruction));
            // Perform the identified object static field operation with the identified static field,
            // loading or storing into the value register.
            //Note: These opcodes are reasonable candidates for static linking, altering the field
            // argument to be a more direct offset.
            // A: value register or pair; may be source or dest (8 bits)
            //B: static field reference index (16 bits)
            _decoderPerOpCode[0x60] = new OpCodeDecoder("21c", "sget", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x61] = new OpCodeDecoder("21c", "sget-wide", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x62] = new OpCodeDecoder("21c", "sget-object", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x63] = new OpCodeDecoder("21c", "sget-boolean", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x64] = new OpCodeDecoder("21c", "sget-byte", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x65] = new OpCodeDecoder("21c", "sget-char", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x66] = new OpCodeDecoder("21c", "sget-short", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x67] = new OpCodeDecoder("21c", "sput", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x68] = new OpCodeDecoder("21c", "sput-wide", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x69] = new OpCodeDecoder("21c", "sput-object", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x6A] = new OpCodeDecoder("21c", "sput-boolean", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x6B] = new OpCodeDecoder("21c", "sput-byte", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x6C] = new OpCodeDecoder("21c", "sput-char", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            _decoderPerOpCode[0x6D] = new OpCodeDecoder("21c", "sput-short", "vAA, field@BBBB", typeof(StaticFieldInstruction));
            // Call the indicated method. The result (if any) may be stored with an appropriate
            // move-result* variant as the immediately subsequent instruction.
            //Note: These opcodes are reasonable candidates for static linking, altering the method
            // argument to be a more direct offset (or pair thereof).
            //A: argument word count (4 bits)
            //B: method reference index (16 bits)
            //C..G: argument registers (4 bits each)
            //invoke-virtual is used to invoke a normal virtual method (a method that is not private,
            // static, or final, and is also not a constructor).
            _decoderPerOpCode[0x6E] = new OpCodeDecoder("35c", "invoke-virtual", "{vC, vD, vE, vF, vG}, meth@BBBB", typeof(MethodCallInstruction));
            //invoke-super is used to invoke the closest superclass's virtual method (as opposed to
            // the one with the same method_id in the calling class). The same method restrictions
            // hold as for invoke-virtual.
            _decoderPerOpCode[0x6F] = new OpCodeDecoder("35c", "invoke-super", "{vC, vD, vE, vF, vG}, meth@BBBB", typeof(MethodCallInstruction));
            //invoke-direct is used to invoke a non-static direct method (that is, an instance method
            // that is by its nature non-overridable, namely either a private instance method or a 
            // constructor).
            _decoderPerOpCode[0x70] = new OpCodeDecoder("35c", "invoke-direct", "{vC, vD, vE, vF, vG}, meth@BBBB", typeof(MethodCallInstruction));
            //invoke-static is used to invoke a static method (which is always considered a direct
            // method).
            _decoderPerOpCode[0x71] = new OpCodeDecoder("35c", "invoke-static", "{vC, vD, vE, vF, vG}, meth@BBBB", typeof(MethodCallInstruction));
            //invoke-interface is used to invoke an interface method, that is, on an object whose
            // concreteclass isn't known, using a method_id that refers to an interface.
            _decoderPerOpCode[0x72] = new OpCodeDecoder("35c", "invoke-interface", "{vC, vD, vE, vF, vG}, meth@BBBB", typeof(MethodCallInstruction));
            _decoderPerOpCode[0x73] = new OpCodeDecoder("10x", null, null, null);
            // Call the indicated method. See first invoke-kind description above for details,
            // caveats, and suggestions.
            // A: argument word count (8 bits)
            //B: method reference index (16 bits)
            //C: first argument register (16 bits)
            //N = A + C - 1
            _decoderPerOpCode[0x74] = new OpCodeDecoder("3rc", "invoke-virtual/range", "{vCCCC .. vNNNN}, meth@BBBB", typeof(MethodCallInstruction));
            _decoderPerOpCode[0x75] = new OpCodeDecoder("3rc", "invoke-super/range", "{vCCCC .. vNNNN}, meth@BBBB", typeof(MethodCallInstruction));
            _decoderPerOpCode[0x76] = new OpCodeDecoder("3rc", "invoke-direct/range", "{vCCCC .. vNNNN}, meth@BBBB", typeof(MethodCallInstruction));
            _decoderPerOpCode[0x77] = new OpCodeDecoder("3rc", "invoke-static/range", "{vCCCC .. vNNNN}, meth@BBBB", typeof(MethodCallInstruction));
            _decoderPerOpCode[0x78] = new OpCodeDecoder("3rc", "invoke-interface/range", "{vCCCC .. vNNNN}, meth@BBBB", typeof(MethodCallInstruction));
            _decoderPerOpCode[0x79] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0x7A] = new OpCodeDecoder("10x", null, null, null);
            // Perform the identified unary operation on the source register, storing the result
            // in the destination register.
            // A: destination register or pair (4 bits)
            //B: source register or pair (4 bits)
            _decoderPerOpCode[0x7B] = new OpCodeDecoder("12x", "neg-int", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x7C] = new OpCodeDecoder("12x", "not-int", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x7D] = new OpCodeDecoder("12x", "neg-long", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x7E] = new OpCodeDecoder("12x", "not-long", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x7F] = new OpCodeDecoder("12x", "neg-float", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x80] = new OpCodeDecoder("12x", "neg-double", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x81] = new OpCodeDecoder("12x", "int-to-long", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x82] = new OpCodeDecoder("12x", "int-to-float", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x83] = new OpCodeDecoder("12x", "int-to-double", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x84] = new OpCodeDecoder("12x", "long-to-int", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x85] = new OpCodeDecoder("12x", "long-to-float", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x86] = new OpCodeDecoder("12x", "long-to-double", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x87] = new OpCodeDecoder("12x", "float-to-int", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x88] = new OpCodeDecoder("12x", "float-to-long", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x89] = new OpCodeDecoder("12x", "float-to-double", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x8A] = new OpCodeDecoder("12x", "double-to-int", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x8B] = new OpCodeDecoder("12x", "double-to-long", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x8C] = new OpCodeDecoder("12x", "double-to-float", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x8D] = new OpCodeDecoder("12x", "int-to-byte", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x8E] = new OpCodeDecoder("12x", "int-to-char", "vA, vB", typeof(UnaryOperationInstruction));
            _decoderPerOpCode[0x8F] = new OpCodeDecoder("12x", "int-to-short", "vA, vB", typeof(UnaryOperationInstruction));
            // Perform the identified binary operation on the two source registers, storing the
            // result in the first source register.
            // A: destination register or pair (8 bits)
            //B: first source register or pair (8 bits)
            //C: second source register or pair (8 bits)
            _decoderPerOpCode[0x90] = new OpCodeDecoder("23x", "add-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x91] = new OpCodeDecoder("23x", "sub-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x92] = new OpCodeDecoder("23x", "mul-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x93] = new OpCodeDecoder("23x", "div-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x94] = new OpCodeDecoder("23x", "rem-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x95] = new OpCodeDecoder("23x", "and-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x96] = new OpCodeDecoder("23x", "or-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x97] = new OpCodeDecoder("23x", "xor-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x98] = new OpCodeDecoder("23x", "shl-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x99] = new OpCodeDecoder("23x", "shr-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x9A] = new OpCodeDecoder("23x", "ushr-int", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x9B] = new OpCodeDecoder("23x", "add-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x9C] = new OpCodeDecoder("23x", "sub-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x9D] = new OpCodeDecoder("23x", "mul-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x9E] = new OpCodeDecoder("23x", "div-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0x9F] = new OpCodeDecoder("23x", "rem-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA0] = new OpCodeDecoder("23x", "and-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA1] = new OpCodeDecoder("23x", "or-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA2] = new OpCodeDecoder("23x", "xor-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA3] = new OpCodeDecoder("23x", "shl-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA4] = new OpCodeDecoder("23x", "shr-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA5] = new OpCodeDecoder("23x", "ushr-long", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA6] = new OpCodeDecoder("23x", "add-float", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA7] = new OpCodeDecoder("23x", "sub-float", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA8] = new OpCodeDecoder("23x", "mul-float", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xA9] = new OpCodeDecoder("23x", "div-float", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xAA] = new OpCodeDecoder("23x", "rem-float", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xAB] = new OpCodeDecoder("23x", "add-double", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xAC] = new OpCodeDecoder("23x", "sub-double", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xAD] = new OpCodeDecoder("23x", "mul-double", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xAE] = new OpCodeDecoder("23x", "div-double", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xAF] = new OpCodeDecoder("23x", "rem-double", "vAA, vBB, vCC", typeof(BinaryOperationInstruction));
            // Perform the identified binary operation on the two source registers, storing the
            // result in the first source register.
            // A: destination and first source register or pair (4 bits)
            //B: second source register or pair (4 bits)
            _decoderPerOpCode[0xB0] = new OpCodeDecoder("12x", "add-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB1] = new OpCodeDecoder("12x", "sub-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB2] = new OpCodeDecoder("12x", "mul-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB3] = new OpCodeDecoder("12x", "div-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB4] = new OpCodeDecoder("12x", "rem-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB5] = new OpCodeDecoder("12x", "and-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB6] = new OpCodeDecoder("12x", "or-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB7] = new OpCodeDecoder("12x", "xor-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB8] = new OpCodeDecoder("12x", "shl-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xB9] = new OpCodeDecoder("12x", "shr-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xBA] = new OpCodeDecoder("12x", "ushr-int/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xBB] = new OpCodeDecoder("12x", "add-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xBC] = new OpCodeDecoder("12x", "sub-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xBD] = new OpCodeDecoder("12x", "mul-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xBE] = new OpCodeDecoder("12x", "div-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xBF] = new OpCodeDecoder("12x", "rem-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC0] = new OpCodeDecoder("12x", "and-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC1] = new OpCodeDecoder("12x", "or-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC2] = new OpCodeDecoder("12x", "xor-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC3] = new OpCodeDecoder("12x", "shl-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC4] = new OpCodeDecoder("12x", "shr-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC5] = new OpCodeDecoder("12x", "ushr-long/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC6] = new OpCodeDecoder("12x", "add-float/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC7] = new OpCodeDecoder("12x", "sub-float/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC8] = new OpCodeDecoder("12x", "mul-float/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xC9] = new OpCodeDecoder("12x", "div-float/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xCA] = new OpCodeDecoder("12x", "rem-float/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xCB] = new OpCodeDecoder("12x", "add-double/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xCC] = new OpCodeDecoder("12x", "sub-double/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xCD] = new OpCodeDecoder("12x", "mul-double/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xCE] = new OpCodeDecoder("12x", "div-double/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xCF] = new OpCodeDecoder("12x", "rem-double/2addr", "vA, vB", typeof(BinaryOperationInstruction));
            //Perform the indicated binary op on the indicated register (first argument) and
            // literal value (second argument), storing the result in the destination register.
            //A: destination register (4 bits)
            //B: source register (4 bits)
            //C: signed int constant (16 bits)
            _decoderPerOpCode[0xD0] = new OpCodeDecoder("22s", "add-int/lit16", "vA, vB, #+CCCC", typeof(BinaryOperationInstruction));
            // (reverse subtract)
            //Note: rsub-int does not have a suffix since this version is the main opcode of
            // its family. Also, see below for details on its semantics.
            _decoderPerOpCode[0xD1] = new OpCodeDecoder("22s", "rsub-int", "vA, vB, #+CCCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xD2] = new OpCodeDecoder("22s", "mul-int/lit16", "vA, vB, #+CCCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xD3] = new OpCodeDecoder("22s", "div-int/lit16", "vA, vB, #+CCCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xD4] = new OpCodeDecoder("22s", "rem-int/lit16", "vA, vB, #+CCCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xD5] = new OpCodeDecoder("22s", "and-int/lit16", "vA, vB, #+CCCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xD6] = new OpCodeDecoder("22s", "or-int/lit16", "vA, vB, #+CCCC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xD7] = new OpCodeDecoder("22s", "xor-int/lit16", "vA, vB, #+CCCC", typeof(BinaryOperationInstruction));
            //Perform the indicated binary op on the indicated register (first argument) and
            // literal value (second argument), storing the result in the destination register.
            //A: destination register (8 bits)
            //B: source register (8 bits)
            //C: signed int constant (8 bits)
            _decoderPerOpCode[0xD8] = new OpCodeDecoder("22b", "add-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xD9] = new OpCodeDecoder("22b", "rsub-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xDA] = new OpCodeDecoder("22b", "mul-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xDB] = new OpCodeDecoder("22b", "div-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xDC] = new OpCodeDecoder("22b", "rem-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xDD] = new OpCodeDecoder("22b", "and-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xDE] = new OpCodeDecoder("22b", "or-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xDF] = new OpCodeDecoder("22b", "xor-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xE0] = new OpCodeDecoder("22b", "shl-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xE1] = new OpCodeDecoder("22b", "shr-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xE2] = new OpCodeDecoder("22b", "ushr-int/lit8", "vAA, vBB, #+CC", typeof(BinaryOperationInstruction));
            _decoderPerOpCode[0xE3] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xE4] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xE5] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xE6] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xE7] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xE8] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xE9] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xEA] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xEB] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xEC] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xED] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xEE] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xEF] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF0] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF1] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF2] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF3] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF4] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF5] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF6] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF7] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF8] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xF9] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xFA] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xFB] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xFC] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xFD] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xFE] = new OpCodeDecoder("10x", null, null, null);
            _decoderPerOpCode[0xFF] = new OpCodeDecoder("10x", null, null, null);
        }
    }
}
