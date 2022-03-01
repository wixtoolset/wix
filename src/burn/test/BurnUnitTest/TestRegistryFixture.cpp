// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define TEST_REGISTRY_FIXTURE_ROOT_PATH L"SOFTWARE\\WiX_Burn_UnitTest"
#define TEST_REGISTRY_FIXTURE_HKLM_PATH TEST_REGISTRY_FIXTURE_ROOT_PATH L"\\HKLM"
#define TEST_REGISTRY_FIXTURE_HKCU_PATH TEST_REGISTRY_FIXTURE_ROOT_PATH L"\\HKCU"

static LSTATUS APIENTRY TestRegistryFixture_RegCreateKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __reserved DWORD Reserved,
    __in_opt LPWSTR lpClass,
    __in DWORD dwOptions,
    __in REGSAM samDesired,
    __in_opt CONST LPSECURITY_ATTRIBUTES lpSecurityAttributes,
    __out PHKEY phkResult,
    __out_opt LPDWORD lpdwDisposition
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = TEST_REGISTRY_FIXTURE_HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = TEST_REGISTRY_FIXTURE_HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegCreateKeyExW(hkRoot, lpSubKey, Reserved, lpClass, dwOptions, samDesired, lpSecurityAttributes, phkResult, lpdwDisposition);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}

static LSTATUS APIENTRY TestRegistryFixture_RegOpenKeyExW(
    __in HKEY hKey,
    __in_opt LPCWSTR lpSubKey,
    __reserved DWORD ulOptions,
    __in REGSAM samDesired,
    __out PHKEY phkResult
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = TEST_REGISTRY_FIXTURE_HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = TEST_REGISTRY_FIXTURE_HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegOpenKeyExW(hkRoot, lpSubKey, ulOptions, samDesired, phkResult);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}

static LSTATUS APIENTRY TestRegistryFixture_RegDeleteKeyExW(
    __in HKEY hKey,
    __in LPCWSTR lpSubKey,
    __in REGSAM samDesired,
    __reserved DWORD Reserved
    )
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;
    HKEY hkRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        wzRoot = TEST_REGISTRY_FIXTURE_HKLM_PATH;
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        wzRoot = TEST_REGISTRY_FIXTURE_HKCU_PATH;
    }
    else
    {
        hkRoot = hKey;
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE | samDesired, &hkRoot);
        if (ERROR_SUCCESS != ls)
        {
            ExitFunction();
        }
    }

    ls = ::RegDeleteKeyExW(hkRoot, lpSubKey, samDesired, Reserved);

LExit:
    ReleaseRegKey(hkRoot);

    return ls;
}

namespace WixBuildTools
{
    namespace TestSupport
    {
        using namespace System;
        using namespace System::IO;
        using namespace Microsoft::Win32;

        TestRegistryFixture::TestRegistryFixture()
        {
            this->rootPath = gcnew String(TEST_REGISTRY_FIXTURE_ROOT_PATH);
            this->hkcuPath = gcnew String(TEST_REGISTRY_FIXTURE_HKCU_PATH);
            this->hklmPath = gcnew String(TEST_REGISTRY_FIXTURE_HKLM_PATH);
        }

        TestRegistryFixture::~TestRegistryFixture()
        {
            this->TearDown();
        }

        void TestRegistryFixture::SetUp()
        {
            // set mock API's
            RegFunctionOverride(TestRegistryFixture_RegCreateKeyExW, TestRegistryFixture_RegOpenKeyExW, TestRegistryFixture_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL);

            Registry::CurrentUser->CreateSubKey(this->hkcuPath);
            Registry::CurrentUser->CreateSubKey(this->hklmPath);
        }

        void TestRegistryFixture::TearDown()
        {
            Registry::CurrentUser->DeleteSubKeyTree(this->rootPath, false);

            RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
        }

        String^ TestRegistryFixture::GetDirectHkcuPath(... array<String^>^ paths)
        {
            return Path::Combine(Registry::CurrentUser->Name, this->hkcuPath, Path::Combine(paths));
        }

        String^ TestRegistryFixture::GetDirectHklmPath(... array<String^>^ paths)
        {
            return Path::Combine(Registry::CurrentUser->Name, this->hklmPath, Path::Combine(paths));
        }
    }
}
