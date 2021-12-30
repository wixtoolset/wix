// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#pragma section(BURN_SECTION_NAME,read)

#pragma data_seg(push, BURN_SECTION_NAME)
static DWORD dwMagic = BURN_SECTION_MAGIC;
static DWORD dwVersion = BURN_SECTION_VERSION;

static GUID guidBundleId = { };

static DWORD dwStubSize = 0;
static DWORD dwOriginalChecksum = 0;
static DWORD dwOriginalSignatureOffset = 0;
static DWORD dwOriginalSignatureSize = 0;

static DWORD dwContainerFormat = 1;
static DWORD dwContainerCount = 0;
// (512 (minimum section size) - 48 (size of above data)) / 4 (size of DWORD)
static DWORD qwAttachedContainerSizes[116];
#pragma data_seg(pop)
