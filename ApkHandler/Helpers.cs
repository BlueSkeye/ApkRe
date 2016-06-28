using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkHandler
{
    public static class Helpers
    {
        #region METHODS
        internal static void Align(BinaryReader reader, int alignement)
        {
            int overshoot = ((int)reader.BaseStream.Position % alignement);

            if (0 == overshoot) { return; }
            long newPosition = reader.BaseStream.Position - overshoot + alignement;
            if (newPosition >= reader.BaseStream.Length) {
                throw new Dex.ParseException();
            }
            reader.BaseStream.Position = newPosition;
            return;
        }

        /// <summary>Check whether two array of bytes are equal. The condition is considered
        /// to be met if none of them is a null reference, and both have the same size, and
        /// they are equal byte by byte at any array index.</summary>
        /// <param name="x">First array</param>
        /// <param name="y">Second array</param>
        /// <returns>true whenever the above condition is met,false otherwise.</returns>
        internal static bool AreEqual(byte[] x, byte[] y)
        {
            if ((null == x) || (null == y)) { return false; }
            if (x.Length != y.Length) { return false; }
            for (int index = 0; index < x.Length; index++) {
                if (x[index] != y[index]) { return false; }
            }
            return true;
        }

        /// <summary>Sometimes we use streams that are unexpectedly closed by methods we
        /// are invoking such as by the <see cref="CryptoStream"/>. This method creates a
        /// special kind of <see cref="Stream"/> that will shield an existing <see cref="Stream"/>
        /// from being closed in such situation.</summary>
        /// <param name="from">The stream to be isolated.</param>
        /// <returns>The isolating stream.</returns>
        internal static Stream CreateIsolatingStream(Stream from)
        {
            return new IsolatingStream(from);
        }

        internal static string DecodeString(BinaryReader reader)
        {
            // utf16_size 	uleb128 	size of this string, in UTF-16 code units (which is
            //          the "string length" in many systems). That is, this is the decoded
            //          length of the string. (The encoded length is implied by the position
            //          of the 0 byte.)
            uint utf16Size = ReadULEB128(reader);
            // data 	ubyte[] 	a series of MUTF-8 code units (a.k.a. octets, a.k.a. bytes)
            //          followed by a byte of value 0. See "MUTF-8 (Modified UTF-8) Encoding"
            //          above for details and discussion about the data format.

            // Note: It is acceptable to have a string which includes (the encoded form of)
            // UTF-16 surrogate code units (that is, U+d800 … U+dfff) either in isolation or
            // out-of-order with respect to the usual encoding of Unicode into UTF-16. It is
            // up to higher-level uses of strings to reject such invalid encodings, if
            // appropriate.

            // As a concession to easier legacy support, the .dex format encodes its string
            // data in a de facto standard modified UTF-8 form, hereafter referred to as MUTF-8.
            // This form is identical to standard UTF-8, except:
            // 1°) Only the one-, two-, and three-byte encodings are used.
            // 2°) Code points in the range U+10000 … U+10ffff are encoded as a surrogate pair,
            //      each of which is represented as a three-byte encoded value.
            // 3°) The code point U+0000 is encoded in two-byte form.
            // 4°) A plain null byte (value 0) indicates the end of a string, as is the standard
            //      C language interpretation.
            // The first two items above can be summarized as: MUTF-8 is an encoding format for
            // UTF-16, instead of being a more direct encoding format for Unicode characters.
            // The final two items above make it simultaneously possible to include the code
            // point U+0000 in a string and still manipulate it as a C-style null-terminated
            // string.
            // However, the special encoding of U+0000 means that, unlike normal UTF-8, the result
            // of calling the standard C function strcmp() on a pair of MUTF-8 strings does not
            // always indicate the properly signed result of comparison of unequal strings. When
            // ordering (not just equality) is a concern, the most straightforward way to compare
            // MUTF-8 strings is to decode them character by character, and compare the decoded
            // values. (However, more clever implementations are also possible.)
            // Please refer to The Unicode Standard for further information about character encoding.
            // MUTF-8 is actually closer to the (relatively less well-known) encoding CESU-8 than
            // to UTF-8 per se.
            List<byte> mutf8Bytes = new List<byte>();
            while (true) {
                byte scannedByte = reader.ReadByte();

                if (0 == scannedByte) { break; }
                if (0 == (0x80 & scannedByte)) {
                    mutf8Bytes.Add(scannedByte);
                    continue;
                }
                int codePointBytesCount;
                if (0xC0 == (scannedByte & 0xE0)) { codePointBytesCount = 2; }
                else if (0xE0 == (scannedByte & 0xF0)) { codePointBytesCount = 3; }
                else { throw new Dex.ParseException(); }
                while (true) {
                    mutf8Bytes.Add(scannedByte);
                    if (0 >= --codePointBytesCount) { break; }
                    scannedByte = reader.ReadByte();
                    if (0x80 != (scannedByte & 0xC0)) { throw new Dex.ParseException(); }
                }
            }
            return UTF8Encoding.UTF8.GetString(mutf8Bytes.ToArray());
        }

        /// <summary>Make sure all directories within the relative file path exist under the
        /// given base directory. Create missing directories.</summary>
        /// <param name="baseDirectory">Base directory to start scan from.</param>
        /// <param name="relativeFilePath">The file path relative to base directory.</param>
        /// <returns>The full patch of the target file.</returns>
        internal static string EnsurePath(DirectoryInfo baseDirectory, string relativeFilePath)
        {
            string[] filenameItems = relativeFilePath.Split('/');
            string scannedPath = baseDirectory.FullName;
            for (int index = 0; index < filenameItems.Length - 1; index++) {
                scannedPath = Path.Combine(scannedPath, filenameItems[index]);
                if (!Directory.Exists(scannedPath)) { Directory.CreateDirectory(scannedPath); }
            }
            return Path.Combine(scannedPath, filenameItems[filenameItems.Length - 1]);
        }

        internal static bool IsValidClassName(string candidate, bool arrayAllowed)
        {
            if (arrayAllowed) {
                int dimensionsCount = 0;
                while (candidate.StartsWith("[")) {
                    dimensionsCount++;
                    if (1 == candidate.Length) { return false; }
                    candidate = candidate.Substring(1);
                }
                if (255 < dimensionsCount) { return false; }
            }
            if (!candidate.StartsWith("L")) { return false; }
            if (2 >= candidate.Length) { return false; }
            if (!candidate.EndsWith(";")) { return false; }
            return IsValidFullClassName(candidate.Substring(1, candidate.Length - 2));
        }

        /// <summary></summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        /// <remarks>A FullClassName is a fully-qualified class name,
        /// including an optional package specifier followed by a required
        /// name.
        /// FullClassName →
        ///     OptionalPackagePrefix SimpleName
        /// OptionalPackagePrefix →
        ///     (SimpleName '/')*
        /// </remarks>
        internal static bool IsValidFullClassName(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) { return false; }
            while(true) {
                int nextSplitIndex = candidate.IndexOf('/');
                if (-1 == nextSplitIndex) { break; }
                if (0 == nextSplitIndex) { return false; }
                if (nextSplitIndex == (candidate.Length - 1)) { return false; }
                if (!IsValidSimpleName(candidate.Substring(0, nextSplitIndex))) {
                    return false;
                }
                candidate = candidate.Substring(nextSplitIndex + 1);
            }
            return IsValidSimpleName(candidate);
        }

        /// <summary></summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        /// <remarks>
        /// A MemberName is the name of a member of a class, members being fields,
        /// methods, and inner classes.
        /// MemberName →
        ///     SimpleName
        ///     | 	'<' SimpleName '>'
        /// </remarks>
        internal static bool IsValidMemberName(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) { return false; }
            if ('<' == candidate[0]) {
                if ((2 >= candidate.Length)
                    || ('>' != candidate[candidate.Length - 1]))
                {
                    return false;
                }
                candidate = candidate.Substring(1, candidate.Length - 2);
            }
            return IsValidSimpleName(candidate);
        }

        /// <summary></summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        /// <remarks>A ShortyDescriptor is the short form representation of a method
        /// prototype, including return and parameter types, except that there is no
        /// distinction between various reference (class or array) types. Instead,
        /// all reference types are represented by a single 'L' character.
        /// ShortyDescriptor →
        ///     ShortyReturnType (ShortyFieldType)*
        /// ShortyReturnType →
        ///     'V'
        ///     | 	ShortyFieldType
        /// ShortyFieldType →
        ///     'Z'
        ///     | 	'B'
        ///     | 	'S'
        ///     | 	'C'
        ///     | 	'I'
        ///     | 	'J'
        ///     | 	'F'
        ///     | 	'D'
        ///     | 	'L'
        /// </remarks>
        internal static bool IsValidShortDescriptor(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) { return false; }
            for(int index = 0; index < candidate.Length; index++) {
                if (IsValidShortFieldType(candidate[index])) { continue; }
                if (0 < index) { return false; }
                if ('V' != candidate[index]) { return false; }
            }
            return true;
        }

        internal static bool IsValidShortFieldType(char candidate)
        {
            switch (candidate) {
                case 'Z':
                case 'B':
                case 'S':
                case 'C':
                case 'I':
                case 'J':
                case 'F':
                case 'D':
                case 'L':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary></summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        /// <remarks>SimpleNameA SimpleName is the basis for the syntax
        /// of the names of other things. The .dex format allows a fair
        /// amount of latitude here (much more than most common source
        /// languages). In brief, a simple name consists of any low-ASCII
        /// alphabetic character or digit, a few specific low-ASCII symbols,
        /// and most non-ASCII code points that are not control, space, or
        /// special characters. Note that surrogate code points (in the
        /// range U+d800 … U+dfff) are not considered valid name characters,
        /// per se, but Unicode supplemental characters are valid (which are
        /// represented by the final alternative of the rule for SimpleNameChar),
        /// and they should be represented in a file as pairs of surrogate code
        /// points in the MUTF-8 encoding.
        /// SimpleName →
        ///     SimpleNameChar (SimpleNameChar)*
        /// SimpleNameChar →
        ///     'A' … 'Z'
        ///     | 	'a' … 'z'
        ///     | 	'0' … '9'
        ///     | 	'$'
        ///     | 	'-'
        ///     | 	'_'
        ///     | 	U+00a1 … U+1fff
        ///     | 	U+2010 … U+2027
        ///     | 	U+2030 … U+d7ff
        ///     | 	U+e000 … U+ffef
        ///     | 	U+10000 … U+10ffff
        /// </remarks>
        internal static bool IsValidSimpleName(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) { return false; }
            for(int index = 0; index < candidate.Length; index++) {
                int utf32Value = char.ConvertToUtf32(candidate, index);

                if (0xA0 < utf32Value) {
                    if ((0xA1 <= utf32Value) && (0x1FFF >= utf32Value)) { continue; }
                    if ((0x2010 <= utf32Value) && (0x2027 >= utf32Value)) { continue; }
                    if ((0x2030 <= utf32Value) && (0xD7FF >= utf32Value)) { continue; }
                    if ((0xE000 <= utf32Value) && (0xFFEF >= utf32Value)) { continue; }
                    if ((0x10000 <= utf32Value) && (0x10FFFF >= utf32Value)) { continue; }
                    return false;
                }
                else {
                    char scannedCharacter = candidate[index];
                    if (char.IsLetterOrDigit(scannedCharacter)) { continue; }
                    switch (scannedCharacter)
                    {
                        case '$':
                        case '-':
                        case '_':
                            continue;
                        default:
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary></summary>
        /// <param name="candidate"></param>
        /// <returns></returns>
        /// <remarks>A TypeDescriptor is the representation of any type,
        /// including primitives, classes, arrays, and void. See below
        /// for the meaning of the various versions.
        /// TypeDescriptor →
        ///     'V'
        ///     | 	FieldTypeDescriptor
        /// FieldTypeDescriptor →
        ///     NonArrayFieldTypeDescriptor
        ///     | 	('[' * 1…255) NonArrayFieldTypeDescriptor
        /// NonArrayFieldTypeDescriptor→
        ///         'Z'
        ///     | 	'B'
        ///     | 	'S'
        ///     | 	'C'
        ///     | 	'I'
        ///     | 	'J'
        ///     | 	'F'
        ///     | 	'D'
        ///     | 	'L' FullClassName ';'
        /// </remarks>
        internal static bool IsValidTypeDescriptor(string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) { return false; }
            // Special case "void" descriptor used as a return type
            if ("V" == candidate) { return true; }
            int indexesCount = 0;
            while (candidate.StartsWith("[")) {
                indexesCount++;
                if (1 == candidate.Length) { return false; }
                candidate = candidate.Substring(1);
            }
            if (255 < indexesCount) { return false; }
            // NOTA : Array dimensions already handled.
            if (candidate.StartsWith("L")) { return IsValidClassName(candidate, false); }
            switch (candidate) {
                case "Z":
                case "B":
                case "S":
                case "C":
                case "I":
                case "J":
                case "F":
                case "D":
                    return true;
                default:
                    return false;
            }
        }
        
        internal static ushort PeekUInt16(byte[] from, int offset)
        {
            return ReadUInt16(from, ref offset);
        }

        internal static uint PeekUInt32(byte[] from, int offset)
        {
            return ReadUInt32(from, ref offset);
        }
        
        internal static ushort ReadUInt16(byte[] from, ref int offset)
        {
            return (ushort)(from[offset++] + (256 * from[offset++]));
        }
        
        internal static ushort ReadUInt16(FileStream from)
        {
            byte[] buffer = new byte[2];
            
            if (buffer.Length != from.Read(buffer, 0, buffer.Length)) {
                throw new ApplicationException("Not enough data");
            }
            return (ushort)(buffer[0] + (256 * buffer[1]));
        }
        
        internal static uint ReadUInt32(byte[] from, ref int offset)
        {
            return (uint)(from[offset++] + (256 * from[offset++]) +
                (65536 * from[offset++]) + (16777216 * from[offset++]));
        }

        // Each LEB128 encoded value consists of one to five bytes, which together represent
        // a single 32-bit value. Each byte has its most significant bit set except for the
        // final byte in the sequence, which has its most significant bit clear. The remaining
        // seven bits of each byte are payload, with the least significant seven bits of the
        // quantity in the first byte, the next seven in the second byte and so on. In the
        // case of a signed LEB128 (sleb128), the most significant payload bit of the final
        // byte in the sequence is sign-extended to produce the final value. In the unsigned
        // case (uleb128), any bits not explicitly represented are interpreted as 0.

        //Bitwise diagram of a two-byte LEB128 value
        //First byte 	Second byte
        //1 	bit6 	bit5 	bit4 	bit3 	bit2 	bit1 	bit0 	0 	bit13 	bit12 	bit11 	bit10 	bit9 	bit8 	bit7

        //The variant uleb128p1 is used to represent a signed value, where the representation is of the value plus one encoded as a uleb128. This makes the encoding of -1 (alternatively thought of as the unsigned value 0xffffffff) — but no other negative number — a single byte, and is useful in exactly those cases where the represented number must either be non-negative or -1 (or 0xffffffff), and where no other negative values are allowed (or where large unsigned values are unlikely to be needed).

        //Here are some examples of the formats:
        //Encoded Sequence 	As sleb128 	As uleb128 	As uleb128p1
        //00	                0	        0	        -1
        //01	                1	        1	         0
        //7f	                -1	        127	        126
        //80 7f                 -128	    16256	    16255
        internal static int ReadSLEB128(BinaryReader reader)
        {
            uint result = 0;
            uint scannedByte;
            int shift = 0;
            int signBit = 0;

            do
            {
                scannedByte = reader.ReadByte();
                result |= ((scannedByte & 0x7F) << shift);
                signBit = (int)((scannedByte & 0x40) >> 6);
                shift += 7;
            } while (0 != (0x80 & scannedByte));
            return (0 == signBit)
                ? (int)result
                : (int)(((uint)0xFFFFFFFF << shift) | result);
        }

        internal static uint ReadULEB128(BinaryReader reader)
        {
            uint result = 0;
            uint scannedByte;
            int shift = 0;

            do {
                scannedByte = reader.ReadByte();
                result |= ((scannedByte & 0x7F) << shift);
                shift += 7;
            } while (0 != (0x80 & scannedByte));
            return result;
        }

        internal static long ReadULEB128P1(BinaryReader reader)
        {
            return ReadULEB128(reader) - 1;
        }

        /// <summary>Copy <paramref name="from"/> content to <paramref name="to"/>
        /// stream starting at current location of <paramref name="from"/> stream
        /// until the end of the <paramref name="from"/> stream is reached. On
        /// return the original position of the <paramref name="from"/> stream is
        /// restored.</summary>
        /// <param name="from">Originating stream.</param>
        /// <param name="to">Destination stream.</param>
        /// <param name="count">Number of bytes to copy from the input stream.
        /// A -1 value implies copying the whole input stream.</param>
        /// <param name="preservePosition">true if position should be preserved on
        /// method return.</param>
        internal static void StreamCopy(Stream from, Stream to, int count = -1,
            bool preservePosition = true, byte xorWith = 0x00)
        {
            long initialPosition = preservePosition ? from.Position : 0;
            
            if (-1 == count) { count = (int)(from.Length - from.Position); }
            try {
                byte[] localBuffer = new byte[8192];
                while (0 < count) {
                    int readSize = Math.Min(localBuffer.Length, count);
                    
                    if (readSize != from.Read(localBuffer, 0, readSize)) {
                        throw new ApplicationException("File copy size error.");
                    }
                    if (0x00 != xorWith) {
                        for(int index = 0; index < readSize; index++) {
                            localBuffer[index] ^= xorWith;
                        }
                    }
                    to.Write(localBuffer, 0, readSize);
                    count -= readSize;
                 }
            }
            finally { if (preservePosition) { from.Position = initialPosition; } }
        }
        #endregion

        private class IsolatingStream : Stream
        {
            #region CONSTRUCTORS
            internal IsolatingStream(Stream wrapped)
            {
                _wrapped = wrapped;
                return;
            }
            #endregion

            #region PROPERTIES
            public override bool CanRead
            {
                get { return _wrapped.CanRead; }
            }

            public override bool CanSeek
            {
                get { return _wrapped.CanSeek; }
            }

            public override bool CanWrite
            {
                get { return _wrapped.CanWrite; }
            }

            public override long Length
            {
                get { return _wrapped.Length; }
            }

            public override long Position
            {
                get { return _wrapped.Position; }
                set { _wrapped.Position = value; }
            }
            #endregion

            #region METHODS
            public override void Close()
            {
                Dispose(true);
                return;
            }

            protected override void Dispose(bool disposing)
            {
                _wrapped = null;
                _closed = true;
                base.Dispose(disposing);
                return;
            }

            public override void Flush()
            {
                _wrapped.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return _wrapped.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                _wrapped.SetLength(value);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return _wrapped.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                _wrapped.Write(buffer, offset, count);
            }
            #endregion

            #region FIELDS
            private bool _closed;
            private Stream _wrapped;
            #endregion
        }
    }
}
