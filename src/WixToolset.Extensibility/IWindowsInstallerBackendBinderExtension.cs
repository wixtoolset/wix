// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface IWindowsInstallerBackendBinderExtension
    {
        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        void PreBackendBind(IBindContext context);

        IResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<IBindFileWithPath> files);

        string ResolveMedia(MediaTuple mediaRow, string mediaLayoutDirectory, string layoutDirectory);

        bool TryAddTupleToOutput(IntermediateTuple tuple, WindowsInstallerData output);

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void PostBackendBind(IBindResult result, WixOutput wixout);
    }
}
