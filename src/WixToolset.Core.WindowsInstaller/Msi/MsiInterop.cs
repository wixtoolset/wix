// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Msi
{
    using System;
    using System.Text;
    using System.Runtime.InteropServices;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
    using WixToolset.Core.Native;

    /// <summary>
    /// A callback function that the installer calls for progress notification and error messages.
    /// </summary>
    /// <param name="context">Pointer to an application context.
    /// This parameter can be used for error checking.</param>
    /// <param name="messageType">Specifies a combination of one message box style,
    /// one message box icon type, one default button, and one installation message type.</param>
    /// <param name="message">Specifies the message text.</param>
    /// <returns>-1 for an error, 0 if no action was taken, 1 if OK, 3 to abort.</returns>
    public delegate int InstallUIHandler(IntPtr context, uint messageType, [MarshalAs(UnmanagedType.LPWStr)] string message);

    /// <summary>
    /// Enum of predefined persist modes used when opening a database.
    /// </summary>
    public enum OpenDatabase
    {
        /// <summary>
        /// Open a database read-only, no persistent changes.
        /// </summary>
        ReadOnly = 0,

        /// <summary>
        /// Open a database read/write in transaction mode.
        /// </summary>
        Transact = 1,

        /// <summary>
        /// Open a database direct read/write without transaction.
        /// </summary>
        Direct = 2,

        /// <summary>
        /// Create a new database, transact mode read/write.
        /// </summary>
        Create = 3,

        /// <summary>
        /// Create a new database, direct mode read/write.
        /// </summary>
        CreateDirect = 4,

        /// <summary>
        /// Indicates a patch file is being opened.
        /// </summary>
        OpenPatchFile = 32
    }

    /// <summary>
    /// The errors to suppress when applying a transform.
    /// </summary>
    [Flags]
    public enum TransformErrorConditions
    {
        /// <summary>
        /// None of the following conditions.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Suppress error when adding a row that exists.
        /// </summary>
        AddExistingRow = 0x1,

        /// <summary>
        /// Suppress error when deleting a row that does not exist.
        /// </summary>
        DeleteMissingRow = 0x2,

        /// <summary>
        /// Suppress error when adding a table that exists.
        /// </summary>
        AddExistingTable = 0x4,

        /// <summary>
        /// Suppress error when deleting a table that does not exist.
        /// </summary>
        DeleteMissingTable = 0x8,

        /// <summary>
        /// Suppress error when updating a row that does not exist.
        /// </summary>
        UpdateMissingRow = 0x10,

        /// <summary>
        /// Suppress error when transform and database code pages do not match, and their code pages are neutral.
        /// </summary>
        ChangeCodepage = 0x20,

        /// <summary>
        /// Create the temporary _TransformView table when applying a transform.
        /// </summary>
        ViewTransform = 0x100,

        /// <summary>
        /// Suppress all errors but the option to create the temporary _TransformView table.
        /// </summary>
        All = 0x3F
    }

    /// <summary>
    /// The validation to run while applying a transform.
    /// </summary>
    [Flags]
    public enum TransformValidations
    {
        /// <summary>
        /// Do not validate properties.
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Default language must match base database.
        /// </summary>
        Language = 0x1,

        /// <summary>
        /// Product must match base database.
        /// </summary>
        Product = 0x2,

        /// <summary>
        /// Check major version only.
        /// </summary>
        MajorVersion = 0x8,

        /// <summary>
        /// Check major and minor versions only.
        /// </summary>
        MinorVersion = 0x10,

        /// <summary>
        /// Check major, minor, and update versions.
        /// </summary>
        UpdateVersion = 0x20,

        /// <summary>
        /// Installed version &lt; base version.
        /// </summary>
        NewLessBaseVersion = 0x40,

        /// <summary>
        /// Installed version &lt;= base version.
        /// </summary>
        NewLessEqualBaseVersion = 0x80,

        /// <summary>
        /// Installed version = base version.
        /// </summary>
        NewEqualBaseVersion = 0x100,

        /// <summary>
        /// Installed version &gt;= base version.
        /// </summary>
        NewGreaterEqualBaseVersion = 0x200,

        /// <summary>
        /// Installed version &gt; base version.
        /// </summary>
        NewGreaterBaseVersion = 0x400,

        /// <summary>
        /// UpgradeCode must match base database.
        /// </summary>
        UpgradeCode = 0x800
    }

    /// <summary>
    /// Class exposing static functions and structs from MSI API.
    /// </summary>
    internal sealed class MsiInterop
    {
        // Patching constants
        public const int MsiMaxStreamNameLength = 62; // http://msdn2.microsoft.com/library/aa370551.aspx

        public const int MSICONDITIONFALSE = 0;   // The table is temporary.
        public const int MSICONDITIONTRUE = 1;   // The table is persistent.
        public const int MSICONDITIONNONE = 2;   // The table is unknown.
        public const int MSICONDITIONERROR = 3;   // An invalid handle or invalid parameter was passed to the function.
        /*
        public const int MSIDBOPENREADONLY = 0;
        public const int MSIDBOPENTRANSACT = 1;
        public const int MSIDBOPENDIRECT = 2;
        public const int MSIDBOPENCREATE = 3;
        public const int MSIDBOPENCREATEDIRECT = 4;
        public const int MSIDBOPENPATCHFILE = 32;

        public const int MSIMODIFYSEEK = -1;   // Refreshes the information in the supplied record without changing the position in the result set and without affecting subsequent fetch operations. The record may then be used for subsequent Update, Delete, and Refresh. All primary key columns of the table must be in the query and the record must have at least as many fields as the query. Seek cannot be used with multi-table queries. This mode cannot be used with a view containing joins. See also the remarks.
        public const int MSIMODIFYREFRESH = 0;   // Refreshes the information in the record. Must first call MsiViewFetch with the same record. Fails for a deleted row. Works with read-write and read-only records.
        public const int MSIMODIFYINSERT = 1;   // Inserts a record. Fails if a row with the same primary keys exists. Fails with a read-only database. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYUPDATE = 2;   // Updates an existing record. Nonprimary keys only. Must first call MsiViewFetch. Fails with a deleted record. Works only with read-write records.
        public const int MSIMODIFYASSIGN = 3;   // Writes current data in the cursor to a table row. Updates record if the primary keys match an existing row and inserts if they do not match. Fails with a read-only database. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYREPLACE = 4;   // Updates or deletes and inserts a record into a table. Must first call MsiViewFetch with the same record. Updates record if the primary keys are unchanged. Deletes old row and inserts new if primary keys have changed. Fails with a read-only database. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYMERGE = 5;   // Inserts or validates a record in a table. Inserts if primary keys do not match any row and validates if there is a match. Fails if the record does not match the data in the table. Fails if there is a record with a duplicate key that is not identical. Works only with read-write records. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYDELETE = 6;   // Remove a row from the table. You must first call the MsiViewFetch function with the same record. Fails if the row has been deleted. Works only with read-write records. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYINSERTTEMPORARY = 7;   // Inserts a temporary record. The information is not persistent. Fails if a row with the same primary key exists. Works only with read-write records. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYVALIDATE = 8;   // Validates a record. Does not validate across joins. You must first call the MsiViewFetch function with the same record. Obtain validation errors with MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYVALIDATENEW = 9;   // Validate a new record. Does not validate across joins. Checks for duplicate keys. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYVALIDATEFIELD = 10;   // Validates fields of a fetched or new record. Can validate one or more fields of an incomplete record. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.
        public const int MSIMODIFYVALIDATEDELETE = 11;   // Validates a record that will be deleted later. You must first call MsiViewFetch. Fails if another row refers to the primary keys of this row. Validation does not check for the existence of the primary keys of this row in properties or strings. Does not check if a column is a foreign key to multiple tables. Obtain validation errors by calling MsiViewGetError. Works with read-write and read-only records. This mode cannot be used with a view containing joins.

        public const uint VTI2 = 2;
        public const uint VTI4 = 3;
        public const uint VTLPWSTR = 30;
        public const uint VTFILETIME = 64;
        */

        public const int MSICOLINFONAMES = 0;  // return column names
        public const int MSICOLINFOTYPES = 1;  // return column definitions, datatype code followed by width

        /// <summary>
        /// Protect the constructor.
        /// </summary>
        private MsiInterop()
        {
        }

        /// <summary>
        /// PInvoke of MsiCloseHandle.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiCloseHandle", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiCloseHandle(uint database);

        /// <summary>
        /// PInvoke of MsiCreateRecord
        /// </summary>
        /// <param name="parameters">Count of columns in the record.</param>
        /// <returns>Handle referencing the record.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiCreateRecord", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern uint MsiCreateRecord(int parameters);

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
        public static extern int MsiCreateTransformSummaryInfo(uint database, uint referenceDatabase, string transformFile, TransformErrorConditions errorConditions, TransformValidations validations);

        /// <summary>
        /// Applies a transform to a database.
        /// </summary>
        /// <param name="database">Handle to the database obtained from MsiOpenDatabase to transform.</param>
        /// <param name="transformFile">Specifies the name of the transform file to apply.</param>
        /// <param name="errorConditions">Error conditions that should be suppressed.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseApplyTransformW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDatabaseApplyTransform(uint database, string transformFile, TransformErrorConditions errorConditions);

        /// <summary>
        /// PInvoke of MsiDatabaseCommit.
        /// </summary>
        /// <param name="database">Handle to a databse.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseCommit", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDatabaseCommit(uint database);

        /// <summary>
        /// PInvoke of MsiDatabaseExportW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseExportW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDatabaseExport(uint database, string tableName, string folderPath, string fileName);

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
        public static extern int MsiDatabaseGenerateTransform(uint database, uint databaseReference, string transformFile, int reserved1, int reserved2);

        /// <summary>
        /// PInvoke of MsiDatabaseImportW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="folderPath">Folder path.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseImportW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDatabaseImport(uint database, string folderPath, string fileName);

        /// <summary>
        /// PInvoke of MsiDatabaseMergeW.
        /// </summary>
        /// <param name="database">The handle to the database obtained from MsiOpenDatabase.</param>
        /// <param name="databaseMerge">The handle to the database obtained from MsiOpenDatabase to merge into the base database.</param>
        /// <param name="tableName">The name of the table to receive merge conflict information.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseMergeW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDatabaseMerge(uint database, uint databaseMerge, string tableName);

        /// <summary>
        /// PInvoke of MsiDatabaseOpenViewW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="query">SQL query.</param>
        /// <param name="view">View handle.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseOpenViewW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDatabaseOpenView(uint database, string query, out uint view);

        /// <summary>
        /// PInvoke of MsiGetFileHashW.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="options">Hash options (must be 0).</param>
        /// <param name="hash">Buffer to recieve hash.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetFileHashW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiGetFileHash(string filePath, uint options, MSIFILEHASHINFO hash);

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
        public static extern int MsiGetFileVersion(string filePath, StringBuilder versionBuf, ref int versionBufSize, StringBuilder langBuf, ref int langBufSize);

        /// <summary>
        /// PInvoke of MsiGetLastErrorRecord.
        /// </summary>
        /// <returns>Handle to error record if one exists.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetLastErrorRecord", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern uint MsiGetLastErrorRecord();

        /// <summary>
        /// PInvoke of MsiDatabaseGetPrimaryKeysW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <param name="record">Handle to receive resulting record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseGetPrimaryKeysW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDatabaseGetPrimaryKeys(uint database, string tableName, out uint record);

        /// <summary>
        /// PInvoke of MsiDoActionW.
        /// </summary>
        /// <param name="product">Handle to the installation provided to a DLL custom action or
        /// obtained through MsiOpenPackage, MsiOpenPackageEx, or MsiOpenProduct.</param>
        /// <param name="action">Specifies the action to execute.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDoActionW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDoAction(uint product, string action);

        /// <summary>
        /// PInvoke of MsiGetSummaryInformationW.  Can use either database handle or database path as input.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="databasePath">Path to a database.</param>
        /// <param name="updateCount">Max number of updated values.</param>
        /// <param name="summaryInfo">Handle to summary information.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiGetSummaryInformationW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiGetSummaryInformation(uint database, string databasePath, uint updateCount, ref uint summaryInfo);

        /// <summary>
        /// PInvoke of MsiDatabaseIsTablePersitentW.
        /// </summary>
        /// <param name="database">Handle to a database.</param>
        /// <param name="tableName">Table name.</param>
        /// <returns>MSICONDITION</returns>
        [DllImport("msi.dll", EntryPoint = "MsiDatabaseIsTablePersistentW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiDatabaseIsTablePersistent(uint database, string tableName);

        /// <summary>
        /// PInvoke of MsiOpenDatabaseW.
        /// </summary>
        /// <param name="databasePath">Path to database.</param>
        /// <param name="persist">Persist mode.</param>
        /// <param name="database">Handle to database.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiOpenDatabaseW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiOpenDatabase(string databasePath, IntPtr persist, out uint database);

        /// <summary>
        /// PInvoke of MsiOpenPackageW.
        /// </summary>
        /// <param name="packagePath">The path to the package.</param>
        /// <param name="product">A pointer to a variable that receives the product handle.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiOpenPackageW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiOpenPackage(string packagePath, out uint product);

        /// <summary>
        /// PInvoke of MsiRecordIsNull.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to check for null value.</param>
        /// <returns>true if the field is null, false if not, and an error code for any error.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordIsNull", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiRecordIsNull(uint record, int field);

        /// <summary>
        /// PInvoke of MsiRecordGetInteger.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to retrieve integer from.</param>
        /// <returns>Integer value.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetInteger", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiRecordGetInteger(uint record, int field);

        /// <summary>
        /// PInvoke of MsiRectordSetInteger.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set integer value in.</param>
        /// <param name="value">Value to set field to.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetInteger", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiRecordSetInteger(uint record, int field, int value);

        /// <summary>
        /// PInvoke of MsiRecordGetStringW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to get string value from.</param>
        /// <param name="valueBuf">Buffer to recieve value.</param>
        /// <param name="valueBufSize">Size of buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiRecordGetString(uint record, int field, StringBuilder valueBuf, ref int valueBufSize);

        /// <summary>
        /// PInvoke of MsiRecordSetStringW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set string value in.</param>
        /// <param name="value">String value.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiRecordSetString(uint record, int field, string value);

        /// <summary>
        /// PInvoke of MsiRecordSetStreamW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to set stream value in.</param>
        /// <param name="filePath">Path to file to set stream value to.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordSetStreamW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiRecordSetStream(uint record, int field, string filePath);

        /// <summary>
        /// PInvoke of MsiRecordReadStreamW.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <param name="field">Index of field to read stream from.</param>
        /// <param name="dataBuf">Data buffer to recieve stream value.</param>
        /// <param name="dataBufSize">Size of data buffer.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordReadStream", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiRecordReadStream(uint record, int field, byte[] dataBuf, ref int dataBufSize);

        /// <summary>
        /// PInvoke of MsiRecordGetFieldCount.
        /// </summary>
        /// <param name="record">MSI Record handle.</param>
        /// <returns>Count of fields in the record.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiRecordGetFieldCount", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiRecordGetFieldCount(uint record);

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
        public static extern InstallUIHandler MsiSetExternalUI(InstallUIHandler installUIHandler, int installLogMode, IntPtr context);

        /// <summary>
        /// PInvoke of MsiSetpublicUI.
        /// </summary>
        /// <param name="uiLevel">Specifies the level of complexity of the user interface.</param>
        /// <param name="hwnd">Pointer to a window. This window becomes the owner of any user interface created.
        /// A pointer to the previous owner of the user interface is returned.
        /// If this parameter is null, the owner of the user interface does not change.</param>
        /// <returns>The previous user interface level is returned. If an invalid dwUILevel is passed, then INSTALLUILEVEL_NOCHANGE is returned.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiSetpublicUI", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiSetInternalUI(int uiLevel, ref IntPtr hwnd);

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
        public static extern int MsiSummaryInfoGetProperty(uint summaryInfo, int property, out uint dataType, out int integerValue, ref FILETIME fileTimeValue, StringBuilder stringValueBuf, ref int stringValueBufSize);

        /// <summary>
        /// PInvoke of MsiViewGetColumnInfo.
        /// </summary>
        /// <param name="view">Handle to view.</param>
        /// <param name="columnInfo">Column info.</param>
        /// <param name="record">Handle for returned record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewGetColumnInfo", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiViewGetColumnInfo(uint view, int columnInfo, out uint record);

        /// <summary>
        /// PInvoke of MsiViewExecute.
        /// </summary>
        /// <param name="view">Handle of view to execute.</param>
        /// <param name="record">Handle to a record that supplies the parameters for the view.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewExecute", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiViewExecute(uint view, uint record);

        /// <summary>
        /// PInvoke of MsiViewFetch.
        /// </summary>
        /// <param name="view">Handle of view to fetch a row from.</param>
        /// <param name="record">Handle to receive record info.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewFetch", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiViewFetch(uint view, out uint record);

        /// <summary>
        /// PInvoke of MsiViewModify.
        /// </summary>
        /// <param name="view">Handle of view to modify.</param>
        /// <param name="modifyMode">Modify mode.</param>
        /// <param name="record">Handle of record.</param>
        /// <returns>Error code.</returns>
        [DllImport("msi.dll", EntryPoint = "MsiViewModify", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern int MsiViewModify(uint view, int modifyMode, uint record);
    }
}
