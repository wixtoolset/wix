// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Base class for creating an optimizer extension.
    /// </summary>
    public abstract class BaseOptimizerExtension : IOptimizerExtension
    {
        /// <summary>
        /// Called after all files have been compiled, before built-in optimizations, and before all sections are linked into a single section.
        /// </summary>
        public virtual void PreOptimize(IOptimizeContext context)
        {
        }

        /// <summary>
        /// Called after all files have been compiled, after built-in optimizations, and before all sections are linked into a single section.
        /// </summary>
        public virtual void PostOptimize(IOptimizeContext context)
        {
        }
    }
}
