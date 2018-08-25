CREATE OR REPLACE PACKAGE ODPT.odpt_pkg_main AS

	-- associative array types
	TYPE t_assocarray_integer IS TABLE OF odpt_table_big.col_integer%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_binary_integer IS TABLE OF BINARY_INTEGER INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_pls_integer IS TABLE OF PLS_INTEGER INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_smallint IS TABLE OF odpt_table_big.col_smallint%TYPE INDEX BY PLS_INTEGER;  
  
	TYPE t_assocarray_number IS TABLE OF odpt_table_big.col_number%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_binary_double IS TABLE OF odpt_table_big.col_binary_double%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_binary_float IS TABLE OF odpt_table_big.col_binary_float%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_double_precision IS TABLE OF odpt_table_big.col_double_precision%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_float IS TABLE OF odpt_table_big.col_float%TYPE INDEX BY PLS_INTEGER;  

	TYPE t_assocarray_varchar2 IS TABLE OF odpt_table_big.col_varchar2_max%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_nvarchar2 IS TABLE OF odpt_table_big.col_nvarchar2_max%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_string IS TABLE OF STRING(32767) INDEX BY PLS_INTEGER;  

	TYPE t_assocarray_char IS TABLE OF odpt_table_big.col_char_max%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_nchar IS TABLE OF odpt_table_big.col_nchar_max%TYPE INDEX BY PLS_INTEGER;  
  
	TYPE t_assocarray_date IS TABLE OF odpt_table_big.col_date%TYPE INDEX BY PLS_INTEGER;  
  
	TYPE t_assocarray_timestamp IS TABLE OF odpt_table_big.col_timestamp%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_timestamp_prec0 IS TABLE OF odpt_table_big.col_timestamp_prec0%TYPE INDEX BY PLS_INTEGER;  
	TYPE t_assocarray_timestamp_prec9 IS TABLE OF odpt_table_big.col_timestamp_prec9%TYPE INDEX BY PLS_INTEGER;  

	TYPE t_assocarray_boolean IS TABLE OF BOOLEAN INDEX BY PLS_INTEGER;  
	
	-- data types ignored (not implemented) in code generation
	TYPE t_record_type_ignored IS RECORD (
		f_numeric							NUMERIC, -- implementation to be determined
		f_decimal							DECIMAL, -- implementation to be determined
		f_rowid								ROWID,
		f_urowid							UROWID,
		f_timestamp_w_l_time_zone 			TIMESTAMP WITH LOCAL TIME ZONE, 
		f_timestamp_w_time_zone				TIMESTAMP WITH TIME ZONE,	
		f_raw								RAW(1),
		f_bfile								BFILE,
		f_blob								BLOB,
		f_boolean							BOOLEAN,	 -- .NET cannot handle PL/SQL BOOLEAN
		f_clob								CLOB,
		f_nclob								NCLOB,
		f_xmltype							XMLTYPE,
		f_long								LONG,						-- deprecated
		f_long_raw							LONG RAW,			-- deprecated
		f_last								NUMBER
	);
	TYPE t_cursor_typed_ignored IS REF CURSOR RETURN t_record_type_ignored;
	
	PROCEDURE proc_underscore_suffix_;
	PROCEDURE proc_no_param;
	FUNCTION func_no_param RETURN NUMBER;

	PROCEDURE proc_optional_param(p_in_number_required IN NUMBER, p_in_out_number_required IN OUT NUMBER, p_in_number_optional IN NUMBER DEFAULT 0, p_in_varchar2_optional IN VARCHAR2 DEFAULT 'TEST');
	FUNCTION func_optional_param(p_in_number_required IN NUMBER, p_in_out_number_required IN OUT NUMBER, 
		p_in_number_optional IN NUMBER DEFAULT 0, p_in_varchar2_optional IN VARCHAR2 DEFAULT 'TEST') RETURN NUMBER;

	FUNCTION func_cursor_typed_ignored RETURN t_cursor_typed_ignored;
   
	FUNCTION func_integer(p_in IN INTEGER, p_in_out IN OUT INTEGER, p_out OUT INTEGER) RETURN INTEGER;
	FUNCTION func_int(p_in IN INT, p_in_out IN OUT INT, p_out OUT INT) RETURN INT;
	FUNCTION func_smallint(p_in IN SMALLINT, p_in_out IN OUT SMALLINT, p_out OUT SMALLINT) RETURN SMALLINT;

	FUNCTION func_binary_integer(p_in IN BINARY_INTEGER, p_in_out IN OUT BINARY_INTEGER, p_out OUT BINARY_INTEGER) RETURN BINARY_INTEGER;
	FUNCTION func_pls_integer(p_in IN PLS_INTEGER, p_in_out IN OUT PLS_INTEGER, p_out OUT PLS_INTEGER) RETURN PLS_INTEGER;

	FUNCTION func_number(p_in IN NUMBER, p_in_out IN OUT NUMBER, p_out OUT NUMBER) RETURN NUMBER;
	FUNCTION func_float(p_in IN FLOAT, p_in_out IN OUT FLOAT, p_out OUT FLOAT) RETURN FLOAT;
	FUNCTION func_binary_float(p_in IN BINARY_FLOAT, p_in_out IN OUT BINARY_FLOAT, p_out OUT BINARY_FLOAT) RETURN BINARY_FLOAT;
	PROCEDURE proc_binary_float_const(p_min_normal OUT BINARY_FLOAT, p_max_normal OUT BINARY_FLOAT);
	FUNCTION func_binary_double(p_in IN BINARY_DOUBLE, p_in_out IN OUT BINARY_DOUBLE, p_out OUT BINARY_DOUBLE) RETURN BINARY_DOUBLE;
	PROCEDURE proc_binary_double_const(p_min_normal OUT BINARY_DOUBLE, p_max_normal OUT BINARY_DOUBLE);
	FUNCTION func_double_precision(p_in IN DOUBLE PRECISION, p_in_out IN OUT DOUBLE PRECISION, p_out OUT DOUBLE PRECISION) RETURN DOUBLE PRECISION;

	FUNCTION func_varchar(p_in IN VARCHAR, p_in_out IN OUT VARCHAR, p_out OUT VARCHAR) RETURN VARCHAR;
	FUNCTION func_varchar2(p_in IN VARCHAR2, p_in_out IN OUT VARCHAR2, p_out OUT VARCHAR2) RETURN VARCHAR2;
	FUNCTION func_nvarchar2(p_in IN NVARCHAR2, p_in_out IN OUT NVARCHAR2, p_out OUT NVARCHAR2) RETURN NVARCHAR2;
	FUNCTION func_string(p_in IN STRING, p_in_out IN OUT STRING, p_out OUT STRING) RETURN STRING;
	
	FUNCTION func_nchar(p_in IN NCHAR, p_in_out IN OUT NCHAR, p_out OUT NCHAR) RETURN NCHAR;
	FUNCTION func_char(p_in IN CHAR, p_in_out IN OUT CHAR, p_out OUT CHAR) RETURN CHAR;   
	
	FUNCTION func_ref(p_in IN REF odpt_big_ot, p_in_out IN OUT REF odpt_big_ot, p_out OUT REF odpt_big_ot) RETURN REF odpt_big_ot;

	FUNCTION func_date(p_in IN DATE, p_in_out IN OUT DATE, p_out OUT DATE) RETURN DATE;
	FUNCTION func_timestamp(p_in IN TIMESTAMP, p_in_out IN OUT TIMESTAMP, p_out OUT TIMESTAMP)	RETURN TIMESTAMP;

	FUNCTION func_aa_integer (p_in IN t_assocarray_integer, p_in_out IN OUT t_assocarray_integer, p_out OUT t_assocarray_integer) RETURN t_assocarray_integer;
	FUNCTION func_aa_smallint (p_in IN t_assocarray_smallint, p_in_out IN OUT t_assocarray_smallint, p_out OUT t_assocarray_smallint) RETURN t_assocarray_smallint;
	FUNCTION func_aa_number (p_in IN t_assocarray_number, p_in_out IN OUT t_assocarray_number, p_out OUT t_assocarray_number) RETURN t_assocarray_number;
	FUNCTION func_aa_binary_double (p_in IN t_assocarray_binary_double, p_in_out IN OUT t_assocarray_binary_double, p_out OUT t_assocarray_binary_double) RETURN t_assocarray_binary_double;
	FUNCTION func_aa_binary_float (p_in IN t_assocarray_binary_float, p_in_out IN OUT t_assocarray_binary_float, p_out OUT t_assocarray_binary_float) RETURN t_assocarray_binary_float;
	FUNCTION func_aa_double_precision (p_in IN t_assocarray_double_precision, p_in_out IN OUT t_assocarray_double_precision, p_out OUT t_assocarray_double_precision) RETURN t_assocarray_double_precision;
	FUNCTION func_aa_float (p_in IN t_assocarray_float, p_in_out IN OUT t_assocarray_float, p_out OUT t_assocarray_float) RETURN t_assocarray_float;
	FUNCTION func_aa_varchar2 (p_in IN t_assocarray_varchar2, p_in_out IN OUT t_assocarray_varchar2, p_out OUT t_assocarray_varchar2) RETURN t_assocarray_varchar2;
	FUNCTION func_aa_nvarchar2 (p_in IN t_assocarray_nvarchar2, p_in_out IN OUT t_assocarray_nvarchar2, p_out OUT t_assocarray_nvarchar2) RETURN t_assocarray_nvarchar2;
	FUNCTION func_aa_string (p_in IN t_assocarray_string, p_in_out IN OUT t_assocarray_string, p_out OUT t_assocarray_string) RETURN t_assocarray_string;
	FUNCTION func_aa_nchar (p_in IN t_assocarray_nchar, p_in_out IN OUT t_assocarray_nchar, p_out OUT t_assocarray_nchar) RETURN t_assocarray_nchar;
	FUNCTION func_aa_char (p_in IN t_assocarray_char, p_in_out IN OUT t_assocarray_char, p_out OUT t_assocarray_char) RETURN t_assocarray_char;
	FUNCTION func_aa_date (p_in IN t_assocarray_date, p_in_out IN OUT t_assocarray_date, p_out OUT t_assocarray_date) RETURN t_assocarray_date;
	FUNCTION func_aa_timestamp (p_in IN t_assocarray_timestamp, p_in_out IN OUT t_assocarray_timestamp, p_out OUT t_assocarray_timestamp) RETURN t_assocarray_timestamp;
	FUNCTION func_aa_timestamp_prec0 (p_in IN t_assocarray_timestamp_prec0, p_in_out IN OUT t_assocarray_timestamp_prec0, p_out OUT t_assocarray_timestamp_prec0) RETURN t_assocarray_timestamp_prec0;
	FUNCTION func_aa_timestamp_prec9 (p_in IN t_assocarray_timestamp_prec9, p_in_out IN OUT t_assocarray_timestamp_prec9, p_out OUT t_assocarray_timestamp_prec9) RETURN t_assocarray_timestamp_prec9;
	
	----------------------------------
	-- UNIMPLEMENTED and COMMENTED OUT	
	-- pending implementation
	FUNCTION func_blob(p_in IN BLOB, p_in_out IN OUT BLOB, p_out OUT BLOB) RETURN BLOB;
	FUNCTION func_clob(p_in IN CLOB, p_in_out IN OUT CLOB, p_out OUT CLOB) RETURN CLOB;
	FUNCTION func_nclob(p_in IN NCLOB, p_in_out IN OUT NCLOB, p_out OUT NCLOB) RETURN NCLOB;
	FUNCTION func_xmltype(p_in IN XMLTYPE, p_in_out IN OUT XMLTYPE, p_out OUT XMLTYPE) RETURN XMLTYPE;
	FUNCTION func_rowid(p_in IN ROWID, p_in_out IN OUT ROWID, p_out OUT ROWID)	RETURN ROWID;
	FUNCTION func_urowid(p_in IN UROWID, p_in_out IN OUT UROWID, p_out OUT UROWID) RETURN UROWID;
	FUNCTION func_timestamp_w_l_time_zone(p_in IN TIMESTAMP WITH LOCAL TIME ZONE, p_in_out IN OUT TIMESTAMP WITH LOCAL TIME ZONE, p_out OUT TIMESTAMP WITH LOCAL TIME ZONE) RETURN TIMESTAMP WITH LOCAL TIME ZONE;
	FUNCTION func_timestamp_w_time_zone(p_in IN TIMESTAMP WITH TIME ZONE, p_in_out IN OUT TIMESTAMP WITH TIME ZONE, p_out OUT TIMESTAMP WITH TIME ZONE) RETURN TIMESTAMP WITH TIME ZONE;
	FUNCTION func_bfile(p_in IN BFILE, p_in_out IN OUT BFILE, p_out OUT BFILE) RETURN BFILE;
	FUNCTION func_raw(p_in IN RAW, p_in_out IN OUT RAW, p_out OUT RAW) RETURN RAW;
	FUNCTION func_numeric(p_in IN NUMERIC, p_in_out IN OUT NUMERIC, p_out OUT NUMERIC) RETURN NUMERIC;
	FUNCTION func_decimal(p_in IN DECIMAL, p_in_out IN OUT DECIMAL, p_out OUT DECIMAL) RETURN DECIMAL;
	FUNCTION func_interval_day_to_second(p_in IN INTERVAL DAY TO SECOND, p_in_out IN OUT INTERVAL DAY TO SECOND, p_out OUT INTERVAL DAY TO SECOND) RETURN INTERVAL DAY TO SECOND;
	FUNCTION func_interval_year_to_month(p_in IN INTERVAL YEAR TO MONTH, p_in_out IN OUT INTERVAL YEAR TO MONTH, p_out OUT INTERVAL YEAR TO MONTH) RETURN INTERVAL YEAR TO MONTH;

	-- implementation not possible due to binding restriction of PL/SQL types
	FUNCTION func_object_type(p_in IN odpt_big_ot, p_in_out IN OUT odpt_big_ot, p_out OUT odpt_big_ot) RETURN odpt_big_ot;
	FUNCTION func_record(p_in IN odpt_pkg_table_big.t_table_big, p_in_out IN OUT odpt_pkg_table_big.t_table_big, p_out OUT odpt_pkg_table_big.t_table_big) RETURN odpt_pkg_table_big.t_table_big;
	FUNCTION func_boolean(p_in IN BOOLEAN, p_in_out IN OUT BOOLEAN, p_out OUT BOOLEAN) RETURN BOOLEAN; -- impossible to bind in C#
	FUNCTION func_aa_binary_integer (p_in IN t_assocarray_binary_integer, p_in_out IN OUT t_assocarray_binary_integer, p_out OUT t_assocarray_binary_integer) RETURN t_assocarray_binary_integer;
	FUNCTION func_aa_pls_integer (p_in IN t_assocarray_pls_integer, p_in_out IN OUT t_assocarray_pls_integer, p_out OUT t_assocarray_pls_integer) RETURN t_assocarray_pls_integer;
	FUNCTION func_aa_boolean (p_in IN t_assocarray_boolean, p_in_out IN OUT t_assocarray_boolean, p_out OUT t_assocarray_boolean) RETURN t_assocarray_boolean;
	
	-- will not be implemented due to deprecation, demand or practicality
	FUNCTION func_long(p_in IN LONG, p_in_out IN OUT LONG, p_out OUT LONG) RETURN LONG;
	FUNCTION func_long_raw(p_in IN LONG RAW, p_in_out IN OUT LONG RAW, p_out OUT LONG RAW) RETURN LONG RAW;

END odpt_pkg_main;
/