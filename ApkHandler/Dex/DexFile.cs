using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using com.rackham.ApkJava;
using com.rackham.ApkJava.API;
using com.rackham.ApkHandler.API;

namespace com.rackham.ApkHandler.Dex
{
    public class DexFile : IResolver
    {
        static DexFile()
        {
            ColonSeparatorAsArray = new string[] { "::" };
        }

        #region PROPERTIES
        internal Header Header { get; set; }

        /// <summary>string identifiers list. These are identifiers for all the strings used
        /// by this file, either for internal naming (e.g., type descriptors) or as constant
        /// objects referred to by code. This list must be sorted by string contents, using
        /// UTF-16 code point values (not in a locale-sensitive manner), and it must not
        /// contain any duplicate entries.</summary>
        internal List<string> Strings { get; private set; }

        /// <summary>type identifiers list. These are identifiers for all types (classes, arrays,
        /// or primitive types) referred to by this file, whether defined in the file or not.
        /// This list must be sorted by string_id index, and it must not contain any duplicate
        /// entries.</summary>
        internal List<KnownType> Types { get; private set; }

        /// <summary>method prototype identifiers list. These are identifiers for all prototypes
        /// referred to by this file. This list must be sorted in return-type (by type_id index)
        /// major order, and then by arguments (also by type_id index). The list must not contain
        /// any duplicate entries.</summary>
        internal List<Prototype> Prototypes { get; private set; }

        /// <summary>field identifiers list. These are identifiers for all fields referred to by
        /// this file, whether defined in the file or not. This list must be sorted, where the
        /// defining type (by type_id index) is the major order, field name (by string_id index)
        /// is the intermediate order, and type (by type_id index) is the minor order. The list
        /// must not contain any duplicate entries.</summary>
        internal List<Field> Fields { get; private set; }

        /// <summary>method identifiers list. These are identifiers for all methods referred to
        /// by this file, whether defined in the file or not. This list must be sorted, where the
        /// defining type (by type_id index) is the major order, method name (by string_id index)
        /// is the intermediate order, and method prototype (by proto_id index) is the minor order.
        /// The list must not contain any duplicate entries.</summary>
        internal List<Method> Methods { get; private set; }

        /// <summary>class definitions list. The classes must be ordered such that a given class's
        /// superclass and implemented interfaces appear in the list earlier than the referring class.
        /// Furthermore, it is invalid for a definition for the same-named class to appear more than
        /// once in the list.</summary>
        internal List<ClassDefinition> Classes { get; private set; }

        /// <summary>data area, containing all the support data for the tables listed above. Different
        /// items have different alignment requirements, and padding bytes are inserted before each
        /// item if necessary to achieve proper alignment.</summary>
        internal byte[] Data { get; private set; }

        /// <summary>data used in statically linked files. The format of the data in this section is
        /// left unspecified by this document. This section is empty in unlinked files, and runtime
        /// implementations may use it as they see fit.</summary>
        internal byte[] LinkData { get; private set; }
        #endregion

        #region METHODS
        private static byte[] ComputeSHA1Hash(Stream from)
        {
            SHA1 hasher = SHA1.Create();

            using (CryptoStream hashingStream =
                new CryptoStream(new MemoryStream(), hasher, CryptoStreamMode.Write))
            {
                Helpers.StreamCopy(from, hashingStream);
            }
            return hasher.Hash;
        }

        /// <summary>Provides an enumerable object suitable for use with foreach C#
        /// syntax that will enumerate classes from this file.</summary>
        /// <returns></returns>
        public IEnumerable<IAnnotatableClass> EnumerateClasses()
        {
            foreach (IAnnotatableClass item in Classes) { yield return item; }
            yield break;
        }

        /// <summary></summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <remarks>The name is expeected to be properly decorated with a leading
        /// uppercase L and a trailing dot comma sign.</remarks>
        public IClass FindClass(string name)
        {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentException(); }
            IClass result = null;
            string[] nameItems = name.Split(ColonSeparatorAsArray, StringSplitOptions.None);
            foreach(string nameItem in nameItems) {
                if (string.Empty == nameItem) {
                    throw new ArgumentException();
                }
            }
            foreach(IClass candidate in EnumerateClasses()) {
                IClass scannedClass = candidate;
                int nameItemIndex = nameItems.Length - 1;
                string candidateName = nameItems[nameItemIndex];
                if (scannedClass.Name != candidateName) { continue; }
                if (0 > --nameItemIndex) {
                    if (null != scannedClass.SuperClass) { continue; }
                }
                IClass candidateSuperClass = scannedClass.SuperClass;
                while ((0 <= nameItemIndex) && (null != candidateSuperClass)) {
                    if (candidateSuperClass.Name != nameItems[nameItemIndex]) {
                        break;
                    }
                    nameItemIndex--;
                    candidateSuperClass = candidateSuperClass.SuperClass;
                }
                if ((0 <= nameItemIndex) || (null != candidateSuperClass)) { continue; }
                if (null != result) {
                    throw new InvalidOperationException(
                        string.Format(Messages.AmbiguousTypeName, name));
                }
                result = candidate;
            }
            return result;
        }

        private IMethod FindMethod(string name)
        {
            if (string.IsNullOrEmpty(name)) { throw new ArgumentException(); }
            int lastSplitIndex = name.LastIndexOf("::");
            if (   (0 == lastSplitIndex)
                || (-1 == lastSplitIndex)
                || (name.Length <= (lastSplitIndex + 2)))
            {
                throw new ArgumentException();
            }
            string methodName = name.Substring(lastSplitIndex + 2);
            string className = name.Substring(0, lastSplitIndex);
            IClass owningClass = FindClass(className);
            if (null == owningClass) { return null; }
            return owningClass.FindMethod(name);
        }

        private KnownType FindKnownType(string fullName)
        {
            KnownType result = null;
            foreach (KnownType candidate in this.Types) {
                if (fullName != candidate.FullName) { continue; }
                if (null != result) {
                    throw new InvalidOperationException(
                        string.Format(Messages.AmbiguousTypeName, fullName));
                }
                result = candidate;
            }
            return result;
        }

        /// <summary>Validate given index as being in the <see cref="TypeNames"/>
        /// array bound and check the value has being a valid class name.</summary>
        /// <param name="nameIndex">Index in type names array of the name to be
        /// returned.</param>
        /// <param name="mayBeNull">true if the name index can be equal to 
        /// <see cref="Constants.NoIndex"/>.</param>
        /// <param name="arrayAllowed">Whether arrays are considered valid class
        /// names.</param>
        /// <returns></returns>
        private string GetClassName(uint nameIndex, bool mayBeNull, bool arrayAllowed)
        {
            if (mayBeNull && (Constants.NoIndex == nameIndex)) { return null; }
            if (nameIndex >= this.Types.Count) { throw new ParseException(); }
            string result = this.Types[(int)nameIndex].FullName;
            if (!Helpers.IsValidClassName(result, arrayAllowed)) { throw new ParseException(); }
            return result;
        }

        /// <summary>Retrieve a uint class index at current reader position,
        /// then validate it as being in the <see cref="TypeNames"/> array
        /// bound and check the value has being a valid class name that is :
        /// this must be a class type, and not an array or primitive type.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <remarks>This differs from the <see cref="GetMemberClassNameFromIndex"/>
        /// method by the index size.</remarks>
        private string GetClassNameFromIndex(BinaryReader reader,
            bool mayBeNull = false)
        {
            uint classIndex = reader.ReadUInt32();
            // Array are OK
            return GetClassName(classIndex, mayBeNull, false);
        }

        /// <summary>Retrieve a ULEB128 class index at current reader position,
        /// then validate it as being in the <see cref="TypeNames"/> array
        /// bound and check the value has being a valid class name that is :
        /// This must be a class (not array or primitive) type.</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private string GetClassNameFromULEB128Index(BinaryReader reader)
        {
            uint classIndex = Helpers.ReadULEB128(reader);

            return GetClassName(classIndex, false, false);
        }

        private IClass GetExternalClass(string className)
        {
            IClass result;

            if (!_externalClasses.TryGetValue(className, out result)) {
                result = new ExternalClass(className);
                _externalClasses[className] = result;
            }
            return result;
        }

        private Field GetFieldFromIndex(BinaryReader reader)
        {
            uint fieldNameIndex = reader.ReadUInt32();
            if (fieldNameIndex >= this.Fields.Count) { throw new ParseException(); }
            return this.Fields[(int)fieldNameIndex];
        }

        /// <summary>Read a string index from current reader position, assert the
        /// index as being in the strings pool range and return the associated
        /// string.</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private string GetIndexedString(BinaryReader reader, bool mayBeNull = false)
        {
            uint stringIndex = reader.ReadUInt32();
            if (mayBeNull && (Constants.NoIndex == stringIndex)) { return null; }
            if (this.Strings.Count <= stringIndex) { throw new ParseException(); }
            return this.Strings[(int)stringIndex];
        }

        /// <summary>Read an ushort index from the current reader position,
        /// then seek for this entry in the Types collection and return the
        /// associated full name.</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal string GetKnownTypeName(BinaryReader reader, bool shortIndex)
        {
            uint typeIndex = (shortIndex) ? reader.ReadUInt16() : reader.ReadUInt32();
            if (typeIndex >= this.Types.Count) { throw new ParseException(); }
            return this.Types[(int)typeIndex].FullName;
        }

        /// <summary>Retrieve a ushort class index at current reader position,
        /// then validate it as being in the <see cref="TypeNames"/> array
        /// bound and check the value has being a valid class name.</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <remarks>This differs from the <see cref="GetClassNameFromIndex"/>
        /// method by the index size.</remarks>
        private string GetMemberClassNameFromIndex(BinaryReader reader,
            bool mayBeNull = false)
        {
            ushort classIndex = reader.ReadUInt16();

            return GetClassName(classIndex, mayBeNull, true);
        }

        private string GetMemberNameFromULEB128Index(BinaryReader reader)
        {
            uint memberNameIndex = Helpers.ReadULEB128(reader);
            if (memberNameIndex >= this.Strings.Count) { throw new ParseException(); }
            string result = this.Strings[(int)memberNameIndex];
            if (!Helpers.IsValidMemberName(result)) { throw new ParseException(); }
            return result;
        }

        private Method GetMethodFromIndex(BinaryReader reader)
        {
            uint methodNameIndex = reader.ReadUInt32();
            if (methodNameIndex >= this.Methods.Count) { throw new ParseException(); }
            return this.Methods[(int)methodNameIndex];
        }

        /// <summary>Enumerate the item collection and for each in turn find the class
        /// instance from the class name located in the item then link the item to the
        /// class.</summary>
        /// <typeparam name="T">A type that implements the <see cref="IClassMember"/>
        /// interface.</typeparam>
        /// <param name="items">A collection of <see cref="IClassMember"/> objects.</param>
        private void LinkMembersToClass<T>(List<T> items)
            where T : IClassMember
        {
            foreach (IClassMember scannedItem in items) {
                string className = scannedItem.ClassName;
                IClass owner = null;
                foreach (ClassDefinition scannedClass in this.Classes) {
                    if (scannedClass.Name == className) {
                        owner = scannedClass;
                        break;
                    }
                }
                scannedItem.LinkTo((null == owner) ? this.GetExternalClass(className) : owner);
            }
        }

        /// <summary>Reads an annotation set offset at current reader position
        /// then jump to that position and load the set. On return the reader
        /// is positioned just after the annotation offset that has been read
        /// at method startup.</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private List<Annotation> LoadAnnotationSet(BinaryReader reader)
        {
            uint annotationsOffset = reader.ReadUInt32();
            if (0 == annotationsOffset) { return null; }
            Header.AssertOffsetInDataSection(annotationsOffset);
            long initialPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = annotationsOffset;

            // size of the set, in entries
            uint size = reader.ReadUInt32();
            if (0 == size) { return null; }
            List<Annotation> result = new List<Annotation>();
            // The elements must be sorted in increasing order, by type_idx.
            for (uint index = 0; index < size; index++) {
                // offset from the start of the file to an annotation. The offset
                // should be to a location in the data section, and the format of
                // the data at that location is specified by "annotation_item" below.
                uint annotationOffset = reader.ReadUInt32();
                Header.AssertOffsetInDataSection(annotationOffset);
                long savedPosition = reader.BaseStream.Position;
                reader.BaseStream.Position = annotationOffset;

                // Read the annotation item.
                // intended visibility of this annotation
                AnnotationVisibility visibility = (AnnotationVisibility)reader.ReadByte();
                // encoded annotation contents, in the format described by "encoded_annotation Format"
                // under "encoded_value Encoding" above.
                result.Add(LoadEncodedAnnotation(reader));
                reader.BaseStream.Position = savedPosition;
            }
            reader.BaseStream.Position = initialPosition;
            return result;
        }

        private void LoadClasses(BinaryReader reader,
            out List<PendingClassResolution> pendingResolutions)
        {
            if (null != this.Classes) { throw new InvalidOperationException(); }
            this.Classes = new List<ClassDefinition>();
            pendingResolutions = new List<PendingClassResolution>();
            uint listOffset = this.Header.ClassesDefinition.Offset;
            if (0 == listOffset) { return; }
            uint listSize = this.Header.ClassesDefinition.Size;
            reader.BaseStream.Position = listOffset;
            for (uint index = 0; index < listSize; index++) {
                // index into the type_ids list for this class. This must be a class type,
                // and not an array or primitive type. 
                string thisClassName = GetClassNameFromIndex(reader);
                // access flags for the class (public, final, etc.). See
                // "access_flags Definitions" for details.
                AccessFlags accessFlags = (AccessFlags)reader.ReadUInt32(); // uint
                // index into the type_ids list for the superclass, or the constant value
                // NO_INDEX if this class has no superclass (i.e., it is a root class such
                // as Object). If present, this must be a class type, and not an array or
                // primitive type.
                string superClassName = GetClassNameFromIndex(reader, true); // uint
                // offset from the start of the file to the list of interfaces, or 0 if
                // there are none. This offset should be in the data section, and the data
                // there should be in the format specified by "type_list" below. Each of the
                // elements of the list must be a class type (not an array or primitive type),
                // and there must not be any duplicates.
                List<string> interfaces = LoadTypeList(reader);
                // index into the string_ids list for the name of the file containing the original
                // source for (at least most of) this class, or the special value NO_INDEX to
                // represent a lack of this information. The debug_info_item of any given method
                // may override this source file, but the expectation is that most classes will
                // only come from one source file.
                string sourceFileName = GetIndexedString(reader, true);
                // offset from the start of the file to the annotations structure for this class,
                // or 0 if there are no annotations on this class. This offset, if non-zero,
                // should be in the data section, and the data there should be in the format
                // specified by "annotations_directory_item" below, with all items referring to
                // this class as the definer.
                List<Annotation> classAnnotations = LoadClassAnnotations(reader);
                // offset from the start of the file to the associated class data for this item,
                // or 0 if there is no class data for this class. (This may be the case, for
                // example, if this class is a marker interface.) The offset, if non-zero, should
                // be in the data section, and the data there should be in the format specified by
                // "class_data_item" below, with all items referring to this class as the definer.
                ClassDefinition thisClass = LoadClassData(reader, thisClassName);
                thisClass.Access = accessFlags;
                thisClass.Annotations = classAnnotations;
                thisClass.Filename = sourceFileName;
                this.Classes.Add(thisClass);
                PendingClassResolution pendingResolution = new PendingClassResolution();
                pendingResolution.Class = thisClass;
                pendingResolution.ImplementedInterfacesName = interfaces;
                pendingResolution.SuperClassName = superClassName;
                pendingResolutions.Add(pendingResolution);
                // offset from the start of the file to the list of initial values for static fields,
                // or 0 if there are none (and all static fields are to be initialized with 0 or null).
                // This offset should be in the data section, and the data there should be in the
                // format specified by "encoded_array_item" below. The size of the array must be no
                // larger than the number of static fields declared by this class, and the elements
                // correspond to the static fields in the same order as declared in the corresponding
                // field_list. The type of each array element must match the declared type of its
                // corresponding field. If there are fewer elements in the array than there are static
                // fields, then the leftover fields are initialized with a type-appropriate 0 or null.
                uint staticValuesOffset = reader.ReadUInt32();
                if (0 < staticValuesOffset) {
                    Header.AssertOffsetInDataSection(staticValuesOffset);
                    throw new NotImplementedException();
                }
                FindKnownType(thisClassName).SetDefinition(thisClass);
            }
        }

        private List<Annotation> LoadClassAnnotations(BinaryReader reader)
        {
            uint annotationsOffset = reader.ReadUInt32();
            if (0 == annotationsOffset) { return null; }
            Header.AssertOffsetInDataSection(annotationsOffset);
            long initialPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = annotationsOffset;

            // offset from the start of the file to the annotations made directly on the class, or 0
            // if the class has no direct annotations. The offset, if non-zero, should be to a
            // location in the data section. The format of the data is specified by
            // "annotation_set_item" below.
            List<Annotation> result = LoadAnnotationSet(reader);
            // count of fields annotated by this item
            uint annotatedFieldsSize = reader.ReadUInt32();
            // count of methods annotated by this item
            uint annotatedMethodsSize = reader.ReadUInt32();
            // count of method parameter lists annotated by this item
            uint annotatedParametersSize = reader.ReadUInt32();
            // list of associated field annotations. The elements of the list must be sorted in
            // increasing order, by field_idx.
            for (int index = 0; index < annotatedFieldsSize; index++) { LoadFieldAnnotations(reader); }
            for (int index = 0; index < annotatedMethodsSize; index++) { LoadMethodAnnotations(reader); }
            for (int index = 0; index < annotatedParametersSize; index++) { LoadParameterAnnotations(reader); }

            reader.BaseStream.Position = initialPosition;
            return result;
        }

        private ClassDefinition LoadClassData(BinaryReader reader, string className)
        {
            ClassDefinition result = new ClassDefinition(className);

            uint classDataOffset = reader.ReadUInt32();
            if (0 == classDataOffset) { return result; }
            Header.AssertOffsetInDataSection(classDataOffset);
            long initialPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = classDataOffset;
            // the number of static fields defined in this item
            uint staticFieldsCount = Helpers.ReadULEB128(reader);
            // the number of instance fields defined in this item
            uint instanceFieldsCount = Helpers.ReadULEB128(reader);
            // the number of direct methods defined in this item
            uint directMethodsCount = Helpers.ReadULEB128(reader);
            // the number of virtual methods defined in this item
            uint virtualMethodsCount = Helpers.ReadULEB128(reader);
            // the defined static fields, represented as a sequence of encoded elements.
            // The fields must be sorted by field_idx in increasing order.
            uint previousFieldIndex = 0;
            for(int index = 0; index < staticFieldsCount; index++) {
                result.AddStaticField(LoadEncodedField(reader, ref previousFieldIndex));
            }
            // the defined instance fields, represented as a sequence of encoded elements.
            // The fields must be sorted by field_idx in increasing order.
            previousFieldIndex = 0;
            for(int index = 0; index < instanceFieldsCount; index++) {
                result.AddInstanceField(LoadEncodedField(reader, ref previousFieldIndex));
            }
            // the defined direct (any of static, private, or constructor) methods, represented
            // as a sequence of encoded elements. The methods must be sorted by method_idx in
            // increasing order.
            uint previousMethodIndex = 0;
            for(int index = 0; index < directMethodsCount; index++) {
                result.AddDirectMethod(LoadEncodedMethod(reader, ref previousMethodIndex));
            }
            previousMethodIndex = 0;
            for(int index = 0; index < virtualMethodsCount; index++) {
                result.AddVirtualMethod(LoadEncodedMethod(reader, ref previousMethodIndex));
            }
            reader.BaseStream.Position = initialPosition;
            return result;
        }

        private DebugInfo LoadDebugInfo(BinaryReader reader)
        {
            uint debugInfoOffset = reader.ReadUInt32();
            if (0 == debugInfoOffset) { return null; }
            Header.AssertOffsetInDataSection(debugInfoOffset);
            long savedPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = debugInfoOffset;

            throw new NotImplementedException();
            //Each debug_info_item defines a DWARF3-inspired byte-coded state machine that, when interpreted, emits the positions table and (potentially) the local variable information for a code_item. The sequence begins with a variable-length header (the length of which depends on the number of method parameters), is followed by the state machine bytecodes, and ends with an DBG_END_SEQUENCE byte.

            //The state machine consists of five registers. The address register represents the instruction offset in the associated insns_item in 16-bit code units. The address register starts at 0 at the beginning of each debug_info sequence and must only monotonically increase. The line register represents what source line number should be associated with the next positions table entry emitted by the state machine. It is initialized in the sequence header, and may change in positive or negative directions but must never be less than 1. The source_file register represents the source file that the line number entries refer to. It is initialized to the value of source_file_idx in class_def_item. The other two variables, prologue_end and epilogue_begin, are boolean flags (initialized to false) that indicate whether the next position emitted should be considered a method prologue or epilogue. The state machine must also track the name and type of the last local variable live in each register for the DBG_RESTART_LOCAL code.

            //The header is as follows:
            //Name 	Format 	Description
            //line_start 	uleb128 	the initial value for the state machine's line register. Does not represent an actual positions entry.
            //parameters_size 	uleb128 	the number of parameter names that are encoded. There should be one per method parameter, excluding an instance method's this, if any.
            //parameter_names 	uleb128p1[parameters_size] 	string index of the method parameter name. An encoded value of NO_INDEX indicates that no name is available for the associated parameter. The type descriptor and signature are implied from the method descriptor and signature.

            //The byte code values are as follows:
            //Name 	Value 	Format 	Arguments 	Description
            //DBG_END_SEQUENCE 	0x00 		(none) 	terminates a debug info sequence for a code_item
            //DBG_ADVANCE_PC 	0x01 	uleb128 addr_diff 	addr_diff: amount to add to address register 	advances the address register without emitting a positions entry
            //DBG_ADVANCE_LINE 	0x02 	sleb128 line_diff 	line_diff: amount to change line register by 	advances the line register without emitting a positions entry
            //DBG_START_LOCAL 	0x03 	uleb128 register_num
            //uleb128p1 name_idx
            //uleb128p1 type_idx 	register_num: register that will contain local
            //name_idx: string index of the name
            //type_idx: type index of the type 	introduces a local variable at the current address. Either name_idx or type_idx may be NO_INDEX to indicate that that value is unknown.
            //DBG_START_LOCAL_EXTENDED 	0x04 	uleb128 register_num
            //uleb128p1 name_idx
            //uleb128p1 type_idx
            //uleb128p1 sig_idx 	register_num: register that will contain local
            //name_idx: string index of the name
            //type_idx: type index of the type
            //sig_idx: string index of the type signature 	introduces a local with a type signature at the current address. Any of name_idx, type_idx, or sig_idx may be NO_INDEX to indicate that that value is unknown. (If sig_idx is -1, though, the same data could be represented more efficiently using the opcode DBG_START_LOCAL.)

            //Note: See the discussion under "dalvik.annotation.Signature" below for caveats about handling signatures.
            //DBG_END_LOCAL 	0x05 	uleb128 register_num 	register_num: register that contained local 	marks a currently-live local variable as out of scope at the current address
            //DBG_RESTART_LOCAL 	0x06 	uleb128 register_num 	register_num: register to restart 	re-introduces a local variable at the current address. The name and type are the same as the last local that was live in the specified register.
            //DBG_SET_PROLOGUE_END 	0x07 		(none) 	sets the prologue_end state machine register, indicating that the next position entry that is added should be considered the end of a method prologue (an appropriate place for a method breakpoint). The prologue_end register is cleared by any special (>= 0x0a) opcode.
            //DBG_SET_EPILOGUE_BEGIN 	0x08 		(none) 	sets the epilogue_begin state machine register, indicating that the next position entry that is added should be considered the beginning of a method epilogue (an appropriate place to suspend execution before method exit). The epilogue_begin register is cleared by any special (>= 0x0a) opcode.
            //DBG_SET_FILE 	0x09 	uleb128p1 name_idx 	name_idx: string index of source file name; NO_INDEX if unknown 	indicates that all subsequent line number entries make reference to this source file name, instead of the default name specified in code_item
            //Special Opcodes 	0x0a…0xff 		(none) 	advances the line and address registers, emits a position entry, and clears prologue_end and epilogue_begin. See below for description.
            //Special Opcodes

            //Opcodes with values between 0x0a and 0xff (inclusive) move both the line and address registers by a small amount and then emit a new position table entry. The formula for the increments are as follows:

            //DBG_FIRST_SPECIAL = 0x0a  // the smallest special opcode
            //DBG_LINE_BASE   = -4      // the smallest line number increment
            //DBG_LINE_RANGE  = 15      // the number of line increments represented

            //adjusted_opcode = opcode - DBG_FIRST_SPECIAL

            //line += DBG_LINE_BASE + (adjusted_opcode % DBG_LINE_RANGE)
            //address += (adjusted_opcode / DBG_LINE_RANGE)


            //Note: All elements' field_ids and method_ids must refer to the same defining class.


            reader.BaseStream.Position = savedPosition;
        }

        private Annotation LoadEncodedAnnotation(BinaryReader reader)
        {
            // type of the annotation. This must be a class (not array or primitive) type.
            string className = GetClassNameFromULEB128Index(reader);
            // number of name-value mappings in this annotation
            uint size = Helpers.ReadULEB128(reader);
            List<AnnotationElement> mappings = new List<AnnotationElement>();
            // elements of the annotataion, represented directly in-line (not as offsets).
            // Elements must be sorted in increasing order by string_id index.
            for (int index = 0; index < size; index++)
            {
                mappings.Add(LoadEncodedAnnotationElement(reader));
            }
            return new Annotation(className, mappings);
        }

        private AnnotationElement LoadEncodedAnnotationElement(BinaryReader reader)
        {
            // element name, represented as an index into the string_ids section.
            // The string must conform to the syntax for MemberName, defined above.
            string memberName = GetMemberNameFromULEB128Index(reader);
            object value = LoadEncodedValue(reader);

            return new AnnotationElement(memberName, value);
        }

        private object LoadEncodedArray(BinaryReader reader)
        {
            // number of elements in the array
            uint arraySize = Helpers.ReadULEB128(reader);
            object[] result = new object[arraySize];
            // a series of size encoded_value byte sequences in the format specified by
            // this section, concatenated sequentially. 
            for (int index = 0; index < arraySize; index++) {
                result[index] = LoadEncodedValue(reader);
            }
            return result;
        }

        private Field LoadEncodedField(BinaryReader reader, ref uint previousFieldIndex)
        {
            // index into the field_ids list for the identity of this field (includes
            // the name and descriptor), represented as a difference from the index of
            // previous element in the list. The index of the first element in a list
            // is represented directly.
            uint fieldIndexDiff = Helpers.ReadULEB128(reader);
            uint trueFieldIndex = fieldIndexDiff + previousFieldIndex;
            if (Fields.Count <= trueFieldIndex) { throw new ParseException(); }
            Field result = Fields[(int)trueFieldIndex];
            // (public, final, etc.). See "access_flags Definitions" for details.
            result.AccessFlags = (AccessFlags)Helpers.ReadULEB128(reader);

            previousFieldIndex = trueFieldIndex;
            return result;
        }

        private Method LoadEncodedMethod(BinaryReader reader, ref uint previousMethodIndex)
        {
            // index into the method_ids list for the identity of this method (includes
            // the name and descriptor), represented as a difference from the index of
            // previous element in the list. The index of the first element in a list
            // is represented directly.
            uint methodIndexDiff = Helpers.ReadULEB128(reader);
            uint trueMethodIndex = methodIndexDiff + previousMethodIndex;
            if (Methods.Count <= trueMethodIndex) { throw new ParseException(); }
            Method result = Methods[(int)trueMethodIndex];
            // access flags for the method (public, final, etc.). See "access_flags
            // Definitions" for details.
            result.AccessFlags = (AccessFlags)Helpers.ReadULEB128(reader);

            try {
                // offset from the start of the file to the code structure for this method,
                // or 0 if this method is either abstract or native. The offset should be
                // to a location in the data section. The format of the data is specified
                // by "code_item" below.
                uint codeOffset = Helpers.ReadULEB128(reader);
                if (0 == codeOffset) {
                    if ((0 == (result.AccessFlags & AccessFlags.Native))
                        && (0 == (result.AccessFlags & AccessFlags.Abstract)))
                    {
                        throw new ParseException();
                    }
                    return result;
                }
                Header.AssertOffsetInDataSection(codeOffset);
                if ((0 != (result.AccessFlags & AccessFlags.Native))
                    || (0 != (result.AccessFlags & AccessFlags.Abstract)))
                {
                    throw new ParseException();
                }
                long savedPosition = reader.BaseStream.Position;
                reader.BaseStream.Position = codeOffset;

                // Loading the code item.
                // the number of registers used by this code
                result.RegistersCount = reader.ReadUInt16();
                // the number of words of incoming arguments to the method that
                // this code is for
                result.ArgumentsWordsCount = reader.ReadUInt16();
                // the number of words of outgoing argument space required by
                // this code for method invocation
                result.ResultsWordsCount = reader.ReadUInt16();
                // the number of try_items for this instance. If non-zero, then these
                // appear as the tries array just after the insns in this instance.
                ushort triesItemsCount = reader.ReadUInt16();
                // offset from the start of the file to the debug info (line numbers +
                // local variable info) sequence for this code, or 0 if there simply is
                // no information. The offset, if non-zero, should be to a location in
                // the data section. The format of the data is specified by
                // "debug_info_item" below.
                result.DebugInfo = LoadDebugInfo(reader);
                // size of the instructions list, in 16-bit code units
                uint instructionsListWordCount = reader.ReadUInt32();
                // actual array of bytecode. The format of code in an insns array is specified
                // by the companion document "Bytecode for the Dalvik VM". Note that though this
                // is defined as an array of ushort, there are some internal structures that
                // prefer four-byte alignment. Also, if this happens to be in an endian-swapped
                // file, then the swapping is only done on individual ushorts and not on the
                // larger internal structures.
                result.ByteCodeRawAddress = (uint)reader.BaseStream.Position;
                result.ByteCode = reader.ReadBytes((int)(sizeof(ushort) * instructionsListWordCount));
                // (optional) = 0 two bytes of padding to make tries four-byte aligned. This
                // element is only present if tries_size is non-zero and insns_size is odd.
                if (0 < triesItemsCount) {
                    Helpers.Align(reader, 4);
                    // array indicating where in the code exceptions are caught and how to handle them.
                    // Elements of the array must be non-overlapping in range and in order from low
                    // to high address. This element is only present if tries_size is non-zero.
                    Dictionary<ushort, List<TryBlock>> blocksPendingHandlerResolution =
                        new Dictionary<ushort, List<TryBlock>>();
                    for(int index = 0; index < triesItemsCount; index++) {
                        // start address of the block of code covered by this entry. The address is a
                        // count of 16-bit code units to the start of the first covered instruction.
                        uint methodStartOffset = reader.ReadUInt32() * sizeof(ushort);
                        // number of 16-bit code units covered by this entry. The last code unit covered
                        // (inclusive) is start_addr + insn_count - 1.
                        ushort blockSize = (ushort)(reader.ReadUInt16() * sizeof(ushort));
                        // offset in bytes from the start of the associated encoded_catch_hander_list to
                        // the encoded_catch_handler for this entry. This must be an offset to the start
                        // of an encoded_catch_handler.
                        ushort handlerOffset = reader.ReadUInt16();

                        TryBlock block = new TryBlock(methodStartOffset, blockSize);
                        result.AddGuardedBlock(block);
                        // The spec is unclear about whether two blocks could share the same
                        // handler. However we encountered some DEX files where this is the case.
                        List<TryBlock> tryBlocks;
                        if (!blocksPendingHandlerResolution.TryGetValue(handlerOffset, out tryBlocks))
                        {
                            tryBlocks = new List<TryBlock>();
                            blocksPendingHandlerResolution.Add(handlerOffset, tryBlocks);
                        }
                        tryBlocks.Add(block);
                    }
                    // We capture the position of the first byte of the encoded_catch_handler_list
                    // in order to identify guard handler offset.
                    long handlersListBasePosition = reader.BaseStream.Position;
                    // bytes representing a list of lists of catch types and associated handler addresses.
                    // Each try_item has a byte-wise offset into this structure. This element is only
                    // present if tries_size is non-zero.
                    // size of this list, in entries
                    uint handlersCount = Helpers.ReadULEB128(reader);
                    // actual list of handler lists, represented directly (not as offsets), and concatenated
                    // sequentially
                    for(int index = 0; index < handlersCount; index++) {
                        GuardHandlers guardHandler = new GuardHandlers();
                        uint thisHandlerOffset =
                            (uint)(reader.BaseStream.Position - handlersListBasePosition);
                        List<TryBlock> pendingBlocks;
                        if (blocksPendingHandlerResolution.TryGetValue((ushort)thisHandlerOffset, out pendingBlocks)) {
                            // It is unclear whether a guard block that is not referenced is considered
                            // to be an error. However we encountered some DEX files where this seems to
                            // be the case.
                            foreach (TryBlock pendingBlock in pendingBlocks) {
                                pendingBlock.Handlers = guardHandler;
                            }
                            blocksPendingHandlerResolution.Remove((ushort)thisHandlerOffset);
                        }

                        // number of catch types in this list. If non-positive, then this is the negative of
                        // the number of catch types, and the catches are followed by a catch-all handler.
                        // For example: A size of 0 means that there is a catch-all but no explicitly typed
                        // catches. A size of 2 means that there are two explicitly typed catches and no
                        // catch-all. And a size of -1 means that there is one typed catch along with a
                        // catch-all.
                        int catchesCount = Helpers.ReadSLEB128(reader);
                        bool hasCatchAll = (0 >= catchesCount);
                        if (hasCatchAll) { catchesCount = -catchesCount; }
                        // stream of abs(size) encoded items, one for each caught type, in the order that
                        // the types should be tested.
                        for(int addrPairIndex = 0; addrPairIndex < catchesCount; addrPairIndex++) {
                            // index into the type_ids list for the type of the exception to catch
                            uint caughtTypeIndex = Helpers.ReadULEB128(reader);
                            if (Types.Count <= caughtTypeIndex) { throw new ParseException(); }
                            string typeName = Types[(int)caughtTypeIndex].FullName;
                            // bytecode address of the associated exception handler
                            uint handlerCodeAddress = Helpers.ReadULEB128(reader) * sizeof(ushort);
                            guardHandler.AddCatchClause(typeName, handlerCodeAddress);
                        }
                        if (hasCatchAll) {
                            // bytecode address of the catch-all handler. This element is only present if
                            // size is non-positive.
                            guardHandler.CatchAllHandlerAddress = Helpers.ReadULEB128(reader) * sizeof(ushort);
                        }
                    }
                    if (0 < blocksPendingHandlerResolution.Count) {
                        // Some try blocks were not matched against a guard handler. This is
                        // definitively an error.
                        throw new ParseException();
                    }
                }
                reader.BaseStream.Position = savedPosition;
                return result;
            }
            finally { previousMethodIndex += methodIndexDiff; }
        }

        /// <summary></summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        /// <remarks>We do not support REVERSE_ENDIAN encoding at current time. This makes
        /// the code simpler.</remarks>
        private object LoadEncodedValue(BinaryReader reader)
        {
            // An encoded_value is an encoded piece of (nearly) arbitrary hierarchically
            // structured data. The encoding is meant to be both compact and straightforward
            // to parse.

            // byte indicating the type of the immediately subsequent value along with an
            // optional clarifying argument in the high-order three bits. See below for the
            // various value definitions. In most cases, value_arg encodes the length of the
            // immediately-subsequent value in bytes, as (size - 1), e.g., 0 means that the
            // value requires one byte, and 7 means it requires eight bytes; however, there
            // are exceptions as noted below.
            byte inputByte = reader.ReadByte();
            EncodedValueType valueType = (EncodedValueType)(inputByte & 0x1F);
            byte valueArg = (byte)((inputByte & 0xE0) >> 5);

            switch (valueType) {
                // value_arg must be 0. data type is byte.
                // signed one-byte integer value
                case EncodedValueType.Byte:
                    if (0 != valueArg) { throw new ParseException(); }
                    return reader.ReadSByte();
                // value_arg is size - 1 (0 or 1). data type is an array of bytes
                // of variable length defined by value_arg. 
                // signed two-byte integer value, sign-extended
                case EncodedValueType.Short:
                    switch (valueArg) {
                        case 0:
                            return (short)(sbyte)reader.ReadByte();
                        case 1:
                            return reader.ReadInt16();
                        default:
                            throw new ParseException();
                    }
                // value_arg is size - 1 (0 or 1). data type is an array of bytes
                // of variable length defined by value_arg. 
                // unsigned two-byte integer value, zero-extended
                case EncodedValueType.Char:
                    switch (valueArg) {
                        case 0:
                            return (char)reader.ReadByte();
                        case 1:
                            return (char)reader.ReadUInt16();
                        default:
                            throw new ParseException();
                    }
                // value_arg is size - 1 (0 to 3). data type is an array of bytes
                // of variable length defined by value_arg. 
                // signed four-byte integer value, sign-extended
                case EncodedValueType.Int:
                    if (3 < valueArg) { throw new ParseException(); }
                    int intResult = 0;
                    for (int index = 0; index <= valueArg; index++) {
                        intResult += (reader.ReadByte() << (8 * index));
                    }
                    return intResult;
                // value_arg is size - 1 (0 to 7). data type is an array of bytes
                // of variable length defined by value_arg. 
                // signed eight-byte integer value, sign-extended
                case EncodedValueType.Long:
                    if (7 < valueArg) { throw new ParseException(); }
                    long longResult = 0;
                    for (int index = 0; index <= valueArg; index++) {
                        longResult += (reader.ReadByte() << (8 * index));
                    }
                    return longResult;
                // value_arg is size - 1 (0 to 3). data type is an array of bytes
                // of variable length defined by value_arg. 
                // four-byte bit pattern, zero-extended to the right, and interpreted
                // as an IEEE754 32-bit floating point value
                case EncodedValueType.Float:
                    throw new NotImplementedException();
                // value_arg is size - 1 (0 to 7). data type is an array of bytes
                // of variable length defined by value_arg. 
                // eight-byte bit pattern, zero-extended to the right, and interpreted
                // as an IEEE754 64-bit floating point value
                case EncodedValueType.Double:
                    throw new NotImplementedException();
                // value_arg is size - 1 (0 to 3). data type is an array of bytes
                // of variable length defined by value_arg. 
                // unsigned (zero-extended) four-byte integer value, interpreted as an
                // index into the string_ids section and representing a string value
                case EncodedValueType.String:
                    return LoadIndexedEncodedValue(reader, valueArg, this.Strings);
                // value_arg is size - 1 (0 to 3). data type is an array of bytes
                // of variable length defined by value_arg. 
                // unsigned (zero-extended) four-byte integer value, interpreted as an
                // index into the type_ids section and representing a reflective
                // type/class value
                case EncodedValueType.Type:
                    return LoadIndexedEncodedValue(reader, valueArg, this.Types);
                // value_arg is size - 1 (0 to 3). data type is an array of bytes
                // of variable length defined by value_arg. 
                // unsigned (zero-extended) four-byte integer value, interpreted as an
                // index into the field_ids section and representing a reflective field
                // value
                case EncodedValueType.Field:
                    return LoadIndexedEncodedValue(reader, valueArg, this.Fields);
                // value_arg is size - 1 (0 to 3). data type is an array of bytes
                // of variable length defined by value_arg. 
                // unsigned (zero-extended) four-byte integer value, interpreted as an
                // index into the method_ids section and representing a reflective method
                // value
                case EncodedValueType.Method:
                    return LoadIndexedEncodedValue(reader, valueArg, this.Methods);
                // value_arg is size - 1 (0 to 3). data type is an array of bytes
                // of variable length defined by value_arg. 
                // unsigned (zero-extended) four-byte integer value, interpreted as an
                // index into the field_ids section and representing the value of an
                // enumerated type constant
                case EncodedValueType.Enum:
                    return LoadIndexedEncodedValue(reader, valueArg, this.Fields);
                // value_arg must be 0. data type is an encoded array.
                // an array of values, in the format specified by "encoded_array Format"
                // below. The size of the value is implicit in the encoding.
                case EncodedValueType.Array:
                    if (0 != valueArg) { throw new ParseException(); }
                    return LoadEncodedArray(reader);
                // value_arg must be 0. data type is an encoded annotation.
                // a sub-annotation, in the format specified by "encoded_annotation Format"
                // below. The size of the value is implicit in the encoding.
                case EncodedValueType.Annotation:
                    if (0 != valueArg) { throw new ParseException(); }
                    return LoadEncodedAnnotation(reader);
                // value_arg must be 0. no data.
                // A null reference value
                case EncodedValueType.Null:
                    if (0 != valueArg) { throw new ParseException(); }
                    return null;
                // value_arg is 0 or 1. no data
                // one-bit value; 0 for false and 1 for true. The bit is represented
                // in the value_arg.
                case EncodedValueType.Boolean:
                    switch(valueArg) {
                        case 0:
                            return false;
                        case 1:
                            return true;
                        default:
                            throw new ParseException();
                    }
                default:
                    throw new ParseException();
            }
        }

        private void LoadFieldAnnotations(BinaryReader reader)
        {
            // index into the field_ids list for the identity of the field being annotated
            Field field = GetFieldFromIndex(reader);
            // offset from the start of the file to the list of annotations for the field.
            // The offset should be to a location in the data section. The format of the
            // data is specified by "annotation_set_item" below.
            field.Annotations = LoadAnnotationSet(reader);
            return;
        }

        /// <summary></summary>
        /// <param name="reader"></param>
        /// <remarks>This list must be sorted, where the defining type (by type_id index) is
        /// the major order, field name (by string_id index) is the intermediate order, and
        /// type (by type_id index) is the minor order. The list must not contain any duplicate
        /// entries. </remarks>
        private void LoadFields(BinaryReader reader)
        {
            if (null != this.Fields) { throw new InvalidOperationException(); }
            Fields = new List<Field>();
            uint listOffset = this.Header.FieldIdsDefinition.Offset;
            if (0 == listOffset) { return; }
            uint listSize = this.Header.FieldIdsDefinition.Size;
            reader.BaseStream.Position = listOffset;
            for (uint index = 0; index < listSize; index++) {
                // index into the type_ids list for the definer of this field.
                // This must be a class type, and not an array or primitive type.
                string owningType = GetMemberClassNameFromIndex(reader);

                // index into the type_ids list for the type of this field.
                ushort typeIndex = reader.ReadUInt16();
                if (typeIndex >= this.Types.Count) { throw new ParseException(); }
                string fieldTypeName = this.Types[(int)typeIndex].FullName;

                // index into the string_ids list for the name of this field.
                // The string must conform to the syntax for MemberName, defined above.
                uint nameIndex = reader.ReadUInt32();
                if (nameIndex >= this.Strings.Count) { throw new ParseException(); }
                string fieldName = this.Strings[(int)nameIndex];
                if (!Helpers.IsValidMemberName(fieldName)) { throw new ParseException(); }
                Fields.Add(new Field(owningType, fieldTypeName, fieldName));
            }
            return;
        }

        private T LoadIndexedEncodedValue<T>(BinaryReader reader, int valueArg,
            List<T> from)
        {
            if (3 < valueArg) { throw new ParseException(); }
            int indexValue = 0;
            for (int index = 0; index <= valueArg; index++) {
                indexValue += (reader.ReadByte() << (8 * index));
            }
            if (from.Count <= indexValue) { throw new ParseException(); }
            return from[indexValue];
        }

        private void LoadMethodAnnotations(BinaryReader reader)
        {
            // index into the method_ids list for the identity of the method being annotated
            Method method = GetMethodFromIndex(reader);
            // offset from the start of the file to the list of annotations for the method.
            // The offset should be to a location in the data section. The format of the
            // data is specified by "annotation_set_item" below.
            method.Annotations = LoadAnnotationSet(reader);
            return;
        }

        /// <summary></summary>
        /// <param name="reader"></param>
        /// <remarks>This list must be sorted, where the defining type (by type_id index)
        /// is the major order, method name (by string_id index) is the intermediate order,
        /// and method prototype (by proto_id index) is the minor order. The list must not
        /// contain any duplicate entries. </remarks>
        private void LoadMethods(BinaryReader reader)
        {
            if (null != this.Methods) { throw new InvalidOperationException(); }
            this.Methods = new List<Method>();
            uint listOffset = this.Header.MethodIdsDefinition.Offset;
            if (0 == listOffset) { return; }
            uint listSize = this.Header.MethodIdsDefinition.Size;
            reader.BaseStream.Position = listOffset;

            for (uint index = 0; index < listSize; index++) {
                // index into the type_ids list for the definer of this method. This must be a
                // class or array type, and not a primitive type.
                string className = GetMemberClassNameFromIndex(reader);
                // index into the proto_ids list for the prototype of this method
                ushort prototypeIndex = reader.ReadUInt16();
                if (prototypeIndex >= this.Prototypes.Count) { throw new ParseException(); }
                Prototype methodPrototype = this.Prototypes[prototypeIndex];
                // into the string_ids list for the name of this method. The string must conform
                // to the syntax for MemberName, defined above.
                uint nameIndex = reader.ReadUInt32();
                if (nameIndex >= this.Strings.Count) { throw new ParseException(); }
                string methodName = this.Strings[(int)nameIndex];
                if (!Helpers.IsValidMemberName(methodName)) { throw new ParseException(); }

                this.Methods.Add(new Method(className, methodName, methodPrototype));
            }

            return;
        }

        private void LoadParameterAnnotations(BinaryReader reader)
        {
            // index into the method_ids list for the identity of the method whose parameters
            // are being annotated
            Method method = GetMethodFromIndex(reader);
            // offset from the start of the file to the list of annotations for the method
            // parameters. The offset should be to a location in the data section. The format
            // of the data is specified by "annotation_set_ref_list" below.
            uint offset = reader.ReadUInt32();
            Header.AssertOffsetInDataSection(offset);
            long savedPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = offset;

            // Read the annotation set ref list.
            // size of the list, in entries
            uint itemsCount = reader.ReadUInt32();
            List<List<Annotation>> annotations = new List<List<Annotation>>();
            for(int index = 0; index < itemsCount; index++) {
                // Read an annotation_set_ref_item
                List<Annotation> parameterAnnotations = LoadAnnotationSet(reader);

                annotations.Add(parameterAnnotations);
            }

            reader.BaseStream.Position = savedPosition;
            return;
        }

        //private delegate T ItemParserDelegate<T>(BinaryReader reader);

        //private List<T> LoadIndexList<T>(BinaryReader reader,
        //    ItemParserDelegate<T> itemParser)
        //{
        //    uint listOffset = reader.ReadUInt32();
        //    if (0 == listOffset) { return null; }
        //    Header.AssertOffsetInDataSection(listOffset);
        //    long savedPosition = reader.BaseStream.Position;
        //    reader.BaseStream.Position = listOffset;
        //    List<T> result = new List<T>();
        //    for(int index = 0; index < 
        //    reader.BaseStream.Position = savedPosition;
        //    return result;
        //}

        /// <summary></summary>
        /// <param name="reader"></param>
        /// <remarks>This list must be sorted in return-type (by type_id index) major
        /// order, and then by arguments (also by type_id index). The list must not
        /// contain any duplicate entries.</remarks>
        private void LoadPrototypes(BinaryReader reader)
        {
            if (null != this.Prototypes) { throw new InvalidOperationException(); }
            this.Prototypes = new List<Prototype>();
            uint listOffset = this.Header.ProtoIdsDefinition.Offset;
            if (0 == listOffset) { return; }
            uint listSize = this.Header.ProtoIdsDefinition.Size;
            reader.BaseStream.Position = listOffset;

            for (uint index = 0; index < listSize; index++) {
                // index into the string_ids list for the short-form descriptor string
                // of this prototype. The string must conform to the syntax for
                // ShortyDescriptor, and must correspond to the return type and parameters
                // of this item.
                uint shortIndex = reader.ReadUInt32();
                if (shortIndex >= this.Strings.Count) { throw new ParseException(); }
                string shortDescriptor = this.Strings[(int)shortIndex];
                if (!Helpers.IsValidShortDescriptor(shortDescriptor)) { throw new ParseException(); }
                // index into the type_ids list for the return type of this prototype
                string returnType = GetKnownTypeName(reader, false);
                // offset from the start of the file to the list of parameter types for
                // this prototype, or 0 if this prototype has no parameters. This offset,
                // if non-zero, should be in the data section, and the data there should
                // be in the format specified by "type_list" below. Additionally, there
                // should be no reference to the type void in the list.
                List<string> parameters = LoadTypeList(reader);
                this.Prototypes.Add(new Prototype(returnType, shortDescriptor, parameters));
            }
            return;
        }

        /// <summary>Read a type list offset from the current reader position then
        /// jump to the given offset and extract the type list. On return the reader
        /// position is set just after the type list offset that is read at method
        /// startup.</summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private List<string> LoadTypeList(BinaryReader reader)
        {
            uint listOffset = reader.ReadUInt32();
            if (0 == listOffset) { return null; }
            this.Header.AssertOffsetInDataSection(listOffset);
            long savedPosition = reader.BaseStream.Position;
            reader.BaseStream.Position = listOffset;
            List<string> result = new List<string>();
            // size of the list, in entries
            uint listSize = reader.ReadUInt32();

            for(int index = 0; index < listSize; index++) {
                // index into the type_ids list
                result.Add(GetKnownTypeName(reader, true));
            }
            reader.BaseStream.Position = savedPosition;
            return result;
        }

        private void LoadStrings(BinaryReader reader)
        {
            if (null != this.Strings) { throw new InvalidOperationException(); }
            this.Strings = new List<string>();
            uint stringsListOffset = this.Header.StringIdsDefinition.Offset;
            if (0 == stringsListOffset) { return; }
            uint stringsSize = this.Header.StringIdsDefinition.Size;
            reader.BaseStream.Position = stringsListOffset;
            // TODO : Check alignement.
            for (uint index = 0; index < stringsSize; index++) {
                uint stringOffset = reader.ReadUInt32();
                this.Header.AssertOffsetInDataSection(stringOffset);
                int listPosition = (int)reader.BaseStream.Position;
                reader.BaseStream.Position = stringOffset;
                this.Strings.Add(Helpers.DecodeString(reader));
                reader.BaseStream.Position = listPosition;
            }
            return;
        }

        private void LoadTypes(BinaryReader reader)
        {
            if (null != this.Types) { throw new InvalidOperationException(); }
            this.Types = new List<KnownType>();
            uint typesListOffset = this.Header.TypeIdsDefinition.Offset;
            if (0 == typesListOffset) { return; }
            uint typesSize = this.Header.TypeIdsDefinition.Size;
            reader.BaseStream.Position = typesListOffset;
            // Read descriptor indexes and bind them to the associated
            // string. Also validate string syntax :
            // The string must conform to the syntax for TypeDescriptor.
            long previousStringIndex = -1;
            for (uint index = 0; index < typesSize; index++) {
                uint stringIndex = reader.ReadUInt32();
                if (previousStringIndex >= stringIndex) {
                    throw new ParseException();
                }
                else { previousStringIndex = stringIndex; }
                if (stringIndex >= this.Strings.Count) {
                    throw new ParseException();
                }
                string descriptor = this.Strings[(int)stringIndex];
                if (!Helpers.IsValidTypeDescriptor(descriptor)) {
                    throw new ParseException();
                }
                this.Types.Add(new KnownType(descriptor));
            }
            return;
        }

        public static DexFile Parse(Stream from)
        {
            DexFile result = new DexFile();
            MemoryStream stream = Preload(from);
            BinaryReader reader = new BinaryReader(stream);

            result.Header = Header.Parse(reader);
            // We can directly compute hash thanks to the way the above method
            // set up reader position on return.
            byte[] realHash = ComputeSHA1Hash(stream);
            if (!Helpers.AreEqual(realHash, result.Header._expectedHash)) {
                throw new ParseException();
            }

            // Initial loading.
            List<PendingClassResolution> pendingResolutions;
            result.LoadStrings(reader);
            result.LoadTypes(reader);
            result.LoadPrototypes(reader);
            result.LoadFields(reader);
            result.LoadMethods(reader);
            result.LoadClasses(reader, out pendingResolutions);

            // Second stage resolution.
            foreach(KnownType scannedType in result.Types) {
                if (null != scannedType.Definition) { continue; }
                // This is an external type. Add a dummy definition.
                scannedType.SetDefinition(new ExternalClass(scannedType.FullName));
            }
            foreach (PendingClassResolution resolution in pendingResolutions) {
                string superClassName = resolution.SuperClassName;
                KnownType superClass = result.FindKnownType(superClassName);
                if (null == superClass) { throw new ParseException(); }
                if (superClass.Definition is ExternalClass) {
                    resolution.Class.SetBaseClass(superClass.Definition.Name);
                }
                else {
                    resolution.Class.SetBaseClass(superClass.Definition);
                }
            }
            result.LinkMembersToClass(result.Methods);
            result.LinkMembersToClass(result.Fields);
            return result;
        }

        /// <summary>Load in a new memory stream the full content of the
        /// <paramref name="from"/> stream. On return the initial position
        /// of the <paramref name="from"/> stream is restored.</summary>
        /// <param name="from">Originating stream.</param>
        /// <returns>The resulting memory stream. Position of this stream
        /// is already set to the first stream byte.</returns>
        private static MemoryStream Preload(Stream from)
        {
            long initialPosition = from.Position;

            try {
                from.Position = 0;
                MemoryStream result = new MemoryStream();
                Helpers.StreamCopy(from, result);
                result.Position = 0;
                return result;
            } finally { from.Position = initialPosition; }
        }

        public IField ResolveField(ushort index)
        {
            if (index >= this.Fields.Count) { throw new ArgumentOutOfRangeException(); }
            return this.Fields[index];
        }

        public IMethod ResolveMethod(ushort index)
        {
            if (index >= this.Methods.Count) { throw new ArgumentOutOfRangeException(); }
            return this.Methods[index];
        }

        public string ResolveString(ushort index)
        {
            if (index >= this.Strings.Count) { throw new ArgumentOutOfRangeException(); }
            return this.Strings[index];
        }

        public IType ResolveType(ushort index)
        {
            if (index >= this.Types.Count) { throw new ArgumentOutOfRangeException(); }
            return this.Types[index];
        }
        #endregion

        #region FIELDS
        private static readonly string[] ColonSeparatorAsArray;
        private Dictionary<string, IClass> _externalClasses = new Dictionary<string, IClass>();
        #endregion

        #region INNER CLASSES
        internal enum EncodedValueType : byte
        {
            // value_arg must be 0. data type is byte.
            // signed one-byte integer value
            Byte = 0,
            // value_arg is size - 1 (0 or 1). data type is an array of bytes
            // of variable length defined by value_arg. 
            // signed two-byte integer value, sign-extended
            Short = 2,
            // value_arg is size - 1 (0 or 1). data type is an array of bytes
            // of variable length defined by value_arg. 
            // unsigned two-byte integer value, zero-extended
            Char = 3,
            // value_arg is size - 1 (0 to 3). data type is an array of bytes
            // of variable length defined by value_arg. 
            // signed four-byte integer value, sign-extended
            Int = 4,
            // value_arg is size - 1 (0 to 7). data type is an array of bytes
            // of variable length defined by value_arg. 
            // signed eight-byte integer value, sign-extended
            Long = 6,
            // value_arg is size - 1 (0 to 3). data type is an array of bytes
            // of variable length defined by value_arg. 
            // four-byte bit pattern, zero-extended to the right, and interpreted
            // as an IEEE754 32-bit floating point value
            Float = 16,
            // value_arg is size - 1 (0 to 7). data type is an array of bytes
            // of variable length defined by value_arg. 
            // eight-byte bit pattern, zero-extended to the right, and interpreted
            // as an IEEE754 64-bit floating point value
            Double = 17,
            // value_arg is size - 1 (0 to 3). data type is an array of bytes
            // of variable length defined by value_arg. 
            // unsigned (zero-extended) four-byte integer value, interpreted as an
            // index into the string_ids section and representing a string value
            String = 23,
            // value_arg is size - 1 (0 to 3). data type is an array of bytes
            // of variable length defined by value_arg. 
            // unsigned (zero-extended) four-byte integer value, interpreted as an
            // index into the type_ids section and representing a reflective
            // type/class value
            Type = 24,
            // value_arg is size - 1 (0 to 3). data type is an array of bytes
            // of variable length defined by value_arg. 
            // unsigned (zero-extended) four-byte integer value, interpreted as an
            // index into the field_ids section and representing a reflective field
            // value
            Field = 25,
            // value_arg is size - 1 (0 to 3). data type is an array of bytes
            // of variable length defined by value_arg. 
            // unsigned (zero-extended) four-byte integer value, interpreted as an
            // index into the method_ids section and representing a reflective method
            // value
            Method = 26,
            // value_arg is size - 1 (0 to 3). data type is an array of bytes
            // of variable length defined by value_arg. 
            // unsigned (zero-extended) four-byte integer value, interpreted as an
            // index into the field_ids section and representing the value of an
            // enumerated type constant
            Enum = 27,
            // value_arg must be 0. data type is an encoded array.
            // an array of values, in the format specified by "encoded_array Format"
            // below. The size of the value is implicit in the encoding.
            Array = 28,
            // value_arg must be 0. data type is an encoded annotation.
            // a sub-annotation, in the format specified by "encoded_annotation Format"
            // below. The size of the value is implicit in the encoding.
            Annotation = 29,
            // value_arg must be 0. no data.
            // A null reference value
            Null = 30,
            // value_arg is 0 or 1. no data
            // one-bit value; 0 for false and 1 for true. The bit is represented
            // in the value_arg.
            Boolean = 31,
        }

        private class ExternalClass : BaseClassDefinition, IClass, IAnnotatable
        {
            #region CONSTRUCTORS
            internal ExternalClass(string fullName)
                : base(fullName)
            {
                return;
            }
            #endregion

            #region PROPERTIES
            public override AccessFlags Access
            {
                get { return (AccessFlags)0; }
            }

            public override bool IsExternal
            {
                get { return true; }
            }
            #endregion

            #region METHODS
            public override void SetBaseClass(IClass value)
            {
                throw new InvalidOperationException();
            }

            protected override void SetImplementedInterfaces(List<string> value)
            {
                throw new InvalidOperationException();
            }
            #endregion
        }

        private struct PendingClassResolution
        {
            internal ClassDefinition Class;
            internal string SuperClassName;
            internal List<string> ImplementedInterfacesName;
        }
        #endregion
    }
}
