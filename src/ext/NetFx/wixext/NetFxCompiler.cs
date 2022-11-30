// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Netfx.Symbols;

    /// <summary>
    /// The compiler for the WiX Toolset .NET Framework Extension.
    /// </summary>
    public sealed class NetfxCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/netfx";

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "File":
                    string fileId = context["FileId"];

                    switch (element.Name.LocalName)
                    {
                        case "NativeImage":
                            this.ParseNativeImageElement(intermediate, section, element, fileId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                    switch (element.Name.LocalName)
                    {
                        case "DotNetCoreSearch":
                            this.ParseDotNetCoreSearchElement(intermediate, section, element);
                            break;
                        case "DotNetCoreSearchRef":
                            this.ParseDotNetCoreSearchRefElement(intermediate, section, element);
                            break;
                        case "DotNetCoreSdkSearch":
                            this.ParseDotNetCoreSdkSearchElement(intermediate, section, element);
                            break;
                        case "DotNetCoreSdkSearchRef":
                            this.ParseDotNetCoreSdkSearchRefElement(intermediate, section, element);
                            break;
                        case "DotNetCompatibilityCheck":
                            this.ParseDotNetCompatibilityCheckElement(intermediate, section, element);
                            break;
                        case "DotNetCompatibilityCheckRef":
                            this.ParseDotNetCompatibilityCheckRefElement(intermediate, section, element);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Bundle":
                    switch (element.Name.LocalName)
                    {
                        case "DotNetCoreSearch":
                            this.ParseDotNetCoreSearchElement(intermediate, section, element);
                            break;
                        case "DotNetCoreSearchRef":
                            this.ParseDotNetCoreSearchRefElement(intermediate, section, element);
                            break;
                        case "DotNetCoreSdkSearch":
                            this.ParseDotNetCoreSdkSearchElement(intermediate, section, element);
                            break;
                        case "DotNetCoreSdkSearchRef":
                            this.ParseDotNetCoreSdkSearchRefElement(intermediate, section, element);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Package":
                case "Module":
                    switch (element.Name.LocalName)
                    {
                        case "DotNetCompatibilityCheck":
                            this.ParseDotNetCompatibilityCheckElement(intermediate, section, element);
                            break;
                        case "DotNetCompatibilityCheckRef":
                            this.ParseDotNetCompatibilityCheckRefElement(intermediate, section, element);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.ParseHelper.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        private void ParseDotNetCoreSearchElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            NetCoreSearchRuntimeType? runtimeType = null;
            NetCoreSearchPlatform? platform = null;
            var majorVersion = CompilerConstants.IntegerNotSet;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Variable":
                            variable = this.ParseHelper.GetAttributeBundleVariableNameValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "After":
                            after = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RuntimeType":
                            var runtimeTypeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (runtimeTypeValue)
                            {
                                case "aspnet":
                                    runtimeType = NetCoreSearchRuntimeType.Aspnet;
                                    break;
                                case "core":
                                    runtimeType = NetCoreSearchRuntimeType.Core;
                                    break;
                                case "desktop":
                                    runtimeType = NetCoreSearchRuntimeType.Desktop;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "RuntimeType", runtimeTypeValue, "aspnet", "core", "desktop"));
                                    break;
                            }
                            break;
                        case "Platform":
                            var platformValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (platformValue)
                            {
                                case "arm64":
                                    platform = NetCoreSearchPlatform.Arm64;
                                    break;
                                case "x64":
                                    platform = NetCoreSearchPlatform.X64;
                                    break;
                                case "x86":
                                    platform = NetCoreSearchPlatform.X86;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Platform", platformValue, "arm64", "x64", "x86"));
                                    break;
                            }
                            break;
                        case "MajorVersion":
                            // .NET Core had a different deployment strategy before .NET Core 3.0 which would require different detection logic.
                            majorVersion = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 3, Int32.MaxValue);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (id == null)
            {
                id = this.ParseHelper.CreateIdentifier("dncs", variable, condition, after);
            }

            if (!runtimeType.HasValue)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "RuntimeType"));
            }

            if (!platform.HasValue)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Platform"));
            }

            if (majorVersion == CompilerConstants.IntegerNotSet)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "MajorVersion"));
            }
            else if (majorVersion == 4)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "MajorVersion", "4", "3", "5+"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            var bundleExtensionId = this.ParseHelper.CreateIdentifierValueFromPlatform("Wix4NetfxBundleExtension", this.Context.Platform, BurnPlatforms.X86 | BurnPlatforms.X64 | BurnPlatforms.ARM64);
            if (bundleExtensionId == null)
            {
                this.Messaging.Write(ErrorMessages.UnsupportedPlatformForElement(sourceLineNumbers, this.Context.Platform.ToString(), element.Name.LocalName));
            }

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, bundleExtensionId);

                section.AddSymbol(new NetFxNetCoreSearchSymbol(sourceLineNumbers, id)
                {
                    RuntimeType = runtimeType.Value,
                    Platform = platform.Value,
                    MajorVersion = majorVersion,
                });
            }
        }

        private void ParseDotNetCoreSearchRefElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            var refId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, NetfxSymbolDefinitions.NetFxNetCoreSearch, refId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);
        }

        private void ParseDotNetCoreSdkSearchElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string variable = null;
            string condition = null;
            string after = null;
            NetCoreSdkSearchPlatform? platform = null;
            string version = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Variable":
                            variable = this.ParseHelper.GetAttributeBundleVariableNameValue(sourceLineNumbers, attrib);
                            break;
                        case "Condition":
                            condition = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "After":
                            after = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Platform":
                            var platformValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (platformValue)
                            {
                                case "arm64":
                                    platform = NetCoreSdkSearchPlatform.Arm64;
                                    break;
                                case "x64":
                                    platform = NetCoreSdkSearchPlatform.X64;
                                    break;
                                case "x86":
                                    platform = NetCoreSdkSearchPlatform.X86;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Platform", platformValue, "arm64", "x64", "x86"));
                                    break;
                            }
                            break;
                        case "Version":
                            // .NET Core had a different deployment strategy before .NET Core 3.0 which would require different detection logic.
                            version = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (id == null)
            {
                id = this.ParseHelper.CreateIdentifier("dncss", variable, condition, after);
            }

            if (!platform.HasValue)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Platform"));
            }

            
            if (String.IsNullOrEmpty(version))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Version"));
            }

            var ver = Version.Parse(version);
            if (ver.Major == 4)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Version", version, "3.*", "5+.*"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            var bundleExtensionId = this.ParseHelper.CreateIdentifierValueFromPlatform("Wix4NetfxBundleExtension", this.Context.Platform, BurnPlatforms.X86 | BurnPlatforms.X64 | BurnPlatforms.ARM64);
            if (bundleExtensionId == null)
            {
                this.Messaging.Write(ErrorMessages.UnsupportedPlatformForElement(sourceLineNumbers, this.Context.Platform.ToString(), element.Name.LocalName));
            }

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateWixSearchSymbol(section, sourceLineNumbers, element.Name.LocalName, id, variable, condition, after, bundleExtensionId);

                section.AddSymbol(new NetFxNetCoreSdkSearchSymbol(sourceLineNumbers, id)
                {
                    Platform = platform.Value,
                    Version = version,
                });
            }
        }

        private void ParseDotNetCoreSdkSearchRefElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            var refId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, NetfxSymbolDefinitions.NetFxNetCoreSdkSearch, refId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);
        }

        /// <summary>
        /// Parses a NativeImage element.
        /// </summary>
        /// <param name="element">The element to parse.</param>
        /// <param name="fileId">The file identifier of the parent element.</param>
        private void ParseNativeImageElement(Intermediate intermediate, IntermediateSection section, XElement element, string fileId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string appBaseDirectory = null;
            string assemblyApplication = null;
            int attributes = 0x8; // 32bit is on by default
            int priority = 3;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AppBaseDirectory":
                            appBaseDirectory = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);

                            // See if a formatted value is specified.
                            if (-1 == appBaseDirectory.IndexOf("[", StringComparison.Ordinal))
                            {
                                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Directory, appBaseDirectory);
                            }
                            break;
                        case "AssemblyApplication":
                            assemblyApplication = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);

                            // See if a formatted value is specified.
                            if (-1 == assemblyApplication.IndexOf("[", StringComparison.Ordinal))
                            {
                                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.File, assemblyApplication);
                            }
                            break;
                        case "Debug":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1;
                            }
                            break;
                        case "Dependencies":
                            if (YesNoType.No == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x2;
                            }
                            break;
                        case "Platform":
                            string platformValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < platformValue.Length)
                            {
                                switch (platformValue)
                                {
                                    case "32bit":
                                        // 0x8 is already on by default
                                        break;
                                    case "64bit":
                                        attributes &= ~0x8;
                                        attributes |= 0x10;
                                        break;
                                    case "all":
                                        attributes |= 0x10;
                                        break;
                                }
                            }
                            break;
                        case "Priority":
                            priority = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 3);
                            break;
                        case "Profile":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x4;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("nni", fileId);
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4NetFxScheduleNativeImage", this.Context.Platform, CustomActionPlatforms.ARM64 | CustomActionPlatforms.X64 | CustomActionPlatforms.X86);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new NetFxNativeImageSymbol(sourceLineNumbers, id)
                {
                    FileRef = fileId,
                    Priority = priority,
                    Attributes = attributes,
                    ApplicationFileRef = assemblyApplication,
                    ApplicationBaseDirectoryRef = appBaseDirectory,
                });
            }
        }

        /// <summary>
        /// Parses a DotNetCompatibilityCheck element.
        /// </summary>
        /// <param name="element">The element to parse.</param>
        private void ParseDotNetCompatibilityCheckElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string property = null;
            string runtimeType = null;
            string platform = null;
            string version = null;
            string rollForward = "Minor";

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Property":
                            property = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "RuntimeType":
                            runtimeType = this.ParseRuntimeType(element, sourceLineNumbers, attrib);
                            break;
                        case "Platform":
                            platform = this.ParsePlatform(element, sourceLineNumbers, attrib);
                            break;
                        case "FeatureBand":
                            platform = this.ParseFeatureBand(element, sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "RollForward":
                            rollForward = this.ParseRollForward(element, sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("ndncc", property, runtimeType, platform, version);
            }

            if (String.IsNullOrEmpty(property))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Property"));
            }

            if (String.IsNullOrEmpty(runtimeType))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "RuntimeType"));
            }

            if (String.IsNullOrEmpty(platform))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Platform"));
            }

            if (String.IsNullOrEmpty(version))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Version"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4NetFxDotNetCompatibilityCheck", this.Context.Platform, CustomActionPlatforms.ARM64 | CustomActionPlatforms.X64 | CustomActionPlatforms.X86);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new NetFxDotNetCompatibilityCheckSymbol(sourceLineNumbers, id)
                {
                    RuntimeType = runtimeType,
                    Platform = platform,
                    Version = version,
                    RollForward = rollForward,
                    Property = property,
                });
            }
        }

        private string ParseRollForward(XElement element, SourceLineNumber sourceLineNumbers, XAttribute attrib)
        {
            string rollForward;
            rollForward = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
            switch (rollForward.ToLowerInvariant())
            {
                case "latestmajor":
                    rollForward = "LatestMajor";
                    break;
                case "major":
                    rollForward = "Major";
                    break;
                case "latestminor":
                    rollForward = "LatestMinor";
                    break;
                case "minor":
                    rollForward = "Minor";
                    break;
                case "latestpatch":
                    rollForward = "LatestPatch";
                    break;
                case "disable":
                    rollForward = "Disable";
                    break;
                default:
                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName,
                        attrib.Name.LocalName, rollForward, "latestmajor", "major", "latestminor", "minor", "latestpatch", "disable"));
                    break;
            }

            return rollForward;
        }

        private string ParsePlatform(XElement element, SourceLineNumber sourceLineNumbers, XAttribute attrib)
        {
            string platform;
            platform = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
            switch (platform.ToLower())
            {
                case "x86":
                case "x64":
                case "arm64":
                    platform = platform.ToLower();
                    break;
                default:
                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName,
                        attrib.Name.LocalName, platform, "x86", "x64", "arm64"));
                    break;
            }

            return platform;
        }

        private string ParseFeatureBand(XElement element, SourceLineNumber sourceLineNumbers, XAttribute attrib)
        {
            string featureBand = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);

            if (!Int32.TryParse(featureBand, out var intFeatureBand) || (100 > intFeatureBand) || (intFeatureBand > 999))
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName,
                    attrib.Name.LocalName, featureBand, "An integer in the range [100 - 999]"));

            }

            return featureBand;
        }

        private string ParseRuntimeType(XElement element, SourceLineNumber sourceLineNumbers, XAttribute attrib)
        {
            var runtimeType = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
            switch (runtimeType.ToLower())
            {
                case "aspnet":
                    runtimeType = "Microsoft.AspNetCore.App";
                    break;
                case "desktop":
                    runtimeType = "Microsoft.WindowsDesktop.App";
                    break;
                case "core":
                    runtimeType = "Microsoft.NETCore.App";
                    break;
                default:
                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName,
                        attrib.Name.LocalName, runtimeType, "aspnet", "desktop", "core"));
                    break;
            }

            return runtimeType;
        }


        /// <summary>
        /// Parses a DotNetCompatibilityCheckRef element.
        /// </summary>
        /// <param name="element">The element to parse.</param>
        private void ParseDotNetCompatibilityCheckRefElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            var refId = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, NetfxSymbolDefinitions.NetFxDotNetCompatibilityCheck, refId);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);
        }
    }
}
