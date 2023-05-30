// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Test.BA
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;
    using Microsoft.Win32;
    using WixToolset.Mba.Core;

    /// <summary>
    /// A minimal UX used for testing.
    /// </summary>
    public class TestBA : BootstrapperApplication
    {
        private const string BurnBundleVersionVariable = "WixBundleVersion";

        private Form dummyWindow;
        private IntPtr windowHandle;
        private LaunchAction action;
        private readonly ManualResetEvent wait;
        private int result;

        private string updateBundlePath;

        private bool allowAcquireAfterValidationFailure;
        private bool forceKeepRegistration;
        private bool immediatelyQuit;
        private bool quitAfterDetect;
        private bool explicitlyElevateAndPlanFromOnElevateBegin;
        private int redetectRemaining;
        private int sleepDuringCache;
        private int cancelCacheAtProgress;
        private int sleepDuringExecute;
        private int cancelExecuteAtProgress;
        private string cancelExecuteActionName;
        private int cancelOnProgressAtProgress;
        private int retryExecuteFilesInUse;
        private bool rollingBack;

        private IBootstrapperCommand Command { get; }

        private IEngine Engine => this.engine;

        /// <summary>
        /// Initializes test user experience.
        /// </summary>
        public TestBA(IEngine engine, IBootstrapperCommand bootstrapperCommand)
            : base(engine)
        {
            this.Command = bootstrapperCommand;
            this.wait = new ManualResetEvent(false);
        }

        /// <summary>
        /// Get the version of the install.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Indicates if DetectUpdate found a newer version to update.
        /// </summary>
        private bool UpdateAvailable { get; set; }

        /// <summary>
        /// UI Thread entry point for TestUX.
        /// </summary>
        protected override void OnStartup(StartupEventArgs args)
        {
            string immediatelyQuit = this.ReadPackageAction(null, "ImmediatelyQuit");
            if (!String.IsNullOrEmpty(immediatelyQuit) && Boolean.TryParse(immediatelyQuit, out this.immediatelyQuit) && this.immediatelyQuit)
            {
                this.Engine.Quit(0);
                return;
            }

            this.action = this.Command.Action;
            this.TestVariables();

            this.Version = this.engine.GetVariableVersion(BurnBundleVersionVariable);
            this.Log("Version: {0}", this.Version);

            List<string> verifyArguments = this.ReadVerifyArguments();

            IBootstrapperApplicationData baManifest = new BootstrapperApplicationData();
            IMbaCommand mbaCommand = this.Command.ParseCommandLine();
            mbaCommand.SetOverridableVariables(baManifest.Bundle.OverridableVariables, this.engine);

            foreach (string arg in mbaCommand.UnknownCommandLineArgs)
            {
                // If we're not in the update already, process the updatebundle.
                if (this.Command.Relation != RelationType.Update && arg.StartsWith("-updatebundle:", StringComparison.OrdinalIgnoreCase))
                {
                    this.updateBundlePath = arg.Substring(14);
                    FileInfo info = new FileInfo(this.updateBundlePath);
                    this.Engine.SetUpdate(this.updateBundlePath, null, info.Length, UpdateHashType.None, null);
                    this.UpdateAvailable = true;
                    this.action = LaunchAction.UpdateReplaceEmbedded;
                }
                else if (this.Command.Relation != RelationType.Update && arg.StartsWith("-checkupdate", StringComparison.OrdinalIgnoreCase))
                {
                    this.action = LaunchAction.UpdateReplace;
                }

                verifyArguments.Remove(arg);
            }
            this.Log("Action: {0}", this.action);

            // If there are any verification arguments left, error out.
            if (0 < verifyArguments.Count)
            {
                foreach (string expectedArg in verifyArguments)
                {
                    this.Log("Failure. Expected command-line to have argument: {0}", expectedArg);
                }

                this.Engine.Quit(-1);
                return;
            }

            base.OnStartup(args);

            int redetectCount;
            string redetect = this.ReadPackageAction(null, "RedetectCount");
            if (String.IsNullOrEmpty(redetect) || !Int32.TryParse(redetect, out redetectCount))
            {
                redetectCount = 0;
            }

            string allowAcquireAfterValidationFailure = this.ReadPackageAction(null, "AllowAcquireAfterValidationFailure");
            if (String.IsNullOrEmpty(allowAcquireAfterValidationFailure) || !Boolean.TryParse(allowAcquireAfterValidationFailure, out this.allowAcquireAfterValidationFailure))
            {
                this.allowAcquireAfterValidationFailure = false;
            }

            string explicitlyElevateAndPlanFromOnElevateBegin = this.ReadPackageAction(null, "ExplicitlyElevateAndPlanFromOnElevateBegin");
            if (String.IsNullOrEmpty(explicitlyElevateAndPlanFromOnElevateBegin) || !Boolean.TryParse(explicitlyElevateAndPlanFromOnElevateBegin, out this.explicitlyElevateAndPlanFromOnElevateBegin))
            {
                this.explicitlyElevateAndPlanFromOnElevateBegin = false;
            }

            string forceKeepRegistration = this.ReadPackageAction(null, "ForceKeepRegistration");
            if (String.IsNullOrEmpty(forceKeepRegistration) || !Boolean.TryParse(forceKeepRegistration, out this.forceKeepRegistration))
            {
                this.forceKeepRegistration = false;
            }

            string quitAfterDetect = this.ReadPackageAction(null, "QuitAfterDetect");
            if (String.IsNullOrEmpty(quitAfterDetect) || !Boolean.TryParse(quitAfterDetect, out this.quitAfterDetect))
            {
                this.quitAfterDetect = false;
            }

            this.ImportContainerSources();
            this.ImportPayloadSources();

            this.wait.WaitOne();

            if (this.action == LaunchAction.Help)
            {
                this.Log("This is a BA for automated testing");
                this.Engine.Quit(0);
                return;
            }

            this.redetectRemaining = redetectCount;
            for (int i = -1; i < redetectCount; i++)
            {
                this.Engine.Detect(this.windowHandle);
            }
        }

        protected override void Run()
        {
            this.dummyWindow = new Form();
            this.windowHandle = this.dummyWindow.Handle;

            this.Log("Running TestBA application");
            this.wait.Set();
            Application.Run();
        }

        private void ShutdownUiThread()
        {
            if (this.dummyWindow != null)
            {
                this.dummyWindow.Invoke(new Action(Application.ExitThread));
                this.dummyWindow.Dispose();
            }

            var exitCode = this.result;
            if ((exitCode & 0xFFFF0000) == unchecked(0x80070000))
            {
                exitCode &= 0xFFFF; // return plain old Win32 error, not HRESULT.
            }

            this.Engine.Quit(exitCode);
        }

        protected override void OnDetectUpdateBegin(DetectUpdateBeginEventArgs args)
        {
            this.Log("OnDetectUpdateBegin");
            if (LaunchAction.UpdateReplaceEmbedded == this.action || LaunchAction.UpdateReplace == this.action)
            {
                args.Skip = false;
            }
        }

        protected override void OnDetectUpdate(DetectUpdateEventArgs e)
        {
            // The list of updates is sorted in descending version, so the first callback should be the largest update available.
            // This update should be either larger than ours (so we are out of date), the same as ours (so we are current)
            // or smaller than ours (we have a private build).
            // Enumerate all of the updates anyway in case something's broken.
            this.Log(String.Format("Potential update v{0} from '{1}'; current version: v{2}", e.Version, e.UpdateLocation, this.Version));
            if (!this.UpdateAvailable && this.Engine.CompareVersions(e.Version, this.Version) > 0)
            {
                this.Log(String.Format("Selected update v{0}", e.Version));
                this.Engine.SetUpdate(null, e.UpdateLocation, e.Size, e.HashAlgorithm, e.Hash);
                this.UpdateAvailable = true;
            }
        }

        protected override void OnDetectUpdateComplete(DetectUpdateCompleteEventArgs e)
        {
            this.Log("OnDetectUpdateComplete");

            // Failed to process an update, allow the existing bundle to still install.
            if (!Hresult.Succeeded(e.Status))
            {
                this.Log(String.Format("Failed to locate an update, status of 0x{0:X8}, updates disabled.", e.Status));
                e.IgnoreError = true; // But continue on...
            }
        }

        protected override void OnDetectComplete(DetectCompleteEventArgs args)
        {
            this.result = args.Status;

            if (Hresult.Succeeded(this.result) &&
                (this.UpdateAvailable || LaunchAction.UpdateReplaceEmbedded != this.action && LaunchAction.UpdateReplace != this.action))
            {
                if (this.redetectRemaining > 0)
                {
                    this.Log("Completed detection phase: {0} re-runs remaining", this.redetectRemaining--);
                }
                else if (this.quitAfterDetect)
                {
                    this.ShutdownUiThread();
                }
                else if (this.explicitlyElevateAndPlanFromOnElevateBegin)
                {
                    this.Engine.Elevate(this.windowHandle);
                }
                else
                {
                    this.Engine.Plan(this.action);
                }
            }
            else
            {
                this.ShutdownUiThread();
            }
        }

        protected override void OnDetectRelatedBundle(DetectRelatedBundleEventArgs args)
        {
            this.Log("OnDetectRelatedBundle() - id: {0}, missing from cache: {1}", args.ProductCode, args.MissingFromCache);
        }

        protected override void OnElevateBegin(ElevateBeginEventArgs args)
        {
            if (this.explicitlyElevateAndPlanFromOnElevateBegin)
            {
                this.Engine.Plan(this.action);

                // Simulate showing some UI since these tests won't actually show the UAC prompt.
                MessagePump.ProcessMessages(10);
            }
        }

        protected override void OnPlanPackageBegin(PlanPackageBeginEventArgs args)
        {
            RequestState state;
            string action = this.ReadPackageAction(args.PackageId, "Requested");
            if (TryParseEnum<RequestState>(action, out state))
            {
                args.State = state;
            }

            BOOTSTRAPPER_CACHE_TYPE cacheType;
            string cacheAction = this.ReadPackageAction(args.PackageId, "CacheRequested");
            if (TryParseEnum<BOOTSTRAPPER_CACHE_TYPE>(cacheAction, out cacheType))
            {
                args.CacheType = cacheType;
            }

            this.Log("OnPlanPackageBegin() - id: {0}, currentState: {1}, defaultState: {2}, requestedState: {3}, defaultCache: {4}, requestedCache: {5}", args.PackageId, args.CurrentState, args.RecommendedState, args.State, args.RecommendedCacheType, args.CacheType);
        }

        protected override void OnPlanPatchTarget(PlanPatchTargetEventArgs args)
        {
            RequestState state;
            string action = this.ReadPackageAction(args.PackageId, "Requested");
            if (TryParseEnum<RequestState>(action, out state))
            {
                args.State = state;
            }
        }

        protected override void OnPlanMsiFeature(PlanMsiFeatureEventArgs args)
        {
            FeatureState state;
            string action = this.ReadFeatureAction(args.PackageId, args.FeatureId, "Requested");
            if (TryParseEnum<FeatureState>(action, out state))
            {
                args.State = state;
            }

            this.Log("OnPlanMsiFeature() - id: {0}, defaultState: {1}, requestedState: {2}", args.PackageId, args.RecommendedState, args.State);
        }

        protected override void OnPlanComplete(PlanCompleteEventArgs args)
        {
            this.result = args.Status;
            if (Hresult.Succeeded(this.result))
            {
                this.Engine.Apply(this.windowHandle);
            }
            else
            {
                this.ShutdownUiThread();
            }
        }

        protected override void OnCachePackageBegin(CachePackageBeginEventArgs args)
        {
            this.Log("OnCachePackageBegin() - package: {0}, payloads to cache: {1}", args.PackageId, args.CachePayloads);

            string slowProgress = this.ReadPackageAction(args.PackageId, "SlowCache");
            if (String.IsNullOrEmpty(slowProgress) || !Int32.TryParse(slowProgress, out this.sleepDuringCache))
            {
                this.sleepDuringCache = 0;
            }
            else
            {
                this.Log("    SlowCache: {0}", this.sleepDuringCache);
            }

            string cancelCache = this.ReadPackageAction(args.PackageId, "CancelCacheAtProgress");
            if (String.IsNullOrEmpty(cancelCache) || !Int32.TryParse(cancelCache, out this.cancelCacheAtProgress))
            {
                this.cancelCacheAtProgress = -1;
            }
            else
            {
                this.Log("    CancelCacheAtProgress: {0}", this.cancelCacheAtProgress);
            }
        }

        protected override void OnCachePackageNonVitalValidationFailure(CachePackageNonVitalValidationFailureEventArgs args)
        {
            if (this.allowAcquireAfterValidationFailure)
            {
                args.Action = BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION.Acquire;
            }

            this.Log("OnCachePackageNonVitalValidationFailure() - id: {0}, default: {1}, requested: {2}", args.PackageId, args.Recommendation, args.Action);
        }

        protected override void OnCacheAcquireProgress(CacheAcquireProgressEventArgs args)
        {
            this.Log("OnCacheAcquireProgress() - container/package: {0}, payload: {1}, progress: {2}, total: {3}, overall progress: {4}%", args.PackageOrContainerId, args.PayloadId, args.Progress, args.Total, args.OverallPercentage);

            if (this.cancelCacheAtProgress >= 0 && this.cancelCacheAtProgress <= args.Progress)
            {
                args.Cancel = true;
                this.Log("OnCacheAcquireProgress(cancel)");
            }
            else if (this.sleepDuringCache > 0)
            {
                this.Log("OnCacheAcquireProgress(sleep {0})", this.sleepDuringCache);
                Thread.Sleep(this.sleepDuringCache);
            }
        }

        protected override void OnCacheContainerOrPayloadVerifyProgress(CacheContainerOrPayloadVerifyProgressEventArgs args)
        {
            this.Log("OnCacheContainerOrPayloadVerifyProgress() - container/package: {0}, payload: {1}, progress: {2}, total: {3}, overall progress: {4}%", args.PackageOrContainerId, args.PayloadId, args.Progress, args.Total, args.OverallPercentage);
        }

        protected override void OnCachePayloadExtractProgress(CachePayloadExtractProgressEventArgs args)
        {
            this.Log("OnCachePayloadExtractProgress() - container/package: {0}, payload: {1}, progress: {2}, total: {3}, overall progress: {4}%", args.PackageOrContainerId, args.PayloadId, args.Progress, args.Total, args.OverallPercentage);
        }

        protected override void OnCacheVerifyProgress(CacheVerifyProgressEventArgs args)
        {
            this.Log("OnCacheVerifyProgress() - container/package: {0}, payload: {1}, progress: {2}, total: {3}, overall progress: {4}%, step: {5}", args.PackageOrContainerId, args.PayloadId, args.Progress, args.Total, args.OverallPercentage, args.Step);
        }

        protected override void OnExecutePackageBegin(ExecutePackageBeginEventArgs args)
        {
            this.Log("OnExecutePackageBegin() - package: {0}, rollback: {1}", args.PackageId, !args.ShouldExecute);

            this.rollingBack = !args.ShouldExecute;

            string slowProgress = this.ReadPackageAction(args.PackageId, "SlowExecute");
            if (String.IsNullOrEmpty(slowProgress) || !Int32.TryParse(slowProgress, out this.sleepDuringExecute))
            {
                this.sleepDuringExecute = 0;
            }
            else
            {
                this.Log("    SlowExecute: {0}", this.sleepDuringExecute);
            }

            string cancelExecute = this.ReadPackageAction(args.PackageId, "CancelExecuteAtProgress");
            if (String.IsNullOrEmpty(cancelExecute) || !Int32.TryParse(cancelExecute, out this.cancelExecuteAtProgress))
            {
                this.cancelExecuteAtProgress = -1;
            }
            else
            {
                this.Log("    CancelExecuteAtProgress: {0}", this.cancelExecuteAtProgress);
            }

            this.cancelExecuteActionName = this.ReadPackageAction(args.PackageId, "CancelExecuteAtActionStart");
            if (!String.IsNullOrEmpty(this.cancelExecuteActionName))
            {
                this.Log("    CancelExecuteAtActionState: {0}", this.cancelExecuteActionName);
            }

            string cancelOnProgressAtProgress = this.ReadPackageAction(args.PackageId, "CancelOnProgressAtProgress");
            if (String.IsNullOrEmpty(cancelOnProgressAtProgress) || !Int32.TryParse(cancelOnProgressAtProgress, out this.cancelOnProgressAtProgress))
            {
                this.cancelOnProgressAtProgress = -1;
            }
            else
            {
                this.Log("    CancelOnProgressAtProgress: {0}", this.cancelOnProgressAtProgress);
            }

            string retryBeforeCancel = this.ReadPackageAction(args.PackageId, "RetryExecuteFilesInUse");
            if (String.IsNullOrEmpty(retryBeforeCancel) || !Int32.TryParse(retryBeforeCancel, out this.retryExecuteFilesInUse))
            {
                this.retryExecuteFilesInUse = 0;
            }
            else
            {
                this.Log("    RetryExecuteFilesInUse: {0}", this.retryExecuteFilesInUse);
            }
        }

        protected override void OnExecutePackageComplete(ExecutePackageCompleteEventArgs args)
        {
            bool logTestRegistryValue;
            string recordTestRegistryValue = this.ReadPackageAction(args.PackageId, "RecordTestRegistryValue");
            if (!String.IsNullOrEmpty(recordTestRegistryValue) && Boolean.TryParse(recordTestRegistryValue, out logTestRegistryValue) && logTestRegistryValue)
            {
                var value = this.ReadTestRegistryValue(args.PackageId);
                this.Log("TestRegistryValue: {0}, {1}, Version, '{2}'", this.rollingBack ? "Rollback" : "Execute", args.PackageId, value);
            }
        }

        protected override void OnExecuteProcessCancel(ExecuteProcessCancelEventArgs args)
        {
            BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION action;
            string actionValue = this.ReadPackageAction(args.PackageId, "ProcessCancelAction");
            if (actionValue != null && TryParseEnum<BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION>(actionValue, out action))
            {
                args.Action = action;
            }

            if (args.Action == BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION.Abandon)
            {
                // Kill process to make sure it doesn't affect other tests.
                try
                {
                    using (Process process = Process.GetProcessById(args.ProcessId))
                    {
                        if (process != null)
                        {
                            process.Kill();
                        }
                    }
                }
                catch (Exception e)
                {
                    this.Log("Failed to kill process {0}: {1}", args.ProcessId, e);
                    Thread.Sleep(5000);
                }
            }

            this.Log("OnExecuteProcessCancel({0})", args.Action);
        }

        protected override void OnExecuteFilesInUse(ExecuteFilesInUseEventArgs args)
        {
            this.Log("OnExecuteFilesInUse() - package: {0}, source: {1}, retries remaining: {2}, data: {3}", args.PackageId, args.Source, this.retryExecuteFilesInUse, String.Join(", ", args.Files.ToArray()));

            if (this.retryExecuteFilesInUse > 0)
            {
                --this.retryExecuteFilesInUse;
                args.Result = Result.Retry;
            }
            else
            {
                args.Result = Result.Cancel;
            }
        }

        protected override void OnExecuteMsiMessage(ExecuteMsiMessageEventArgs args)
        {
            this.Log("OnExecuteMsiMessage() - MessageType: {0}, Message: {1}, Data: '{2}'", args.MessageType, args.Message, String.Join("','", args.Data.ToArray()));

            if (!String.IsNullOrEmpty(this.cancelExecuteActionName) && args.MessageType == InstallMessage.ActionStart &&
                args.Data.Count > 0 && args.Data[0] == this.cancelExecuteActionName)
            {
                this.Log("OnExecuteMsiMessage(cancelNextProgress)");
                this.cancelExecuteAtProgress = 0;
            }
        }

        protected override void OnExecuteProgress(ExecuteProgressEventArgs args)
        {
            this.Log("OnExecuteProgress() - package: {0}, progress: {1}%, overall progress: {2}%", args.PackageId, args.ProgressPercentage, args.OverallPercentage);

            if (this.cancelExecuteAtProgress >= 0 && this.cancelExecuteAtProgress <= args.ProgressPercentage)
            {
                args.Cancel = true;
                this.Log("OnExecuteProgress(cancel)");
            }
            else if (this.sleepDuringExecute > 0)
            {
                this.Log("OnExecuteProgress(sleep {0})", this.sleepDuringExecute);
                Thread.Sleep(this.sleepDuringExecute);
            }
        }

        protected override void OnExecutePatchTarget(ExecutePatchTargetEventArgs args)
        {
            this.Log("OnExecutePatchTarget - Patch Package: {0}, Target Product Code: {1}", args.PackageId, args.TargetProductCode);
        }

        protected override void OnProgress(ProgressEventArgs args)
        {
            this.Log("OnProgress() - progress: {0}%, overall progress: {1}%", args.ProgressPercentage, args.OverallPercentage);
            if (this.Command.Display == Display.Embedded)
            {
                this.Engine.SendEmbeddedProgress(args.ProgressPercentage, args.OverallPercentage);
            }

            if (this.cancelOnProgressAtProgress >= 0 && this.cancelOnProgressAtProgress <= args.OverallPercentage)
            {
                args.Cancel = true;
                this.Log("OnProgress(cancel)");
            }
        }

        protected override void OnApplyBegin(ApplyBeginEventArgs args)
        {
            this.cancelOnProgressAtProgress = -1;
            this.cancelExecuteAtProgress = -1;
            this.cancelCacheAtProgress = -1;
            this.rollingBack = false;
        }

        protected override void OnApplyComplete(ApplyCompleteEventArgs args)
        {
            // Output what the privileges are now.
            this.Log("After elevation: WixBundleElevated = {0}", this.Engine.GetVariableNumeric("WixBundleElevated"));

            this.result = args.Status;
            this.ShutdownUiThread();
        }

        protected override void OnUnregisterBegin(UnregisterBeginEventArgs args)
        {
            if (this.forceKeepRegistration && args.RegistrationType == RegistrationType.None)
            {
                args.RegistrationType = RegistrationType.InProgress;
            }

            this.Log("OnUnregisterBegin, default: {0}, requested: {1}", args.RecommendedRegistrationType, args.RegistrationType);
        }

        private void TestVariables()
        {
            // First make sure we can check and get standard variables of each type.
            if (this.Engine.ContainsVariable("WindowsFolder"))
            {
                string value = this.Engine.GetVariableString("WindowsFolder");
                this.Engine.Log(LogLevel.Verbose, String.Format("TEST: Successfully retrieved a string variable: WindowsFolder '{0}'", value));
            }
            else
            {
                throw new Exception("Engine did not define a standard variable: WindowsFolder");
            }

            if (this.Engine.ContainsVariable("NTProductType"))
            {
                long value = this.Engine.GetVariableNumeric("NTProductType");
                this.Engine.Log(LogLevel.Verbose, String.Format("TEST: Successfully retrieved a numeric variable: NTProductType '{0}'", value));
            }
            else
            {
                throw new Exception("Engine did not define a standard variable: NTProductType");
            }

            if (this.Engine.ContainsVariable("VersionMsi"))
            {
                string value = this.Engine.GetVariableVersion("VersionMsi");
                this.Engine.Log(LogLevel.Verbose, String.Format("TEST: Successfully retrieved a version variable: VersionMsi '{0}'", value));
            }
            else
            {
                throw new Exception("Engine did not define a standard variable: VersionMsi");
            }

            // Now validate that Contians returns false for non-existant variables of each type.
            if (this.Engine.ContainsVariable("TestStringVariableShouldNotExist"))
            {
                throw new Exception("Engine defined a variable that should not exist: TestStringVariableShouldNotExist");
            }
            else
            {
                this.Engine.Log(LogLevel.Verbose, "TEST: Successfully checked for non-existent string variable: TestStringVariableShouldNotExist");
            }

            if (this.Engine.ContainsVariable("TestNumericVariableShouldNotExist"))
            {
                throw new Exception("Engine defined a variable that should not exist: TestNumericVariableShouldNotExist");
            }
            else
            {
                this.Engine.Log(LogLevel.Verbose, "TEST: Successfully checked for non-existent numeric variable: TestNumericVariableShouldNotExist");
            }

            if (this.Engine.ContainsVariable("TestVersionVariableShouldNotExist"))
            {
                throw new Exception("Engine defined a variable that should not exist: TestVersionVariableShouldNotExist");
            }
            else
            {
                this.Engine.Log(LogLevel.Verbose, "TEST: Successfully checked for non-existent version variable: TestVersionVariableShouldNotExist");
            }

            // Output what the initially run privileges were.
            this.Engine.Log(LogLevel.Verbose, String.Format("TEST: WixBundleElevated = {0}", this.Engine.GetVariableNumeric("WixBundleElevated")));
        }

        private void Log(string format, params object[] args)
        {
            string relation = this.Command.Relation != RelationType.None ? String.Concat(" (", this.Command.Relation.ToString().ToLowerInvariant(), ")") : String.Empty;
            string message = String.Format(format, args);

            this.Engine.Log(LogLevel.Standard, String.Concat("TESTBA", relation, ": ", message));
        }

        private void ImportContainerSources()
        {
            string testName = this.Engine.GetVariableString("TestGroupName");
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\TestBAControl\{0}\container", testName)))
            {
                if (testKey == null)
                {
                    return;
                }

                foreach (var containerId in testKey.GetSubKeyNames())
                {
                    using (RegistryKey subkey = testKey.OpenSubKey(containerId))
                    {
                        string initialSource = subkey == null ? null : subkey.GetValue("InitialLocalSource") as string;
                        if (initialSource != null)
                        {
                            this.Engine.SetLocalSource(containerId, null, initialSource);
                        }
                    }
                }
            }
        }

        private void ImportPayloadSources()
        {
            string testName = this.Engine.GetVariableString("TestGroupName");
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\TestBAControl\{0}\payload", testName)))
            {
                if (testKey == null)
                {
                    return;
                }

                foreach (var payloadId in testKey.GetSubKeyNames())
                {
                    using (RegistryKey subkey = testKey.OpenSubKey(payloadId))
                    {
                        string initialSource = subkey == null ? null : subkey.GetValue("InitialLocalSource") as string;
                        if (initialSource != null)
                        {
                            this.Engine.SetLocalSource(null, payloadId, initialSource);
                        }
                    }
                }
            }
        }

        private List<string> ReadVerifyArguments()
        {
            string testName = this.Engine.GetVariableString("TestGroupName");
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\TestBAControl\{0}", testName)))
            {
                string verifyArguments = testKey == null ? null : testKey.GetValue("VerifyArguments") as string;
                return verifyArguments == null ? new List<string>() : new List<string>(verifyArguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        private string ReadPackageAction(string packageId, string state)
        {
            string testName = this.Engine.GetVariableString("TestGroupName");
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\TestBAControl\{0}\{1}", testName, String.IsNullOrEmpty(packageId) ? String.Empty : packageId)))
            {
                return testKey == null ? null : testKey.GetValue(state) as string;
            }
        }

        private string ReadFeatureAction(string packageId, string featureId, string state)
        {
            string testName = this.Engine.GetVariableString("TestGroupName");
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\TestBAControl\{0}\{1}", testName, packageId)))
            {
                string registryName = String.Concat(featureId, state);
                return testKey == null ? null : testKey.GetValue(registryName) as string;
            }
        }

        private string ReadTestRegistryValue(string name)
        {
            string testName = this.Engine.GetVariableString("TestGroupName");
            using (RegistryKey testKey = Registry.LocalMachine.OpenSubKey(String.Format(@"Software\WiX\Tests\{0}\{1}", testName, name)))
            {
                return testKey == null ? null : testKey.GetValue("Version") as string;
            }
        }

        private static bool TryParseEnum<T>(string value, out T t)
        {
            try
            {
                t = (T)Enum.Parse(typeof(T), value, true);
                return true;
            }
            catch (ArgumentException) { }
            catch (OverflowException) { }

            t = default(T);
            return false;
        }
    }
}
