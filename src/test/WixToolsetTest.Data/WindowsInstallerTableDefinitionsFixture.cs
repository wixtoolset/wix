// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Data
{
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class WindowsInstallerTableDefinitionsFixture
    {
        [Fact]
        public void CanCreateWindowsInstallerRows()
        {
            foreach (var tableDefinition in WindowsInstallerTableDefinitions.All)
            {
                var table = new Table(tableDefinition);
                var rowFromTable = table.CreateRow(null);
                var rowFromTableDefinition = tableDefinition.CreateRow(null);
                var expectedRowTypeName = tableDefinition.Name.Replace("_", "") + "Row";
                var expectedRowType = rowFromTable.GetType();

                Assert.Equal(expectedRowType, rowFromTableDefinition.GetType());
                if (typeof(Row) != expectedRowType)
                {
                    Assert.Equal(expectedRowTypeName, expectedRowType.Name);
                }
            }
        }
    }
}
