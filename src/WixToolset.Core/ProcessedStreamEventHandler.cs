// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Xml.Linq;

    /// <summary>
    /// Preprocessed output stream event handler delegate.
    /// </summary>
    /// <param name="sender">Sender of the message.</param>
    /// <param name="ea">Arguments for the preprocessed stream event.</param>
    public delegate void ProcessedStreamEventHandler(object sender, ProcessedStreamEventArgs e);

    /// <summary>
    /// Event args for preprocessed stream event.
    /// </summary>
    public class ProcessedStreamEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new ProcessedStreamEventArgs.
        /// </summary>
        /// <param name="sourceFile">Source file that is preprocessed.</param>
        /// <param name="document">Preprocessed output document.</param>
        public ProcessedStreamEventArgs(string sourceFile, XDocument document)
        {
            this.SourceFile = sourceFile;
            this.Document = document;
        }

        /// <summary>
        /// Gets the full path of the source file.
        /// </summary>
        /// <value>The full path of the source file.</value>
        public string SourceFile { get; private set; }

        /// <summary>
        /// Gets the preprocessed output stream.
        /// </summary>
        /// <value>The the preprocessed output stream.</value>
        public XDocument Document { get; private set; }
    }
}
