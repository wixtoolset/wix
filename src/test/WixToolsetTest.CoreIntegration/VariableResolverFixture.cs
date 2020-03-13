
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class VariableResolverFixture
    {
        [Fact]
        public void CanRecursivelyResolveVariables()
        {
            var serviceProvider = new WixToolsetServiceProvider();
            var variableResolver = serviceProvider.GetService<IVariableResolver>();

            var variables = new Dictionary<string, BindVariable>()
            {
                { "ProductName", new BindVariable() { Id = "ProductName", Value = "Localized Product Name" } },
                { "ProductNameEdition", new BindVariable() { Id = "ProductNameEdition", Value = "!(loc.ProductName) Enterprise Edition" } },
                { "ProductNameEditionVersion", new BindVariable() { Id = "ProductNameEditionVersion", Value = "!(loc.ProductNameEdition) v1.2.3" } },
            };

            var localization = new Localization(0, "x-none", variables, new Dictionary<string,LocalizedControl>());

            variableResolver.AddLocalization(localization);

            Assert.Equal("Welcome to Localized Product Name", variableResolver.ResolveVariables(null, "Welcome to !(loc.ProductName)", false).Value);
            Assert.Equal("Welcome to Localized Product Name Enterprise Edition", variableResolver.ResolveVariables(null, "Welcome to !(loc.ProductNameEdition)", false).Value);
            Assert.Equal("Welcome to Localized Product Name Enterprise Edition v1.2.3", variableResolver.ResolveVariables(null, "Welcome to !(loc.ProductNameEditionVersion)", false).Value);
            Assert.Throws<WixException>(() => variableResolver.ResolveVariables(null, "Welcome to !(loc.UnknownLocalizationVariable)", false));
        }
    }
}
