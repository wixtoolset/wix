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
        private ParseHelper parseHelper;
        private TupleDefinitionCreator tupleDefinitionCreator;

        public object GetService(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            // Transients.
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

            if (serviceType == typeof(ITupleDefinitionCreator))
            {
                return this.tupleDefinitionCreator = this.tupleDefinitionCreator ?? new TupleDefinitionCreator(this);
            }

            if (serviceType == typeof(IParseHelper))
            {
                return this.parseHelper = this.parseHelper ?? new ParseHelper(this);
            }

            throw new ArgumentException($"Unknown service type: {serviceType.Name}", nameof(serviceType));
        }
    }
}
