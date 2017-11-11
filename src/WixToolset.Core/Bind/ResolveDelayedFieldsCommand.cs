// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using WixToolset.Data;
    using WixToolset.Extensibility;

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
        public ResolveDelayedFieldsCommand(IEnumerable<IDelayedField> delayedFields, Dictionary<string, string> variableCache)
        {
            this.DelayedFields = delayedFields;
            this.VariableCache = variableCache;
        }

        private IEnumerable<IDelayedField> DelayedFields { get;}

        private IDictionary<string, string> VariableCache { get; }

        public void Execute()
        {
            var deferredFields = new List<IDelayedField>();

            foreach (var delayedField in this.DelayedFields)
            {
                try
                {
                    var propertyRow = delayedField.Row;

                    // process properties first in case they refer to other binder variables
                    if (delayedField.Row.Definition.Type == TupleDefinitionType.Property)
                    {
                        var value = WixVariableResolver.ResolveDelayedVariables(propertyRow.SourceLineNumbers, delayedField.Field.AsString(), this.VariableCache);

                        // update the variable cache with the new value
                        var key = String.Concat("property.", propertyRow.AsString(0));
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
                    Messaging.Instance.OnMessage(we.Error);
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
                    var value = WixVariableResolver.ResolveDelayedVariables(delayedField.Row.SourceLineNumbers, delayedField.Field.AsString(), this.VariableCache);
                    delayedField.Field.Set(value);
                }
                catch (WixException we)
                {
                    Messaging.Instance.OnMessage(we.Error);
                }
            }
        }
    }
}
