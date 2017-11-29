#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include <windows.h>
#include <aclapi.h>
#include <mergemod.h>

#include "dutil.h"
#include "conutil.h"
#include "memutil.h"
#include "pathutil.h"
#include "strutil.h"
#include "cabcutil.h"
#include "cabutil.h"

HRESULT SmartCabCommand(int argc, LPWSTR argv[]);
HRESULT ResetAclsCommand(int argc, LPWSTR argv[]);
HRESULT EnumCabCommand(int argc, LPWSTR argv[]);
HRESULT ExtractCabCommand(int argc, LPWSTR argv[]);
