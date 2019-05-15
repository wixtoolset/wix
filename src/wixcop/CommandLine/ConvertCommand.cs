// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools.WixCop.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using WixToolset.Converters;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ConvertCommand : ICommandLineCommand
    {
        private const string SettingsFileDefault = "wixcop.settings.xml";

        public ConvertCommand(IServiceProvider serviceProvider, bool fixErrors, int indentationAmount, List<string> searchPatterns, bool subDirectories, string settingsFile1, string settingsFile2)
        {
            this.ErrorsAsWarnings = new HashSet<string>();
            this.ExemptFiles = new HashSet<string>();
            this.FixErrors = fixErrors;
            this.IndentationAmount = indentationAmount;
            this.IgnoreErrors = new HashSet<string>();
            this.SearchPatternResults = new HashSet<string>();
            this.SearchPatterns = searchPatterns;
            this.ServiceProvider = serviceProvider;
            this.SettingsFile1 = settingsFile1;
            this.SettingsFile2 = settingsFile2;
            this.SubDirectories = subDirectories;
        }

        private HashSet<string> ErrorsAsWarnings { get; }

        private HashSet<string> ExemptFiles { get; }

        private bool FixErrors { get; }

        private int IndentationAmount { get; }

        private HashSet<string> IgnoreErrors { get; }

        private HashSet<string> SearchPatternResults { get; }

        private List<string> SearchPatterns { get; }

        private IServiceProvider ServiceProvider { get; }

        private string SettingsFile1 { get; }

        private string SettingsFile2 { get; }

        private bool SubDirectories { get; }

        public bool ShowLogo => throw new NotImplementedException();

        public bool StopParsing => throw new NotImplementedException();

        public bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            throw new NotImplementedException();
        }

        public int Execute()
        {
            // parse the settings if any were specified
            if (null != this.SettingsFile1 || null != this.SettingsFile2)
            {
                this.ParseSettingsFiles(this.SettingsFile1, this.SettingsFile2);
            }
            else
            {
                if (File.Exists(ConvertCommand.SettingsFileDefault))
                {
                    this.ParseSettingsFiles(ConvertCommand.SettingsFileDefault, null);
                }
            }

            var messaging = this.ServiceProvider.GetService<IMessaging>();
            var converter = new Wix3Converter(messaging, this.IndentationAmount, this.ErrorsAsWarnings, this.IgnoreErrors);

            var errors = this.InspectSubDirectories(converter, Path.GetFullPath("."));

            foreach (var searchPattern in this.SearchPatterns)
            {
                if (!this.SearchPatternResults.Contains(searchPattern))
                {
                    Console.Error.WriteLine("Could not find file \"{0}\"", searchPattern);
                    errors++;
                }
            }

            return errors != 0 ? 2 : 0;
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

        /// <summary>
        /// Inspect sub-directories.
        /// </summary>
        /// <param name="directory">The directory whose sub-directories will be inspected.</param>
        /// <returns>The number of errors that were found.</returns>
        private int InspectSubDirectories(Wix3Converter converter, string directory)
        {
            var errors = 0;

            foreach (var searchPattern in this.SearchPatterns)
            {
                foreach (var sourceFilePath in GetFiles(directory, searchPattern))
                {
                    var file = new FileInfo(sourceFilePath);

                    if (!this.ExemptFiles.Contains(file.Name.ToUpperInvariant()))
                    {
                        this.SearchPatternResults.Add(searchPattern);
                        errors += converter.ConvertFile(file.FullName, this.FixErrors);
                    }
                }
            }

            if (this.SubDirectories)
            {
                foreach (var childDirectoryPath in Directory.GetDirectories(directory))
                {
                    errors += this.InspectSubDirectories(converter, childDirectoryPath);
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
                throw new ArgumentException("Cannot specify a secondary settings file (set2) without a primary settings file (set1).", "localSettingsFile2");
            }

            var settingsFile = localSettingsFile1;
            while (null != settingsFile)
            {
                XmlTextReader reader = null;
                try
                {
                    reader = new XmlTextReader(settingsFile);
                    var doc = new XmlDocument();
                    doc.Load(reader);

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
                }
                finally
                {
                    if (null != reader)
                    {
                        reader.Close();
                    }
                }

                settingsFile = localSettingsFile2;
                localSettingsFile2 = null;
            }
        }
    }
}
