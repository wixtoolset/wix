// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Databases
{
    using System;
    using System.Globalization;
    using WixToolset.Data;

    /// <summary>
    /// Binds the summary information table of a database.
    /// </summary>
    internal class BindSummaryInfoCommand
    {
        /// <summary>
        /// The output to bind.
        /// </summary>
        public Output Output { private get; set; }

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

            Table summaryInformationTable = this.Output.Tables["_SummaryInformation"];

            if (null != summaryInformationTable)
            {
                bool foundCreateDataTime = false;
                bool foundLastSaveDataTime = false;
                bool foundCreatingApplication = false;
                string now = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

                foreach (Row summaryInformationRow in summaryInformationTable.Rows)
                {
                    switch (summaryInformationRow.FieldAsInteger(0))
                    {
                        case 1: // PID_CODEPAGE
                            // make sure the code page is an int and not a web name or null
                            string codepage = summaryInformationRow.FieldAsString(1);

                            if (null == codepage)
                            {
                                codepage = "0";
                            }
                            else
                            {
                                summaryInformationRow[1] = Common.GetValidCodePage(codepage, false, false, summaryInformationRow.SourceLineNumbers).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case 9: // PID_REVNUMBER
                            string packageCode = (string)summaryInformationRow[1];

                            if (OutputType.Module == this.Output.Type)
                            {
                                this.ModularizationGuid = packageCode.Substring(1, 36).Replace('-', '_');
                            }
                            else if ("*" == packageCode)
                            {
                                // set the revision number (package/patch code) if it should be automatically generated
                                summaryInformationRow[1] = Common.GenerateGuid();
                            }
                            break;
                        case 12: // PID_CREATE_DTM
                            foundCreateDataTime = true;
                            break;
                        case 13: // PID_LASTSAVE_DTM
                            foundLastSaveDataTime = true;
                            break;
                        case 14:
                            this.InstallerVersion = summaryInformationRow.FieldAsInteger(1);
                            break;
                        case 15: // PID_WORDCOUNT
                            if (OutputType.Patch == this.Output.Type)
                            {
                                this.LongNames = true;
                                this.Compressed = true;
                            }
                            else
                            {
                                this.LongNames = (0 == (summaryInformationRow.FieldAsInteger(1) & 1));
                                this.Compressed = (2 == (summaryInformationRow.FieldAsInteger(1) & 2));
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
                    Row createTimeDateRow = summaryInformationTable.CreateRow(null);
                    createTimeDateRow[0] = 12;
                    createTimeDateRow[1] = now;
                }

                // add a summary information row for the last save time/date property if its not already set
                if (!foundLastSaveDataTime)
                {
                    Row lastSaveTimeDateRow = summaryInformationTable.CreateRow(null);
                    lastSaveTimeDateRow[0] = 13;
                    lastSaveTimeDateRow[1] = now;
                }

                // add a summary information row for the creating application property if its not already set
                if (!foundCreatingApplication)
                {
                    Row creatingApplicationRow = summaryInformationTable.CreateRow(null);
                    creatingApplicationRow[0] = 18;
                    creatingApplicationRow[1] = String.Format(CultureInfo.InvariantCulture, AppCommon.GetCreatingApplicationString());
                }
            }
        }
    }
}
