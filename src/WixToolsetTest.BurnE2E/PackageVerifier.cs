// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
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
        private string PackageName { get; }

        public string PackagePdb { get; }

        private WindowsInstallerData WiData { get; set; }

        public string GetInstalledFilePath(string filename)
        {
            return this.TestContext.GetTestInstallFolder(Path.Combine(this.PackageName, filename));
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
    }
}
