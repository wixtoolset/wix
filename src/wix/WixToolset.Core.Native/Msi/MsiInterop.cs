// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Class exposing static functions and structs from MSI API.
    /// </summary>
    internal static class MsiInterop
    {
        // Patching constants
        internal const int MsiMaxStreamNameLength = 62; // http://msdn2.microsoft.com/library/aa370551.aspx

        internal const int MSICONDITIONFALSE = 0;   // The table is temporary.
        internal const int MSICONDITIONTRUE = 1;   // The table is persistent.
        internal const int MSICONDITIONNONE = 2;   // The table is unknown.
        internal const int MSICONDITIONERROR = 3;   // An invalid handle or invalid parameter was passed to the function.

        /*
        internal const int MSIDBOPENREADONLY = 0;
        internal const int MSIDBOPENTRANSACT = 1;
        internal const int MSIDBOPENDIRECT = 2;
        internal const int MSIDBOPENCREATE = 3;
        internal const int MSIDBOPENCREATEDIRECT = 4;
        internal const int MSIDBOPENPATCHFILE = 32;

        internal const int MSIMODIFYSEEK = -1;   // Refreshes the information in the supplied record without changing the position in the result set and without affecting subsequent fetch operations. The record may then be used for subsequent Update, Delete, and Refresh. All primary key columns of the table must be in the query and the record must have at least as many fields as the query. Seek cannot be used with multi-table queries. This mode cannot be used with a view containing joins. See also the remarks.
        internal const int MSIMODIFYREFRESH = 0;   // Refreshes the information in the record. Must first call MsiViewFetch with the same record. Fails for a deleted row. Works with read-write and read-only records.
        internal const int MSIMODIFYINSERT = 1;   // Inserts a record. Fails if a row with the same primary keys exists. Fails with a read-only database. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYUPDATE = 2;   // Updates an existing record. Nonprimary keys only. Must first call MsiViewFetch. Fails with a deleted record. Works only with read-write records.
        internal const int MSIMODIFYASSIGN = 3;   // Writes current data in the cursor to a table row. Updates record if the primary keys match an existing row and inserts if they do not match. Fails with a read-only database. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYREPLACE = 4;   // Updates or deletes and inserts a record into a table. Must first call MsiViewFetch with the same record. Updates record if the primary keys are unchanged. Deletes old row and inserts new if primary keys have changed. Fails with a read-only database. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYMERGE = 5;   // Inserts or validates a record in a table. Inserts if primary keys do not match any row and validates if there is a match. Fails if the record does not match the data in the table. Fails if there is a record with a duplicate key that is not identical. Works only with read-write records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYDELETE = 6;   // Remove a row from the table. You must first call the MsiViewFetch function with the same record. Fails if the row has been deleted. Works only with read-write records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYINSERTTEMPORARY = 7;   // Inserts a temporary record. The information is not persistent. Fails if a row with the same primary key exists. Works only with read-write records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYVALIDATE = 8;   // Validates a record. Does not validate across joins. You must first call the MsiViewFetch function with the same record. Obtain validation errors with MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYVALIDATENEW = 9;   // Validate a new record. Does not validate across joins. Checks for duplicate keys. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYVALIDATEFIELD = 10;   // Validates fields of a fetched or new record. Can validate one or more fields of an incomplete record. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        internal const int MSIMODIFYVALIDATEDELETE = 11;   // Validates a record that will be deleted later. You must first call MsiViewFetch. Fails if another row refers to the primary keys of this row. Validation does not check for the existence of the primary keys of this row in properties or strings. Does not check if a column is a foreign key to multiple tables. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.

        internal const uint VTI2 = 2;
        internal const uint VTI4 = 3;
        internal const uint VTLPWSTR = 30;
        internal const uint VTFILETIME = 64;
        */

        internal const int MSICOLINFONAMES = 0;  // return column names
        internal const int MSICOLINFOTYPES = 1;  // return column definitions, datatype code followed by width

        /// <summary>
        /// PInvoke of MsiCloseHandle.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiCloseHandle", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiCloseHandle(IntPtr database);

        /// <summary>
        /// PInvoke of MsiCreateRecord
        /// </summary>
        /// <param name="parameters">Count of columns in the record.</param>
        /// <returns>Handle referencing the record.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiCreateRecord", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr MsiCreateRecord(int parameters);

        /// <summary>
        /// Creates summary information of an existing transform to include validation and error conditions.
        /// </summary>
        /// <param name="database">The handle to the database that contains the new database summary information.</param>
        /// <param name="referenceDatabase">The handle to the database that contains the original summary information.</param>
        /// <param name="transformFile">The name of the transform to which the summary information is added.</param>
        /// <param name="errorConditions">The error conditions that should be suppressed when the transform is applied.</param>
        /// <param name="validations">Specifies the properties to be validated to verify that the transform can be applied to the database.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiCreateTransformSummaryInfoW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiCreateTransformSummaryInfo(IntPtr database, IntPtr referenceDatabase, string transformFile, TransformErrorConditions errorConditions, TransformValidations validations);

        /// <summary>
        /// Applies a transform to a database.
        /// </summary>
        /// <param name="database">Handle to the database obtained from MsiOpenDatabase to transform.</param>
        /// <param name="transformFile">Specifies the name of the transform file to apply.</param>
        /// <param name="errorConditions">Error conditions that should be suppressed.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseApplyTransformW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseApplyTransform(IntPtr database, string transformFile, TransformErrorConditions errorConditions);

        /// <summary>
        /// PInvoke of MsiDatabaseCommit.
        /// </summary>
        /// <param name="database">Handle to a databse.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseCommit", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseCommit(IntPtr database);

        /// <summary>
        /// PInvoke of MsiDatabaseExportW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseExportW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseExport(IntPtr database, string tableName, string folderPath, string fileName);

        /// <summary>
        /// Generates a transform file of differences between two databases.
        /// </summary>
        /// <param name="database">Handle to the database obtained from MsiOpenDatabase that includes the changes.</param>
        /// <param name="databaseReference">Handle to the database obtained from MsiOpenDatabase that does not include the changes.</param>
        /// <param name="transformFile">A null-terminated string that specifies the name of the transform file being generated.
        /// This parameter can be null. If szTransformFile is null, you can use MsiDatabaseGenerateTransform to test whether two
        /// databases are identical without creating a transform. If the databases are identical, the function returns ERROR_NO_DATA.
        /// If the databases are different the function returns NOERROR.</param>
        /// <param name="reserved1">This is a reserved argument and must be set to 0.</param>
        /// <param name="reserved2">This is a reserved argument and must be set to 0.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseGenerateTransformW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseGenerateTransform(IntPtr database, IntPtr databaseReference, string transformFile, int reserved1, int reserved2);

        /// <summary>
        /// PInvoke of MsiDatabaseImportW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseImportW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseImport(IntPtr database, string folderPath, string fileName);

        /// <summary>
        /// PInvoke of MsiDatabaseMergeW.
        /// </summary>
        /// <param name="database">The handle to the database obtained from MsiOpenDatabase.</param>
        /// <param name="databaseMerge">The handle to the database obtained from MsiOpenDatabase to merge into the base database.</param>
        /// <param name="tableName">The name of the table to receive merge conflict information.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseMergeW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseMerge(IntPtr database, IntPtr databaseMerge, string tableName);

        /// <summary>
        /// PInvoke of MsiDatabaseOpenViewW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="query">SQL query.</param>
        /// <param name="view">View handle.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseOpenViewW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseOpenView(IntPtr database, string query, out IntPtr view);

        /// <summary>
        /// PInvoke of MsiExtractPatchXMLDataW.
        /// </summary>
        /// <param name="szPatchPath">Path to patch.</param>
        /// <param name="dwReserved">Reserved for future use.</param>
        /// <param name="szXMLData">Output XML data.</param>
        /// <param name="pcchXMLData">Count of characters in XML.</param>
        /// <returns></returns>
        [DllImport("msi.dll", EntryPoint = "MsiExtractPatchXMLDataW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiExtractPatchXMLData(string szPatchPath, int dwReserved, StringBuilder szXMLData, ref int pcchXMLData);

        /// <summary>
        /// PInvoke of MsiGetFileHashW.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="options">Hash options (must be 0).</param>
        /// <param name="hash">Buffer to recieve hash.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetFileHashW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiGetFileHash(string filePath, uint options, MSIFILEHASHINFO hash);

        /// <summary>
        /// PInvoke of MsiGetFileVersionW.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="versionBuf">Buffer to receive version info.</param>
        /// <param name="versionBufSize">Size of version buffer.</param>
        /// <param name="langBuf">Buffer to recieve lang info.</param>
        /// <param name="langBufSize">Size of lang buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetFileVersionW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiGetFileVersion(string filePath, StringBuilder versionBuf, ref int versionBufSize, StringBuilder langBuf, ref int langBufSize);

        /// <summary>
        /// PInvoke of MsiGetLastErrorRecord.
        /// </summary>
        /// <returns>Handle to error record if one exists.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetLastErrorRecord", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr MsiGetLastErrorRecord();

        /// <summary>
        /// PInvoke of MsiDatabaseGetPrimaryKeysW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="record">Handle to receive resulting record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseGetPrimaryKeysW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseGetPrimaryKeys(IntPtr database, string tableName, out IntPtr record);

        /// <summary>
        /// PInvoke of MsiDoActionW.
        /// </summary>
        /// <param name="product">Handle to the installation provided to a DLL custom action or
        /// obtained through MsiOpenPackage, MsiOpenPackageEx, or MsiOpenProduct.</param>
        /// <param name="action">Specifies the action to execute.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDoActionW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDoAction(IntPtr product, string action);

        /// <summary>
        /// PInvoke of MsiGetSummaryInformationW.  Can use either database handle or database path as input.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="databasePath">Path to a database.</param>
        /// <param name="updateCount">Max number of updated values.</param>
        /// <param name="summaryInfo">Handle to summary information.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetSummaryInformationW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiGetSummaryInformation(IntPtr database, string databasePath, uint updateCount, ref IntPtr summaryInfo);

        /// <summary>
        /// PInvoke of MsiDatabaseIsTablePersitentW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>MSICONDITION</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseIsTablePersistentW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiDatabaseIsTablePersistent(IntPtr database, string tableName);

        /// <summary>
        /// PInvoke of MsiOpenDatabaseW.
        /// </summary>
        /// <param name="databasePath">Path to database.</param>
        /// <param name="persist">Persist mode.</param>
        /// <param name="database">Handle to database.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiOpenDatabaseW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiOpenDatabase(string databasePath, IntPtr persist, out IntPtr database);

        /// <summary>
        /// PInvoke of MsiOpenPackageW.
        /// </summary>
        /// <param name="packagePath">The path to the package.</param>
        /// <param name="product">A pointer to a variable that receives the product handle.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiOpenPackageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiOpenPackage(string packagePath, out IntPtr product);

        /// <summary>
        /// PInvoke of MsiRecordIsNull.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to check for null value.</param>
        /// <returns>true if the field is null, false if not, and an error code for any error.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordIsNull", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordIsNull(IntPtr record, int field);

        /// <summary>
        /// PInvoke of MsiRecordGetInteger.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to retrieve integer from.</param>
        /// <returns>Integer value.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetInteger", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordGetInteger(IntPtr record, int field);

        /// <summary>
        /// PInvoke of MsiRectordSetInteger.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set integer value in.</param>
        /// <param name="value">Value to set field to.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetInteger", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordSetInteger(IntPtr record, int field, int value);

        /// <summary>
        /// PInvoke of MsiRecordGetStringW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to get string value from.</param>
        /// <param name="valueBuf">Buffer to recieve value.</param>
        /// <param name="valueBufSize">Size of buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordGetString(IntPtr record, int field, StringBuilder valueBuf, ref int valueBufSize);

        /// <summary>
        /// PInvoke of MsiRecordSetStringW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set string value in.</param>
        /// <param name="value">String value.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordSetString(IntPtr record, int field, string value);

        /// <summary>
        /// PInvoke of MsiRecordSetStreamW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set stream value in.</param>
        /// <param name="filePath">Path to file to set stream value to.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetStreamW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordSetStream(IntPtr record, int field, string filePath);

        /// <summary>
        /// PInvoke of MsiRecordReadStreamW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to read stream from.</param>
        /// <param name="dataBuf">Data buffer to recieve stream value.</param>
        /// <param name="dataBufSize">Size of data buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordReadStream", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordReadStream(IntPtr record, int field, byte[] dataBuf, ref int dataBufSize);

        /// <summary>
        /// PInvoke of MsiRecordGetFieldCount.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <returns>Count of fields in the record.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetFieldCount", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiRecordGetFieldCount(IntPtr record);

        /// <summary>
        /// PInvoke of MsiSetExternalUIW.
        /// </summary>
        /// <param name="installUIHandler">Specifies a callback function that conforms to the INSTALLUI_HANDLER specification.</param>
        /// <param name="installLogMode">Specifies which messages to handle using the external message handler. If the external
        /// handler returns a non-zero result, then that message will not be sent to the UI, instead the message will be logged
        /// if logging has been enabled.</param>
        /// <param name="context">Pointer to an application context that is passed to the callback function.
        /// This parameter can be used for error checking.</param>
        /// <returns>The return value is the previously set external handler, or zero (0) if there was no previously set handler.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiSetExternalUIW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern InstallUIHandler MsiSetExternalUI(InstallUIHandler installUIHandler, int installLogMode, IntPtr context);

        /// <summary>
        /// PInvoke of MsiSetInternalUI.
        /// </summary>
        /// <param name="uiLevel">Specifies the level of complexity of the user interface.</param>
        /// <param name="hwnd">Pointer to a window. This window becomes the owner of any user interface created.
        /// A pointer to the previous owner of the user interface is returned.
        /// If this parameter is null, the owner of the user interface does not change.</param>
        /// <returns>The previous user interface level is returned. If an invalid dwUILevel is passed, then INSTALLUILEVEL_NOCHANGE is returned.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiSetInternalUI", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiSetInternalUI(int uiLevel, ref IntPtr hwnd);

        /// <summary>
        /// PInvoke of MsiSummaryInfoGetPropertyW.
        /// </summary>
        /// <param name="summaryInfo">Handle to summary info.</param>
        /// <param name="property">Property to get value from.</param>
        /// <param name="dataType">Data type of property.</param>
        /// <param name="integerValue">Integer to receive integer value.</param>
        /// <param name="fileTimeValue">File time to receive file time value.</param>
        /// <param name="stringValueBuf">String buffer to receive string value.</param>
        /// <param name="stringValueBufSize">Size of string buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiSummaryInfoGetPropertyW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiSummaryInfoGetProperty(IntPtr summaryInfo, int property, out uint dataType, out int integerValue, ref System.Runtime.InteropServices.ComTypes.FILETIME fileTimeValue, StringBuilder stringValueBuf, ref int stringValueBufSize);

        /// <summary>
        /// PInvoke of MsiViewGetColumnInfo.
        /// </summary>
        /// <param name="view">Handle to view.</param>
        /// <param name="columnInfo">Column info.</param>
        /// <param name="record">Handle for returned record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewGetColumnInfo", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiViewGetColumnInfo(IntPtr view, int columnInfo, out IntPtr record);

        /// <summary>
        /// PInvoke of MsiViewExecute.
        /// </summary>
        /// <param name="view">Handle of view to execute.</param>
        /// <param name="record">Handle to a record that supplies the parameters for the view.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewExecute", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiViewExecute(IntPtr view, IntPtr record);

        /// <summary>
        /// PInvoke of MsiViewFetch.
        /// </summary>
        /// <param name="view">Handle of view to fetch a row from.</param>
        /// <param name="record">Handle to receive record info.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewFetch", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiViewFetch(IntPtr view, out IntPtr record);

        /// <summary>
        /// PInvoke of MsiViewModify.
        /// </summary>
        /// <param name="view">Handle of view to modify.</param>
        /// <param name="modifyMode">Modify mode.</param>
        /// <param name="record">Handle of record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewModify", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int MsiViewModify(IntPtr view, int modifyMode, IntPtr record);
    }
}
