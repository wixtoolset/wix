#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include <windows.h>
#include <msiquery.h>
#include <corerror.h>
#include <Shlwapi.h>

#include <dutil.h>
#include <memutil.h>
#include <pathutil.h>
#include <strutil.h>
#include <xmlutil.h>

#include <BootstrapperEngine.h>
#include <BootstrapperApplication.h>

#include <IBootstrapperEngine.h>
#include <IBootstrapperApplication.h>
#include <IBootstrapperApplicationFactory.h>
#include <balutil.h>

#include <WixToolset.Dnc.Host.h>
#define NETHOST_USE_AS_STATIC
#include <nethost.h>
#include <hostfxr.h>

#include "coreclrhost.h"
#include "dncutil.h"
#include "dnchost.h"
