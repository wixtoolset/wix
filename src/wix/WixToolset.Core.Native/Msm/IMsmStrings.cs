// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msm
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// A collection of strings.
    /// </summary>
    [ComImport, Guid("0ADDA827-2C26-11D2-AD65-00A0C9AF11A6")]
    public interface IMsmStrings
    {
        /// <summary>
        /// Gets the string at the specified index.
        /// </summary>
        /// <param name="index">The one-based index of the string to get.</param>
        string this[int index]
        {
            get;
        }

        /// <summary>
        /// Gets the count of strings in this collection.
        /// </summary>
        /// <value>The count of strings in this collection.</value>
        int Count
        {
            get;
        }
    }
}
