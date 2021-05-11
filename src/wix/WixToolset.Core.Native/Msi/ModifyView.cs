// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    /// <summary>
    /// Enumeration of different modify modes.
    /// </summary>
    public enum ModifyView
    {
        /// <summary>
        /// Writes current data in the cursor to a table row. Updates record if the primary 
        /// keys match an existing row and inserts if they do not match. Fails with a read-only 
        /// database. This mode cannot be used with a view containing joins.
        /// </summary>
        Assign = 3,   // Writes current data in the cursor to a table row. Updates record if the primary keys match an existing row and inserts if they do not match. Fails with a read-only database. This mode cannot be used with a view containing joins.

        /// <summary>
        /// Remove a row from the table. You must first call the Fetch function with the same
        /// record. Fails if the row has been deleted. Works only with read-write records. This
        /// mode cannot be used with a view containing joins.
        /// </summary>
        Delete = 6,   // Remove a row from the table. You must first call the MsiViewFetch function with the same record. Fails if the row has been deleted. Works only with read-write records. This mode cannot be used with a view containing joins.

        /// <summary>
        /// Inserts a record. Fails if a row with the same primary keys exists. Fails with a read-only
        /// database. This mode cannot be used with a view containing joins.
        /// </summary>
        Insert = 1,   // Inserts a record. Fails if a row with the same primary keys exists. Fails with a read-only database. This mode cannot be used with a view containing joins.

        /// <summary>
        /// Inserts a temporary record. The information is not persistent. Fails if a row with the 
        /// same primary key exists. Works only with read-write records. This mode cannot be 
        /// used with a view containing joins.
        /// </summary>
        InsertTemporary = 7,   // Inserts a temporary record. The information is not persistent. Fails if a row with the same primary key exists. Works only with read-write records. This mode cannot be used with a view containing joins.

        /// <summary>
        /// Inserts or validates a record in a table. Inserts if primary keys do not match any row
        /// and validates if there is a match. Fails if the record does not match the data in
        /// the table. Fails if there is a record with a duplicate key that is not identical.
        /// Works only with read-write records. This mode cannot be used with a view containing joins.
        /// </summary>
        Merge = 5,   // Inserts or validates a record in a table. Inserts if primary keys do not match any row and validates if there is a match. Fails if the record does not match the data in the table. Fails if there is a record with a duplicate key that is not identical. Works only with read-write records. This mode cannot be used with a view containing joins.

        /// <summary>
        /// Refreshes the information in the record. Must first call Fetch with the
        /// same record. Fails for a deleted row. Works with read-write and read-only records.
        /// </summary>
        Refresh = 0,   // Refreshes the information in the record. Must first call MsiViewFetch with the same record. Fails for a deleted row. Works with read-write and read-only records.

        /// <summary>
        /// Updates or deletes and inserts a record into a table. Must first call Fetch with
        /// the same record. Updates record if the primary keys are unchanged. Deletes old row and
        /// inserts new if primary keys have changed. Fails with a read-only database. This mode cannot
        /// be used with a view containing joins.
        /// </summary>
        Replace = 4,   // Updates or deletes and inserts a record into a table. Must first call MsiViewFetch with the same record. Updates record if the primary keys are unchanged. Deletes old row and inserts new if primary keys have changed. Fails with a read-only database. This mode cannot be used with a view containing joins.

        /// <summary>
        /// Refreshes the information in the supplied record without changing the position in the
        /// result set and without affecting subsequent fetch operations. The record may then
        /// be used for subsequent Update, Delete, and Refresh. All primary key columns of the
        /// table must be in the query and the record must have at least as many fields as the
        /// query. Seek cannot be used with multi-table queries. This mode cannot be used with
        /// a view containing joins. See also the remarks.
        /// </summary>
        Seek = -1,   // Refreshes the information in the supplied record without changing the position in the result set and without affecting subsequent fetch operations. The record may then be used for subsequent Update, Delete, and Refresh. All primary key columns of the table must be in the query and the record must have at least as many fields as the query. Seek cannot be used with multi-table queries. This mode cannot be used with a view containing joins. See also the remarks.

        /// <summary>
        /// Updates an existing record. Non-primary keys only. Must first call Fetch. Fails with a
        /// deleted record. Works only with read-write records.
        /// </summary>
        Update = 2,   // Updates an existing record. Nonprimary keys only. Must first call MsiViewFetch. Fails with a deleted record. Works only with read-write records.
    }
}
