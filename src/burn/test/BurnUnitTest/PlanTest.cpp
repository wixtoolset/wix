// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT WINAPI PlanTestBAProc(
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    );

static LPCWSTR wzMsiTransactionManifestFileName = L"MsiTransaction_BundleAv1_manifest.xml";
static LPCWSTR wzMultipleBundlePackageManifestFileName = L"BundlePackage_Multiple_manifest.xml";
static LPCWSTR wzSingleExeManifestFileName = L"Failure_BundleD_manifest.xml";
static LPCWSTR wzSingleMsiManifestFileName = L"BasicFunctionality_BundleA_manifest.xml";
static LPCWSTR wzSingleMsuManifestFileName = L"MsuPackageFixture_manifest.xml";
static LPCWSTR wzSlipstreamManifestFileName = L"Slipstream_BundleA_manifest.xml";
static LPCWSTR wzSlipstreamModifiedManifestFileName = L"Slipstream_BundleA_modified_manifest.xml";

static BOOL vfUsePackageRequestState = FALSE;
static BOOTSTRAPPER_REQUEST_STATE vPackageRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
static BOOL vfUseRelatedBundleRequestState = FALSE;
static BOOTSTRAPPER_REQUEST_STATE vRelatedBundleRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
static BOOL vfUseRelatedBundlePlanType = FALSE;
static BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE vRelatedBundlePlanType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE;

static BURN_DEPENDENCY_ACTION registerActions1[] = { BURN_DEPENDENCY_ACTION_REGISTER };
static BURN_DEPENDENCY_ACTION unregisterActions1[] = { BURN_DEPENDENCY_ACTION_UNREGISTER };

namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace Xunit;

    public ref class PlanTest : BurnUnitTest
    {
    public:
        PlanTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void MsiTransactionInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzMsiTransactionManifestFileName, pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectRelatedBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"1.0.0.0", BOOTSTRAPPER_RELATION_UPGRADE);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 9);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageB");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 14);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageC");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageB");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 9);
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageC");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 14);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(107082ull, pPlan->qwEstimatedSize);
            Assert::Equal(522548ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ", TRUE, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteBeginMsiTransaction(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ");
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageB");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageB", registerActions1, 1);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageB", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageC");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageC", registerActions1, 1);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageC", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageC", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCommitMsiTransaction(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ", TRUE, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageB");
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageB", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageC");
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageC", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageC", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(4ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(7ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            ValidateRestoreRelatedBundle(pPlan, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(3ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[1], L"PackageB", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[2], L"PackageC", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void MsiTransactionUninstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzMsiTransactionManifestFileName, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ", TRUE, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteBeginMsiTransaction(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageC", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageC", unregisterActions1, 1);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageC", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageB", unregisterActions1, 1);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageB", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCommitMsiTransaction(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ", TRUE, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageC", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageC", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageB", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(3ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(3ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageC");
            ValidateCleanAction(pPlan, dwIndex++, L"PackageB");
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{E6469F05-BDC8-4EB8-B218-67412543EFAA}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{A497C5E5-C78B-4F0B-BF72-B33E1DB1C4B8}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{D1D01094-23CE-4AF0-84B6-4A1A133F21D3}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{01E6B748-7B95-4BA9-976D-B6F35076CEF4}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(3ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[1], L"PackageB", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[2], L"PackageC", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void MultipleBundlePackageInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzMultipleBundlePackageManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPermanentPackagesAsPresentAndCached(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 6);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageB");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(18575450ull, pPlan->qwEstimatedSize);
            Assert::Equal(78462280ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteBundlePackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageB");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteBundlePackage(pPlan, fRollback, dwIndex++, L"PackageB", BOOTSTRAPPER_ACTION_STATE_INSTALL, L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteBundlePackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteBundlePackage(pPlan, fRollback, dwIndex++, L"PackageB", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(2ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(4ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"NetFx48Web");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{35192ED0-C70A-49B2-9D12-3B1FA39B5E6F}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(3ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[1], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[2], L"PackageB", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void OrphanCompatiblePackageTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectCompatibleMsiPackage(pEngineState, pEngineState->packages.rgPackages, L"{C24F3903-38E7-4D44-8037-D9856B3C5046}", L"2.0.0.0");

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", unregisterActions1, 1);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateUninstallMsiCompatiblePackage(pPlan, fRollback, dwIndex++, L"PackageA", 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(1ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(1ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            ValidateCleanCompatibleAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{64633047-D172-4BBB-B202-64337D15C952}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void RelatedBundlesAreSortedByPlanType()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectRelatedBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"0.9.0.0", BOOTSTRAPPER_RELATION_UPGRADE);
            DetectRelatedBundle(pEngineState, L"{6B2D8401-C0C2-4060-BFEF-5DDFD04BD586}", L"0.2.0.0", BOOTSTRAPPER_RELATION_PATCH);
            DetectRelatedBundle(pEngineState, L"{5C80A327-61B9-44CF-A6D4-64C45F4F90A9}", L"0.4.0.0", BOOTSTRAPPER_RELATION_ADDON);
            DetectRelatedBundle(pEngineState, L"{33A8757F-32EA-4974-888E-D15547259B3C}", L"0.3.0.0", BOOTSTRAPPER_RELATION_DEPENDENT_PATCH);
            DetectRelatedBundle(pEngineState, L"{59CD5A25-0398-41CA-AD53-AD8C061E2A1A}", L"0.7.0.0", BOOTSTRAPPER_RELATION_DEPENDENT_ADDON);

            RelatedBundlesSortDetect(&pEngineState->registration.relatedBundles);
            NativeAssert::StringEqual(L"{5C80A327-61B9-44CF-A6D4-64C45F4F90A9}", pEngineState->registration.relatedBundles.rgRelatedBundles[0].package.sczId);
            NativeAssert::StringEqual(L"{6B2D8401-C0C2-4060-BFEF-5DDFD04BD586}", pEngineState->registration.relatedBundles.rgRelatedBundles[1].package.sczId);
            NativeAssert::StringEqual(L"{59CD5A25-0398-41CA-AD53-AD8C061E2A1A}", pEngineState->registration.relatedBundles.rgRelatedBundles[2].package.sczId);
            NativeAssert::StringEqual(L"{33A8757F-32EA-4974-888E-D15547259B3C}", pEngineState->registration.relatedBundles.rgRelatedBundles[3].package.sczId);
            NativeAssert::StringEqual(L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", pEngineState->registration.relatedBundles.rgRelatedBundles[4].package.sczId);

            vfUseRelatedBundlePlanType = TRUE;
            vRelatedBundlePlanType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_UPGRADE;

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(35694ull, pPlan->qwEstimatedSize);
            Assert::Equal(168715ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{6B2D8401-C0C2-4060-BFEF-5DDFD04BD586}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{33A8757F-32EA-4974-888E-D15547259B3C}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{5C80A327-61B9-44CF-A6D4-64C45F4F90A9}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{59CD5A25-0398-41CA-AD53-AD8C061E2A1A}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{6B2D8401-C0C2-4060-BFEF-5DDFD04BD586}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{33A8757F-32EA-4974-888E-D15547259B3C}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{5C80A327-61B9-44CF-A6D4-64C45F4F90A9}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{59CD5A25-0398-41CA-AD53-AD8C061E2A1A}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(6ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(7ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            ValidateRestoreRelatedBundle(pPlan, dwIndex++, L"{6B2D8401-C0C2-4060-BFEF-5DDFD04BD586}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            ValidateRestoreRelatedBundle(pPlan, dwIndex++, L"{33A8757F-32EA-4974-888E-D15547259B3C}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            ValidateRestoreRelatedBundle(pPlan, dwIndex++, L"{5C80A327-61B9-44CF-A6D4-64C45F4F90A9}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            ValidateRestoreRelatedBundle(pPlan, dwIndex++, L"{59CD5A25-0398-41CA-AD53-AD8C061E2A1A}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            ValidateRestoreRelatedBundle(pPlan, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void RelatedBundleMissingFromCacheTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            BURN_RELATED_BUNDLE* pRelatedBundle = DetectRelatedBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"0.9.0.0", BOOTSTRAPPER_RELATION_UPGRADE);
            pRelatedBundle->fPlannable = FALSE;

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(35694ull, pPlan->qwEstimatedSize);
            Assert::Equal(168715ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(1ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(2ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void SingleExeInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleExeManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPermanentPackagesAsPresentAndCached(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{9C184683-04FB-49AD-9D79-65101BDC3EE3}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{9C184683-04FB-49AD-9D79-65101BDC3EE3}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{9C184683-04FB-49AD-9D79-65101BDC3EE3}", L"{9C184683-04FB-49AD-9D79-65101BDC3EE3}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{9C184683-04FB-49AD-9D79-65101BDC3EE3}", L"{9C184683-04FB-49AD-9D79-65101BDC3EE3}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"ExeA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(1463267ull, pPlan->qwEstimatedSize);
            Assert::Equal(119695ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"ExeA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteExePackage(pPlan, fRollback, dwIndex++, L"ExeA", BOOTSTRAPPER_ACTION_STATE_INSTALL);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteExePackage(pPlan, fRollback, dwIndex++, L"ExeA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(1ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(2ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"NetFx48Web");
            ValidateCleanAction(pPlan, dwIndex++, L"ExeA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{9C184683-04FB-49AD-9D79-65101BDC3EE3}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(2ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[1], L"ExeA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void SingleMsiCacheTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectRelatedBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"0.9.0.0", BOOTSTRAPPER_RELATION_UPGRADE);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_CACHE);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_CACHE, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(33743ull, pPlan->qwEstimatedSize);
            Assert::Equal(168715ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(0ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(1ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void SingleMsiDowngradeTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectRelatedBundle(pEngineState, L"{AF8355C9-CCDD-4D61-BF5F-EA5F948D8F01}", L"1.1.0.0", BOOTSTRAPPER_RELATION_UPGRADE);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(FALSE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(TRUE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_NONE, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(0ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(0ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN, BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN);
        }

        [Fact]
        void SingleMsiForceAbsentTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectRelatedBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"0.9.0.0", BOOTSTRAPPER_RELATION_UPGRADE);

            vfUsePackageRequestState = TRUE;
            vPackageRequestState = BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT;
            vfUseRelatedBundleRequestState = TRUE;
            vRelatedBundleRequestState = BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT;

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(2ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(2ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{64633047-D172-4BBB-B202-64337D15C952}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void SingleMsiForcePresentTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);
            DetectRelatedBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"0.9.0.0", BOOTSTRAPPER_RELATION_UPGRADE);

            vfUsePackageRequestState = TRUE;
            vPackageRequestState = BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT;
            vfUseRelatedBundleRequestState = TRUE;
            vRelatedBundleRequestState = BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT;

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(35694ull, pPlan->qwEstimatedSize);
            Assert::Equal(175674ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(2ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(3ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            ValidateRestoreRelatedBundle(pPlan, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void SingleMsiInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            pEngineState->internalCommand.fArpSystemComponent = TRUE;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectRelatedBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"0.9.0.0", BOOTSTRAPPER_RELATION_UPGRADE);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY | BURN_REGISTRATION_ACTION_OPERATIONS_ARP_SYSTEM_COMPONENT, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(35694ull, pPlan->qwEstimatedSize);
            Assert::Equal(168715ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            ValidateExecuteRelatedBundle(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(2ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(3ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            ValidateRestoreRelatedBundle(pPlan, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void SingleMsiInstalledWithNoInstalledPackagesModifyTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectPackagesAsAbsent(pEngineState);

            pEngineState->registration.detectedRegistrationType = BOOTSTRAPPER_REGISTRATION_TYPE_FULL;

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_MODIFY);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_MODIFY, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(0ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(0ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void SingleMsiUninstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(1ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(1ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{64633047-D172-4BBB-B202-64337D15C952}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void SingleMsiUninstallWithDependentTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);
            DetectBundleDependent(pEngineState, L"{29855EB1-724D-4285-A89C-5D37D8549DCD}");

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(TRUE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(0ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(0ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN, BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN);
        }

        [Fact]
        void SingleMsiUninstallTestFromUpgradeBundleWithSameExactPackage()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectAsRelatedUpgradeBundle(&engineState, L"{02940F3E-C83E-452D-BFCF-C943777ACEAE}", L"2.0.0.0");

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(0ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(0ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_IGNORED, BURN_PACKAGE_REGISTRATION_STATE_IGNORED);
        }

        [Fact]
        void SingleMsiUnsafeUninstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifestFileName, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(TRUE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(1ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(1ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{A6F0CBF7-1578-450C-B9D7-9CF2EEC40002}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{64633047-D172-4BBB-B202-64337D15C952}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void SingleMsuInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsuManifestFileName, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{06077C60-DC46-4F4A-8D3C-05F869187191}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{06077C60-DC46-4F4A-8D3C-05F869187191}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{06077C60-DC46-4F4A-8D3C-05F869187191}", L"{06077C60-DC46-4F4A-8D3C-05F869187191}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{06077C60-DC46-4F4A-8D3C-05F869187191}", L"{06077C60-DC46-4F4A-8D3C-05F869187191}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"test.msu");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"test.msu");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(56ull, pPlan->qwEstimatedSize);
            Assert::Equal(140ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"test.msu");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsuPackage(pPlan, fRollback, dwIndex++, L"test.msu", BOOTSTRAPPER_ACTION_STATE_INSTALL);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"test.msu");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsuPackage(pPlan, fRollback, dwIndex++, L"test.msu", BOOTSTRAPPER_ACTION_STATE_UNINSTALL);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(1ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(2ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{06077C60-DC46-4F4A-8D3C-05F869187191}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(1ul, pEngineState->packages.cPackages);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"test.msu", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void SlipstreamInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSlipstreamManifestFileName, pEngineState);
            DetectPermanentPackagesAsPresentAndCached(pEngineState);
            PlanTestDetectPatchInitialize(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"NetFx48Web");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 3);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PatchA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 5);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(3055111ull, pPlan->qwEstimatedSize);
            Assert::Equal(6130592ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            BURN_EXECUTE_ACTION* pExecuteAction = NULL;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"NetFx48Web");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PatchA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PatchA", registerActions1, 1);
            pExecuteAction = ValidateDeletedExecuteMspTarget(pPlan, fRollback, dwIndex++, L"PatchA", BOOTSTRAPPER_ACTION_STATE_INSTALL, L"{5FF7F534-3FFC-41E0-80CD-E6361E5E7B7B}", TRUE, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, TRUE);
            ValidateExecuteMspTargetPatch(pExecuteAction, 0, L"PatchA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PatchA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PatchA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            pExecuteAction = ValidateDeletedExecuteMspTarget(pPlan, fRollback, dwIndex++, L"PatchA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, L"{5FF7F534-3FFC-41E0-80CD-E6361E5E7B7B}", TRUE, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, TRUE);
            ValidateExecuteMspTargetPatch(pExecuteAction, 0, L"PatchA");
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PatchA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(2ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(5ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(3ul, pEngineState->packages.cPackages);
            ValidatePermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"NetFx48Web");
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[1], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[2], L"PatchA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

        [Fact]
        void SlipstreamUninstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSlipstreamManifestFileName, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            BURN_EXECUTE_ACTION* pExecuteAction = NULL;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PatchA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PatchA", unregisterActions1, 1);
            pExecuteAction = ValidateDeletedExecuteMspTarget(pPlan, fRollback, dwIndex++, L"PatchA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, L"{5FF7F534-3FFC-41E0-80CD-E6361E5E7B7B}", TRUE, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, TRUE);
            ValidateExecuteMspTargetPatch(pExecuteAction, 0, L"PatchA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PatchA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PatchA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            pExecuteAction = ValidateDeletedExecuteMspTarget(pPlan, fRollback, dwIndex++, L"PatchA", BOOTSTRAPPER_ACTION_STATE_INSTALL, L"{5FF7F534-3FFC-41E0-80CD-E6361E5E7B7B}", TRUE, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, TRUE);
            ValidateExecuteMspTargetPatch(pExecuteAction, 0, L"PatchA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(2ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(2ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PatchA");
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{0A5113E3-06A5-4CE0-8E83-9EB42F6764A6}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{5FF7F534-3FFC-41E0-80CD-E6361E5E7B7B}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(3ul, pEngineState->packages.cPackages);
            ValidatePermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"NetFx48Web");
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[1], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[2], L"PatchA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void UnuninstallableExePackageForceAbsentTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSlipstreamModifiedManifestFileName, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);

            vfUsePackageRequestState = TRUE;
            vPackageRequestState = BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT;

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(1ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(1ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{DC94A8E0-4BF4-4026-B80B-2755DAFC05D3}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{5FF7F534-3FFC-41E0-80CD-E6361E5E7B7B}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(2ul, pEngineState->packages.cPackages);
            ValidatePermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"NetFx48Web");
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[1], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_ABSENT, BURN_PACKAGE_REGISTRATION_STATE_ABSENT);
        }

        [Fact]
        void UnuninstallableExePackageInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSlipstreamModifiedManifestFileName, pEngineState);
            DetectPackagesAsAbsent(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            NativeAssert::StringEqual(L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", pPlan->wzBundleId);
            NativeAssert::StringEqual(L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", pPlan->wzBundleProviderKey);
            Assert::Equal<BOOL>(FALSE, pPlan->fEnabledForwardCompatibleBundle);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(TRUE, pPlan->fCanAffectMachineState);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisallowRemoval);
            Assert::Equal<BOOL>(FALSE, pPlan->fDowngrade);
            Assert::Equal<DWORD>(BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE | BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY, pPlan->dwRegistrationOperations);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, TRUE, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}");
            Assert::Equal(dwIndex, pPlan->cRegistrationActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateDependentRegistrationAction(pPlan, fRollback, dwIndex++, FALSE, L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}");
            Assert::Equal(dwIndex, pPlan->cRollbackRegistrationActions);

            fRollback = FALSE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"NetFx48Web");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 4);
            ValidateCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"NetFx48Web");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 4);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(2993671ull, pPlan->qwEstimatedSize);
            Assert::Equal(6048672ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"NetFx48Web");
            ValidateExecuteExePackage(pPlan, fRollback, dwIndex++, L"NetFx48Web", BOOTSTRAPPER_ACTION_STATE_INSTALL);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitCachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", registerActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundaryStart(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"NetFx48Web");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{22D1DDBA-284D-40A7-BD14-95EA07906F21}", unregisterActions1, 1);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundaryEnd(pPlan, fRollback, dwIndex++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(2ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(4ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRestoreRelatedBundleActions);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{DC94A8E0-4BF4-4026-B80B-2755DAFC05D3}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);

            Assert::Equal(2ul, pEngineState->packages.cPackages);
            ValidatePermanentPackageExpectedStates(&pEngineState->packages.rgPackages[0], L"NetFx48Web");
            ValidateNonPermanentPackageExpectedStates(&pEngineState->packages.rgPackages[1], L"PackageA", BURN_PACKAGE_REGISTRATION_STATE_PRESENT, BURN_PACKAGE_REGISTRATION_STATE_PRESENT);
        }

    private:
        // This doesn't initialize everything, just enough for CorePlan to work.
        void InitializeEngineStateForCorePlan(LPCWSTR wzManifestFileName, BURN_ENGINE_STATE* pEngineState)
        {
            HRESULT hr = S_OK;
            LPWSTR sczFilePath = NULL;

            vfUsePackageRequestState = FALSE;
            vfUseRelatedBundleRequestState = FALSE;
            vfUseRelatedBundlePlanType = FALSE;

            ::InitializeCriticalSection(&pEngineState->userExperience.csEngineActive);

            hr = CacheInitialize(&pEngineState->cache, &pEngineState->internalCommand);
            NativeAssert::Succeeded(hr, "Failed to initialize cache.");

            hr = VariableInitialize(&pEngineState->variables);
            NativeAssert::Succeeded(hr, "Failed to initialize variables.");

            try
            {
                pin_ptr<const wchar_t> dataDirectory = PtrToStringChars(this->TestContext->TestDirectory);
                hr = PathConcat(dataDirectory, L"TestData\\PlanTest", &sczFilePath);
                NativeAssert::Succeeded(hr, "Failed to get path to test file directory.");
                hr = PathConcat(sczFilePath, wzManifestFileName, &sczFilePath);
                NativeAssert::Succeeded(hr, "Failed to get path to test file.");
                Assert::True(FileExistsEx(sczFilePath, NULL), "Test file does not exist.");

                hr = ManifestLoadXmlFromFile(sczFilePath, pEngineState);
                NativeAssert::Succeeded(hr, "Failed to load manifest.");
            }
            finally
            {
                ReleaseStr(sczFilePath);
            }

            hr = CoreInitializeConstants(pEngineState);
            NativeAssert::Succeeded(hr, "Failed to initialize core constants");

            hr = CacheInitializeSources(&pEngineState->cache, &pEngineState->registration, &pEngineState->variables, &pEngineState->internalCommand);
            NativeAssert::Succeeded(hr, "Failed to initialize cache sources.");

            pEngineState->userExperience.hUXModule = reinterpret_cast<HMODULE>(1);
            pEngineState->userExperience.pfnBAProc = PlanTestBAProc;
        }

        void PlanTestDetect(BURN_ENGINE_STATE* pEngineState)
        {
            DetectReset(&pEngineState->registration, &pEngineState->packages);
            PlanReset(&pEngineState->plan, &pEngineState->variables, &pEngineState->containers, &pEngineState->packages, &pEngineState->layoutPayloads);

            pEngineState->userExperience.fEngineActive = TRUE;
            pEngineState->fDetected = TRUE;
        }

        void PlanTestDetectPatchInitialize(BURN_ENGINE_STATE* pEngineState)
        {
            HRESULT hr = MsiEngineDetectInitialize(&pEngineState->packages);
            NativeAssert::Succeeded(hr, "MsiEngineDetectInitialize failed");

            for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
            {
                BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;

                if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
                {
                    for (DWORD j = 0; j < pPackage->Msp.cTargetProductCodes; ++j)
                    {
                        BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + j;

                        if (BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN == pTargetProduct->patchPackageState)
                        {
                            pTargetProduct->patchPackageState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
                        }
                    }
                }
            }
        }

        void DetectAttachedContainerAsAttached(BURN_ENGINE_STATE* pEngineState)
        {
            for (DWORD i = 0; i < pEngineState->containers.cContainers; ++i)
            {
                BURN_CONTAINER* pContainer = pEngineState->containers.rgContainers + i;
                if (pContainer->fAttached)
                {
                    pContainer->fActuallyAttached = TRUE;
                }
            }
        }

        void DetectCompatibleMsiPackage(BURN_ENGINE_STATE* pEngineState, BURN_PACKAGE* pPackage, LPCWSTR wzProductCode, LPCWSTR wzVersion)
        {
            HRESULT hr = S_OK;
            Assert(BOOTSTRAPPER_PACKAGE_STATE_PRESENT > pPackage->currentState);
            Assert(0 < pPackage->cDependencyProviders);
            BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders;
            BURN_COMPATIBLE_PACKAGE* pCompatiblePackage = &pPackage->compatiblePackage;
            pCompatiblePackage->fDetected = TRUE;
            pCompatiblePackage->fPlannable = TRUE;
            pCompatiblePackage->type = BURN_PACKAGE_TYPE_MSI;

            hr = StrAllocFormatted(&pCompatiblePackage->sczCacheId, L"%lsv%ls", wzProductCode, wzVersion);
            NativeAssert::Succeeded(hr, "Failed to format cache id");

            hr = StrAllocString(&pCompatiblePackage->Msi.sczVersion, wzVersion, 0);
            NativeAssert::Succeeded(hr, "Failed to copy MSI version");

            hr = VerParseVersion(wzVersion, 0, FALSE, &pCompatiblePackage->Msi.pVersion);
            NativeAssert::Succeeded(hr, "Failed to parse MSI version");

            hr = StrAllocString(&pCompatiblePackage->compatibleEntry.sczId, wzProductCode, 0);
            NativeAssert::Succeeded(hr, "Failed to copy product code");

            hr = StrAllocString(&pCompatiblePackage->compatibleEntry.sczVersion, wzVersion, 0);
            NativeAssert::Succeeded(hr, "Failed to copy version");

            hr = StrAllocString(&pCompatiblePackage->compatibleEntry.sczProviderKey, pProvider->sczKey, 0);
            NativeAssert::Succeeded(hr, "Failed to copy provider key");

            DetectPackageDependent(pPackage, pEngineState->registration.sczId);
        }

        void DetectPackageAsAbsent(BURN_PACKAGE* pPackage)
        {
            pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
            if (pPackage->fCanAffectRegistration)
            {
                pPackage->cacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
                pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
            }
        }

        void DetectPackageAsPresentAndCached(BURN_PACKAGE* pPackage)
        {
            pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_PRESENT;
            pPackage->fCached = TRUE;
            if (pPackage->fCanAffectRegistration)
            {
                pPackage->cacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
                pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
            }
        }

        void DetectBundleDependent(BURN_ENGINE_STATE* pEngineState, LPCWSTR wzId)
        {
            HRESULT hr = S_OK;
            BURN_DEPENDENCIES* pDependencies = &pEngineState->dependencies;
            BURN_REGISTRATION* pRegistration = &pEngineState->registration;

            hr = DepDependencyArrayAlloc(&pRegistration->rgDependents, &pRegistration->cDependents, wzId, NULL);
            NativeAssert::Succeeded(hr, "Failed to add package dependent");

            if (pDependencies->fSelfDependent || pDependencies->fActiveParent)
            {
                if (pDependencies->fActiveParent && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pDependencies->wzActiveParent, -1, wzId, -1))
                {
                    pRegistration->fParentRegisteredAsDependent = TRUE;
                }

                if (pDependencies->fSelfDependent && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pDependencies->wzSelfDependent, -1, wzId, -1))
                {
                    pRegistration->fSelfRegisteredAsDependent = TRUE;
                }
            }
        }

        void DetectPackageDependent(BURN_PACKAGE* pPackage, LPCWSTR wzId)
        {
            HRESULT hr = S_OK;

            for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
            {
                BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders + i;

                hr = DepDependencyArrayAlloc(&pProvider->rgDependents, &pProvider->cDependents, wzId, NULL);
                NativeAssert::Succeeded(hr, "Failed to add package dependent");

                pProvider->fExists = TRUE;
                pProvider->fBundleRegisteredAsDependent = TRUE;
            }
        }

        void DetectPackagesAsAbsent(BURN_ENGINE_STATE* pEngineState)
        {
            PlanTestDetect(pEngineState);

            for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
            {
                BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;
                DetectPackageAsAbsent(pPackage);
            }
        }

        void DetectPackagesAsPresentAndCached(BURN_ENGINE_STATE* pEngineState)
        {
            PlanTestDetect(pEngineState);

            pEngineState->registration.detectedRegistrationType = BOOTSTRAPPER_REGISTRATION_TYPE_FULL;

            if (pEngineState->dependencies.wzSelfDependent)
            {
                DetectBundleDependent(pEngineState, pEngineState->dependencies.wzSelfDependent);
            }

            for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
            {
                BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;
                DetectPackageAsPresentAndCached(pPackage);
                DetectPackageDependent(pPackage, pEngineState->registration.sczId);

                if (BURN_PACKAGE_TYPE_MSI == pPackage->type)
                {
                    for (DWORD j = 0; j < pPackage->Msi.cSlipstreamMspPackages; ++j)
                    {
                        pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED;

                        BURN_PACKAGE* pMspPackage = pPackage->Msi.rgSlipstreamMsps[j].pMspPackage;
                        MspEngineAddDetectedTargetProduct(&pEngineState->packages, pMspPackage, j, pPackage->Msi.sczProductCode, pPackage->fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED);

                        BURN_MSPTARGETPRODUCT* pTargetProduct = pMspPackage->Msp.rgTargetProducts + (pMspPackage->Msp.cTargetProductCodes - 1);
                        pTargetProduct->patchPackageState = BOOTSTRAPPER_PACKAGE_STATE_PRESENT;
                        pTargetProduct->registrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
                    }
                }
            }
        }

        void DetectPermanentPackagesAsPresentAndCached(BURN_ENGINE_STATE* pEngineState)
        {
            PlanTestDetect(pEngineState);

            pEngineState->registration.detectedRegistrationType = BOOTSTRAPPER_REGISTRATION_TYPE_FULL;

            for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
            {
                BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;
                if (!pPackage->fPermanent)
                {
                    DetectPackageAsAbsent(pPackage);
                }
                else
                {
                    DetectPackageAsPresentAndCached(pPackage);
                    DetectPackageDependent(pPackage, pEngineState->registration.sczId);
                }
            }
        }

        BURN_RELATED_BUNDLE* DetectRelatedBundle(
            __in BURN_ENGINE_STATE* pEngineState,
            __in LPCWSTR wzId,
            __in LPCWSTR wzVersion,
            __in BOOTSTRAPPER_RELATION_TYPE relationType
            )
        {
            HRESULT hr = S_OK;
            BURN_RELATED_BUNDLES* pRelatedBundles = &pEngineState->registration.relatedBundles;
            BURN_DEPENDENCY_PROVIDER dependencyProvider = { };
            LPCWSTR wzFilePath = pEngineState->registration.sczExecutableName;

            hr = StrAllocString(&dependencyProvider.sczKey, wzId, 0);
            NativeAssert::Succeeded(hr, "Failed to copy provider key");

            hr = StrAllocString(&dependencyProvider.sczDisplayName, wzId, 0);
            NativeAssert::Succeeded(hr, "Failed to copy display name");

            dependencyProvider.fImported = TRUE;

            hr = StrAllocString(&dependencyProvider.sczVersion, wzVersion, 0);
            NativeAssert::Succeeded(hr, "Failed to copy version");

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pRelatedBundles->rgRelatedBundles), pRelatedBundles->cRelatedBundles + 1, sizeof(BURN_RELATED_BUNDLE), 5);
            NativeAssert::Succeeded(hr, "Failed to ensure there is space for related bundles.");

            BURN_RELATED_BUNDLE* pRelatedBundle = pRelatedBundles->rgRelatedBundles + pRelatedBundles->cRelatedBundles;

            hr = VerParseVersion(wzVersion, 0, FALSE, &pRelatedBundle->pVersion);
            NativeAssert::Succeeded(hr, "Failed to parse pseudo bundle version: %ls", wzVersion);

            pRelatedBundle->fPlannable = TRUE;
            pRelatedBundle->detectRelationType = relationType;

            hr = PseudoBundleInitializeRelated(&pRelatedBundle->package, TRUE, TRUE, wzId,
#ifdef DEBUG
                                               pRelatedBundle->detectRelationType,
#endif
                                               TRUE, wzFilePath, 0, &dependencyProvider);
            NativeAssert::Succeeded(hr, "Failed to initialize related bundle to represent bundle: %ls", wzId);

            ++pRelatedBundles->cRelatedBundles;

            return pRelatedBundle;
        }

        void DetectAsRelatedUpgradeBundle(
            __in BURN_ENGINE_STATE* pEngineState,
            __in LPCWSTR wzId,
            __in LPCWSTR wzVersion
            )
        {
            HRESULT hr = StrAllocString(&pEngineState->internalCommand.sczAncestors, wzId, 0);
            NativeAssert::Succeeded(hr, "Failed to set registration's ancestors");

            hr = StrAllocFormatted(&pEngineState->registration.sczBundlePackageAncestors, L"%ls;%ls", wzId, pEngineState->registration.sczId);
            NativeAssert::Succeeded(hr, "Failed to set registration's package ancestors");

            pEngineState->command.relationType = BOOTSTRAPPER_RELATION_UPGRADE;

            DetectPackagesAsPresentAndCached(pEngineState);
            DetectRelatedBundle(pEngineState, wzId, wzVersion, BOOTSTRAPPER_RELATION_UPGRADE);

            for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
            {
                BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;
                DetectPackageDependent(pPackage, wzId);
            }
        }

        void ValidateCacheContainer(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzContainerId
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_CONTAINER, pAction->type);
            NativeAssert::StringEqual(wzContainerId, pAction->container.pContainer->sczId);
        }

        BURN_CACHE_ACTION* ValidateCacheActionExists(BURN_PLAN* pPlan, BOOL fRollback, DWORD dwIndex)
        {
            Assert::InRange(dwIndex + 1ul, 1ul, (fRollback ? pPlan->cRollbackCacheActions : pPlan->cCacheActions));
            return (fRollback ? pPlan->rgRollbackCacheActions : pPlan->rgCacheActions) + dwIndex;
        }

        void ValidateCacheCheckpoint(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in DWORD dwId
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_CHECKPOINT, pAction->type);
            Assert::Equal(dwId, pAction->checkpoint.dwId);
        }

        DWORD ValidateCachePackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->package.pPackage->sczId);
            return dwIndex + 1;
        }

        void ValidateCacheRollbackPackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->rollbackPackage.pPackage->sczId);
        }

        void ValidateCacheSignalSyncpoint(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT, pAction->type);
            Assert::NotEqual((DWORD_PTR)NULL, (DWORD_PTR)pAction->syncpoint.hEvent);
        }

        void ValidateCleanAction(
            __in BURN_PLAN* pPlan,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId
            )
        {
            BURN_CLEAN_ACTION* pCleanAction = ValidateCleanActionExists(pPlan, dwIndex);
            Assert::Equal<DWORD>(BURN_CLEAN_ACTION_TYPE_PACKAGE, pCleanAction->type);
            Assert::NotEqual((DWORD_PTR)0, (DWORD_PTR)pCleanAction->pPackage);
            NativeAssert::StringEqual(wzPackageId, pCleanAction->pPackage->sczId);
        }

        BURN_CLEAN_ACTION* ValidateCleanActionExists(BURN_PLAN* pPlan, DWORD dwIndex)
        {
            Assert::InRange(dwIndex + 1ul, 1ul, pPlan->cCleanActions);
            return pPlan->rgCleanActions + dwIndex;
        }

        void ValidateCleanCompatibleAction(
            __in BURN_PLAN* pPlan,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId
            )
        {
            BURN_CLEAN_ACTION* pCleanAction = ValidateCleanActionExists(pPlan, dwIndex);
            Assert::Equal<DWORD>(BURN_CLEAN_ACTION_TYPE_COMPATIBLE_PACKAGE, pCleanAction->type);
            Assert::NotEqual((DWORD_PTR)0, (DWORD_PTR)pCleanAction->pPackage);
            NativeAssert::StringEqual(wzPackageId, pCleanAction->pPackage->sczId);
        }

        BURN_DEPENDENT_REGISTRATION_ACTION* ValidateDependentRegistrationActionExists(BURN_PLAN* pPlan, BOOL fRollback, DWORD dwIndex)
        {
            Assert::InRange(dwIndex + 1ul, 1ul, (fRollback ? pPlan->cRollbackRegistrationActions : pPlan->cRegistrationActions));
            return (fRollback ? pPlan->rgRollbackRegistrationActions : pPlan->rgRegistrationActions) + dwIndex;
        }

        void ValidateDependentRegistrationAction(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in BOOL fRegister,
            __in LPCWSTR wzBundleId,
            __in LPCWSTR wzProviderKey
            )
        {
            BURN_DEPENDENT_REGISTRATION_ACTION* pAction = ValidateDependentRegistrationActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(fRegister ? BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER : BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER, pAction->type);
            NativeAssert::StringEqual(wzBundleId, pAction->sczBundleId);
            NativeAssert::StringEqual(wzProviderKey, pAction->sczDependentProviderKey);
        }

        BURN_EXECUTE_ACTION* ValidateExecuteActionExists(BURN_PLAN* pPlan, BOOL fRollback, DWORD dwIndex)
        {
            Assert::InRange(dwIndex + 1ul, 1ul, (fRollback ? pPlan->cRollbackActions : pPlan->cExecuteActions));
            return (fRollback ? pPlan->rgRollbackActions : pPlan->rgExecuteActions) + dwIndex;
        }

        void ValidateExecuteBeginMsiTransaction(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzRollbackBoundaryId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_BEGIN_MSI_TRANSACTION, pAction->type);
            NativeAssert::StringEqual(wzRollbackBoundaryId, pAction->msiTransaction.pRollbackBoundary->sczId);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteCheckpoint(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in DWORD dwId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_CHECKPOINT, pAction->type);
            Assert::Equal(dwId, pAction->checkpoint.dwId);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteCommitMsiTransaction(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzRollbackBoundaryId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_COMMIT_MSI_TRANSACTION, pAction->type);
            NativeAssert::StringEqual(wzRollbackBoundaryId, pAction->msiTransaction.pRollbackBoundary->sczId);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteRelatedBundle(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action,
            __in LPCWSTR wzIgnoreDependencies
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->relatedBundle.pRelatedBundle->package.sczId);
            Assert::Equal<DWORD>(action, pAction->relatedBundle.action);
            NativeAssert::StringEqual(wzIgnoreDependencies, pAction->relatedBundle.sczIgnoreDependencies);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteBundlePackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action,
            __in LPCWSTR wzParent
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_BUNDLE_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->bundlePackage.pPackage->sczId);
            Assert::Equal<DWORD>(action, pAction->bundlePackage.action);
            NativeAssert::StringEqual(wzParent, pAction->bundlePackage.sczParent);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteExePackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->exePackage.pPackage->sczId);
            Assert::Equal<DWORD>(action, pAction->exePackage.action);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteMsiPackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in_z LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action,
            __in BURN_MSI_PROPERTY actionMsiProperty,
            __in DWORD uiLevel,
            __in BOOL fDisableExternalUiHandler,
            __in BOOTSTRAPPER_MSI_FILE_VERSIONING fileVersioning,
            __in DWORD dwLoggingAttributes
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->msiPackage.pPackage->sczId);
            Assert::Equal<DWORD>(action, pAction->msiPackage.action);
            Assert::Equal<DWORD>(actionMsiProperty, pAction->msiPackage.actionMsiProperty);
            Assert::Equal<DWORD>(uiLevel, pAction->msiPackage.uiLevel);
            Assert::Equal<BOOL>(fDisableExternalUiHandler, pAction->msiPackage.fDisableExternalUiHandler);
            Assert::Equal<DWORD>(fileVersioning, pAction->msiPackage.fileVersioning);
            NativeAssert::NotNull(pAction->msiPackage.sczLogPath);
            Assert::Equal<DWORD>(dwLoggingAttributes, pAction->msiPackage.dwLoggingAttributes);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        BURN_EXECUTE_ACTION* ValidateDeletedExecuteMspTarget(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in_z LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action,
            __in_z LPCWSTR wzTargetProductCode,
            __in BOOL fPerMachineTarget,
            __in BURN_MSI_PROPERTY actionMsiProperty,
            __in DWORD uiLevel,
            __in BOOL fDisableExternalUiHandler,
            __in BOOTSTRAPPER_MSI_FILE_VERSIONING fileVersioning,
            __in BOOL fDeleted
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_MSP_TARGET, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->mspTarget.pPackage->sczId);
            Assert::Equal<DWORD>(action, pAction->mspTarget.action);
            NativeAssert::StringEqual(wzTargetProductCode, pAction->mspTarget.sczTargetProductCode);
            Assert::Equal<BOOL>(fPerMachineTarget, pAction->mspTarget.fPerMachineTarget);
            Assert::Equal<DWORD>(actionMsiProperty, pAction->mspTarget.actionMsiProperty);
            Assert::Equal<DWORD>(uiLevel, pAction->mspTarget.uiLevel);
            Assert::Equal<BOOL>(fDisableExternalUiHandler, pAction->mspTarget.fDisableExternalUiHandler);
            Assert::Equal<DWORD>(fileVersioning, pAction->mspTarget.fileVersioning);
            NativeAssert::NotNull(pAction->mspTarget.sczLogPath);
            Assert::Equal<BOOL>(fDeleted, pAction->fDeleted);
            return pAction;
        }

        void ValidateExecuteMspTargetPatch(
            __in BURN_EXECUTE_ACTION* pAction,
            __in DWORD dwIndex,
            __in_z LPCWSTR wzPackageId
            )
        {
            Assert::InRange(dwIndex + 1ul, 1ul, pAction->mspTarget.cOrderedPatches);
            BURN_ORDERED_PATCHES* pOrderedPatch = pAction->mspTarget.rgOrderedPatches + dwIndex;
            NativeAssert::StringEqual(wzPackageId, pOrderedPatch->pPackage->sczId);
        }

        void ValidateExecuteMsuPackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->msuPackage.pPackage->sczId);
            Assert::Equal<DWORD>(action, pAction->msuPackage.action);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecutePackageDependency(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in LPCWSTR wzBundleProviderKey,
            __in BURN_DEPENDENCY_ACTION* rgActions,
            __in DWORD cActions
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->packageDependency.pPackage->sczId);
            NativeAssert::StringEqual(wzBundleProviderKey, pAction->packageDependency.sczBundleProviderKey);
            Assert::Equal<DWORD>(cActions, pAction->packageProvider.pPackage->cDependencyProviders);
            for (DWORD i = 0; i < cActions; ++i)
            {
                const BURN_DEPENDENCY_PROVIDER* pProvider = pAction->packageProvider.pPackage->rgDependencyProviders + i;
                Assert::Equal<DWORD>(rgActions[i], fRollback ? pProvider->dependentRollback : pProvider->dependentExecute);
            }
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecutePackageProvider(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BURN_DEPENDENCY_ACTION* rgActions,
            __in DWORD cActions
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->packageProvider.pPackage->sczId);
            Assert::Equal<DWORD>(cActions, pAction->packageProvider.pPackage->cDependencyProviders);
            for (DWORD i = 0; i < cActions; ++i)
            {
                const BURN_DEPENDENCY_PROVIDER* pProvider = pAction->packageProvider.pPackage->rgDependencyProviders + i;
                Assert::Equal<DWORD>(rgActions[i], fRollback ? pProvider->providerRollback : pProvider->providerExecute);
            }
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteRollbackBoundaryStart(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzId,
            __in BOOL fVital,
            __in BOOL fTransaction
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_START, pAction->type);
            NativeAssert::StringEqual(wzId, pAction->rollbackBoundary.pRollbackBoundary->sczId);
            Assert::Equal<BOOL>(fVital, pAction->rollbackBoundary.pRollbackBoundary->fVital);
            Assert::Equal<BOOL>(fTransaction, pAction->rollbackBoundary.pRollbackBoundary->fTransaction);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteRollbackBoundaryEnd(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_END, pAction->type);
            Assert::Equal<size_t>(NULL, (size_t)(pAction->rollbackBoundary.pRollbackBoundary));
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteUncachePackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->uncachePackage.pPackage->sczId);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateExecuteWaitCachePackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in_z LPCWSTR wzPackageId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_WAIT_CACHE_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->waitCachePackage.pPackage->sczId);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        void ValidateNonPermanentPackageExpectedStates(
            __in BURN_PACKAGE* pPackage,
            __in_z LPCWSTR wzPackageId,
            __in BURN_PACKAGE_REGISTRATION_STATE expectedCacheState,
            __in BURN_PACKAGE_REGISTRATION_STATE expectedInstallState
            )
        {
            NativeAssert::StringEqual(wzPackageId, pPackage->sczId);
            Assert::Equal<BOOL>(TRUE, pPackage->fCanAffectRegistration);
            Assert::Equal<DWORD>(expectedCacheState, pPackage->expectedCacheRegistrationState);
            Assert::Equal<DWORD>(expectedInstallState, pPackage->expectedInstallRegistrationState);
        }

        void ValidatePermanentPackageExpectedStates(
            __in BURN_PACKAGE* pPackage,
            __in_z LPCWSTR wzPackageId
            )
        {
            NativeAssert::StringEqual(wzPackageId, pPackage->sczId);
            Assert::Equal<BOOL>(FALSE, pPackage->fCanAffectRegistration);
            Assert::Equal<DWORD>(BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN, pPackage->expectedCacheRegistrationState);
            Assert::Equal<DWORD>(BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN, pPackage->expectedInstallRegistrationState);
        }

        void ValidatePlannedProvider(
            __in BURN_PLAN* pPlan,
            __in UINT uIndex,
            __in LPCWSTR wzKey,
            __in LPCWSTR wzName
            )
        {
            Assert::InRange(uIndex + 1u, 1u, pPlan->cPlannedProviders);

            DEPENDENCY* pProvider = pPlan->rgPlannedProviders + uIndex;
            NativeAssert::StringEqual(wzKey, pProvider->sczKey);
            NativeAssert::StringEqual(wzName, pProvider->sczName);
        }

        void ValidateRestoreRelatedBundle(
            __in BURN_PLAN* pPlan,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action,
            __in LPCWSTR wzIgnoreDependencies
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateRestoreRelatedBundleActionExists(pPlan, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->relatedBundle.pRelatedBundle->package.sczId);
            Assert::Equal<DWORD>(action, pAction->relatedBundle.action);
            NativeAssert::StringEqual(wzIgnoreDependencies, pAction->relatedBundle.sczIgnoreDependencies);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }

        BURN_EXECUTE_ACTION* ValidateRestoreRelatedBundleActionExists(BURN_PLAN* pPlan, DWORD dwIndex)
        {
            Assert::InRange(dwIndex + 1ul, 1ul, pPlan->cRestoreRelatedBundleActions);
            return pPlan->rgRestoreRelatedBundleActions + dwIndex;
        }

        void ValidateUninstallMsiCompatiblePackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in_z LPCWSTR wzPackageId,
            __in DWORD dwLoggingAttributes
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_UNINSTALL_MSI_COMPATIBLE_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->uninstallMsiCompatiblePackage.pParentPackage->sczId);
            NativeAssert::NotNull(pAction->msiPackage.sczLogPath);
            Assert::Equal<DWORD>(dwLoggingAttributes, pAction->uninstallMsiCompatiblePackage.dwLoggingAttributes);
            Assert::Equal<BOOL>(FALSE, pAction->fDeleted);
        }
    };
}
}
}
}
}

static HRESULT WINAPI PlanTestBAProc(
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in const LPVOID /*pvArgs*/,
    __inout LPVOID pvResults,
    __in_opt LPVOID /*pvContext*/
    )
{
    switch (message)
    {
    case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN:
        if (vfUsePackageRequestState)
        {
            BA_ONPLANPACKAGEBEGIN_RESULTS* pResults = reinterpret_cast<BA_ONPLANPACKAGEBEGIN_RESULTS*>(pvResults);
            pResults->requestedState = vPackageRequestState;
        }
        break;
    case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE:
        if (vfUseRelatedBundleRequestState)
        {
            BA_ONPLANRELATEDBUNDLE_RESULTS* pResults = reinterpret_cast<BA_ONPLANRELATEDBUNDLE_RESULTS*>(pvResults);
            pResults->requestedState = vRelatedBundleRequestState;
        }
        break;
    case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLETYPE:
        if (vfUseRelatedBundlePlanType)
        {
            BA_ONPLANRELATEDBUNDLETYPE_RESULTS* pResults = reinterpret_cast<BA_ONPLANRELATEDBUNDLETYPE_RESULTS*>(pvResults);
            pResults->requestedType = vRelatedBundlePlanType;
        }
        break;
    }

    return S_OK;
}
