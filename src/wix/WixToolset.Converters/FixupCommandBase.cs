// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal abstract class FixupCommandBase : ICommandLineCommand
    {
        protected FixupCommandBase()
        {
            this.IndentationAmount = 4; // default indentation amount
            this.ErrorsAsWarnings = new HashSet<string>();
            this.ExemptFiles = new HashSet<string>();
            this.IgnoreErrors = new HashSet<string>();
            this.SearchPatternResults = new HashSet<string>();
            this.SearchPatterns = new List<string>();
        }

        public bool ShowLogo { get; private set; }

        public bool StopParsing { get; private set; }

        protected bool ShowHelp { get; set; }

        protected CustomTableTarget CustomTableSetting { get; set; }

        protected bool DryRun { get; set; }

        protected HashSet<string> ErrorsAsWarnings { get; }

        protected HashSet<string> IgnoreErrors { get; }

        protected HashSet<string> ExemptFiles { get; }

        protected int IndentationAmount { get; set; }

        protected bool Recurse { get; set; }

        private HashSet<string> SearchPatternResults { get; } 

        private List<string> SearchPatterns { get; } 

        private string SettingsFile1 { get; set; }

        private string SettingsFile2 { get; set; }

        public bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (!parser.IsSwitch(argument))
            {
                this.SearchPatterns.Add(argument);
                return true;
            }

            var parameter = argument.Substring(1);
            switch (parameter.ToLowerInvariant())
            {
                case "?":
                case "h":
                case "-help":
                    this.ShowHelp = true;
                    this.ShowLogo = true;
                    this.StopParsing = true;
                    return true;

                case "-custom-table":
                    var customTableSetting = parser.GetNextArgumentOrError(argument);
                    switch (customTableSetting)
                    {
                        case "bundle":
                            this.CustomTableSetting = CustomTableTarget.Bundle;
                            break;
                        case "msi":
                            this.CustomTableSetting = CustomTableTarget.Msi;
                            break;
                        default:
                            parser.ReportErrorArgument(argument);
                            break;
                    }
                    return true;

                case "n":
                case "-dry-run":
                    this.DryRun = true;
                    return true;

                case "nologo":
                case "-nologo":
                    this.ShowLogo = false;
                    return true;

                case "s":
                case "r":
                case "-recurse":
                case "-recursive":
                    this.Recurse = true;
                    return true;

                default: // other parameters
                    if (parameter.StartsWith("set1", StringComparison.Ordinal))
                    {
                        this.SettingsFile1 = parameter.Substring(4);
                        return true;
                    }
                    else if (parameter.StartsWith("set2", StringComparison.Ordinal))
                    {
                        this.SettingsFile2 = parameter.Substring(4);
                        return true;
                    }
                    else if (parameter.StartsWith("indent:", StringComparison.Ordinal))
                    {
                        try
                        {
                            this.IndentationAmount = Convert.ToInt32(parameter.Substring(7));
                        }
                        catch
                        {
                            parser.ReportErrorArgument(parameter); //  $"Invalid numeric argument: {parameter}";
                        }
                        return true;
                    }

                    return false;
            }
        }

        public abstract Task<int> ExecuteAsync(CancellationToken cancellationToken);

        protected void ParseSettings(string defaultSettingsFile)
        {
            // parse the settings if any were specified
            if (null != this.SettingsFile1 || null != this.SettingsFile2)
            {
                this.ParseSettingsFiles(this.SettingsFile1, this.SettingsFile2);
            }
            else
            {
                if (File.Exists(defaultSettingsFile))
                {
                    this.ParseSettingsFiles(defaultSettingsFile, null);
                }
            }
        }

        protected int Inspect(Func<string, bool, int> inspector, CancellationToken cancellationToken)
        {
            var errors = this.InspectSubDirectories(inspector, Path.GetFullPath("."), cancellationToken);

            foreach (var searchPattern in this.SearchPatterns)
            {
                if (!this.SearchPatternResults.Contains(searchPattern))
                {
                    Console.Error.WriteLine("Could not find file \"{0}\"", searchPattern);
                    errors++;
                }
            }

            return errors;
        }

        /// <summary>
        /// Inspect sub-directories.
        /// </summary>
        /// <param name="inspector"></param>
        /// <param name="directory">The directory whose sub-directories will be inspected.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The number of errors that were found.</returns>
        private int InspectSubDirectories(Func<string, bool, int> inspector, string directory, CancellationToken cancellationToken)
        {
            var errors = 0;

            foreach (var searchPattern in this.SearchPatterns)
            {
                foreach (var sourceFilePath in GetFiles(directory, searchPattern))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var file = new FileInfo(sourceFilePath);

                    if (!this.ExemptFiles.Contains(file.Name.ToUpperInvariant()))
                    {
                        this.SearchPatternResults.Add(searchPattern);
                        errors += inspector(file.FullName, !this.DryRun);
                    }
                }
            }

            if (this.Recurse)
            {
                foreach (var childDirectoryPath in Directory.GetDirectories(directory))
                {
                    errors += this.InspectSubDirectories(inspector, childDirectoryPath, cancellationToken);
                }
            }

            return errors;
        }

        /// <summary>
        /// Parse the primary and secondary settings files.
        /// </summary>
        /// <param name="localSettingsFile1">The primary settings file.</param>
        /// <param name="localSettingsFile2">The secondary settings file.</param>
        private void ParseSettingsFiles(string localSettingsFile1, string localSettingsFile2)
        {
            if (null == localSettingsFile1 && null != localSettingsFile2)
            {
                throw new ArgumentException("Cannot specify a secondary settings file (set2) without a primary settings file (set1).", nameof(localSettingsFile2));
            }

            var settingsFile = localSettingsFile1;
            while (null != settingsFile)
            {
                var doc = new XmlDocument();
                doc.Load(settingsFile);

                // get the types of tests that will have their errors displayed as warnings
                var testsIgnoredElements = doc.SelectNodes("/Settings/IgnoreErrors/Test");
                foreach (XmlElement test in testsIgnoredElements)
                {
                    var key = test.GetAttribute("Id");
                    this.IgnoreErrors.Add(key);
                }

                // get the types of tests that will have their errors displayed as warnings
                var testsAsWarningsElements = doc.SelectNodes("/Settings/ErrorsAsWarnings/Test");
                foreach (XmlElement test in testsAsWarningsElements)
                {
                    var key = test.GetAttribute("Id");
                    this.ErrorsAsWarnings.Add(key);
                }

                // get the exempt files
                var localExemptFiles = doc.SelectNodes("/Settings/ExemptFiles/File");
                foreach (XmlElement file in localExemptFiles)
                {
                    var key = file.GetAttribute("Name").ToUpperInvariant();
                    this.ExemptFiles.Add(key);
                }

                settingsFile = localSettingsFile2;
                localSettingsFile2 = null;
            }
        }

        /// <summary>
        /// Get the files that match a search path pattern.
        /// </summary>
        /// <param name="baseDir">The base directory at which to begin the search.</param>
        /// <param name="searchPath">The search path pattern.</param>
        /// <returns>The files matching the pattern.</returns>
        private static string[] GetFiles(string baseDir, string searchPath)
        {
            // convert alternate directory separators to the standard one
            var filePath = searchPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var lastSeparator = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            string[] files = null;

            try
            {
                if (0 > lastSeparator)
                {
                    files = Directory.GetFiles(baseDir, filePath);
                }
                else // found directory separator
                {
                    var searchPattern = filePath.Substring(lastSeparator + 1);

                    files = Directory.GetFiles(filePath.Substring(0, lastSeparator + 1), searchPattern);
                }
            }
            catch (DirectoryNotFoundException)
            {
                // don't let this function throw the DirectoryNotFoundException. (this exception
                // occurs for non-existant directories and invalid characters in the searchPattern)
            }

            return files;
        }
    }
}
