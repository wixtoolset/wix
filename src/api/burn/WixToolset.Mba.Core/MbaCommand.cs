// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// Default implementation of <see cref="IMbaCommand"/>.
    /// </summary>
    internal sealed class MbaCommand : IMbaCommand
    {
        public string[] UnknownCommandLineArgs { get; internal set; }

        public KeyValuePair<string, string>[] Variables { get; internal set; }

        internal MbaCommand() { }

        public void SetOverridableVariables(IOverridableVariables overridableVariables, IEngine engine)
        {
            foreach (var kvp in this.Variables)
            {
                var key = overridableVariables.CommandLineType == VariableCommandLineType.UpperCase ? kvp.Key.ToUpperInvariant() : kvp.Key;

                if (!overridableVariables.Variables.TryGetValue(key, out var overridableVariable))
                {
                    engine.Log(LogLevel.Error, string.Format("Ignoring attempt to set non-overridable variable: '{0}'.", key));
                }
                else
                {
                    engine.SetVariableString(overridableVariable.Name, kvp.Value, false);
                }
            }
        }
    }
}
