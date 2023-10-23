// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface that all optimizer extensions implement.
    /// </summary>
    public interface IOptimizerExtension
    {
        /// <summary>
        /// Called after all files have been compiled, before built-in optimizations, and before all sections are linked into a single section.
        /// </summary>
        void PreOptimize(IOptimizeContext context);

        /// <summary>
        /// Called after all files have been compiled, after built-in optimizations, and before all sections are linked into a single section.
        /// </summary>
        void PostOptimize(IOptimizeContext context);
    }
}
