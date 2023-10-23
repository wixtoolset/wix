// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using System;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    public class ExampleExtensionFactory : IExtensionFactory
    {
        private ExamplePreprocessorExtensionAndCommandLine preprocessorExtension;

        public ExampleExtensionFactory(IWixToolsetCoreServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// This exists just to show it is possible to get a service provider to the extension factory.
        /// </summary>
        private IWixToolsetCoreServiceProvider ServiceProvider { get; }

        public bool TryCreateExtension(Type extensionType, out object extension)
        {
            if (extensionType == typeof(IExtensionCommandLine) || extensionType == typeof(IPreprocessorExtension))
            {
                if (this.preprocessorExtension == null)
                {
                    this.preprocessorExtension = new ExamplePreprocessorExtensionAndCommandLine();
                }

                extension = this.preprocessorExtension;
            }
            else if (extensionType == typeof(ICompilerExtension))
            {
                extension = new ExampleCompilerExtension();
            }
            else if (extensionType == typeof(IOptimizerExtension))
            {
                extension = new ExampleOptimizerExtension();
            }
            else if (extensionType == typeof(IExtensionData))
            {
                extension = new ExampleExtensionData();
            }
            else if (extensionType == typeof(IWindowsInstallerBackendBinderExtension))
            {
                extension = new ExampleWindowsInstallerBackendExtension();
            }
            else if (extensionType == typeof(IWindowsInstallerDecompilerExtension))
            {
                extension = new ExampleWindowsInstallerDecompilerExtension();
            }
            else
            {
                extension = null;
            }

            return extension != null;
        }
    }
}
