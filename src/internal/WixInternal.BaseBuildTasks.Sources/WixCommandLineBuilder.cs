// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BaseBuildTasks
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Helper class for appending the command line arguments.
    /// </summary>
    public class WixCommandLineBuilder : CommandLineBuilder
    {
        internal const int Unspecified = -1;

        /// <summary>
        /// Append a switch to the command line if the value has been specified.
        /// </summary>
        /// <param name="switchName">Switch to append.</param>
        /// <param name="value">Value specified by the user.</param>
        public void AppendIfSpecified(string switchName, int value)
        {
            if (value != Unspecified)
            {
                this.AppendSwitchIfNotNull(switchName, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Append a switch to the command line if the condition is true.
        /// </summary>
        /// <param name="switchName">Switch to append.</param>
        /// <param name="condition">Condition specified by the user.</param>
        public void AppendIfTrue(string switchName, bool condition)
        {
            if (condition)
            {
                this.AppendSwitch(switchName);
            }
        }

        /// <summary>
        /// Append a switch to the command line if any values in the array have been specified.
        /// </summary>
        /// <param name="switchName">Switch to append.</param>
        /// <param name="values">Values specified by the user.</param>
        public void AppendArrayIfNotNull(string switchName, IEnumerable<ITaskItem> values)
        {
            if (values != null)
            {
                foreach (ITaskItem value in values)
                {
                    this.AppendSwitchIfNotNull(switchName, value);
                }
            }
        }

        /// <summary>
        /// Append a switch to the command line if any values in the array have been specified.
        /// </summary>
        /// <param name="switchName">Switch to append.</param>
        /// <param name="values">Values specified by the user.</param>
        public void AppendArrayIfNotNull(string switchName, IEnumerable<string> values)
        {
            if (values != null)
            {
                foreach (string value in values)
                {
                    this.AppendSwitchIfNotNull(switchName, value);
                }
            }
        }

        /// <summary>
        /// Append arbitrary text to the command-line if specified.
        /// </summary>
        /// <param name="textToAppend">Text to append.</param>
        public void AppendTextIfNotNull(string textToAppend)
        {
            if (!String.IsNullOrEmpty(textToAppend))
            {
                this.AppendSpaceIfNotEmpty();
                this.AppendTextUnquoted(textToAppend);
            }
        }

        /// <summary>
        /// Append arbitrary text to the command-line if specified.
        /// </summary>
        /// <param name="textToAppend">Text to append.</param>
        public void AppendTextIfNotWhitespace(string textToAppend)
        {
            if (!String.IsNullOrWhiteSpace(textToAppend))
            {
                this.AppendSpaceIfNotEmpty();
                this.AppendTextUnquoted(textToAppend);
            }
        }
    }
}
