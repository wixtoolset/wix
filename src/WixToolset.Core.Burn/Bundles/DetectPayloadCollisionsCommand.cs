// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class DetectPayloadCollisionsCommand
    {
        public DetectPayloadCollisionsCommand(IMessaging messaging, Dictionary<string, WixBundleContainerSymbol> containerSymbols, IEnumerable<PackageFacade> packages, Dictionary<string, WixBundlePayloadSymbol> payloadSymbols, Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>> packagePayloads)
        {
            this.Messaging = messaging;
            this.Containers = containerSymbols;
            this.Packages = packages;
            this.PayloadSymbols = payloadSymbols;
            this.PackagePayloads = packagePayloads;
        }

        private IMessaging Messaging { get; }

        private Dictionary<string, WixBundleContainerSymbol> Containers { get; }

        private IEnumerable<PackageFacade> Packages { get; }

        private Dictionary<string, WixBundlePayloadSymbol> PayloadSymbols { get; }

        private Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>> PackagePayloads { get; }

        public void Execute()
        {
            this.DetectAttachedContainerCollisions();
            this.DetectExternalCollisions();
            this.DetectPackageCacheCollisions();
        }

        public void DetectAttachedContainerCollisions()
        {
            var attachedContainerPayloadsByNameByContainer = new Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>>();

            foreach (var payload in this.PayloadSymbols.Values.Where(p => p.Packaging == PackagingType.Embedded))
            {
                var containerId = payload.ContainerRef;
                var container = this.Containers[containerId];
                if (container.Type == ContainerType.Attached)
                {
                    if (!attachedContainerPayloadsByNameByContainer.TryGetValue(containerId, out var attachedContainerPayloadsByName))
                    {
                        attachedContainerPayloadsByName = new Dictionary<string, WixBundlePayloadSymbol>(StringComparer.OrdinalIgnoreCase);
                        attachedContainerPayloadsByNameByContainer.Add(containerId, attachedContainerPayloadsByName);
                    }

                    if (!attachedContainerPayloadsByName.TryGetValue(payload.Name, out var collisionPayload))
                    {
                        attachedContainerPayloadsByName.Add(payload.Name, payload);
                    }
                    else
                    {
                        if (containerId == BurnConstants.BurnUXContainerName)
                        {
                            this.Messaging.Write(BurnBackendErrors.BAContainerPayloadCollision(payload.SourceLineNumbers, payload.Id.Id, payload.Name));
                            this.Messaging.Write(BurnBackendErrors.BAContainerPayloadCollision2(collisionPayload.SourceLineNumbers));
                        }
                        else
                        {
                            this.Messaging.Write(BurnBackendWarnings.AttachedContainerPayloadCollision(payload.SourceLineNumbers, payload.Id.Id, payload.Name));
                            this.Messaging.Write(BurnBackendWarnings.AttachedContainerPayloadCollision2(collisionPayload.SourceLineNumbers));
                        }
                    }
                }
            }
        }

        public void DetectExternalCollisions()
        {
            var externalPayloadsByName = new Dictionary<string, IntermediateSymbol>(StringComparer.OrdinalIgnoreCase);

            foreach (var payload in this.PayloadSymbols.Values.Where(p => p.Packaging == PackagingType.External))
            {
                if (!externalPayloadsByName.TryGetValue(payload.Name, out var collisionSymbol))
                {
                    externalPayloadsByName.Add(payload.Name, payload);
                }
                else
                {
                    this.Messaging.Write(BurnBackendErrors.ExternalPayloadCollision(payload.SourceLineNumbers, "Payload", payload.Id.Id, payload.Name));
                    this.Messaging.Write(BurnBackendErrors.ExternalPayloadCollision2(collisionSymbol.SourceLineNumbers));
                }
            }

            foreach (var container in this.Containers.Values.Where(c => c.Type == ContainerType.Detached))
            {
                if (!externalPayloadsByName.TryGetValue(container.Name, out var collisionSymbol))
                {
                    externalPayloadsByName.Add(container.Name, container);
                }
                else
                {
                    this.Messaging.Write(BurnBackendErrors.ExternalPayloadCollision(container.SourceLineNumbers, "Container", container.Id.Id, container.Name));
                    this.Messaging.Write(BurnBackendErrors.ExternalPayloadCollision2(collisionSymbol.SourceLineNumbers));
                }
            }
        }

        public void DetectPackageCacheCollisions()
        {
            var packageCachePayloadsByNameByCacheId = new Dictionary<string, Dictionary<string, WixBundlePayloadSymbol>>();

            foreach (var packageFacade in this.Packages)
            {
                var packagePayloads = this.PackagePayloads[packageFacade.PackageId];
                if (!packageCachePayloadsByNameByCacheId.TryGetValue(packageFacade.PackageSymbol.CacheId, out var packageCachePayloadsByName))
                {
                    packageCachePayloadsByName = new Dictionary<string, WixBundlePayloadSymbol>(StringComparer.OrdinalIgnoreCase);
                    packageCachePayloadsByNameByCacheId.Add(packageFacade.PackageSymbol.CacheId, packageCachePayloadsByName);
                }

                foreach (var payload in packagePayloads.Values)
                {
                    if (!packageCachePayloadsByName.TryGetValue(payload.Name, out var collisionPayload))
                    {
                        packageCachePayloadsByName.Add(payload.Name, payload);
                    }
                    else
                    {
                        this.Messaging.Write(BurnBackendErrors.PackageCachePayloadCollision(payload.SourceLineNumbers, payload.Id.Id, payload.Name, packageFacade.PackageId));
                        this.Messaging.Write(BurnBackendErrors.PackageCachePayloadCollision2(collisionPayload.SourceLineNumbers));
                    }
                }
            }
        }
    }
}
