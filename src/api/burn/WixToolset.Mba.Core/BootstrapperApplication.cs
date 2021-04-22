// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// The default bootstrapper application.
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    public abstract class BootstrapperApplication : MarshalByRefObject, IDefaultBootstrapperApplication
    {
        /// <summary>
        /// Specifies whether this bootstrapper should run asynchronously. The default is true.
        /// </summary>
        protected readonly bool asyncExecution;

        /// <summary>
        /// Gets the <see cref="IEngine"/> for interaction with the engine.
        /// </summary>
        protected readonly IEngine engine;

        private bool applying;

        /// <summary>
        /// Creates a new instance of the <see cref="BootstrapperApplication"/> class.
        /// </summary>
        protected BootstrapperApplication(IEngine engine)
        {
            this.engine = engine;
            this.applying = false;
            this.asyncExecution = true;
        }

        /// <inheritdoc/>
        public event EventHandler<StartupEventArgs> Startup;

        /// <inheritdoc/>
        public event EventHandler<ShutdownEventArgs> Shutdown;

        /// <inheritdoc/>
        public event EventHandler<SystemShutdownEventArgs> SystemShutdown;

        /// <inheritdoc/>
        public event EventHandler<DetectBeginEventArgs> DetectBegin;

        /// <inheritdoc/>
        public event EventHandler<DetectForwardCompatibleBundleEventArgs> DetectForwardCompatibleBundle;

        /// <inheritdoc/>
        public event EventHandler<DetectUpdateBeginEventArgs> DetectUpdateBegin;

        /// <inheritdoc/>
        public event EventHandler<DetectUpdateEventArgs> DetectUpdate;

        /// <inheritdoc/>
        public event EventHandler<DetectUpdateCompleteEventArgs> DetectUpdateComplete;

        /// <inheritdoc/>
        public event EventHandler<DetectRelatedBundleEventArgs> DetectRelatedBundle;

        /// <inheritdoc/>
        public event EventHandler<DetectPackageBeginEventArgs> DetectPackageBegin;
        
        /// <inheritdoc/>
        public event EventHandler<DetectRelatedMsiPackageEventArgs> DetectRelatedMsiPackage;

        /// <inheritdoc/>
        public event EventHandler<DetectPatchTargetEventArgs> DetectPatchTarget;

        /// <inheritdoc/>
        public event EventHandler<DetectMsiFeatureEventArgs> DetectMsiFeature;

        /// <inheritdoc/>
        public event EventHandler<DetectPackageCompleteEventArgs> DetectPackageComplete;

        /// <inheritdoc/>
        public event EventHandler<DetectCompleteEventArgs> DetectComplete;

        /// <inheritdoc/>
        public event EventHandler<PlanBeginEventArgs> PlanBegin;

        /// <inheritdoc/>
        public event EventHandler<PlanRelatedBundleEventArgs> PlanRelatedBundle;

        /// <inheritdoc/>
        public event EventHandler<PlanPackageBeginEventArgs> PlanPackageBegin;

        /// <inheritdoc/>
        public event EventHandler<PlanPatchTargetEventArgs> PlanPatchTarget;

        /// <inheritdoc/>
        public event EventHandler<PlanMsiFeatureEventArgs> PlanMsiFeature;

        /// <inheritdoc/>
        public event EventHandler<PlanMsiPackageEventArgs> PlanMsiPackage;

        /// <inheritdoc/>
        public event EventHandler<PlanPackageCompleteEventArgs> PlanPackageComplete;

        /// <inheritdoc/>
        public event EventHandler<PlannedPackageEventArgs> PlannedPackage;

        /// <inheritdoc/>
        public event EventHandler<PlanCompleteEventArgs> PlanComplete;

        /// <inheritdoc/>
        public event EventHandler<ApplyBeginEventArgs> ApplyBegin;

        /// <inheritdoc/>
        public event EventHandler<ElevateBeginEventArgs> ElevateBegin;

        /// <inheritdoc/>
        public event EventHandler<ElevateCompleteEventArgs> ElevateComplete;

        /// <inheritdoc/>
        public event EventHandler<ProgressEventArgs> Progress;

        /// <inheritdoc/>
        public event EventHandler<ErrorEventArgs> Error;

        /// <inheritdoc/>
        public event EventHandler<RegisterBeginEventArgs> RegisterBegin;

        /// <inheritdoc/>
        public event EventHandler<RegisterCompleteEventArgs> RegisterComplete;

        /// <inheritdoc/>
        public event EventHandler<UnregisterBeginEventArgs> UnregisterBegin;

        /// <inheritdoc/>
        public event EventHandler<UnregisterCompleteEventArgs> UnregisterComplete;

        /// <inheritdoc/>
        public event EventHandler<CacheBeginEventArgs> CacheBegin;

        /// <inheritdoc/>
        public event EventHandler<CachePackageBeginEventArgs> CachePackageBegin;

        /// <inheritdoc/>
        public event EventHandler<CacheAcquireBeginEventArgs> CacheAcquireBegin;

        /// <inheritdoc/>
        public event EventHandler<CacheAcquireProgressEventArgs> CacheAcquireProgress;

        /// <inheritdoc/>
        public event EventHandler<CacheAcquireResolvingEventArgs> CacheAcquireResolving;

        /// <inheritdoc/>
        public event EventHandler<CacheAcquireCompleteEventArgs> CacheAcquireComplete;

        /// <inheritdoc/>
        public event EventHandler<CacheVerifyBeginEventArgs> CacheVerifyBegin;

        /// <inheritdoc/>
        public event EventHandler<CacheVerifyProgressEventArgs> CacheVerifyProgress;

        /// <inheritdoc/>
        public event EventHandler<CacheVerifyCompleteEventArgs> CacheVerifyComplete;

        /// <inheritdoc/>
        public event EventHandler<CachePackageCompleteEventArgs> CachePackageComplete;

        /// <inheritdoc/>
        public event EventHandler<CacheCompleteEventArgs> CacheComplete;

        /// <inheritdoc/>
        public event EventHandler<ExecuteBeginEventArgs> ExecuteBegin;

        /// <inheritdoc/>
        public event EventHandler<ExecutePackageBeginEventArgs> ExecutePackageBegin;

        /// <inheritdoc/>
        public event EventHandler<ExecutePatchTargetEventArgs> ExecutePatchTarget;

        /// <inheritdoc/>
        public event EventHandler<ExecuteMsiMessageEventArgs> ExecuteMsiMessage;

        /// <inheritdoc/>
        public event EventHandler<ExecuteFilesInUseEventArgs> ExecuteFilesInUse;

        /// <inheritdoc/>
        public event EventHandler<ExecutePackageCompleteEventArgs> ExecutePackageComplete;

        /// <inheritdoc/>
        public event EventHandler<ExecuteCompleteEventArgs> ExecuteComplete;

        /// <inheritdoc/>
        public event EventHandler<ApplyCompleteEventArgs> ApplyComplete;

        /// <inheritdoc/>
        public event EventHandler<ExecuteProgressEventArgs> ExecuteProgress;

        /// <inheritdoc/>
        public event EventHandler<LaunchApprovedExeBeginEventArgs> LaunchApprovedExeBegin;

        /// <inheritdoc/>
        public event EventHandler<LaunchApprovedExeCompleteEventArgs> LaunchApprovedExeComplete;

        /// <inheritdoc/>
        public event EventHandler<BeginMsiTransactionBeginEventArgs> BeginMsiTransactionBegin;

        /// <inheritdoc/>
        public event EventHandler<BeginMsiTransactionCompleteEventArgs> BeginMsiTransactionComplete;

        /// <inheritdoc/>
        public event EventHandler<CommitMsiTransactionBeginEventArgs> CommitMsiTransactionBegin;

        /// <inheritdoc/>
        public event EventHandler<CommitMsiTransactionCompleteEventArgs> CommitMsiTransactionComplete;

        /// <inheritdoc/>
        public event EventHandler<RollbackMsiTransactionBeginEventArgs> RollbackMsiTransactionBegin;

        /// <inheritdoc/>
        public event EventHandler<RollbackMsiTransactionCompleteEventArgs> RollbackMsiTransactionComplete;

        /// <inheritdoc/>
        public event EventHandler<PauseAutomaticUpdatesBeginEventArgs> PauseAutomaticUpdatesBegin;

        /// <inheritdoc/>
        public event EventHandler<PauseAutomaticUpdatesCompleteEventArgs> PauseAutomaticUpdatesComplete;

        /// <inheritdoc/>
        public event EventHandler<SystemRestorePointBeginEventArgs> SystemRestorePointBegin;

        /// <inheritdoc/>
        public event EventHandler<SystemRestorePointCompleteEventArgs> SystemRestorePointComplete;

        /// <inheritdoc/>
        public event EventHandler<PlanForwardCompatibleBundleEventArgs> PlanForwardCompatibleBundle;

        /// <inheritdoc/>
        public event EventHandler<CacheContainerOrPayloadVerifyBeginEventArgs> CacheContainerOrPayloadVerifyBegin;

        /// <inheritdoc/>
        public event EventHandler<CacheContainerOrPayloadVerifyProgressEventArgs> CacheContainerOrPayloadVerifyProgress;

        /// <inheritdoc/>
        public event EventHandler<CacheContainerOrPayloadVerifyCompleteEventArgs> CacheContainerOrPayloadVerifyComplete;

        /// <inheritdoc/>
        public event EventHandler<CachePayloadExtractBeginEventArgs> CachePayloadExtractBegin;

        /// <inheritdoc/>
        public event EventHandler<CachePayloadExtractProgressEventArgs> CachePayloadExtractProgress;

        /// <inheritdoc/>
        public event EventHandler<CachePayloadExtractCompleteEventArgs> CachePayloadExtractComplete;

        /// <summary>
        /// Entry point that is called when the bootstrapper application is ready to run.
        /// </summary>
        protected abstract void Run();

        /// <summary>
        /// Called by the engine, raises the <see cref="Startup"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnStartup(StartupEventArgs args)
        {
            EventHandler<StartupEventArgs> handler = this.Startup;
            if (null != handler)
            {
                handler(this, args);
            }

            if (this.asyncExecution)
            {
                this.engine.Log(LogLevel.Verbose, "Creating BA thread to run asynchronously.");
                Thread uiThread = new Thread(this.Run);
                uiThread.Name = "UIThread";
                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
            }
            else
            {
                this.engine.Log(LogLevel.Verbose, "Creating BA thread to run synchronously.");
                this.Run();
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="Shutdown"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnShutdown(ShutdownEventArgs args)
        {
            EventHandler<ShutdownEventArgs> handler = this.Shutdown;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="SystemShutdown"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnSystemShutdown(SystemShutdownEventArgs args)
        {
            EventHandler<SystemShutdownEventArgs> handler = this.SystemShutdown;
            if (null != handler)
            {
                handler(this, args);
            }
            else if (null != args)
            {
                // Allow requests to shut down when critical or not applying.
                bool critical = EndSessionReasons.Critical == (EndSessionReasons.Critical & args.Reasons);
                args.Cancel = !critical && this.applying;
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectBegin(DetectBeginEventArgs args)
        {
            EventHandler<DetectBeginEventArgs> handler = this.DetectBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectForwardCompatibleBundle"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectForwardCompatibleBundle(DetectForwardCompatibleBundleEventArgs args)
        {
            EventHandler<DetectForwardCompatibleBundleEventArgs> handler = this.DetectForwardCompatibleBundle;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectUpdateBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectUpdateBegin(DetectUpdateBeginEventArgs args)
        {
            EventHandler<DetectUpdateBeginEventArgs> handler = this.DetectUpdateBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectUpdate"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectUpdate(DetectUpdateEventArgs args)
        {
            EventHandler<DetectUpdateEventArgs> handler = this.DetectUpdate;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectUpdateComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectUpdateComplete(DetectUpdateCompleteEventArgs args)
        {
            EventHandler<DetectUpdateCompleteEventArgs> handler = this.DetectUpdateComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectRelatedBundle"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectRelatedBundle(DetectRelatedBundleEventArgs args)
        {
            EventHandler<DetectRelatedBundleEventArgs> handler = this.DetectRelatedBundle;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectPackageBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectPackageBegin(DetectPackageBeginEventArgs args)
        {
            EventHandler<DetectPackageBeginEventArgs> handler = this.DetectPackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectRelatedMsiPackage"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectRelatedMsiPackage(DetectRelatedMsiPackageEventArgs args)
        {
            EventHandler<DetectRelatedMsiPackageEventArgs> handler = this.DetectRelatedMsiPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectPatchTarget"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectPatchTarget(DetectPatchTargetEventArgs args)
        {
            EventHandler<DetectPatchTargetEventArgs> handler = this.DetectPatchTarget;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectMsiFeature"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectMsiFeature(DetectMsiFeatureEventArgs args)
        {
            EventHandler<DetectMsiFeatureEventArgs> handler = this.DetectMsiFeature;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectPackageComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectPackageComplete(DetectPackageCompleteEventArgs args)
        {
            EventHandler<DetectPackageCompleteEventArgs> handler = this.DetectPackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="DetectComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectComplete(DetectCompleteEventArgs args)
        {
            EventHandler<DetectCompleteEventArgs> handler = this.DetectComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanBegin(PlanBeginEventArgs args)
        {
            EventHandler<PlanBeginEventArgs> handler = this.PlanBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanRelatedBundle"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanRelatedBundle(PlanRelatedBundleEventArgs args)
        {
            EventHandler<PlanRelatedBundleEventArgs> handler = this.PlanRelatedBundle;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanPackageBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanPackageBegin(PlanPackageBeginEventArgs args)
        {
            EventHandler<PlanPackageBeginEventArgs> handler = this.PlanPackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanPatchTarget"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanPatchTarget(PlanPatchTargetEventArgs args)
        {
            EventHandler<PlanPatchTargetEventArgs> handler = this.PlanPatchTarget;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanMsiFeature"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanMsiFeature(PlanMsiFeatureEventArgs args)
        {
            EventHandler<PlanMsiFeatureEventArgs> handler = this.PlanMsiFeature;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanMsiPackage"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanMsiPackage(PlanMsiPackageEventArgs args)
        {
            EventHandler<PlanMsiPackageEventArgs> handler = this.PlanMsiPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanPackageComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanPackageComplete(PlanPackageCompleteEventArgs args)
        {
            EventHandler<PlanPackageCompleteEventArgs> handler = this.PlanPackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlannedPackage"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlannedPackage(PlannedPackageEventArgs args)
        {
            EventHandler<PlannedPackageEventArgs> handler = this.PlannedPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanComplete(PlanCompleteEventArgs args)
        {
            EventHandler<PlanCompleteEventArgs> handler = this.PlanComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ApplyBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnApplyBegin(ApplyBeginEventArgs args)
        {
            EventHandler<ApplyBeginEventArgs> handler = this.ApplyBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ElevateBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnElevateBegin(ElevateBeginEventArgs args)
        {
            EventHandler<ElevateBeginEventArgs> handler = this.ElevateBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ElevateComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnElevateComplete(ElevateCompleteEventArgs args)
        {
            EventHandler<ElevateCompleteEventArgs> handler = this.ElevateComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="Progress"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnProgress(ProgressEventArgs args)
        {
            EventHandler<ProgressEventArgs> handler = this.Progress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="Error"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnError(ErrorEventArgs args)
        {
            EventHandler<ErrorEventArgs> handler = this.Error;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="RegisterBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnRegisterBegin(RegisterBeginEventArgs args)
        {
            EventHandler<RegisterBeginEventArgs> handler = this.RegisterBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="RegisterComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnRegisterComplete(RegisterCompleteEventArgs args)
        {
            EventHandler<RegisterCompleteEventArgs> handler = this.RegisterComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="UnregisterBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnUnregisterBegin(UnregisterBeginEventArgs args)
        {
            EventHandler<UnregisterBeginEventArgs> handler = this.UnregisterBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="UnregisterComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnUnregisterComplete(UnregisterCompleteEventArgs args)
        {
            EventHandler<UnregisterCompleteEventArgs> handler = this.UnregisterComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheBegin"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheBegin(CacheBeginEventArgs args)
        {
            EventHandler<CacheBeginEventArgs> handler = this.CacheBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CachePackageBegin"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCachePackageBegin(CachePackageBeginEventArgs args)
        {
            EventHandler<CachePackageBeginEventArgs> handler = this.CachePackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheAcquireBegin"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheAcquireBegin(CacheAcquireBeginEventArgs args)
        {
            EventHandler<CacheAcquireBeginEventArgs> handler = this.CacheAcquireBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheAcquireProgress"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheAcquireProgress(CacheAcquireProgressEventArgs args)
        {
            EventHandler<CacheAcquireProgressEventArgs> handler = this.CacheAcquireProgress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheAcquireResolving"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnCacheAcquireResolving(CacheAcquireResolvingEventArgs args)
        {
            EventHandler<CacheAcquireResolvingEventArgs> handler = this.CacheAcquireResolving;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheAcquireComplete"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheAcquireComplete(CacheAcquireCompleteEventArgs args)
        {
            EventHandler<CacheAcquireCompleteEventArgs> handler = this.CacheAcquireComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheVerifyBegin"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheVerifyBegin(CacheVerifyBeginEventArgs args)
        {
            EventHandler<CacheVerifyBeginEventArgs> handler = this.CacheVerifyBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheVerifyProgress"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheVerifyProgress(CacheVerifyProgressEventArgs args)
        {
            EventHandler<CacheVerifyProgressEventArgs> handler = this.CacheVerifyProgress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheVerifyComplete"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheVerifyComplete(CacheVerifyCompleteEventArgs args)
        {
            EventHandler<CacheVerifyCompleteEventArgs> handler = this.CacheVerifyComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CachePackageComplete"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCachePackageComplete(CachePackageCompleteEventArgs args)
        {
            EventHandler<CachePackageCompleteEventArgs> handler = this.CachePackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnCacheComplete(CacheCompleteEventArgs args)
        {
            EventHandler<CacheCompleteEventArgs> handler = this.CacheComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ExecuteBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteBegin(ExecuteBeginEventArgs args)
        {
            EventHandler<ExecuteBeginEventArgs> handler = this.ExecuteBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ExecutePackageBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecutePackageBegin(ExecutePackageBeginEventArgs args)
        {
            EventHandler<ExecutePackageBeginEventArgs> handler = this.ExecutePackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ExecutePatchTarget"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecutePatchTarget(ExecutePatchTargetEventArgs args)
        {
            EventHandler<ExecutePatchTargetEventArgs> handler = this.ExecutePatchTarget;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ExecuteMsiMessage"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteMsiMessage(ExecuteMsiMessageEventArgs args)
        {
            EventHandler<ExecuteMsiMessageEventArgs> handler = this.ExecuteMsiMessage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ExecuteFilesInUse"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteFilesInUse(ExecuteFilesInUseEventArgs args)
        {
            EventHandler<ExecuteFilesInUseEventArgs> handler = this.ExecuteFilesInUse;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ExecutePackageComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecutePackageComplete(ExecutePackageCompleteEventArgs args)
        {
            EventHandler<ExecutePackageCompleteEventArgs> handler = this.ExecutePackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ExecuteComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteComplete(ExecuteCompleteEventArgs args)
        {
            EventHandler<ExecuteCompleteEventArgs> handler = this.ExecuteComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ApplyComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnApplyComplete(ApplyCompleteEventArgs args)
        {
            EventHandler<ApplyCompleteEventArgs> handler = this.ApplyComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="ExecuteProgress"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteProgress(ExecuteProgressEventArgs args)
        {
            EventHandler<ExecuteProgressEventArgs> handler = this.ExecuteProgress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="LaunchApprovedExeBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnLaunchApprovedExeBegin(LaunchApprovedExeBeginEventArgs args)
        {
            EventHandler<LaunchApprovedExeBeginEventArgs> handler = this.LaunchApprovedExeBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="LaunchApprovedExeComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnLaunchApprovedExeComplete(LaunchApprovedExeCompleteEventArgs args)
        {
            EventHandler<LaunchApprovedExeCompleteEventArgs> handler = this.LaunchApprovedExeComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="BeginMsiTransactionBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnBeginMsiTransactionBegin(BeginMsiTransactionBeginEventArgs args)
        {
            EventHandler<BeginMsiTransactionBeginEventArgs> handler = this.BeginMsiTransactionBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="BeginMsiTransactionComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnBeginMsiTransactionComplete(BeginMsiTransactionCompleteEventArgs args)
        {
            EventHandler<BeginMsiTransactionCompleteEventArgs> handler = this.BeginMsiTransactionComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CommitMsiTransactionBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnCommitMsiTransactionBegin(CommitMsiTransactionBeginEventArgs args)
        {
            EventHandler<CommitMsiTransactionBeginEventArgs> handler = this.CommitMsiTransactionBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CommitMsiTransactionComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnCommitMsiTransactionComplete(CommitMsiTransactionCompleteEventArgs args)
        {
            EventHandler<CommitMsiTransactionCompleteEventArgs> handler = this.CommitMsiTransactionComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="RollbackMsiTransactionBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnRollbackMsiTransactionBegin(RollbackMsiTransactionBeginEventArgs args)
        {
            EventHandler<RollbackMsiTransactionBeginEventArgs> handler = this.RollbackMsiTransactionBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="RollbackMsiTransactionComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnRollbackMsiTransactionComplete(RollbackMsiTransactionCompleteEventArgs args)
        {
            EventHandler<RollbackMsiTransactionCompleteEventArgs> handler = this.RollbackMsiTransactionComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PauseAutomaticUpdatesBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPauseAutomaticUpdatesBegin(PauseAutomaticUpdatesBeginEventArgs args)
        {
            EventHandler<PauseAutomaticUpdatesBeginEventArgs> handler = this.PauseAutomaticUpdatesBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PauseAutomaticUpdatesComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPauseAutomaticUpdatesComplete(PauseAutomaticUpdatesCompleteEventArgs args)
        {
            EventHandler<PauseAutomaticUpdatesCompleteEventArgs> handler = this.PauseAutomaticUpdatesComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="SystemRestorePointBegin"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnSystemRestorePointBegin(SystemRestorePointBeginEventArgs args)
        {
            EventHandler<SystemRestorePointBeginEventArgs> handler = this.SystemRestorePointBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="SystemRestorePointComplete"/> event.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnSystemRestorePointComplete(SystemRestorePointCompleteEventArgs args)
        {
            EventHandler<SystemRestorePointCompleteEventArgs> handler = this.SystemRestorePointComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="PlanForwardCompatibleBundle"/> event.
        /// </summary>
        protected virtual void OnPlanForwardCompatibleBundle(PlanForwardCompatibleBundleEventArgs args)
        {
            EventHandler<PlanForwardCompatibleBundleEventArgs> handler = this.PlanForwardCompatibleBundle;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheContainerOrPayloadVerifyBegin"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheContainerOrPayloadVerifyBegin(CacheContainerOrPayloadVerifyBeginEventArgs args)
        {
            EventHandler<CacheContainerOrPayloadVerifyBeginEventArgs> handler = this.CacheContainerOrPayloadVerifyBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheContainerOrPayloadVerifyProgress"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheContainerOrPayloadVerifyProgress(CacheContainerOrPayloadVerifyProgressEventArgs args)
        {
            EventHandler<CacheContainerOrPayloadVerifyProgressEventArgs> handler = this.CacheContainerOrPayloadVerifyProgress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CacheContainerOrPayloadVerifyComplete"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheContainerOrPayloadVerifyComplete(CacheContainerOrPayloadVerifyCompleteEventArgs args)
        {
            EventHandler<CacheContainerOrPayloadVerifyCompleteEventArgs> handler = this.CacheContainerOrPayloadVerifyComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CachePayloadExtractBegin"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCachePayloadExtractBegin(CachePayloadExtractBeginEventArgs args)
        {
            EventHandler<CachePayloadExtractBeginEventArgs> handler = this.CachePayloadExtractBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CachePayloadExtractProgress"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCachePayloadExtractProgress(CachePayloadExtractProgressEventArgs args)
        {
            EventHandler<CachePayloadExtractProgressEventArgs> handler = this.CachePayloadExtractProgress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine, raises the <see cref="CachePayloadExtractComplete"/> event.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCachePayloadExtractComplete(CachePayloadExtractCompleteEventArgs args)
        {
            EventHandler<CachePayloadExtractCompleteEventArgs> handler = this.CachePayloadExtractComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        #region IBootstrapperApplication Members

        int IBootstrapperApplication.BAProc(int message, IntPtr pvArgs, IntPtr pvResults, IntPtr pvContext)
        {
            switch (message)
            {
                default:
                    return NativeMethods.E_NOTIMPL;
            }
        }

        void IBootstrapperApplication.BAProcFallback(int message, IntPtr pvArgs, IntPtr pvResults, ref int phr, IntPtr pvContext)
        {
        }

        int IBootstrapperApplication.OnStartup()
        {
            StartupEventArgs args = new StartupEventArgs();
            this.OnStartup(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnShutdown(ref BOOTSTRAPPER_SHUTDOWN_ACTION action)
        {
            ShutdownEventArgs args = new ShutdownEventArgs(action);
            this.OnShutdown(args);

            action = args.Action;
            return args.HResult;
        }

        int IBootstrapperApplication.OnSystemShutdown(EndSessionReasons dwEndSession, ref bool fCancel)
        {
            SystemShutdownEventArgs args = new SystemShutdownEventArgs(dwEndSession, fCancel);
            this.OnSystemShutdown(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectBegin(bool fCached, bool fInstalled, int cPackages, ref bool fCancel)
        {
            DetectBeginEventArgs args = new DetectBeginEventArgs(fCached, fInstalled, cPackages, fCancel);
            this.OnDetectBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectForwardCompatibleBundle(string wzBundleId, RelationType relationType, string wzBundleTag, bool fPerMachine, string wzVersion, bool fMissingFromCache, ref bool fCancel)
        {
            DetectForwardCompatibleBundleEventArgs args = new DetectForwardCompatibleBundleEventArgs(wzBundleId, relationType, wzBundleTag, fPerMachine, wzVersion, fMissingFromCache, fCancel);
            this.OnDetectForwardCompatibleBundle(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectUpdateBegin(string wzUpdateLocation, ref bool fCancel, ref bool fSkip)
        {
            DetectUpdateBeginEventArgs args = new DetectUpdateBeginEventArgs(wzUpdateLocation, fCancel, fSkip);
            this.OnDetectUpdateBegin(args);

            fCancel = args.Cancel;
            fSkip = args.Skip;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectUpdate(string wzUpdateLocation, long dw64Size, string wzVersion, string wzTitle, string wzSummary, string wzContentType, string wzContent, ref bool fCancel, ref bool fStopProcessingUpdates)
        {
            DetectUpdateEventArgs args = new DetectUpdateEventArgs(wzUpdateLocation, dw64Size, wzVersion, wzTitle, wzSummary, wzContentType, wzContent, fCancel, fStopProcessingUpdates);
            this.OnDetectUpdate(args);

            fCancel = args.Cancel;
            fStopProcessingUpdates = args.StopProcessingUpdates;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectUpdateComplete(int hrStatus, ref bool fIgnoreError)
        {
            DetectUpdateCompleteEventArgs args = new DetectUpdateCompleteEventArgs(hrStatus, fIgnoreError);
            this.OnDetectUpdateComplete(args);

            fIgnoreError = args.IgnoreError;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectRelatedBundle(string wzProductCode, RelationType relationType, string wzBundleTag, bool fPerMachine, string wzVersion, RelatedOperation operation, bool fMissingFromCache, ref bool fCancel)
        {
            DetectRelatedBundleEventArgs args = new DetectRelatedBundleEventArgs(wzProductCode, relationType, wzBundleTag, fPerMachine, wzVersion, operation, fMissingFromCache, fCancel);
            this.OnDetectRelatedBundle(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectPackageBegin(string wzPackageId, ref bool fCancel)
        {
            DetectPackageBeginEventArgs args = new DetectPackageBeginEventArgs(wzPackageId, fCancel);
            this.OnDetectPackageBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectRelatedMsiPackage(string wzPackageId, string wzUpgradeCode, string wzProductCode, bool fPerMachine, string wzVersion, RelatedOperation operation, ref bool fCancel)
        {
            DetectRelatedMsiPackageEventArgs args = new DetectRelatedMsiPackageEventArgs(wzPackageId, wzUpgradeCode, wzProductCode, fPerMachine, wzVersion, operation, fCancel);
            this.OnDetectRelatedMsiPackage(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectPatchTarget(string wzPackageId, string wzProductCode, PackageState patchState, ref bool fCancel)
        {
            DetectPatchTargetEventArgs args = new DetectPatchTargetEventArgs(wzPackageId, wzProductCode, patchState, fCancel);
            this.OnDetectPatchTarget(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectMsiFeature(string wzPackageId, string wzFeatureId, FeatureState state, ref bool fCancel)
        {
            DetectMsiFeatureEventArgs args = new DetectMsiFeatureEventArgs(wzPackageId, wzFeatureId, state, fCancel);
            this.OnDetectMsiFeature(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectPackageComplete(string wzPackageId, int hrStatus, PackageState state, bool fCached)
        {
            DetectPackageCompleteEventArgs args = new DetectPackageCompleteEventArgs(wzPackageId, hrStatus, state, fCached);
            this.OnDetectPackageComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectComplete(int hrStatus, bool fEligibleForCleanup)
        {
            DetectCompleteEventArgs args = new DetectCompleteEventArgs(hrStatus, fEligibleForCleanup);
            this.OnDetectComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanBegin(int cPackages, ref bool fCancel)
        {
            PlanBeginEventArgs args = new PlanBeginEventArgs(cPackages, fCancel);
            this.OnPlanBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanRelatedBundle(string wzBundleId, RequestState recommendedState, ref RequestState pRequestedState, ref bool fCancel)
        {
            PlanRelatedBundleEventArgs args = new PlanRelatedBundleEventArgs(wzBundleId, recommendedState, pRequestedState, fCancel);
            this.OnPlanRelatedBundle(args);

            pRequestedState = args.State;
            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanPackageBegin(string wzPackageId, PackageState state, bool fCached, BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition, RequestState recommendedState, BOOTSTRAPPER_CACHE_TYPE recommendedCacheType, ref RequestState pRequestedState, ref BOOTSTRAPPER_CACHE_TYPE pRequestedCacheType, ref bool fCancel)
        {
            PlanPackageBeginEventArgs args = new PlanPackageBeginEventArgs(wzPackageId, state, fCached, installCondition, recommendedState, recommendedCacheType, pRequestedState, pRequestedCacheType, fCancel);
            this.OnPlanPackageBegin(args);

            pRequestedState = args.State;
            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanPatchTarget(string wzPackageId, string wzProductCode, RequestState recommendedState, ref RequestState pRequestedState, ref bool fCancel)
        {
            PlanPatchTargetEventArgs args = new PlanPatchTargetEventArgs(wzPackageId, wzProductCode, recommendedState, pRequestedState, fCancel);
            this.OnPlanPatchTarget(args);

            pRequestedState = args.State;
            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanMsiFeature(string wzPackageId, string wzFeatureId, FeatureState recommendedState, ref FeatureState pRequestedState, ref bool fCancel)
        {
            PlanMsiFeatureEventArgs args = new PlanMsiFeatureEventArgs(wzPackageId, wzFeatureId, recommendedState, pRequestedState, fCancel);
            this.OnPlanMsiFeature(args);

            pRequestedState = args.State;
            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanMsiPackage(string wzPackageId, bool fExecute, ActionState action, ref bool fCancel, ref BURN_MSI_PROPERTY actionMsiProperty, ref INSTALLUILEVEL uiLevel, ref bool fDisableExternalUiHandler)
        {
            PlanMsiPackageEventArgs args = new PlanMsiPackageEventArgs(wzPackageId, fExecute, action, fCancel, actionMsiProperty, uiLevel, fDisableExternalUiHandler);
            this.OnPlanMsiPackage(args);

            fCancel = args.Cancel;
            actionMsiProperty = args.ActionMsiProperty;
            uiLevel = args.UiLevel;
            fDisableExternalUiHandler = args.DisableExternalUiHandler;
            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanPackageComplete(string wzPackageId, int hrStatus, RequestState requested)
        {
            var args = new PlanPackageCompleteEventArgs(wzPackageId, hrStatus, requested);
            this.OnPlanPackageComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnPlannedPackage(string wzPackageId, ActionState execute, ActionState rollback, bool fPlannedCache, bool fPlannedUncache)
        {
            var args = new PlannedPackageEventArgs(wzPackageId, execute, rollback, fPlannedCache, fPlannedUncache);
            this.OnPlannedPackage(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanComplete(int hrStatus)
        {
            PlanCompleteEventArgs args = new PlanCompleteEventArgs(hrStatus);
            this.OnPlanComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnApplyBegin(int dwPhaseCount, ref bool fCancel)
        {
            this.applying = true;

            ApplyBeginEventArgs args = new ApplyBeginEventArgs(dwPhaseCount, fCancel);
            this.OnApplyBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnElevateBegin(ref bool fCancel)
        {
            ElevateBeginEventArgs args = new ElevateBeginEventArgs(fCancel);
            this.OnElevateBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnElevateComplete(int hrStatus)
        {
            ElevateCompleteEventArgs args = new ElevateCompleteEventArgs(hrStatus);
            this.OnElevateComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnProgress(int dwProgressPercentage, int dwOverallPercentage, ref bool fCancel)
        {
            ProgressEventArgs args = new ProgressEventArgs(dwProgressPercentage, dwOverallPercentage, fCancel);
            this.OnProgress(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnError(ErrorType errorType, string wzPackageId, int dwCode, string wzError, int dwUIHint, int cData, string[] rgwzData, Result nRecommendation, ref Result pResult)
        {
            ErrorEventArgs args = new ErrorEventArgs(errorType, wzPackageId, dwCode, wzError, dwUIHint, rgwzData, nRecommendation, pResult);
            this.OnError(args);

            pResult = args.Result;
            return args.HResult;
        }

        int IBootstrapperApplication.OnRegisterBegin(ref bool fCancel)
        {
            RegisterBeginEventArgs args = new RegisterBeginEventArgs(fCancel);
            this.OnRegisterBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnRegisterComplete(int hrStatus)
        {
            RegisterCompleteEventArgs args = new RegisterCompleteEventArgs(hrStatus);
            this.OnRegisterComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheBegin(ref bool fCancel)
        {
            CacheBeginEventArgs args = new CacheBeginEventArgs(fCancel);
            this.OnCacheBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCachePackageBegin(string wzPackageId, int cCachePayloads, long dw64PackageCacheSize, ref bool fCancel)
        {
            CachePackageBeginEventArgs args = new CachePackageBeginEventArgs(wzPackageId, cCachePayloads, dw64PackageCacheSize, fCancel);
            this.OnCachePackageBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheAcquireBegin(string wzPackageOrContainerId, string wzPayloadId, string wzSource, string wzDownloadUrl, string wzPayloadContainerId, CacheOperation recommendation, ref CacheOperation action, ref bool fCancel)
        {
            CacheAcquireBeginEventArgs args = new CacheAcquireBeginEventArgs(wzPackageOrContainerId, wzPayloadId, wzSource, wzDownloadUrl, wzPayloadContainerId, recommendation, action, fCancel);
            this.OnCacheAcquireBegin(args);

            action = args.Action;
            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheAcquireProgress(string wzPackageOrContainerId, string wzPayloadId, long dw64Progress, long dw64Total, int dwOverallPercentage, ref bool fCancel)
        {
            CacheAcquireProgressEventArgs args = new CacheAcquireProgressEventArgs(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, fCancel);
            this.OnCacheAcquireProgress(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheAcquireResolving(string wzPackageOrContainerId, string wzPayloadId, string[] searchPaths, int cSearchPaths, bool fFoundLocal, int dwRecommendedSearchPath, string wzDownloadUrl, string wzPayloadContainerId, CacheResolveOperation recommendation, ref int dwChosenSearchPath, ref CacheResolveOperation action, ref bool fCancel)
        {
            CacheAcquireResolvingEventArgs args = new CacheAcquireResolvingEventArgs(wzPackageOrContainerId, wzPayloadId, searchPaths, fFoundLocal, dwRecommendedSearchPath, wzDownloadUrl, wzPayloadContainerId, recommendation, dwChosenSearchPath, action, fCancel);
            this.OnCacheAcquireResolving(args);

            dwChosenSearchPath = args.ChosenSearchPath;
            action = args.Action;
            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheAcquireComplete(string wzPackageOrContainerId, string wzPayloadId, int hrStatus, BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION recommendation, ref BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION action)
        {
            CacheAcquireCompleteEventArgs args = new CacheAcquireCompleteEventArgs(wzPackageOrContainerId, wzPayloadId, hrStatus, recommendation, action);
            this.OnCacheAcquireComplete(args);

            action = args.Action;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheVerifyBegin(string wzPackageOrContainerId, string wzPayloadId, ref bool fCancel)
        {
            CacheVerifyBeginEventArgs args = new CacheVerifyBeginEventArgs(wzPackageOrContainerId, wzPayloadId, fCancel);
            this.OnCacheVerifyBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheVerifyProgress(string wzPackageOrContainerId, string wzPayloadId, long dw64Progress, long dw64Total, int dwOverallPercentage, CacheVerifyStep verifyStep, ref bool fCancel)
        {
            CacheVerifyProgressEventArgs args = new CacheVerifyProgressEventArgs(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, verifyStep, fCancel);
            this.OnCacheVerifyProgress(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheVerifyComplete(string wzPackageOrContainerId, string wzPayloadId, int hrStatus, BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION recommendation, ref BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION action)
        {
            CacheVerifyCompleteEventArgs args = new CacheVerifyCompleteEventArgs(wzPackageOrContainerId, wzPayloadId, hrStatus, recommendation, action);
            this.OnCacheVerifyComplete(args);

            action = args.Action;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCachePackageComplete(string wzPackageId, int hrStatus, BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION recommendation, ref BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION action)
        {
            CachePackageCompleteEventArgs args = new CachePackageCompleteEventArgs(wzPackageId, hrStatus, recommendation, action);
            this.OnCachePackageComplete(args);

            action = args.Action;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheComplete(int hrStatus)
        {
            CacheCompleteEventArgs args = new CacheCompleteEventArgs(hrStatus);
            this.OnCacheComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnExecuteBegin(int cExecutingPackages, ref bool fCancel)
        {
            ExecuteBeginEventArgs args = new ExecuteBeginEventArgs(cExecutingPackages, fCancel);
            this.OnExecuteBegin(args);

            args.Cancel = fCancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnExecutePackageBegin(string wzPackageId, bool fExecute, ActionState action, INSTALLUILEVEL uiLevel, bool fDisableExternalUiHandler, ref bool fCancel)
        {
            ExecutePackageBeginEventArgs args = new ExecutePackageBeginEventArgs(wzPackageId, fExecute, action, uiLevel, fDisableExternalUiHandler, fCancel);
            this.OnExecutePackageBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnExecutePatchTarget(string wzPackageId, string wzTargetProductCode, ref bool fCancel)
        {
            ExecutePatchTargetEventArgs args = new ExecutePatchTargetEventArgs(wzPackageId, wzTargetProductCode, fCancel);
            this.OnExecutePatchTarget(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnExecuteProgress(string wzPackageId, int dwProgressPercentage, int dwOverallPercentage, ref bool fCancel)
        {
            ExecuteProgressEventArgs args = new ExecuteProgressEventArgs(wzPackageId, dwProgressPercentage, dwOverallPercentage, fCancel);
            this.OnExecuteProgress(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnExecuteMsiMessage(string wzPackageId, InstallMessage messageType, int dwUIHint, string wzMessage, int cData, string[] rgwzData, Result nRecommendation, ref Result pResult)
        {
            ExecuteMsiMessageEventArgs args = new ExecuteMsiMessageEventArgs(wzPackageId, messageType, dwUIHint, wzMessage, rgwzData, nRecommendation, pResult);
            this.OnExecuteMsiMessage(args);

            pResult = args.Result;
            return args.HResult;
        }

        int IBootstrapperApplication.OnExecuteFilesInUse(string wzPackageId, int cFiles, string[] rgwzFiles, Result nRecommendation, ref Result pResult)
        {
            ExecuteFilesInUseEventArgs args = new ExecuteFilesInUseEventArgs(wzPackageId, rgwzFiles, nRecommendation, pResult);
            this.OnExecuteFilesInUse(args);

            pResult = args.Result;
            return args.HResult;
        }

        int IBootstrapperApplication.OnExecutePackageComplete(string wzPackageId, int hrStatus, ApplyRestart restart, BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION recommendation, ref BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION pAction)
        {
            ExecutePackageCompleteEventArgs args = new ExecutePackageCompleteEventArgs(wzPackageId, hrStatus, restart, recommendation, pAction);
            this.OnExecutePackageComplete(args);

            pAction = args.Action;
            return args.HResult;
        }

        int IBootstrapperApplication.OnExecuteComplete(int hrStatus)
        {
            ExecuteCompleteEventArgs args = new ExecuteCompleteEventArgs(hrStatus);
            this.OnExecuteComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnUnregisterBegin(bool fKeepRegistration, ref bool fForceKeepRegistration)
        {
            UnregisterBeginEventArgs args = new UnregisterBeginEventArgs(fKeepRegistration, fForceKeepRegistration);
            this.OnUnregisterBegin(args);

            fForceKeepRegistration = args.ForceKeepRegistration;
            return args.HResult;
        }

        int IBootstrapperApplication.OnUnregisterComplete(int hrStatus)
        {
            UnregisterCompleteEventArgs args = new UnregisterCompleteEventArgs(hrStatus);
            this.OnUnregisterComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnApplyComplete(int hrStatus, ApplyRestart restart, BOOTSTRAPPER_APPLYCOMPLETE_ACTION recommendation, ref BOOTSTRAPPER_APPLYCOMPLETE_ACTION pAction)
        {
            ApplyCompleteEventArgs args = new ApplyCompleteEventArgs(hrStatus, restart, recommendation, pAction);
            this.OnApplyComplete(args);

            this.applying = false;

            pAction = args.Action;
            return args.HResult;
        }

        int IBootstrapperApplication.OnLaunchApprovedExeBegin(ref bool fCancel)
        {
            LaunchApprovedExeBeginEventArgs args = new LaunchApprovedExeBeginEventArgs(fCancel);
            this.OnLaunchApprovedExeBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnLaunchApprovedExeComplete(int hrStatus, int processId)
        {
            LaunchApprovedExeCompleteEventArgs args = new LaunchApprovedExeCompleteEventArgs(hrStatus, processId);
            this.OnLaunchApprovedExeComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnBeginMsiTransactionBegin(string transactionId, ref bool fCancel)
        {
            BeginMsiTransactionBeginEventArgs args = new BeginMsiTransactionBeginEventArgs(transactionId, fCancel);
            this.OnBeginMsiTransactionBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnBeginMsiTransactionComplete(string transactionId, int hrStatus)
        {
            BeginMsiTransactionCompleteEventArgs args = new BeginMsiTransactionCompleteEventArgs(transactionId, hrStatus);
            this.OnBeginMsiTransactionComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnCommitMsiTransactionBegin(string transactionId, ref bool fCancel)
        {
            CommitMsiTransactionBeginEventArgs args = new CommitMsiTransactionBeginEventArgs(transactionId, fCancel);
            this.OnCommitMsiTransactionBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCommitMsiTransactionComplete(string transactionId, int hrStatus)
        {
            CommitMsiTransactionCompleteEventArgs args = new CommitMsiTransactionCompleteEventArgs(transactionId, hrStatus);
            this.OnCommitMsiTransactionComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnRollbackMsiTransactionBegin(string transactionId)
        {
            RollbackMsiTransactionBeginEventArgs args = new RollbackMsiTransactionBeginEventArgs(transactionId);
            this.OnRollbackMsiTransactionBegin(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnRollbackMsiTransactionComplete(string transactionId, int hrStatus)
        {
            RollbackMsiTransactionCompleteEventArgs args = new RollbackMsiTransactionCompleteEventArgs(transactionId, hrStatus);
            this.OnRollbackMsiTransactionComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnPauseAutomaticUpdatesBegin()
        {
            PauseAutomaticUpdatesBeginEventArgs args = new PauseAutomaticUpdatesBeginEventArgs();
            this.OnPauseAutomaticUpdatesBegin(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnPauseAutomaticUpdatesComplete(int hrStatus)
        {
            PauseAutomaticUpdatesCompleteEventArgs args = new PauseAutomaticUpdatesCompleteEventArgs(hrStatus);
            this.OnPauseAutomaticUpdatesComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnSystemRestorePointBegin()
        {
            SystemRestorePointBeginEventArgs args = new SystemRestorePointBeginEventArgs();
            this.OnSystemRestorePointBegin(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnSystemRestorePointComplete(int hrStatus)
        {
            SystemRestorePointCompleteEventArgs args = new SystemRestorePointCompleteEventArgs(hrStatus);
            this.OnSystemRestorePointComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanForwardCompatibleBundle(string wzBundleId, RelationType relationType, string wzBundleTag, bool fPerMachine, string wzVersion, bool fRecommendedIgnoreBundle, ref bool fCancel, ref bool fIgnoreBundle)
        {
            PlanForwardCompatibleBundleEventArgs args = new PlanForwardCompatibleBundleEventArgs(wzBundleId, relationType, wzBundleTag, fPerMachine, wzVersion, fRecommendedIgnoreBundle, fCancel, fIgnoreBundle);
            this.OnPlanForwardCompatibleBundle(args);

            fCancel = args.Cancel;
            fIgnoreBundle = args.IgnoreBundle;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheContainerOrPayloadVerifyBegin(string wzPackageOrContainerId, string wzPayloadId, ref bool fCancel)
        {
            CacheContainerOrPayloadVerifyBeginEventArgs args = new CacheContainerOrPayloadVerifyBeginEventArgs(wzPackageOrContainerId, wzPayloadId, fCancel);
            this.OnCacheContainerOrPayloadVerifyBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheContainerOrPayloadVerifyProgress(string wzPackageOrContainerId, string wzPayloadId, long dw64Progress, long dw64Total, int dwOverallPercentage, ref bool fCancel)
        {
            CacheContainerOrPayloadVerifyProgressEventArgs args = new CacheContainerOrPayloadVerifyProgressEventArgs(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, fCancel);
            this.OnCacheContainerOrPayloadVerifyProgress(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheContainerOrPayloadVerifyComplete(string wzPackageOrContainerId, string wzPayloadId, int hrStatus)
        {
            CacheContainerOrPayloadVerifyCompleteEventArgs args = new CacheContainerOrPayloadVerifyCompleteEventArgs(wzPackageOrContainerId, wzPayloadId, hrStatus);
            this.OnCacheContainerOrPayloadVerifyComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnCachePayloadExtractBegin(string wzContainerId, string wzPayloadId, ref bool fCancel)
        {
            CachePayloadExtractBeginEventArgs args = new CachePayloadExtractBeginEventArgs(wzContainerId, wzPayloadId, fCancel);
            this.OnCachePayloadExtractBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCachePayloadExtractProgress(string wzContainerId, string wzPayloadId, long dw64Progress, long dw64Total, int dwOverallPercentage, ref bool fCancel)
        {
            CachePayloadExtractProgressEventArgs args = new CachePayloadExtractProgressEventArgs(wzContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, fCancel);
            this.OnCachePayloadExtractProgress(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCachePayloadExtractComplete(string wzContainerId, string wzPayloadId, int hrStatus)
        {
            CachePayloadExtractCompleteEventArgs args = new CachePayloadExtractCompleteEventArgs(wzContainerId, wzPayloadId, hrStatus);
            this.OnCachePayloadExtractComplete(args);

            return args.HResult;
        }

        #endregion
    }
}
