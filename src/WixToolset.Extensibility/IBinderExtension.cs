// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface IBinderExtension
    {
        /// <summary>
        /// Gets or sets the binder core for the extension.
        /// </summary>
        /// <value>Binder core for the extension.</value>
        IBinderCore Core { get; set; }

        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        void Initialize(Output output);

        /// <summary>
        /// Called after variable resolution occurs.
        /// </summary>
        void AfterResolvedFields(Output output);

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void Finish(Output output);
    }
}
