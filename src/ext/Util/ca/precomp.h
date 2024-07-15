#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if _WIN32_MSI < 150
#define _WIN32_MSI 150
#endif

#include <windows.h>
#include <msiquery.h>
#include <msidefs.h>
#include <stierr.h>

#include <strsafe.h>

#include <msxml2.h>
#include <Iads.h>
#include <activeds.h>
#include <lm.h>        // NetApi32.lib
#include <Ntsecapi.h>
#include <Dsgetdc.h>
#include <shlobj.h>
#include <intshcut.h>

// #define MAXUINT USHRT_MAX

#include "wcautil.h"
#include "wcawow64.h"
#include "wcawrapquery.h"
#include "aclutil.h"
#include "dirutil.h"
#include "fileutil.h"
#include "memutil.h"
#include "osutil.h"
#include "pathutil.h"
#include "procutil.h"
#include "shelutil.h"
#include "strutil.h"
#include "sczutil.h"
#include "rmutil.h"
#include "userutil.h"
#include "xmlutil.h"
#include "wiutil.h"

#include "CustomMsiErrors.h"

#include "sca.h"
#include "scacost.h"
#include "cost.h"
#include "scauser.h"
#include "scasmb.h"
#include "scasmbexec.h"
#include "utilca.h"

#include "..\..\caDecor.h"
