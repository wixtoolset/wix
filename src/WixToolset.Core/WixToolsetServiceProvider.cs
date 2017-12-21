// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using WixToolset.Core.ExtensibilityServices;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    public class WixToolsetServiceProvider : IServiceProvider
    {
        private ExtensionManager extensionManager;
        private Messaging messaging;
        private ParseHelper parseHelper;
        private PreprocessHelper preprocessHelper;
        private TupleDefinitionCreator tupleDefinitionCreator;
        private WindowsInstallerBackendHelper windowsInstallerBackendHelper;

        public object GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            // Transients.
            if (serviceType == typeof(IPreprocessContext))
            {
                return new PreprocessContext(this);
            }

            if (serviceType == typeof(ICompileContext))
            {
                return new CompileContext(this);
            }

            if (serviceType == typeof(ILinkContext))
            {
                return new LinkContext(this);
            }

            if (serviceType == typeof(IBindContext))
            {
                return new BindContext(this);
            }

            if (serviceType == typeof(ILayoutContext))
            {
                return new LayoutContext(this);
            }

            if (serviceType == typeof(IResolveContext))
            {
                return new ResolveContext(this);
            }

            if (serviceType == typeof(IInscribeContext))
            {
                return new InscribeContext(this);
            }

            if (serviceType == typeof(ICommandLineContext))
            {
                return new CommandLineContext(this);
            }

            if (serviceType == typeof(ICommandLine))
            {
                return new CommandLine();
            }

            // Singletons.
            if (serviceType == typeof(IExtensionManager))
            {
                return this.extensionManager = this.extensionManager ?? new ExtensionManager();
            }

            if (serviceType == typeof(IMessaging))
            {
                return this.messaging = this.messaging ?? new Messaging();
            }

            if (serviceType == typeof(ITupleDefinitionCreator))
            {
                return this.tupleDefinitionCreator = this.tupleDefinitionCreator ?? new TupleDefinitionCreator(this);
            }

            if (serviceType == typeof(IParseHelper))
            {
                return this.parseHelper = this.parseHelper ?? new ParseHelper(this);
            }

            if (serviceType == typeof(IPreprocessHelper))
            {
                return this.preprocessHelper = this.preprocessHelper ?? new PreprocessHelper(this);
            }

            if (serviceType == typeof(IWindowsInstallerBackendHelper))
            {
                return this.windowsInstallerBackendHelper = this.windowsInstallerBackendHelper ?? new WindowsInstallerBackendHelper(this);
            }

            throw new ArgumentException($"Unknown service type: {serviceType.Name}", nameof(serviceType));
        }
    }
}
