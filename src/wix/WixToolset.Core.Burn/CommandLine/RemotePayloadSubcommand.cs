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
    using WixToolset.Core.Burn.Interfaces;
    using WixToolset.Core.Native;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class RemotePayloadSubcommand : BurnSubcommandBase
    {
        private static readonly XName ExePackagePayloadName = "ExePackagePayload";
        private static readonly XName MsuPackagePayloadName = "MsuPackagePayload";
        private static readonly XName PayloadName = "Payload";

        public RemotePayloadSubcommand(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.PayloadHarvester = serviceProvider.GetService<IPayloadHarvester>();
        }

        private IMessaging Messaging { get; }

        private IPayloadHarvester PayloadHarvester { get; }

        private List<string> BasePaths { get; } = new List<string>();

        private string DownloadUrl { get; set; }

        private List<string> InputPaths { get; } = new List<string>();

        private string OutputPath { get; set; }

        private WixBundlePackageType? PackageType { get; set; }

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

            var elements = this.HarvestRemotePayloads(inputPaths);

            if (!this.Messaging.EncounteredError)
            {
                if (!String.IsNullOrEmpty(this.OutputPath))
                {
                    var outputFolder = Path.GetDirectoryName(this.OutputPath);
                    Directory.CreateDirectory(outputFolder);

                    File.WriteAllLines(this.OutputPath, elements.Select(e => e.ToString()));
                }
                else
                {
                    foreach (var element in elements)
                    {
                        Console.WriteLine(element);
                    }
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

                    case "du":
                    case "downloadurl":
                        this.DownloadUrl = parser.GetNextArgumentOrError(argument);
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

        private IEnumerable<XElement> HarvestRemotePayloads(IEnumerable<string> paths)
        {
            var first = true;
            var hashes = new Dictionary<string, CertificateHashes>();

            if (this.UseCertificate)
            {
                hashes = CertificateHashes.Read(paths).Where(c => !String.IsNullOrEmpty(c.PublicKey) && !String.IsNullOrEmpty(c.Thumbprint) && c.Exception is null).ToDictionary(c => c.Path);
            }

            foreach (var path in paths)
            {
                var element = this.CreateRemotePayloadElement(path, first);
                first = false;

                if (element == null)
                {
                    continue;
                }

                var payloadSymbol = new WixBundlePayloadSymbol
                {
                    SourceFile = new IntermediateFieldPathValue { Path = path },
                };

                this.PayloadHarvester.HarvestStandardInformation(payloadSymbol);

                element.Add(new XAttribute("Name", Path.GetFileName(path)));

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
                    var filename = this.GetRelativeFileName(payloadSymbol.SourceFile.Path);
                    var formattedUrl = String.Format(this.DownloadUrl, filename);

                    if (Uri.TryCreate(formattedUrl, UriKind.Absolute, out var url))
                    {
                        element.Add(new XAttribute("DownloadUrl", url.AbsoluteUri));
                    }
                }

                if (hashes.TryGetValue(path, out var certificateHashes))
                {
                    element.Add(new XAttribute("CertificatePublicKey", certificateHashes.PublicKey));
                    element.Add(new XAttribute("CertificateThumbprint", certificateHashes.Thumbprint));
                }

                if (!String.IsNullOrEmpty(payloadSymbol.Hash))
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

                yield return element;
            }
        }

        private XElement CreateRemotePayloadElement(string path, bool firstHarvest)
        {
            if (firstHarvest)
            {
                var extension = this.PackageType.HasValue ? this.PackageType.ToString() : Path.GetExtension(path);

                switch (extension.ToUpperInvariant())
                {
                    case "EXE":
                    case ".EXE":
                        return new XElement(ExePackagePayloadName);

                    case "MSU":
                    case ".MSU":
                        return new XElement(MsuPackagePayloadName);

                    default:
                        this.Messaging.Write(BurnBackendErrors.UnsupportedRemotePackagePayload(extension, path));
                        return null;
                }
            }
            else
            {
                return new XElement(PayloadName);
            }
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
    }
}
