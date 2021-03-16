// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msm
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Collection of merge errors.
    /// </summary>
    [ComImport, Guid("0ADDA82A-2C26-11D2-AD65-00A0C9AF11A6")]
    public interface IMsmErrors
    {
        /// <summary>
        /// Gets the IMsmError at the specified index.
        /// </summary>
        /// <param name="index">The one-based index of the IMsmError to get.</param>
        IMsmError this[int index]
        {
            get;
        }

        /// <summary>
        /// Gets the count of IMsmErrors in this collection.
        /// </summary>
        /// <value>The count of IMsmErrors in this collection.</value>
        int Count
        {
            get;
        }
    }
}
