// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    public class GenerateMetadata : Task
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        };

        [Required]
        public string TargetFile { get; set; }

        [Required]
        public string WixpdbFile { get; set; }

        public override bool Execute()
        {
            var intermediate = Intermediate.Load(this.WixpdbFile);

            var section = intermediate.Sections.Single();

            Metadata metadata;
            SourceLineNumber sourceLineNumber;

            if (section.Type == SectionType.Bundle)
            {
                (metadata, sourceLineNumber) = this.GetBundleMetadata(section, "WixToolset.AdditionalTools");
            }
            else if (section.Type == SectionType.Package)
            {
                (metadata, sourceLineNumber) = this.GetPackageMetadata(section, "WixToolset.CommandLineTools");
            }
            else
            {
                return false;
            }

            if (metadata != null)
            {
                this.PopulateFileInfo(metadata);

                this.SaveMetadata(metadata, sourceLineNumber);
            }

            return true;
        }

        private (Metadata, SourceLineNumber) GetBundleMetadata(IntermediateSection section, string defaultId)
        {
            var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();

            var metadata = new Metadata
            {
                Id = bundleSymbol.Id?.Id ?? defaultId,
                Type = MetadataType.Burn,
                Name = bundleSymbol.Name,
                Version = bundleSymbol.Version,
                Publisher = bundleSymbol.Manufacturer,
                Description = "Installation for " + bundleSymbol.Name,
                License = "MS-RL",
                SupportUrl = bundleSymbol.HelpUrl,
                BundleCode = bundleSymbol.BundleId,
                UpgradeCode = bundleSymbol.UpgradeCode,
                AboutUrl = bundleSymbol.AboutUrl,
                Architecture = PlatformToArchitecture(bundleSymbol.Platform),
            };

            return (metadata, bundleSymbol.SourceLineNumbers);
        }

        private (Metadata, SourceLineNumber) GetPackageMetadata(IntermediateSection section, string defaultId)
        {
            var packageSymbol = section.Symbols.OfType<WixPackageSymbol>().Single();
            var propertySymbols = section.Symbols.OfType<PropertySymbol>().ToDictionary(p => p.Id.Id);
            var platform = GetPlatformFromSummaryInformation(section.Symbols.OfType<SummaryInformationSymbol>());

            var metadata = new Metadata
            {
                Id = packageSymbol.Id?.Id ?? defaultId,
                Type = MetadataType.Msi,
                Name = packageSymbol.Name,
                Version = packageSymbol.Version,
                Publisher = packageSymbol.Manufacturer,
                Description = "Installation for " + packageSymbol.Name,
                License = "MS-RL",
                SupportUrl = propertySymbols["ARPHELPLINK"].Value,
                ProductCode = propertySymbols["ProductCode"].Value,
                UpgradeCode = propertySymbols["UpgradeCode"].Value,
                AboutUrl = propertySymbols["ARPURLINFOABOUT"].Value,
                Architecture = PlatformToArchitecture(platform),
            };

            return (metadata, packageSymbol.SourceLineNumbers);
        }

        private void PopulateFileInfo(Metadata metadata)
        {
            var fi = new FileInfo(this.TargetFile);

            using (var sha256 = SHA256.Create())
            using (var stream = fi.OpenRead())
            {
                var hash = sha256.ComputeHash(stream);

                metadata.File = fi.Name;
                metadata.Created = fi.CreationTimeUtc.ToString("O");
                metadata.Size = fi.Length;
                metadata.Sha256 = BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        private void SaveMetadata(Metadata metadata, SourceLineNumber sourceLineNumber)
        {
            var metadataFilePath = Path.ChangeExtension(this.TargetFile, "metadata.json");

            var json = JsonSerializer.Serialize(metadata, SerializerOptions);

            File.WriteAllText(metadataFilePath, json);
        }

        private static Platform GetPlatformFromSummaryInformation(IEnumerable<SummaryInformationSymbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.PropertyId == SummaryInformationType.PlatformAndLanguage)
                {
                    var value = symbol.Value;
                    var separatorIndex = value.IndexOf(';');
                    var platformValue = separatorIndex > 0 ? value.Substring(0, separatorIndex) : value;

                    switch (platformValue)
                    {
                        case "x64":
                            return Platform.X64;

                        case "Arm64":
                            return Platform.ARM64;

                        case "Intel":
                        default:
                            return Platform.X86;
                    }
                }
            }

            return Platform.X86;
        }

        private static ArchitectureType PlatformToArchitecture(Platform platform)
        {
            switch (platform)
            {
                case Platform.X86: return ArchitectureType.X86;
                case Platform.X64: return ArchitectureType.X64;
                case Platform.ARM64: return ArchitectureType.Arm64;
                default: throw new ArgumentException($"Unknown platform {platform}");
            }
        }
    }
}
