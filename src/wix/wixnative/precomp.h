#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include <windows.h>
#include <aclapi.h>
#include <mergemod.h>
#include <softpub.h>
#include <strsafe.h>
#include <wintrust.h>

#include "dutil.h"
#include "certutil.h"
#include "conutil.h"
#include "memutil.h"
#include "pathutil.h"
#include "strutil.h"
#include "cabcutil.h"
#include "cabutil.h"

HRESULT WixNativeReadStdinPreamble();
HRESULT CertificateHashesCommand(__in int argc, __in_ecount(argc) LPWSTR argv[]);
HRESULT SmartCabCommand(__in int argc, __in_ecount(argc) LPWSTR argv[]);
HRESULT ResetAclsCommand(__in int argc, __in_ecount(argc) LPWSTR argv[]);
HRESULT EnumCabCommand(__in int argc, __in_ecount(argc) LPWSTR argv[]);
HRESULT ExtractCabCommand(__in int argc, __in_ecount(argc) LPWSTR argv[]);
