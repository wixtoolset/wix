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
            var fileRow = this.WiData.Tables["File"].Rows.Single(r => r.FieldAsString(2).Contains(filename));
            var componentRow = this.WiData.Tables["Component"].Rows.Single(r => r.FieldAsString(0) == fileRow.FieldAsString(1));
            var directoryId = componentRow.FieldAsString(2);
            var path = filename;

            while (directoryId != null)
            {
                string directoryName;

                if (directoryId == "ProgramFiles6432Folder")
                {
                    var baseDirectory = this.IsX64 ? Environment.SpecialFolder.ProgramFiles : Environment.SpecialFolder.ProgramFilesX86;
                    directoryName = Environment.GetFolderPath(baseDirectory);

                    directoryId = null;
                }
                else if (directoryId == "LocalAppDataFolder")
                {
                    directoryName = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

                    directoryId = null;
                }
                else
                {
                    var directoryRow = this.WiData.Tables["Directory"].Rows.Single(r => r.FieldAsString(0) == directoryId);
                    var value = directoryRow.FieldAsString(2);
                    var longNameIndex = value.IndexOf('|') + 1;
                    directoryName = longNameIndex > 0 ? value.Substring(longNameIndex) : value;

                    directoryId = directoryRow.FieldAsString(1);
                }

                path = Path.Combine(directoryName, path);
            }

            return path;
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
