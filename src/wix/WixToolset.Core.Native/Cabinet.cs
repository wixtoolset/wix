// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;

    /// <summary>
    /// Cabinet create, enumerate and extract mechanism.
    /// </summary>
    public sealed class Cabinet
    {
        private const string CompressionLevelVariable = "WIX_COMPRESSION_LEVEL";
        private static readonly char[] TextLineSplitter = new[] { '\t' };

        /// <summary>
        /// Creates a cabinet creation, enumeration, extraction mechanism.
        /// </summary>
        /// <param name="path">Path of cabinet</param>
        public Cabinet(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// Cabinet path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Creates a cabinet.
        /// </summary>
        /// <param name="files">Files to compress.</param>
        /// <param name="compressionLevel">Level of compression to apply.</param>
        /// <param name="maxSize">Maximum size of cabinet.</param>
        /// <param name="maxThresh">Maximum threshold for each cabinet.</param>
        public void Compress(IEnumerable<CabinetCompressFile> files, CompressionLevel compressionLevel, int maxSize = 0, int maxThresh = 0)
        {
            var compressionLevelVariable = Environment.GetEnvironmentVariable(CompressionLevelVariable);

            // Override authored compression level if environment variable is present.
            if (!String.IsNullOrEmpty(compressionLevelVariable))
            {
                if (!Enum.TryParse(compressionLevelVariable, true, out compressionLevel))
                {
                    throw new WixException(ErrorMessages.IllegalEnvironmentVariable(CompressionLevelVariable, compressionLevelVariable));
                }
            }

            var wixnative = new WixNativeExe("smartcab", this.Path, Convert.ToInt32(compressionLevel), files.Count(), maxSize, maxThresh);

            foreach (var file in files)
            {
                wixnative.AddStdinLine(file.ToWixNativeStdinLine());
            }

            wixnative.Run();

#if TOOD_ERROR_HANDLING
            catch (COMException ce)
            {
                // If we get a "the file exists" error, we must have a full temp directory - so report the issue
                if (0x80070050 == unchecked((uint)ce.ErrorCode))
                {
                    throw new WixException(WixErrors.FullTempDirectory("WSC", Path.GetTempPath()));
                }

                throw;
            }
#endif
        }

        /// <summary>
        /// Enumerates all files in a cabinet.
        /// </summary>
        /// <returns>>List of CabinetFileInfo</returns>
        public List<CabinetFileInfo> Enumerate()
        {
            var wixnative = new WixNativeExe("enumcab", this.Path);
            var lines = wixnative.Run();

            var fileInfoList = new List<CabinetFileInfo>();

            foreach (var line in lines)
            {
                if (String.IsNullOrEmpty(line))
                {
                    continue;
                }

                var data = line.Split(TextLineSplitter, StringSplitOptions.None);

                var size = Convert.ToInt32(data[1]);
                var date = Convert.ToInt32(data[2]);
                var time = Convert.ToInt32(data[3]);

                fileInfoList.Add(new CabinetFileInfo(data[0], size, date, time));
            }

            return fileInfoList;
        }

        /// <summary>
        /// Extracts all the files from a cabinet to a directory.
        /// </summary>
        /// <param name="outputFolder">Directory to extract files to.</param>
        public IEnumerable<string> Extract(string outputFolder)
        {
            if (!outputFolder.EndsWith("\\", StringComparison.Ordinal))
            {
                outputFolder += "\\";
            }

            var wixnative = new WixNativeExe("extractcab", this.Path, outputFolder);
            return wixnative.Run().Where(output => !String.IsNullOrWhiteSpace(output));
        }
    }
}
