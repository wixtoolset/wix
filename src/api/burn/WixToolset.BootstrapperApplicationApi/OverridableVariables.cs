// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.XPath;

    /// <summary>
    /// Default implementation of <see cref="IOverridableVariables"/>.
    /// </summary>
    public class OverridableVariablesInfo : IOverridableVariables
    {
        /// <inheritdoc />
        public VariableCommandLineType CommandLineType { get; internal set; }

        /// <inheritdoc />
        public IDictionary<string, IOverridableVariableInfo> Variables { get; internal set; }

        internal OverridableVariablesInfo() { }

        /// <summary>
        /// Parses the overridable variable info from the BA manifest.
        /// </summary>
        /// <param name="root">XML root</param>
        /// <returns>The parsed information.</returns>
        public static IOverridableVariables ParseFromXml(XPathNavigator root)
        {
            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(root.NameTable);
            namespaceManager.AddNamespace("p", BootstrapperApplicationData.XMLNamespace);
            XPathNavigator commandLineNode = root.SelectSingleNode("/p:BootstrapperApplicationData/p:WixStdbaCommandLine", namespaceManager);
            XPathNodeIterator nodes = root.Select("/p:BootstrapperApplicationData/p:WixStdbaOverridableVariable", namespaceManager);

            var overridableVariables = new OverridableVariablesInfo();
            IEqualityComparer<string> variableNameComparer;

            if (commandLineNode == null)
            {
                overridableVariables.CommandLineType = VariableCommandLineType.CaseSensitive;
                variableNameComparer = StringComparer.InvariantCulture;
            }
            else
            {
                string variablesValue = BootstrapperApplicationData.GetAttribute(commandLineNode, "VariableType");

                if (variablesValue == null)
                {
                    throw new Exception("Failed to get command line variable type.");
                }

                if (variablesValue.Equals("caseInsensitive", StringComparison.InvariantCulture))
                {
                    overridableVariables.CommandLineType = VariableCommandLineType.CaseInsensitive;
                    variableNameComparer = StringComparer.InvariantCultureIgnoreCase;
                }
                else if (variablesValue.Equals("caseSensitive", StringComparison.InvariantCulture))
                {
                    overridableVariables.CommandLineType = VariableCommandLineType.CaseSensitive;
                    variableNameComparer = StringComparer.InvariantCulture;
                }
                else
                {
                    throw new Exception(String.Format("Unknown command line variable type: '{0}'", variablesValue));
                }
            }

            overridableVariables.Variables = new Dictionary<string, IOverridableVariableInfo>(variableNameComparer);

            foreach (XPathNavigator node in nodes)
            {
                var variable = new OverridableVariableInfo();

                string name = BootstrapperApplicationData.GetAttribute(node, "Name");
                if (name == null)
                {
                    throw new Exception("Failed to get name for overridable variable.");
                }
                variable.Name = name;

                overridableVariables.Variables.Add(variable.Name, variable);
            }

            return overridableVariables;
        }
    }
}
