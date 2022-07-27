// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.HeatTasks
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Build.Utilities;

    /// <summary>
    /// Helper class for appending the command line arguments.
    /// </summary>
    public class WixCommandLineBuilder : CommandLineBuilder
    {
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
        public void AppendArrayIfNotNull(string switchName, IEnumerable<string> values)
        {
            if (values != null)
            {
                foreach (var value in values)
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
            if (!String.IsNullOrWhiteSpace(textToAppend))
            {
                this.AppendSpaceIfNotEmpty();
                this.AppendTextUnquoted(textToAppend);
            }
        }
    }
}
