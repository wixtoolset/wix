// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using Xunit;

    public partial class PackageInstaller
    {
        public string PackagePdb { get; }

        private WindowsInstallerData WiData { get; set; }

        public string GetInstalledFilePath(string filename)
        {
            return this.TestContext.GetTestInstallFolder(Path.Combine(this.GetInstallFolderName(), filename));
        }

        private WindowsInstallerData GetWindowsInstallerData()
        {
            if (this.WiData == null)
            {
                using var wixOutput = WixOutput.Read(this.PackagePdb);
                this.WiData = WindowsInstallerData.Load(wixOutput);
            }

            return this.WiData;
        }

        public string GetInstallFolderName()
        {
            var wiData = this.GetWindowsInstallerData();
            var row = wiData.Tables["Directory"].Rows.Single(r => r.FieldAsString(0) == "INSTALLFOLDER");
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
            var wiData = this.GetWindowsInstallerData();
            var row = wiData.Tables["Property"].Rows.Cast<PropertyRow>().Single(r => r.Property == name);
            return row.Value;
        }

        public void VerifyInstalled(bool installed)
        {
            var productCode = this.GetProperty("ProductCode");
            Assert.Equal(installed, MsiUtilities.IsProductInstalled(productCode));
        }

        public void DeleteTestRegistryValue(string name)
        {
            using (var root = this.TestContext.GetTestRegistryRoot())
            {
                Assert.NotNull(root);
                root.DeleteValue(name);
            }
        }

        public void VerifyTestRegistryRootDeleted()
        {
            using var testRegistryRoot = this.TestContext.GetTestRegistryRoot();
            Assert.Null(testRegistryRoot);
        }

        public void VerifyTestRegistryValue(string name, string expectedValue)
        {
            using (var root = this.TestContext.GetTestRegistryRoot())
            {
                Assert.NotNull(root);
                var actualValue = root.GetValue(name) as string;
                Assert.Equal(expectedValue, actualValue);
            }
        }
    }
}
