// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#pragma section(".wixburn",read)

// If these defaults ever change, be sure to update constants in burn\engine\section.cpp as well.
#pragma data_seg(push, ".wixburn")
static DWORD dwMagic = 0x00f14300;
static DWORD dwVersion = 0x00000003;

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
