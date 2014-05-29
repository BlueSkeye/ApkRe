using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using com.rackham.ApkHandler.API;
using com.rackham.ApkRe.AST;
using com.rackham.ApkRe.ByteCode;

namespace com.rackham.ApkRe
{
    internal static class Helpers
    {
        #region INITIALIZER
        static Helpers()
        {
            JavaSourceModifierByAccessFlag = new Dictionary<AccessFlags, string>();
            JavaSourceModifierByAccessFlag[AccessFlags.Public] = "public";
            JavaSourceModifierByAccessFlag[AccessFlags.Protected] = "protected";
            JavaSourceModifierByAccessFlag[AccessFlags.Private] = "private";
            JavaSourceModifierByAccessFlag[AccessFlags.Abstract] = "abstract";
            JavaSourceModifierByAccessFlag[AccessFlags.Static] = "static";
            JavaSourceModifierByAccessFlag[AccessFlags.Final] = "final";
            JavaSourceModifierByAccessFlag[AccessFlags.Strict] = "strictftp";
            return;
        }
        #endregion

        #region METHODS
        internal static bool AreInstructionAdjacent(DalvikInstruction x, DalvikInstruction y)
        {
            if (null == x) { throw new ArgumentNullException(); }
            if (null == y) { throw new ArgumentNullException(); }
            if (object.ReferenceEquals(x, y)) { throw new InvalidOperationException(); }
            DalvikInstruction first;
            DalvikInstruction second;
            if (x.MethodRelativeOffset < y.MethodRelativeOffset) {
                first = x;
                second = y;
            } else {
                first = y;
                second = x;
            }

            return (first.MethodRelativeOffset + first.BlockSize) == second.MethodRelativeOffset;
        }

        /// <summary>Starting from a full encoded Dalvik class name as defined in "Type descriptor
        /// semantics", resolve to a Java type name and split apart the simple class name and the
        /// namespace name.</summary>
        /// <param name="fullDalvikTypeName"></param>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        internal static string GetCanonicTypeName(string fullDalvikTypeName, out string namespaceName)
        {
            int arrayDimensions = 0;

            while (fullDalvikTypeName.StartsWith("[")) {
                arrayDimensions++;
                fullDalvikTypeName = fullDalvikTypeName.Substring(1);
            }
            namespaceName = null;
            string canonicType;
            switch (fullDalvikTypeName)
            {
                case "V":
                    canonicType = "void";
                    break;
                case "Z":
                    canonicType = "boolean";
                    break;
                case "B":
                    canonicType = "byte";
                    break;
                case "S":
                    canonicType = "short";
                    break;
                case "C":
                    canonicType = "char";
                    break;
                case "I":
                    canonicType = "int";
                    break;
                case "J":
                    canonicType = "long";
                    break;
                case "F":
                    canonicType = "float";
                    break;
                case "D":
                    canonicType = "double";
                    break;
                default:
                    if (!fullDalvikTypeName.StartsWith("L")) { throw new REException(); }
                    if (!fullDalvikTypeName.EndsWith(";")) { throw new REException(); }
                    fullDalvikTypeName = fullDalvikTypeName.Substring(1, fullDalvikTypeName.Length - 2);
                    string[] item = fullDalvikTypeName.Split('/');
                    namespaceName = string.Empty;
                    for (int index = 0; index < (item.Length - 1); index++) {
                        if (0 < index) { namespaceName += "."; }
                        namespaceName += item[index];
                    }
                    canonicType = item[item.Length - 1];
                    break;
            }
            for (int index = 0; index < arrayDimensions; index++) { canonicType += "[]"; }
            return canonicType;
        }

        internal static AccessFlags GetInterfaceModifiers(AccessFlags candidate)
        {
            return (AccessFlags)(candidate & ClassModifiersMask & ~(AccessFlags.Final));
        }

        internal static string GetClassAndPackageName(string fullClassName, out string[] packageNameItems)
        {
            if (string.IsNullOrEmpty(fullClassName)) { throw new ArgumentNullException(); }
            if ('L' != fullClassName[0]) { throw new ArgumentException(); }
            if (';' != fullClassName[fullClassName.Length - 1]) { throw new ArgumentException(); }
            fullClassName = fullClassName.Substring(1, fullClassName.Length - 2);
            if (-1 != fullClassName.IndexOf('$')) { throw new REException(); }

            string[] items = fullClassName.Split('/');
            packageNameItems = new string[items.Length - 1];
            Array.Copy(items, 0, packageNameItems, 0, items.Length - 1);
            return items[items.Length - 1];
        }


        private static AccessFlags GetModifiers(AccessFlags candidate)
        {
            bool isInterface = (0 != (candidate & AccessFlags.Interface));
            return (AccessFlags)(candidate & ClassModifiersMask & ~(isInterface ? AccessFlags.Final : 0));
        }

        internal static string GetModifiersSourceCode(AccessFlags flags)
        {
            AccessFlags modifiers = GetModifiers(flags);
            StringBuilder resultBuilder = new StringBuilder();
            foreach (AccessFlags value in Enum.GetValues(typeof(AccessFlags))) {
                if (0 != (value & modifiers)) {
                    resultBuilder.Append(JavaSourceModifierByAccessFlag[value] + " ");
                }
            }
            return resultBuilder.ToString();
        }
        #endregion

        #region FIELDS
        private static Dictionary<AccessFlags, string> JavaSourceModifierByAccessFlag;
        private static AccessFlags ClassModifiersMask =
            AccessFlags.Public | AccessFlags.Protected | AccessFlags.Private |
            AccessFlags.Abstract | AccessFlags.Static | AccessFlags.Final | AccessFlags.Strict;
        #endregion
    }
}
