using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using com.rackham.ApkJava.API;
   
namespace com.rackham.ApkJava
{
    /// <summary>A lite version of a Java class file parser.
    /// Actually we are seeking for class definition only.
    /// We are not interested in fetching not interpreting
    /// the byte code.</summary>
    /// <remarks>Based on :
    /// https://docs.oracle.com/javase/specs/jvms/se8/html/jvms-4.html
    /// </remarks>
    internal static class JavaClassFileLiteParser
    {
        private static void AssertPoolTag(this Stream poolStream, ConstantPoolTag expected)
        {
            if (255 == (byte)expected) { throw new ArgumentException(); }
            // We can safely cast to ConstantPoolTag. If read fail the retrieved value
            // will be 255 which is considered an invalid argument.
            ConstantPoolTag received = (ConstantPoolTag)poolStream.ReadByte();
            if (received != expected) { throw new JavaClassParsingException(); }
        }

        internal static IClass Parse(FileInfo classFile)
        {
            if (null == classFile) { throw new ArgumentNullException(); }
            if (!classFile.Exists) { throw new ArgumentException(); }
            try
            {
                using (FileStream input = File.Open(classFile.FullName, FileMode.Open, FileAccess.Read))
                {
                    if (0xCAFEBABE != input.ReadUInt32())
                    {
                        throw new JavaClassParsingException();
                    }
                    // Get rid of version numbers.
                    input.ReadUInt16();
                    input.ReadUInt16();
                    ushort constantPoolCount = input.ReadUInt16();
                    byte[] constantPool = new byte[constantPoolCount];
                    if (constantPoolCount != input.Read(constantPool, 0, constantPoolCount))
                    {
                        throw new JavaClassParsingException();
                    }
                    ushort accessFlags = input.ReadUInt16();
                    ushort thisClass = input.ReadUInt16();
                    ushort superClass = input.ReadUInt16();
                    ushort[] interfacesIndex = ParseArray(input, ReadUInt16);
                    JavaFieldInfo[] fields = ParseArray(input, JavaFieldInfo.ReadItem);
                    JavaMethodInfo[] methods = ParseArray(input, JavaMethodInfo.ReadItem);
                    JavaAttributeInfo[] attributes = ParseArray(input, JavaAttributeInfo.ReadItem);
                    using (MemoryStream poolStream = new MemoryStream(constantPool))
                    {
                        constantPool = null;
                        string thisClassName = poolStream.ReadClassInfo(thisClass);
                        ExternalClass result = new ExternalClass(thisClassName);
                        result.SetBaseClass((0 == superClass)
                            ? BaseClassDefinition.ObjectClassName
                            : poolStream.ReadClassInfo(superClass));
                        List<string> interfaces = new List<string>();
                        for (int index = 0; index < interfacesIndex.Length; index++)
                        {
                            interfaces.Add(poolStream.ReadClassInfo(interfacesIndex[index]));
                        }
                        result.SetImplementedInterfaces(interfaces);
                        for (int index = 0; index < fields.Length; index++)
                        {
                            string fieldDescriptor = poolStream.ReadUtf8(fields[index].descriptor_index);
                            Field newField = new Field(thisClassName,
                                ,
                                poolStream.ReadUtf8(fields[index].name_index));
                            newField.LinkTo(result);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to parse java class file '{0}'. Error : {1}",
                    classFile.FullName, e.Message);
                return null;
            }
            throw new NotImplementedException();
        }

        private static T[] ParseArray<T>(FileStream input, StructReaderDelegate<T> reader)
        {
            ushort resultsCount = input.ReadUInt16();
            T[] result = null;
            if (0 < resultsCount)
            {
                result = new T[resultsCount];
                for (int index = 0; index < resultsCount; index++)
                {
                    result[index] = reader(input);
                }
            }
            return result;
        }

        private static string ReadClassInfo(this Stream poolStream, ushort index)
        {
            poolStream.SeekTo(index);
            poolStream.AssertPoolTag(ConstantPoolTag.Class);
            return ReadUtf8(poolStream, ReadUInt16(poolStream));
        }

        private static byte ReadUInt8(this Stream from)
        {
            return (byte)from.ReadUnsigned(1);
        }

        private static ushort ReadUInt16(this Stream from)
        {
            return (ushort)from.ReadUnsigned(2);
        }

        private static uint ReadUInt32(this Stream from)
        {
            return (uint)from.ReadUnsigned(4);
        }

        private static ulong ReadUnsigned(this Stream from, int length)
        {
            ulong result = 0;
            for (int index = length - 1; 0 <= index; index--)
            {
                int inputByte = from.ReadByte();
                if (-1 == inputByte) { throw new JavaClassParsingException(); }
                result += (ulong)(((byte)inputByte) << (8 * index));
            }
            return result;
        }

        private static string ReadUtf8(this Stream poolStream, ushort index)
        {
            poolStream.SeekTo(index);
            poolStream.AssertPoolTag(ConstantPoolTag.Utf8);
            byte[] buffer = new byte[poolStream.ReadUInt8()];
            if (buffer.Length != poolStream.Read(buffer, 0, buffer.Length))
            {
                throw new JavaClassParsingException();
            }
            return UTF8Encoding.UTF8.GetString(buffer);
        }

        private static void SeekTo(this Stream input, ushort to)
        {
            if (to != input.Seek(to, SeekOrigin.Begin))
            {
                throw new JavaClassParsingException();
            }
        }

        private class ExternalResolvedClass : BaseClassDefinition, IClass
        {
            #region CONSTRUCTORS
            internal ExternalResolvedClass(string fullName)
                : base(fullName)
            {
                return;
            }
            #endregion

            #region PROPERTIES
            public override bool IsExternal
            {
                get { return true; }
            }
            #endregion
        }

        private delegate T StructReaderDelegate<T>(FileStream input);

        private enum ConstantPoolTag : byte
        {
            Class = 7,
            Fieldref = 9,
            Methodref = 10,
            InterfaceMethodref = 11,
            String = 8,
            Integer = 3,
            Float = 4,
            Long = 5,
            Double = 6,
            NameAndType = 12,
            Utf8 = 1,
            MethodHandle = 15,
            MethodType = 16,
            InvokeDynamic = 18,
        }

        private class JavaAttributeInfo
        {
            private JavaAttributeInfo()
            {
                return;
            }

            internal ushort attribute_name_index { get; private set; }
            internal uint attribute_length { get; private set; }
            internal byte[] attributes { get; private set; }

            internal static JavaAttributeInfo ReadItem(FileStream input)
            {
                JavaAttributeInfo result = new JavaAttributeInfo();

                result.attribute_name_index = ReadUInt16(input);
                result.attribute_length = ReadUInt32(input);
                if (0 < result.attribute_length)
                {
                    result.attributes = new byte[result.attribute_length];
                    if (result.attributes.Length != input.Read(result.attributes, 0, result.attributes.Length))
                    {
                        throw new JavaClassParsingException();
                    }
                }
                return result;
            }
        }

        private class JavaFieldInfo
        {
            private JavaFieldInfo()
            {
                return;
            }

            internal ushort access_flags { get; private set; }
            internal ushort name_index { get; private set; }
            internal ushort descriptor_index { get; private set; }
            internal JavaAttributeInfo[] attributes { get; private set; }

            internal static JavaFieldInfo ReadItem(FileStream input)
            {
                JavaFieldInfo result = new JavaFieldInfo();

                result.access_flags = ReadUInt16(input);
                result.name_index = ReadUInt16(input);
                result.descriptor_index = ReadUInt16(input);
                result.attributes = ParseArray(input, JavaAttributeInfo.ReadItem);
                return result;
            }
        }

        private class JavaMethodInfo
        {
            private JavaMethodInfo()
            {
                return;
            }

            internal ushort access_flags { get; private set; }
            internal ushort name_index { get; private set; }
            internal ushort descriptor_index { get; private set; }
            internal JavaAttributeInfo[] attributes { get; private set; }

            internal static JavaMethodInfo ReadItem(FileStream input)
            {
                JavaMethodInfo result = new JavaMethodInfo();

                result.access_flags = ReadUInt16(input);
                result.name_index = ReadUInt16(input);
                result.descriptor_index = ReadUInt16(input);
                result.attributes = ParseArray(input, JavaAttributeInfo.ReadItem);
                return result;
            }
        }

        private class ExternalClass : BaseClassDefinition, IClass
        {
            internal ExternalClass(string fullName)
                : base(fullName)
            {
                return;
            }

            public override bool IsExternal
            {
                get { return true; }
            }
        }
    }
}
