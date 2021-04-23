// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msm
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A merge error.
    /// </summary>
    [ComImport, Guid("0ADDA828-2C26-11D2-AD65-00A0C9AF11A6")]
    public interface IMsmError
    {
        /// <summary>
        /// Gets the type of merge error.
        /// </summary>
        /// <value>The type of merge error.</value>
        MsmErrorType Type
        {
            get;
        }

        /// <summary>
        /// Gets the path information from the merge error.
        /// </summary>
        /// <value>The path information from the merge error.</value>
        string Path
        {
            get;
        }

        /// <summary>
        /// Gets the language information from the merge error.
        /// </summary>
        /// <value>The language information from the merge error.</value>
        short Language
        {
            get;
        }

        /// <summary>
        /// Gets the database table from the merge error.
        /// </summary>
        /// <value>The database table from the merge error.</value>
        string DatabaseTable
        {
            get;
        }

        /// <summary>
        /// Gets the collection of database keys from the merge error.
        /// </summary>
        /// <value>The collection of database keys from the merge error.</value>
        IMsmStrings DatabaseKeys
        {
            get;
        }

        /// <summary>
        /// Gets the module table from the merge error.
        /// </summary>
        /// <value>The module table from the merge error.</value>
        string ModuleTable
        {
            get;
        }

        /// <summary>
        /// Gets the collection of module keys from the merge error.
        /// </summary>
        /// <value>The collection of module keys from the merge error.</value>
        IMsmStrings ModuleKeys
        {
            get;
        }
    }
}
