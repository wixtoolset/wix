// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// Command information parsed from the command line.
    /// </summary>
    public interface IMbaCommand
    {
        /// <summary>
        /// The command line arguments not parsed into <see cref="IBootstrapperCommand"/> or <see cref="IMbaCommand"/>.
        /// </summary>
        string[] UnknownCommandLineArgs { get; }

        /// <summary>
        /// The variables that were parsed from the command line.
        /// Key = variable name, Value = variable value.
        /// </summary>
        KeyValuePair<string, string>[] Variables { get; }

        /// <summary>
        /// Sets overridable variables from the command line.
        /// </summary>
        /// <param name="overridableVariables">The overridable variable information from <see cref="IBootstrapperApplicationData"/>.</param>
        /// <param name="engine">The engine.</param>
        void SetOverridableVariables(IOverridableVariables overridableVariables, IEngine engine);
    }
}
