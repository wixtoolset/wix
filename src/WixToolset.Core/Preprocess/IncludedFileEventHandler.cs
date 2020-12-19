// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Preprocess
{
    using System;
    using WixToolset.Data;

    /// <summary>
    /// Included file event handler delegate.
    /// </summary>
    /// <param name="sender">Sender of the message.</param>
    /// <param name="e">Arguments for the included file event.</param>
    internal delegate void IncludedFileEventHandler(object sender, IncludedFileEventArgs e);

    /// <summary>
    /// Event args for included file event.
    /// </summary>
    internal class IncludedFileEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new IncludedFileEventArgs.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers for the included file.</param>
        /// <param name="fullName">The full path of the included file.</param>
        public IncludedFileEventArgs(SourceLineNumber sourceLineNumbers, string fullName)
        {
            this.SourceLineNumbers = sourceLineNumbers;
            this.FullName = fullName;
        }

        /// <summary>
        /// Gets the full path of the included file.
        /// </summary>
        /// <value>The full path of the included file.</value>
        public string FullName { get; }

        /// <summary>
        /// Gets the source line numbers.
        /// </summary>
        /// <value>The source line numbers.</value>
        public SourceLineNumber SourceLineNumbers { get; }
    }
}
