// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Create libraries from input intermediates.
    /// </summary>
    public interface ILibrarian
    {
        /// <summary>
        /// Combine intermediates into a single result.
        /// </summary>
        /// <param name="context">Library context.</param>
        /// <returns>Library result.</returns>
        ILibraryResult Combine(ILibraryContext context);
    }
}
