// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.IO;
    using System.Linq;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using Xunit;

    public partial class PackageInstaller
    {
        public string PackagePdb { get; }

        private bool IsX64 { get; }

        private WindowsInstallerData WiData { get; }

        public string GetInstalledFilePath(string filename)
        {
            return this.TestContext.GetTestInstallFolder(this.IsX64, Path.Combine(this.GetInstallFolderName(), filename));
        }

        public string GetInstallFolderName()
        {
            var row = this.WiData.Tables["Directory"].Rows.Single(r => r.FieldAsString(0) == "INSTALLFOLDER");
            var value = row.FieldAsString(2);
            var longNameIndex = value.IndexOf('|') + 1;
            if (longNameIndex > 0)
            {
                return value.Substring(longNameIndex);
            }
            return value;
        }

        public string GetProperty(string name)
        {
            var row = this.WiData.Tables["Property"].Rows.Cast<PropertyRow>().Single(r => r.Property == name);
            return row.Value;
        }

        public void VerifyInstalled(bool installed)
        {
            var productCode = this.GetProperty("ProductCode");
            Assert.Equal(installed, MsiUtilities.IsProductInstalled(productCode));
        }

        public void VerifyInstalledWithVersion(bool installed)
        {
            var productCode = this.GetProperty("ProductCode");
            Version prodVersion = new Version(this.GetProperty("ProductVersion"));
            Assert.Equal(installed, MsiUtilities.IsProductInstalledWithVersion(productCode, prodVersion));
        }

        public void DeleteTestRegistryValue(string name)
        {
            using (var root = this.TestContext.GetTestRegistryRoot(this.IsX64))
            {
                Assert.NotNull(root);
                root.DeleteValue(name);
            }
        }

        public void VerifyTestRegistryRootDeleted()
        {
            using var testRegistryRoot = this.TestContext.GetTestRegistryRoot(this.IsX64);
            Assert.Null(testRegistryRoot);
        }

        public void VerifyTestRegistryValue(string name, string expectedValue)
        {
            using (var root = this.TestContext.GetTestRegistryRoot(this.IsX64))
            {
                Assert.NotNull(root);
                var actualValue = root.GetValue(name) as string;
                Assert.Equal(expectedValue, actualValue);
            }
        }
    }
}
