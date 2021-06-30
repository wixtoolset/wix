// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// Overridable variable information from the BA manifest.
    /// </summary>
    public interface IOverridableVariables
    {
        /// <summary>
        /// Variable Dictionary of variable name to <see cref="IOverridableVariableInfo"/>.
        /// </summary>
        IDictionary<string, IOverridableVariableInfo> Variables { get; }
    }
}