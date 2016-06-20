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

        internal static string BuildMethodDeclarationString(IMethod from, bool demangle = true)
        {
            if (null == from) { throw new ArgumentNullException(); }
            IPrototype prototype = from.Prototype;
            string returnTypeNamespace = null;
            string returnTypeName = demangle
                ? com.rackham.ApkHandler.Helpers.GetCanonicTypeName(prototype.ReturnType, out returnTypeNamespace)
                : prototype.ReturnType;
            StringBuilder builder = new StringBuilder();
            builder.Append(string.IsNullOrEmpty(returnTypeNamespace)
                ? returnTypeName
                : returnTypeNamespace + "." + returnTypeName)
                .Append(" ")
                .Append(from.Name)
                .Append("(");
            List<string> parameters = prototype.ParametersType;
            if (null != parameters) {
                for(int index = 0; index < parameters.Count; index++) {
                    if (0 < index) { builder.Append(", "); }
                    string parameterTypeName = parameters[index];
                    if (!demangle) { builder.Append(parameterTypeName); }
                    else {
                        string typeNamespace;
                        parameterTypeName = com.rackham.ApkHandler.Helpers.GetCanonicTypeName(parameterTypeName,
                            out typeNamespace);
                        builder.Append((string.IsNullOrEmpty(typeNamespace)
                            ? parameterTypeName
                            : typeNamespace + "." + parameterTypeName));
                    }
                }
            }
            return builder.Append(")").ToString();
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

        internal static AccessFlags GetInterfaceModifiers(AccessFlags candidate)
        {
            return (AccessFlags)(candidate & ClassModifiersMask & ~(AccessFlags.Final));
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
