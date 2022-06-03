// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define TEST_REGISTRY_FIXTURE_ROOT_PATH L"SOFTWARE\\WiX_Burn_UnitTest"
#define TEST_REGISTRY_FIXTURE_HKLM_PATH TEST_REGISTRY_FIXTURE_ROOT_PATH L"\\HKLM"
#define TEST_REGISTRY_FIXTURE_HKLM32_PATH TEST_REGISTRY_FIXTURE_ROOT_PATH L"\\Wow6432Node\\HKLM"
#define TEST_REGISTRY_FIXTURE_HKCU_PATH TEST_REGISTRY_FIXTURE_ROOT_PATH L"\\HKCU"
#define TEST_REGISTRY_FIXTURE_HKCU32_PATH TEST_REGISTRY_FIXTURE_ROOT_PATH L"\\Wow6432Node\\HKCU"

static REG_KEY_BITNESS GetDesiredBitness(
    __in REGSAM samDesired
    )
{
    REG_KEY_BITNESS desiredBitness = REG_KEY_DEFAULT;

    switch (KEY_WOW64_RES & samDesired)
    {
    case KEY_WOW64_32KEY:
        desiredBitness = REG_KEY_32BIT;
        break;
    case KEY_WOW64_64KEY:
        desiredBitness = REG_KEY_64BIT;
        break;
    default:
#if defined(_WIN64)
        desiredBitness = REG_KEY_64BIT;
#else
        desiredBitness = REG_KEY_32BIT;
#endif
        break;
    }

    return desiredBitness;
}

static LSTATUS GetRootKey(
    __in HKEY hKey,
    __in REGSAM samDesired,
    __in ACCESS_MASK accessDesired,
    __inout HKEY* phkRoot)
{
    LSTATUS ls = ERROR_SUCCESS;
    LPCWSTR wzRoot = NULL;

    if (HKEY_LOCAL_MACHINE == hKey)
    {
        if (REG_KEY_32BIT == GetDesiredBitness(samDesired))
        {
            wzRoot = TEST_REGISTRY_FIXTURE_HKLM32_PATH;
        }
        else
        {
            wzRoot = TEST_REGISTRY_FIXTURE_HKLM_PATH;
        }
    }
    else if (HKEY_CURRENT_USER == hKey)
    {
        if (REG_KEY_32BIT == GetDesiredBitness(samDesired))
        {
            wzRoot = TEST_REGISTRY_FIXTURE_HKCU32_PATH;
        }
        else
        {
            wzRoot = TEST_REGISTRY_FIXTURE_HKCU_PATH;
        }
    }

    if (wzRoot)
    {
        ls = ::RegOpenKeyExW(HKEY_CURRENT_USER, wzRoot, 0, KEY_WRITE | accessDesired, phkRoot);
    }
    else
    {
        *phkRoot = hKey;
    }

    return ls;
}

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
    HKEY hkRoot = NULL;

    ls = GetRootKey(hKey, samDesired, 0, &hkRoot);
    if (ERROR_SUCCESS != ls)
    {
        ExitFunction();
    }

    ls = ::RegCreateKeyExW(hkRoot, lpSubKey, Reserved, lpClass, dwOptions, samDesired, lpSecurityAttributes, phkResult, lpdwDisposition);

LExit:
    if (hkRoot != hKey)
    {
        ReleaseRegKey(hkRoot);
    }

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
    HKEY hkRoot = NULL;

    ls = GetRootKey(hKey, samDesired, 0, &hkRoot);
    if (ERROR_SUCCESS != ls)
    {
        ExitFunction();
    }

    ls = ::RegOpenKeyExW(hkRoot, lpSubKey, ulOptions, samDesired, phkResult);

LExit:
    if (hkRoot != hKey)
    {
        ReleaseRegKey(hkRoot);
    }

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
    HKEY hkRoot = NULL;

    ls = GetRootKey(hKey, samDesired, samDesired, &hkRoot);
    if (ERROR_SUCCESS != ls)
    {
        ExitFunction();
    }

    ls = ::RegDeleteKeyExW(hkRoot, lpSubKey, samDesired, Reserved);

LExit:
    if (hkRoot != hKey)
    {
        ReleaseRegKey(hkRoot);
    }

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
        }

        TestRegistryFixture::~TestRegistryFixture()
        {
            this->TearDown();
        }

        void TestRegistryFixture::SetUp()
        {
            // set mock API's
            RegFunctionOverride(TestRegistryFixture_RegCreateKeyExW, TestRegistryFixture_RegOpenKeyExW, TestRegistryFixture_RegDeleteKeyExW, NULL, NULL, NULL, NULL, NULL, NULL, NULL);

            Registry::CurrentUser->CreateSubKey(TEST_REGISTRY_FIXTURE_HKCU_PATH);
            Registry::CurrentUser->CreateSubKey(TEST_REGISTRY_FIXTURE_HKCU32_PATH);
            Registry::CurrentUser->CreateSubKey(TEST_REGISTRY_FIXTURE_HKLM_PATH);
            Registry::CurrentUser->CreateSubKey(TEST_REGISTRY_FIXTURE_HKLM32_PATH);
        }

        void TestRegistryFixture::TearDown()
        {
            Registry::CurrentUser->DeleteSubKeyTree(this->rootPath, false);

            RegFunctionOverride(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
        }

        String^ TestRegistryFixture::GetDirectHkcuPath(REG_KEY_BITNESS bitness, ... array<String^>^ paths)
        {
            String^ hkcuPath;

            switch (bitness)
            {
            case REG_KEY_32BIT:
                hkcuPath = TEST_REGISTRY_FIXTURE_HKCU32_PATH;
                break;
            case REG_KEY_64BIT:
                hkcuPath = TEST_REGISTRY_FIXTURE_HKCU_PATH;
                break;
            default:
#if defined(_WIN64)
                hkcuPath = TEST_REGISTRY_FIXTURE_HKCU_PATH;
#else
                hkcuPath = TEST_REGISTRY_FIXTURE_HKCU32_PATH;
#endif
                break;
            }

            return Path::Combine(Registry::CurrentUser->Name, hkcuPath, Path::Combine(paths));
        }

        String^ TestRegistryFixture::GetDirectHklmPath(REG_KEY_BITNESS bitness, ... array<String^>^ paths)
        {
            String^ hklmPath;

            switch (bitness)
            {
            case REG_KEY_32BIT:
                hklmPath = TEST_REGISTRY_FIXTURE_HKLM32_PATH;
                break;
            case REG_KEY_64BIT:
                hklmPath = TEST_REGISTRY_FIXTURE_HKLM_PATH;
                break;
            default:
#if defined(_WIN64)
                hklmPath = TEST_REGISTRY_FIXTURE_HKLM_PATH;
#else
                hklmPath = TEST_REGISTRY_FIXTURE_HKLM32_PATH;
#endif
                break;
            }

            return Path::Combine(Registry::CurrentUser->Name, hklmPath, Path::Combine(paths));
        }
    }
}
