using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public static class JavaHelpers
    {

        public static string BuildMethodDeclarationString(IMethod from, bool demangle = true)
        {
            if (null == from) { throw new ArgumentNullException(); }
            IPrototype prototype = from.Prototype;
            string returnTypeNamespace = null;
            string returnTypeName = demangle
                ? JavaHelpers.GetCanonicTypeName(prototype.ReturnType, out returnTypeNamespace)
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
                        parameterTypeName = JavaHelpers.GetCanonicTypeName(parameterTypeName,
                            out typeNamespace);
                        builder.Append((string.IsNullOrEmpty(typeNamespace)
                            ? parameterTypeName
                            : typeNamespace + "." + parameterTypeName));
                    }
                }
            }
            return builder.Append(")").ToString();
        }

        /// <summary>Starting from a full encoded Dalvik class name as defined in "Type descriptor
        /// semantics", resolve to a Java type name and split apart the simple class name and the
        /// namespace name.</summary>
        /// <param name="fullDalvikTypeName"></param>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        public static string GetCanonicTypeName(string fullDalvikTypeName, out string namespaceName)
        {
            int arrayDimensions = 0;

            while (fullDalvikTypeName.StartsWith("[")) {
                arrayDimensions++;
                fullDalvikTypeName = fullDalvikTypeName.Substring(1);
            }
            namespaceName = null;
            string canonicType;
            switch (fullDalvikTypeName) {
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
                    if (!fullDalvikTypeName.StartsWith("L")) { throw new JavaClassParsingException(); }
                    if (!fullDalvikTypeName.EndsWith(";")) { throw new JavaClassParsingException(); }
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

        public static string GetUndecoratedClassName(string candidate)
        {
            return candidate;

            //// TODO : Handle inner classes naming convention.
            //if (('L' != candidate[0]) || (';' != candidate[candidate.Length - 1])) {
            //    return candidate;
            //}
            //return candidate.Substring(1, candidate.Length - 2);
        }
    }
}
