#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>
#include <strsafe.h>
#include <ShlObj.h>
#include <sddl.h>

// Include error.h before dutil.h
#include <dutilsources.h>
#include "error.h"
#include <dutil.h>

#include <verutil.h>
#include <apputil.h>
#include <atomutil.h>
#include <dictutil.h>
#include <dirutil.h>
#include <envutil.h>
#include <fileutil.h>
#include <guidutil.h>
#include <iniutil.h>
#include <locutil.h>
#include <memutil.h>
#include <pathutil.h>
#include <pipeutil.h>
#include <procutil.h>
#include <strutil.h>
#include <monutil.h>
#include <regutil.h>
#include <rssutil.h>
#include <apuputil.h> // NOTE: this must come after atomutil.h and rssutil.h since it uses them.
#include <uriutil.h>
#include <xmlutil.h>

#pragma managed
#include <vcclr.h>
