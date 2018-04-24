Odapter - a C# code generator for Oracle packages
========================================

Odapter generates C# adapter classes that provide maximum integration with an Oracle schema's packages. Generated DTO Lists are hydrated from any returned cursor results sets, both strongly (record type based) and weakly typed. The generated C# provides de facto compile-time resolution with Oracle packages from within the IDE. Additionally, C# DTOs can be generated for object types, tables and views.

### Minimum System Requirements

* Oracle RDBMS 11g
* Windows 64-bit OS
* .NET Framework 
    - 4.0 minimum for code generator
    - 3.5 minimum for project
* ODP.NET for destination project
    - Managed Driver if targeting .NET Framework 4.0 or higher
    - Unmanaged Driver if targeting .NET Framework 3.5

### Oracle to C# Translation Concepts

| PL/SQL and Schema Objects                           | C# |
| --------------------------------------------------- | -------- |
| Case insensitive                                    | Case senstive | 
| underscore_delimited naming                         | PascalCase & camelCase naming     |
| Package Record Type Field                           | Property     |
| Package Record Type                                 | Nested public class of properties    |
| Function or Stored Procedure (packaged)             | Method    |
| Package                                             | Singleton class of methods and nested classes    |
| Schema                                              | Namespace (with nesting)    |
| Object Type, Table, View                            | Class of properties    |

### Code Generation Features

* Generates adapter class for each package and nested DTOs for respective record types
* Translates all common Oracle data types to C#
* Translates Oracle associative array type to C#
* Configurable translation of Oracle NUMBER, DATE and TIMESTAMP types to C# (including ODP.NET safe types OracleDecimal, OracleDate, OracleTimestamp)
* Translates Oracle IN, OUT and IN OUT parameters to C#
* Translates Oracle optional (defaulted) parameters to C# (4.0+)
* Translates strongly and weakly typed cursors (both as function return and OUT parameters) to C#
* Generates DTO for each object type, table, and view
* Configurable for either auto-implemented or protected field wrapped DTO properties
* Generates ancestor classes and basic schema connection code
* Configurable C# namespaces and base class names
* Generates post hook for profiling a package procedure invoked from C#
* Optionally filters schema objects via prefix and special characters
* Optionally generates C# classes as partial for packages, package record types, object types, tables and views
* Optionally generates C# DTOs with Serializable, DataContract/DataMember (incl. namespace) or XmlElementAttribute attributes for package record types, object types, tables and views
* Generates C# 3.0 or 4.0+ code (respectively .NET 3.5 or 4.0+)
* Generates single C# file for all packages, object types, tables and views, respectively
* Handles package referencing a record type defined in a diffferent package (including filtered)
* Easily adaptable to legacy C# projects and Oracle schemas
* Locates and parses local TNSNAMES.ORA for Oracle instances
* Persists custom generation settings to config file for multiple projects or schemas

### Run Time Features - Packages

* Invokes packaged functions and stored procedures
* Hydrates a List of (record type derived) DTOs from a returned (or OUT param) strongly typed cursor result set
* Hydrates a List of DTOs from a returned weakly typed cursor result set using configurable mapping:
    - Mapping by name: column name to property name (translates underscore_delimited to PascalCase)
    - Mapping by position: column position to property attribute position (unmapped column silent fail option)
    - For performance, uses thread-safe static cache for mappings of C# DTO to Oracle result set
* Constructs (from underlying columns) and hydrates DataTable from returned strongly or weakly typed cursor result set
* Optionally limits the number of rows returned from any cursor result set

### Getting Started: Generating Code for Packages

1. Download Odapter/bin/x64/Release/Odapter.exe and run
2. If Oracle Client Homes are found, select appropriate value
3. Enter DB Instance, Schema, Login and Password
4. Enter the Output Path with to be the destination of the generated files (your project folder)
5. If project is using the .NET 3.5 framework, select 3.0 as the C# Version
6. For all other fields, use default settings
6. Click Generate 
7. After successful generation, enter a .config file name in File Source and click Save Current
8. Open your project and add the generated files
9. Add "using Schema.YourSchemaName.Package" to project files in order to access packages
10. See Tester/Tester.cs for code examples

### Code Sample #1
###### Package Specification

```SQLPL
CREATE OR REPLACE PACKAGE ODPT.odpt_pkg_sample AS

    -- strongly typed cursor
    TYPE t_table_big_partial IS RECORD (
        id                  odpt_table_big.id%TYPE,                 -- NUMBER
        col_integer         odpt_table_big.col_integer%TYPE,        -- INTEGER
        col_number          odpt_table_big.col_number%TYPE,         -- NUMBER
        col_varchar2_max    odpt_table_big.col_varchar2_max%TYPE,   -- VARCHAR2(4000)
        col_date            odpt_table_big.col_date%TYPE,           -- DATE
        col_timestamp       odpt_table_big.col_timestamp%TYPE);     -- TIMESTAMP
    TYPE t_ref_cursor_table_big_partial IS REF CURSOR RETURN t_table_big_partial;
	
    FUNCTION get_rows_typed_ret (p_in_number IN NUMBER, p_in_out_varchar2 IN OUT VARCHAR2, p_out_date OUT DATE) RETURN t_ref_cursor_table_big_partial;

END odpt_pkg_sample;
/
```

###### Package Body

```SQLPL
CREATE OR REPLACE PACKAGE BODY ODPT.odpt_pkg_sample AS
                                     
    FUNCTION get_rows_typed_ret (p_in_number IN NUMBER, p_in_out_varchar2 IN OUT VARCHAR2, p_out_date OUT DATE) RETURN t_ref_cursor_table_big_partial IS
        l_cursor    t_ref_cursor_table_big_partial;
    BEGIN
        OPEN l_cursor FOR
        SELECT      id, col_integer, col_number, col_varchar2_max, col_date, col_timestamp
        FROM        odpt_table_big
        ORDER BY    id;    

        p_in_out_varchar2 := 'Goodbye';        
        p_out_date := TO_DATE ('31-DEC-1999');        
        RETURN l_cursor;
    END;	

END odpt_pkg_sample;
/
```

###### Generation 

![](Odapter/OdapterScreenShot.png "")

###### Generated Code

```C#
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Odapter;
using System.Linq;

namespace Schema.Odpt.Package {
    public partial class OdptPkgSample : Schema.Odpt.OdptAdapter {
        private OdptPkgSample() { }
        private static OdptPkgSample _instance = new OdptPkgSample();
        public static OdptPkgSample Instance { get { return _instance; } }

        public interface ITTableBigPartial {
            Int64? Id { get; set; }
            Int64? ColInteger { get; set; }
            Decimal? ColNumber { get; set; }
            String ColVarchar2Max { get; set; }
            DateTime? ColDate { get; set; }
            OracleTimeStamp? ColTimestamp { get; set; }
        } // ITTableBigPartial

        [DataContract(Namespace="http://odpt.business.com")][Serializable()]
        public partial class TTableBigPartial : Schema.Odpt.OdptPackageRecord, ITTableBigPartial {
            private Byte propertyToEnsuresPartialClassNamesAreUniqueAtCompileTime { get; set; }
            [DataMember(Order=0, IsRequired=false)][XmlElement(Order=0, IsNullable=true)]
            public virtual Int64? Id { get; set; }
            [DataMember(Order=1, IsRequired=false)][XmlElement(Order=1, IsNullable=true)]
            public virtual Int64? ColInteger { get; set; }
            [DataMember(Order=2, IsRequired=false)][XmlElement(Order=2, IsNullable=true)]
            public virtual Decimal? ColNumber { get; set; }
            [DataMember(Order=3, IsRequired=false)][XmlElement(Order=3, IsNullable=true)]
            public virtual String ColVarchar2Max { get; set; }
            [DataMember(Order=4, IsRequired=false)][XmlElement(Order=4, IsNullable=true)]
            public virtual DateTime? ColDate { get; set; }
            [DataMember(Order=5, IsRequired=false)][XmlElement(Order=5, IsNullable=true)]
            public virtual OracleTimeStamp? ColTimestamp { get; set; }
        } // TTableBigPartial

        public List<T_TTableBigPartial> ReadResultITTableBigPartial<T_TTableBigPartial>(OracleDataReader rdr, UInt32? optionalMaxNumberRowsToReadFromAnyCursor = null)
                where T_TTableBigPartial : class, ITTableBigPartial, new()   {
            List<T_TTableBigPartial> __ret = new List<T_TTableBigPartial>();
            if (rdr != null && rdr.HasRows) {
                while (rdr.Read()) {
                    T_TTableBigPartial obj = new T_TTableBigPartial();
                    if (!rdr.IsDBNull(0)) obj.Id = Convert.ToInt64(rdr.GetValue(0));
                    if (!rdr.IsDBNull(1)) obj.ColInteger = Convert.ToInt64(rdr.GetValue(1));
                    if (!rdr.IsDBNull(2)) obj.ColNumber = (Decimal?)OracleDecimal.SetPrecision(rdr.GetOracleDecimal(2), 29);
                    if (!rdr.IsDBNull(3)) obj.ColVarchar2Max = Convert.ToString(rdr.GetValue(3));
                    if (!rdr.IsDBNull(4)) obj.ColDate = Convert.ToDateTime(rdr.GetValue(4));
                    if (!rdr.IsDBNull(5)) obj.ColTimestamp = (OracleTimeStamp?)rdr.GetOracleValue(5);
                    __ret.Add(obj);
                    if (optionalMaxNumberRowsToReadFromAnyCursor != null && __ret.Count >= optionalMaxNumberRowsToReadFromAnyCursor) break;
                }
            }
            return __ret;
        } // ReadResultITTableBigPartial

        public List<T_TTableBigPartial> GetRowsTypedRet<T_TTableBigPartial>(Decimal? pInNumber, ref String pInOutVarchar2, out DateTime? pOutDate, UInt32? optionalMaxNumberRowsToReadFromAnyCursor = null, 
                OracleConnection optionalPreexistingOpenConnection = null)
                where T_TTableBigPartial : class, ITTableBigPartial, new() {
            List<T_TTableBigPartial> __ret = new List<T_TTableBigPartial>(); pOutDate = null; 
            OracleConnection __conn = optionalPreexistingOpenConnection ?? GetConnection();
            try {
                using (OracleCommand __cmd = new OracleCommand("ODPT.ODPT_PKG_SAMPLE.GET_ROWS_TYPED_RET", __conn)) {
                    __cmd.CommandType = CommandType.StoredProcedure;
                    __cmd.BindByName = true;
                    __cmd.Parameters.Add(new OracleParameter("!RETURN", OracleDbType.RefCursor, null, ParameterDirection.ReturnValue));
                    __cmd.Parameters.Add(new OracleParameter("P_IN_NUMBER", OracleDbType.Decimal, pInNumber, ParameterDirection.Input));
                    __cmd.Parameters.Add(new OracleParameter("P_IN_OUT_VARCHAR2", OracleDbType.Varchar2, 32767, pInOutVarchar2, ParameterDirection.InputOutput));
                    __cmd.Parameters.Add(new OracleParameter("P_OUT_DATE", OracleDbType.Date, null, ParameterDirection.Output));

                    OracleCommandTrace __cmdTrace = IsTracing(__cmd) ? new OracleCommandTrace(__cmd) : null;
                    int __rowsAffected = __cmd.ExecuteNonQuery();
                    if (!((OracleRefCursor)__cmd.Parameters["!RETURN"].Value).IsNull)
                        using (OracleDataReader __rdr = ((OracleRefCursor)__cmd.Parameters["!RETURN"].Value).GetDataReader()) {
                            __ret = ReadResultITTableBigPartial<T_TTableBigPartial>(__rdr, optionalMaxNumberRowsToReadFromAnyCursor);
                        } // using OracleDataReader
                    pInOutVarchar2 = __cmd.Parameters["P_IN_OUT_VARCHAR2"].Status == OracleParameterStatus.NullFetched
                        ? (String)null
                        : Convert.ToString(__cmd.Parameters["P_IN_OUT_VARCHAR2"].Value.ToString());
                    pOutDate = __cmd.Parameters["P_OUT_DATE"].Status == OracleParameterStatus.NullFetched
                        ? (DateTime?)null
                        : Convert.ToDateTime(__cmd.Parameters["P_OUT_DATE"].Value.ToString());
                    if (__cmdTrace != null) TraceCompletion(__cmdTrace, __ret.Count);
                } // using OracleCommand
            } finally {
                if (optionalPreexistingOpenConnection == null) {
                    __conn.Close();
                    __conn.Dispose();
                }
            }
            return __ret;
        } // GetRowsTypedRet
    } // OdptPkgSample
}
```

###### Executing Generated Code

```C#
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Schema.Odpt.Package;

namespace Odapter.Sample {
    public class Sample {
        public void TestSample() {
            uint? rowLimit = 10;
            Decimal? pInDecimal = 10.0M;
            String pInOutString = "Hello";
            DateTime? pOutDate;

            List<OdptPkgSample.TTableBigPartial> retTableBigPartialList = OdptPkgSample.Instance.GetRowsTypedRet<OdptPkgSample.TTableBigPartial>(pInDecimal, ref pInOutString, out pOutDate, rowLimit);
            Debug.Assert(retTableBigPartialList.Count == rowLimit);
            Debug.Assert(pInOutString.Equals("Goodbye"));
            Debug.Assert(pOutDate.Equals(new DateTime(1999, 12, 31)));
        }
    }
}
``
