﻿//------------------------------------------------------------------------------
//    Odapter - a C# code generator for Oracle packages
//    Copyright(C) 2018 Clay Lipscomb
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.If not, see<http://www.gnu.org/licenses/>.
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Reflection;

namespace Odapter {
    public class Generator {
        #region User Defined Options
        private readonly String _outputPath;
        private readonly String _schema;
        private readonly String _databaseInstance;
        private readonly String _login;
        private readonly String _password;
        private readonly String _baseNamespace = "Schema"; // default

        private String _objectTypeNamespace { get; set; }

        private Boolean IsCSharp30 { get { return Parameter.Instance.CSharpVersion == CSharpVersion.ThreeZero; } }
        private Boolean IsCSharp40 { get { return Parameter.Instance.CSharpVersion == CSharpVersion.FourZero; } }
        #endregion

        #region Member Variables
        private readonly List<String> GeneratedPacakgeRecordTypes = new List<string>();
        private readonly Action<string> _displayMessageMethod;
        #endregion

        #region Constants/Readonly
        // method parameter names - over 30 characters to avoid Oracle clash
        private const String _oracleConnectionParamName = "optionalPreexistingOpenConnection"; // over 30 characters to avoid Oracle clash
        private const String PARAM_NAME_MAP_BY_POSITION                     = "mapColumnToObjectPropertyByPosition";
        private const String PARAM_NAME_ALLOW_UNMAPPED_COLUMNS              = "allowUnmappedColumnsToBeExcluded"; 
        private const String PARAM_NAME_MAXIMUM_ROWS_CURSOR                 = "optionalMaxNumberRowsToReadFromAnyCursor"; 
        private const String PARAM_NAME_CONVERT_COLUMN_NAME_TO_TITLE_CASE   = "convertColumnNameToTitleCaseInCaption"; 

        // local variable names generated from a base name
        private readonly String LOCAL_VAR_NAME_RETURN           = GenerateLocalVariableName(@"ret");
        private readonly String LOCAL_VAR_NAME_READER           = GenerateLocalVariableName(@"rdr");
        private readonly String LOCAL_VAR_NAME_COMMAND          = GenerateLocalVariableName(@"cmd");
        private readonly string LOCAL_VAR_NAME_COMMAND_PARAMS   = GenerateLocalVariableName(@"cmd") + @".Parameters";
        private readonly string LOCAL_VAR_NAME_COMMAND_TRACE    = GenerateLocalVariableName(@"cmdTrace");
        private readonly String LOCAL_VAR_NAME_CONNECTION       = GenerateLocalVariableName(@"conn");
        private readonly string LOCAL_VAR_NAME_ROWS_AFFECTED    = GenerateLocalVariableName(@"rowsAffected");

        private const String FUNC_RETURN_PARAM_NAME = "!RETURN";
        private const String ORCL_UTIL_NAMESPACE = "Odapter";
        private const String ORCL_UTIL_CLASS = "Hydrator";
        public const String APPLICATION_NAME = "Odapter";

        private const string USING = "using";
        private readonly string USING_ORACLE_DATAACCESS_CLIENT = USING + " " + "Oracle." + (Parameter.Instance.IsCSharp40 ? "Managed" : "") + "DataAccess.Client";
        private readonly string USING_ORACLE_DATAACCESS_TYPES = USING + " " + "Oracle." + (Parameter.Instance.IsCSharp40 ? "Managed" : "") + "DataAccess.Types";
        #endregion

        #region Nested Classes
        private class GenericType {
            //public string SchemaPackageNamespace { get; set; }
            public string PackageTypeName { get; set; }
            public string TypeName { get; set; }
            public bool WeaklyTyped { get; set; }
            public GenericType(string packageTypeName, string typeName, bool weaklyTyped) {
                //SchemaPackageNamespace = schemaPackageNamespace;
                PackageTypeName = packageTypeName;
                TypeName = typeName;
                WeaklyTyped = weaklyTyped;
            }
        }
        #endregion

        #region Constructors
        public Generator(String schema, String outputPath, Action<string> messageMethod,
                        String instance, String login, String password, 
                        String baseNamespace,
                        String objectTypeNameSpace) {
            _displayMessageMethod = messageMethod;
            _outputPath = outputPath;
            _schema = schema;
            _databaseInstance = instance;
            _login = login;
            _password = password;
            _baseNamespace = baseNamespace;
            _objectTypeNamespace = objectTypeNameSpace; // must have internally in order to generate "using" for other entity types
        }
        #endregion

        #region Namespace Generation
        /// <summary>
        /// Generate the de facto namespace for the schema, which will include a filter component if provided.
        /// </summary>
        /// <param name="baseNamespace"></param>
        /// <param name="schema"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static string GenerateNamespaceSchema(String baseNamespace, String schema, String filter) {
            return String.IsNullOrEmpty(schema) 
                    ? "" 
                    : (String.IsNullOrEmpty(baseNamespace) 
                        ? ""
                        : baseNamespace + ".") 
                        + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(schema)
                        + (String.IsNullOrEmpty(filter) 
                            ? ""
                            : "." + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(filter)); 
        }

        //public string GenerateNamespaceSchema() { return Generator.GenerateNamespaceSchema(_baseNamespace, _schema, GetFilterValueIfUsedInNaming()); }

        public static string GenerateNamespacePackage(String baseNamespace, String schema, String filter) {
            return String.IsNullOrEmpty(schema) ? "" : Generator.GenerateNamespaceSchema(baseNamespace, schema, filter) + @".Package"; 
        }

        public static string GenerateNamespaceObjectType(String baseNamespace, String schema, String filter) {
            return String.IsNullOrEmpty(schema) ? "" : Generator.GenerateNamespaceSchema(baseNamespace, schema, filter) + @".Type.Object";
        }

        public static string GenerateNamespaceTable(String baseNamespace, String schema, String filter) {
            return String.IsNullOrEmpty(schema) ? "" : Generator.GenerateNamespaceSchema(baseNamespace, schema, filter) + @".Table";
        }

        public static string GenerateNamespaceView(String baseNamespace, String schema, String filter) {
            return String.IsNullOrEmpty(schema) ? "" : Generator.GenerateNamespaceSchema(baseNamespace, schema, filter) + @".View";
        }

        public static string GetFilterValueIfUsedInNaming() {
            return (Parameter.Instance.IsIncludeFilterPrefixInNaming && !String.IsNullOrWhiteSpace(Parameter.Instance.Filter)) ? Parameter.Instance.Filter.Trim() : "";
        }
        #endregion

        #region Base Class Name Generation
        public static string GenerateBaseAdapterClassName(String schema) {
            return String.IsNullOrEmpty(schema) ? "" : CaseConverter.ConvertUnderscoreDelimitedToPascalCase(schema) + "Adapter";
        }

        public static string GenerateBaseRecordClassName(String schema) {
            return String.IsNullOrEmpty(schema) ? "" : CaseConverter.ConvertUnderscoreDelimitedToPascalCase(schema) + "PackageRecord";
        }

        public static string GenerateBaseObjectTypeClassName(String schema) {
            return String.IsNullOrEmpty(schema) ? "" : CaseConverter.ConvertUnderscoreDelimitedToPascalCase(schema) + "ObjectType";
        }

        public static string GenerateBaseTableClassName(String schema) {
            return String.IsNullOrEmpty(schema) ? "" : CaseConverter.ConvertUnderscoreDelimitedToPascalCase(schema) + "Table";
        }

        public static string GenerateBaseViewClassName(String schema) {
            return String.IsNullOrEmpty(schema) ? "" : CaseConverter.ConvertUnderscoreDelimitedToPascalCase(schema) + "View";
        }

        //private string GetEntityNamespace<TEntity>() {
        //    if (typeof(TEntity).Equals(typeof(ObjectType))) {
        //        return ObjectTypeNamespace;
        //    } else if (typeof(TEntity).Equals(typeof(Table))) {
        //        return TableNamespace;
        //    } else if (typeof(TEntity).Equals(typeof(View))) {
        //        return ViewNamespace;
        //    }
        //    return "Undefined_namespace_for_type_" + typeof(TEntity).Name;
        //}
        #endregion

        #region Package Method Generation
        /// <summary>
        /// create C# return type for the method that wraps a procedure
        /// </summary>
        /// <param name="args">Oracle arguments of the proc</param>
        /// <returns></returns>
        private static string GenerateMethodReturnType(IProcedure proc) {
            if (!proc.IsFunction()) return CSharp.VOID;
            return Translater.ConvertOracleArgTypeToCSharpType(proc.Arguments[0], false);//, proc.Arguments.Count > 1 ? proc.Arguments[1] : null);
        }

        /// <summary>
        /// Return a list of the C# generic types of a proc's out cursor arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns>list of types</returns>
        private List<GenericType> GetMethodGenericTypes(IProcedure proc) {
            List<GenericType> genericTypes = new List<GenericType>(); // created empty list

            string cSharpType, packageTypeName;
            foreach (IArgument arg in proc.Arguments) {
                if (arg.DataLevel != 0) continue; // all signature arguments are initially found at 0 data level
                if (arg.DataType == Orcl.REF_CURSOR && arg.InOut.Equals(Orcl.OUT)) { // only out cursors can use generics
                    //Argument nextArg = arg.NextArgument;// (proc.Arguments.IndexOf(arg) + 1 < proc.Arguments.Count ? proc.Arguments[proc.Arguments.IndexOf(arg) + 1] : null);
                    cSharpType = Translater.ConvertOracleArgTypeToCSharpType(arg, false);
                    packageTypeName = arg.NextArgument != null && !String.IsNullOrEmpty(arg.NextArgument.TypeName)
                            //&& (arg.NextArgument.TypeName.StartsWith(Parameter.Instance.Filter)) 
                            && String.IsNullOrWhiteSpace(Parameter.Instance.Filter)
                            && !arg.PackageName.Equals(arg.NextArgument.TypeName)
                        ? Translater.ConvertOracleNameToCSharpName(arg.NextArgument.TypeName, false)
                        : null;
                    if (!genericTypes.Exists(a => a.TypeName == CSharp.ExtractSubtypeFromGenericCollectionType(cSharpType, false)))
                        genericTypes.Add(new GenericType(packageTypeName, CSharp.ExtractSubtypeFromGenericCollectionType(cSharpType, false), 
                            arg.NextArgument == null || arg.NextArgument.DataLevel == arg.DataLevel));
                }
            }
            return genericTypes;
        }

        /// <summary>
        /// Determine all optional Oracle proc params that can be implemented in C# as optional params. For C# 4.0 optional params,
        ///  an optional param must follow all required params. For C# 3.0, there are no optional params.
        /// </summary>
        /// <param name="args">Oracle argument list for function</param>
        /// <returns>A list of the Oracle param names that can be optional in C#</returns>
        private List<String> GetOptionalCSharpParameters(List<IArgument> args) {
            List<String> optionalParamNames = new List<String>();
            if (!IsCSharp30) {
                for (int i = args.Count - 1; i >= 0; i--) { // loop in reverse - C# optinal params must be declared after req params
                    if (args[i].Defaulted) optionalParamNames.Add(args[i].ArgumentName);
                    else break; // quit upon finding first required arg
                }
            }
            return optionalParamNames;
        }

        /// <summary>
        /// Create C# code of methods' arguments with types, comma delimited as in a method signature
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private string GenerateMethodArgumentsCommaDelimited(List<IArgument> args, bool methodHasGenerics, bool dynamicMapping,
                                                            bool commentOutOnWrap, bool excludeTypes) {

            // based on Oracle params, determine all the C# optional params that can be implemented
            List<String> optionalParamNamesInCSharp = GetOptionalCSharpParameters(args);

            // loop arguments and build list
            List<string> argList = new List<string>();
            int argNum = 1; // start our arg numbering at 1 for the sake of the modulus check below

            foreach (IArgument arg in args) {
                if (arg.DataLevel != 0) continue; // all signature arguments are initially found at 0 data level
                if (arg.IsReturnArgument) continue; // ignore return value, only doing true args at this point

                if (arg.DataLevel == 0 && !string.IsNullOrEmpty(arg.ArgumentName)) {
                    argList.Add(
                        (((argNum++ - 5) % 6 == 0) ? "\r\n" + Tab(2) + (commentOutOnWrap ? "//" : "") + Tab(2) : "") // wrap as argument count increases
                        + (arg.InOut.Equals(Orcl.INOUT) ? "ref " : (arg.InOut.Equals(Orcl.OUT) ? "out " : "")) // pass inout/out args as ref/out in C#, respectively
                        + (excludeTypes
                            ? ""
                            : Translater.ConvertOracleArgTypeToCSharpType(arg, false) + " ")
                        + Translater.ConvertOracleNameToCSharpName(arg.ArgumentName, true)
                        + (optionalParamNamesInCSharp.Contains(arg.ArgumentName) ? " = null" : "") // an optional C# 4.0 param defaulted to null
                        );
                }
            }

            // if method is using generics (i.e., proc has an out cursor) then add optional arguments 
            if (methodHasGenerics) {

                if (dynamicMapping) {
                    // mapping arguments with defaults 
                    argList.Add(("\r\n" + Tab(2) + (commentOutOnWrap ? "//" : "") + Tab(2)) // wrap as argument count increases
                        + (excludeTypes ? "" : "bool ") + PARAM_NAME_MAP_BY_POSITION
                        + (excludeTypes
                            ? ""
                            : (IsCSharp30 ? "" : " = false"))
                            );
                    argNum++;
                    argList.Add("" // wrap as argument count increases
                        + (excludeTypes ? "" : "bool ") + PARAM_NAME_ALLOW_UNMAPPED_COLUMNS
                        + (excludeTypes
                            ? ""
                            : (IsCSharp30 ? "" : " = false"))
                            );
                    argNum++;
                } 
            }

            // datatable column name conversion to title case arg
            if (dynamicMapping && !Translater.UseGenericListForCursor) {
                argList.Add((((argNum++ - 5) % 6 == 0) ? "\r\n" + Tab(2) + (commentOutOnWrap ? "//" : "") + Tab(2) : "") // wrap as argument count increases
                    + CSharp.BOOLEAN + " " + PARAM_NAME_CONVERT_COLUMN_NAME_TO_TITLE_CASE + (IsCSharp30 ? "" : " = false"));
            }

            // row count limit argument for any method with cursor cursor (generics or datatable)
            if (methodHasGenerics || (dynamicMapping && !Translater.UseGenericListForCursor)) {
                argList.Add((((argNum++ - 5) % 6 == 0) ? "\r\n" + Tab(2) + (commentOutOnWrap ? "//" : "") + Tab(2) : "") // wrap as argument count increases
                    + CSharp.UINT32 + "? " + PARAM_NAME_MAXIMUM_ROWS_CURSOR + (IsCSharp30 ? "" : " = null"));
            }

            // add optional Oracle connection arg for all methods
            argList.Add((((argNum++ - 5) % 6 == 0) ? "\r\n" + Tab(2) + (commentOutOnWrap ? "//" : "") + Tab(2) : "") // wrap as argument count increases
                + "OracleConnection" + " " + _oracleConnectionParamName + (IsCSharp30 ? "" : " = null"));

            return String.Join(", ", argList.ToArray());
        }

        /// <summary>
        /// Generate constraint code for generic types
        /// </summary>
        /// <param name="genericTypes"></param>
        /// <returns></returns>
        private string GenerateMethodConstraintsCode(List<GenericType> genericTypes, bool dynamicMapping) {
            StringBuilder sb = new StringBuilder("");
            foreach (GenericType gt in genericTypes) {
                sb.AppendLine();
                sb.Append(Tab(4) + "where " + gt.TypeName + " : class"
                    + (dynamicMapping || gt.WeaklyTyped 
                        ? "" 
                        : ", " 
                            + (gt.PackageTypeName == null ? "" : gt.PackageTypeName + ".")
                            + "I" + gt.TypeName.Substring(CSharp.GENERIC_TYPE_PREFIX.Length))
                    + ", new()");
            }
            return sb.ToString();
        }

        private string GenerateRefCursorOutArgumentRetrieveCode(List<GenericType> genericTypesUsed, String cSharpArgType, String cSharpArgName, String oracleArgName,
                int tabIndentCount, bool dynamicMapping = false) {

            String outListSubType = CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, false);
            String returnListSubTypeFullyQualifiedPackageTypeName = null;
            if (genericTypesUsed.Count > 0) {
                GenericType gt = genericTypesUsed.Find(g => g.TypeName == outListSubType && g.PackageTypeName != null);
                if (gt != null) returnListSubTypeFullyQualifiedPackageTypeName = gt.PackageTypeName;
            }

            StringBuilder sb = new StringBuilder("");
            sb.AppendLine(Tab(tabIndentCount) + "if (" + "!((" + CSharp.ORACLE_REF_CURSOR + ")" + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + oracleArgName + "\"].Value).IsNull" + ")");
            sb.AppendLine(Tab(tabIndentCount + 1) + "using (OracleDataReader " + LOCAL_VAR_NAME_READER + " = ((" + CSharp.ORACLE_REF_CURSOR + ")" + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + oracleArgName + "\"].Value).GetDataReader()) {");
            sb.Append(Tab(tabIndentCount + 2) + cSharpArgName + " = ");
            if (dynamicMapping) {
                sb.AppendLine(ORCL_UTIL_CLASS + ".ReadResult"
                    + (cSharpArgType == CSharp.DATATABLE ? "" : "<" + CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, false) + ">")
                    + "(" + LOCAL_VAR_NAME_READER
                            + (cSharpArgType == CSharp.DATATABLE
                                ? ", " + PARAM_NAME_CONVERT_COLUMN_NAME_TO_TITLE_CASE
                                : ", " + PARAM_NAME_MAP_BY_POSITION + ", " + PARAM_NAME_ALLOW_UNMAPPED_COLUMNS)
                                    + ", " + PARAM_NAME_MAXIMUM_ROWS_CURSOR // max rows to read
                    + ");");
            } else {
                sb.AppendLine((returnListSubTypeFullyQualifiedPackageTypeName == null ? "" : returnListSubTypeFullyQualifiedPackageTypeName + ".Instance.")
                    + CSharp.READ_RESULT + "I" + CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, false).Substring(2)
                    + "<" + CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, false) + ">"
                    + "(" + LOCAL_VAR_NAME_READER
                    + ", " + PARAM_NAME_MAXIMUM_ROWS_CURSOR // max rows to read
                    + ");");
            }
            sb.AppendLine(Tab(tabIndentCount + 1) + "}" + " // using OracleDataReader");
            return sb.ToString();
        }

        /// <summary>
        /// Generate code to retrieve an associative array value from an out argument or return
        /// </summary>
        /// <param name="cSharpArgType"></param>
        /// <param name="cSharpArgName"></param>
        /// <param name="oracleArgName"></param>
        /// <param name="tabIndentCount"></param>
        /// <returns></returns>
        private string GenerateAssocArrayOutArgumentRetrieveCode(String cSharpArgType,  String cSharpArgName, IArgument oracleArg, int tabIndentCount) {

            StringBuilder sb = new StringBuilder("");
            sb.AppendLine(Tab(5) + cSharpArgName + " = new " + cSharpArgType.TrimStart('I') + "();");   // instantiate non-interface type
            String oracleArrayCode = "(" + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + (oracleArg.ArgumentName ?? FUNC_RETURN_PARAM_NAME) + "\"].Value as "
                + Translater.ConvertOracleTypeToOdpNetType(oracleArg.NextArgument.DataType)  + "[])";
            sb.AppendLine(Tab(5) + "for (int _i = 0; _i < " + oracleArrayCode + ".Length; _i++)");
            sb.AppendLine(Tab(tabIndentCount + 1) + cSharpArgName + ".Add(" + oracleArrayCode + "[_i].IsNull");

            if (CSharp.IsOdpNetType(CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, false))) {
                sb.AppendLine(Tab(tabIndentCount + 2) + "? " + CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, true) + ".Null ");
            } else {
                sb.AppendLine(Tab(tabIndentCount + 2) + "? (" + CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, false) + ")null ");
            }

            if (CSharp.IsOdpNetType(CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, false))) {
                sb.AppendLine(Tab(tabIndentCount + 2) + ": (" + CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, false) + ")"
                    + "(" + oracleArrayCode + "[_i].ToString()));");
            } else {
                sb.AppendLine(Tab(tabIndentCount + 2) 
                    + ": " + (CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, true).Equals(CSharp.BIG_INTEGER) ? CSharp.BIG_INTEGER + ".Parse" : "Convert.To" + CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, true))
                    + "((" + oracleArrayCode + "[_i].ToString())));");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generate C# code which retrieves data for all out arguments of proc, including return
        /// </summary>
        /// <param name="args"></param>
        /// <param name="parametersVarName"></param>
        /// <returns></returns>
        private string GenerateOutArgumentRetrieveCode(List<IArgument> args, List<GenericType> genericTypesUsed, bool dynamicMapping = false) {
            StringBuilder sb = new StringBuilder("");
            bool prevArgIsAssocArray = false, isAssocArray = false;

            foreach (IArgument arg in args) {
                String cSharpArgType = Translater.ConvertOracleArgTypeToCSharpType(arg, true);          // type defind as not nullable
                String cSharpArgTypeNullable = Translater.ConvertOracleArgTypeToCSharpType(arg, false); 
                String cSharpArgName = (arg.IsReturnArgument ? LOCAL_VAR_NAME_RETURN : Translater.ConvertOracleNameToCSharpName(arg.ArgumentName, true));
                String oracleArgName = (arg.ArgumentName ?? FUNC_RETURN_PARAM_NAME);

                // ignore argument if not an out or return parameter
                if (arg.DataLevel != 0 || !arg.InOut.EndsWith(Orcl.OUT)) continue;

                isAssocArray = (arg.DataType == Orcl.ASSOCIATITVE_ARRAY);
                if (isAssocArray || prevArgIsAssocArray) sb.AppendLine(); // visually delimit assoc array code with blank line

                if (arg.DataType == Orcl.REF_CURSOR) {
                    sb.Append(GenerateRefCursorOutArgumentRetrieveCode(genericTypesUsed, cSharpArgType, cSharpArgName, oracleArgName, 5, dynamicMapping));
                } else if (isAssocArray) {
                    sb.Append(GenerateAssocArrayOutArgumentRetrieveCode(cSharpArgType, cSharpArgName, arg, 5));
                } else { // standard types (built-ins)
                    sb.AppendLine(Tab(5) + cSharpArgName + " = " + LOCAL_VAR_NAME_COMMAND_PARAMS
                        + "[\"" + oracleArgName + "\"].Status == OracleParameterStatus.NullFetched"); // check for null value

                    if (CSharp.IsOdpNetType(cSharpArgType)) {
                        sb.AppendLine(Tab(6) + "? " + cSharpArgType + ".Null"); // assign null value
                        sb.AppendLine(Tab(6) + ": (" + cSharpArgTypeNullable + ")" 
                            + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + oracleArgName + "\"].Value;"); // assign non-null value
                    } else {
                        bool isLobDataType = new List<String> { Orcl.BLOB, Orcl.CLOB, Orcl.NCLOB }.Contains(arg.DataType);
                        sb.AppendLine(Tab(6) + "? (" + (isLobDataType ? cSharpArgType : cSharpArgTypeNullable) + ")null"); // assign null value
                        if (isLobDataType) // assign non-null value 
                            sb.AppendLine(Tab(6) + ": ((" + Translater.ConvertOracleTypeToOdpNetType(arg.DataType) + ")" + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + oracleArgName + "\"].Value).Value;");
                        else
                            sb.AppendLine(Tab(6) + ": Convert.To" + cSharpArgType + "(" + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + oracleArgName + "\"].Value.ToString());");
                    }
                }
                prevArgIsAssocArray = isAssocArray;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generate C# code that binds all arguments to call proc
        /// </summary>
        /// <param name="args"></param>
        /// <param name="parametersVarName"></param>
        /// <returns></returns>
        private string GenerateArgumentBindCode(List<IArgument> args) {
            StringBuilder sb = new StringBuilder("");
            bool prevArgIsAssocArray = false, isAssocArray = false;

            // determine all C# optional params based on Oracle params
            List<String> optionalParamNamesInCSharp = GetOptionalCSharpParameters(args);// new List<String>();

            foreach (IArgument arg in args) {
                string cSharpArgName = Translater.ConvertOracleNameToCSharpName(arg.ArgumentName, true);
                string cSharpArgType = Translater.ConvertOracleArgTypeToCSharpType(arg, /*(args.IndexOf(arg) + 1 < args.Count ? args[args.IndexOf(arg) + 1] : null),*/ true);
                string clientOracleDbType = Translater.ConvertOracleArgTypeToCSharpOracleDbType(arg, (args.IndexOf(arg) + 1 < args.Count ? args[args.IndexOf(arg) + 1] : null));
                isAssocArray = (arg.DataType == Orcl.ASSOCIATITVE_ARRAY);

                if (isAssocArray) sb.AppendLine(); // visually delimit assoc array code with blank line

                if (arg.IsReturnArgument) {
                    // standard init used for all return types
                    sb.AppendLine(Tab(5) + LOCAL_VAR_NAME_COMMAND_PARAMS + ".Add(new OracleParameter("
                        + "\"" + FUNC_RETURN_PARAM_NAME + "\""
                        + ", " + clientOracleDbType
                        + (isAssocArray ? ", " + Parameter.Instance.MaxAssocArraySize.ToString() : "")
                        + (cSharpArgType == CSharp.STRING ? ", " + Translater.GetStringArgBindSize(arg.DataType).ToString() : "") // returning String requires size
                        + ", null"
                        + ", ParameterDirection.ReturnValue));");

                    // and for associative arrays
                    if (isAssocArray) {
                        sb.AppendLine(Tab(5) + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + FUNC_RETURN_PARAM_NAME + "\"].CollectionType = OracleCollectionType.PLSQLAssociativeArray;");
                        // for assoc array of variable length types, set the ArrayBindSize with the maximum length of the type
//                        if (cSharpArgType.Equals(CSharp.LIST_OF_STRING)) {
                        if (CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, true).Equals(CSharp.STRING)) {
                                sb.AppendLine(Tab(5) + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + FUNC_RETURN_PARAM_NAME + "\"].ArrayBindSize = new int[" + Parameter.Instance.MaxAssocArraySize.ToString() + "];");
                            sb.AppendLine(Tab(5) 
                                + "for (int _i = 0; _i < " + Parameter.Instance.MaxAssocArraySize.ToString() + "; _i++) { " + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + FUNC_RETURN_PARAM_NAME + "\"].ArrayBindSize[_i] = "
                                + Translater.GetCharLength(arg) + "; }");
                        }
                    }
                } else if (arg.DataLevel == 0 && !string.IsNullOrEmpty(arg.ArgumentName)) {
                    // determine whether C# parameter can be defined as optional
                    bool isCSharpParamOptional = optionalParamNamesInCSharp.Contains(arg.ArgumentName);

                    // standard init used for all parameter types
                    sb.AppendLine(Tab(5)
                        + (isCSharpParamOptional ? "if (" + cSharpArgName + " != null) " + (isAssocArray ? "{\r\n" + Tab(6) : "") : "")  // do not bind optional arg if not set
                        + LOCAL_VAR_NAME_COMMAND_PARAMS + ".Add(new OracleParameter("
                        + "\"" + arg.ArgumentName + "\""
                        + ", " + clientOracleDbType
                        + (isAssocArray ? ", " + (arg.InOut.EndsWith(Orcl.OUT.ToString()) ? Parameter.Instance.MaxAssocArraySize.ToString() : "(" + cSharpArgName + " == null ? 0 : " + cSharpArgName + ".Count)") : "")
                        + (arg.InOut.EndsWith(Orcl.OUT.ToString()) && cSharpArgType == CSharp.STRING ? ", " + Translater.GetStringArgBindSize(arg.DataType).ToString() : "") // returning String requires size
                        + ", " + (arg.InOut.Equals(Orcl.OUT.ToString()) || isAssocArray ? "null" : cSharpArgName)
                        + ", ParameterDirection." + (arg.InOut.StartsWith(Orcl.IN.ToString()) ? "Input" : "") + (arg.InOut.EndsWith(Orcl.OUT.ToString()) ? "Output" : "") + ")"
                        + ");");

                    // and for associative arrays
                    if (isAssocArray) {
                        string cSharpArgSubTypeNullable = Translater.ConvertOracleArgTypeToCSharpType(args[args.IndexOf(arg) + 1], false); // e.g., Int32? for List<Int32>
                        if (arg.InOut.StartsWith(Orcl.IN.ToString()))
                            sb.AppendLine(Tab(5) + (isCSharpParamOptional ? "\t" : "") + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + arg.ArgumentName + "\"].Value = " 
                                + "(" + cSharpArgName + " == null || " + cSharpArgName + ".Count == 0 ? new "
                                + cSharpArgSubTypeNullable + "[]{} : "
                                    //+ (cSharpArgSubTypeNullable == CSharp.STRING || cSharpArgSubTypeNullable == CSharp.DATE_TIME + "?" ? "null" : "0") + "} : "
                                + cSharpArgName + ".ToArray()" + ");");
                        sb.AppendLine(Tab(5) + (isCSharpParamOptional ? "\t" : "") + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + arg.ArgumentName + "\"].CollectionType = OracleCollectionType.PLSQLAssociativeArray;");
                        // for assoc array of variable length types, set the ArrayBindSize with the maximum length of the type
//                        if (cSharpArgType.Equals(CSharp.LIST_OF_STRING)) {
                        if (CSharp.ExtractSubtypeFromGenericCollectionType(cSharpArgType, true).Equals(CSharp.STRING)) {
                                sb.AppendLine(Tab(5) + (isCSharpParamOptional ? "\t" : "") + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + arg.ArgumentName + "\"].ArrayBindSize = new int[" + Parameter.Instance.MaxAssocArraySize.ToString() + "];");
                            sb.AppendLine(Tab(5) + (isCSharpParamOptional ? "\t" : "")
                                + "for (int _i = 0; _i < " + Parameter.Instance.MaxAssocArraySize.ToString() + "; _i++) { " + LOCAL_VAR_NAME_COMMAND_PARAMS + "[\"" + arg.ArgumentName + "\"].ArrayBindSize[_i] = "
                                + Translater.GetCharLength(arg) + "; }");
                        }
                        if (isCSharpParamOptional) sb.AppendLine(Tab(5) + "}");
                    }
                }

                prevArgIsAssocArray = isAssocArray;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generate complete C# method for a given stored proc
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private string GenerateMethodCode(IProcedure proc, IPackage pack, bool forceDynamicMapping) {
            StringBuilder methodText = new StringBuilder("");
            String methodReturnType = GenerateMethodReturnType(proc);
            List<GenericType> genericTypesUsed = new List<GenericType>();

            // get generic types (for cursors when in given translation mode) used by the method
            if (Translater.UseGenericListForCursor) genericTypesUsed = GetMethodGenericTypes(proc);

            /////////////////////////////////////////////////////////////////////////
            // bypass creation of methods that use certain types of arguments/returns
            String ignoreReason;
            if (proc.IsIgnoredDueToOracleArgumentTypes(out ignoreReason)) {
                methodText.AppendLine();
                methodText.AppendLine(Tab(2) + "// **PROC IGNORED** - " + ignoreReason);
                methodText.Append(Tab(2) + "//" + " public " + methodReturnType + " " + Translater.ConvertOracleProcNameToMethodName(proc, pack));
                if (genericTypesUsed.Count > 0) methodText.Append("<" + String.Join(", ", genericTypesUsed.Select(gt => gt.TypeName).ToList()) + ">");
                methodText.Append("(" + GenerateMethodArgumentsCommaDelimited(proc.Arguments, genericTypesUsed.Count > 0, forceDynamicMapping, true, false) + ")");
                return methodText.ToString();
            }

            // method header
            methodText.AppendLine();
            methodText.Append(Tab(2) + "public " + methodReturnType + " " + Translater.ConvertOracleProcNameToMethodName(proc, pack));

            // if the method is using generics for cursors, add all generic lists to sig
            if (genericTypesUsed.Count > 0) methodText.Append("<" + String.Join(", ", genericTypesUsed.Select(gt => gt.TypeName).ToList())  + ">");

            // arguments
            methodText.Append("(" + GenerateMethodArgumentsCommaDelimited(proc.Arguments, genericTypesUsed.Count > 0, forceDynamicMapping, false, false) + ")");

            // generic constraint
            if (genericTypesUsed.Count > 0) methodText.Append(GenerateMethodConstraintsCode(genericTypesUsed, forceDynamicMapping));

            methodText.Append(" {");
            methodText.AppendLine();

            // create/default return variable and default OUT parameters
            if (proc.IsFunction() || proc.HasOutArgument()) {
                methodText.Append(Tab(3));
                foreach (IArgument arg in proc.Arguments) 
                    if (arg.IsReturnArgument || (arg.DataLevel == 0 && arg.InOut.Equals(Orcl.OUT))) {
                        String cSharpType = Translater.ConvertOracleArgTypeToCSharpType(arg, !arg.IsReturnArgument);
                        String cSharpName = arg.IsReturnArgument ? LOCAL_VAR_NAME_RETURN : Translater.ConvertOracleNameToCSharpName(arg.ArgumentName, true);
                        methodText.Append((arg.IsReturnArgument ? cSharpType + " " : "") + cSharpName +
                             (CSharp.IsValidGenericCollectionType(cSharpType) 
                                ? " = new " + (Translater.CanBeCSharpInterface(arg.DataType)
                                    ? Translater.ConvertOracleArgTypeToCSharpType(arg, !arg.IsReturnArgument, true) 
                                    : cSharpType) + "()" 
                                : " = null") + "; ");
                    }
                methodText.AppendLine();
            }

            // local connection variable
            methodText.AppendLine(Tab(3) + "OracleConnection " + LOCAL_VAR_NAME_CONNECTION + " = " + _oracleConnectionParamName + " ?? GetConnection();");

            // begin body
            methodText.AppendLine(Tab(3) + "try {");

            // start using of OracleCommand
            methodText.AppendLine(Tab(4) + "using (OracleCommand " + LOCAL_VAR_NAME_COMMAND
                + " = new OracleCommand(" + "\"" 
                + _schema.ToUpper() + "."
                + (String.IsNullOrEmpty(proc.PackageName) ? "" : proc.PackageName + ".") 
                + proc.ProcedureName + "\"" + ", " + LOCAL_VAR_NAME_CONNECTION + ")) {");
            methodText.AppendLine(Tab(5) + LOCAL_VAR_NAME_COMMAND + ".CommandType = CommandType.StoredProcedure;");

            // For versions above C# 3.0, bind by name since it is necessary to handle not binding/settting Oracle optional parameters; 
            // the corresponding C# optional params are defaulted to null. C# 3.0 has no optional params and thus we cannot replicate
            // Oracle optional parameters; so we will use the defauult bind by position for 3.0.
            if (!IsCSharp30) methodText.AppendLine(Tab(5) + LOCAL_VAR_NAME_COMMAND + ".BindByName = true;");

            methodText.Append(GenerateArgumentBindCode(proc.Arguments));

            // initialize trace time
            methodText.AppendLine();
            methodText.AppendLine(Tab(5) + "OracleCommandTrace " + LOCAL_VAR_NAME_COMMAND_TRACE + " = "
                + "IsTracing(" + LOCAL_VAR_NAME_COMMAND + ") ? new OracleCommandTrace(" + LOCAL_VAR_NAME_COMMAND + ") : null;");

            // execute proc call
            methodText.AppendLine(Tab(5) + "int " + LOCAL_VAR_NAME_ROWS_AFFECTED + " = " + LOCAL_VAR_NAME_COMMAND + ".ExecuteNonQuery();");

            // set returned values for OUT parameters and return
            methodText.Append(GenerateOutArgumentRetrieveCode(proc.Arguments, genericTypesUsed, forceDynamicMapping));

            // trace completion of command
            methodText.AppendLine(Tab(5) + "if (" + LOCAL_VAR_NAME_COMMAND_TRACE + " != null) TraceCompletion(" + LOCAL_VAR_NAME_COMMAND_TRACE 
                + (proc.ReturnOracleDataType == Orcl.REF_CURSOR 
                    ? ", " + LOCAL_VAR_NAME_RETURN + (methodReturnType == CSharp.DATATABLE ? ".Rows" : "") + ".Count" 
                    : "") + ");");

            // end using of OracleCommand
            methodText.AppendLine(Tab(4) + "}" + " // using OracleCommand" );

            /////////////////
            // finally clause
            methodText.AppendLine(Tab(3) + "} finally {");
            methodText.AppendLine(  Tab(4) + "if (" + _oracleConnectionParamName + " == null) {");
            methodText.AppendLine(      Tab(5) + LOCAL_VAR_NAME_CONNECTION + ".Close();");
            methodText.AppendLine(      Tab(5) + LOCAL_VAR_NAME_CONNECTION + ".Dispose();");
            methodText.AppendLine(  Tab(4) + "}");
            methodText.AppendLine(Tab(3) + "}");
            /////////////////

            // return a value for function
            if (proc.IsFunction()) methodText.AppendLine(Tab(3) + "return " + LOCAL_VAR_NAME_RETURN + ";");

            // close body
            methodText.Append(Tab(2) + "} // " + Translater.ConvertOracleProcNameToMethodName(proc, pack));

            return methodText.ToString();
        }

        /// <summary>
        /// Generate all versions of a method required for a proc 
        /// </summary>
        /// <param name="proc"></param>
        /// <param name="classText"></param>
        private void GenerateAllMethodVersions(IProcedure proc, IPackage pack, ref StringBuilder classText) {

            // if method has at least one cursor, main version of method will use generics 
            if (proc.HasArgumentOfOracleType(Orcl.REF_CURSOR)) {
                // create main method using generics
                Translater.UseGenericListForCursor = true;

                // dynamic mapping
                if ((proc.UsesWeaklyTypedCursor() || Parameter.Instance.IsGenerateDynamicMappingMethodForTypedCursor) && !proc.HasInArgumentOfOracleTypeRefCursor()) {
                    classText.AppendLine(GenerateMethodCode(proc, pack, true));
                }
                // static mapping
                if (!proc.UsesWeaklyTypedCursor()) classText.AppendLine(GenerateMethodCode(proc, pack, false));

                // create extra method (w/o generics) for DataTable version of weakly typed cursors in return/args
                if (proc.UsesWeaklyTypedCursor() && !proc.HasInArgumentOfOracleTypeRefCursor()) {
                    Translater.UseGenericListForCursor = false;
                    classText.AppendLine(GenerateMethodCode(proc, pack, true));
                }
            } else {
                // just create basic non-generic method 
                classText.AppendLine(GenerateMethodCode(proc, pack, false));
            }
        }
        #endregion

        #region Base Class Generation
        private string GenerateBaseEntityClass(string baseClassName, string classNamespace, string ancestorClassName) {
            StringBuilder classText = new StringBuilder("");

            // method header
            classText.AppendLine(Tab() + CSharp.ATTRIBUTE_SERIALIZABLE);
            classText.AppendLine(Tab() + "public abstract class " + baseClassName + (String.IsNullOrEmpty(ancestorClassName) ? "" : " : " + ancestorClassName) + " {");
            classText.AppendLine();
            classText.AppendLine(Tab() + "}" + " // " + baseClassName);

            return classText.ToString();
        }
        #endregion

        #region Package Record Type Generation
        private string GenerateRecordTypeReadResultMethod(IPackageRecord rec) {
            String cSharpType = rec.CSharpType;
            
            StringBuilder classText = new StringBuilder("");
            String interfaceName = "I" + cSharpType;
            String methodName = CSharp.READ_RESULT + interfaceName;
            String genericTypeParam = CSharp.GENERIC_TYPE_PREFIX + cSharpType;
            String paramNameOracleReader = "rdr"; // Oracle clash not possible
            String returnType = CSharp.GenericCollectionOf(Translater.CSharpTypeUsedForOracleRefCursor, genericTypeParam);

            // signature
            classText.AppendLine(Tab(2) + "public " + returnType + " " + methodName + "<" + genericTypeParam + ">"
                + "(OracleDataReader " + paramNameOracleReader + "" 
                + ", " + CSharp.UINT32 + "? " + PARAM_NAME_MAXIMUM_ROWS_CURSOR + (Parameter.Instance.IsCSharp30 ? "" : " = " + CSharp.NULL) + ")");
            classText.AppendLine(Tab(4) + "where " + genericTypeParam + " : class, " + interfaceName + ", new()  " + " {");

            classText.AppendLine(Tab(3) + returnType + " " + LOCAL_VAR_NAME_RETURN + " = new " + CSharp.GenericCollectionOf(CSharp.LIST_OF_T, genericTypeParam) + "();");

            classText.AppendLine(Tab(3) + "if (" + paramNameOracleReader + " != " + CSharp.NULL + " && " + paramNameOracleReader + ".HasRows) {");
            classText.AppendLine(Tab(4) + "while (" + paramNameOracleReader + ".Read()) {"); 
            classText.AppendLine(Tab(5) + genericTypeParam + " obj = new " + genericTypeParam + "();");
            foreach (IField f in rec.Attributes) { // loop through all fields
                classText.Append(Tab(5) + "if (!" + paramNameOracleReader + ".IsDBNull(" + f.MapPosition + ")) "
                    + "obj." + Translater.ConvertOracleRecordFieldNameToCSharpPropertyName(f.Name, rec.Name, false) + " = ");

                if (f.CSharpType.TrimEnd('?').Equals(CSharp.DECIMAL) && Orcl.IsOracleNumberEquivalent(f.AttrType)) {
                    classText.Append("(Decimal?)OracleDecimal.SetPrecision(" + paramNameOracleReader + ".GetOracleDecimal(" + f.MapPosition.ToString() + "), 29)");
                } else if (CSharp.IsOdpNetType(f.CSharpType)) {
                    classText.Append("(" + f.CSharpType + ")" + paramNameOracleReader + ".GetOracleValue(" + f.MapPosition.ToString() + ")"); // ODP.NET
                } else if ((new List<String> { Orcl.BLOB, Orcl.CLOB, Orcl.NCLOB }).Contains(f.AttrType)) {
                    classText.Append(paramNameOracleReader + "." + CSharp.GET_ORACLE + CaseConverter.ConvertToCapitalized(f.AttrType.Substring(f.AttrType.Length - 4, 4)) + "(" + f.MapPosition.ToString() + ").Value");
                } else {
                    classText.Append("Convert.To" + f.CSharpType.TrimEnd('?') + "(" + paramNameOracleReader + ".GetValue(" + f.MapPosition.ToString() + "))"); // primitive
                }
                classText.AppendLine(";");
            }
            classText.AppendLine(Tab(5) + LOCAL_VAR_NAME_RETURN + ".Add(obj);");
            classText.AppendLine(Tab(5) + "if (" + PARAM_NAME_MAXIMUM_ROWS_CURSOR + " != " + CSharp.NULL + " && " + LOCAL_VAR_NAME_RETURN + ".Count >= " + PARAM_NAME_MAXIMUM_ROWS_CURSOR + ") break;");
            classText.AppendLine(Tab(4) + "}");
            classText.AppendLine(Tab(3) + "}");

            classText.AppendLine(Tab(3) + "return " + LOCAL_VAR_NAME_RETURN + ";");
            classText.AppendLine(Tab(2) + "} // " + methodName);
            return classText.ToString();
        }

        private void WriteBaseEntityClasses(string baseClassName) {
            string fileName = _outputPath + @"\" + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema)
                + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(GetFilterValueIfUsedInNaming()) + @"BaseEntity.cs";

            try {
                StreamWriter outFile = new StreamWriter(fileName);

                StringBuilder headerText = new StringBuilder("");
                headerText = new StringBuilder("");
                headerText.AppendLine(Comment.COMMENT_AUTO_GENERATED_FOR_BASE_DTO);
                headerText.AppendLine("using System;");
                headerText.AppendLine(USING_ORACLE_DATAACCESS_CLIENT + ";");
                headerText.AppendLine();

                outFile.Write(headerText);

                // namespace should be at schema level to avoid class name clashes
                outFile.WriteLine("namespace " + Parameter.Instance.NamespaceSchema + " {");

                // determine the class name of the base entity
                string baseEntityClassName = CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema) + "Entity";

                // create all base entity classes
                outFile.WriteLine(GenerateBaseEntityClass(baseEntityClassName,
                    Parameter.Instance.NamespaceSchema, null));
                outFile.WriteLine(GenerateBaseEntityClass(CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema) + @"PackageRecord",
                    Parameter.Instance.NamespaceSchema, baseEntityClassName));
                outFile.WriteLine(GenerateBaseEntityClass(CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema) + @"Table",
                    Parameter.Instance.NamespaceSchema,  baseEntityClassName));
                outFile.WriteLine(GenerateBaseEntityClass(CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema) + @"View",
                    Parameter.Instance.NamespaceSchema, baseEntityClassName));
                outFile.WriteLine(GenerateBaseEntityClass(CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema) + @"ObjectType",
                    Parameter.Instance.NamespaceSchema, baseEntityClassName));

                // close namespace 
                outFile.Write("" + "} // " + Parameter.Instance.NamespaceSchema);

                outFile.Close();
            } catch (UnauthorizedAccessException) {
                DisplayMessage(Message.BASE_PERMISSION_ERROR_MSG + Path.GetFileName(fileName));
            } catch (Exception e) {
                DisplayMessage("Error writing " + Path.GetFileName(fileName) + " - " + e.Message);
            }
        }
        #endregion

        #region Package Class Generation
        private string GenerateBasePackageClass(string className) {
            StringBuilder classText = new StringBuilder("");

            // method header
            classText.AppendLine("\t" + "public abstract class " + className + " {");

            // default GetConnectionString()
            classText.AppendLine(Tab(2) + "protected string GetConnectionString() { return \"data source=" 
                + _databaseInstance + ";user id=" + _login + ";password=" + _password + ";enlist=false\"; }");

            // default GetConnection()
            classText.AppendLine();
            classText.AppendLine(Tab(2) + "protected OracleConnection GetConnection() {");
            classText.AppendLine("\t\t\t" + "OracleConnection connection = new OracleConnection(GetConnectionString());");
            classText.AppendLine("\t\t\t" + "connection.Open();");
            classText.AppendLine("\t\t\t" + "return connection;");
            classText.AppendLine(Tab(2) + "}");

            // default IsTracing()
            classText.AppendLine();
            classText.AppendLine(Tab(2) + "/// <summary>");
            classText.AppendLine(Tab(2) + "/// Determine if completion of OracleCommand execution should be traced (hook)");
            classText.AppendLine(Tab(2) + "/// </summary>");
            classText.AppendLine(Tab(2) + "/// <param name=\"cmd\">An OracleCommand prepared for executing</param>");
            classText.AppendLine(Tab(2) + "/// <returns>true if command should be traced</returns>");
            classText.AppendLine(Tab(2) + "protected bool IsTracing(OracleCommand cmd) {");
            classText.AppendLine("\t\t\t" + "return false;");
            classText.AppendLine(Tab(2) + "}");

            // default Trace()
            classText.AppendLine();
            classText.AppendLine(Tab(2) + "/// <summary>");
            classText.AppendLine(Tab(2) + "/// Perform trace functionality for a completed OracleCommand (hook)");
            classText.AppendLine(Tab(2) + "/// </summary>");
            classText.AppendLine(Tab(2) + "/// <param name=\"cmdTrace\">An OracleCommandTrace just executed</param>");
            classText.AppendLine(Tab(2) + "/// <param name=\"returnRowCount\">Row count returned in cursor</param>");
            classText.AppendLine(Tab(2) + "protected void TraceCompletion(Odapter.OracleCommandTrace cmdTrace, int? returnRowCount) {");
            classText.AppendLine("\t\t\t" + "// stop the timer first");
            classText.AppendLine("\t\t\t" + "cmdTrace.Stopwatch.Stop();");
            classText.AppendLine("\t\t\t" + "// trace logic goes here");
            classText.AppendLine("\t\t\t" + "return;");
            classText.AppendLine(Tab(2) + "}");

            classText.AppendLine();
            classText.AppendLine(Tab(2) + "/// <summary>");
            classText.AppendLine(Tab(2) + "/// Perform trace functionality for a completed OracleCommand (hook)");
            classText.AppendLine(Tab(2) + "/// </summary>");
            classText.AppendLine(Tab(2) + "/// <param name=\"cmdTrace\">An OracleCommandTrace just executed</param>");
            classText.AppendLine(Tab(2) + "protected void TraceCompletion(Odapter.OracleCommandTrace cmdTrace) {");
            classText.AppendLine("\t\t\t" + "TraceCompletion(cmdTrace, null);");
            classText.AppendLine("\t\t\t" + "return;");
            classText.AppendLine(Tab(2) + "}");

            classText.AppendLine(Tab() + "}" + " // " + className);

            return classText.ToString();
        }

        private void WriteBasePackageClass(string baseClassName) {
            string fileName = _outputPath + @"\" + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema) 
                + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(GetFilterValueIfUsedInNaming()) + "BaseAdapter.cs";

            try {
                StreamWriter outFile = new StreamWriter(fileName);

                StringBuilder headerText = new StringBuilder("");
                headerText = new StringBuilder("");
                headerText.AppendLine(Comment.COMMENT_AUTO_GENERATED_FOR_BASE_ADAPTER);
                headerText.AppendLine("using System;");
                headerText.AppendLine(USING_ORACLE_DATAACCESS_CLIENT + ";");
                headerText.AppendLine();
                outFile.Write(headerText);

                // namespace should be at schema level
                outFile.WriteLine("namespace " + Parameter.Instance.NamespaceSchema + " {");

                // create base package manager class
                outFile.WriteLine(GenerateBasePackageClass(baseClassName));

                // close namespace for 
                outFile.Write("" + "} // " + Parameter.Instance.NamespaceSchema);

                outFile.Close();
            } catch (UnauthorizedAccessException) {
                DisplayMessage(Message.BASE_PERMISSION_ERROR_MSG + Path.GetFileName(fileName));
            } catch (Exception e) {
                DisplayMessage("Error writing " + Path.GetFileName(fileName) + " - " + e.Message);
            }
        }

        private void WritePackageClasses(List<IPackage> packages, IList<IPackageRecord> records, 
            string packageNamespace, string ancestorAdapterClassName, bool partialPackage, string ancestorRecordTypeClassName) {

            if (packages.Count == 0) return;

            string fileName = _outputPath + @"\" + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema)
                + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(GetFilterValueIfUsedInNaming()) + "Package.cs";
            DisplayMessage("Coding packages (" + fileName.Substring(fileName.LastIndexOf('\\') + 1) + ")...");

            try {
                StreamWriter outFilePackage = new StreamWriter(fileName);

                Comment.WriteAutoGeneratedComment(outFilePackage);

                StringBuilder headerText = new StringBuilder("");

                // create using statements

                // package file
                headerText = new StringBuilder("");
                headerText.AppendLine("using System;");
                headerText.AppendLine("using System.Collections.Generic;");
                headerText.AppendLine("using System.Data;");
                headerText.AppendLine("using System.Data.Common;");
                headerText.AppendLine(USING_ORACLE_DATAACCESS_CLIENT + ";");
                headerText.AppendLine(USING_ORACLE_DATAACCESS_TYPES + ";");
                headerText.AppendLine("using System.Collections;");
                headerText.AppendLine("using System.Diagnostics;");
                headerText.AppendLine("using System.Runtime.Serialization;");
                headerText.AppendLine("using System.Xml;");
                headerText.AppendLine("using System.Xml.Serialization;");
                headerText.AppendLine("using System.Linq;");
                headerText.AppendLine("using " + ORCL_UTIL_NAMESPACE + ";");
                outFilePackage.Write(headerText);

                outFilePackage.WriteLine();

                // write namespace 
                outFilePackage.WriteLine("namespace " + packageNamespace + " {");

                foreach (IPackage pack in packages) {
                    //DisplayMessage("Coding package " + pack.PackageName);
                    String className = Translater.ConvertOracleNameToCSharpName(pack.PackageName, false);
                    StringBuilder classText = new StringBuilder("");
    
                    // class definition
                    classText.AppendLine();
                    classText.AppendLine(Tab() + "public sealed " + (partialPackage ? "partial " : "") + "class " + className + " : " + Parameter.Instance.NamespaceSchema + "." + ancestorAdapterClassName + " {");

                    // created as Singleton
                    classText.AppendLine(Tab(2) + "private " + className + "() { }");
                    classText.AppendLine(Tab(2) + "private static readonly " + className + " _instance = new " + className + "();");
                    classText.AppendLine(Tab(2) + "public static " + className + " Instance { get { return _instance; } }");

                    // for each record type in this package
                    int i = 0;
                    foreach (IPackageRecord rec in records
                        .Where(r => (r.PackageName ?? "").Equals(pack.PackageName) || (r.Name ?? "").Equals(pack.PackageName))
                        .GroupBy(r => new { r.Name, r.CSharpType } )
                        .Select(g => g.First())
                        .ToList()) {

                        // unless otherwise specified, skip creation of records derived from a package outside of the filter or schema
                        if (!Parameter.Instance.IsDuplicatePackageRecordOriginatingOutsideFilterAndSchema) {
                            // owned by another schema
                            if (!(rec.Owner ?? "").Equals(pack.Owner)) continue;

                            // owned by package within schema but was filtered out 
                            if (!packages.Exists(p => p.PackageName.Equals(rec.Name))) continue;
                        }

                        // always skip record creation if owned by another package *within* both the filter and schema
                        if (!(rec.Name ?? "").Equals(pack.PackageName) 
                            && packages.Exists(p => p.PackageName.Equals(rec.Name))) { // package of origin of record being created
                            i++;
                            continue;
                        }

                        String reasonMsg;
                        if (!Translater.IsIgnoredDueToOracleTypes(rec, out reasonMsg)) {
                            // create interface for record class
                            classText.AppendLine();
                            classText.Append(GenerateEntityInterface(rec, 1));
                        }

                        // create DTO 
                        classText.AppendLine();
                        classText.Append(GenerateEntityClass<IField>(rec, ancestorRecordTypeClassName, 
                            Parameter.Instance.IsSerializablePackageRecord, Parameter.Instance.IsPartialPackage,
                            Parameter.Instance.IsDataContractPackageRecord, Parameter.Instance.IsXmlElementPackageRecord, 2));

                        if (!Translater.IsIgnoredDueToOracleTypes(rec, out reasonMsg)) {
                            // create custom reader
                            classText.AppendLine();
                            classText.Append(GenerateRecordTypeReadResultMethod(rec));
                        }
                    }

                    // create method for each package proc
                    foreach (IProcedure proc in pack.Procedures) GenerateAllMethodVersions(proc, pack, ref classText);
                    classText.AppendLine(Tab() + "} // " + className);

                    // write entire class to file
                    outFilePackage.Write(classText);
                }

                // close class and namespace for package
                outFilePackage.Write("" + "} // " + packageNamespace);

                outFilePackage.Close();
            } catch (UnauthorizedAccessException) {
                DisplayMessage(Message.BASE_PERMISSION_ERROR_MSG + Path.GetFileName(fileName));
            } catch (Exception e) {
                DisplayMessage("Error writing " + Path.GetFileName(fileName) + " - " + e.Message);
            }
        }
        #endregion

        #region Entity Generation (Record Type, Object Type, Table, View)
        /// <summary>
        /// Generate the class for an Oracle entity 
        /// </summary>
        /// <typeparam name="TEntityAttribute"></typeparam>
        /// <param name="entity"></param>
        /// <param name="ancestorClassName">Use as ancestor class if there is no ancestor class already defined in Oracle (e.g., object type)</param>
        /// <param name="isSerializable"></param>
        /// <param name="isPartial"></param>
        /// <param name="tabIndentCount"></param>
        /// <returns></returns>
        private string GenerateEntityClass<TEntityAttribute>(IEntity entity, string ancestorClassName, bool isSerializable, bool isPartial, 
            bool isDataContract, bool isXmlElement, int tabIndentCount) {

            String className = entity.CSharpType ?? Translater.ConvertOracleNameToCSharpName(entity.EntityName, false);
            bool isPackageRecord = entity is IPackageRecord;
            StringBuilder classText = new StringBuilder("");

            bool isInstantiable = true;         // only object type can be non-instantiable
            string dbAncestorTypeName = null;   // only object type can have a database ancestor
            if (entity is IObjectType) {
                isInstantiable = ((IObjectType)entity).Instantiable;
                dbAncestorTypeName = ((IObjectType)entity).DbAncestorTypeName;
            }

            String classFirstLine = "public" + (isInstantiable ? "" : " abstract") + (isPartial ? " partial" : "") + " class " + className
                + (!String.IsNullOrEmpty(dbAncestorTypeName)
                        ? " : " + Translater.ConvertOracleNameToCSharpName(dbAncestorTypeName, false) // Oracle ancestor gets precedence
                        : (!String.IsNullOrEmpty(ancestorClassName)
                            ? " : " + Parameter.Instance.NamespaceSchema + "." + ancestorClassName + (isPackageRecord ? ", " + "I" + className : "")
                            : "")) // user defined ancestor
                + " {"; // start entity type class;

            /////////////////////////////////////////////////////////////////////////////
            // bypass creation of package records that using unimplemented Oracle types
            String ignoreReason;
            if (isPackageRecord && Translater.IsIgnoredDueToOracleTypes(entity, out ignoreReason)) {
                classText.AppendLine(Tab(2) + "// **RECORD IGNORED** - " + ignoreReason);
                classText.AppendLine(Tab(2) + "// " + classFirstLine);
                return classText.ToString();
            }

            // C# attributes: DataContract, Serializable
            if (isDataContract || isSerializable) classText.Append(Tab(tabIndentCount));
            if (isDataContract) classText.Append(GenerateDataContractAttribute());
            if (isSerializable) classText.Append(CSharp.ATTRIBUTE_SERIALIZABLE);
            if (isDataContract || isSerializable) classText.AppendLine();

            classText.AppendLine(Tab(tabIndentCount) + classFirstLine);

            if (isPartial) classText.AppendLine(Tab(tabIndentCount + 1) + "private " + CSharp.BYTE + " " + "propertyToEnsuresPartialClassNamesAreUniqueAtCompileTime" + " { get; set; }");

            foreach (IEntityAttribute attr in entity.Attributes) { // generate all attributes
                String nonPublicMemberName = Translater.ConvertOracleNameToCSharpName(attr.AttrName, true);
                String cSharpType = attr.CSharpType ?? 
                    Translater.ConvertOracleTypeToCSharpType(Translater.BuildAggregateOracleType(attr), attr.AttrName, false, null);
                if (attr.AttrTypeOwner != null && !attr.AttrTypeOwner.Equals(entity.Owner) && !attr.AttrTypeOwner.Equals("SYS")) {
                    cSharpType = GenerateNamespaceObjectType(_baseNamespace, attr.AttrTypeOwner, GetFilterValueIfUsedInNaming()) + "." + cSharpType;
                }

                //if (isRecordType && !MapCursorColumnsByName) classText.AppendLine(("\t") + "\t\t[MapAttribute(Position = " + ((Field)attr).MapPosition.ToString() + ")]");
                // C# attributes
                if (isDataContract || isXmlElement) classText.Append(Tab(tabIndentCount + 1));
                if (isDataContract) classText.Append("[DataMember(Order=" + attr.Position.ToString() 
                    + ", IsRequired=" + (attr.Nullable ? "false" : "true") + ")]");
                if (isXmlElement) classText.Append("[XmlElement(Order=" + attr.Position.ToString() + ", IsNullable=true)]");
                if (isDataContract || isXmlElement) classText.AppendLine();

                classText.Append(Tab(tabIndentCount + 1) + "public virtual "
                    + (attr.ContainerClassName == null ? "" : attr.ContainerClassName + ".")
                    + cSharpType + " " + Translater.ConvertOracleNameToCSharpName(attr.AttrName, false)
                    + (Parameter.Instance.IsUseAutoImplementedProperties 
                        ? " { get; set; }"
                        : " { get { return " + nonPublicMemberName + "; } set { " + nonPublicMemberName + " = value; } }"));
                classText.AppendLine(Parameter.Instance.IsUseAutoImplementedProperties 
                    ? ""
                    : " protected " + (attr.ContainerClassName == null ? "" : attr.ContainerClassName + ".") + cSharpType + " " + nonPublicMemberName + ";");
            }

            classText.AppendLine(Tab(tabIndentCount) + "} // " + className); // end entity type class
            return classText.ToString();
        }

        /// <summary>
        /// Generate the interface for an entity class
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="tabIndentCount">number of tabs to indent</param>
        /// <returns></returns>
        private string GenerateEntityInterface(IEntity entity, int tabIndentCount) {

            String interfaceName = entity.CSharpType ?? Translater.ConvertOracleNameToCSharpName(entity.EntityName, false);

            StringBuilder classText = new StringBuilder("");
            classText.AppendLine(Tab(tabIndentCount + 1) + "public" + " interface I" + interfaceName + " {"); // start record interface
            foreach (IEntityAttribute attr in entity.Attributes) { // loop through all fields
                String cSharpType = attr.CSharpType ?? Translater.ConvertOracleTypeToCSharpType(attr.AttrType, attr.AttrName, false, null);
                classText.AppendLine(Tab(tabIndentCount + 2) + (attr.ContainerClassName == null ? "" : attr.ContainerClassName + ".") + cSharpType 
                    + " " + Translater.ConvertOracleNameToCSharpName(attr.AttrName, false)
                    + " { get; set; }");
            }
            classText.AppendLine(Tab(tabIndentCount + 1) + "} // I" + interfaceName); // end record interface
            return classText.ToString();
        }

        private void WriteUnpackagedEntityClasses<TEntity, TEntityAttribute>(List<TEntity> entities, string entityNamespace, string ancestorClassName, 
            bool isSerializable, bool isPartial, bool isDataMember, bool isXmlElement)
            where TEntity : IEntity
            where TEntityAttribute : IEntityAttribute {

            string fileName = _outputPath + @"\" + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(_schema)
                + CaseConverter.ConvertUnderscoreDelimitedToPascalCase(GetFilterValueIfUsedInNaming()) + typeof(TEntity).Name + ".cs";

            DisplayMessage("Coding " + typeof(TEntity).Name.ToLower() + "s (" + fileName.Substring(fileName.LastIndexOf('\\') + 1) + ")...");

            try {
                StreamWriter outFile = new StreamWriter(fileName);

                Comment.WriteAutoGeneratedComment(outFile);

                // create using statements
                StringBuilder headerText = new StringBuilder("");
                headerText.AppendLine("using System;");
                headerText.AppendLine("using System.Runtime.Serialization;");
                headerText.AppendLine("using System.Xml;");
                headerText.AppendLine("using System.Xml.Serialization;");
                headerText.AppendLine(USING_ORACLE_DATAACCESS_TYPES + ";");
                if (typeof(TEntity).Equals(typeof(Table)) || typeof(TEntity).Equals(typeof(View)))
                    headerText.AppendLine("using " + _objectTypeNamespace + ";"); // tables and views need access to object type in case column uses one as a type
                outFile.Write(headerText);
                outFile.WriteLine();

                // write namespace 
                outFile.WriteLine("namespace " + entityNamespace + " {");

                foreach (TEntity entity in entities) {
                    if (entities.IndexOf(entity) != 0) outFile.WriteLine();
                    outFile.Write(GenerateEntityClass<TEntityAttribute>(entity, ancestorClassName, isSerializable, isPartial, isDataMember, isXmlElement, 1));
                }

                // close class and namespace for package
                outFile.Write("" + "} // " + entityNamespace);
                outFile.Close();
            } catch (UnauthorizedAccessException) {
                DisplayMessage(Message.BASE_PERMISSION_ERROR_MSG + Path.GetFileName(fileName));
            } catch (Exception e) {
                DisplayMessage("Error writing " + Path.GetFileName(fileName) + " - " + e.Message);
            }
        }

        private void WriteObjectTypeClasses(List<ObjectType> objectTypes, string entityNamespace, string ancestorClassName) {
            WriteUnpackagedEntityClasses<ObjectType, ObjectTypeAttribute>(objectTypes, entityNamespace, ancestorClassName,
                Parameter.Instance.IsSerializableObjectType, Parameter.Instance.IsPartialObjectType, Parameter.Instance.IsDataContractObjectType, Parameter.Instance.IsXmlElementObjectType);
        }

        private void WriteTableClasses(List<Table> tables, string entityNamespace, string ancestorClassName) {
            WriteUnpackagedEntityClasses<Table, Column>(tables, entityNamespace, ancestorClassName,
                Parameter.Instance.IsSerializableTable, Parameter.Instance.IsPartialTable, Parameter.Instance.IsDataContractTable, Parameter.Instance.IsXmlElementTable);
        }

        private void WriteViewClasses(List<View> views, string entityNamespace, string ancestorClassName) {
            WriteUnpackagedEntityClasses<View, Column>(views, entityNamespace, ancestorClassName,
                Parameter.Instance.IsSerializableView, Parameter.Instance.IsPartialView, Parameter.Instance.IsDataContractView, Parameter.Instance.IsXmlElementView);
        }
        #endregion

        /// <summary>
        /// Generator entry point
        /// </summary>
        /// <param name="displayMessageMethod">Method used to display message in UI</param>
        public static void Run(Action<String> displayMessageMethod) {

            if (Parameter.Instance.IsGeneratePackage || Parameter.Instance.IsGenerateObjectType || Parameter.Instance.IsGenerateTable || Parameter.Instance.IsGenerateView) {

                // Set these options first since Loader does some translation. In the future, we need to modify Loader to do no translation (if possible).
                Translater.CSharpTypeUsedForOracleRefCursor = Parameter.Instance.CSharpTypeUsedForOracleRefCursor;
                Translater.CSharpTypeUsedForOracleAssociativeArray = Parameter.Instance.CSharpTypeUsedForOracleAssociativeArray;
                Translater.CSharpTypeUsedForOracleInteger = Parameter.Instance.CSharpTypeUsedForOracleInteger;
                Translater.CSharpTypeUsedForOracleNumber = Parameter.Instance.CSharpTypeUsedForOracleNumber;
                Translater.CSharpTypeUsedForOracleDate = Parameter.Instance.CSharpTypeUsedForOracleDate;
                Translater.CSharpTypeUsedForOracleTimeStamp = Parameter.Instance.CSharpTypeUsedForOracleTimeStamp;
                Translater.CSharpTypeUsedForOracleIntervalDayToSecond = Parameter.Instance.CSharpTypeUsedForOracleIntervalDayToSecond;
                Translater.CSharpTypeUsedForOracleBlob = Parameter.Instance.CSharpTypeUsedForOracleBlob;
                Translater.CSharpTypeUsedForOracleClob = Parameter.Instance.CSharpTypeUsedForOracleClob;
                //Translater.CSharpTypeUsedForOracleBFile = Parameter.Instance.CSharpTypeUsedForOracleBFile;
                Translater.ConvertOracleNumberToIntegerIfColumnNameIsId = Parameter.Instance.IsConvertOracleNumberToIntegerIfColumnNameIsId;
                Translater.ObjectTypeNamespace = Parameter.Instance.NamespaceObjectType;

                // instantiate generator
                Generator generator = new Generator(Parameter.Instance.Schema, Parameter.Instance.OutputPath, displayMessageMethod, Parameter.Instance.DatabaseInstance,
                    Parameter.Instance.UserLogin, Parameter.Instance.Password, Parameter.Instance.NamespaceBase, Parameter.Instance.NamespaceObjectType);

                    // retrieve necessary data from schema
                    Loader loader = new Loader(displayMessageMethod);
                try {
                    displayMessageMethod(Parameter.Instance.DatabaseInstance + " " + Parameter.Instance.Schema 
                        + (String.IsNullOrEmpty(Parameter.Instance.Filter) ? String.Empty : " " + Parameter.Instance.Filter + "*") + " generation:");
                    loader.Load();
                } catch (Exception e) {
                    displayMessageMethod(e.Message);
                    return;
                } 

                ////////////////////////
                // generate base classes
                if (Parameter.Instance.IsGenerateBaseAdapter) generator.WriteBasePackageClass(Parameter.Instance.AncestorClassNamePackage);
                if (Parameter.Instance.IsGenerateBaseEntities) generator.WriteBaseEntityClasses(Parameter.Instance.AncestorClassNamePackageRecord);

                //////////////////////////////////
                // generate schema-derived classes
                if (Parameter.Instance.IsGeneratePackage)
                    generator.WritePackageClasses(loader.Packages, loader.PackageRecordTypes, Parameter.Instance.NamespacePackage, Parameter.Instance.AncestorClassNamePackage, 
                        Parameter.Instance.IsPartialPackage, Parameter.Instance.AncestorClassNamePackageRecord);
                if (Parameter.Instance.IsGenerateObjectType)
                    generator.WriteObjectTypeClasses(loader.ObjectTypes, Parameter.Instance.NamespaceObjectType, Generator.GenerateBaseObjectTypeClassName(Parameter.Instance.Schema));
                if (Parameter.Instance.IsGenerateTable)
                    generator.WriteTableClasses(loader.Tables, Parameter.Instance.NamespaceTable, Generator.GenerateBaseTableClassName(Parameter.Instance.Schema));
                if (Parameter.Instance.IsGenerateView)
                    generator.WriteViewClasses(loader.Views, Parameter.Instance.NamespaceView, Generator.GenerateBaseViewClassName(Parameter.Instance.Schema));

                generator.DeployUtilityClasses(Parameter.Instance.IsDeployResources);
                displayMessageMethod(Message.GENERATION_COMPLETE);
            } else {
                displayMessageMethod(Message.NO_GENERATE_OPTIONS_SELECTED);
            }
        }

        #region Miscellanous Methods
        public static String GetAppNameVersionLabel() {
            String version = "";
            object[] attributes = typeof(Generator).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            if (attributes.Length > 0) version = (attributes[0] as AssemblyInformationalVersionAttribute).InformationalVersion;
            return APPLICATION_NAME + " " + version 
#if DEBUG
                + " *** DEBUG BUILD ***"
#endif
                ;
        }
       
        /// <summary>
        /// Deploy copy of necessary code that is not generated from schema
        /// </summary>
        private void DeployUtilityClasses(bool overwrite) {
            string fileName, filePath;

            // deploy OrclPower
            fileName = @"OrclPower.cs";
            filePath = _outputPath + @"\" + fileName;
            try {
                if (overwrite || !File.Exists(filePath)) {
                    File.Delete(filePath); // delete existing file since we have to write file in sections
                    File.WriteAllText(filePath, Comment.COMMENT_AUTO_GENERATED + Environment.NewLine);
                    if (IsCSharp30) File.AppendAllText(filePath, "#define CSHARP30\r\n");   // for C# 3.0, add compiler option
                    File.AppendAllText(filePath, Properties.Resources.OrclPower);           // write body of source code
                }
            } catch (UnauthorizedAccessException) {
                DisplayMessage(Message.BASE_PERMISSION_ERROR_MSG + Path.GetFileName(fileName));
            } catch (Exception e) {
                DisplayMessage("Error writing " + Path.GetFileName(fileName) + " - " + e.Message);
            }

            // deploy CaseConversion
            fileName = @"CaseConversion.cs";
            filePath = _outputPath + @"\" + fileName;
            try {
                if (overwrite || !File.Exists(filePath)) {
                    File.Delete(filePath); // delete existing file since we have to write file in sections
                    File.WriteAllText(filePath, Comment.COMMENT_AUTO_GENERATED + Environment.NewLine);
                    File.AppendAllText(filePath, Properties.Resources.CaseConversion);  // write body of source code
                }
            } catch (UnauthorizedAccessException) {
                DisplayMessage(Message.BASE_PERMISSION_ERROR_MSG + Path.GetFileName(fileName));
            } catch (Exception e) {
                DisplayMessage("Error writing " + Path.GetFileName(fileName) + " - " + e.Message);
            }

        }

        /// <summary>
        /// Return a string spaces to be used as a single tab 
        /// </summary>
        /// <returns></returns>
        private string Tab() {
            return Tab(1);
        }

        /// <summary>
        /// Return a string of spaces for a given number of tabs 
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        private string Tab(int count) {
            string tabs = "";
            for (int i = 0; i < count; i++) tabs += new String(' ', 4); // tab is 4 spaces
            return tabs;
        }

        private String GenerateDataContractAttribute() {
            return @"[" + CSharp.ATTRIBUTE_DATA_CONTRACT + "("
                + (String.IsNullOrEmpty(Parameter.Instance.NamespaceDataContract)
                    ? ""
                    : @"Namespace=""" + Parameter.Instance.NamespaceDataContract + @"""") 
                + ")]";
        }

        private static String GenerateLocalVariableName(string baseLocalVarName) {
            return Parameter.Instance.LocalVariableNameSuffix + baseLocalVarName;
        }

        private void DisplayMessage(String msg) { _displayMessageMethod(msg); }
        #endregion
    }
}