// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    /// <summary>
    /// Exception when file does not match the expected format.
    /// </summary>
    public class WixUnexpectedFileFormatException : WixException
    {
        public WixUnexpectedFileFormatException(string path, string expectedFormat, string format, Exception innerException = null)
            : base(ErrorMessages.UnexpectedFileFormat(path, expectedFormat, format), innerException)
        {
            this.Path = path;
            this.ExpectedFileFormat = expectedFormat;
            this.FileFormat = format;
        }

        /// <summary>
        /// Gets the expected file format.
        /// </summary>
        public string ExpectedFileFormat { get; }

        /// <summary>
        /// Gets the actual file format found in the file.
        /// </summary>
        public string FileFormat { get; }

        /// <summary>
        /// Gets the path to the file with unexpected format.
        /// </summary>
        public string Path { get; set; }
    }
}
