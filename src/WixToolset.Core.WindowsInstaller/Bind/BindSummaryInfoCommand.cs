// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Globalization;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

    /// <summary>
    /// Binds the summary information table of a database.
    /// </summary>
    internal class BindSummaryInfoCommand
    {
        public BindSummaryInfoCommand(IntermediateSection section)
        {
            this.Section = section;
        }

        private IntermediateSection Section { get; }

        /// <summary>
        /// Returns a flag indicating if files are compressed by default.
        /// </summary>
        public bool Compressed { get; private set; }

        /// <summary>
        /// Returns a flag indicating if uncompressed files use long filenames.
        /// </summary>
        public bool LongNames { get; private set; }

        public int InstallerVersion { get; private set; }

        /// <summary>
        /// Modularization guid, or null if the output is not a module.
        /// </summary>
        public string ModularizationGuid { get; private set; }

        public void Execute()
        {
            this.Compressed = false;
            this.LongNames = false;
            this.InstallerVersion = 0;
            this.ModularizationGuid = null;

            bool foundCreateDataTime = false;
            bool foundLastSaveDataTime = false;
            bool foundCreatingApplication = false;
            string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

            foreach (var summaryInformationRow in this.Section.Tuples.OfType<_SummaryInformationTuple>())
            {
                switch (summaryInformationRow.PropertyId)
                {
                    case 1: // PID_CODEPAGE
                            // make sure the code page is an int and not a web name or null
                        var codepage = summaryInformationRow.Value;

                        if (String.IsNullOrEmpty(codepage))
                        {
                            codepage = "0";
                        }
                        else
                        {
                            summaryInformationRow.Value = Common.GetValidCodePage(codepage, false, false, summaryInformationRow.SourceLineNumbers).ToString(CultureInfo.InvariantCulture);
                        }
                        break;
                    case 9: // PID_REVNUMBER
                        var packageCode = summaryInformationRow.Value;

                        if (SectionType.Module == this.Section.Type)
                        {
                            this.ModularizationGuid = packageCode.Substring(1, 36).Replace('-', '_');
                        }
                        else if ("*" == packageCode)
                        {
                            // set the revision number (package/patch code) if it should be automatically generated
                            summaryInformationRow.Value = Common.GenerateGuid();
                        }
                        break;
                    case 12: // PID_CREATE_DTM
                        foundCreateDataTime = true;
                        break;
                    case 13: // PID_LASTSAVE_DTM
                        foundLastSaveDataTime = true;
                        break;
                    case 14:
                        this.InstallerVersion = summaryInformationRow[_SummaryInformationTupleFields.Value].AsNumber();
                        break;
                    case 15: // PID_WORDCOUNT
                        if (SectionType.Patch == this.Section.Type)
                        {
                            this.LongNames = true;
                            this.Compressed = true;
                        }
                        else
                        {
                            var attributes = summaryInformationRow[_SummaryInformationTupleFields.Value].AsNumber();
                            this.LongNames = (0 == (attributes & 1));
                            this.Compressed = (2 == (attributes & 2));
                        }
                        break;
                    case 18: // PID_APPNAME
                        foundCreatingApplication = true;
                        break;
                }
            }

            // add a summary information row for the create time/date property if its not already set
            if (!foundCreateDataTime)
            {
                var createTimeDateRow = new _SummaryInformationTuple(null, new Identifier(12, AccessModifier.Private));
                createTimeDateRow.PropertyId = 12;
                createTimeDateRow.Value = now;

                this.Section.Tuples.Add(createTimeDateRow);
            }

            // add a summary information row for the last save time/date property if its not already set
            if (!foundLastSaveDataTime)
            {
                var lastSaveTimeDateRow = new _SummaryInformationTuple(null, new Identifier(13, AccessModifier.Private));
                lastSaveTimeDateRow.PropertyId = 13;
                lastSaveTimeDateRow.Value = now;

                this.Section.Tuples.Add(lastSaveTimeDateRow);
            }

            // add a summary information row for the creating application property if its not already set
            if (!foundCreatingApplication)
            {
                var creatingApplicationRow = new _SummaryInformationTuple(null, new Identifier(18, AccessModifier.Private));
                creatingApplicationRow.PropertyId = 18;
                creatingApplicationRow.Value = String.Format(CultureInfo.InvariantCulture, AppCommon.GetCreatingApplicationString());

                this.Section.Tuples.Add(creatingApplicationRow);
            }
        }
    }
}
