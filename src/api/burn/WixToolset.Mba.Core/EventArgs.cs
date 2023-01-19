// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Base class for BA <see cref="EventArgs"/> classes.
    /// </summary>
    [Serializable]
    public abstract class HResultEventArgs : EventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public HResultEventArgs()
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="HResult"/> of the operation. This is passed back to the engine.
        /// </summary>
        public int HResult { get; set; }
    }

    /// <summary>
    /// Base class for cancellable BA <see cref="EventArgs"/> classes.
    /// </summary>
    [Serializable]
    public abstract class CancellableHResultEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CancellableHResultEventArgs(bool cancelRecommendation)
        {
            this.Cancel = cancelRecommendation;
        }

        /// <summary>
        /// Gets or sets whether to cancel the operation. This is passed back to the engine.
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Base class for <see cref="EventArgs"/> classes that must return a <see cref="Result"/>.
    /// </summary>
    [Serializable]
    public abstract class ResultEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ResultEventArgs(Result recommendation, Result result)
        {
            this.Recommendation = recommendation;
            this.Result = result;
        }

        /// <summary>
        /// Gets the recommended <see cref="Result"/> of the operation.
        /// </summary>
        public Result Recommendation { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="Result"/> of the operation. This is passed back to the engine.
        /// </summary>
        public Result Result { get; set; }
    }

    /// <summary>
    /// Base class for <see cref="EventArgs"/> classes that receive status from the engine.
    /// </summary>
    [Serializable]
    public abstract class StatusEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public StatusEventArgs(int hrStatus)
        {
            this.Status = hrStatus;
        }

        /// <summary>
        /// Gets the return code of the operation.
        /// </summary>
        public int Status { get; private set; }
    }

    /// <summary>
    /// Base class for <see cref="EventArgs"/> classes that receive status from the engine and return an action.
    /// </summary>
    public abstract class ActionEventArgs<T> : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ActionEventArgs(int hrStatus, T recommendation, T action)
            : base(hrStatus)
        {
            this.Recommendation = recommendation;
            this.Action = action;
        }

        /// <summary>
        /// Gets the recommended action from the engine.
        /// </summary>
        public T Recommendation { get; private set; }

        /// <summary>
        /// Gets or sets the action to be performed. This is passed back to the engine.
        /// </summary>
        public T Action { get; set; }
    }

    /// <summary>
    /// Base class for cancellable action BA <see cref="EventArgs"/> classes.
    /// </summary>
    [Serializable]
    public abstract class CancellableActionEventArgs<T> : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CancellableActionEventArgs(bool cancelRecommendation, T recommendation, T action)
            : base(cancelRecommendation)
        {
            this.Recommendation = recommendation;
            this.Action = action;
        }

        /// <summary>
        /// Gets the recommended action from the engine.
        /// </summary>
        public T Recommendation { get; private set; }

        /// <summary>
        /// Gets or sets the action to be performed. This is passed back to the engine.
        /// </summary>
        public T Action { get; set; }
    }

    /// <summary>
    /// Base class for cache progress events.
    /// </summary>
    [Serializable]
    public abstract class CacheProgressBaseEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheProgressBaseEventArgs(string packageOrContainerId, string payloadId, long progress, long total, int overallPercentage, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
            this.Progress = progress;
            this.Total = total;
            this.OverallPercentage = overallPercentage;
        }

        /// <summary>
        /// Gets the identifier of the container or package.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }

        /// <summary>
        /// Gets the number of bytes cached thus far.
        /// </summary>
        public long Progress { get; private set; }

        /// <summary>
        /// Gets the total bytes to cache.
        /// </summary>
        public long Total { get; private set; }

        /// <summary>
        /// Gets the overall percentage of progress of caching.
        /// </summary>
        public int OverallPercentage { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.Startup"/>.
    /// </summary>
    [Serializable]
    public class StartupEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public StartupEventArgs()
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.Shutdown"/>.
    /// </summary>
    [Serializable]
    public class ShutdownEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ShutdownEventArgs(BOOTSTRAPPER_SHUTDOWN_ACTION action)
        {
            this.Action = action;
        }

        /// <summary>
        /// The action for OnShutdown.
        /// </summary>
        public BOOTSTRAPPER_SHUTDOWN_ACTION Action { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectBegin"/>
    /// </summary>
    [Serializable]
    public class DetectBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectBeginEventArgs(bool cached, RegistrationType registrationType, int packageCount, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.Cached = cached;
            this.RegistrationType = registrationType;
            this.PackageCount = packageCount;
        }

        /// <summary>
        /// Gets whether the bundle is cached.
        /// </summary>
        public bool Cached { get; private set; }

        /// <summary>
        /// Gets the bundle's registration state.
        /// </summary>
        public RegistrationType RegistrationType { get; private set; }

        /// <summary>
        /// Gets the number of packages to detect.
        /// </summary>
        public int PackageCount { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectForwardCompatibleBundle"/>
    /// </summary>
    [Serializable]
    public class DetectForwardCompatibleBundleEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectForwardCompatibleBundleEventArgs(string bundleId, RelationType relationType, string bundleTag, bool perMachine, string version, bool missingFromCache, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.BundleId = bundleId;
            this.RelationType = relationType;
            this.BundleTag = bundleTag;
            this.PerMachine = perMachine;
            this.Version = version;
            this.MissingFromCache = missingFromCache;
        }

        /// <summary>
        /// Gets the identity of the forward compatible bundle detected.
        /// </summary>
        public string BundleId { get; private set; }

        /// <summary>
        /// Gets the relationship type of the forward compatible bundle.
        /// </summary>
        public RelationType RelationType { get; private set; }

        /// <summary>
        /// Gets the tag of the forward compatible bundle.
        /// </summary>
        public string BundleTag { get; private set; }

        /// <summary>
        /// Gets whether the detected forward compatible bundle is per machine.
        /// </summary>
        public bool PerMachine { get; private set; }

        /// <summary>
        /// Gets the version of the forward compatible bundle detected.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Whether the forward compatible bundle is missing from the package cache.
        /// </summary>
        public bool MissingFromCache { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectUpdateBegin"/>
    /// </summary>
    [Serializable]
    public class DetectUpdateBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectUpdateBeginEventArgs(string updateLocation, bool cancelRecommendation, bool skipRecommendation)
            : base(cancelRecommendation)
        {
            this.UpdateLocation = updateLocation;
            this.Skip = skipRecommendation;
        }

        /// <summary>
        /// Gets the identity of the bundle to detect.
        /// </summary>
        public string UpdateLocation { get; private set; }

        /// <summary>
        /// Whether to skip checking for bundle updates.
        /// </summary>
        public bool Skip { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectUpdate"/>
    /// </summary>
    [Serializable]
    public class DetectUpdateEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectUpdateEventArgs(string updateLocation, long size, string hash, UpdateHashType hashAlgorithm, string version, string title, string summary, string contentType, string content, bool cancelRecommendation, bool stopRecommendation)
            : base(cancelRecommendation)
        {
            this.UpdateLocation = updateLocation;
            this.Size = size;
            this.Hash = hash;
            this.HashAlgorithm = hashAlgorithm;
            this.Version = version;
            this.Title = title;
            this.Summary = summary;
            this.ContentType = contentType;
            this.Content = content;
            this.StopProcessingUpdates = stopRecommendation;
        }

        /// <summary>
        /// Gets the identity of the bundle to detect.
        /// </summary>
        public string UpdateLocation { get; private set; }

        /// <summary>
        /// Gets the size of the updated bundle.
        /// </summary>
        public long Size { get; private set; }

        /// <summary>
        /// File hash of the updated bundle.
        /// </summary>
        public string Hash { get; }

        /// <summary>
        /// The algorithm of the updated bundle's hash.
        /// </summary>
        public UpdateHashType HashAlgorithm { get; }

        /// <summary>
        /// Gets the version of the updated bundle.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the title of the the updated bundle.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the summary of the updated bundle.
        /// </summary>
        public string Summary { get; private set; }

        /// <summary>
        /// Gets the content type of the content of the updated bundle.
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// Gets the content of the updated bundle.
        /// </summary>
        public string Content { get; private set; }

        /// <summary>
        /// Tells the engine to stop giving the rest of the updates found in the feed.
        /// </summary>
        public bool StopProcessingUpdates { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectUpdateComplete"/>
    /// </summary>
    [Serializable]
    public class DetectUpdateCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectUpdateCompleteEventArgs(int hrStatus, bool ignoreRecommendation)
            : base(hrStatus)
        {
            this.IgnoreError = ignoreRecommendation;
        }

        /// <summary>
        /// If Status is an error, then set this to true to ignore it and continue detecting.
        /// </summary>
        public bool IgnoreError { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectRelatedBundle"/>
    /// </summary>
    [Serializable]
    public class DetectRelatedBundleEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectRelatedBundleEventArgs(string productCode, RelationType relationType, string bundleTag, bool perMachine, string version, bool missingFromCache, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.ProductCode = productCode;
            this.RelationType = relationType;
            this.BundleTag = bundleTag;
            this.PerMachine = perMachine;
            this.Version = version;
            this.MissingFromCache = missingFromCache;
        }

        /// <summary>
        /// Gets the identity of the related bundle detected.
        /// </summary>
        public string ProductCode { get; private set; }

        /// <summary>
        /// Gets the relationship type of the related bundle.
        /// </summary>
        public RelationType RelationType { get; private set; }

        /// <summary>
        /// Gets the tag of the related package bundle.
        /// </summary>
        public string BundleTag { get; private set; }

        /// <summary>
        /// Gets whether the detected bundle is per machine.
        /// </summary>
        public bool PerMachine { get; private set; }

        /// <summary>
        /// Gets the version of the related bundle detected.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Whether the related bundle is missing from the package cache.
        /// </summary>
        public bool MissingFromCache { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectPackageBegin"/>
    /// </summary>
    [Serializable]
    public class DetectPackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectPackageBeginEventArgs(string packageId, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
        }

        /// <summary>
        /// Gets the identity of the package to detect.
        /// </summary>
        public string PackageId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectCompatibleMsiPackage"/>
    /// </summary>
    [Serializable]
    public class DetectCompatibleMsiPackageEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectCompatibleMsiPackageEventArgs(string packageId, string compatiblePackageId, string compatiblePackageVersion, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.CompatiblePackageId = compatiblePackageId;
            this.CompatiblePackageVersion = compatiblePackageVersion;
        }

        /// <summary>
        /// Gets the identity of the package that was not detected.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identity of the compatible package that was detected.
        /// </summary>
        public string CompatiblePackageId { get; private set; }

        /// <summary>
        /// Gets the version of the compatible package that was detected.
        /// </summary>
        public string CompatiblePackageVersion { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectRelatedMsiPackage"/>
    /// </summary>
    [Serializable]
    public class DetectRelatedMsiPackageEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectRelatedMsiPackageEventArgs(string packageId, string upgradeCode, string productCode, bool perMachine, string version, RelatedOperation operation, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.UpgradeCode = upgradeCode;
            this.ProductCode = productCode;
            this.PerMachine = perMachine;
            this.Version = version;
            this.Operation = operation;
        }

        /// <summary>
        /// Gets the identity of the product's package detected.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the upgrade code of the related package detected.
        /// </summary>
        public string UpgradeCode { get; private set; }

        /// <summary>
        /// Gets the identity of the related package detected.
        /// </summary>
        public string ProductCode { get; private set; }

        /// <summary>
        /// Gets whether the detected package is per machine.
        /// </summary>
        public bool PerMachine { get; private set; }

        /// <summary>
        /// Gets the version of the related package detected.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the operation that will be taken on the detected package.
        /// </summary>
        public RelatedOperation Operation { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectPatchTarget"/>
    /// </summary>
    public class DetectPatchTargetEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectPatchTargetEventArgs(string packageId, string productCode, PackageState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ProductCode = productCode;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the patch's package.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the product code of the target.
        /// </summary>
        public string ProductCode { get; private set; }

        /// <summary>
        /// Gets the detected patch state for the target.
        /// </summary>
        public PackageState State { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectMsiFeature"/>
    /// </summary>
    public class DetectMsiFeatureEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectMsiFeatureEventArgs(string packageId, string featureId, FeatureState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.FeatureId = featureId;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the feature's package detected.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identity of the feature detected.
        /// </summary>
        public string FeatureId { get; private set; }

        /// <summary>
        /// Gets the detected feature state.
        /// </summary>
        public FeatureState State { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectPackageComplete"/>.
    /// </summary>
    [Serializable]
    public class DetectPackageCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectPackageCompleteEventArgs(string packageId, int hrStatus, PackageState state, bool cached)
            : base(hrStatus)
        {
            this.PackageId = packageId;
            this.State = state;
            this.Cached = cached;
        }

        /// <summary>
        /// Gets the identity of the package detected.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the state of the specified package.
        /// </summary>
        public PackageState State { get; private set; }

        /// <summary>
        /// Gets whether any part of the package is cached.
        /// </summary>
        public bool Cached { get; private set; }
    }

    /// <summary>
    /// Event arguments used when the detection phase has completed.
    /// </summary>
    [Serializable]
    public class DetectCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectCompleteEventArgs(int hrStatus, bool eligibleForCleanup)
            : base(hrStatus)
        {
            this.EligibleForCleanup = eligibleForCleanup;
        }

        /// <summary>
        /// Indicates whether the engine will uninstall the bundle if shutdown without running Apply.
        /// </summary>
        public bool EligibleForCleanup { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanBegin"/>
    /// </summary>
    [Serializable]
    public class PlanBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanBeginEventArgs(int packageCount, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageCount = packageCount;
        }

        /// <summary>
        /// Gets the number of packages to plan for.
        /// </summary>
        public int PackageCount { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanRelatedBundle"/>
    /// </summary>
    [Serializable]
    public class PlanRelatedBundleEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanRelatedBundleEventArgs(string bundleId, RequestState recommendedState, RequestState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.BundleId = bundleId;
            this.RecommendedState = recommendedState;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the bundle to plan for.
        /// </summary>
        public string BundleId { get; private set; }

        /// <summary>
        /// Gets the recommended requested state for the bundle.
        /// </summary>
        public RequestState RecommendedState { get; private set; }

        /// <summary>
        /// Gets or sets the requested state for the bundle.
        /// </summary>
        public RequestState State { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanRelatedBundleType"/>
    /// </summary>
    [Serializable]
    public class PlanRelatedBundleTypeEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanRelatedBundleTypeEventArgs(string bundleId, RelatedBundlePlanType recommendedType, RelatedBundlePlanType type, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.BundleId = bundleId;
            this.RecommendedType = recommendedType;
            this.Type = type;
        }

        /// <summary>
        /// Gets the identity of the bundle to plan for.
        /// </summary>
        public string BundleId { get; private set; }

        /// <summary>
        /// Gets the recommended plan type for the bundle.
        /// </summary>
        public RelatedBundlePlanType RecommendedType { get; private set; }

        /// <summary>
        /// Gets or sets the plan type for the bundle.
        /// </summary>
        public RelatedBundlePlanType Type { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanPackageBegin"/>
    /// </summary>
    [Serializable]
    public class PlanPackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanPackageBeginEventArgs(string packageId, PackageState currentState, bool cached, BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition, BOOTSTRAPPER_PACKAGE_CONDITION_RESULT repairCondition, RequestState recommendedState, BOOTSTRAPPER_CACHE_TYPE recommendedCacheType, RequestState state, BOOTSTRAPPER_CACHE_TYPE cacheType, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.CurrentState = currentState;
            this.Cached = cached;
            this.InstallCondition = installCondition;
            this.RepairCondition = repairCondition;
            this.RecommendedState = recommendedState;
            this.RecommendedCacheType = recommendedCacheType;
            this.State = state;
            this.CacheType = cacheType;
        }

        /// <summary>
        /// Gets the identity of the package to plan for.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the current state of the package.
        /// </summary>
        public PackageState CurrentState { get; private set; }

        /// <summary>
        /// Gets whether any part of the package is cached.
        /// </summary>
        public bool Cached { get; private set; }

        /// <summary>
        /// Gets the evaluated result of the package's install condition.
        /// </summary>
        public BOOTSTRAPPER_PACKAGE_CONDITION_RESULT InstallCondition { get; private set; }

        /// <summary>
        /// Gets the evaluated result of the package's repair condition.
        /// </summary>
        public BOOTSTRAPPER_PACKAGE_CONDITION_RESULT RepairCondition { get; private set; }

        /// <summary>
        /// Gets the recommended requested state for the package.
        /// </summary>
        public RequestState RecommendedState { get; private set; }

        /// <summary>
        /// The authored cache type of the package.
        /// </summary>
        public BOOTSTRAPPER_CACHE_TYPE RecommendedCacheType { get; private set; }

        /// <summary>
        /// Gets or sets the requested state for the package.
        /// </summary>
        public RequestState State { get; set; }

        /// <summary>
        /// Gets or sets the requested cache type for the package.
        /// </summary>
        public BOOTSTRAPPER_CACHE_TYPE CacheType { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanCompatibleMsiPackageBegin"/>
    /// </summary>
    [Serializable]
    public class PlanCompatibleMsiPackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanCompatibleMsiPackageBeginEventArgs(string packageId, string compatiblePackageId, string compatiblePackageVersion, bool recommendedRemove, bool requestRemove, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.CompatiblePackageId = compatiblePackageId;
            this.CompatiblePackageVersion = compatiblePackageVersion;
            this.RecommendedRemove = recommendedRemove;
            this.RequestRemove = requestRemove;
        }

        /// <summary>
        /// Gets the identity of the package that was not detected.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identity of the compatible package detected.
        /// </summary>
        public string CompatiblePackageId { get; private set; }

        /// <summary>
        /// Gets the version of the compatible package detected.
        /// </summary>
        public string CompatiblePackageVersion { get; private set; }

        /// <summary>
        /// Gets the recommended state to use for the compatible package for planning.
        /// </summary>
        public bool RecommendedRemove { get; private set; }

        /// <summary>
        /// Gets or sets whether to uninstall the compatible package.
        /// </summary>
        public bool RequestRemove { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanCompatibleMsiPackageComplete"/>
    /// </summary>
    [Serializable]
    public class PlanCompatibleMsiPackageCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanCompatibleMsiPackageCompleteEventArgs(string packageId, string compatiblePackageId, int hrStatus, bool requestedRemove)
            : base(hrStatus)
        {
            this.PackageId = packageId;
            this.CompatiblePackageId = compatiblePackageId;
            this.RequestedRemove = requestedRemove;
        }

        /// <summary>
        /// Gets the identity of the package planned for.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identity of the compatible package detected.
        /// </summary>
        public string CompatiblePackageId { get; private set; }

        /// <summary>
        /// Gets the requested state of the package.
        /// </summary>
        public bool RequestedRemove { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanRollbackBoundary"/>
    /// </summary>
    [Serializable]
    public class PlanRollbackBoundaryEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanRollbackBoundaryEventArgs(string rollbackBoundaryId, bool recommendedTransaction, bool transaction, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.RollbackBoundaryId = rollbackBoundaryId;
            this.RecommendedTransaction = recommendedTransaction;
            this.Transaction = transaction;
        }

        /// <summary>
        /// Gets the identity of the rollback boundary to plan for.
        /// </summary>
        public string RollbackBoundaryId { get; private set; }

        /// <summary>
        /// Whether or not the rollback boundary was authored to use an MSI transaction.
        /// </summary>
        public bool RecommendedTransaction { get; private set; }

        /// <summary>
        /// Whether or not an MSI transaction will be used in the rollback boundary.
        /// If <see cref="RecommendedTransaction"/> is false, setting the value to true has no effect.
        /// If <see cref="RecommendedTransaction"/> is true, setting the value to false will cause the packages inside this rollback boundary to be executed without a wrapping MSI transaction.
        /// </summary>
        public bool Transaction { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanPatchTarget"/>
    /// </summary>
    [Serializable]
    public class PlanPatchTargetEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanPatchTargetEventArgs(string packageId, string productCode, RequestState recommendedState, RequestState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ProductCode = productCode;
            this.RecommendedState = recommendedState;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the patch's package.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the product code of the target.
        /// </summary>
        public string ProductCode { get; private set; }

        /// <summary>
        /// Gets the recommended state of the patch to use by planning for the target.
        /// </summary>
        public RequestState RecommendedState { get; private set; }

        /// <summary>
        /// Gets or sets the state of the patch to use by planning for the target.
        /// </summary>
        public RequestState State { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanMsiFeature"/>
    /// </summary>
    [Serializable]
    public class PlanMsiFeatureEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanMsiFeatureEventArgs(string packageId, string featureId, FeatureState recommendedState, FeatureState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.FeatureId = featureId;
            this.RecommendedState = recommendedState;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the feature's package to plan.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identity of the feature to plan.
        /// </summary>
        public string FeatureId { get; private set; }

        /// <summary>
        /// Gets the recommended feature state to use by planning.
        /// </summary>
        public FeatureState RecommendedState { get; private set; }

        /// <summary>
        /// Gets or sets the feature state to use by planning.
        /// </summary>
        public FeatureState State { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanMsiPackage"/>
    /// </summary>
    [Serializable]
    public class PlanMsiPackageEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanMsiPackageEventArgs(string packageId, bool shouldExecute, ActionState action, BOOTSTRAPPER_MSI_FILE_VERSIONING recommendedFileVersioning, bool cancelRecommendation, BURN_MSI_PROPERTY actionMsiProperty, INSTALLUILEVEL uiLevel, bool disableExternalUiHandler, BOOTSTRAPPER_MSI_FILE_VERSIONING fileVersioning)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ShouldExecute = shouldExecute;
            this.Action = action;
            this.RecommendedFileVersioning = recommendedFileVersioning;
            this.ActionMsiProperty = actionMsiProperty;
            this.UiLevel = uiLevel;
            this.DisableExternalUiHandler = disableExternalUiHandler;
            this.FileVersioning = fileVersioning;
        }

        /// <summary>
        /// Gets identity of the package planned for.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets whether the package is planned to execute or roll back.
        /// </summary>
        public bool ShouldExecute { get; private set; }

        /// <summary>
        /// Gets the action planned for the package.
        /// </summary>
        public ActionState Action { get; private set; }

        /// <summary>
        /// Gets the recommended file versioning for the package.
        /// </summary>
        public BOOTSTRAPPER_MSI_FILE_VERSIONING RecommendedFileVersioning { get; private set; }

        /// <summary>
        /// Gets or sets the requested MSI property to add.
        /// </summary>
        public BURN_MSI_PROPERTY ActionMsiProperty { get; set; }

        /// <summary>
        /// Gets or sets the requested internal UI level.
        /// </summary>
        public INSTALLUILEVEL UiLevel { get; set; }

        /// <summary>
        /// Gets or sets whether Burn is requested to set up an external UI handler.
        /// </summary>
        public bool DisableExternalUiHandler { get; set; }

        /// <summary>
        /// Gets or sets the requested file versioning.
        /// </summary>
        public BOOTSTRAPPER_MSI_FILE_VERSIONING FileVersioning { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanPackageComplete"/>
    /// </summary>
    [Serializable]
    public class PlanPackageCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanPackageCompleteEventArgs(string packageId, int hrStatus, RequestState requested)
            : base(hrStatus)
        {
            this.PackageId = packageId;
            this.Requested = requested;
        }

        /// <summary>
        /// Gets the identity of the package planned for.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the requested state for the package.
        /// </summary>
        public RequestState Requested { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlannedCompatiblePackage"/>
    /// </summary>
    [Serializable]
    public class PlannedCompatiblePackageEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlannedCompatiblePackageEventArgs(string packageId, string compatiblePackageId, bool remove)
        {
            this.PackageId = packageId;
            this.CompatiblePackageId = compatiblePackageId;
            this.Remove = remove;
        }

        /// <summary>
        /// Gets the identity of the package planned for.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identity of the compatible package detected.
        /// </summary>
        public string CompatiblePackageId { get; private set; }

        /// <summary>
        /// Gets the planned state of the package.
        /// </summary>
        public bool Remove { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlannedPackage"/>
    /// </summary>
    [Serializable]
    public class PlannedPackageEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlannedPackageEventArgs(string packageId, ActionState execute, ActionState rollback, bool cache, bool uncache)
        {
            this.PackageId = packageId;
            this.Execute = execute;
            this.Rollback = rollback;
            this.Cache = cache;
            this.Uncache = uncache;
        }

        /// <summary>
        /// Gets the identity of the package planned for.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the planned execution action.
        /// </summary>
        public ActionState Execute { get; private set; }

        /// <summary>
        /// Gets the planned rollback action.
        /// </summary>
        public ActionState Rollback { get; private set; }

        /// <summary>
        /// Gets whether the package will be cached.
        /// </summary>
        public bool Cache { get; private set; }

        /// <summary>
        /// Gets whether the package will be removed from the package cache.
        /// </summary>
        public bool Uncache { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanComplete"/>.
    /// </summary>
    [Serializable]
    public class PlanCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanForwardCompatibleBundle"/>
    /// </summary>
    [Serializable]
    public class PlanForwardCompatibleBundleEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanForwardCompatibleBundleEventArgs(string bundleId, RelationType relationType, string bundleTag, bool perMachine, string version, bool recommendedIgnoreBundle, bool cancelRecommendation, bool ignoreBundle)
            : base(cancelRecommendation)
        {
            this.BundleId = bundleId;
            this.RelationType = relationType;
            this.BundleTag = bundleTag;
            this.PerMachine = perMachine;
            this.Version = version;
            this.RecommendedIgnoreBundle = recommendedIgnoreBundle;
            this.IgnoreBundle = ignoreBundle;
        }

        /// <summary>
        /// Gets the identity of the forward compatible bundle detected.
        /// </summary>
        public string BundleId { get; private set; }

        /// <summary>
        /// Gets the relationship type of the forward compatible bundle.
        /// </summary>
        public RelationType RelationType { get; private set; }

        /// <summary>
        /// Gets the tag of the forward compatible bundle.
        /// </summary>
        public string BundleTag { get; private set; }

        /// <summary>
        /// Gets whether the forward compatible bundle is per machine.
        /// </summary>
        public bool PerMachine { get; private set; }

        /// <summary>
        /// Gets the version of the forward compatible bundle.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Gets the recommendation of whether the engine should use the forward compatible bundle.
        /// </summary>
        public bool RecommendedIgnoreBundle { get; set; }

        /// <summary>
        /// Gets or sets whether the engine will use the forward compatible bundle.
        /// </summary>
        public bool IgnoreBundle { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ApplyBegin"/>
    /// </summary>
    [Serializable]
    public class ApplyBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ApplyBeginEventArgs(int phaseCount, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PhaseCount = phaseCount;
        }

        /// <summary>
        /// Gets the number of phases that the engine will go through in apply.
        /// There are currently two possible phases: cache and execute.
        /// </summary>
        public int PhaseCount { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ElevateBegin"/>
    /// </summary>
    [Serializable]
    public class ElevateBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ElevateBeginEventArgs(bool cancelRecommendation)
            : base(cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ElevateComplete"/>.
    /// </summary>
    [Serializable]
    public class ElevateCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ElevateCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.Progress"/>
    /// </summary>
    [Serializable]
    public class ProgressEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ProgressEventArgs(int progressPercentage, int overallPercentage, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.ProgressPercentage = progressPercentage;
            this.OverallPercentage = overallPercentage;
        }

        /// <summary>
        /// Gets the percentage from 0 to 100 completed for a package.
        /// </summary>
        public int ProgressPercentage { get; private set; }

        /// <summary>
        /// Gets the percentage from 0 to 100 completed for the bundle.
        /// </summary>
        public int OverallPercentage { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.Error"/>
    /// </summary>
    [Serializable]
    public class ErrorEventArgs : ResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ErrorEventArgs(ErrorType errorType, string packageId, int errorCode, string errorMessage, int dwUIHint, string[] data, Result recommendation, Result result)
            : base(recommendation, result)
        {
            this.ErrorType = errorType;
            this.PackageId = packageId;
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
            this.UIHint = dwUIHint;
            this.Data = new ReadOnlyCollection<string>(data ?? new string[] { });
        }

        /// <summary>
        /// Gets the type of error that occurred.
        /// </summary>
        public ErrorType ErrorType { get; private set; }

        /// <summary>
        /// Gets the identity of the package that yielded the error.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the error code.
        /// </summary>
        public int ErrorCode { get; private set; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the recommended display flags for an error dialog.
        /// </summary>
        public int UIHint { get; private set; }

        /// <summary>
        /// Gets the extended data for the error.
        /// </summary>
        public IList<string> Data { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.RegisterBegin"/>
    /// </summary>
    [Serializable]
    public class RegisterBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public RegisterBeginEventArgs(RegistrationType recommendedRegistrationType, bool cancelRecommendation, RegistrationType registrationType)
            : base(cancelRecommendation)
        {
            this.RecommendedRegistrationType = recommendedRegistrationType;
            this.RegistrationType = registrationType;
        }

        /// <summary>
        /// Gets the recommended registration type.
        /// </summary>
        public RegistrationType RecommendedRegistrationType { get; private set; }

        /// <summary>
        /// Gets or sets the registration type.
        /// </summary>
        public RegistrationType RegistrationType { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.RegisterComplete"/>.
    /// </summary>
    [Serializable]
    public class RegisterCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public RegisterCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.UnregisterBegin"/>
    /// </summary>
    [Serializable]
    public class UnregisterBeginEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public UnregisterBeginEventArgs(RegistrationType recommendedRegistrationType, RegistrationType registrationType)
        {
            this.RecommendedRegistrationType = recommendedRegistrationType;
            this.RegistrationType = registrationType;
        }

        /// <summary>
        /// Gets the recommended registration type.
        /// </summary>
        public RegistrationType RecommendedRegistrationType { get; private set; }

        /// <summary>
        /// Gets or sets the registration type.
        /// </summary>
        public RegistrationType RegistrationType { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.UnregisterComplete"/>
    /// </summary>
    [Serializable]
    public class UnregisterCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public UnregisterCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CacheBegin"/>
    /// </summary>
    [Serializable]
    public class CacheBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheBeginEventArgs(bool cancelRecommendation)
            : base(cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CacheAcquireBegin"/>.
    /// </summary>
    [Serializable]
    public class CacheAcquireBeginEventArgs : CancellableActionEventArgs<CacheOperation>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheAcquireBeginEventArgs(string packageOrContainerId, string payloadId, string source, string downloadUrl, string payloadContainerId, CacheOperation recommendation, CacheOperation action, bool cancelRecommendation)
            : base(cancelRecommendation, recommendation, action)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
            this.Source = source;
            this.DownloadUrl = downloadUrl;
            this.PayloadContainerId = payloadContainerId;
        }

        /// <summary>
        /// Gets the identifier of the container or package.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload (if acquiring a payload).
        /// </summary>
        public string PayloadId { get; private set; }

        /// <summary>
        /// Gets the source of the container or payload.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Gets the optional URL to download container or payload.
        /// </summary>
        public string DownloadUrl { get; private set; }

        /// <summary>
        /// Gets the optional identity of the container that contains the payload being acquired.
        /// </summary>
        public string PayloadContainerId { get; private set; }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CacheAcquireProgress"/>.
    /// </summary>
    [Serializable]
    public class CacheAcquireProgressEventArgs : CacheProgressBaseEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheAcquireProgressEventArgs(string packageOrContainerId, string payloadId, long progress, long total, int overallPercentage, bool cancelRecommendation)
            : base(packageOrContainerId, payloadId, progress, total, overallPercentage, cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CacheAcquireComplete"/>.
    /// </summary>
    [Serializable]
    public class CacheAcquireCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheAcquireCompleteEventArgs(string packageOrContainerId, string payloadId, int hrStatus, BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION recommendation, BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the container or package.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload (if acquiring a payload).
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CacheVerifyBegin"/>.
    /// </summary>
    [Serializable]
    public class CacheVerifyBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheVerifyBeginEventArgs(string packageOrContainerId, string payloadId, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the container or package.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CacheVerifyProgress"/>.
    /// </summary>
    [Serializable]
    public class CacheVerifyProgressEventArgs : CacheProgressBaseEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheVerifyProgressEventArgs(string packageOrContainerId, string payloadId, long progress, long total, int overallPercentage, CacheVerifyStep verifyStep, bool cancelRecommendation)
            : base(packageOrContainerId, payloadId, progress, total, overallPercentage, cancelRecommendation)
        {
            this.Step = verifyStep;
        }

        /// <summary>
        /// Gets the current verification step.
        /// </summary>
        public CacheVerifyStep Step { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CacheVerifyComplete"/>
    /// </summary>
    [Serializable]
    public class CacheVerifyCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheVerifyCompleteEventArgs(string packageOrContainerId, string payloadId, int hrStatus, BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION recommendation, BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the container or package.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CacheComplete"/>.
    /// </summary>
    [Serializable]
    public class CacheCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecuteBegin"/>
    /// </summary>
    [Serializable]
    public class ExecuteBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecuteBeginEventArgs(int packageCount, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageCount = packageCount;
        }

        /// <summary>
        /// Gets the number of packages to act on.
        /// </summary>
        public int PackageCount { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecutePackageBegin"/>
    /// </summary>
    [Serializable]
    public class ExecutePackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecutePackageBeginEventArgs(string packageId, bool shouldExecute, ActionState action, INSTALLUILEVEL uiLevel, bool disableExternalUiHandler, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ShouldExecute = shouldExecute;
            this.Action = action;
            this.UiLevel = uiLevel;
            this.DisableExternalUiHandler = disableExternalUiHandler;
        }

        /// <summary>
        /// Gets the identity of the package to act on.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets whether the package is being executed or rolled back.
        /// </summary>
        public bool ShouldExecute { get; private set; }

        /// <summary>
        /// Gets the action about to be executed.
        /// </summary>
        public ActionState Action { get; private set; }

        /// <summary>
        /// Gets the internal UI level (if this is an MSI or MSP package).
        /// </summary>
        public INSTALLUILEVEL UiLevel { get; private set; }

        /// <summary>
        /// Gets whether Burn will set up an external UI handler (if this is an MSI or MSP package).
        /// </summary>
        public bool DisableExternalUiHandler { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecutePatchTarget"/>
    /// </summary>
    [Serializable]
    public class ExecutePatchTargetEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecutePatchTargetEventArgs(string packageId, string targetProductCode, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.TargetProductCode = targetProductCode;
        }

        /// <summary>
        /// Gets the identity of the package to act on.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the product code being targeted.
        /// </summary>
        public string TargetProductCode { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecuteMsiMessage"/>
    /// </summary>
    [Serializable]
    public class ExecuteMsiMessageEventArgs : ResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecuteMsiMessageEventArgs(string packageId, InstallMessage messageType, int dwUIHint, string message, string[] data, Result recommendation, Result result)
            : base(recommendation, result)
        {
            this.PackageId = packageId;
            this.MessageType = messageType;
            this.UIHint = dwUIHint;
            this.Message = message;
            this.Data = new ReadOnlyCollection<string>(data ?? new string[] { });
        }

        /// <summary>
        /// Gets the identity of the package that yielded this message.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the type of this message.
        /// </summary>
        public InstallMessage MessageType { get; private set; }

        /// <summary>
        /// Gets the recommended display flags for this message.
        /// </summary>
        public int UIHint { get; private set; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the extended data for the message.
        /// </summary>
        public IList<string> Data { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecuteFilesInUse"/>
    /// </summary>
    [Serializable]
    public class ExecuteFilesInUseEventArgs : ResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecuteFilesInUseEventArgs(string packageId, string[] files, Result recommendation, FilesInUseType source, Result result)
            : base(recommendation, result)
        {
            this.PackageId = packageId;
            this.Files = new ReadOnlyCollection<string>(files ?? new string[] { });
            this.Source = source;
        }

        /// <summary>
        /// Gets the identity of the package that yielded the files in use message.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the list of files in use.
        /// </summary>
        public IList<string> Files { get; private set; }

        /// <summary>
        /// Gets the source of the message.
        /// </summary>
        public FilesInUseType Source { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecutePackageComplete"/>
    /// </summary>
    [Serializable]
    public class ExecutePackageCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecutePackageCompleteEventArgs(string packageId, int hrStatus, ApplyRestart restart, BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION recommendation, BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.PackageId = packageId;
            this.Restart = restart;
        }

        /// <summary>
        /// Gets the identity of the package that was acted on.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the package restart state after being applied.
        /// </summary>
        public ApplyRestart Restart { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecuteComplete"/>.
    /// </summary>
    [Serializable]
    public class ExecuteCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecuteCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ApplyComplete"/>
    /// </summary>
    [Serializable]
    public class ApplyCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_APPLYCOMPLETE_ACTION>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ApplyCompleteEventArgs(int hrStatus, ApplyRestart restart, BOOTSTRAPPER_APPLYCOMPLETE_ACTION recommendation, BOOTSTRAPPER_APPLYCOMPLETE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.Restart = restart;
        }

        /// <summary>
        /// Gets the apply restart state when complete.
        /// </summary>
        public ApplyRestart Restart { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ApplyDowngrade"/>
    /// </summary>
    [Serializable]
    public class ApplyDowngradeEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ApplyDowngradeEventArgs(int hrRecommendation, int hrStatus)
        {
            this.Recommendation = hrRecommendation;
            this.Status = hrStatus;
        }

        /// <summary>
        /// Gets the recommended HRESULT.
        /// </summary>
        public int Recommendation { get; private set; }

        /// <summary>
        /// Gets or sets the HRESULT for Apply.
        /// </summary>
        public int Status { get; set; }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CacheAcquireResolving"/>.
    /// </summary>
    [Serializable]
    public class CacheAcquireResolvingEventArgs : CancellableActionEventArgs<CacheResolveOperation>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheAcquireResolvingEventArgs(string packageOrContainerId, string payloadId, string[] searchPaths, bool foundLocal, int recommendedSearchPath, string downloadUrl, string payloadContainerId, CacheResolveOperation recommendation, int chosenSearchPath, CacheResolveOperation action, bool cancel)
            : base(cancel, recommendation, action)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
            this.SearchPaths = searchPaths;
            this.FoundLocal = foundLocal;
            this.RecommendedSearchPath = recommendedSearchPath;
            this.DownloadUrl = downloadUrl;
            this.PayloadContainerId = payloadContainerId;
            this.ChosenSearchPath = chosenSearchPath;
        }

        /// <summary>
        /// Gets the identity of the package or container that is being acquired.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identity of the payload that is being acquired.
        /// </summary>
        public string PayloadId { get; private set; }

        /// <summary>
        /// Gets the search paths used for source resolution.
        /// </summary>
        public string[] SearchPaths { get; private set; }

        /// <summary>
        /// Gets whether <see cref="RecommendedSearchPath"/> indicates that a file was found at that search path.
        /// </summary>
        public bool FoundLocal { get; private set; }

        /// <summary>
        /// When <see cref="FoundLocal"/> is true, the index to <see cref="SearchPaths"/> for the recommended local file.
        /// </summary>
        public int RecommendedSearchPath { get; private set; }

        /// <summary>
        /// Gets the optional URL to download container or payload.
        /// </summary>
        public string DownloadUrl { get; private set; }

        /// <summary>
        /// Gets the optional identity of the container that contains the payload being acquired.
        /// </summary>
        public string PayloadContainerId { get; private set; }

        /// <summary>
        /// Gets or sets the index to <see cref="SearchPaths"/> to use when <see cref="CancellableActionEventArgs{T}.Action"/> is set to <see cref="CacheOperation.Copy"/>.
        /// </summary>
        public int ChosenSearchPath { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CachePackageBegin"/>
    /// </summary>
    [Serializable]
    public class CachePackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CachePackageBeginEventArgs(string packageId, int cachePayloads, long packageCacheSize, bool vital, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.CachePayloads = cachePayloads;
            this.PackageCacheSize = packageCacheSize;
            this.Vital = vital;
        }

        /// <summary>
        /// Gets the identity of the package that is being cached.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets number of payloads to be cached.
        /// </summary>
        public long CachePayloads { get; private set; }

        /// <summary>
        /// Gets the size on disk required by the specific package.
        /// </summary>
        public long PackageCacheSize { get; private set; }

        /// <summary>
        /// If caching a package is not vital, then acquisition will be skipped unless the BA opts in through <see cref="IDefaultBootstrapperApplication.CachePackageNonVitalValidationFailure"/>.
        /// </summary>
        public bool Vital { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CachePackageComplete"/>
    /// </summary>
    [Serializable]
    public class CachePackageCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CachePackageCompleteEventArgs(string packageId, int hrStatus, BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION recommendation, BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.PackageId = packageId;
        }

        /// <summary>
        /// Gets the identity of the package that was cached.
        /// </summary>
        public string PackageId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecuteProgress"/>
    /// </summary>
    [Serializable]
    public class ExecuteProgressEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecuteProgressEventArgs(string packageId, int progressPercentage, int overallPercentage, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ProgressPercentage = progressPercentage;
            this.OverallPercentage = overallPercentage;
        }

        /// <summary>
        /// Gets the identity of the package that was executed.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the percentage from 0 to 100 of the execution progress for a single payload.
        /// </summary>
        public int ProgressPercentage { get; private set; }

        /// <summary>
        /// Gets the percentage from 0 to 100 of the execution progress for all payloads.
        /// </summary>
        public int OverallPercentage { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.LaunchApprovedExeBegin"/>.
    /// </summary>
    [Serializable]
    public class LaunchApprovedExeBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public LaunchApprovedExeBeginEventArgs(bool cancelRecommendation)
            : base(cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.LaunchApprovedExeComplete"/>.
    /// </summary>
    [Serializable]
    public class LaunchApprovedExeCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public LaunchApprovedExeCompleteEventArgs(int hrStatus, int processId)
            : base(hrStatus)
        {
            this.ProcessId = processId;
        }

        /// <summary>
        /// Gets the ProcessId of the process that was launched.
        /// This is only valid if the status reports success.
        /// </summary>
        public int ProcessId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.BeginMsiTransactionBegin"/>.
    /// </summary>
    [Serializable]
    public class BeginMsiTransactionBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public BeginMsiTransactionBeginEventArgs(string transactionId, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.TransactionId = transactionId;
        }

        /// <summary>
        /// Gets the MSI transaction Id.
        /// </summary>
        public string TransactionId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.BeginMsiTransactionComplete"/>.
    /// </summary>
    [Serializable]
    public class BeginMsiTransactionCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public BeginMsiTransactionCompleteEventArgs(string transactionId, int hrStatus)
            : base(hrStatus)
        {
            this.TransactionId = transactionId;
        }

        /// <summary>
        /// Gets the MSI transaction Id.
        /// </summary>
        public string TransactionId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CommitMsiTransactionBegin"/>.
    /// </summary>
    [Serializable]
    public class CommitMsiTransactionBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CommitMsiTransactionBeginEventArgs(string transactionId, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.TransactionId = transactionId;
        }

        /// <summary>
        /// Gets the MSI transaction Id.
        /// </summary>
        public string TransactionId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CommitMsiTransactionComplete"/>.
    /// </summary>
    [Serializable]
    public class CommitMsiTransactionCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CommitMsiTransactionCompleteEventArgs(string transactionId, int hrStatus, ApplyRestart restart, BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION recommendation, BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.TransactionId = transactionId;
            this.Restart = restart;
        }

        /// <summary>
        /// Gets the MSI transaction Id.
        /// </summary>
        public string TransactionId { get; private set; }

        /// <summary>
        /// Gets the package restart state after being applied.
        /// </summary>
        public ApplyRestart Restart { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.RollbackMsiTransactionBegin"/>.
    /// </summary>
    [Serializable]
    public class RollbackMsiTransactionBeginEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public RollbackMsiTransactionBeginEventArgs(string transactionId)
        {
            this.TransactionId = transactionId;
        }

        /// <summary>
        /// Gets the MSI transaction Id.
        /// </summary>
        public string TransactionId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.RollbackMsiTransactionComplete"/>.
    /// </summary>
    [Serializable]
    public class RollbackMsiTransactionCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public RollbackMsiTransactionCompleteEventArgs(string transactionId, int hrStatus, ApplyRestart restart, BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION recommendation, BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.TransactionId = transactionId;
            this.Restart = restart;
        }

        /// <summary>
        /// Gets the MSI transaction Id.
        /// </summary>
        public string TransactionId { get; private set; }

        /// <summary>
        /// Gets the package restart state after being applied.
        /// </summary>
        public ApplyRestart Restart { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PauseAutomaticUpdatesBegin"/>.
    /// </summary>
    [Serializable]
    public class PauseAutomaticUpdatesBeginEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PauseAutomaticUpdatesBeginEventArgs()
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PauseAutomaticUpdatesComplete"/>.
    /// </summary>
    [Serializable]
    public class PauseAutomaticUpdatesCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PauseAutomaticUpdatesCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.SystemRestorePointBegin"/>.
    /// </summary>
    [Serializable]
    public class SystemRestorePointBeginEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public SystemRestorePointBeginEventArgs()
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.SystemRestorePointComplete"/>.
    /// </summary>
    [Serializable]
    public class SystemRestorePointCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public SystemRestorePointCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CacheContainerOrPayloadVerifyBegin"/>.
    /// </summary>
    [Serializable]
    public class CacheContainerOrPayloadVerifyBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheContainerOrPayloadVerifyBeginEventArgs(string packageOrContainerId, string payloadId, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the container or package.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CacheContainerOrPayloadVerifyProgress"/>.
    /// </summary>
    [Serializable]
    public class CacheContainerOrPayloadVerifyProgressEventArgs : CacheProgressBaseEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheContainerOrPayloadVerifyProgressEventArgs(string packageOrContainerId, string payloadId, long progress, long total, int overallPercentage, bool cancelRecommendation)
            : base(packageOrContainerId, payloadId, progress, total, overallPercentage, cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CacheContainerOrPayloadVerifyComplete"/>
    /// </summary>
    [Serializable]
    public class CacheContainerOrPayloadVerifyCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CacheContainerOrPayloadVerifyCompleteEventArgs(string packageOrContainerId, string payloadId, int hrStatus)
            : base(hrStatus)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the container or package.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CachePayloadExtractBegin"/>.
    /// </summary>
    [Serializable]
    public class CachePayloadExtractBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CachePayloadExtractBeginEventArgs(string containerId, string payloadId, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.ContainerId = containerId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the container.
        /// </summary>
        public string ContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.CachePayloadExtractProgress"/>.
    /// </summary>
    [Serializable]
    public class CachePayloadExtractProgressEventArgs : CacheProgressBaseEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CachePayloadExtractProgressEventArgs(string containerId, string payloadId, long progress, long total, int overallPercentage, bool cancelRecommendation)
            : base(containerId, payloadId, progress, total, overallPercentage, cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CachePayloadExtractComplete"/>
    /// </summary>
    [Serializable]
    public class CachePayloadExtractCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CachePayloadExtractCompleteEventArgs(string containerId, string payloadId, int hrStatus)
            : base(hrStatus)
        {
            this.ContainerId = containerId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the container.
        /// </summary>
        public string ContainerId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// EventArgs for <see cref="IDefaultBootstrapperApplication.SetUpdateBegin"/>.
    /// </summary>
    [Serializable]
    public class SetUpdateBeginEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public SetUpdateBeginEventArgs()
        {
        }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.SetUpdateComplete"/>
    /// </summary>
    [Serializable]
    public class SetUpdateCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public SetUpdateCompleteEventArgs(int hrStatus, string previousPackageId, string newPackageId)
            : base(hrStatus)
        {
            this.PreviousPackageId = previousPackageId;
            this.NewPackageId = newPackageId;
        }

        /// <summary>
        /// Gets the identifier of the update package that was removed.
        /// </summary>
        public string PreviousPackageId { get; private set; }

        /// <summary>
        /// Gets the identifier of the update package that was added.
        /// </summary>
        public string NewPackageId { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.PlanRestoreRelatedBundle"/>
    /// </summary>
    [Serializable]
    public class PlanRestoreRelatedBundleEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public PlanRestoreRelatedBundleEventArgs(string bundleId, RequestState recommendedState, RequestState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.BundleId = bundleId;
            this.RecommendedState = recommendedState;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the bundle to plan for.
        /// </summary>
        public string BundleId { get; private set; }

        /// <summary>
        /// Gets the recommended requested state for the bundle.
        /// </summary>
        public RequestState RecommendedState { get; private set; }

        /// <summary>
        /// Gets or sets the requested state for the bundle.
        /// </summary>
        public RequestState State { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.ExecuteProcessCancel"/>
    /// </summary>
    [Serializable]
    public class ExecuteProcessCancelEventArgs : HResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public ExecuteProcessCancelEventArgs(string packageId, int processId, BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION recommendation, BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION action)
        {
            this.PackageId = packageId;
            this.ProcessId = processId;
            this.Recommendation = recommendation;
            this.Action = action;
        }

        /// <summary>
        /// Gets the identity of the package.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the process id.
        /// </summary>
        public int ProcessId { get; private set; }

        /// <summary>
        /// Gets the recommended action from the engine.
        /// </summary>
        public BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION Recommendation { get; private set; }

        /// <summary>
        /// Gets or sets the action to be performed. This is passed back to the engine.
        /// </summary>
        public BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION Action { get; set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.DetectRelatedBundlePackage"/>
    /// </summary>
    [Serializable]
    public class DetectRelatedBundlePackageEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public DetectRelatedBundlePackageEventArgs(string packageId, string productCode, RelationType relationType, bool perMachine, string version, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ProductCode = productCode;
            this.RelationType = relationType;
            this.PerMachine = perMachine;
            this.Version = version;
        }

        /// <summary>
        /// Gets the identity of the product's package detected.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identity of the related bundle detected.
        /// </summary>
        public string ProductCode { get; private set; }

        /// <summary>
        /// Gets the relationship type of the related bundle.
        /// </summary>
        public RelationType RelationType { get; private set; }

        /// <summary>
        /// Gets whether the detected bundle is per machine.
        /// </summary>
        public bool PerMachine { get; private set; }

        /// <summary>
        /// Gets the version of the related bundle detected.
        /// </summary>
        public string Version { get; private set; }
    }

    /// <summary>
    /// Event arguments for <see cref="IDefaultBootstrapperApplication.CachePackageNonVitalValidationFailure"/>
    /// </summary>
    [Serializable]
    public class CachePackageNonVitalValidationFailureEventArgs : ActionEventArgs<BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION>
    {
        /// <summary>
        /// This class is for events raised by the engine.
        /// It is not intended to be instantiated by user code.
        /// </summary>
        public CachePackageNonVitalValidationFailureEventArgs(string packageId, int hrStatus, BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION recommendation, BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.PackageId = packageId;
        }

        /// <summary>
        /// Gets the identity of the package that was being validated.
        /// </summary>
        public string PackageId { get; private set; }
    }
}
