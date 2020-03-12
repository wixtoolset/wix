// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// WiX variable resolver.
    /// </summary>
    internal class VariableResolver : IVariableResolver
    {
        private readonly Dictionary<string, BindVariable> locVariables;
        private readonly Dictionary<string, BindVariable> wixVariables;
        private readonly Dictionary<string, LocalizedControl> localizedControls;

        /// <summary>
        /// Instantiate a new VariableResolver.
        /// </summary>
        internal VariableResolver(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();

            this.locVariables = new Dictionary<string, BindVariable>();
            this.wixVariables = new Dictionary<string, BindVariable>();
            this.localizedControls = new Dictionary<string, LocalizedControl>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        public int VariableCount => this.wixVariables.Count;

        public void AddLocalization(Localization localization)
        {
            foreach (var variable in localization.Variables)
            {
                if (!TryAddWixVariable(this.locVariables, variable))
                {
                    this.Messaging.Write(ErrorMessages.DuplicateLocalizationIdentifier(variable.SourceLineNumbers, variable.Id));
                }
            }

            foreach (KeyValuePair<string, LocalizedControl> localizedControl in localization.LocalizedControls)
            {
                if (!this.localizedControls.ContainsKey(localizedControl.Key))
                {
                    this.localizedControls.Add(localizedControl.Key, localizedControl.Value);
                }
            }
        }

        public void AddVariable(SourceLineNumber sourceLineNumber, string name, string value, bool overridable)
        {
            var bindVariable = new BindVariable { Id = name, Value = value, Overridable = overridable, SourceLineNumbers = sourceLineNumber };

            if (!TryAddWixVariable(this.wixVariables, bindVariable))
            {
                this.Messaging.Write(ErrorMessages.WixVariableCollision(sourceLineNumber, name));
            }
        }

        public IVariableResolution ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly)
        {
            return this.ResolveVariables(sourceLineNumbers, value, localizationOnly, true);
        }

        public bool TryGetLocalizedControl(string dialog, string control, out LocalizedControl localizedControl)
        {
            var key = LocalizedControl.GetKey(dialog, control);
            return this.localizedControls.TryGetValue(key, out localizedControl);
        }

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <param name="errorOnUnknown">true if unknown variables should throw errors.</param>
        /// <returns>The resolved value.</returns>
        internal IVariableResolution ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly, bool errorOnUnknown)
        {
            var matches = Common.WixVariableRegex.Matches(value);

            // the value is the default unless its substituted further down
            var result = this.ServiceProvider.GetService<IVariableResolution>();
            result.IsDefault = true;
            result.Value = value;

            while (!result.DelayedResolve && matches.Count > 0)
            {
                var sb = new StringBuilder(value);

                // notice how this code walks backward through the list
                // because it modifies the string as we move through it
                for (int i = matches.Count - 1; 0 <= i; i--)
                {
                    var variableNamespace = matches[i].Groups["namespace"].Value;
                    var variableId = matches[i].Groups["fullname"].Value;
                    string variableDefaultValue = null;

                    // get the default value if one was specified
                    if (matches[i].Groups["value"].Success)
                    {
                        variableDefaultValue = matches[i].Groups["value"].Value;

                        // localization variables do not support inline default values
                        if ("loc" == variableNamespace)
                        {
                            this.Messaging.Write(ErrorMessages.IllegalInlineLocVariable(sourceLineNumbers, variableId, variableDefaultValue));
                        }
                    }

                    // get the scope if one was specified
                    if (matches[i].Groups["scope"].Success)
                    {
                        if ("bind" == variableNamespace)
                        {
                            variableId = matches[i].Groups["name"].Value;
                        }
                    }

                    // check for an escape sequence of !! indicating the match is not a variable expression
                    if (0 < matches[i].Index && '!' == sb[matches[i].Index - 1])
                    {
                        if (!localizationOnly)
                        {
                            sb.Remove(matches[i].Index - 1, 1);

                            result.UpdatedValue = true;
                        }
                    }
                    else
                    {
                        string resolvedValue = null;

                        if ("loc" == variableNamespace)
                        {
                            // warn about deprecated syntax of $(loc.var)
                            if ('$' == sb[matches[i].Index])
                            {
                                this.Messaging.Write(WarningMessages.DeprecatedLocalizationVariablePrefix(sourceLineNumbers, variableId));
                            }

                            if (this.locVariables.TryGetValue(variableId, out var bindVariable))
                            {
                                resolvedValue = bindVariable.Value;
                            }
                        }
                        else if (!localizationOnly && "wix" == variableNamespace)
                        {
                            // illegal syntax of $(wix.var)
                            if ('$' == sb[matches[i].Index])
                            {
                                this.Messaging.Write(ErrorMessages.IllegalWixVariablePrefix(sourceLineNumbers, variableId));
                            }
                            else
                            {
                                if (this.wixVariables.TryGetValue(variableId, out var bindVariable))
                                {
                                    resolvedValue = bindVariable.Value ?? String.Empty;
                                    result.IsDefault = false;
                                }
                                else if (null != variableDefaultValue) // default the resolved value to the inline value if one was specified
                                {
                                    resolvedValue = variableDefaultValue;
                                }
                            }
                        }

                        if ("bind" == variableNamespace)
                        {
                            // can't resolve these yet, but keep track of where we find them so they can be resolved later with less effort
                            result.DelayedResolve = true;
                        }
                        else
                        {
                            // insert the resolved value if it was found or display an error
                            if (null != resolvedValue)
                            {
                                sb.Remove(matches[i].Index, matches[i].Length);
                                sb.Insert(matches[i].Index, resolvedValue);

                                result.UpdatedValue = true;
                            }
                            else if ("loc" == variableNamespace && errorOnUnknown) // unresolved loc variable
                            {
                                this.Messaging.Write(ErrorMessages.LocalizationVariableUnknown(sourceLineNumbers, variableId));
                            }
                            else if (!localizationOnly && "wix" == variableNamespace && errorOnUnknown) // unresolved wix variable
                            {
                                this.Messaging.Write(ErrorMessages.WixVariableUnknown(sourceLineNumbers, variableId));
                            }
                        }
                    }
                }

                result.Value = sb.ToString();
                value = result.Value;
                matches = Common.WixVariableRegex.Matches(value);
            }

            return result;
        }

        private static bool TryAddWixVariable(IDictionary<string, BindVariable> variables, BindVariable variable)
        {
            if (!variables.TryGetValue(variable.Id, out var existingWixVariableRow) || (existingWixVariableRow.Overridable && !variable.Overridable))
            {
                variables[variable.Id] = variable;
                return true;
            }

            return variable.Overridable;
        }
    }
}
