// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.Tuples
{
    using System;

    /// <summary>
    /// Summary information values for the CharCount property in transforms.
    /// </summary>
    [Flags]
    public enum TransformFlags
    {
        /// <summary>Ignore error when adding a row that exists.</summary>
        ErrorAddExistingRow = 0x1,

        /// <summary>Ignore error when deleting a row that does not exist.</summary>
        ErrorDeleteMissingRow = 0x2,

        /// <summary>Ignore error when adding a table that exists. </summary>
        ErrorAddExistingTable = 0x4,

        /// <summary>Ignore error when deleting a table that does not exist. </summary>
        ErrorDeleteMissingTable = 0x8,

        /// <summary>Ignore error when updating a row that does not exist. </summary>
        ErrorUpdateMissingRow = 0x10,

        /// <summary>Ignore error when transform and database code pages do not match, and their code pages are neutral.</summary>
        ErrorChangeCodePage = 0x20,

        /// <summary>Default language must match base database. </summary>
        ValidateLanguage = 0x10000,

        /// <summary>Product must match base database.</summary>
        ValidateProduct = 0x20000,

        /// <summary>Check major version only. </summary>
        ValidateMajorVersion = 0x80000,

        /// <summary>Check major and minor versions only. </summary>
        ValidateMinorVersion = 0x100000,

        /// <summary>Check major, minor, and update versions.</summary>
        ValidateUpdateVersion = 0x200000,

        /// <summary>Installed version lt base version. </summary>
        ValidateNewLessBaseVersion = 0x400000,

        /// <summary>Installed version lte base version. </summary>
        ValidateNewLessEqualBaseVersion = 0x800000,

        /// <summary>Installed version eq base version. </summary>
        ValidateNewEqualBaseVersion = 0x1000000,

        /// <summary>Installed version gte base version.</summary>
        ValidateNewGreaterEqualBaseVersion = 0x2000000,

        /// <summary>Installed version gt base version.</summary>
        ValidateNewGreaterBaseVersion = 0x4000000,

        /// <summary>UpgradeCode must match base database.</summary>
        ValidateUpgradeCode = 0x8000000,

        /// <summary>Masks all version checks on ProductVersion.</summary>
        ProductVersionMask = ValidateMajorVersion | ValidateMinorVersion | ValidateUpdateVersion,

        /// <summary>Masks all operations on ProductVersion.</summary>
        ProductVersionOperatorMask = ValidateNewLessBaseVersion | ValidateNewLessEqualBaseVersion | ValidateNewEqualBaseVersion | ValidateNewGreaterEqualBaseVersion | ValidateNewGreaterBaseVersion,

        /// <summary>Default value for instance transforms.</summary>
        InstanceTransformDefault = ErrorAddExistingRow | ErrorDeleteMissingRow | ErrorAddExistingTable | ErrorDeleteMissingTable | ErrorUpdateMissingRow | ErrorChangeCodePage | ValidateProduct | ValidateUpdateVersion | ValidateNewGreaterEqualBaseVersion,

        /// <summary>Default value for language transforms.</summary>
        LanguageTransformDefault = ErrorAddExistingRow | ErrorDeleteMissingRow | ErrorAddExistingTable | ErrorDeleteMissingTable | ErrorUpdateMissingRow | ErrorChangeCodePage | ValidateProduct,

        /// <summary>Default value for patch transforms.</summary>
        PatchTransformDefault = ErrorAddExistingRow | ErrorDeleteMissingRow | ErrorAddExistingTable | ErrorDeleteMissingTable | ErrorUpdateMissingRow | ValidateProduct | ValidateUpdateVersion | ValidateNewEqualBaseVersion | ValidateUpgradeCode,
    }
}
