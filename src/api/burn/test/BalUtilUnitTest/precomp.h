#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>

#pragma warning(push)
#pragma warning(disable:4458) // declaration of 'xxx' hides class member
#include <gdiplus.h>
#pragma warning(pop)

#include <msiquery.h>
#include <CommCtrl.h>

#include <dutil.h>
#include <dictutil.h>

#include <baenginetypes.h>
#include <batypes.h>

#include <BAFunctions.h>
#include <IBootstrapperEngine.h>
#include <IBootstrapperApplication.h>
#include <IBAFunctions.h>
#include <balutil.h>
#include <balretry.h>

#include "TestBAFunctions.h"
#include "TestBootstrapperApplication.h"

#pragma managed
#include <vcclr.h>
