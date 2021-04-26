#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if _WIN32_MSI < 150
#define _WIN32_MSI 150
#endif

#include <windows.h>
#include <msiquery.h>

#include <strsafe.h>

#include "wcautil.h"
#include "fileutil.h"
#include "memutil.h"
#include "strutil.h"
#include "wiutil.h"

#include "CustomMsiErrors.h"

#include "sca.h"
#include "scacost.h"
#include "scasqlstr.h"

#include "caDecor.h"
