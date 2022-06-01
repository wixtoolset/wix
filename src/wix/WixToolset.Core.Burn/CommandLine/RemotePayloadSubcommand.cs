// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Core.Burn.Interfaces;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class RemotePayloadSubcommand : BurnSubcommandBase
    {
        private static readonly XName BundlePackageName = "BundlePackage";
        private static readonly XName ExePackageName = "ExePackage";
        private static readonly XName MsuPackageName = "MsuPackage";
        private static readonly XName BundlePackagePayloadName = "BundlePackagePayload";
        private static readonly XName ExePackagePayloadName = "ExePackagePayload";
        private static readonly XName MsuPackagePayloadName = "MsuPackagePayload";
        private static readonly XName PayloadName = "Payload";
        private static readonly XName RemoteBundleName = "RemoteBundle";
        private static readonly XName RemoteRelatedBundleName = "RemoteRelatedBundle";

        public RemotePayloadSubcommand(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.PayloadHarvester = serviceProvider.GetService<IPayloadHarvester>();
            var extensionManager = serviceProvider.GetService<IExtensionManager>();

            this.BackendExtensions = extensionManager.GetServices<IBurnBackendBinderExtension>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IPayloadHarvester PayloadHarvester { get; }

        private IReadOnlyCollection<IBurnBackendBinderExtension> BackendExtensions { get; }

        private List<string> BasePaths { get; } = new List<string>();

        private string DownloadUrl { get; set; }

        private List<string> InputPaths { get; } = new List<string>();

        private string IntermediateFolder { get; set; }

        private string OutputPath { get; set; }

        private WixBundlePackageType? PackageType { get; set; }

        private BundlePackagePayloadGenerationType BundlePayloadGeneration { get; set; } = BundlePackagePayloadGenerationType.ExternalWithoutDownloadUrl;

        private bool Recurse { get; set; }

        private bool UseCertificate { get; set; }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            var inputPaths = this.ExpandInputPaths();
            if (inputPaths.Count == 0)
            {
                Console.Error.WriteLine("Path to a remote payload is required");
                return Task.FromResult(-1);
            }

            // Reverse sort to ensure longest paths are matched first.
            this.BasePaths.Sort();
            this.BasePaths.Reverse();

            if (String.IsNullOrEmpty(this.IntermediateFolder))
            {
                this.IntermediateFolder = Path.GetTempPath();
            }

            var element = this.HarvestPackageElement(inputPaths);

            if (!this.Messaging.EncounteredError)
            {
                if (!String.IsNullOrEmpty(this.OutputPath))
                {
                    var outputFolder = Path.GetDirectoryName(this.OutputPath);
                    Directory.CreateDirectory(outputFolder);

                    File.WriteAllText(this.OutputPath, element.ToString());
                }
                else
                {
                    Console.WriteLine(element.ToString());
                }
            }

            return Task.FromResult(this.Messaging.LastErrorNumber);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (parser.IsSwitch(argument))
            {
                var parameter = argument.Substring(1);
                switch (parameter.ToLowerInvariant())
                {
                    case "bp":
                    case "basepath":
                        this.BasePaths.Add(parser.GetNextArgumentAsDirectoryOrError(argument));
                        return true;

                    case "bundlepayloadgeneration":
                        var bundlePayloadGenerationValue = parser.GetNextArgumentOrError(argument);
                        if (Enum.TryParse<BundlePackagePayloadGenerationType>(bundlePayloadGenerationValue, ignoreCase: true, out var bundlePayloadGeneration))
                        {
                            this.BundlePayloadGeneration = bundlePayloadGeneration;
                            return true;
                        }
                        break;

                    case "du":
                    case "downloadurl":
                        this.DownloadUrl = parser.GetNextArgumentOrError(argument);
                        return true;

                    case "intermediatefolder":
                        this.IntermediateFolder = parser.GetNextArgumentAsDirectoryOrError(argument);
                        return true;

                    case "packagetype":
                        var packageTypeValue = parser.GetNextArgumentOrError(argument);
                        if (Enum.TryParse<WixBundlePackageType>(packageTypeValue, ignoreCase: true, out var packageType))
                        {
                            this.PackageType = packageType;
                            return true;
                        }
                        break;

                    case "o":
                    case "out":
                        this.OutputPath = parser.GetNextArgumentAsFilePathOrError(argument);
                        return true;

                    case "r":
                    case "recurse":
                        this.Recurse = true;
                        return true;

                    case "usecertificate":
                        this.UseCertificate = true;
                        return true;
                }
            }
            else
            {
                this.InputPaths.Add(argument);
                return true;
            }

            return false;
        }

        private IReadOnlyCollection<string> ExpandInputPaths()
        {
            var result = new List<string>();

            foreach (var inputPath in this.InputPaths)
            {
                var filename = Path.GetFileName(inputPath);
                var folder = Path.GetDirectoryName(inputPath);

                if (String.IsNullOrEmpty(folder))
                {
                    folder = ".";
                }

                foreach (var path in Directory.EnumerateFiles(folder, filename, this.Recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                {
                    result.Add(path);
                }
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private XElement HarvestPackageElement(IEnumerable<string> paths)
        {
            var harvestedFiles = this.HarvestRemotePayloads(paths).ToList();

            XElement element;

            switch (harvestedFiles[0].PackageType)
            {
                case WixBundlePackageType.Bundle:
                    element = new XElement(BundlePackageName);
                    break;

                case WixBundlePackageType.Exe:
                    element = new XElement(ExePackageName);
                    break;

                case WixBundlePackageType.Msu:
                    element = new XElement(MsuPackageName);
                    break;

                default:
                    return null;
            }

            var packagePayloadFile = harvestedFiles.FirstOrDefault();

            if (packagePayloadFile != null)
            {
                if (packagePayloadFile.Element.Attribute("CertificateThumbprint") != null)
                {
                    var cacheId = CacheIdGenerator.GenerateCacheIdFromPayloadHashAndThumbprint(packagePayloadFile.PayloadSymbol);

                    element.Add(new XAttribute("CacheId", cacheId));
                }

                element.Add(harvestedFiles.Select(h => h.Element));
            }

            return element;
        }

        private IEnumerable<HarvestedFile> HarvestRemotePayloads(IEnumerable<string> paths)
        {
            var first = true;
            var hashes = this.GetCertificateHashes(paths);

            foreach (var path in paths)
            {
                var harvestedFile = this.HarvestFile(path, first, hashes);
                first = false;

                if (harvestedFile == null)
                {
                    continue;
                }

                yield return harvestedFile;

                if (harvestedFile.PackagePayloads.Any())
                {
                    var packageCertificateHashes = this.GetCertificateHashes(harvestedFile.PackagePayloads.Select(x => x.SourceFile.Path));

                    foreach (var payloadSymbol in harvestedFile.PackagePayloads)
                    {
                        var harvestedPackageFile = this.HarvestFile(payloadSymbol.SourceFile.Path, false, packageCertificateHashes);
                        yield return harvestedPackageFile;
                    }
                }
            }
        }

        private Dictionary<string, CertificateHashes> GetCertificateHashes(IEnumerable<string> paths)
        {
            var hashes = new Dictionary<string, CertificateHashes>();

            if (this.UseCertificate)
            {
                hashes = CertificateHashes.Read(paths)
                                          .Where(c => !String.IsNullOrEmpty(c.PublicKey) && !String.IsNullOrEmpty(c.Thumbprint) && c.Exception is null)
                                          .ToDictionary(c => c.Path);
            }

            return hashes;
        }

        private HarvestedFile HarvestFile(string path, bool isPackage, Dictionary<string, CertificateHashes> certificateHashes)
        {
            XElement element;
            WixBundlePackageType? packageType = null;

            if (isPackage)
            {
                var extension = this.PackageType.HasValue ? this.PackageType.ToString() : Path.GetExtension(path);

                switch (extension.ToUpperInvariant())
                {
                    case "BUNDLE":
                        packageType = WixBundlePackageType.Bundle;
                        element = new XElement(BundlePackagePayloadName);
                        break;

                    case "EXE":
                    case ".EXE":
                        packageType = WixBundlePackageType.Exe;
                        element = new XElement(ExePackagePayloadName);
                        break;

                    case "MSU":
                    case ".MSU":
                        packageType = WixBundlePackageType.Msu;
                        element = new XElement(MsuPackagePayloadName);
                        break;

                    default:
                        this.Messaging.Write(BurnBackendErrors.UnsupportedRemotePackagePayload(extension, path));
                        return null;
                }
            }
            else
            {
                element = new XElement(PayloadName);
            }

            var payloadSymbol = new WixBundlePayloadSymbol(null, new Identifier(AccessModifier.Section, "id"))
            {
                SourceFile = new IntermediateFieldPathValue { Path = path },
                Name = this.GetRelativeFileName(path),
            };

            this.PayloadHarvester.HarvestStandardInformation(payloadSymbol);

            element.Add(new XAttribute("Name", payloadSymbol.Name));

            if (!String.IsNullOrEmpty(payloadSymbol.DisplayName))
            {
                element.Add(new XAttribute("ProductName", payloadSymbol.DisplayName));
            }

            if (!String.IsNullOrEmpty(payloadSymbol.Description))
            {
                element.Add(new XAttribute("Description", payloadSymbol.Description));
            }

            if (!String.IsNullOrEmpty(this.DownloadUrl))
            {
                element.Add(new XAttribute("DownloadUrl", this.DownloadUrl));
            }

            if (certificateHashes.TryGetValue(path, out var certificateHashForPath))
            {
                payloadSymbol.CertificatePublicKey = certificateHashForPath.PublicKey;
                payloadSymbol.CertificateThumbprint = certificateHashForPath.Thumbprint;

                element.Add(new XAttribute("CertificatePublicKey", payloadSymbol.CertificatePublicKey));
                element.Add(new XAttribute("CertificateThumbprint", payloadSymbol.CertificateThumbprint));
            }
            else if (!String.IsNullOrEmpty(payloadSymbol.Hash))
            {
                element.Add(new XAttribute("Hash", payloadSymbol.Hash));
            }

            if (payloadSymbol.FileSize.HasValue)
            {
                element.Add(new XAttribute("Size", payloadSymbol.FileSize.Value));
            }

            if (!String.IsNullOrEmpty(payloadSymbol.Version))
            {
                element.Add(new XAttribute("Version", payloadSymbol.Version));
            }

            var harvestedFile = new HarvestedFile
            {
                Element = element,
                PackageType = packageType,
                PayloadSymbol = payloadSymbol,
            };

            switch (packageType)
            {
                case WixBundlePackageType.Bundle:
                    this.HarvestBundle(harvestedFile);
                    break;
            }

            return harvestedFile;
        }

        private string GetRelativeFileName(string path)
        {
            foreach (var basePath in this.BasePaths)
            {
                if (path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                {
                    return path.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
            }

            return Path.GetFileName(path); 
        }

        private void HarvestBundle(HarvestedFile harvestedFile)
        {
            var packagePayloadSymbol = new WixBundleBundlePackagePayloadSymbol(null, new Identifier(AccessModifier.Section, harvestedFile.PayloadSymbol.Id.Id))
            {
                PayloadGeneration = this.BundlePayloadGeneration,
            };

            var command = new HarvestBundlePackageCommand(this.ServiceProvider, this.BackendExtensions, this.IntermediateFolder, harvestedFile.PayloadSymbol, packagePayloadSymbol, new Dictionary<string, WixBundlePayloadSymbol>());
            command.Execute();

            if (!this.Messaging.EncounteredError)
            {
                var bundleElement = new XElement(RemoteBundleName);

                bundleElement.Add(new XAttribute("BundleId", command.HarvestedBundlePackage.BundleId));

                if (!String.IsNullOrEmpty(command.HarvestedBundlePackage.DisplayName))
                {
                    bundleElement.Add(new XAttribute("DisplayName", command.HarvestedBundlePackage.DisplayName));
                }

                if (!String.IsNullOrEmpty(command.HarvestedBundlePackage.EngineVersion))
                {
                    bundleElement.Add(new XAttribute("EngineVersion", command.HarvestedBundlePackage.EngineVersion));
                }

                bundleElement.Add(new XAttribute("InstallSize", command.HarvestedBundlePackage.InstallSize));
                bundleElement.Add(new XAttribute("ManifestNamespace", command.HarvestedBundlePackage.ManifestNamespace));
                bundleElement.Add(new XAttribute("PerMachine", command.HarvestedBundlePackage.PerMachine ? "yes" : "no"));
                bundleElement.Add(new XAttribute("ProviderKey", command.HarvestedDependencyProvider.ProviderKey));
                bundleElement.Add(new XAttribute("ProtocolVersion", command.HarvestedBundlePackage.ProtocolVersion));

                if (!String.IsNullOrEmpty(command.HarvestedBundlePackage.Version))
                {
                    bundleElement.Add(new XAttribute("Version", command.HarvestedBundlePackage.Version));
                }

                bundleElement.Add(new XAttribute("Win64", command.HarvestedBundlePackage.Win64 ? "yes" : "no"));

                var setUpgradeCode = false;
                foreach (var relatedBundle in command.RelatedBundles)
                {
                    if (!setUpgradeCode && relatedBundle.Action == RelatedBundleActionType.Upgrade)
                    {
                        setUpgradeCode = true;
                        bundleElement.Add(new XAttribute("UpgradeCode", relatedBundle.BundleId));
                        continue;
                    }

                    var relatedBundleElement = new XElement(RemoteRelatedBundleName);

                    relatedBundleElement.Add(new XAttribute("Id", relatedBundle.BundleId));
                    relatedBundleElement.Add(new XAttribute("Action", relatedBundle.Action.ToString()));

                    bundleElement.Add(relatedBundleElement);
                }

                harvestedFile.PackagePayloads.AddRange(command.Payloads);

                harvestedFile.Element.Add(bundleElement);
            }
        }

        private class HarvestedFile
        {
            public XElement Element { get; set; }

            public WixBundlePackageType? PackageType { get; internal set; }

            public WixBundlePayloadSymbol PayloadSymbol { get; set; }

            public List<WixBundlePayloadSymbol> PackagePayloads { get; } = new List<WixBundlePayloadSymbol>();
        }
    }
}
