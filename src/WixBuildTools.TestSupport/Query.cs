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

            if (tables?.Length > 0)
            {
                var sb = new StringBuilder();
                using (var db = new Database(path))
                {
                    foreach (var table in tables)
                    {
                        if (!db.IsTablePersistent(table))
                        {
                            continue;
                        }

                        using (var view = db.OpenView("SELECT * FROM `{0}`", table))
                        {
                            view.Execute();

                            Record record;
                            while ((record = view.Fetch()) != null)
                            {
                                sb.Clear();
                                sb.AppendFormat("{0}:", table);

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

                                results.Add(sb.ToString());
                            }
                        }
                    }
                }
            }

            results.Sort();
            return results.ToArray();
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
