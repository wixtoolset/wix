#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <http.h>
#include <msiquery.h>
#include <strsafe.h>

#include "wcautil.h"
#include "cryputil.h"
#include "dutil.h"
#include "memutil.h"
#include "strutil.h"
#include "aclutil.h"

#include "cost.h"

#include "..\..\caDecor.h"

enum eHandleExisting
{
    heReplace = 0,
    heIgnore = 1,
    heFail = 2
};

enum eCertificateType
{
    ctSniSsl = 0,
    ctIpSsl = 1,
};
