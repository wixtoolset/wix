// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class BootstrapperApplicationFixture
    {
        [Fact]
        public void CanSetBootstrapperApplicationDllDpiAwareness()
        {
            var folder = TestData.Get(@"TestData\BootstrapperApplication");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "DpiAwareness.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(wixlibPath);
                var allSymbols = intermediate.Sections.SelectMany(s => s.Symbols);
                var baDllSymbol = allSymbols.OfType<WixBootstrapperApplicationDllSymbol>()
                                              .SingleOrDefault();
                Assert.NotNull(baDllSymbol);

                Assert.Equal(WixBootstrapperApplicationDpiAwarenessType.GdiScaled, baDllSymbol.DpiAwareness);
            }
        }
    }
}
