// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;

    /// <summary>
    /// Interface for built-in implementation of <see cref="IBootstrapperApplication"/>.
    /// </summary>
    public interface IDefaultBootstrapperApplication : IBootstrapperApplication
    {
        /// <summary>
        /// Fired when the engine has begun installing the bundle.
        /// </summary>
        event EventHandler<ApplyBeginEventArgs> ApplyBegin;

        /// <summary>
        /// Fired when the engine has completed installing the bundle.
        /// </summary>
        event EventHandler<ApplyCompleteEventArgs> ApplyComplete;

        /// <summary>
        /// Fired when the engine is about to begin an MSI transaction.
        /// </summary>
        event EventHandler<BeginMsiTransactionBeginEventArgs> BeginMsiTransactionBegin;

        /// <summary>
        /// Fired when the engine has completed beginning an MSI transaction.
        /// </summary>
        event EventHandler<BeginMsiTransactionCompleteEventArgs> BeginMsiTransactionComplete;

        /// <summary>
        /// Fired when the engine has begun acquiring the installation sources.
        /// </summary>
        event EventHandler<CacheAcquireBeginEventArgs> CacheAcquireBegin;

        /// <summary>
        /// Fired when the engine has completed the acquisition of the installation sources.
        /// </summary>
        event EventHandler<CacheAcquireCompleteEventArgs> CacheAcquireComplete;

        /// <summary>
        /// Fired when the engine has progress acquiring the installation sources.
        /// </summary>
        event EventHandler<CacheAcquireProgressEventArgs> CacheAcquireProgress;

        /// <summary>
        /// Fired when the engine has begun caching the installation sources.
        /// </summary>
        event EventHandler<CacheBeginEventArgs> CacheBegin;

        /// <summary>
        /// Fired after the engine has cached the installation sources.
        /// </summary>
        event EventHandler<CacheCompleteEventArgs> CacheComplete;

        /// <summary>
        /// Fired when the engine has begun caching a specific package.
        /// </summary>
        event EventHandler<CachePackageBeginEventArgs> CachePackageBegin;

        /// <summary>
        /// Fired when the engine has completed caching a specific package.
        /// </summary>
        event EventHandler<CachePackageCompleteEventArgs> CachePackageComplete;

        /// <summary>
        /// Fired when the engine begins the verification of the acquired installation sources.
        /// </summary>
        event EventHandler<CacheVerifyBeginEventArgs> CacheVerifyBegin;

        /// <summary>
        /// Fired when the engine complete the verification of the acquired installation sources.
        /// </summary>
        event EventHandler<CacheVerifyCompleteEventArgs> CacheVerifyComplete;

        /// <summary>
        /// Fired when the engine is about to commit an MSI transaction.
        /// </summary>
        event EventHandler<CommitMsiTransactionBeginEventArgs> CommitMsiTransactionBegin;

        /// <summary>
        /// Fired when the engine has completed comitting an MSI transaction.
        /// </summary>
        event EventHandler<CommitMsiTransactionCompleteEventArgs> CommitMsiTransactionComplete;

        /// <summary>
        /// Fired when the overall detection phase has begun.
        /// </summary>
        event EventHandler<DetectBeginEventArgs> DetectBegin;

        /// <summary>
        /// Fired when a package was not detected but a package using the same provider key was.
        /// </summary>
        event EventHandler<DetectCompatibleMsiPackageEventArgs> DetectCompatibleMsiPackage;

        /// <summary>
        /// Fired when the detection phase has completed.
        /// </summary>
        event EventHandler<DetectCompleteEventArgs> DetectComplete;

        /// <summary>
        /// Fired when a forward compatible bundle is detected.
        /// </summary>
        event EventHandler<DetectForwardCompatibleBundleEventArgs> DetectForwardCompatibleBundle;

        /// <summary>
        /// Fired when a feature in an MSI package has been detected.
        /// </summary>
        event EventHandler<DetectMsiFeatureEventArgs> DetectMsiFeature;

        /// <summary>
        /// Fired when the detection for a specific package has begun.
        /// </summary>
        event EventHandler<DetectPackageBeginEventArgs> DetectPackageBegin;

        /// <summary>
        /// Fired when the detection for a specific package has completed.
        /// </summary>
        event EventHandler<DetectPackageCompleteEventArgs> DetectPackageComplete;

        /// <summary>
        /// Fired when a related bundle has been detected for a bundle.
        /// </summary>
        event EventHandler<DetectRelatedBundleEventArgs> DetectRelatedBundle;

        /// <summary>
        /// Fired when a related MSI package has been detected for a package.
        /// </summary>
        event EventHandler<DetectRelatedMsiPackageEventArgs> DetectRelatedMsiPackage;

        /// <summary>
        /// Fired when an MSP package detects a target MSI has been detected.
        /// </summary>
        event EventHandler<DetectTargetMsiPackageEventArgs> DetectTargetMsiPackage;

        /// <summary>
        /// Fired when the update detection has found a potential update candidate.
        /// </summary>
        event EventHandler<DetectUpdateEventArgs> DetectUpdate;

        /// <summary>
        /// Fired when the update detection phase has begun.
        /// </summary>
        event EventHandler<DetectUpdateBeginEventArgs> DetectUpdateBegin;

        /// <summary>
        /// Fired when the update detection phase has completed.
        /// </summary>
        event EventHandler<DetectUpdateCompleteEventArgs> DetectUpdateComplete;

        /// <summary>
        /// Fired when the engine is about to start the elevated process.
        /// </summary>
        event EventHandler<ElevateBeginEventArgs> ElevateBegin;

        /// <summary>
        /// Fired when the engine has completed starting the elevated process.
        /// </summary>
        event EventHandler<ElevateCompleteEventArgs> ElevateComplete;

        /// <summary>
        /// Fired when the engine has encountered an error.
        /// </summary>
        event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// Fired when the engine has begun installing packages.
        /// </summary>
        event EventHandler<ExecuteBeginEventArgs> ExecuteBegin;

        /// <summary>
        /// Fired when the engine has completed installing packages.
        /// </summary>
        event EventHandler<ExecuteCompleteEventArgs> ExecuteComplete;

        /// <summary>
        /// Fired when Windows Installer sends a files in use installation message.
        /// </summary>
        event EventHandler<ExecuteFilesInUseEventArgs> ExecuteFilesInUse;

        /// <summary>
        /// Fired when Windows Installer sends an installation message.
        /// </summary>
        event EventHandler<ExecuteMsiMessageEventArgs> ExecuteMsiMessage;

        /// <summary>
        /// Fired when the engine has begun installing a specific package.
        /// </summary>
        event EventHandler<ExecutePackageBeginEventArgs> ExecutePackageBegin;

        /// <summary>
        /// Fired when the engine has completed installing a specific package.
        /// </summary>
        event EventHandler<ExecutePackageCompleteEventArgs> ExecutePackageComplete;

        /// <summary>
        /// Fired when the engine executes one or more patches targeting a product.
        /// </summary>
        event EventHandler<ExecutePatchTargetEventArgs> ExecutePatchTarget;

        /// <summary>
        /// Fired by the engine while executing on payload.
        /// </summary>
        event EventHandler<ExecuteProgressEventArgs> ExecuteProgress;

        /// <summary>
        /// Fired when the engine is about to launch the preapproved executable.
        /// </summary>
        event EventHandler<LaunchApprovedExeBeginEventArgs> LaunchApprovedExeBegin;

        /// <summary>
        /// Fired when the engine has completed launching the preapproved executable.
        /// </summary>
        event EventHandler<LaunchApprovedExeCompleteEventArgs> LaunchApprovedExeComplete;

        /// <summary>
        /// Fired when the engine is about to pause Windows automatic updates.
        /// </summary>
        event EventHandler<PauseAutomaticUpdatesBeginEventArgs> PauseAutomaticUpdatesBegin;

        /// <summary>
        /// Fired when the engine has completed pausing Windows automatic updates.
        /// </summary>
        event EventHandler<PauseAutomaticUpdatesCompleteEventArgs> PauseAutomaticUpdatesComplete;

        /// <summary>
        /// Fired when the engine has begun planning the installation.
        /// </summary>
        event EventHandler<PlanBeginEventArgs> PlanBegin;

        /// <summary>
        /// Fired when the engine plans a new, compatible package using the same provider key.
        /// </summary>
        event EventHandler<PlanCompatibleMsiPackageBeginEventArgs> PlanCompatibleMsiPackageBegin;

        /// <summary>
        /// Fired when the engine has completed planning the installation of a specific package.
        /// </summary>
        event EventHandler<PlanCompatibleMsiPackageCompleteEventArgs> PlanCompatibleMsiPackageComplete;

        /// <summary>
        /// Fired when the engine has completed planning the installation.
        /// </summary>
        event EventHandler<PlanCompleteEventArgs> PlanComplete;

        /// <summary>
        /// Fired when the engine is about to plan a feature in an MSI package.
        /// </summary>
        event EventHandler<PlanMsiFeatureEventArgs> PlanMsiFeature;

        /// <summary>
        /// Fired when the engine is planning an MSI or MSP package.
        /// </summary>
        event EventHandler<PlanMsiPackageEventArgs> PlanMsiPackage;

        /// <summary>
        /// Fired when the engine has begun planning the installation of a specific package.
        /// </summary>
        event EventHandler<PlanPackageBeginEventArgs> PlanPackageBegin;

        /// <summary>
        /// Fired when the engine has completed planning the installation of a specific package.
        /// </summary>
        event EventHandler<PlanPackageCompleteEventArgs> PlanPackageComplete;

        /// <summary>
        /// Fired when the engine has begun planning for a related bundle.
        /// </summary>
        event EventHandler<PlanRelatedBundleEventArgs> PlanRelatedBundle;

        /// <summary>
        /// Fired when the engine is about to plan the target MSI of a MSP package.
        /// </summary>
        event EventHandler<PlanTargetMsiPackageEventArgs> PlanTargetMsiPackage;

        /// <summary>
        /// Fired when the engine has changed progress for the bundle installation.
        /// </summary>
        event EventHandler<ProgressEventArgs> Progress;

        /// <summary>
        /// Fired when the engine has begun registering the location and visibility of the bundle.
        /// </summary>
        event EventHandler<RegisterBeginEventArgs> RegisterBegin;

        /// <summary>
        /// Fired when the engine has completed registering the location and visibility of the bundle.
        /// </summary>
        event EventHandler<RegisterCompleteEventArgs> RegisterComplete;

        /// <summary>
        /// Fired by the engine to allow the BA to change the source
        /// using <see cref="IEngine.SetLocalSource(string, string, string)"/> or <see cref="IEngine.SetDownloadSource(string, string, string, string, string)"/>.
        /// </summary>
        event EventHandler<ResolveSourceEventArgs> ResolveSource;

        /// <summary>
        /// Fired when the engine is about to rollback an MSI transaction.
        /// </summary>
        event EventHandler<RollbackMsiTransactionBeginEventArgs> RollbackMsiTransactionBegin;

        /// <summary>
        /// Fired when the engine has completed rolling back an MSI transaction.
        /// </summary>
        event EventHandler<RollbackMsiTransactionCompleteEventArgs> RollbackMsiTransactionComplete;

        /// <summary>
        /// Fired when the engine is shutting down the bootstrapper application.
        /// </summary>
        event EventHandler<ShutdownEventArgs> Shutdown;

        /// <summary>
        /// Fired when the engine is starting up the bootstrapper application.
        /// </summary>
        event EventHandler<StartupEventArgs> Startup;

        /// <summary>
        /// Fired when the engine is about to take a system restore point.
        /// </summary>
        event EventHandler<SystemRestorePointBeginEventArgs> SystemRestorePointBegin;

        /// <summary>
        /// Fired when the engine has completed taking a system restore point.
        /// </summary>
        event EventHandler<SystemRestorePointCompleteEventArgs> SystemRestorePointComplete;

        /// <summary>
        /// Fired when the system is shutting down or user is logging off.
        /// </summary>
        /// <remarks>
        /// <para>To prevent shutting down or logging off, set <see cref="CancellableHResultEventArgs.Cancel"/> to
        /// true; otherwise, set it to false.</para>
        /// <para>By default setup will prevent shutting down or logging off between
        /// <see cref="IDefaultBootstrapperApplication.ApplyBegin"/> and <see cref="IDefaultBootstrapperApplication.ApplyComplete"/>.
        /// Derivatives can change this behavior by handling <see cref="IDefaultBootstrapperApplication.SystemShutdown"/>.</para>
        /// <para>If <see cref="SystemShutdownEventArgs.Reasons"/> contains <see cref="EndSessionReasons.Critical"/>
        /// the bootstrapper cannot prevent the shutdown and only has a few seconds to save state or perform any other
        /// critical operations before being closed by the operating system.</para>
        /// <para>This event may be fired on a different thread.</para>
        /// </remarks>
        event EventHandler<SystemShutdownEventArgs> SystemShutdown;

        /// <summary>
        /// Fired when the engine has begun removing the registration for the location and visibility of the bundle.
        /// </summary>
        event EventHandler<UnregisterBeginEventArgs> UnregisterBegin;

        /// <summary>
        /// Fired when the engine has completed removing the registration for the location and visibility of the bundle.
        /// </summary>
        event EventHandler<UnregisterCompleteEventArgs> UnregisterComplete;
    }
}