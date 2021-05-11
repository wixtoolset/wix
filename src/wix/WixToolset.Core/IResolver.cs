// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Resolves localization and bind variables.
    /// </summary>
    public interface IResolver
    {
        /// <summary>
        /// Resolve localization and bind variables.
        /// </summary>
        /// <param name="context">Resolve context.</param>
        /// <returns>Resolve result.</returns>
        IResolveResult Resolve(IResolveContext context);
    }
}
