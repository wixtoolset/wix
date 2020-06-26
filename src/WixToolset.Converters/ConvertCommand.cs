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

    internal class ConvertCommand : ICommandLineCommand
    {
        private const string SettingsFileDefault = "wixcop.settings.xml";

        public ConvertCommand(IWixToolsetServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();

            this.IndentationAmount = 4; // default indentation amount
            this.ErrorsAsWarnings = new HashSet<string>();
            this.ExemptFiles = new HashSet<string>();
            this.IgnoreErrors = new HashSet<string>();
            this.SearchPatternResults = new HashSet<string>();
            this.SearchPatterns = new List<string>();
        }

        private IMessaging Messaging { get; }

        public bool ShowLogo { get; private set; }

        public bool StopParsing { get; private set; }

        private bool ShowHelp { get; set; }

        private HashSet<string> ErrorsAsWarnings { get; }

        private HashSet<string> ExemptFiles { get; } 

        private bool FixErrors { get; set; }

        private int IndentationAmount { get; set; }

        private HashSet<string> IgnoreErrors { get; } 

        private HashSet<string> SearchPatternResults { get; } 

        private List<string> SearchPatterns { get; } 

        private string SettingsFile1 { get; set; }

        private string SettingsFile2 { get; set; }

        private bool SubDirectories { get; set; }

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
                    this.ShowHelp = true;
                    this.ShowLogo = true;
                    this.StopParsing = true;
                    return true;

                case "f":
                    this.FixErrors = true;
                    return true;

                case "nologo":
                    this.ShowLogo = false;
                    return true;

                case "s":
                    this.SubDirectories = true;
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
                            parser.ErrorArgument = parameter; //  $"Invalid numeric argument: {parameter}";
                        }
                        return true;
                    }

                    return false;
            }
        }

        public Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (this.ShowHelp)
            {
                DisplayHelp();
                return Task.FromResult(1);
            }

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

            var converter = new WixConverter(this.Messaging, this.IndentationAmount, this.ErrorsAsWarnings, this.IgnoreErrors);

            var errors = this.InspectSubDirectories(converter, Path.GetFullPath("."), cancellationToken);

            foreach (var searchPattern in this.SearchPatterns)
            {
                if (!this.SearchPatternResults.Contains(searchPattern))
                {
                    Console.Error.WriteLine("Could not find file \"{0}\"", searchPattern);
                    errors++;
                }
            }

            return Task.FromResult(errors != 0 ? 2 : 0);
        }

        private static void DisplayHelp()
        {
            Console.WriteLine(" usage:  wix.exe convert sourceFile [sourceFile ...]");
            Console.WriteLine();
            Console.WriteLine("   -f       fix errors automatically for writable files");
            Console.WriteLine("   -nologo  suppress displaying the logo information");
            Console.WriteLine("   -s       search for matching files in current dir and subdirs");
            Console.WriteLine("   -set1<file> primary settings file");
            Console.WriteLine("   -set2<file> secondary settings file (overrides primary)");
            Console.WriteLine("   -indent:<n> indentation multiple (overrides default of 4)");
            Console.WriteLine("   -?       this help information");
            Console.WriteLine();
            Console.WriteLine("   sourceFile may use wildcards like *.wxs");
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
        private int InspectSubDirectories(WixConverter converter, string directory, CancellationToken cancellationToken)
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
                        errors += converter.ConvertFile(file.FullName, this.FixErrors);
                    }
                }
            }

            if (this.SubDirectories)
            {
                foreach (var childDirectoryPath in Directory.GetDirectories(directory))
                {
                    errors += this.InspectSubDirectories(converter, childDirectoryPath, cancellationToken);
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
    }
}
