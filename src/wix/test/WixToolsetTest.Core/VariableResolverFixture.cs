// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Core
{
    using System.Collections.Generic;
    using WixInternal.TestSupport;
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
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var variableResolver = serviceProvider.GetService<IVariableResolver>();

            var variables = new Dictionary<string, BindVariable>()
            {
                { "ProductName", new BindVariable() { Id = "ProductName", Value = "Localized Product Name" } },
                { "ProductNameEdition", new BindVariable() { Id = "ProductNameEdition", Value = "!(loc.ProductName) Enterprise Edition" } },
                { "ProductNameEditionVersion", new BindVariable() { Id = "ProductNameEditionVersion", Value = "!(loc.ProductNameEdition) v1.2.3" } },
                { "ProductNameEditionVersion.WithDot", new BindVariable() { Id = "ProductNameEditionVersion.WithDot", Value = "Test of localization variable with dot" } },
                { "ProductNameEditionVersion.WithDotTest", new BindVariable() { Id = "ProductNameEditionVersion.WithDotTest", Value = "!(loc.ProductNameEditionVersion.WithDot) v1.2.3" } },
            };

            var localization = new Localization(0, null, "x-none", variables, new Dictionary<string, LocalizedControl>());

            variableResolver.AddLocalization(localization);

            var result = variableResolver.ResolveVariables(null, "These are not the loc strings you're looking for.");
            WixAssert.StringEqual("These are not the loc strings you're looking for.", result.Value);
            Assert.False(result.UpdatedValue);

            result = variableResolver.ResolveVariables(null, "Welcome to !(loc.ProductName)");
            WixAssert.StringEqual("Welcome to Localized Product Name", result.Value);
            Assert.True(result.UpdatedValue);

            result = variableResolver.ResolveVariables(null, "Welcome to !(loc.ProductNameEdition)");
            WixAssert.StringEqual("Welcome to Localized Product Name Enterprise Edition", result.Value);
            Assert.True(result.UpdatedValue);

            result = variableResolver.ResolveVariables(null, "Welcome to !(loc.ProductNameEditionVersion)");
            WixAssert.StringEqual("Welcome to Localized Product Name Enterprise Edition v1.2.3", result.Value);
            Assert.True(result.UpdatedValue);

            result = variableResolver.ResolveVariables(null, "Welcome to !(loc.ProductNameEditionVersion.WithDotTest)");
            WixAssert.StringEqual("Welcome to Test of localization variable with dot v1.2.3", result.Value);
            Assert.True(result.UpdatedValue);

            result = variableResolver.ResolveVariables(null, "Welcome to !(bind.property.ProductVersion)");
            WixAssert.StringEqual("Welcome to !(bind.property.ProductVersion)", result.Value);
            Assert.False(result.UpdatedValue);
            Assert.True(result.DelayedResolve);

            var withUnknownLocString = "Welcome to !(loc.UnknownLocalizationVariable)";
            Assert.Throws<WixException>(() => variableResolver.ResolveVariables(null, withUnknownLocString));

            result = variableResolver.ResolveVariables(null, withUnknownLocString, errorOnUnknown: false);
            WixAssert.StringEqual(withUnknownLocString, result.Value);
            Assert.False(result.UpdatedValue);

            result = variableResolver.ResolveVariables(null, "Welcome to !!(loc.UnknownLocalizationVariable)");
            WixAssert.StringEqual("Welcome to !(loc.UnknownLocalizationVariable)", result.Value);
            Assert.True(result.UpdatedValue);

            result = variableResolver.ResolveVariables(null, "Welcome to !!(loc.UnknownLocalizationVariable) v!(bind.property.ProductVersion)");
            WixAssert.StringEqual("Welcome to !(loc.UnknownLocalizationVariable) v!(bind.property.ProductVersion)", result.Value);
            Assert.True(result.UpdatedValue);
            Assert.True(result.DelayedResolve);

            result = variableResolver.ResolveVariables(null, "Welcome to !(loc.ProductNameEditionVersion) !!(loc.UnknownLocalizationVariable) v!(bind.property.ProductVersion)");
            WixAssert.StringEqual("Welcome to Localized Product Name Enterprise Edition v1.2.3 !(loc.UnknownLocalizationVariable) v!(bind.property.ProductVersion)", result.Value);
            Assert.True(result.UpdatedValue);
            Assert.True(result.DelayedResolve);
        }
    }
}
