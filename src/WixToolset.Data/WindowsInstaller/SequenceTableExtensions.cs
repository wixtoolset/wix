// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Enhancements to the SequenceTable enum.
    /// </summary>
    public static class SequenceTableExtensions
    {
        /// <summary>
        /// Gets the SequenceTable enum as the Windows Installer table name.
        /// </summary>
        public static string WindowsInstallerTableName(this SequenceTable sequence) => (sequence == SequenceTable.AdvertiseExecuteSequence) ? "AdvtExecuteSequence" : sequence.ToString();
    }
}
