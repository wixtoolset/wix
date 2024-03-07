// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller.Rows;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class BurnBackendHelper : IInternalBurnBackendHelper
    {
        public static readonly XmlReaderSettings ReaderSettings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment };
        public static readonly XmlWriterSettings WriterSettings = new XmlWriterSettings { ConformanceLevel = ConformanceLevel.Fragment };

        private readonly IBackendHelper backendHelper;
        private readonly IBundleValidator bundleValidator;

        private ManifestData BootstrapperApplicationManifestData { get; } = new ManifestData();

        private Dictionary<string, ManifestData> BootstrapperExtensionDataById { get; } = new Dictionary<string, ManifestData>();

        public BurnBackendHelper(IServiceProvider serviceProvider)
        {
            this.backendHelper = serviceProvider.GetService<IBackendHelper>();
            this.bundleValidator = serviceProvider.GetService<IBundleValidator>();
        }

        #region IBackendHelper interfaces

        public IFileTransfer CreateFileTransfer(string source, string destination, bool move, SourceLineNumber sourceLineNumbers = null)
        {
            return this.backendHelper.CreateFileTransfer(source, destination, move, sourceLineNumbers);
        }

        public string CreateGuid()
        {
            return this.backendHelper.CreateGuid();
        }

        public string CreateGuid(Guid namespaceGuid, string value)
        {
            return this.backendHelper.CreateGuid(namespaceGuid, value);
        }

        public IResolvedDirectory CreateResolvedDirectory(string directoryParent, string name)
        {
            return this.backendHelper.CreateResolvedDirectory(directoryParent, name);
        }

        public IReadOnlyList<ITrackedFile> ExtractEmbeddedFiles(IEnumerable<IExpectedExtractFile> embeddedFiles)
        {
            return this.backendHelper.ExtractEmbeddedFiles(embeddedFiles);
        }

        public string GenerateIdentifier(string prefix, params string[] args)
        {
            return this.backendHelper.GenerateIdentifier(prefix, args);
        }

        public int GetValidCodePage(string value, bool allowNoChange, bool onlyAnsi = false, SourceLineNumber sourceLineNumbers = null)
        {
            return this.backendHelper.GetValidCodePage(value, allowNoChange, onlyAnsi, sourceLineNumbers);
        }

        public string GetMsiFileName(string value, bool source, bool longName)
        {
            return this.backendHelper.GetMsiFileName(value, source, longName);
        }

        public bool IsValidBinderVariable(string variable)
        {
            return this.backendHelper.IsValidBinderVariable(variable);
        }

        public bool IsValidFourPartVersion(string version)
        {
            return this.backendHelper.IsValidFourPartVersion(version);
        }

        public bool IsValidIdentifier(string id)
        {
            return this.backendHelper.IsValidIdentifier(id);
        }

        public bool IsValidMsiProductVersion(string version)
        {
            return this.backendHelper.IsValidMsiProductVersion(version);
        }

        public bool IsValidWixVersion(string version)
        {
            return this.backendHelper.IsValidWixVersion(version);
        }

        public bool IsValidLongFilename(string filename, bool allowWildcards, bool allowRelative)
        {
            return this.backendHelper.IsValidLongFilename(filename, allowWildcards, allowRelative);
        }

        public bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            return this.backendHelper.IsValidShortFilename(filename, allowWildcards);
        }

        public void ResolveDelayedFields(IEnumerable<IDelayedField> delayedFields, Dictionary<string, string> variableCache)
        {
            this.backendHelper.ResolveDelayedFields(delayedFields, variableCache);
        }

        public string[] SplitMsiFileName(string value)
        {
            return this.backendHelper.SplitMsiFileName(value);
        }

        public bool TryParseFourPartVersion(string version, out string parsedVersion)
        {
            return this.backendHelper.TryParseFourPartVersion(version, out parsedVersion);
        }

        public bool TryParseMsiProductVersion(string version, bool strict, out string parsedVersion)
        {
            return this.backendHelper.TryParseMsiProductVersion(version, strict, out parsedVersion);
        }

        public ITrackedFile TrackFile(string path, TrackedFileType type, SourceLineNumber sourceLineNumbers = null)
        {
            return this.backendHelper.TrackFile(path, type, sourceLineNumbers);
        }

        #endregion

        #region IBurnBackendHelper interfaces

        public void AddBootstrapperApplicationData(string xml)
        {
            this.BootstrapperApplicationManifestData.AddXml(xml);
        }

        public void AddBootstrapperApplicationData(IntermediateSymbol symbol, bool symbolIdIsIdAttribute = false)
        {
            this.BootstrapperApplicationManifestData.AddSymbol(symbol, symbolIdIsIdAttribute, BurnConstants.BootstrapperApplicationDataNamespace);
        }

        public void AddBootstrapperExtensionData(string extensionId, string xml)
        {
            var manifestData = this.GetBootstrapperExtensionManifestData(extensionId);
            manifestData.AddXml(xml);
        }

        public void AddBootstrapperExtensionData(string extensionId, IntermediateSymbol symbol, bool symbolIdIsIdAttribute = false)
        {
            var manifestData = this.GetBootstrapperExtensionManifestData(extensionId);
            manifestData.AddSymbol(symbol, symbolIdIsIdAttribute, BurnConstants.BootstrapperExtensionDataNamespace);
        }

        #endregion

        #region IBundleValidator

        public string GetCanonicalRelativePath(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string relativePath)
        {
            return this.bundleValidator.GetCanonicalRelativePath(sourceLineNumbers, elementName, attributeName, relativePath);
        }

        public bool ValidateBundleMsiPropertyName(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string propertyName)
        {
            return this.bundleValidator.ValidateBundleMsiPropertyName(sourceLineNumbers, elementName, attributeName, propertyName);
        }

        public bool ValidateBundleVariableNameDeclaration(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string variableName)
        {
            return this.bundleValidator.ValidateBundleVariableNameDeclaration(sourceLineNumbers, elementName, attributeName, variableName);
        }

        public bool ValidateBundleVariableNameValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string variableName, BundleVariableNameRule nameRule)
        {
            return this.bundleValidator.ValidateBundleVariableNameValue(sourceLineNumbers, elementName, attributeName, variableName, nameRule);
        }

        public bool ValidateBundleVariableNameTarget(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string variableName)
        {
            return this.bundleValidator.ValidateBundleVariableNameTarget(sourceLineNumbers, elementName, attributeName, variableName);
        }

        public bool ValidateBundleCondition(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string condition, BundleConditionPhase phase)
        {
            return this.bundleValidator.ValidateBundleCondition(sourceLineNumbers, elementName, attributeName, condition, phase);
        }

        #endregion

        #region IInternalBurnBackendHelper interfaces

        public void WriteBootstrapperApplicationData(XmlWriter writer)
        {
            this.BootstrapperApplicationManifestData.Write(writer);
        }

        public void WriteBootstrapperExtensionData(XmlWriter writer)
        {
            foreach (var kvp in this.BootstrapperExtensionDataById)
            {
                this.WriteExtension(writer, kvp.Key, kvp.Value);
            }
        }

        #endregion

        private ManifestData GetBootstrapperExtensionManifestData(string extensionId)
        {
            if (!this.backendHelper.IsValidIdentifier(extensionId))
            {
                throw new ArgumentException($"'{extensionId}' is not a valid extensionId");
            }

            if (!this.BootstrapperExtensionDataById.TryGetValue(extensionId, out var manifestData))
            {
                manifestData = new ManifestData();
                this.BootstrapperExtensionDataById.Add(extensionId, manifestData);
            }

            return manifestData;
        }

        private void WriteExtension(XmlWriter writer, string extensionId, ManifestData manifestData)
        {
            writer.WriteStartElement("BootstrapperExtension");

            writer.WriteAttributeString("Id", extensionId);

            manifestData.Write(writer);

            writer.WriteEndElement();
        }

        private class ManifestData
        {
            public ManifestData()
            {
                this.Builder = new StringBuilder();
            }

            private StringBuilder Builder { get; }

            public void AddSymbol(IntermediateSymbol symbol, bool symbolIdIsIdAttribute, string ns)
            {
                // There might be a more efficient way to do this,
                // but this is an easy way to ensure we're creating valid XML.
                var sb = new StringBuilder();
                using (var writer = XmlWriter.Create(sb, WriterSettings))
                {
                    writer.WriteStartElement(symbol.Definition.Name, ns);

                    if (symbolIdIsIdAttribute && symbol.Id != null)
                    {
                        writer.WriteAttributeString("Id", symbol.Id.Id);
                    }

                    foreach (var field in symbol.Fields)
                    {
                        if (!field.IsNull())
                        {
                            writer.WriteAttributeString(field.Definition.Name, field.AsString());
                        }
                    }

                    writer.WriteEndElement();
                }

                this.AddXml(sb.ToString());
            }

            public void AddXml(string xml)
            {
                // There might be a more efficient way to do this,
                // but this is an easy way to ensure we're given valid XML.
                var sb = new StringBuilder();
                using (var xmlWriter = XmlWriter.Create(sb, WriterSettings))
                {
                    AddManifestDataFromString(xmlWriter, xml);
                }
                this.Builder.Append(sb.ToString());
            }

            public void Write(XmlWriter writer)
            {
                AddManifestDataFromString(writer, this.Builder.ToString());
            }

            private static void AddManifestDataFromString(XmlWriter xmlWriter, string xml)
            {
                using (var stringReader = new StringReader(xml))
                using (var xmlReader = XmlReader.Create(stringReader, ReaderSettings))
                {
                    while (xmlReader.MoveToContent() != XmlNodeType.None)
                    {
                        xmlWriter.WriteNode(xmlReader, false);
                    }
                }
            }
        }
    }
}
