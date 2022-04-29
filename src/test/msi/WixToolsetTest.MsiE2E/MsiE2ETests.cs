// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using System.Collections.Generic;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    [Collection("MsiE2E")]
    public abstract class MsiE2ETests : WixTestBase, IDisposable
    {
        protected MsiE2ETests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        private Stack<IDisposable> Installers { get; } = new Stack<IDisposable>();

        protected PackageInstaller CreatePackageInstaller(string filename)
        {
            var installer = new PackageInstaller(this.TestContext, filename);
            this.Installers.Push(installer);
            return installer;
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

    [CollectionDefinition("MsiE2E", DisableParallelization = true)]
    public class MsiE2ECollectionDefinition : ICollectionFixture<MsiE2EFixture>
    {
    }
}
