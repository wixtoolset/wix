// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface for built-in optimizer.
    /// </summary>
    public interface IOptimizer
    {
        /// <summary>
        /// Called after all files have been compiled and before all sections are linked into a single section.
        /// </summary>
        void Optimize(IOptimizeContext context);
    }
}
