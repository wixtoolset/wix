// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;

    public interface IDefaultBootstrapperApplication : IBootstrapperApplication
    {
        event EventHandler<ApplyBeginEventArgs> ApplyBegin;
        event EventHandler<ApplyCompleteEventArgs> ApplyComplete;
        event EventHandler<CacheAcquireBeginEventArgs> CacheAcquireBegin;
        event EventHandler<CacheAcquireCompleteEventArgs> CacheAcquireComplete;
        event EventHandler<CacheAcquireProgressEventArgs> CacheAcquireProgress;
        event EventHandler<CacheBeginEventArgs> CacheBegin;
        event EventHandler<CacheCompleteEventArgs> CacheComplete;
        event EventHandler<CachePackageBeginEventArgs> CachePackageBegin;
        event EventHandler<CachePackageCompleteEventArgs> CachePackageComplete;
        event EventHandler<CacheVerifyBeginEventArgs> CacheVerifyBegin;
        event EventHandler<CacheVerifyCompleteEventArgs> CacheVerifyComplete;
        event EventHandler<DetectBeginEventArgs> DetectBegin;
        event EventHandler<DetectCompatibleMsiPackageEventArgs> DetectCompatibleMsiPackage;
        event EventHandler<DetectCompleteEventArgs> DetectComplete;
        event EventHandler<DetectForwardCompatibleBundleEventArgs> DetectForwardCompatibleBundle;
        event EventHandler<DetectMsiFeatureEventArgs> DetectMsiFeature;
        event EventHandler<DetectPackageBeginEventArgs> DetectPackageBegin;
        event EventHandler<DetectPackageCompleteEventArgs> DetectPackageComplete;
        event EventHandler<DetectRelatedBundleEventArgs> DetectRelatedBundle;
        event EventHandler<DetectRelatedMsiPackageEventArgs> DetectRelatedMsiPackage;
        event EventHandler<DetectTargetMsiPackageEventArgs> DetectTargetMsiPackage;
        event EventHandler<DetectUpdateEventArgs> DetectUpdate;
        event EventHandler<DetectUpdateBeginEventArgs> DetectUpdateBegin;
        event EventHandler<DetectUpdateCompleteEventArgs> DetectUpdateComplete;
        event EventHandler<ElevateBeginEventArgs> ElevateBegin;
        event EventHandler<ElevateCompleteEventArgs> ElevateComplete;
        event EventHandler<ErrorEventArgs> Error;
        event EventHandler<ExecuteBeginEventArgs> ExecuteBegin;
        event EventHandler<ExecuteCompleteEventArgs> ExecuteComplete;
        event EventHandler<ExecuteFilesInUseEventArgs> ExecuteFilesInUse;
        event EventHandler<ExecuteMsiMessageEventArgs> ExecuteMsiMessage;
        event EventHandler<ExecutePackageBeginEventArgs> ExecutePackageBegin;
        event EventHandler<ExecutePackageCompleteEventArgs> ExecutePackageComplete;
        event EventHandler<ExecutePatchTargetEventArgs> ExecutePatchTarget;
        event EventHandler<ExecuteProgressEventArgs> ExecuteProgress;
        event EventHandler<LaunchApprovedExeBeginArgs> LaunchApprovedExeBegin;
        event EventHandler<LaunchApprovedExeCompleteArgs> LaunchApprovedExeComplete;
        event EventHandler<PlanBeginEventArgs> PlanBegin;
        event EventHandler<PlanCompatibleMsiPackageBeginEventArgs> PlanCompatibleMsiPackageBegin;
        event EventHandler<PlanCompatibleMsiPackageCompleteEventArgs> PlanCompatibleMsiPackageComplete;
        event EventHandler<PlanCompleteEventArgs> PlanComplete;
        event EventHandler<PlanMsiFeatureEventArgs> PlanMsiFeature;
        event EventHandler<PlanPackageBeginEventArgs> PlanPackageBegin;
        event EventHandler<PlanPackageCompleteEventArgs> PlanPackageComplete;
        event EventHandler<PlanRelatedBundleEventArgs> PlanRelatedBundle;
        event EventHandler<PlanTargetMsiPackageEventArgs> PlanTargetMsiPackage;
        event EventHandler<ProgressEventArgs> Progress;
        event EventHandler<RegisterBeginEventArgs> RegisterBegin;
        event EventHandler<RegisterCompleteEventArgs> RegisterComplete;
        event EventHandler<ResolveSourceEventArgs> ResolveSource;
        event EventHandler<ShutdownEventArgs> Shutdown;
        event EventHandler<StartupEventArgs> Startup;
        event EventHandler<SystemShutdownEventArgs> SystemShutdown;
        event EventHandler<UnregisterBeginEventArgs> UnregisterBegin;
        event EventHandler<UnregisterCompleteEventArgs> UnregisterComplete;
    }
}