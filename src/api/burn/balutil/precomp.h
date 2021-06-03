#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>

#pragma warning(push)
#pragma warning(disable:4458) // declaration of 'xxx' hides class member
#include <gdiplus.h>
#pragma warning(pop)

#include <bitsmsg.h>
#include <msi.h>
#include <wininet.h>
#include <CommCtrl.h>
#include <strsafe.h>

#include <dutil.h>
#include <pathutil.h>
#include <locutil.h>
#include <memutil.h>
#include <dictutil.h>
#include <strutil.h>
#include <thmutil.h>
#include <xmlutil.h>

#include <BootstrapperEngine.h>
#include <BootstrapperApplication.h>

#include "IBootstrapperEngine.h"
#include "IBootstrapperApplication.h"

#include "BAFunctions.h"
#include "IBAFunctions.h"

#include "balutil.h"
#include "BalBootstrapperEngine.h"
#include "balcondition.h"
#include "balinfo.h"
#include "balretry.h"
