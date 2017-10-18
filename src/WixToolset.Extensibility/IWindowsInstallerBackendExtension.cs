// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data.Rows;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface IWindowsInstallerBackendExtension
    {
        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        void PreBackendBind(IBindContext context);

        ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<BindFileWithPath> files);

        string ResolveMedia(MediaRow mediaRow, string mediaLayoutDirectory, string layoutDirectory);

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void PostBackendBind(BindResult result);
    }
}
