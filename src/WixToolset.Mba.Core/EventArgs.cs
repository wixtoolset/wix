// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Base class for BA <see cref="EventArgs"/> classes.
    /// </summary>
    [Serializable]
    public abstract class HResultEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="HResultEventArgs"/> class.
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
        /// Creates a new instance of the <see cref="CancellableHResultEventArgs"/> class.
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
        /// Creates a new instance of the <see cref="ResultEventArgs"/> class.
        /// </summary>
        /// <param name="recommendation">Recommended result from engine.</param>
        /// <param name="result">The result to return to the engine.</param>
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
        /// Creates a new instance of the <see cref="StatusEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
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
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        /// <param name="recommendation">Recommended action from engine.</param>
        /// <param name="action">The action to perform.</param>
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
    /// Additional arguments used when startup has begun.
    /// </summary>
    [Serializable]
    public class StartupEventArgs : HResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="StartupEventArgs"/> class.
        /// </summary>
        public StartupEventArgs()
        {
        }
    }

    /// <summary>
    /// Additional arguments used when shutdown has begun.
    /// </summary>
    [Serializable]
    public class ShutdownEventArgs : HResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ShutdownEventArgs"/> class.
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
    /// Additional arguments used when the system is shutting down or the user is logging off.
    /// </summary>
    /// <remarks>
    /// <para>To prevent shutting down or logging off, set <see cref="CancellableHResultEventArgs.Cancel"/> to
    /// true; otherwise, set it to false.</para>
    /// <para>By default setup will prevent shutting down or logging off between
    /// <see cref="BootstrapperApplication.ApplyBegin"/> and <see cref="BootstrapperApplication.ApplyComplete"/>.</para>
    /// <para>If <see cref="SystemShutdownEventArgs.Reasons"/> contains <see cref="EndSessionReasons.Critical"/>
    /// the bootstrapper cannot prevent the shutdown and only has a few seconds to save state or perform any other
    /// critical operations before being closed by the operating system.</para>
    /// </remarks>
    [Serializable]
    public class SystemShutdownEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="SystemShutdownEventArgs"/> class.
        /// </summary>
        /// <param name="reasons">The reason the application is requested to close or being closed.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public SystemShutdownEventArgs(EndSessionReasons reasons, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.Reasons = reasons;
        }

        /// <summary>
        /// Gets the reason the application is requested to close or being closed.
        /// </summary>
        /// <remarks>
        /// <para>To prevent shutting down or logging off, set <see cref="CancellableHResultEventArgs.Cancel"/> to
        /// true; otherwise, set it to false.</para>
        /// <para>If <see cref="SystemShutdownEventArgs.Reasons"/> contains <see cref="EndSessionReasons.Critical"/>
        /// the bootstrapper cannot prevent the shutdown and only has a few seconds to save state or perform any other
        /// critical operations before being closed by the operating system.</para>
        /// </remarks>
        public EndSessionReasons Reasons { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when the overall detection phase has begun.
    /// </summary>
    [Serializable]
    public class DetectBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectBeginEventArgs"/> class.
        /// </summary>
        /// <param name="installed">Specifies whether the bundle is installed.</param>
        /// <param name="packageCount">The number of packages to detect.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public DetectBeginEventArgs(bool installed, int packageCount, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.Installed = installed;
            this.PackageCount = packageCount;
        }

        /// <summary>
        /// Gets whether the bundle is installed.
        /// </summary>
        public bool Installed { get; private set; }

        /// <summary>
        /// Gets the number of packages to detect.
        /// </summary>
        public int PackageCount { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when detected a forward compatible bundle.
    /// </summary>
    [Serializable]
    public class DetectForwardCompatibleBundleEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectUpdateBeginEventArgs"/> class.
        /// </summary>
        /// <param name="bundleId">The identity of the forward compatible bundle.</param>
        /// <param name="relationType">Relationship type for this forward compatible bundle.</param>
        /// <param name="bundleTag">The tag of the forward compatible bundle.</param>
        /// <param name="perMachine">Whether the detected forward compatible bundle is per machine.</param>
        /// <param name="version">The version of the forward compatible bundle detected.</param>
        /// <param name="cancelRecommendation">The cancel recommendation from the engine.</param>
        /// <param name="ignoreBundleRecommendation">The ignore recommendation from the engine.</param>
        public DetectForwardCompatibleBundleEventArgs(string bundleId, RelationType relationType, string bundleTag, bool perMachine, long version, bool cancelRecommendation, bool ignoreBundleRecommendation)
            : base(cancelRecommendation)
        {
            this.BundleId = bundleId;
            this.RelationType = relationType;
            this.BundleTag = bundleTag;
            this.PerMachine = perMachine;
            this.Version = Engine.LongToVersion(version);
            this.IgnoreBundle = ignoreBundleRecommendation;
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
        public Version Version { get; private set; }

        /// <summary>
        /// Instructs the engine whether to use the forward compatible bundle.
        /// </summary>
        public bool IgnoreBundle { get; set; }
    }

    /// <summary>
    /// Additional arguments used when the detection for an update has begun.
    /// </summary>
    [Serializable]
    public class DetectUpdateBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectUpdateBeginEventArgs"/> class.
        /// </summary>
        /// <param name="updateLocation">The location to check for an updated bundle.</param>
        /// <param name="cancelRecommendation">The cancel recommendation from the engine.</param>
        /// <param name="skipRecommendation">The skip recommendation from the engine.</param>
        public DetectUpdateBeginEventArgs(string updateLocation, bool cancelRecommendation, bool skipRecommendation)
            : base(cancelRecommendation)
        {
            this.UpdateLocation = updateLocation;
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
    /// Additional arguments used when the detection for an update has begun.
    /// </summary>
    [Serializable]
    public class DetectUpdateEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectUpdateBeginEventArgs"/> class.
        /// </summary>
        /// <param name="updateLocation">The location to check for an updated bundle.</param>
        /// <param name="size">The expected size of the updated bundle.</param>
        /// <param name="version">The expected version of the updated bundle.</param>
        /// <param name="title">The title of the updated bundle.</param>
        /// <param name="summary">The summary of the updated bundle.</param>
        /// <param name="contentType">The content type of the content of the updated bundle.</param>
        /// <param name="content">The content of the updated bundle.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        /// <param name="stopRecommendation">The recommendation from the engine.</param>
        public DetectUpdateEventArgs(string updateLocation, long size, long version, string title, string summary, string contentType, string content, bool cancelRecommendation, bool stopRecommendation)
            : base(cancelRecommendation)
        {
            this.UpdateLocation = updateLocation;
            this.Size = size;
            this.Version = Engine.LongToVersion(version);
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
        /// Gets the version of the updated bundle.
        /// </summary>
        public Version Version { get; private set; }

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
    /// Additional arguments used when the detection for an update has completed.
    /// </summary>
    [Serializable]
    public class DetectUpdateCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectUpdateCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        /// <param name="ignoreRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when a related bundle has been detected for a bundle.
    /// </summary>
    [Serializable]
    public class DetectRelatedBundleEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectRelatedBundleEventArgs"/> class.
        /// </summary>
        /// <param name="productCode">The identity of the related package bundle.</param>
        /// <param name="relationType">Relationship type for this related bundle.</param>
        /// <param name="bundleTag">The tag of the related package bundle.</param>
        /// <param name="perMachine">Whether the detected bundle is per machine.</param>
        /// <param name="version">The version of the related bundle detected.</param>
        /// <param name="operation">The operation that will be taken on the detected bundle.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public DetectRelatedBundleEventArgs(string productCode, RelationType relationType, string bundleTag, bool perMachine, long version, RelatedOperation operation, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.ProductCode = productCode;
            this.RelationType = relationType;
            this.BundleTag = bundleTag;
            this.PerMachine = perMachine;
            this.Version = Engine.LongToVersion(version);
            this.Operation = operation;
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
        public Version Version { get; private set; }

        /// <summary>
        /// Gets the operation that will be taken on the detected bundle.
        /// </summary>
        public RelatedOperation Operation { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when the detection for a specific package has begun.
    /// </summary>
    [Serializable]
    public class DetectPackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectPackageBeginEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package to detect.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when a package was not found but a newer package using the same provider key was.
    /// </summary>
    [Serializable]
    public class DetectCompatibleMsiPackageEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectCompatibleMsiPackageEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package that was not detected.</param>
        /// <param name="compatiblePackageId">The identity of the compatible package that was detected.</param>
        /// <param name="compatiblePackageVersion">The version of the compatible package that was detected.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public DetectCompatibleMsiPackageEventArgs(string packageId, string compatiblePackageId, long compatiblePackageVersion, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.CompatiblePackageId = compatiblePackageId;
            this.CompatiblePackageVersion = Engine.LongToVersion(compatiblePackageVersion);
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
        public Version CompatiblePackageVersion { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when a related MSI package has been detected for a package.
    /// </summary>
    [Serializable]
    public class DetectRelatedMsiPackageEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectRelatedMsiPackageEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package detecting.</param>
        /// <param name="upgradeCode">The upgrade code of the related package detected.</param>
        /// <param name="productCode">The identity of the related package detected.</param>
        /// <param name="perMachine">Whether the detected package is per machine.</param>
        /// <param name="version">The version of the related package detected.</param>
        /// <param name="operation">The operation that will be taken on the detected package.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public DetectRelatedMsiPackageEventArgs(string packageId, string upgradeCode, string productCode, bool perMachine, long version, RelatedOperation operation, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.UpgradeCode = upgradeCode;
            this.ProductCode = productCode;
            this.PerMachine = perMachine;
            this.Version = Engine.LongToVersion(version);
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
        public Version Version { get; private set; }

        /// <summary>
        /// Gets the operation that will be taken on the detected package.
        /// </summary>
        public RelatedOperation Operation { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when a target MSI package has been detected.
    /// </summary>
    public class DetectTargetMsiPackageEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectMsiFeatureEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">Detected package identifier.</param>
        /// <param name="productCode">Detected product code.</param>
        /// <param name="state">Package state detected.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public DetectTargetMsiPackageEventArgs(string packageId, string productCode, PackageState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ProductCode = productCode;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the target's package detected.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the product code of the target MSI detected.
        /// </summary>
        public string ProductCode { get; private set; }

        /// <summary>
        /// Gets the detected patch package state.
        /// </summary>
        public PackageState State { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when a feature in an MSI package has been detected.
    /// </summary>
    public class DetectMsiFeatureEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectMsiFeatureEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">Detected package identifier.</param>
        /// <param name="featureId">Detected feature identifier.</param>
        /// <param name="state">Feature state detected.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when the detection for a specific package has completed.
    /// </summary>
    [Serializable]
    public class DetectPackageCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectPackageCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package detected.</param>
        /// <param name="hrStatus">The return code of the operation.</param>
        /// <param name="state">The state of the specified package.</param>
        public DetectPackageCompleteEventArgs(string packageId, int hrStatus, PackageState state)
            : base(hrStatus)
        {
            this.PackageId = packageId;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the package detected.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the state of the specified package.
        /// </summary>
        public PackageState State { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when the detection phase has completed.
    /// </summary>
    [Serializable]
    public class DetectCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="DetectCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        public DetectCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has begun planning the installation.
    /// </summary>
    [Serializable]
    public class PlanBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanBeginEventArgs"/> class.
        /// </summary>
        /// <param name="packageCount">The number of packages to plan for.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when the engine has begun planning for a related bundle.
    /// </summary>
    [Serializable]
    public class PlanRelatedBundleEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanRelatedBundleEventArgs"/> class.
        /// </summary>
        /// <param name="bundleId">The identity of the bundle to plan for.</param>
        /// <param name="recommendedState">The recommended requested state for the bundle.</param>
        /// <param name="state">The requested state for the bundle.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when the engine has begun planning the installation of a specific package.
    /// </summary>
    [Serializable]
    public class PlanPackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanPackageBeginEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package to plan for.</param>
        /// <param name="recommendedState">The recommended requested state for the package.</param>
        /// <param name="state">The requested state for the package.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public PlanPackageBeginEventArgs(string packageId, RequestState recommendedState, RequestState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.RecommendedState = recommendedState;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the package to plan for.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the recommended requested state for the package.
        /// </summary>
        public RequestState RecommendedState { get; private set; }

        /// <summary>
        /// Gets or sets the requested state for the package.
        /// </summary>
        public RequestState State { get; set; }
    }

    /// <summary>
    /// Additional arguments used when the engine is about to plan a newer package using the same provider key.
    /// </summary>
    [Serializable]
    public class PlanCompatibleMsiPackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanCompatibleMsiPackageBeginEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package that was not detected.</param>
        /// <param name="compatiblePackageId">The identity of the compatible package that was detected.</param>
        /// <param name="compatiblePackageVersion">The version of the compatible package that was detected.</param>
        /// <param name="recommendedState">The recommended request state for the compatible package.</param>
        /// <param name="state">The requested state for the compatible package.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public PlanCompatibleMsiPackageBeginEventArgs(string packageId, string compatiblePackageId, long compatiblePackageVersion, RequestState recommendedState, RequestState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.CompatiblePackageId = compatiblePackageId;
            this.CompatiblePackageVersion = Engine.LongToVersion(compatiblePackageVersion);
            this.RecommendedState = recommendedState;
            this.State = state;
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
        public Version CompatiblePackageVersion { get; private set; }

        /// <summary>
        /// Gets the recommended state to use for the compatible package for planning.
        /// </summary>
        public RequestState RecommendedState { get; private set; }

        /// <summary>
        /// Gets or sets the state to use for the compatible package for planning.
        /// </summary>
        public RequestState State { get; set; }
    }

    /// <summary>
    /// Additional arguments used when the engine has completed planning the installation of a specific package.
    /// </summary>
    [Serializable]
    public class PlanCompatibleMsiPackageCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanCompatibleMsiPackageCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package planned for.</param>
        /// <param name="compatiblePackageId">The identity of the compatible package that was detected.</param>
        /// <param name="hrStatus">The return code of the operation.</param>
        /// <param name="state">The current state of the package.</param>
        /// <param name="requested">The requested state for the package</param>
        /// <param name="execute">The execution action to take.</param>
        /// <param name="rollback">The rollback action to take.</param>
        public PlanCompatibleMsiPackageCompleteEventArgs(string packageId, string compatiblePackageId, int hrStatus, PackageState state, RequestState requested, ActionState execute, ActionState rollback)
            : base(hrStatus)
        {
            this.PackageId = packageId;
            this.CompatiblePackageId = compatiblePackageId;
            this.State = state;
            this.Requested = requested;
            this.Execute = execute;
            this.Rollback = rollback;
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
        /// Gets the current state of the package.
        /// </summary>
        public PackageState State { get; private set; }

        /// <summary>
        /// Gets the requested state for the package.
        /// </summary>
        public RequestState Requested { get; private set; }

        /// <summary>
        /// Gets the execution action to take.
        /// </summary>
        public ActionState Execute { get; private set; }

        /// <summary>
        /// Gets the rollback action to take.
        /// </summary>
        public ActionState Rollback { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when engine is about to plan a MSP applied to a target MSI package.
    /// </summary>
    [Serializable]
    public class PlanTargetMsiPackageEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanMsiFeatureEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">Package identifier of the patch being planned.</param>
        /// <param name="productCode">Product code identifier being planned.</param>
        /// <param name="recommendedState">Recommended package state of the patch being planned.</param>
        /// <param name="state">Package state of the patch being planned.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public PlanTargetMsiPackageEventArgs(string packageId, string productCode, RequestState recommendedState, RequestState state, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ProductCode = productCode;
            this.RecommendedState = recommendedState;
            this.State = state;
        }

        /// <summary>
        /// Gets the identity of the patch package to plan.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identity of the patch's target MSI to plan.
        /// </summary>
        public string ProductCode { get; private set; }

        /// <summary>
        /// Gets the recommended state of the patch to use by planning.
        /// </summary>
        public RequestState RecommendedState { get; private set; }

        /// <summary>
        /// Gets or sets the state of the patch to use by planning.
        /// </summary>
        public RequestState State { get; set; }
    }

    /// <summary>
    /// Additional arguments used when engine is about to plan a feature in an MSI package.
    /// </summary>
    [Serializable]
    public class PlanMsiFeatureEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanMsiFeatureEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">Package identifier being planned.</param>
        /// <param name="featureId">Feature identifier being planned.</param>
        /// <param name="recommendedState">Recommended feature state being planned.</param>
        /// <param name="state">Feature state being planned.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when then engine has completed planning the installation of a specific package.
    /// </summary>
    [Serializable]
    public class PlanPackageCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanPackageCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package planned for.</param>
        /// <param name="hrStatus">The return code of the operation.</param>
        /// <param name="state">The current state of the package.</param>
        /// <param name="requested">The requested state for the package</param>
        /// <param name="execute">The execution action to take.</param>
        /// <param name="rollback">The rollback action to take.</param>
        public PlanPackageCompleteEventArgs(string packageId, int hrStatus, PackageState state, RequestState requested, ActionState execute, ActionState rollback)
            : base(hrStatus)
        {
            this.PackageId = packageId;
            this.State = state;
            this.Requested = requested;
            this.Execute = execute;
            this.Rollback = rollback;
        }

        /// <summary>
        /// Gets the identity of the package planned for.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the current state of the package.
        /// </summary>
        public PackageState State { get; private set; }

        /// <summary>
        /// Gets the requested state for the package.
        /// </summary>
        public RequestState Requested { get; private set; }

        /// <summary>
        /// Gets the execution action to take.
        /// </summary>
        public ActionState Execute { get; private set; }

        /// <summary>
        /// Gets the rollback action to take.
        /// </summary>
        public ActionState Rollback { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when the engine has completed planning the installation.
    /// </summary>
    [Serializable]
    public class PlanCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="PlanCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        public PlanCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has begun installing the bundle.
    /// </summary>
    [Serializable]
    public class ApplyBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ApplyBeginEventArgs"/> class.
        /// </summary>
        /// <param name="phaseCount">The number of phases during apply.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when the engine is about to start the elevated process.
    /// </summary>
    [Serializable]
    public class ElevateBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ElevateBeginEventArgs"/> class.
        /// </summary>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public ElevateBeginEventArgs(bool cancelRecommendation)
            : base(cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has completed starting the elevated process.
    /// </summary>
    [Serializable]
    public class ElevateCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ElevateCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        public ElevateCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has changed progress for the bundle installation.
    /// </summary>
    [Serializable]
    public class ProgressEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates an new instance of the <see cref="ProgressEventArgs"/> class.
        /// </summary>
        /// <param name="progressPercentage">The percentage from 0 to 100 completed for a package.</param>
        /// <param name="overallPercentage">The percentage from 0 to 100 completed for the bundle.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when the engine has encountered an error.
    /// </summary>
    [Serializable]
    public class ErrorEventArgs : ResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ErrorEventArgs"/> class.
        /// </summary>
        /// <param name="errorType">The error type.</param>
        /// <param name="packageId">The identity of the package that yielded the error.</param>
        /// <param name="errorCode">The error code.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="dwUIHint">Recommended display flags for an error dialog.</param>
        /// <param name="data">The exteded data for the error.</param>
        /// <param name="recommendation">Recommended result from engine.</param>
        /// <param name="result">The result to return to the engine.</param>
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
    /// Additional arguments used when the engine has begun registering the location and visibility of the bundle.
    /// </summary>
    [Serializable]
    public class RegisterBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="RegisterBeginEventArgs"/> class.
        /// </summary>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public RegisterBeginEventArgs(bool cancelRecommendation)
            : base(cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has completed registering the location and visilibity of the bundle.
    /// </summary>
    [Serializable]
    public class RegisterCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="RegisterCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        public RegisterCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has begun removing the registration for the location and visibility of the bundle.
    /// </summary>
    [Serializable]
    public class UnregisterBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="UnregisterBeginEventArgs"/> class.
        /// </summary>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public UnregisterBeginEventArgs(bool cancelRecommendation)
            : base(cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has completed removing the registration for the location and visibility of the bundle.
    /// </summary>
    [Serializable]
    public class UnregisterCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="UnregisterCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        public UnregisterCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has begun caching the installation sources.
    /// </summary>
    [Serializable]
    public class CacheBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CacheBeginEventArgs"/> class.
        /// </summary>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public CacheBeginEventArgs(bool cancelRecommendation)
            : base(cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine begins to acquire containers or payloads.
    /// </summary>
    [Serializable]
    public class CacheAcquireBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CacheAcquireBeginEventArgs"/> class.
        /// </summary>
        public CacheAcquireBeginEventArgs(string packageOrContainerId, string payloadId, CacheOperation operation, string source, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
            this.Operation = operation;
            this.Source = source;
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
        /// Gets the cache acquire operation.
        /// </summary>
        public CacheOperation Operation { get; private set; }

        /// <summary>
        /// Gets the source of the container or payload.
        /// </summary>
        public string Source { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when the engine acquires some part of a container or payload.
    /// </summary>
    [Serializable]
    public class CacheAcquireProgressEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CacheAcquireBeginEventArgs"/> class.
        /// </summary>
        public CacheAcquireProgressEventArgs(string packageOrContainerId, string payloadId, long progress, long total, int overallPercentage, bool cancelRecommendation)
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
        /// Gets the identifier of the payload (if acquiring a payload).
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
    /// Additional arguments used when the engine completes the acquisition of a container or payload.
    /// </summary>
    [Serializable]
    public class CacheAcquireCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CacheAcquireCompleteEventArgs"/> class.
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
    /// Additional arguments used when the engine starts the verification of a payload.
    /// </summary>
    [Serializable]
    public class CacheVerifyBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CacheVerifyBeginEventArgs"/> class.
        /// </summary>
        public CacheVerifyBeginEventArgs(string packageId, string payloadId, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the package.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when the engine completes the verification of a payload.
    /// </summary>
    [Serializable]
    public class CacheVerifyCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CacheVerifyCompleteEventArgs"/> class.
        /// </summary>
        public CacheVerifyCompleteEventArgs(string packageId, string payloadId, int hrStatus, BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION recommendation, BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION action)
            : base(hrStatus, recommendation, action)
        {
            this.PackageId = packageId;
            this.PayloadId = payloadId;
        }

        /// <summary>
        /// Gets the identifier of the package.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the identifier of the payload.
        /// </summary>
        public string PayloadId { get; private set; }
    }

    /// <summary>
    /// Additional arguments used after the engine has cached the installation sources.
    /// </summary>
    [Serializable]
    public class CacheCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CacheCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        public CacheCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has begun installing packages.
    /// </summary>
    [Serializable]
    public class ExecuteBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ExecuteBeginEventArgs"/> class.
        /// </summary>
        /// <param name="packageCount">The number of packages to act on.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when the engine has begun installing a specific package.
    /// </summary>
    [Serializable]
    public class ExecutePackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ExecutePackageBeginEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package to act on.</param>
        /// <param name="shouldExecute">Whether the package should really be acted on.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public ExecutePackageBeginEventArgs(string packageId, bool shouldExecute, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.ShouldExecute = shouldExecute;
        }

        /// <summary>
        /// Gets the identity of the package to act on.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets whether the package should really be acted on.
        /// </summary>
        public bool ShouldExecute { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when the engine executes one or more patches targeting a product.
    /// </summary>
    [Serializable]
    public class ExecutePatchTargetEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ExecutePatchTargetEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package to act on.</param>
        /// <param name="targetProductCode">The product code of the target of the patch.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments used when Windows Installer sends an installation message.
    /// </summary>
    [Serializable]
    public class ExecuteMsiMessageEventArgs : ResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ExecuteMsiMessageEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package that yielded this message.</param>
        /// <param name="messageType">The type of this message.</param>
        /// <param name="dwUIHint">Recommended display flags for this message.</param>
        /// <param name="message">The message.</param>
        /// <param name="data">The extended data for the message.</param>
        /// <param name="recommendation">Recommended result from engine.</param>
        /// <param name="result">The result to return to the engine.</param>
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
    /// Additional arugments used for file in use installation messages.
    /// </summary>
    [Serializable]
    public class ExecuteFilesInUseEventArgs : ResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ExecuteFilesInUseEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package that yielded the files in use message.</param>
        /// <param name="files">The list of files in use.</param>
        /// <param name="recommendation">Recommended result from engine.</param>
        /// <param name="result">The result to return to the engine.</param>
        public ExecuteFilesInUseEventArgs(string packageId, string[] files, Result recommendation, Result result)
            : base(recommendation, result)
        {
            this.PackageId = packageId;
            this.Files = new ReadOnlyCollection<string>(files ?? new string[] { });
        }

        /// <summary>
        /// Gets the identity of the package that yielded the files in use message.
        /// </summary>
        public string PackageId { get; private set; }

        /// <summary>
        /// Gets the list of files in use.
        /// </summary>
        public IList<string> Files { get; private set; }
    }

    /// <summary>
    /// Additional arguments used when the engine has completed installing a specific package.
    /// </summary>
    [Serializable]
    public class ExecutePackageCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ExecutePackageCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package that was acted on.</param>
        /// <param name="hrStatus">The return code of the operation.</param>
        /// <param name="restart">Whether a restart is required.</param>
        /// <param name="recommendation">Recommended action from engine.</param>
        /// <param name="action">The action to perform.</param>
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
    /// Additional arguments used when the engine has completed installing packages.
    /// </summary>
    [Serializable]
    public class ExecuteCompleteEventArgs : StatusEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ExecuteCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        public ExecuteCompleteEventArgs(int hrStatus)
            : base(hrStatus)
        {
        }
    }

    /// <summary>
    /// Additional arguments used when the engine has completed installing the bundle.
    /// </summary>
    [Serializable]
    public class ApplyCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_APPLYCOMPLETE_ACTION>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ApplyCompleteEventArgs"/> clas.
        /// </summary>
        /// <param name="hrStatus">The return code of the operation.</param>
        /// <param name="restart">Whether a restart is required.</param>
        /// <param name="recommendation">Recommended action from engine.</param>
        /// <param name="action">The action to perform.</param>
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
    /// Additional arguments used by the engine to allow the BA to change the source
    /// using <see cref="Engine.SetLocalSource"/> or <see cref="Engine.SetDownloadSource"/>.
    /// </summary>
    [Serializable]
    public class ResolveSourceEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ResolveSourceEventArgs"/> class.
        /// </summary>
        /// <param name="packageOrContainerId">The identity of the package or container that requires source.</param>
        /// <param name="payloadId">The identity of the payload that requires source.</param>
        /// <param name="localSource">The current path used for source resolution.</param>
        /// <param name="downloadSource">Optional URL to download container or payload.</param>
        /// <param name="recommendation">The recommended action from the engine.</param>
        /// <param name="action">The action to perform.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public ResolveSourceEventArgs(string packageOrContainerId, string payloadId, string localSource, string downloadSource, BOOTSTRAPPER_RESOLVESOURCE_ACTION recommendation, BOOTSTRAPPER_RESOLVESOURCE_ACTION action, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageOrContainerId = packageOrContainerId;
            this.PayloadId = payloadId;
            this.LocalSource = localSource;
            this.DownloadSource = downloadSource;
            this.Recommendation = recommendation;
            this.Action = action;
        }

        /// <summary>
        /// Gets the identity of the package or container that requires source.
        /// </summary>
        public string PackageOrContainerId { get; private set; }

        /// <summary>
        /// Gets the identity of the payload that requires source.
        /// </summary>
        public string PayloadId { get; private set; }

        /// <summary>
        /// Gets the current path used for source resolution.
        /// </summary>
        public string LocalSource { get; private set; }

        /// <summary>
        /// Gets the optional URL to download container or payload.
        /// </summary>
        public string DownloadSource { get; private set; }

        /// <summary>
        /// Gets the recommended action from the engine.
        /// </summary>
        public BOOTSTRAPPER_RESOLVESOURCE_ACTION Recommendation { get; private set; }

        /// <summary>
        /// Gets or sets the action to perform.
        /// </summary>
        public BOOTSTRAPPER_RESOLVESOURCE_ACTION Action { get; set; }
    }

    /// <summary>
    /// Additional arguments used by the engine when it has begun caching a specific package.
    /// </summary>
    [Serializable]
    public class CachePackageBeginEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CachePackageBeginEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package that is being cached.</param>
        /// <param name="cachePayloads">Number of payloads to be cached.</param>
        /// <param name="packageCacheSize">The size on disk required by the specific package.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
        public CachePackageBeginEventArgs(string packageId, int cachePayloads, long packageCacheSize, bool cancelRecommendation)
            : base(cancelRecommendation)
        {
            this.PackageId = packageId;
            this.CachePayloads = cachePayloads;
            this.PackageCacheSize = packageCacheSize;
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
    }

    /// <summary>
    /// Additional arguments passed by the engine when it has completed caching a specific package.
    /// </summary>
    [Serializable]
    public class CachePackageCompleteEventArgs : ActionEventArgs<BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION>
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CachePackageCompleteEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identity of the package that was cached.</param>
        /// <param name="hrStatus">The return code of the operation.</param>
        /// <param name="recommendation">Recommended action from engine.</param>
        /// <param name="action">The action to perform.</param>
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
    /// Additional arguments passed by the engine while executing on payload.
    /// </summary>
    [Serializable]
    public class ExecuteProgressEventArgs : CancellableHResultEventArgs
    {
        /// <summary>
        /// Creates a new instance of the <see cref="ExecuteProgressEventArgs"/> class.
        /// </summary>
        /// <param name="packageId">The identifier of the package being executed.</param>
        /// <param name="progressPercentage">The percentage from 0 to 100 of the execution progress for a single payload.</param>
        /// <param name="overallPercentage">The percentage from 0 to 100 of the execution progress for all payload.</param>
        /// <param name="cancelRecommendation">The recommendation from the engine.</param>
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
    /// Additional arguments passed by the engine before it tries to launch the preapproved executable.
    /// </summary>
    [Serializable]
    public class LaunchApprovedExeBeginArgs : CancellableHResultEventArgs
    {
        public LaunchApprovedExeBeginArgs(bool cancelRecommendation)
            : base(cancelRecommendation)
        {
        }
    }

    /// <summary>
    /// Additional arguments passed by the engine after it finished trying to launch the preapproved executable.
    /// </summary>
    [Serializable]
    public class LaunchApprovedExeCompleteArgs : StatusEventArgs
    {
        private int processId;

        public LaunchApprovedExeCompleteArgs(int hrStatus, int processId)
            : base(hrStatus)
        {
            this.processId = processId;
        }

        /// <summary>
        /// Gets the ProcessId of the process that was launched.
        /// This is only valid if the status reports success.
        /// </summary>
        public int ProcessId
        {
            get { return this.processId; }
        }
    }
}
