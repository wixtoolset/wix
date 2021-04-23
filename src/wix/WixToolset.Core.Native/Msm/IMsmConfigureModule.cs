// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msm
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Callback for configurable merge modules.
    /// </summary>
    [ComImport, Guid("AC013209-18A7-4851-8A21-2353443D70A0"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IMsmConfigureModule
    {
        /// <summary>
        /// Callback to retrieve text data for configurable merge modules.
        /// </summary>
        /// <param name="name">Name of the data to be retrieved.</param>
        /// <param name="configData">The data corresponding to the name.</param>
        /// <returns>The error code (HRESULT).</returns>
        [PreserveSig]
        int ProvideTextData([In, MarshalAs(UnmanagedType.BStr)] string name, [MarshalAs(UnmanagedType.BStr)] out string configData);

        /// <summary>
        /// Callback to retrieve integer data for configurable merge modules.
        /// </summary>
        /// <param name="name">Name of the data to be retrieved.</param>
        /// <param name="configData">The data corresponding to the name.</param>
        /// <returns>The error code (HRESULT).</returns>
        [PreserveSig]
        int ProvideIntegerData([In, MarshalAs(UnmanagedType.BStr)] string name, out int configData);
    }
}
