#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if _WIN32_MSI < 150
#define _WIN32_MSI 150
#endif

#include <http.h>
#include <msiquery.h>
#include <strsafe.h>

#include "wcautil.h"

#include "certutil.h"
#include "cryputil.h"
#include "fileutil.h"
#include "dutil.h"
#include "memutil.h"
#include "strutil.h"
#include "aclutil.h"
#include "wcawrapquery.h"

#include "cost.h"
#include "certificates.h"
#include "CustomMsiErrors.h"

#include "..\..\caDecor.h"

enum eHandleExisting
{
    heReplace = 0,
    heIgnore = 1,
    heFail = 2
};

// Generic action enum.
enum SCA_ACTION
{
    SCA_ACTION_NONE,
    SCA_ACTION_INSTALL,
    SCA_ACTION_UNINSTALL
};
