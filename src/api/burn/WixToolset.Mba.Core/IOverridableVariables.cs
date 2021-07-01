// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// The case sensitivity of variables from the command line.
    /// </summary>
    public enum VariableCommandLineType
    {
        /// <summary>
        /// Similar to Windows Installer, all variable names specified on the command line are automatically converted to upper case.
        /// </summary>
        UpperCase,
        /// <summary>
        /// All variable names specified on the command line must match the case specified when building the bundle.
        /// </summary>
        CaseSensitive,
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