// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    /// Default implementation of <see cref="IEngine"/>.
    /// </summary>
    public sealed class Engine : IEngine
    {
        private IBootstrapperEngine engine;

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
            return BalUtil.BalVariableExistsFromEngine(this.engine, name);
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
            StrUtil.StrHandle handle = new StrUtil.StrHandle();
            try
            {
                int ret = BalUtil.BalEscapeStringFromEngine(this.engine, input, ref handle);
                if (ret != NativeMethods.S_OK)
                {
                    throw new Win32Exception(ret);
                }

                return handle.ToUniString();
            }
            finally
            {
                handle.Dispose();
            }
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
            StrUtil.StrHandle handle = new StrUtil.StrHandle();
            try
            {
                int ret = BalUtil.BalFormatStringFromEngine(this.engine, format, ref handle);
                if (ret != NativeMethods.S_OK)
                {
                    throw new Win32Exception(ret);
                }

                return handle.ToUniString();
            }
            finally
            {
                handle.Dispose();
            }
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
            StrUtil.StrHandle handle = new StrUtil.StrHandle();
            try
            {
                int ret = BalUtil.BalGetStringVariableFromEngine(this.engine, name, ref handle);
                if (ret != NativeMethods.S_OK)
                {
                    throw new Win32Exception(ret);
                }

                return handle.ToSecureString();
            }
            finally
            {
                handle.Dispose();
            }
        }

        /// <inheritdoc/>
        public string GetVariableString(string name)
        {
            StrUtil.StrHandle handle = new StrUtil.StrHandle();
            try
            {
                int ret = BalUtil.BalGetStringVariableFromEngine(this.engine, name, ref handle);
                if (ret != NativeMethods.S_OK)
                {
                    throw new Win32Exception(ret);
                }

                return handle.ToUniString();
            }
            finally
            {
                handle.Dispose();
            }
        }

        /// <inheritdoc/>
        public string GetVariableVersion(string name)
        {
            StrUtil.StrHandle handle = new StrUtil.StrHandle();
            try
            {
                int ret = BalUtil.BalGetVersionVariableFromEngine(this.engine, name, ref handle);
                if (ret != NativeMethods.S_OK)
                {
                    throw new Win32Exception(ret);
                }

                return handle.ToUniString();
            }
            finally
            {
                handle.Dispose();
            }
        }

        /// <inheritdoc/>
        public string GetRelatedBundleVariable(string bundleId, string name)
        {
            StrUtil.StrHandle handle = new StrUtil.StrHandle();
            try
            {
                int ret = BalUtil.BalGetRelatedBundleVariableFromEngine(this.engine, bundleId, name, ref handle);
                if (ret != NativeMethods.S_OK)
                {
                    throw new Win32Exception(ret);
                }

                return handle.ToUniString();
            }
            finally
            {
                handle.Dispose();
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
        public void SetUpdate(string localSource, string downloadSource, long size, UpdateHashType hashType, string hash, string updatePackageId)
        {
            this.engine.SetUpdate(localSource, downloadSource, size, hashType, hash, updatePackageId);
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
    }
}
