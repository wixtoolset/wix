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
        internal VariableResolver(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();

            this.locVariables = new Dictionary<string, BindVariable>();
            this.wixVariables = new Dictionary<string, BindVariable>();
            this.localizedControls = new Dictionary<string, LocalizedControl>();
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

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

        public IVariableResolution ResolveVariables(SourceLineNumber sourceLineNumbers, string value)
        {
            return this.ResolveVariables(sourceLineNumbers, value, errorOnUnknown: true);
        }

        public bool TryGetLocalizedControl(string dialog, string control, out LocalizedControl localizedControl)
        {
            var key = LocalizedControl.GetKey(dialog, control);
            return this.localizedControls.TryGetValue(key, out localizedControl);
        }

        public IVariableResolution ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool errorOnUnknown)
        {
            var start = 0;
            var defaulted = true;
            var delayed = false;
            var updated = false;

            while (Common.TryParseWixVariable(value, start, out var parsed))
            {
                var variableNamespace = parsed.Namespace;
                var variableId = parsed.Name;
                var variableDefaultValue = parsed.DefaultValue;

                // check for an escape sequence of !! indicating the match is not a variable expression
                if (0 < parsed.Index && '!' == value[parsed.Index - 1])
                {
                    var sb = new StringBuilder(value);
                    sb.Remove(parsed.Index - 1, 1);
                    value = sb.ToString();

                    updated = true;
                    start = parsed.Index + parsed.Length - 1;

                    continue;
                }

                string resolvedValue = null;

                if ("loc" == variableNamespace)
                {
                    // localization variables do not support inline default values
                    if (variableDefaultValue != null)
                    {
                        this.Messaging.Write(ErrorMessages.IllegalInlineLocVariable(sourceLineNumbers, variableId, variableDefaultValue));
                        continue;
                    }

                    if (this.locVariables.TryGetValue(variableId, out var bindVariable))
                    {
                        resolvedValue = bindVariable.Value;
                    }
                }
                else if ("wix" == variableNamespace)
                {
                    if (this.wixVariables.TryGetValue(variableId, out var bindVariable))
                    {
                        resolvedValue = bindVariable.Value ?? String.Empty;
                        defaulted = false;
                    }
                    else if (null != variableDefaultValue) // default the resolved value to the inline value if one was specified
                    {
                        resolvedValue = variableDefaultValue;
                    }
                }

                if ("bind" == variableNamespace)
                {
                    // Can't resolve these yet, but keep track of where we find them so they can be resolved later with less effort.
                    delayed = true;
                    start = parsed.Index + parsed.Length - 1;
                }
                else
                {
                    // insert the resolved value if it was found or display an error
                    if (null != resolvedValue)
                    {
                        if (parsed.Index == 0 && parsed.Length == value.Length)
                        {
                            value = resolvedValue;
                        }
                        else
                        {
                            var sb = new StringBuilder(value);
                            sb.Remove(parsed.Index, parsed.Length);
                            sb.Insert(parsed.Index, resolvedValue);
                            value = sb.ToString();
                        }

                        updated = true;
                        start = parsed.Index;
                    }
                    else
                    {
                        if ("loc" == variableNamespace && errorOnUnknown) // unresolved loc variable
                        {
                            this.Messaging.Write(ErrorMessages.LocalizationVariableUnknown(sourceLineNumbers, variableId));
                        }
                        else if ("wix" == variableNamespace && errorOnUnknown) // unresolved wix variable
                        {
                            this.Messaging.Write(ErrorMessages.WixVariableUnknown(sourceLineNumbers, variableId));
                        }

                        start = parsed.Index + parsed.Length;
                    }
                }
            }

            return new VariableResolution
            {
                DelayedResolve = delayed,
                IsDefault = defaulted,
                UpdatedValue = updated,
                Value = value,
            };
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
