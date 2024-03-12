// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using Xunit;
    using Xunit.Abstractions;

    public class UIExtensionTests : MsiE2ETests
    {
        public UIExtensionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public void CanBuildLocalizedWixUIPackageWithDefaultUSEnglish()
        {
            var product = this.CreatePackageInstaller("LocalizedWixUI");

            var nextButton = product.GetControlText("WelcomeDlg", "Next");
            var cancelButton = product.GetControlText("ExitDialog", "Cancel");
            var updateButton = product.GetControlText("VerifyReadyDlg", "Update");

            Assert.Equal("&Next", nextButton);
            Assert.Equal("Cancel", cancelButton);
            Assert.Equal("&Update", updateButton);
        }
    }
}
