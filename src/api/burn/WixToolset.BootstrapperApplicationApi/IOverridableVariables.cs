// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System.Collections.Generic;

    /// <summary>
    /// The case sensitivity of variables from the command line.
    /// </summary>
    public enum VariableCommandLineType
    {
        /// <summary>
        /// All variable names specified on the command line must match the case specified when building the bundle.
        /// </summary>
        CaseSensitive,
        /// <summary>
        /// Variable names specified on the command line do not have to match the case specified when building the bundle.
        /// </summary>
        CaseInsensitive,
    }

    /// <summary>
    /// Overridable variable information from the BA manifest.
    /// </summary>
    public interface IOverridableVariables
    {
        /// <summary>
        /// The <see cref="VariableCommandLineType"/> of the bundle.
        /// </summary>
        VariableCommandLineType CommandLineType { get; }

        /// <summary>
        /// Variable Dictionary of variable name to <see cref="IOverridableVariableInfo"/>.
        /// </summary>
        IDictionary<string, IOverridableVariableInfo> Variables { get; }
    }
}
