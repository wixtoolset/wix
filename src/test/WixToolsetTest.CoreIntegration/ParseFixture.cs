// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Linq;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class ParseFixture
    {
        [Fact]
        public void GeneratesCorrectCustomActionIdentifiers()
        {
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var section = new IntermediateSection("section", SectionType.Fragment, 0);
            var parseHelper = serviceProvider.GetService<IParseHelper>();

            parseHelper.CreateCustomActionReference(null, section, "CustomAction32", Platform.X86, CustomActionPlatforms.X86);
            parseHelper.CreateCustomActionReference(null, section, "CustomArmAction", Platform.ARM64, CustomActionPlatforms.X86);
            parseHelper.CreateCustomActionReference(null, section, "CustomArmAction", Platform.ARM64, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            parseHelper.CreateCustomActionReference(null, section, "CustomAction", Platform.X64, CustomActionPlatforms.X86);
            parseHelper.CreateCustomActionReference(null, section, "CustomAction", Platform.X64, CustomActionPlatforms.X86 | CustomActionPlatforms.X64);

            var simpleReferences = section.Symbols.OfType<WixSimpleReferenceSymbol>();
            Assert.NotNull(simpleReferences.Where(t => t.SymbolicName == "CustomAction:Wix4CustomAction32_X86").FirstOrDefault());
            Assert.NotNull(simpleReferences.Where(t => t.SymbolicName == "CustomAction:Wix4CustomArmAction_X86").FirstOrDefault());
            Assert.NotNull(simpleReferences.Where(t => t.SymbolicName == "CustomAction:Wix4CustomArmAction_A64").FirstOrDefault());
            Assert.NotNull(simpleReferences.Where(t => t.SymbolicName == "CustomAction:Wix4CustomAction_X86").FirstOrDefault());
            Assert.NotNull(simpleReferences.Where(t => t.SymbolicName == "CustomAction:Wix4CustomAction_X64").FirstOrDefault());
        }
    }
}
