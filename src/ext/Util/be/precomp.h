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

#define MAXUINT USHRT_MAX

#include <dutil.h>
#include <memutil.h>
#include <strutil.h>
#include <pathutil.h>
#include <xmlutil.h>

#include <BundleExtensionEngine.h>
#include <BundleExtension.h>

#include <IBundleExtensionEngine.h>
#include <IBundleExtension.h>
#include <bextutil.h>
#include <BextBundleExtensionEngine.h>

#include "beDecor.h"
#include "utilsearch.h"
#include "detectsha2support.h"
#include "UtilBundleExtension.h"
