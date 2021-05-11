// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using WixBuildTools.TestSupport;
    using Xunit;

    public class TestTool : ExternalExecutable
    {
        /// <summary>
        /// Constructor for a TestTool
        /// </summary>
        public TestTool()
            : this(null)
        {
        }

        /// <summary>
        /// Constructor for a TestTool
        /// </summary>
        /// <param name="toolFile">The full path to the tool. Eg. c:\bin\candle.exe</param>
        public TestTool(string toolFile)
            : base(toolFile)
        {
            this.PrintOutputToConsole = true;
        }

        /// <summary>
        /// The arguments to pass to the tool
        /// </summary>
        public virtual string Arguments { get; set; }

        /// <summary>
        /// Stores the errors that occurred when a run was checked against its expected results
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// A list of Regex's that are expected to match stderr
        /// </summary>
        public List<Regex> ExpectedErrorRegexs { get; set; } = new List<Regex>();

        /// <summary>
        /// The expected error strings to stderr
        /// </summary>
        public List<string> ExpectedErrorStrings { get; set; } = new List<string>();

        /// <summary>
        /// The expected exit code of the tool
        /// </summary>
        public int? ExpectedExitCode { get; set; }

        /// <summary>
        /// A list of Regex's that are expected to match stdout
        /// </summary>
        public List<Regex> ExpectedOutputRegexs { get; set; } = new List<Regex>();

        /// <summary>
        /// The expected output strings to stdout
        /// </summary>
        public List<string> ExpectedOutputStrings { get; set; } = new List<string>();

        /// <summary>
        /// Print output from the tool execution to the console
        /// </summary>
        public bool PrintOutputToConsole { get; set; }

        /// <summary>
        /// The working directory of the tool
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Print the errors from the last run
        /// </summary>
        public void PrintErrors()
        {
            if (null != this.Errors)
            {
                Console.WriteLine("Errors:");

                foreach (string error in this.Errors)
                {
                    Console.WriteLine(error);
                }
            }
        }

        /// <summary>
        /// Run the tool
        /// </summary>
        /// <returns>The results of the run</returns>
        public ExternalExecutableResult Run()
        {
            return this.Run(true);
        }

        /// <summary>
        /// Run the tool
        /// </summary>
        /// <param name="exceptionOnError">Throw an exception if the expected results don't match the actual results</param>
        /// <exception cref="System.Exception">Thrown when the expected results don't match the actual results</exception>
        /// <returns>The results of the run</returns>
        public virtual ExternalExecutableResult Run(bool assertOnError)
        {
            var result = this.Run(this.Arguments, workingDirectory: this.WorkingDirectory ?? String.Empty);

            if (this.PrintOutputToConsole)
            {
                Console.WriteLine(FormatResult(result));
            }

            this.Errors = this.CheckResult(result);

            if (assertOnError && 0 < this.Errors.Count)
            {
                if (this.PrintOutputToConsole)
                {
                    this.PrintErrors();
                }

                Assert.Empty(this.Errors);
            }

            return result;
        }

        /// <summary>
        /// Checks that the result from a run matches the expected results
        /// </summary>
        /// <param name="result">A result from a run</param>
        /// <returns>A list of errors</returns>
        public virtual List<string> CheckResult(ExternalExecutableResult result)
        {
            List<string> errors = new List<string>();

            // Verify that the expected return code matched the actual return code
            if (null != this.ExpectedExitCode && this.ExpectedExitCode != result.ExitCode)
            {
                errors.Add(String.Format("Expected exit code {0} did not match actual exit code {1}", this.ExpectedExitCode, result.ExitCode));
            }

            var standardErrorString = string.Join(Environment.NewLine, result.StandardError);

            // Verify that the expected error string are in stderr
            if (null != this.ExpectedErrorStrings)
            {
                foreach (string expectedString in this.ExpectedErrorStrings)
                {
                    if (!standardErrorString.Contains(expectedString))
                    {
                        errors.Add(String.Format("The text '{0}' was not found in stderr", expectedString));
                    }
                }
            }

            var standardOutputString = string.Join(Environment.NewLine, result.StandardOutput);

            // Verify that the expected output string are in stdout
            if (null != this.ExpectedOutputStrings)
            {
                foreach (string expectedString in this.ExpectedOutputStrings)
                {
                    if (!standardOutputString.Contains(expectedString))
                    {
                        errors.Add(String.Format("The text '{0}' was not found in stdout", expectedString));
                    }
                }
            }

            // Verify that the expected regular expressions match stderr
            if (null != this.ExpectedOutputRegexs)
            {
                foreach (Regex expectedRegex in this.ExpectedOutputRegexs)
                {
                    if (!expectedRegex.IsMatch(standardOutputString))
                    {
                        errors.Add(String.Format("Regex {0} did not match stdout", expectedRegex.ToString()));
                    }
                }
            }

            // Verify that the expected regular expressions match stdout
            if (null != this.ExpectedErrorRegexs)
            {
                foreach (Regex expectedRegex in this.ExpectedErrorRegexs)
                {
                    if (!expectedRegex.IsMatch(standardErrorString))
                    {
                        errors.Add(String.Format("Regex {0} did not match stderr", expectedRegex.ToString()));
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Clears all of the expected results and resets them to the default values
        /// </summary>
        public virtual void SetDefaultExpectedResults()
        {
            this.ExpectedErrorRegexs = new List<Regex>();
            this.ExpectedErrorStrings = new List<string>();
            this.ExpectedExitCode = null;
            this.ExpectedOutputRegexs = new List<Regex>();
            this.ExpectedOutputStrings = new List<string>();
        }

        /// <summary>
        /// Returns a string with data contained in the result.
        /// </summary>
        /// <returns>A string</returns>
        private static string FormatResult(ExternalExecutableResult result)
        {
            var returnValue = new StringBuilder();
            returnValue.AppendLine();
            returnValue.AppendLine("----------------");
            returnValue.AppendLine("Tool run result:");
            returnValue.AppendLine("----------------");
            returnValue.AppendLine("Command:");
            returnValue.AppendLine($"\"{result.StartInfo.FileName}\" {result.StartInfo.Arguments}");
            returnValue.AppendLine();
            returnValue.AppendLine("Standard Output:");
            foreach (var line in result.StandardOutput ?? new string[0])
            {
                returnValue.AppendLine(line);
            }
            returnValue.AppendLine("Standard Error:");
            foreach (var line in result.StandardError ?? new string[0])
            {
                returnValue.AppendLine(line);
            }
            returnValue.AppendLine("Exit Code:");
            returnValue.AppendLine(Convert.ToString(result.ExitCode));
            returnValue.AppendLine("----------------");

            return returnValue.ToString();
        }
    }
}
