#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if _WIN32_MSI < 150
#define _WIN32_MSI 150
#endif

#include <windows.h>
#include <msiquery.h>
#include <msidefs.h>
#include <strsafe.h>

#include <lm.h>        // NetApi32.lib

#include <Dsgetdc.h>
#include <ComAdmin.h>
#include <ahadmin.h>    // IIS 7 config

#include "wcautil.h"
#include "wcawow64.h"
#include "wcawrapquery.h"

#include "certutil.h"
#include "cryputil.h"
#include "fileutil.h"
#include "iis7util.h"
#include "memutil.h"
#include "metautil.h"
#include "strutil.h"
#include "userutil.h"
#include "wiutil.h"

#include "CustomMsiErrors.h"
#include "sca.h"
#include "scacost.h"
#include "scacert.h"
#include "scafilter.h"

#include "scaiis.h"
#include "scaiis7.h"
#include "scaproperty.h"
#include "scaweb.h"
#include "scawebdir.h"
#include "scawebsvcext.h"
#include "scavdir.h"
#include "scaweb7.h"
#include "scaapppool7.h"
#include "scavdir7.h"
#include "scawebapp7.h"
#include "scawebappext7.h"
#include "scamimemap7.h"
#include "scawebprop7.h"
#include "scaweblog7.h"
#include "scafilter7.h"
#include "scahttpheader7.h"
#include "scaweberr7.h"
#include "scawebsvcext7.h"
#include "scaproperty7.h"
#include "scawebdir7.h"
#include "scassl7.h"
#include "scaexecIIS7.h"

#include "caDecor.h"
