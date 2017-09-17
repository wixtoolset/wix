// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    /// <summary>
    /// WiX variable resolver.
    /// </summary>
    public sealed class WixVariableResolver
    {
        private Localizer localizer;
        private Dictionary<string, string> wixVariables;

        /// <summary>
        /// Instantiate a new WixVariableResolver.
        /// </summary>
        public WixVariableResolver()
        {
            this.wixVariables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the localizer.
        /// </summary>
        /// <value>The localizer.</value>
        public Localizer Localizer
        {
            get { return this.localizer; }
            set { this.localizer = value; }
        }

        /// <summary>
        /// Gets the count of variables added to the resolver.
        /// </summary>
        public int VariableCount
        {
            get { return this.wixVariables.Count; }
        }

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value of the variable.</param>
        public void AddVariable(string name, string value)
        {
            try
            {
                this.wixVariables.Add(name, value);
            }
            catch (ArgumentException)
            {
                Messaging.Instance.OnMessage(WixErrors.WixVariableCollision(null, name));
            }
        }

        /// <summary>
        /// Add a variable.
        /// </summary>
        /// <param name="wixVariableRow">The WixVariableRow to add.</param>
        public void AddVariable(WixVariableRow wixVariableRow)
        {
            try
            {
                this.wixVariables.Add(wixVariableRow.Id, wixVariableRow.Value);
            }
            catch (ArgumentException)
            {
                if (!wixVariableRow.Overridable) // collision
                {
                    Messaging.Instance.OnMessage(WixErrors.WixVariableCollision(wixVariableRow.SourceLineNumbers, wixVariableRow.Id));
                }
            }
        }

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <returns>The resolved value.</returns>
        public string ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly)
        {
            bool isDefault = false;
            bool delayedResolve = false;

            return this.ResolveVariables(sourceLineNumbers, value, localizationOnly, ref isDefault, ref delayedResolve);
        }

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <param name="isDefault">true if the resolved value was the default.</param>
        /// <returns>The resolved value.</returns>
        public string ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly, ref bool isDefault)
        {
            bool delayedResolve = false;

            return this.ResolveVariables(sourceLineNumbers, value, localizationOnly, ref isDefault, ref delayedResolve);
        }

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <param name="errorOnUnknown">true if unknown variables should throw errors.</param>
        /// <param name="isDefault">true if the resolved value was the default.</param>
        /// <param name="delayedResolve">true if the value has variables that cannot yet be resolved.</param>
        /// <returns>The resolved value.</returns>
        public string ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly, ref bool isDefault, ref bool delayedResolve)
        {
            return this.ResolveVariables(sourceLineNumbers, value, localizationOnly, true, ref isDefault, ref delayedResolve);
        }

        /// <summary>
        /// Resolve the wix variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="localizationOnly">true to only resolve localization variables; false otherwise.</param>
        /// <param name="errorOnUnknown">true if unknown variables should throw errors.</param>
        /// <param name="isDefault">true if the resolved value was the default.</param>
        /// <param name="delayedResolve">true if the value has variables that cannot yet be resolved.</param>
        /// <returns>The resolved value.</returns>
        public string ResolveVariables(SourceLineNumber sourceLineNumbers, string value, bool localizationOnly, bool errorOnUnknown, ref bool isDefault, ref bool delayedResolve)
        {
            MatchCollection matches = Common.WixVariableRegex.Matches(value);

            // the value is the default unless its substituted further down
            isDefault = true;
            delayedResolve = false;

            if (0 < matches.Count)
            {
                StringBuilder sb = new StringBuilder(value);

                // notice how this code walks backward through the list
                // because it modifies the string as we through it
                for (int i = matches.Count - 1; 0 <= i; i--)
                {
                    string variableNamespace = matches[i].Groups["namespace"].Value;
                    string variableId = matches[i].Groups["fullname"].Value;
                    string variableDefaultValue = null;

                    // get the default value if one was specified
                    if (matches[i].Groups["value"].Success)
                    {
                        variableDefaultValue = matches[i].Groups["value"].Value;

                        // localization variables to not support inline default values
                        if ("loc" == variableNamespace)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalInlineLocVariable(sourceLineNumbers, variableId, variableDefaultValue));
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
                                Messaging.Instance.OnMessage(WixWarnings.DeprecatedLocalizationVariablePrefix(sourceLineNumbers, variableId));
                            }

                            if (null != this.localizer)
                            {
                                resolvedValue = this.localizer.GetLocalizedValue(variableId);
                            }
                        }
                        else if (!localizationOnly && "wix" == variableNamespace)
                        {
                            // illegal syntax of $(wix.var)
                            if ('$' == sb[matches[i].Index])
                            {
                                Messaging.Instance.OnMessage(WixErrors.IllegalWixVariablePrefix(sourceLineNumbers, variableId));
                            }
                            else
                            {
                                if (this.wixVariables.TryGetValue(variableId, out resolvedValue))
                                {
                                    resolvedValue = resolvedValue ?? String.Empty;
                                    isDefault = false;
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
                            delayedResolve = true;
                        }
                        else
                        {
                            // insert the resolved value if it was found or display an error
                            if (null != resolvedValue)
                            {
                                sb.Remove(matches[i].Index, matches[i].Length);
                                sb.Insert(matches[i].Index, resolvedValue);
                            }
                            else if ("loc" == variableNamespace && errorOnUnknown) // unresolved loc variable
                            {
                                Messaging.Instance.OnMessage(WixErrors.LocalizationVariableUnknown(sourceLineNumbers, variableId));
                            }
                            else if (!localizationOnly && "wix" == variableNamespace && errorOnUnknown) // unresolved wix variable
                            {
                                Messaging.Instance.OnMessage(WixErrors.WixVariableUnknown(sourceLineNumbers, variableId));
                            }
                        }
                    }
                }

                value = sb.ToString();
            }

            return value;
        }

        /// <summary>
        /// Resolve the delay variables in a value.
        /// </summary>
        /// <param name="sourceLineNumbers">The source line information for the value.</param>
        /// <param name="value">The value to resolve.</param>
        /// <param name="resolutionData"></param>
        /// <returns>The resolved value.</returns>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "sourceLineNumbers")]
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "This string is not round tripped, and not used for any security decisions")]
        public static string ResolveDelayedVariables(SourceLineNumber sourceLineNumbers, string value, IDictionary<string, string> resolutionData)
        {
            MatchCollection matches = Common.WixVariableRegex.Matches(value);

            if (0 < matches.Count)
            {
                StringBuilder sb = new StringBuilder(value);

                // notice how this code walks backward through the list
                // because it modifies the string as we go through it
                for (int i = matches.Count - 1; 0 <= i; i--)
                {
                    string variableNamespace = matches[i].Groups["namespace"].Value;
                    string variableId = matches[i].Groups["fullname"].Value;
                    string variableDefaultValue = null;
                    string variableScope = null;

                    // get the default value if one was specified
                    if (matches[i].Groups["value"].Success)
                    {
                        variableDefaultValue = matches[i].Groups["value"].Value;
                    }

                    // get the scope if one was specified
                    if (matches[i].Groups["scope"].Success)
                    {
                        variableScope = matches[i].Groups["scope"].Value;
                        if ("bind" == variableNamespace)
                        {
                            variableId = matches[i].Groups["name"].Value;
                        }
                    }

                    // check for an escape sequence of !! indicating the match is not a variable expression
                    if (0 < matches[i].Index && '!' == sb[matches[i].Index - 1])
                    {
                        sb.Remove(matches[i].Index - 1, 1);
                    }
                    else
                    {
                        string key = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", variableId, variableScope).ToLower(CultureInfo.InvariantCulture);
                        string resolvedValue = variableDefaultValue;

                        if (resolutionData.ContainsKey(key))
                        {
                            resolvedValue = resolutionData[key];
                        }

                        if ("bind" == variableNamespace)
                        {
                            // insert the resolved value if it was found or display an error
                            if (null != resolvedValue)
                            {
                                sb.Remove(matches[i].Index, matches[i].Length);
                                sb.Insert(matches[i].Index, resolvedValue);
                            }
                            else
                            {
                                throw new WixException(WixErrors.UnresolvedBindReference(sourceLineNumbers, value));
                            }
                        }
                    }
                }

                value = sb.ToString();
            }

            return value;
        }
    }
}
