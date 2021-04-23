// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;

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
}
