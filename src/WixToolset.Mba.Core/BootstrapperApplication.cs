// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
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

        /// <summary>
        /// Fired when the engine is starting up the bootstrapper application.
        /// </summary>
        public event EventHandler<StartupEventArgs> Startup;

        /// <summary>
        /// Fired when the engine is shutting down the bootstrapper application.
        /// </summary>
        public event EventHandler<ShutdownEventArgs> Shutdown;

        /// <summary>
        /// Fired when the system is shutting down or user is logging off.
        /// </summary>
        /// <remarks>
        /// <para>To prevent shutting down or logging off, set <see cref="CancellableHResultEventArgs.Cancel"/> to
        /// true; otherwise, set it to false.</para>
        /// <para>By default setup will prevent shutting down or logging off between
        /// <see cref="BootstrapperApplication.ApplyBegin"/> and <see cref="BootstrapperApplication.ApplyComplete"/>.
        /// Derivatives can change this behavior by overriding <see cref="BootstrapperApplication.OnSystemShutdown"/>
        /// or handling <see cref="BootstrapperApplication.SystemShutdown"/>.</para>
        /// <para>If <see cref="SystemShutdownEventArgs.Reasons"/> contains <see cref="EndSessionReasons.Critical"/>
        /// the bootstrapper cannot prevent the shutdown and only has a few seconds to save state or perform any other
        /// critical operations before being closed by the operating system.</para>
        /// <para>This event may be fired on a different thread.</para>
        /// </remarks>
        public event EventHandler<SystemShutdownEventArgs> SystemShutdown;

        /// <summary>
        /// Fired when the overall detection phase has begun.
        /// </summary>
        public event EventHandler<DetectBeginEventArgs> DetectBegin;

        /// <summary>
        /// Fired when a forward compatible bundle is detected.
        /// </summary>
        public event EventHandler<DetectForwardCompatibleBundleEventArgs> DetectForwardCompatibleBundle;

        /// <summary>
        /// Fired when the update detection phase has begun.
        /// </summary>
        public event EventHandler<DetectUpdateBeginEventArgs> DetectUpdateBegin;

        /// <summary>
        /// Fired when the update detection has found a potential update candidate.
        /// </summary>
        public event EventHandler<DetectUpdateEventArgs> DetectUpdate;

        /// <summary>
        /// Fired when the update detection phase has completed.
        /// </summary>
        public event EventHandler<DetectUpdateCompleteEventArgs> DetectUpdateComplete;

        /// <summary>
        /// Fired when a related bundle has been detected for a bundle.
        /// </summary>
        public event EventHandler<DetectRelatedBundleEventArgs> DetectRelatedBundle;

        /// <summary>
        /// Fired when the detection for a specific package has begun.
        /// </summary>
        public event EventHandler<DetectPackageBeginEventArgs> DetectPackageBegin;

        /// <summary>
        /// Fired when a package was not detected but a package using the same provider key was.
        /// </summary>
        public event EventHandler<DetectCompatibleMsiPackageEventArgs> DetectCompatibleMsiPackage;

        /// <summary>
        /// Fired when a related MSI package has been detected for a package.
        /// </summary>
        public event EventHandler<DetectRelatedMsiPackageEventArgs> DetectRelatedMsiPackage;

        /// <summary>
        /// Fired when an MSP package detects a target MSI has been detected.
        /// </summary>
        public event EventHandler<DetectTargetMsiPackageEventArgs> DetectTargetMsiPackage;

        /// <summary>
        /// Fired when a feature in an MSI package has been detected.
        /// </summary>
        public event EventHandler<DetectMsiFeatureEventArgs> DetectMsiFeature;

        /// <summary>
        /// Fired when the detection for a specific package has completed.
        /// </summary>
        public event EventHandler<DetectPackageCompleteEventArgs> DetectPackageComplete;

        /// <summary>
        /// Fired when the detection phase has completed.
        /// </summary>
        public event EventHandler<DetectCompleteEventArgs> DetectComplete;

        /// <summary>
        /// Fired when the engine has begun planning the installation.
        /// </summary>
        public event EventHandler<PlanBeginEventArgs> PlanBegin;

        /// <summary>
        /// Fired when the engine has begun planning for a related bundle.
        /// </summary>
        public event EventHandler<PlanRelatedBundleEventArgs> PlanRelatedBundle;

        /// <summary>
        /// Fired when the engine has begun planning the installation of a specific package.
        /// </summary>
        public event EventHandler<PlanPackageBeginEventArgs> PlanPackageBegin;

        /// <summary>
        /// Fired when the engine plans a new, compatible package using the same provider key.
        /// </summary>
        public event EventHandler<PlanCompatibleMsiPackageBeginEventArgs> PlanCompatibleMsiPackageBegin;

        /// <summary>
        /// Fired when the engine has completed planning the installation of a specific package.
        /// </summary>
        public event EventHandler<PlanCompatibleMsiPackageCompleteEventArgs> PlanCompatibleMsiPackageComplete;

        /// <summary>
        /// Fired when the engine is about to plan the target MSI of a MSP package.
        /// </summary>
        public event EventHandler<PlanTargetMsiPackageEventArgs> PlanTargetMsiPackage;

        /// <summary>
        /// Fired when the engine is about to plan a feature in an MSI package.
        /// </summary>
        public event EventHandler<PlanMsiFeatureEventArgs> PlanMsiFeature;

        /// <summary>
        /// Fired when the engine has completed planning the installation of a specific package.
        /// </summary>
        public event EventHandler<PlanPackageCompleteEventArgs> PlanPackageComplete;

        /// <summary>
        /// Fired when the engine has completed planning the installation.
        /// </summary>
        public event EventHandler<PlanCompleteEventArgs> PlanComplete;

        /// <summary>
        /// Fired when the engine has begun installing the bundle.
        /// </summary>
        public event EventHandler<ApplyBeginEventArgs> ApplyBegin;

        /// <summary>
        /// Fired when the engine is about to start the elevated process.
        /// </summary>
        public event EventHandler<ElevateBeginEventArgs> ElevateBegin;

        /// <summary>
        /// Fired when the engine has completed starting the elevated process.
        /// </summary>
        public event EventHandler<ElevateCompleteEventArgs> ElevateComplete;

        /// <summary>
        /// Fired when the engine has changed progress for the bundle installation.
        /// </summary>
        public event EventHandler<ProgressEventArgs> Progress;

        /// <summary>
        /// Fired when the engine has encountered an error.
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// Fired when the engine has begun registering the location and visibility of the bundle.
        /// </summary>
        public event EventHandler<RegisterBeginEventArgs> RegisterBegin;

        /// <summary>
        /// Fired when the engine has completed registering the location and visibility of the bundle.
        /// </summary>
        public event EventHandler<RegisterCompleteEventArgs> RegisterComplete;

        /// <summary>
        /// Fired when the engine has begun removing the registration for the location and visibility of the bundle.
        /// </summary>
        public event EventHandler<UnregisterBeginEventArgs> UnregisterBegin;

        /// <summary>
        /// Fired when the engine has completed removing the registration for the location and visibility of the bundle.
        /// </summary>
        public event EventHandler<UnregisterCompleteEventArgs> UnregisterComplete;

        /// <summary>
        /// Fired when the engine has begun caching the installation sources.
        /// </summary>
        public event EventHandler<CacheBeginEventArgs> CacheBegin;

        /// <summary>
        /// Fired when the engine has begun caching a specific package.
        /// </summary>
        public event EventHandler<CachePackageBeginEventArgs> CachePackageBegin;

        /// <summary>
        /// Fired when the engine has begun acquiring the installation sources.
        /// </summary>
        public event EventHandler<CacheAcquireBeginEventArgs> CacheAcquireBegin;

        /// <summary>
        /// Fired when the engine has progress acquiring the installation sources.
        /// </summary>
        public event EventHandler<CacheAcquireProgressEventArgs> CacheAcquireProgress;

        /// <summary>
        /// Fired by the engine to allow the BA to change the source
        /// using <see cref="M:Engine.SetLocalSource"/> or <see cref="M:Engine.SetDownloadSource"/>.
        /// </summary>
        public event EventHandler<ResolveSourceEventArgs> ResolveSource;

        /// <summary>
        /// Fired when the engine has completed the acquisition of the installation sources.
        /// </summary>
        public event EventHandler<CacheAcquireCompleteEventArgs> CacheAcquireComplete;

        /// <summary>
        /// Fired when the engine begins the verification of the acquired installation sources.
        /// </summary>
        public event EventHandler<CacheVerifyBeginEventArgs> CacheVerifyBegin;

        /// <summary>
        /// Fired when the engine complete the verification of the acquired installation sources.
        /// </summary>
        public event EventHandler<CacheVerifyCompleteEventArgs> CacheVerifyComplete;

        /// <summary>
        /// Fired when the engine has completed caching a specific package.
        /// </summary>
        public event EventHandler<CachePackageCompleteEventArgs> CachePackageComplete;

        /// <summary>
        /// Fired after the engine has cached the installation sources.
        /// </summary>
        public event EventHandler<CacheCompleteEventArgs> CacheComplete;

        /// <summary>
        /// Fired when the engine has begun installing packages.
        /// </summary>
        public event EventHandler<ExecuteBeginEventArgs> ExecuteBegin;

        /// <summary>
        /// Fired when the engine has begun installing a specific package.
        /// </summary>
        public event EventHandler<ExecutePackageBeginEventArgs> ExecutePackageBegin;

        /// <summary>
        /// Fired when the engine executes one or more patches targeting a product.
        /// </summary>
        public event EventHandler<ExecutePatchTargetEventArgs> ExecutePatchTarget;

        /// <summary>
        /// Fired when Windows Installer sends an installation message.
        /// </summary>
        public event EventHandler<ExecuteMsiMessageEventArgs> ExecuteMsiMessage;

        /// <summary>
        /// Fired when Windows Installer sends a files in use installation message.
        /// </summary>
        public event EventHandler<ExecuteFilesInUseEventArgs> ExecuteFilesInUse;

        /// <summary>
        /// Fired when the engine has completed installing a specific package.
        /// </summary>
        public event EventHandler<ExecutePackageCompleteEventArgs> ExecutePackageComplete;

        /// <summary>
        /// Fired when the engine has completed installing packages.
        /// </summary>
        public event EventHandler<ExecuteCompleteEventArgs> ExecuteComplete;

        /// <summary>
        /// Fired when the engine has completed installing the bundle.
        /// </summary>
        public event EventHandler<ApplyCompleteEventArgs> ApplyComplete;

        /// <summary>
        /// Fired by the engine while executing on payload.
        /// </summary>
        public event EventHandler<ExecuteProgressEventArgs> ExecuteProgress;

        /// <summary>
        /// Fired when the engine is about to launch the preapproved executable.
        /// </summary>
        public event EventHandler<LaunchApprovedExeBeginArgs> LaunchApprovedExeBegin;

        /// <summary>
        /// Fired when the engine has completed launching the preapproved executable.
        /// </summary>
        public event EventHandler<LaunchApprovedExeCompleteArgs> LaunchApprovedExeComplete;

        /// <summary>
        /// Entry point that is called when the bootstrapper application is ready to run.
        /// </summary>
        protected abstract void Run();

        /// <summary>
        /// Called by the engine on startup of the bootstrapper application.
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
        /// Called by the engine to uninitialize the BA.
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
        /// Called when the system is shutting down or the user is logging off.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        /// <remarks>
        /// <para>To prevent shutting down or logging off, set <see cref="CancellableHResultEventArgs.Cancel"/> to
        /// true; otherwise, set it to false.</para>
        /// <para>By default setup will prevent shutting down or logging off between
        /// <see cref="BootstrapperApplication.ApplyBegin"/> and <see cref="BootstrapperApplication.ApplyComplete"/>.
        /// Derivatives can change this behavior by overriding <see cref="BootstrapperApplication.OnSystemShutdown"/>
        /// or handling <see cref="BootstrapperApplication.SystemShutdown"/>.</para>
        /// <para>If <see cref="SystemShutdownEventArgs.Reasons"/> contains <see cref="EndSessionReasons.Critical"/>
        /// the bootstrapper cannot prevent the shutdown and only has a few seconds to save state or perform any other
        /// critical operations before being closed by the operating system.</para>
        /// <para>This method may be called on a different thread.</para>
        /// </remarks>
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
        /// Called when the overall detection phase has begun.
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
        /// Called when the update detection phase has begun.
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
        /// Called when the update detection phase has begun.
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
        /// Fired when the update detection has found a potential update candidate.
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
        /// Called when the update detection phase has completed.
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
        /// Called when a related bundle has been detected for a bundle.
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
        /// Called when the detection for a specific package has begun.
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
        /// Called when a package was not detected but a package using the same provider key was.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectCompatibleMsiPackage(DetectCompatibleMsiPackageEventArgs args)
        {
            EventHandler<DetectCompatibleMsiPackageEventArgs> handler = this.DetectCompatibleMsiPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when a related MSI package has been detected for a package.
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
        /// Called when an MSP package detects a target MSI has been detected.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectTargetMsiPackage(DetectTargetMsiPackageEventArgs args)
        {
            EventHandler<DetectTargetMsiPackageEventArgs> handler = this.DetectTargetMsiPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when an MSI feature has been detected for a package.
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
        /// Called when the detection for a specific package has completed.
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
        /// Called when the detection phase has completed.
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
        /// Called when the engine has begun planning the installation.
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
        /// Called when the engine has begun planning for a prior bundle.
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
        /// Called when the engine has begun planning the installation of a specific package.
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
        /// Called when the engine plans a new, compatible package using the same provider key.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanCompatibleMsiPackageBegin(PlanCompatibleMsiPackageBeginEventArgs args)
        {
            EventHandler<PlanCompatibleMsiPackageBeginEventArgs> handler = this.PlanCompatibleMsiPackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has completed planning the installation of a specific package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanCompatibleMsiPackageComplete(PlanCompatibleMsiPackageCompleteEventArgs args)
        {
            EventHandler<PlanCompatibleMsiPackageCompleteEventArgs> handler = this.PlanCompatibleMsiPackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine is about to plan the target MSI of a MSP package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanTargetMsiPackage(PlanTargetMsiPackageEventArgs args)
        {
            EventHandler<PlanTargetMsiPackageEventArgs> handler = this.PlanTargetMsiPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine is about to plan an MSI feature of a specific package.
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
        /// Called when then engine has completed planning the installation of a specific package.
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
        /// Called when the engine has completed planning the installation.
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
        /// Called when the engine has begun installing the bundle.
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
        /// Called when the engine is about to start the elevated process.
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
        /// Called when the engine has completed starting the elevated process.
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
        /// Called when the engine has changed progress for the bundle installation.
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
        /// Called when the engine has encountered an error.
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
        /// Called when the engine has begun registering the location and visibility of the bundle.
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
        /// Called when the engine has completed registering the location and visilibity of the bundle.
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
        /// Called when the engine has begun removing the registration for the location and visibility of the bundle.
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
        /// Called when the engine has completed removing the registration for the location and visibility of the bundle.
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
        /// Called when the engine begins to cache the installation sources.
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
        /// Called by the engine when it begins to cache a specific package.
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
        /// Called when the engine begins to cache the container or payload.
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
        /// Called when the engine has progressed on caching the container or payload.
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
        /// Called by the engine to allow the BA to change the source
        /// using <see cref="M:Engine.SetLocalSource"/> or <see cref="M:Engine.SetDownloadSource"/>.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnResolveSource(ResolveSourceEventArgs args)
        {
            EventHandler<ResolveSourceEventArgs> handler = this.ResolveSource;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine completes caching of the container or payload.
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
        /// Called when the engine has started verify the payload.
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
        /// Called when the engine completes verification of the payload.
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
        /// Called when the engine completes caching a specific package.
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
        /// Called after the engine has cached the installation sources.
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
        /// Called when the engine has begun installing packages.
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
        /// Called when the engine has begun installing a specific package.
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
        /// Called when the engine executes one or more patches targeting a product.
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
        /// Called when Windows Installer sends an installation message.
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
        /// Called when Windows Installer sends a file in use installation message.
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
        /// Called when the engine has completed installing a specific package.
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
        /// Called when the engine has completed installing packages.
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
        /// Called when the engine has completed installing the bundle.
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
        /// Called by the engine while executing on payload.
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
        /// Called by the engine before trying to launch the preapproved executable.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnLaunchApprovedExeBegin(LaunchApprovedExeBeginArgs args)
        {
            EventHandler<LaunchApprovedExeBeginArgs> handler = this.LaunchApprovedExeBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine after trying to launch the preapproved executable.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnLaunchApprovedExeComplete(LaunchApprovedExeCompleteArgs args)
        {
            EventHandler<LaunchApprovedExeCompleteArgs> handler = this.LaunchApprovedExeComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        #region IBootstrapperApplication Members

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

        int IBootstrapperApplication.OnDetectBegin(bool fInstalled, int cPackages, ref bool fCancel)
        {
            DetectBeginEventArgs args = new DetectBeginEventArgs(fInstalled, cPackages, fCancel);
            this.OnDetectBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectForwardCompatibleBundle(string wzBundleId, RelationType relationType, string wzBundleTag, bool fPerMachine, long version, ref bool fCancel, ref bool fIgnoreBundle)
        {
            DetectForwardCompatibleBundleEventArgs args = new DetectForwardCompatibleBundleEventArgs(wzBundleId, relationType, wzBundleTag, fPerMachine, version, fCancel, fIgnoreBundle);
            this.OnDetectForwardCompatibleBundle(args);

            fCancel = args.Cancel;
            fIgnoreBundle = args.IgnoreBundle;
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

        int IBootstrapperApplication.OnDetectUpdate(string wzUpdateLocation, long dw64Size, long dw64Version, string wzTitle, string wzSummary, string wzContentType, string wzContent, ref bool fCancel, ref bool fStopProcessingUpdates)
        {
            DetectUpdateEventArgs args = new DetectUpdateEventArgs(wzUpdateLocation, dw64Size, dw64Version, wzTitle, wzSummary, wzContentType, wzContent, fCancel, fStopProcessingUpdates);
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

        int IBootstrapperApplication.OnDetectRelatedBundle(string wzProductCode, RelationType relationType, string wzBundleTag, bool fPerMachine, long version, RelatedOperation operation, ref bool fCancel)
        {
            DetectRelatedBundleEventArgs args = new DetectRelatedBundleEventArgs(wzProductCode, relationType, wzBundleTag, fPerMachine, version, operation, fCancel);
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

        int IBootstrapperApplication.OnDetectCompatibleMsiPackage(string wzPackageId, string wzCompatiblePackageId, long dw64CompatiblePackageVersion, ref bool fCancel)
        {
            DetectCompatibleMsiPackageEventArgs args = new DetectCompatibleMsiPackageEventArgs(wzPackageId, wzCompatiblePackageId, dw64CompatiblePackageVersion, fCancel);
            this.OnDetectCompatibleMsiPackage(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectRelatedMsiPackage(string wzPackageId, string wzUpgradeCode, string wzProductCode, bool fPerMachine, long version, RelatedOperation operation, ref bool fCancel)
        {
            DetectRelatedMsiPackageEventArgs args = new DetectRelatedMsiPackageEventArgs(wzPackageId, wzUpgradeCode, wzProductCode, fPerMachine, version, operation, fCancel);
            this.OnDetectRelatedMsiPackage(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectTargetMsiPackage(string wzPackageId, string wzProductCode, PackageState patchState, ref bool fCancel)
        {
            DetectTargetMsiPackageEventArgs args = new DetectTargetMsiPackageEventArgs(wzPackageId, wzProductCode, patchState, fCancel);
            this.OnDetectTargetMsiPackage(args);

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

        int IBootstrapperApplication.OnDetectPackageComplete(string wzPackageId, int hrStatus, PackageState state)
        {
            DetectPackageCompleteEventArgs args = new DetectPackageCompleteEventArgs(wzPackageId, hrStatus, state);
            this.OnDetectPackageComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnDetectComplete(int hrStatus)
        {
            DetectCompleteEventArgs args = new DetectCompleteEventArgs(hrStatus);
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

        int IBootstrapperApplication.OnPlanPackageBegin(string wzPackageId, RequestState recommendedState, ref RequestState pRequestedState, ref bool fCancel)
        {
            PlanPackageBeginEventArgs args = new PlanPackageBeginEventArgs(wzPackageId, recommendedState, pRequestedState, fCancel);
            this.OnPlanPackageBegin(args);

            pRequestedState = args.State;
            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanCompatibleMsiPackageBegin(string wzPackageId, string wzCompatiblePackageId, long dw64CompatiblePackageVersion, RequestState recommendedState, ref RequestState pRequestedState, ref bool fCancel)
        {
            PlanCompatibleMsiPackageBeginEventArgs args = new PlanCompatibleMsiPackageBeginEventArgs(wzPackageId, wzCompatiblePackageId, dw64CompatiblePackageVersion, recommendedState, pRequestedState, fCancel);
            this.OnPlanCompatibleMsiPackageBegin(args);

            pRequestedState = args.State;
            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanCompatibleMsiPackageComplete(string wzPackageId, string wzCompatiblePackageId, int hrStatus, PackageState state, RequestState requested, ActionState execute, ActionState rollback)
        {
            PlanCompatibleMsiPackageCompleteEventArgs args = new PlanCompatibleMsiPackageCompleteEventArgs(wzPackageId, wzCompatiblePackageId, hrStatus, state, requested, execute, rollback);
            this.OnPlanCompatibleMsiPackageComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.OnPlanTargetMsiPackage(string wzPackageId, string wzProductCode, RequestState recommendedState, ref RequestState pRequestedState, ref bool fCancel)
        {
            PlanTargetMsiPackageEventArgs args = new PlanTargetMsiPackageEventArgs(wzPackageId, wzProductCode, recommendedState, pRequestedState, fCancel);
            this.OnPlanTargetMsiPackage(args);

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

        int IBootstrapperApplication.OnPlanPackageComplete(string wzPackageId, int hrStatus, PackageState state, RequestState requested, ActionState execute, ActionState rollback)
        {
            var args = new PlanPackageCompleteEventArgs(wzPackageId, hrStatus, state, requested, execute, rollback);
            this.OnPlanPackageComplete(args);

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

        int IBootstrapperApplication.OnCacheAcquireBegin(string wzPackageOrContainerId, string wzPayloadId, CacheOperation operation, string wzSource, ref bool fCancel)
        {
            CacheAcquireBeginEventArgs args = new CacheAcquireBeginEventArgs(wzPackageOrContainerId, wzPayloadId, operation, wzSource, fCancel);
            this.OnCacheAcquireBegin(args);

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

        int IBootstrapperApplication.OnResolveSource(string wzPackageOrContainerId, string wzPayloadId, string wzLocalSource, string wzDownloadSource, BOOTSTRAPPER_RESOLVESOURCE_ACTION recommendation, ref BOOTSTRAPPER_RESOLVESOURCE_ACTION action, ref bool fCancel)
        {
            ResolveSourceEventArgs args = new ResolveSourceEventArgs(wzPackageOrContainerId, wzPayloadId, wzLocalSource, wzDownloadSource, action, recommendation, fCancel);
            this.OnResolveSource(args);

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

        int IBootstrapperApplication.OnCacheVerifyBegin(string wzPackageId, string wzPayloadId, ref bool fCancel)
        {
            CacheVerifyBeginEventArgs args = new CacheVerifyBeginEventArgs(wzPackageId, wzPayloadId, fCancel);
            this.OnCacheVerifyBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnCacheVerifyComplete(string wzPackageId, string wzPayloadId, int hrStatus, BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION recommendation, ref BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION action)
        {
            CacheVerifyCompleteEventArgs args = new CacheVerifyCompleteEventArgs(wzPackageId, wzPayloadId, hrStatus, recommendation, action);
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

        int IBootstrapperApplication.OnExecutePackageBegin(string wzPackageId, bool fExecute, ref bool fCancel)
        {
            ExecutePackageBeginEventArgs args = new ExecutePackageBeginEventArgs(wzPackageId, fExecute, fCancel);
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

        int IBootstrapperApplication.OnUnregisterBegin(ref bool fCancel)
        {
            UnregisterBeginEventArgs args = new UnregisterBeginEventArgs(fCancel);
            this.OnUnregisterBegin(args);

            fCancel = args.Cancel;
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
            LaunchApprovedExeBeginArgs args = new LaunchApprovedExeBeginArgs(fCancel);
            this.OnLaunchApprovedExeBegin(args);

            fCancel = args.Cancel;
            return args.HResult;
        }

        int IBootstrapperApplication.OnLaunchApprovedExeComplete(int hrStatus, int processId)
        {
            LaunchApprovedExeCompleteArgs args = new LaunchApprovedExeCompleteArgs(hrStatus, processId);
            this.OnLaunchApprovedExeComplete(args);

            return args.HResult;
        }

        int IBootstrapperApplication.BAProc(BOOTSTRAPPER_APPLICATION_MESSAGE message, IntPtr pvArgs, IntPtr pvResults, IntPtr pvContext)
        {
            switch (message)
            {
                default:
                    return NativeMethods.E_NOTIMPL;
            }
        }

        void IBootstrapperApplication.BAProcFallback(BOOTSTRAPPER_APPLICATION_MESSAGE message, IntPtr pvArgs, IntPtr pvResults, ref int phr, IntPtr pvContext)
        {
        }

        #endregion
    }
}
