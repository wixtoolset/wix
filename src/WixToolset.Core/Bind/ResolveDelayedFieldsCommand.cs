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
    public class ResolveDelayedFieldsCommand : ICommand
    {
        public OutputType OutputType { private get; set;}

        public IEnumerable<IDelayedField> DelayedFields { private get; set;}

        public IDictionary<string, string> VariableCache { private get; set; }

        public string ModularizationGuid { private get; set; }

        /// <param name="output">Internal representation of the msi database to operate upon.</param>
        /// <param name="delayedFields">The fields which had resolution delayed.</param>
        /// <param name="variableCache">The file information to use when resolving variables.</param>
        /// <param name="modularizationGuid">The modularization guid (used in case of a merge module).</param>
        public void Execute()
        {
            var deferredFields = new List<IDelayedField>();

            foreach (IDelayedField delayedField in this.DelayedFields)
            {
                try
                {
                    Row propertyRow = delayedField.Row;

                    // process properties first in case they refer to other binder variables
                    if ("Property" == propertyRow.Table.Name)
                    {
                        string value = WixVariableResolver.ResolveDelayedVariables(propertyRow.SourceLineNumbers, (string)delayedField.Field.Data, this.VariableCache);

                        // update the variable cache with the new value
                        string key = String.Concat("property.", Common.Demodularize(this.OutputType, this.ModularizationGuid, (string)propertyRow[0]));
                        this.VariableCache[key] = value;

                        // update the field data
                        delayedField.Field.Data = value;
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
            if (this.VariableCache.ContainsKey(keyProductVersion))
            {
                string value = this.VariableCache[keyProductVersion];
                Version productVersion = null;

                try
                {
                    productVersion = new Version(value);

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
                catch
                {
                    // Ignore the error introduced by new behavior.
                }
            }

            // process the remaining fields in case they refer to property binder variables
            foreach (DelayedField delayedField in deferredFields)
            {
                try
                {
                    delayedField.Field.Data = WixVariableResolver.ResolveDelayedVariables(delayedField.Row.SourceLineNumbers, (string)delayedField.Field.Data, this.VariableCache);
                }
                catch (WixException we)
                {
                    Messaging.Instance.OnMessage(we.Error);
                    continue;
                }
            }
        }
    }
}
