// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all backends implement.
    /// </summary>
    public interface IBackend
    {
        /// <summary>
        /// Bind the intermediate into the final output.
        /// </summary>
        /// <param name="context">Bind context.</param>
        /// <returns>Result of the bind operation.</returns>
        IBindResult Bind(IBindContext context);
    }
}
