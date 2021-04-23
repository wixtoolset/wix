// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Native.Msi
{
    using System;
    using System.Diagnostics;
    using System.Text;

    /// <summary>
    /// A callback function that the installer calls for progress notification and error messages.
    /// </summary>
    /// <param name="context">Pointer to an application context.
    /// This parameter can be used for error checking.</param>
    /// <param name="messageType">Specifies a combination of one message box style,
    /// one message box icon type, one default button, and one installation message type.</param>
    /// <param name="message">Specifies the message text.</param>
    /// <returns>-1 for an error, 0 if no action was taken, 1 if OK, 3 to abort.</returns>
    public delegate int InstallUIHandler(IntPtr context, uint messageType, string message);

    /// <summary>
    /// Represents the Windows Installer, provides wrappers to
    /// create the top-level objects and access their methods.
    /// </summary>
    public static class Installer
    {
        /// <summary>
        /// Extacts the patch metadata as XML.
        /// </summary>
        /// <param name="path">Path to patch.</param>
        /// <returns>String XML.</returns>
        public static string ExtractPatchXml(string path)
        {
            var buffer = new StringBuilder(65535);
            var size = buffer.Capacity;

            var error = MsiInterop.MsiExtractPatchXMLData(path, 0, buffer, ref size);
            if (234 == error)
            {
                buffer.EnsureCapacity(++size);
                error = MsiInterop.MsiExtractPatchXMLData(path, 0, buffer, ref size);
            }

            if (error != 0)
            {
                throw new MsiException(error);
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Takes the path to a file and returns a 128-bit hash of that file.
        /// </summary>
        /// <param name="filePath">Path to file that is to be hashed.</param>
        /// <param name="options">The value in this column must be 0. This parameter is reserved for future use.</param>
        /// <param name="hash">Int array that receives the returned file hash information.</param>
        public static void GetFileHash(string filePath, int options, out int[] hash)
        {
            var hashInterop = new MSIFILEHASHINFO();
            hashInterop.FileHashInfoSize = 20;

            var error = MsiInterop.MsiGetFileHash(filePath, Convert.ToUInt32(options), hashInterop);
            if (0 != error)
            {
                throw new MsiException(error);
            }

            Debug.Assert(20 == hashInterop.FileHashInfoSize);

            hash = new int[4];
            hash[0] = hashInterop.Data0;
            hash[1] = hashInterop.Data1;
            hash[2] = hashInterop.Data2;
            hash[3] = hashInterop.Data3;
        }

        /// <summary>
        /// Returns the version string and language string in the format that the installer 
        /// expects to find them in the database.  If you just want version information, set 
        /// lpLangBuf and pcchLangBuf to zero. If you just want language information, set 
        /// lpVersionBuf and pcchVersionBuf to zero.
        /// </summary>
        /// <param name="filePath">Specifies the path to the file.</param>
        /// <param name="version">Returns the file version. Set to 0 for language information only.</param>
        /// <param name="language">Returns the file language. Set to 0 for version information only.</param>
        public static void GetFileVersion(string filePath, out string version, out string language)
        {
            var versionLength = 20;
            var languageLength = 20;
            var versionBuffer = new StringBuilder(versionLength);
            var languageBuffer = new StringBuilder(languageLength);

            var error = MsiInterop.MsiGetFileVersion(filePath, versionBuffer, ref versionLength, languageBuffer, ref languageLength);
            if (234 == error)
            {
                versionBuffer.EnsureCapacity(++versionLength);
                languageBuffer.EnsureCapacity(++languageLength);
                error = MsiInterop.MsiGetFileVersion(filePath, versionBuffer, ref versionLength, languageBuffer, ref languageLength);
            }
            else if (1006 == error)
            {
                // file has no version or language, so no error
                error = 0;
            }

            if (0 != error)
            {
                throw new MsiException(error);
            }

            version = versionBuffer.ToString();
            language = languageBuffer.ToString();
        }

        /// <summary>
        /// Enables an external user-interface handler.
        /// </summary>
        /// <param name="installUIHandler">Specifies a callback function.</param>
        /// <param name="messageFilter">Specifies which messages to handle using the external message handler.</param>
        /// <param name="context">Pointer to an application context that is passed to the callback function.</param>
        /// <returns>The return value is the previously set external handler, or null if there was no previously set handler.</returns>
        public static InstallUIHandler SetExternalUI(InstallUIHandler installUIHandler, int messageFilter, IntPtr context)
        {
            return MsiInterop.MsiSetExternalUI(installUIHandler, messageFilter, context);
        }

        /// <summary>
        /// Enables the installer's internal user interface.
        /// </summary>
        /// <param name="uiLevel">Specifies the level of complexity of the user interface.</param>
        /// <param name="hwnd">Pointer to a window. This window becomes the owner of any user interface created.</param>
        /// <returns>The previous user interface level is returned. If an invalid dwUILevel is passed, then INSTALLUILEVEL_NOCHANGE is returned.</returns>
        public static int SetInternalUI(int uiLevel, ref IntPtr hwnd)
        {
            return MsiInterop.MsiSetInternalUI(uiLevel, ref hwnd);
        }
    }
}
