// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using WixToolset.Dtf.WindowsInstaller;

    /// <summary>
    /// This task assigns Culture metadata to files based on the value of the Culture attribute on the
    /// WixLocalization element inside the file.
    /// </summary>
    public class GetLooseFileList : Task
    {
        internal const int MsidbFileAttributesNoncompressed = 8192;
        internal const int MsidbFileAttributesCompressed = 16384;

        /// <summary>
        /// The list of database files to find Loose Files in
        /// </summary>
        [Required]
        public ITaskItem Database { get; set; }

        /// <summary>
        /// The total list of Loose Files in this database
        /// </summary>
        [Output]
        public ITaskItem[] LooseFileList { get; private set; }

        /// <summary>
        /// Gets a complete list of external Loose Files referenced by the given installer database file.
        /// </summary>
        /// <returns>True upon completion of the task execution.</returns>
        public override bool Execute()
        {
            var databaseFile = this.Database.ItemSpec;
            var looseFileNames = new List<ITaskItem>();
            var ComponentFullDirectory = new Dictionary<string, string>();
            var DirectoryIdDefaultDir = new Dictionary<string, string>();
            var DirectoryIdParent = new Dictionary<string, string>();
            var DirectoryIdFullSource = new Dictionary<string, string>();

            // If the file doesn't exist, no Loose Files to return, so exit now
            if (!File.Exists(databaseFile))
            {
                return true;
            }

            var databaseDir = Path.GetDirectoryName(databaseFile);

            using (var database = new Database(databaseFile))
            {
                // If the File table doesn't exist, no Loose Files to return, so exit now
                if (null == database.Tables["File"])
                {
                    return true;
                }

                var compressed = 2 == (database.SummaryInfo.WordCount & 2);

                // Only setup all these helpful indexes if the database is marked as uncompressed. If it's marked as compressed, files are stored at the root,
                // so none of these indexes will be used
                if (!compressed)
                {
                    if (null == database.Tables["Directory"] || null == database.Tables["Component"])
                    {
                        return true;
                    }

                    var directoryRecords = database.ExecuteQuery("SELECT `Directory`,`Directory_Parent`,`DefaultDir` FROM `Directory`");

                    // First setup a simple index from DirectoryId to DefaultDir
                    for (var i = 0; i < directoryRecords.Count; i += 3)
                    {
                        var directoryId = (string)directoryRecords[i];
                        var directoryParent = (string)directoryRecords[i + 1];
                        var defaultDir = (string)directoryRecords[i + 2];

                        var sourceDir = this.SourceDirFromDefaultDir(defaultDir);

                        DirectoryIdDefaultDir[directoryId] = sourceDir;
                        DirectoryIdParent[directoryId] = directoryParent;
                    }

                    // Setup an index from directory Id to the full source path
                    for (var i = 0; i < directoryRecords.Count; i += 3)
                    {
                        var directoryId = (string)directoryRecords[i];
                        var directoryParent = (string)directoryRecords[i + 1];

                        var sourceDir = DirectoryIdDefaultDir[directoryId];

                        // The TARGETDIR case
                        if (String.IsNullOrEmpty(directoryParent))
                        {
                            DirectoryIdFullSource[directoryId] = databaseDir;
                        }
                        else
                        {
                            var tempDirectoryParent = directoryParent;

                            while (!String.IsNullOrEmpty(tempDirectoryParent) && !String.IsNullOrEmpty(DirectoryIdParent[tempDirectoryParent]))
                            {
                                sourceDir = Path.Combine(DirectoryIdDefaultDir[tempDirectoryParent], sourceDir);

                                tempDirectoryParent = DirectoryIdParent[tempDirectoryParent];
                            }

                            DirectoryIdFullSource[directoryId] = Path.Combine(databaseDir, sourceDir);
                        }
                    }

                    // Setup an index from component Id to full directory path
                    var componentRecords = database.ExecuteQuery("SELECT `Component`,`Directory_` FROM `Component`");

                    for (var i = 0; i < componentRecords.Count; i += 2)
                    {
                        var componentId = (string)componentRecords[i];
                        var componentDir = (string)componentRecords[i + 1];

                        ComponentFullDirectory[componentId] = DirectoryIdFullSource[componentDir];
                    }
                }

                var fileRecords = database.ExecuteQuery("SELECT `Component_`,`FileName`,`Attributes` FROM `File`");

                for (var i = 0; i < fileRecords.Count; i += 3)
                {
                    var componentId = (string)fileRecords[i];
                    var fileName = this.SourceFileFromFileName((string)fileRecords[i + 1]);
                    var attributes = (int)fileRecords[i + 2];

                    // If the whole database is marked uncompressed, use the directory layout made above
                    if (!compressed && MsidbFileAttributesCompressed != (attributes & MsidbFileAttributesCompressed))
                    {
                        looseFileNames.Add(new TaskItem(Path.GetFullPath(Path.Combine(ComponentFullDirectory[componentId], fileName))));
                    }
                    // If the database is marked as compressed, put files at the root
                    else if (compressed && (MsidbFileAttributesNoncompressed == (attributes & MsidbFileAttributesNoncompressed)))
                    {
                        looseFileNames.Add(new TaskItem(Path.GetFullPath(Path.Combine(databaseDir, fileName))));
                    }
                }
            }

            this.LooseFileList = looseFileNames.ToArray();

            return true;
        }

        /// <summary>
        /// Takes the "defaultDir" column
        /// </summary>
        /// <returns>Returns the corresponding sourceDir.</returns>
        public string SourceDirFromDefaultDir(string defaultDir)
        {
            var split = defaultDir.Split(':');

            var sourceDir = (1 == split.Length) ? split[0] : split[1];

            split = sourceDir.Split('|');

            sourceDir = (1 == split.Length) ? split[0] : split[1];

            return sourceDir;
        }

        /// <summary>
        /// Takes the "FileName" column
        /// </summary>
        /// <returns>Returns the corresponding source file name.</returns>
        private string SourceFileFromFileName(string fileName)
        {
            var split = fileName.Split('|');

            return (1 == split.Length) ? split[0] : split[1];
        }
    }
}
