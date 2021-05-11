// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using Microsoft.Win32;
    using WixTestTools;
    using WixToolset.Mba.Core;

    public class TestBAController : IDisposable
    {
        public TestBAController(WixTestContext testContext, bool x64 = false)
        {
            this.TestGroupName = testContext.TestGroupName;
            this.BaseRegKeyPath = x64 ? @"Software\WiX\Tests" : @"Software\WOW6432Node\WiX\Tests";
            this.TestBaseRegKeyPath = String.Format(@"{0}\TestBAControl\{1}", this.BaseRegKeyPath, this.TestGroupName);
        }

        private string BaseRegKeyPath { get; }

        private string TestBaseRegKeyPath { get; }

        public string TestGroupName { get; }

        /// <summary>
        /// Sets a test value in the registry to communicate with the TestBA.
        /// </summary>
        /// <param name="name">Name of the value to set.</param>
        /// <param name="value">Value to set. If this is null, the value is removed.</param>
        public void SetBurnTestValue(string name, string value)
        {
            using (var testKey = Registry.LocalMachine.CreateSubKey(this.TestBaseRegKeyPath))
            {
                if (String.IsNullOrEmpty(value))
                {
                    testKey.DeleteValue(name, false);
                }
                else
                {
                    testKey.SetValue(name, value);
                }
            }
        }

        public void SetExplicitlyElevateAndPlanFromOnElevateBegin(string value = "true")
        {
            this.SetBurnTestValue("ExplicitlyElevateAndPlanFromOnElevateBegin", value);
        }

        public void SetImmediatelyQuit(string value = "true")
        {
            this.SetBurnTestValue("ImmediatelyQuit", value);
        }

        public void SetQuitAfterDetect(string value = "true")
        {
            this.SetBurnTestValue("QuitAfterDetect", value);
        }

        /// <summary>
        /// Slows the cache progress of a package.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="delay">Sets or removes the delay on a package being cached.</param>
        public void SetPackageSlowCache(string packageId, int? delay)
        {
            this.SetPackageState(packageId, "SlowCache", delay.HasValue ? delay.ToString() : null);
        }

        /// <summary>
        /// Cancels the cache of a package at a particular progress point.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="cancelPoint">Sets or removes the cancel progress on a package being cached.</param>
        public void SetPackageCancelCacheAtProgress(string packageId, int? cancelPoint)
        {
            this.SetPackageState(packageId, "CancelCacheAtProgress", cancelPoint.HasValue ? cancelPoint.ToString() : null);
        }

        /// <summary>
        /// Slows the execute progress of a package.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="delay">Sets or removes the delay on a package being executed.</param>
        public void SetPackageSlowExecute(string packageId, int? delay)
        {
            this.SetPackageState(packageId, "SlowExecute", delay.HasValue ? delay.ToString() : null);
        }

        /// <summary>
        /// Cancels the execute of a package at a particular progress point.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="cancelPoint">Sets or removes the cancel progress on a package being executed.</param>
        public void SetPackageCancelExecuteAtProgress(string packageId, int? cancelPoint)
        {
            this.SetPackageState(packageId, "CancelExecuteAtProgress", cancelPoint.HasValue ? cancelPoint.ToString() : null);
        }

        /// <summary>
        /// Cancels the execute of a package at the next progess after the specified MSI action start.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="actionName">Sets or removes the cancel progress on a package being executed.</param>
        public void SetPackageCancelExecuteAtActionStart(string packageId, string actionName)
        {
            this.SetPackageState(packageId, "CancelExecuteAtActionStart", actionName);
        }

        /// <summary>
        /// Cancels the execute of a package at a particular OnProgress point.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="cancelPoint">Sets or removes the cancel OnProgress point on a package being executed.</param>
        public void SetPackageCancelOnProgressAtProgress(string packageId, int? cancelPoint)
        {
            this.SetPackageState(packageId, "CancelOnProgressAtProgress", cancelPoint.HasValue ? cancelPoint.ToString() : null);
        }

        /// <summary>
        /// Sets the requested state for a package that the TestBA will return to the engine during plan.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="state">State to request.</param>
        public void SetPackageRequestedState(string packageId, RequestState state)
        {
            this.SetPackageState(packageId, "Requested", state.ToString());
        }

        /// <summary>
        /// Sets the requested state for a package that the TestBA will return to the engine during plan.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="state">State to request.</param>
        public void SetPackageFeatureState(string packageId, string featureId, FeatureState state)
        {
            this.SetPackageState(packageId, String.Concat(featureId, "Requested"), state.ToString());
        }

        /// <summary>
        /// Sets the number of times to re-run the Detect phase.
        /// </summary>
        /// <param name="state">Number of times to run Detect (after the first, normal, Detect).</param>
        public void SetRedetectCount(int redetectCount)
        {
            this.SetPackageState(null, "RedetectCount", redetectCount.ToString());
        }

        /// <summary>
        /// Resets the state for a package that the TestBA will return to the engine during plan.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        public void ResetPackageStates(string packageId)
        {
            var key = String.Format(@"{0}\{1}", this.TestBaseRegKeyPath, packageId ?? String.Empty);
            Registry.LocalMachine.DeleteSubKey(key);
        }

        public void SetVerifyArguments(string verifyArguments)
        {
            this.SetBurnTestValue("VerifyArguments", verifyArguments);

        }

        private void SetPackageState(string packageId, string name, string value)
        {
            var key = String.Format(@"{0}\{1}", this.TestBaseRegKeyPath, packageId ?? String.Empty);
            using (var packageKey = Registry.LocalMachine.CreateSubKey(key))
            {
                if (String.IsNullOrEmpty(value))
                {
                    packageKey.DeleteValue(name, false);
                }
                else
                {
                    packageKey.SetValue(name, value);
                }
            }
        }

        public void Dispose()
        {
            Registry.LocalMachine.DeleteSubKeyTree($@"{this.BaseRegKeyPath}\{this.TestGroupName}", false);
            Registry.LocalMachine.DeleteSubKeyTree($@"{this.BaseRegKeyPath}\TestBAControl", false);
        }
    }
}
