// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Core
{
    using System.Collections.Generic;
    using System.Linq;
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

            var variables = new BindVariable[]
            {
                new() { Id = "ProductName", Value = "Localized Product Name" },
                new() { Id = "ProductNameEdition", Value = "!(loc.ProductName) Enterprise Edition" },
                new() { Id = "ProductNameEditionVersion", Value = "!(loc.ProductNameEdition) v1.2.3" },
                new() { Id = "Dotted.Loc.Variable", Value = "Dotted.Loc.Variable = !(loc.ProductNameEditionVersion)" },
                new() { Id = "NestedDotted.Loc.Variable", Value = "!(loc.Dotted.Loc.Variable) worked" },
            }.ToDictionary(b => b.Id);

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

            result = variableResolver.ResolveVariables(null, "start !(loc.NestedDotted.Loc.Variable) end");
            WixAssert.StringEqual("start Dotted.Loc.Variable = Localized Product Name Enterprise Edition v1.2.3 worked end", result.Value);
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
