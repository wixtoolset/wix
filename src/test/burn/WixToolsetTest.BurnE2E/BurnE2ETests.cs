// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.Collections.Generic;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    [Collection("BurnE2E")]
    public abstract class BurnE2ETests : WixTestBase, IDisposable
    {
        protected BurnE2ETests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            // https://github.com/microsoft/vstest/issues/3586
            Environment.SetEnvironmentVariable("DOTNET_ROOT", null);
        }

        private Stack<IDisposable> Installers { get; } = new Stack<IDisposable>();

        protected bool SupportAddonAndPatchRelatedBundles =>
#if SUPPORT_ADDON_AND_PATCH_RELATED_BUNDLES
            true;
#else
            false;
#endif

        protected void AddBundleInstaller(BundleInstaller installer)
        {
            this.Installers.Push(installer);
        }

        protected BundleInstaller CreateBundleInstaller(string name)
        {
            var installer = new BundleInstaller(this.TestContext, name);
            this.Installers.Push(installer);
            return installer;
        }

        protected PackageInstaller CreatePackageInstaller(string filename)
        {
            var installer = new PackageInstaller(this.TestContext, filename);
            this.Installers.Push(installer);
            return installer;
        }

        protected TestBAController CreateTestBAController()
        {
            var controller = new TestBAController(this.TestContext);
            this.Installers.Push(controller);
            return controller;
        }

        protected IWebServer CreateWebServer()
        {
            var webServer = new CoreOwinWebServer();
            this.Installers.Push(webServer);
            return webServer;
        }

        public void Dispose()
        {
            while (this.Installers.TryPop(out var installer))
            {
                try
                {
                    installer.Dispose();
                }
                catch { }
            }
        }
    }

    [CollectionDefinition("BurnE2E", DisableParallelization = true)]
    public class BurnE2ECollectionDefinition
    {
    }
}
