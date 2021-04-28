// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    /// <summary>
    /// Default implementation of <see cref="IEngine"/>.
    /// </summary>
    public sealed class Engine : IEngine
    {
        // Burn errs on empty strings, so declare initial buffer size.
        private const int InitialBufferSize = 80;
        private static readonly string normalizeVersionFormatString = "{0} must be less than or equal to " + UInt16.MaxValue;

        private IBootstrapperEngine engine;

        /// <summary>
        /// Creates a new instance of the <see cref="Engine"/> container class.
        /// </summary>
        /// <param name="engine">The <see cref="IBootstrapperEngine"/> to contain.</param>
        internal Engine(IBootstrapperEngine engine)
        {
            this.engine = engine;
        }

        /// <inheritdoc/>
        public int PackageCount
        {
            get
            {
                int count;
                this.engine.GetPackageCount(out count);

                return count;
            }
        }

        /// <inheritdoc/>
        public void Apply(IntPtr hwndParent)
        {
            this.engine.Apply(hwndParent);
        }

        /// <inheritdoc/>
        public void CloseSplashScreen()
        {
            this.engine.CloseSplashScreen();
        }

        /// <inheritdoc/>
        public int CompareVersions(string version1, string version2)
        {
            this.engine.CompareVersions(version1, version2, out var result);
            return result;
        }

        /// <inheritdoc/>
        public bool ContainsVariable(string name)
        {
            IntPtr capacity = new IntPtr(0);
            int ret = this.engine.GetVariableString(name, IntPtr.Zero, ref capacity);
            return NativeMethods.E_NOTFOUND != ret;
        }

        /// <inheritdoc/>
        public void Detect()
        {
            this.Detect(IntPtr.Zero);
        }

        /// <inheritdoc/>
        public void Detect(IntPtr hwndParent)
        {
            this.engine.Detect(hwndParent);
        }

        /// <inheritdoc/>
        public bool Elevate(IntPtr hwndParent)
        {
            int ret = this.engine.Elevate(hwndParent);

            if (NativeMethods.S_OK == ret || NativeMethods.E_ALREADYINITIALIZED == ret)
            {
                return true;
            }
            else if (NativeMethods.E_CANCELLED == ret)
            {
                return false;
            }
            else
            {
                throw new Win32Exception(ret);
            }
        }

        /// <inheritdoc/>
        public string EscapeString(string input)
        {
            IntPtr capacity = new IntPtr(InitialBufferSize);
            StringBuilder sb = new StringBuilder(capacity.ToInt32());

            // Get the size of the buffer.
            int ret = this.engine.EscapeString(input, sb, ref capacity);
            if (NativeMethods.E_INSUFFICIENT_BUFFER == ret || NativeMethods.E_MOREDATA == ret)
            {
                capacity = new IntPtr(capacity.ToInt32() + 1); // Add one for the null terminator.
                sb.Capacity = capacity.ToInt32();
                ret = this.engine.EscapeString(input, sb, ref capacity);
            }

            if (NativeMethods.S_OK != ret)
            {
                throw new Win32Exception(ret);
            }

            return sb.ToString();
        }

        /// <inheritdoc/>
        public bool EvaluateCondition(string condition)
        {
            bool value;
            this.engine.EvaluateCondition(condition, out value);

            return value;
        }

        /// <inheritdoc/>
        public string FormatString(string format)
        {
            IntPtr capacity = new IntPtr(InitialBufferSize);
            StringBuilder sb = new StringBuilder(capacity.ToInt32());

            // Get the size of the buffer.
            int ret = this.engine.FormatString(format, sb, ref capacity);
            if (NativeMethods.E_INSUFFICIENT_BUFFER == ret || NativeMethods.E_MOREDATA == ret)
            {
                capacity = new IntPtr(capacity.ToInt32() + 1); // Add one for the null terminator.
                sb.Capacity = capacity.ToInt32();
                ret = this.engine.FormatString(format, sb, ref capacity);
            }

            if (NativeMethods.S_OK != ret)
            {
                throw new Win32Exception(ret);
            }

            return sb.ToString();
        }

        /// <inheritdoc/>
        public long GetVariableNumeric(string name)
        {
            int ret = this.engine.GetVariableNumeric(name, out long value);
            if (NativeMethods.S_OK != ret)
            {
                throw new Win32Exception(ret);
            }

            return value;
        }

        /// <inheritdoc/>
        public SecureString GetVariableSecureString(string name)
        {
            var pUniString = this.getStringVariable(name, out var length);
            try
            {
                return this.convertToSecureString(pUniString, length);
            }
            finally
            {
                if (IntPtr.Zero != pUniString)
                {
                    Marshal.FreeCoTaskMem(pUniString);
                }
            }
        }

        /// <inheritdoc/>
        public string GetVariableString(string name)
        {
            int length;
            IntPtr pUniString = this.getStringVariable(name, out length);
            try
            {
                return Marshal.PtrToStringUni(pUniString, length);
            }
            finally
            {
                if (IntPtr.Zero != pUniString)
                {
                    Marshal.FreeCoTaskMem(pUniString);
                }
            }
        }

        /// <inheritdoc/>
        public string GetVariableVersion(string name)
        {
            int length;
            IntPtr pUniString = this.getVersionVariable(name, out length);
            try
            {
                return Marshal.PtrToStringUni(pUniString, length);
            }
            finally
            {
                if (IntPtr.Zero != pUniString)
                {
                    Marshal.FreeCoTaskMem(pUniString);
                }
            }
        }

        /// <inheritdoc/>
        public void LaunchApprovedExe(IntPtr hwndParent, string approvedExeForElevationId, string arguments)
        {
            this.LaunchApprovedExe(hwndParent, approvedExeForElevationId, arguments, 0);
        }

        /// <inheritdoc/>
        public void LaunchApprovedExe(IntPtr hwndParent, string approvedExeForElevationId, string arguments, int waitForInputIdleTimeout)
        {
            this.engine.LaunchApprovedExe(hwndParent, approvedExeForElevationId, arguments, waitForInputIdleTimeout);
        }
        /// <inheritdoc/>

        public void Log(LogLevel level, string message)
        {
            this.engine.Log(level, message);
        }

        /// <inheritdoc/>
        public void Plan(LaunchAction action)
        {
            this.engine.Plan(action);
        }

        /// <inheritdoc/>
        public void SetUpdate(string localSource, string downloadSource, long size, UpdateHashType hashType, byte[] hash)
        {
            this.engine.SetUpdate(localSource, downloadSource, size, hashType, hash, null == hash ? 0 : hash.Length);
        }

        /// <inheritdoc/>
        public void SetUpdateSource(string url)
        {
            this.engine.SetUpdateSource(url);
        }

        /// <inheritdoc/>
        public void SetLocalSource(string packageOrContainerId, string payloadId, string path)
        {
            this.engine.SetLocalSource(packageOrContainerId, payloadId, path);
        }

        /// <inheritdoc/>
        public void SetDownloadSource(string packageOrContainerId, string payloadId, string url, string user, string password)
        {
            this.engine.SetDownloadSource(packageOrContainerId, payloadId, url, user, password);
        }

        /// <inheritdoc/>
        public void SetVariableNumeric(string name, long value)
        {
            this.engine.SetVariableNumeric(name, value);
        }

        /// <inheritdoc/>
        public void SetVariableString(string name, SecureString value, bool formatted)
        {
            IntPtr pValue = Marshal.SecureStringToCoTaskMemUnicode(value);
            try
            {
                this.engine.SetVariableString(name, pValue, formatted);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pValue);
            }
        }

        /// <inheritdoc/>
        public void SetVariableString(string name, string value, bool formatted)
        {
            IntPtr pValue = Marshal.StringToCoTaskMemUni(value);
            try
            {
                this.engine.SetVariableString(name, pValue, formatted);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pValue);
            }
        }

        /// <inheritdoc/>
        public void SetVariableVersion(string name, string value)
        {
            IntPtr pValue = Marshal.StringToCoTaskMemUni(value);
            try
            {
                this.engine.SetVariableVersion(name, pValue);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pValue);
            }
        }

        /// <inheritdoc/>
        public int SendEmbeddedError(int errorCode, string message, int uiHint)
        {
            int result = 0;
            this.engine.SendEmbeddedError(errorCode, message, uiHint, out result);
            return result;
        }

        /// <inheritdoc/>
        public int SendEmbeddedProgress(int progressPercentage, int overallPercentage)
        {
            int result = 0;
            this.engine.SendEmbeddedProgress(progressPercentage, overallPercentage, out result);
            return result;
        }

        /// <inheritdoc/>
        public void Quit(int exitCode)
        {
            this.engine.Quit(exitCode);
        }

        /// <summary>
        /// Gets the variable given by <paramref name="name"/> as a string.
        /// </summary>
        /// <param name="name">The name of the variable to get.</param>
        /// <param name="length">The length of the Unicode string.</param>
        /// <returns>The value by a pointer to a Unicode string.  Must be freed by Marshal.FreeCoTaskMem.</returns>
        /// <exception cref="Exception">An error occurred getting the variable.</exception>
        internal IntPtr getStringVariable(string name, out int length)
        {
            IntPtr capacity = new IntPtr(InitialBufferSize);
            bool success = false;
            IntPtr pValue = Marshal.AllocCoTaskMem(capacity.ToInt32() * UnicodeEncoding.CharSize);
            try
            {
                // Get the size of the buffer.
                int ret = this.engine.GetVariableString(name, pValue, ref capacity);
                if (NativeMethods.E_INSUFFICIENT_BUFFER == ret || NativeMethods.E_MOREDATA == ret)
                {
                    // Don't need to add 1 for the null terminator, the engine already includes that.
                    pValue = Marshal.ReAllocCoTaskMem(pValue, capacity.ToInt32() * UnicodeEncoding.CharSize);
                    ret = this.engine.GetVariableString(name, pValue, ref capacity);
                }

                if (NativeMethods.S_OK != ret)
                {
                    throw Marshal.GetExceptionForHR(ret);
                }

                // The engine only returns the exact length of the string if the buffer was too small, so calculate it ourselves.
                int maxLength = capacity.ToInt32();
                for (length = 0; length < maxLength; ++length)
                {
                    if (0 == Marshal.ReadInt16(pValue, length * UnicodeEncoding.CharSize))
                    {
                        break;
                    }
                }

                success = true;
                return pValue;
            }
            finally
            {
                if (!success && IntPtr.Zero != pValue)
                {
                    Marshal.FreeCoTaskMem(pValue);
                }
            }
        }

        /// <summary>
        /// Gets the variable given by <paramref name="name"/> as a version string.
        /// </summary>
        /// <param name="name">The name of the variable to get.</param>
        /// <param name="length">The length of the Unicode string.</param>
        /// <returns>The value by a pointer to a Unicode string.  Must be freed by Marshal.FreeCoTaskMem.</returns>
        /// <exception cref="Exception">An error occurred getting the variable.</exception>
        internal IntPtr getVersionVariable(string name, out int length)
        {
            IntPtr capacity = new IntPtr(InitialBufferSize);
            bool success = false;
            IntPtr pValue = Marshal.AllocCoTaskMem(capacity.ToInt32() * UnicodeEncoding.CharSize);
            try
            {
                // Get the size of the buffer.
                int ret = this.engine.GetVariableVersion(name, pValue, ref capacity);
                if (NativeMethods.E_INSUFFICIENT_BUFFER == ret || NativeMethods.E_MOREDATA == ret)
                {
                    // Don't need to add 1 for the null terminator, the engine already includes that.
                    pValue = Marshal.ReAllocCoTaskMem(pValue, capacity.ToInt32() * UnicodeEncoding.CharSize);
                    ret = this.engine.GetVariableVersion(name, pValue, ref capacity);
                }

                if (NativeMethods.S_OK != ret)
                {
                    throw Marshal.GetExceptionForHR(ret);
                }

                // The engine only returns the exact length of the string if the buffer was too small, so calculate it ourselves.
                int maxLength = capacity.ToInt32();
                for (length = 0; length < maxLength; ++length)
                {
                    if (0 == Marshal.ReadInt16(pValue, length * UnicodeEncoding.CharSize))
                    {
                        break;
                    }
                }

                success = true;
                return pValue;
            }
            finally
            {
                if (!success && IntPtr.Zero != pValue)
                {
                    Marshal.FreeCoTaskMem(pValue);
                }
            }
        }

        /// <summary>
        /// Initialize a SecureString with the given Unicode string.
        /// </summary>
        /// <param name="pUniString">Pointer to Unicode string.</param>
        /// <param name="length">The string's length.</param>
        internal SecureString convertToSecureString(IntPtr pUniString, int length)
        {
            if (IntPtr.Zero == pUniString)
            {
                return null;
            }

            SecureString value = new SecureString();
            short s;
            char c;
            for (int charIndex = 0; charIndex < length; charIndex++)
            {
                s = Marshal.ReadInt16(pUniString, charIndex * UnicodeEncoding.CharSize);
                c = (char)s;
                value.AppendChar(c);
                s = 0;
                c = (char)0;
            }
            return value;
        }

        /// <summary>
        /// Utility method for converting a <see cref="Version"/> into a <see cref="long"/>.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static long VersionToLong(Version version)
        {
            // In Windows, each version component has a max value of 65535,
            // so we truncate the version before shifting it, which will overflow if invalid.
            long major = (long)(ushort)version.Major << 48;
            long minor = (long)(ushort)version.Minor << 32;
            long build = (long)(ushort)version.Build << 16;
            long revision = (long)(ushort)version.Revision;

            return major | minor | build | revision;
        }

        /// <summary>
        /// Utility method for converting a <see cref="long"/> into a <see cref="Version"/>.
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static Version LongToVersion(long version)
        {
            int major = (int)((version & ((long)0xffff << 48)) >> 48);
            int minor = (int)((version & ((long)0xffff << 32)) >> 32);
            int build = (int)((version & ((long)0xffff << 16)) >> 16);
            int revision = (int)(version & 0xffff);

            return new Version(major, minor, build, revision);
        }

        /// <summary>
        /// Verifies that Version can be represented in a <see cref="long"/>.
        /// If the Build or Revision fields are undefined, they are set to zero.
        /// </summary>
        public static Version NormalizeVersion(Version version)
        {
            if (version == null)
            {
                throw new ArgumentNullException("version");
            }

            int major = version.Major;
            int minor = version.Minor;
            int build = version.Build;
            int revision = version.Revision;

            if (major > UInt16.MaxValue)
            {
                throw new ArgumentOutOfRangeException("version", String.Format(normalizeVersionFormatString, "Major"));
            }
            if (minor > UInt16.MaxValue)
            {
                throw new ArgumentOutOfRangeException("version", String.Format(normalizeVersionFormatString, "Minor"));
            }
            if (build > UInt16.MaxValue)
            {
                throw new ArgumentOutOfRangeException("version", String.Format(normalizeVersionFormatString, "Build"));
            }
            if (build == -1)
            {
                build = 0;
            }
            if (revision > UInt16.MaxValue)
            {
                throw new ArgumentOutOfRangeException("version", String.Format(normalizeVersionFormatString, "Revision"));
            }
            if (revision == -1)
            {
                revision = 0;
            }

            return new Version(major, minor, build, revision);
        }
    }
}
