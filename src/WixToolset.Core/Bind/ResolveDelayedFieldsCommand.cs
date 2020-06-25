// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Resolves the fields which had variables that needed to be resolved after the file information
    /// was loaded.
    /// </summary>
    public class ResolveDelayedFieldsCommand
    {
        /// <summary>
        /// Resolve delayed fields.
        /// </summary>
        /// <param name="delayedFields">The fields which had resolution delayed.</param>
        /// <param name="variableCache">The file information to use when resolving variables.</param>
        public ResolveDelayedFieldsCommand(IMessaging messaging, IEnumerable<IDelayedField> delayedFields, Dictionary<string, string> variableCache)
        {
            this.Messaging = messaging;
            this.DelayedFields = delayedFields;
            this.VariableCache = variableCache;
        }

        private IMessaging Messaging { get; }

        private IEnumerable<IDelayedField> DelayedFields { get;}

        private IDictionary<string, string> VariableCache { get; }

        public void Execute()
        {
            var deferredFields = new List<IDelayedField>();

            foreach (var delayedField in this.DelayedFields)
            {
                try
                {
                    var propertySymbol = delayedField.Symbol;

                    // process properties first in case they refer to other binder variables
                    if (delayedField.Symbol.Definition.Type == SymbolDefinitionType.Property)
                    {
                        var value = ResolveDelayedVariables(propertySymbol.SourceLineNumbers, delayedField.Field.AsString(), this.VariableCache);

                        // update the variable cache with the new value
                        var key = String.Concat("property.", propertySymbol.Id.Id);
                        this.VariableCache[key] = value;

                        // update the field data
                        delayedField.Field.Set(value);
                    }
                    else
                    {
                        deferredFields.Add(delayedField);
                    }
                }
                catch (WixException we)
                {
                    this.Messaging.Write(we.Error);
                    continue;
                }
            }

            // add specialization for ProductVersion fields
            string keyProductVersion = "property.ProductVersion";
            if (this.VariableCache.TryGetValue(keyProductVersion, out var versionValue) && Version.TryParse(versionValue, out Version productVersion))
            {
                // Don't add the variable if it already exists (developer defined a property with the same name).
                string fieldKey = String.Concat(keyProductVersion, ".Major");
                if (!this.VariableCache.ContainsKey(fieldKey))
                {
                    this.VariableCache[fieldKey] = productVersion.Major.ToString(CultureInfo.InvariantCulture);
                }

                fieldKey = String.Concat(keyProductVersion, ".Minor");
                if (!this.VariableCache.ContainsKey(fieldKey))
                {
                    this.VariableCache[fieldKey] = productVersion.Minor.ToString(CultureInfo.InvariantCulture);
                }

                fieldKey = String.Concat(keyProductVersion, ".Build");
                if (!this.VariableCache.ContainsKey(fieldKey))
                {
                    this.VariableCache[fieldKey] = productVersion.Build.ToString(CultureInfo.InvariantCulture);
                }

                fieldKey = String.Concat(keyProductVersion, ".Revision");
                if (!this.VariableCache.ContainsKey(fieldKey))
                {
                    this.VariableCache[fieldKey] = productVersion.Revision.ToString(CultureInfo.InvariantCulture);
                }
            }

            // process the remaining fields in case they refer to property binder variables
            foreach (var delayedField in deferredFields)
            {
                try
                {
                    var value = ResolveDelayedVariables(delayedField.Symbol.SourceLineNumbers, delayedField.Field.AsString(), this.VariableCache);
                    delayedField.Field.Set(value);
                }
                catch (WixException we)
                {
                    this.Messaging.Write(we.Error);
                }
            }
        }

        private static string ResolveDelayedVariables(SourceLineNumber sourceLineNumbers, string value, IDictionary<string, string> resolutionData)
        {
            var matches = Common.WixVariableRegex.Matches(value);

            if (0 < matches.Count)
            {
                var sb = new StringBuilder(value);

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
                        var key = String.Format(CultureInfo.InvariantCulture, "{0}.{1}", variableId, variableScope).ToLower(CultureInfo.InvariantCulture);

                        if (!resolutionData.TryGetValue(key, out var resolvedValue))
                        {
                            resolvedValue = variableDefaultValue;
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
                                throw new WixException(ErrorMessages.UnresolvedBindReference(sourceLineNumbers, value));
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
