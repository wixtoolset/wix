#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>

#pragma warning(push)
#pragma warning(disable:4458) // declaration of 'xxx' hides class member
#include <gdiplus.h>
#pragma warning(pop)

#include <msiquery.h>
#include <objbase.h>
#include <shlobj.h>
#include <shlwapi.h>
#include <stdlib.h>
#include <strsafe.h>
#include <stddef.h>

#include <dutil.h>
#include <apputil.h>
#include <memutil.h>
#include <dictutil.h>
#include <dirutil.h>
#include <fileutil.h>
#include <locutil.h>
#include <logutil.h>
#include <pathutil.h>
#include <procutil.h>
#include <resrutil.h>
#include <shelutil.h>
#include <strutil.h>
#include <wndutil.h>
#include <thmutil.h>
#include <verutil.h>
#include <uriutil.h>
#include <xmlutil.h>

#include <IBootstrapperApplication.h>

#include <balutil.h>
#include <balinfo.h>
#include <balcondition.h>

#include <BAFunctions.h>

#include "stdbas.messages.h"
#include "WixStandardBootstrapperApplication.h"
