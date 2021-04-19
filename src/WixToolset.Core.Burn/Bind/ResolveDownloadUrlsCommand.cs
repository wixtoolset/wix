// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bind
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class ResolveDownloadUrlsCommand
    {
        public ResolveDownloadUrlsCommand(IMessaging messaging, IEnumerable<IBurnBackendBinderExtension> backendExtensions, IEnumerable<WixBundleContainerSymbol> containers, Dictionary<string, WixBundlePayloadSymbol> payloadsById)
        {
            this.Messaging = messaging;
            this.BackendExtensions = backendExtensions;
            this.Containers = containers;
            this.PayloadsById = payloadsById;
        }

        private IMessaging Messaging { get; }

        private IEnumerable<IBurnBackendBinderExtension> BackendExtensions { get; }

        private IEnumerable<WixBundleContainerSymbol> Containers { get; }

        private Dictionary<string, WixBundlePayloadSymbol> PayloadsById { get; }

        public void Execute()
        {
            this.ResolveContainerUrls();

            this.ResolvePayloadUrls();
        }

        private void ResolveContainerUrls()
        {
            foreach (var container in this.Containers)
            {
                if (container.Type == ContainerType.Detached)
                {
                    var resolvedUrl = this.ResolveUrl(container.DownloadUrl, null, null, container.Id.Id, container.Name);
                    if (!String.IsNullOrEmpty(resolvedUrl))
                    {
                        container.DownloadUrl = resolvedUrl;
                    }
                }
                else if (container.Type == ContainerType.Attached)
                {
                    if (!String.IsNullOrEmpty(container.DownloadUrl))
                    {
                        this.Messaging.Write(WarningMessages.DownloadUrlNotSupportedForAttachedContainers(container.SourceLineNumbers, container.Id.Id));
                    }
                }
            }
        }

        private void ResolvePayloadUrls()
        {
            foreach (var payload in this.PayloadsById.Values)
            {
                if (payload.Packaging == PackagingType.External)
                {
                    var packageId = payload.ParentPackagePayloadRef;
                    var parentUrl = payload.ParentPackagePayloadRef == null ? null : this.PayloadsById[payload.ParentPackagePayloadRef].DownloadUrl;
                    var resolvedUrl = this.ResolveUrl(payload.DownloadUrl, parentUrl, packageId, payload.Id.Id, payload.Name);
                    if (!String.IsNullOrEmpty(resolvedUrl))
                    {
                        payload.DownloadUrl = resolvedUrl;
                    }
                }
                else if (payload.Packaging == PackagingType.Embedded)
                {
                    if (!String.IsNullOrEmpty(payload.DownloadUrl))
                    {
                        this.Messaging.Write(WarningMessages.DownloadUrlNotSupportedForEmbeddedPayloads(payload.SourceLineNumbers, payload.Id.Id));
                    }
                }
            }
        }

        private string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            string resolvedUrl = null;

            foreach (var extension in this.BackendExtensions)
            {
                resolvedUrl = extension.ResolveUrl(url, fallbackUrl, packageId, payloadId, fileName);
                if (!String.IsNullOrEmpty(resolvedUrl))
                {
                    break;
                }
            }

            if (String.IsNullOrEmpty(resolvedUrl))
            {
                // If a URL was not specified but there is a fallback URL that has a format specifier in it
                // then use the fallback URL formatter for this URL.
                if (String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(fallbackUrl))
                {
                    var formattedFallbackUrl = String.Format(fallbackUrl, packageId, payloadId, fileName);
                    if (!String.Equals(fallbackUrl, formattedFallbackUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        url = fallbackUrl;
                    }
                }

                if (!String.IsNullOrEmpty(url))
                {
                    var formattedUrl = String.Format(url, packageId, payloadId, fileName);

                    if (Uri.TryCreate(formattedUrl, UriKind.Absolute, out var canonicalUri))
                    {
                        resolvedUrl = canonicalUri.AbsoluteUri;
                    }
                    else
                    {
                        resolvedUrl = null;
                    }
                }
            }

            return resolvedUrl;
        }
    }
}
