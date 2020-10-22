// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WixToolset.Dtf.Compression.Cab;
    using WixToolset.Dtf.WindowsInstaller;

    public class Query
    {
        public static string[] QueryDatabase(string path, string[] tables)
        {
            var results = new List<string>();
            var resultsByTable = QueryDatabaseByTable(path, tables);
            var sortedTables = tables.ToList();
            sortedTables.Sort();
            foreach (var tableName in sortedTables)
            {
                var rows = resultsByTable[tableName];
                rows?.ForEach(r => results.Add($"{tableName}:{r}"));
            }
            return results.ToArray();
        }

        /// <summary>
        /// Returns rows from requested tables formatted to facilitate testing.
        /// If the table did not exist in the database, its list will be null.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="tables"></param>
        /// <returns></returns>
        public static Dictionary<string, List<string>> QueryDatabaseByTable(string path, string[] tables)
        {
            var results = new Dictionary<string, List<string>>();

            if (tables?.Length > 0)
            {
                var sb = new StringBuilder();
                using (var db = new Database(path))
                {
                    foreach (var table in tables)
                    {
                        if (table == "_SummaryInformation")
                        {
                            var entries = new List<string>();
                            results.Add(table, entries);

                            entries.Add($"Title\t{db.SummaryInfo.Title}");
                            entries.Add($"Subject\t{db.SummaryInfo.Subject}");
                            entries.Add($"Author\t{db.SummaryInfo.Author}");
                            entries.Add($"Keywords\t{db.SummaryInfo.Keywords}");
                            entries.Add($"Comments\t{db.SummaryInfo.Comments}");
                            entries.Add($"Template\t{db.SummaryInfo.Template}");
                            entries.Add($"CodePage\t{db.SummaryInfo.CodePage}");
                            entries.Add($"PageCount\t{db.SummaryInfo.PageCount}");
                            entries.Add($"WordCount\t{db.SummaryInfo.WordCount}");
                            entries.Add($"CharacterCount\t{db.SummaryInfo.CharacterCount}");
                            entries.Add($"Security\t{db.SummaryInfo.Security}");

                            continue;
                        }

                        if (!db.IsTablePersistent(table))
                        {
                            results.Add(table, null);
                            continue;
                        }

                        var rows = new List<string>();
                        results.Add(table, rows);

                        using (var view = db.OpenView("SELECT * FROM `{0}`", table))
                        {
                            view.Execute();

                            Record record;
                            while ((record = view.Fetch()) != null)
                            {
                                sb.Clear();

                                using (record)
                                {
                                    for (var i = 0; i < record.FieldCount; ++i)
                                    {
                                        if (i > 0)
                                        {
                                            sb.Append("\t");
                                        }

                                        sb.Append(record[i + 1]?.ToString());
                                    }
                                }

                                rows.Add(sb.ToString());
                            }
                        }
                        rows.Sort();
                    }
                }
            }

            return results;
        }

        public static CabFileInfo[] GetCabinetFiles(string path)
        {
            var cab = new CabInfo(path);

            var result = cab.GetFiles();

            return result.Select(c => c).ToArray();
        }

        public static void ExtractStream(string path, string streamName, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (var db = new Database(path))
            using (var view = db.OpenView("SELECT `Data` FROM `_Streams` WHERE `Name` = '{0}'", streamName))
            {
                view.Execute();

                using (var record = view.Fetch())
                {
                    record.GetStream(1, outputPath);
                }
            }
        }

        public static void ExtractSubStorage(string path, string subStorageName, string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (var db = new Database(path))
            using (var view = db.OpenView("SELECT `Name`, `Data` FROM `_Storages` WHERE `Name` = '{0}'", subStorageName))
            {
                view.Execute();

                using (var record = view.Fetch())
                {
                    var name = record.GetString(1);
                    record.GetStream(2, outputPath);
                }
            }
        }

        public static string[] GetSubStorageNames(string path)
        {
            var result = new List<string>();

            using (var db = new Database(path))
            using (var view = db.OpenView("SELECT `Name` FROM `_Storages`"))
            {
                view.Execute();

                Record record;
                while ((record = view.Fetch()) != null)
                {
                    var name = record.GetString(1);
                    result.Add(name);
                }
            }

            result.Sort();
            return result.ToArray();
        }
    }
}
