using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using com.rackham.ApkJava;
using com.rackham.ApkJava.API;
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

            return (first.MethodRelativeOffset + first.InstructionSize) == second.MethodRelativeOffset;
        }

        internal static string BuildMethodDeclarationString(IMethod from)
        {
            if (null == from) { throw new ArgumentNullException(); }
            IPrototype prototype = from.Prototype;
            string returnTypeName = prototype.ReturnType.FullyQualifiedJavaName;
            StringBuilder builder = new StringBuilder();
            builder.Append(returnTypeName)
                .Append(" ")
                .Append(from.Name)
                .Append("(");
            List<IJavaType> parameters = prototype.ParametersType;
            if (null != parameters) {
                for(int index = 0; index < parameters.Count; index++) {
                    if (0 < index) { builder.Append(", "); }
                    builder.Append(parameters[index].FullyQualifiedJavaName);
                }
            }
            return builder.Append(")").ToString();
        }

        internal static string GetClassAndPackageName(IAnnotatableClass item, out string[] packageNameItems)
        {
            if (null == item) { throw new ArgumentNullException(); }
            if (-1 != item.Name.IndexOf('$')) { throw new REException(); }

            string result = item.Name;
            List<string> namespaces = new List<string>();
            for (INamespace scannedNamespace = item.Namespace;
                !scannedNamespace.IsRoot;
                scannedNamespace = scannedNamespace.Parent)
            {
                namespaces.Insert(0, scannedNamespace.Name);
            }
            packageNameItems = (0 == namespaces.Count)
                ? null
                : namespaces.ToArray();
            return result;
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
