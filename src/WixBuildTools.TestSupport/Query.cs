// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;
    using System.Collections.Generic;
    using System.Text;
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

                        using (var view = db.OpenView($"SELECT * FROM `{table}`"))
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
    }
}
