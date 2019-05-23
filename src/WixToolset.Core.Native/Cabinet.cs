// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;

    /// <summary>
    /// Wrapper class around interop with wixcab.dll to compress files into a cabinet.
    /// </summary>
    public sealed class Cabinet
    {
        private const string CompressionLevelVariable = "WIX_COMPRESSION_LEVEL";
        private static readonly char[] TextLineSplitter = new[] { '\t' };

        public Cabinet(string path)
        {
            this.Path = path;
        }

        public string Path { get; }

        /// <summary>
        /// Creates a cabinet.
        /// </summary>
        /// <param name="cabPath">Path of cabinet to create.</param>
        /// <param name="compressionLevel">Level of compression to apply.</param>
        /// <param name="maxFiles">Maximum number of files that will be added to cabinet.</param>
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
        public void Extract(string outputFolder)
        {
            if (!outputFolder.EndsWith("\\", StringComparison.Ordinal))
            {
                outputFolder += "\\";
            }

            var wixnative = new WixNativeExe("extractcab", this.Path, outputFolder);
            wixnative.Run();
        }

#if TOOD_ERROR_HANDLING
        /// <summary>
        /// Adds a file to the cabinet with an optional MSI file hash.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="token">The token for the file.</param>
        /// <param name="fileHash">The MSI file hash of the file.</param>
        //private void AddFile(string file, string token, MsiInterop.MSIFILEHASHINFO fileHash)
        //{
        //    try
        //    {
        //        NativeMethods.CreateCabAddFile(file, token, fileHash, this.handle);
        //    }
        //    catch (COMException ce)
        //    {
        //        if (0x80004005 == unchecked((uint)ce.ErrorCode)) // E_FAIL
        //        {
        //            throw new WixException(WixErrors.CreateCabAddFileFailed());
        //        }
        //        else if (0x80070070 == unchecked((uint)ce.ErrorCode)) // ERROR_DISK_FULL
        //        {
        //            throw new WixException(WixErrors.CreateCabInsufficientDiskSpace());
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }
        //    catch (DirectoryNotFoundException)
        //    {
        //        throw new WixFileNotFoundException(file);
        //    }
        //    catch (FileNotFoundException)
        //    {
        //        throw new WixFileNotFoundException(file);
        //    }
        //}

        /// <summary>
        /// Complete/commit the cabinet - this must be called before Dispose so that errors will be
        /// reported on the same thread.
        /// </summary>
        /// <param name="newCabNamesCallBackAddress">Address of Binder's callback function for Cabinet Splitting</param>
        public void Complete(IntPtr newCabNamesCallBackAddress)
        {
            if (IntPtr.Zero != this.handle)
            {
                try
                {
                    if (newCabNamesCallBackAddress != IntPtr.Zero && this.maxSize != 0)
                    {
                        NativeMethods.CreateCabFinish(this.handle, newCabNamesCallBackAddress);
                    }
                    else
                    {
                        NativeMethods.CreateCabFinish(this.handle, IntPtr.Zero);
                    }

                    GC.SuppressFinalize(this);
                    this.disposed = true;
                }
                catch (COMException ce)
                {
                    //if (0x80004005 == unchecked((uint)ce.ErrorCode)) // E_FAIL
                    //{
                    //    // This error seems to happen, among other situations, when cabbing more than 0xFFFF files
                    //    throw new WixException(WixErrors.FinishCabFailed());
                    //}
                    //else if (0x80070070 == unchecked((uint)ce.ErrorCode)) // ERROR_DISK_FULL
                    //{
                    //    throw new WixException(WixErrors.CreateCabInsufficientDiskSpace());
                    //}
                    //else
                    //{
                    //    throw;
                    //}
                }
                finally
                {
                    this.handle = IntPtr.Zero;
                }
            }
        }
#endif
    }
}
