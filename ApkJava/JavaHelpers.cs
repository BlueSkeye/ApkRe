﻿using System;
using System.Collections.Generic;
using System.Text;

using com.rackham.ApkJava.API;

namespace com.rackham.ApkJava
{
    public static class JavaHelpers
    {
        public static string AssertNotEmpty(string value)
        {
            if (!string.IsNullOrEmpty(value)) { return value; }
            throw new ArgumentNullException();
        }

        public static string BuildMethodDeclarationString(IMethod from)
        {
            if (null == from) { throw new ArgumentNullException(); }
            IPrototype prototype = from.Prototype;
            string returnTypeNamespace = null;
            string returnTypeName = prototype.ReturnType.FullyQualifiedBinaryName;
            StringBuilder builder = new StringBuilder();
            builder.Append(string.IsNullOrEmpty(returnTypeNamespace)
                ? returnTypeName
                : returnTypeNamespace + "." + returnTypeName)
                .Append(" ")
                .Append(from.Name)
                .Append("(");
            List<IJavaType> parameters = prototype.ParametersType;
            if (null != parameters) {
                for(int index = 0; index < parameters.Count; index++) {
                    if (0 < index) { builder.Append(", "); }
                    string parameterTypeName = parameters[index].FullyQualifiedBinaryName;
                    builder.Append(parameterTypeName);
                }
            }
            return builder.Append(")").ToString();
        }

        /// <summary>Starting from a full encoded Dalvik class name as defined in "Type descriptor
        /// semantics", resolve to a Java type name and split apart the simple class name and the
        /// namespace name.</summary>
        /// <param name="fullyQualifiedName"></param>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        public static string GetCanonicTypeName(string fullyQualifiedName, out string namespaceName)
        {
            int arrayDimensions = 0;

            while (fullyQualifiedName.StartsWith("[")) {
                arrayDimensions++;
                fullyQualifiedName = fullyQualifiedName.Substring(1);
            }
            namespaceName = null;
            string canonicType;
            switch (fullyQualifiedName) {
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
                    string[] item =
                        fullyQualifiedName.Split(JavaTypeDefinition.NamespaceItemSeparator);
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
    }
}
