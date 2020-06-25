// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;

    internal class ModularizeCommand
    {
        public ModularizeCommand(WindowsInstallerData output, string modularizationSuffix, IEnumerable<WixSuppressModularizationSymbol> suppressSymbols)
        {
            this.Output = output;
            this.ModularizationSuffix = modularizationSuffix;

            // Gather all the unique suppress modularization identifiers.
            this.SuppressModularizationIdentifiers = new HashSet<string>(suppressSymbols.Select(s => s.Id.Id));
        }

        private WindowsInstallerData Output { get; }

        private string ModularizationSuffix { get; }

        private HashSet<string> SuppressModularizationIdentifiers { get; }

        public void Execute()
        {
            foreach (var table in this.Output.Tables)
            {
                this.ModularizeTable(table);
            }
        }

        private void ModularizeTable(Table table)
        {
            var modularizedColumns = new List<int>();

            // find the modularized columns
            for (var i = 0; i < table.Definition.Columns.Length; ++i)
            {
                if (ColumnModularizeType.None != table.Definition.Columns[i].ModularizeType)
                {
                    modularizedColumns.Add(i);
                }
            }

            if (0 < modularizedColumns.Count)
            {
                foreach (var row in table.Rows)
                {
                    foreach (var modularizedColumn in modularizedColumns)
                    {
                        var field = row.Fields[modularizedColumn];

                        if (field.Data != null)
                        {
                            field.Data = this.ModularizedRowFieldValue(row, field);
                        }
                    }
                }
            }
        }

        private string ModularizedRowFieldValue(Row row, Field field)
        {
            var fieldData = field.AsString();

            if (!(WindowsInstallerStandard.IsStandardAction(fieldData) || WindowsInstallerStandard.IsStandardProperty(fieldData)))
            {
                var modularizeType = field.Column.ModularizeType;

                // special logic for the ControlEvent table's Argument column
                // this column requires different modularization methods depending upon the value of the Event column
                if (ColumnModularizeType.ControlEventArgument == field.Column.ModularizeType)
                {
                    switch (row[2].ToString())
                    {
                        case "CheckExistingTargetPath": // redirectable property name
                        case "CheckTargetPath":
                        case "DoAction": // custom action name
                        case "NewDialog": // dialog name
                        case "SelectionBrowse":
                        case "SetTargetPath":
                        case "SpawnDialog":
                        case "SpawnWaitDialog":
                            if (Common.IsIdentifier(fieldData))
                            {
                                modularizeType = ColumnModularizeType.Column;
                            }
                            else
                            {
                                modularizeType = ColumnModularizeType.Property;
                            }
                            break;
                        default: // formatted
                            modularizeType = ColumnModularizeType.Property;
                            break;
                    }
                }
                else if (ColumnModularizeType.ControlText == field.Column.ModularizeType)
                {
                    // icons are stored in the Binary table, so they get column-type modularization
                    if (("Bitmap" == row[2].ToString() || "Icon" == row[2].ToString()) && Common.IsIdentifier(fieldData))
                    {
                        modularizeType = ColumnModularizeType.Column;
                    }
                    else
                    {
                        modularizeType = ColumnModularizeType.Property;
                    }
                }

                switch (modularizeType)
                {
                    case ColumnModularizeType.Column:
                        // ensure the value is an identifier (otherwise it shouldn't be modularized this way)
                        if (!Common.IsIdentifier(fieldData))
                        {
                            throw new InvalidOperationException(String.Format(CultureInfo.CurrentUICulture, WixDataStrings.EXP_CannotModularizeIllegalID, fieldData));
                        }

                        // if we're not supposed to suppress modularization of this identifier
                        if (!this.SuppressModularizationIdentifiers.Contains(fieldData))
                        {
                            fieldData = String.Concat(fieldData, this.ModularizationSuffix);
                        }
                        break;

                    case ColumnModularizeType.Property:
                    case ColumnModularizeType.Condition:
                        Regex regex;
                        if (ColumnModularizeType.Property == modularizeType)
                        {
                            regex = new Regex(@"\[(?<identifier>[#$!]?[a-zA-Z_][a-zA-Z0-9_\.]*)]", RegexOptions.Singleline | RegexOptions.ExplicitCapture);
                        }
                        else
                        {
                            Debug.Assert(ColumnModularizeType.Condition == modularizeType);

                            // This heinous looking regular expression is actually quite an elegant way
                            // to shred the entire condition into the identifiers that need to be
                            // modularized.  Let's break it down piece by piece:
                            //
                            // 1. Look for the operators: NOT, EQV, XOR, OR, AND, IMP (plus a space).  Note that the
                            //    regular expression is case insensitive so we don't have to worry about
                            //    all the permutations of these strings.
                            // 2. Look for quoted strings.  Quoted strings are just text and are ignored
                            //    outright.
                            // 3. Look for environment variables.  These look like identifiers we might
                            //    otherwise be interested in but start with a percent sign.  Like quoted
                            //    strings these enviroment variable references are ignored outright.
                            // 4. Match all identifiers that are things that need to be modularized.  Note
                            //    the special characters (!, $, ?, &) that denote Component and Feature states.
                            regex = new Regex(@"NOT\s|EQV\s|XOR\s|OR\s|AND\s|IMP\s|"".*?""|%[a-zA-Z_][a-zA-Z0-9_\.]*|(?<identifier>[!$\?&]?[a-zA-Z_][a-zA-Z0-9_\.]*)", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

                            // less performant version of the above with captures showing where everything lives
                            // regex = new Regex(@"(?<operator>NOT|EQV|XOR|OR|AND|IMP)|(?<string>"".*?"")|(?<environment>%[a-zA-Z_][a-zA-Z0-9_\.]*)|(?<identifier>[!$\?&]?[a-zA-Z_][a-zA-Z0-9_\.]*)",RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                        }

                        var matches = regex.Matches(fieldData);

                        var sb = new StringBuilder(fieldData);

                        // Notice how this code walks backward through the list
                        // because it modifies the string as we through it.
                        for (var i = matches.Count - 1; 0 <= i; i--)
                        {
                            var group = matches[i].Groups["identifier"];
                            if (group.Success)
                            {
                                var identifier = group.Value;
                                if (!WindowsInstallerStandard.IsStandardProperty(identifier) && !this.SuppressModularizationIdentifiers.Contains(identifier))
                                {
                                    sb.Insert(group.Index + group.Length, this.ModularizationSuffix);
                                }
                            }
                        }

                        fieldData = sb.ToString();
                        break;

                    case ColumnModularizeType.CompanionFile:
                        // if we're not supposed to ignore this identifier and the value does not start with
                        // a digit, we must have a companion file so modularize it
                        if (!this.SuppressModularizationIdentifiers.Contains(fieldData) &&
                            0 < fieldData.Length && !Char.IsDigit(fieldData, 0))
                        {
                            fieldData = String.Concat(fieldData, this.ModularizationSuffix);
                        }
                        break;

                    case ColumnModularizeType.Icon:
                        if (!this.SuppressModularizationIdentifiers.Contains(fieldData))
                        {
                            var start = fieldData.LastIndexOf(".", StringComparison.Ordinal);
                            if (-1 == start)
                            {
                                fieldData = String.Concat(fieldData, this.ModularizationSuffix);
                            }
                            else
                            {
                                fieldData = String.Concat(fieldData.Substring(0, start), this.ModularizationSuffix, fieldData.Substring(start));
                            }
                        }
                        break;

                    case ColumnModularizeType.SemicolonDelimited:
                        var keys = fieldData.Split(';');
                        for (var i = 0; i < keys.Length; ++i)
                        {
                            if (!String.IsNullOrEmpty(keys[i]))
                            {
                                keys[i] = String.Concat(keys[i], this.ModularizationSuffix);
                            }
                        }

                        fieldData = String.Join(";", keys);
                        break;
                }
            }

            return fieldData;
        }
    }
}
