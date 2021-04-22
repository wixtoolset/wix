#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>
#include <msiquery.h>
#include <wchar.h>
#include <strsafe.h>

const WCHAR MAGIC_MULTISZ_DELIM = 128;

#include "wcautil.h"
#include "inc\wcalog.h"
#include "inc\wcawow64.h"
#include "inc\wcawrapquery.h"
#include "wiutil.h"
#include "fileutil.h"
#include "memutil.h"
#include "strutil.h"
