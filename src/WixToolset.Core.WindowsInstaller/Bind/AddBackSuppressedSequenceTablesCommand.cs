// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;

    /// <summary>
    /// Add back possibly suppressed sequence tables since all sequence tables must be present
    /// for the merge process to work. We'll drop the suppressed sequence tables again as
    /// necessary.
    /// </summary>
    internal class AddBackSuppressedSequenceTablesCommand
    {
        public AddBackSuppressedSequenceTablesCommand(WindowsInstallerData output, TableDefinitionCollection tableDefinitions)
        {
            this.Output = output;
            this.TableDefinitions = tableDefinitions;
        }

        private WindowsInstallerData Output { get; }

        private TableDefinitionCollection TableDefinitions { get; }

        public IEnumerable<string> SuppressedTableNames { get; private set; }

        public IEnumerable<string> Execute()
        {
            var suppressedTableNames = new HashSet<string>();

            foreach (SequenceTable sequence in Enum.GetValues(typeof(SequenceTable)))
            {
                var sequenceTableName = sequence.WindowsInstallerTableName();
                var sequenceTable = this.Output.Tables[sequenceTableName];

                if (null == sequenceTable)
                {
                    sequenceTable = this.Output.EnsureTable(this.TableDefinitions[sequenceTableName]);
                }

                if (0 == sequenceTable.Rows.Count)
                {
                    suppressedTableNames.Add(sequenceTableName);
                }
            }

            return this.SuppressedTableNames = suppressedTableNames;
        }
    }
}
