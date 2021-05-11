// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msm
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Errors returned by merge operations.
    /// </summary>
    [Guid("0ADDA825-2C26-11D2-AD65-00A0C9AF11A6")]
    public enum MsmErrorType
    {
        /// <summary>
        /// A request was made to open a module with a language not supported by the module.
        /// No more general language is supported by the module.
        /// Adds msmErrorLanguageUnsupported to the Type property and the requested language
        /// to the Language Property (Error Object).  All Error object properties are empty.
        /// The OpenModule function returns ERROR_INSTALL_LANGUAGE_UNSUPPORTED (as HRESULT).
        /// </summary>
        msmErrorLanguageUnsupported = 1,

        /// <summary>
        /// A request was made to open a module with a supported language but the module has
        /// an invalid language transform.  Adds msmErrorLanguageFailed to the Type property
        /// and the applied transform's language to the Language Property of the Error object.
        /// This may not be the requested language if a more general language was used.
        /// All other properties of the Error object are empty.  The OpenModule function
        /// returns ERROR_INSTALL_LANGUAGE_UNSUPPORTED (as HRESULT).
        /// </summary>
        msmErrorLanguageFailed = 2,

        /// <summary>
        /// The module cannot be merged because it excludes, or is excluded by, another module
        /// in the database.  Adds msmErrorExclusion to the Type property of the Error object.
        /// The ModuleKeys property or DatabaseKeys property contains the primary keys of the
        /// excluded module's row in the ModuleExclusion table.  If an existing module excludes
        /// the module being merged, the excluded module's ModuleSignature information is added
        /// to ModuleKeys.  If the module being merged excludes an existing module, DatabaseKeys
        /// contains the excluded module's ModuleSignature information.  All other properties
        /// are empty (or -1).
        /// </summary>
        msmErrorExclusion = 3,

        /// <summary>
        /// Merge conflict during merge.  The value of the Type property is set to
        /// msmErrorTableMerge.  The DatabaseTable property and DatabaseKeys property contain
        /// the table name and primary keys of the conflicting row in the database.  The
        /// ModuleTable property and ModuleKeys property contain the table name and primary keys
        /// of the conflicting row in the module.  The ModuleTable and ModuleKeys entries may be
        /// null if the row does not exist in the database.  For example, if the conflict is in a
        /// generated FeatureComponents table entry.  On Windows Installer version 2.0, when
        /// merging a configurable merge module, configuration may cause these properties to
        /// refer to rows that do not exist in the module.
        /// </summary>
        msmErrorTableMerge = 4,

        /// <summary>
        /// There was a problem resequencing a sequence table to contain the necessary merged
        /// actions.  The Type property is set to msmErrorResequenceMerge. The DatabaseTable
        /// and DatabaseKeys properties contain the sequence table name and primary keys
        /// (action name) of the conflicting row.  The ModuleTable and ModuleKeys properties
        /// contain the sequence table name and primary key (action name) of the conflicting row.
        /// On Windows Installer version 2.0, when merging a configurable merge module,
        /// configuration may cause these properties to refer to rows that do not exist in the module.
        /// </summary>
        msmErrorResequenceMerge = 5,

        /// <summary>
        /// Not used.
        /// </summary>
        msmErrorFileCreate = 6,

        /// <summary>
        /// There was a problem creating a directory to extract a file to disk.  The Path property
        /// contains the directory that could not be created.  All other properties are empty or -1.
        /// Not available with Windows Installer version 1.0.
        /// </summary>
        msmErrorDirCreate = 7,

        /// <summary>
        /// A feature name is required to complete the merge, but no feature name was provided.
        /// The Type property is set to msmErrorFeatureRequired.  The DatabaseTable and DatabaseKeys
        /// contain the table name and primary keys of the conflicting row.  The ModuleTable and
        /// ModuleKeys properties contain the table name and primary keys of the row cannot be merged.
        /// On Windows Installer version 2.0, when merging a configurable merge module, configuration
        /// may cause these properties to refer to rows that do not exist in the module.
        /// If the failure is in a generated FeatureComponents table, the DatabaseTable and
        /// DatabaseKeys properties are empty and the ModuleTable and ModuleKeys properties refer to
        /// the row in the Component table causing the failure.
        /// </summary>
        msmErrorFeatureRequired = 8,

        /// <summary>
        /// Available with Window Installer version 2.0. Substitution of a Null value into a
        /// non-nullable column.  This enters msmErrorBadNullSubstitution in the Type property and
        /// enters "ModuleSubstitution" and the keys from the ModuleSubstitution table for this row
        /// into the ModuleTable property and ModuleKeys property.  All other properties of the Error
        /// object are set to an empty string or -1.  This error causes the immediate failure of the
        /// merge and the MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorBadNullSubstitution = 9,

        /// <summary>
        /// Available with Window Installer version 2.0.  Substitution of Text Format Type or Integer
        /// Format Type into a Binary Type data column.  This type of error returns
        /// msmErrorBadSubstitutionType in the Type property and enters "ModuleSubstitution" and the
        /// keys from the ModuleSubstitution table for this row into the ModuleTable property.
        /// All other properties of the Error object are set to an empty string or -1.  This error
        /// causes the immediate failure of the merge and the MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorBadSubstitutionType = 10,

        /// <summary>
        /// Available with Window Installer Version 2.0.  A row in the ModuleSubstitution table
        /// references a configuration item not defined in the ModuleConfiguration table.
        /// This type of error returns msmErrorMissingConfigItem in the Type property and enters
        /// "ModuleSubstitution" and the keys from the ModuleSubstitution table for this row into
        /// the ModuleTable property. All other properties of the Error object are set to an empty
        /// string or -1.  This error causes the immediate failure of the merge and the MergeEx
        /// function to return E_FAIL.
        /// </summary>
        msmErrorMissingConfigItem = 11,

        /// <summary>
        /// Available with Window Installer version 2.0.  The authoring tool has returned a Null
        /// value for an item marked with the msmConfigItemNonNullable attribute.  An error of this
        /// type returns msmErrorBadNullResponse in the Type property and enters "ModuleSubstitution"
        /// and the keys from the ModuleSubstitution table for for the item into the ModuleTable property.
        /// All other properties of the Error object are set to an empty string or -1.  This error
        /// causes the immediate failure of the merge and the MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorBadNullResponse = 12,

        /// <summary>
        /// Available with Window Installer version 2.0.  The authoring tool returned a failure code
        /// (not S_OK or S_FALSE) when asked for data. An error of this type will return
        /// msmErrorDataRequestFailed in the Type property and enters "ModuleSubstitution"
        /// and the keys from the ModuleSubstitution table for the item into the ModuleTable property.
        /// All other properties of the Error object are set to an empty string or -1.  This error
        /// causes the immediate failure of the merge and the MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorDataRequestFailed = 13,

        /// <summary>
        /// Available with Windows Installer 2.0 and later versions.  Indicates that an attempt was
        /// made to merge a 64-bit module into a package that was not a 64-bit package.  An error of
        /// this type returns msmErrorPlatformMismatch in the Type property.  All other properties of
        /// the error object are set to an empty string or -1. This error causes the immediate failure
        /// of the merge and causes the Merge function or MergeEx function to return E_FAIL.
        /// </summary>
        msmErrorPlatformMismatch = 14,
    }
}
