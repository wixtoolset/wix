// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define PerfExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_PERFUTIL, x, s, __VA_ARGS__)
#define PerfExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_PERFUTIL, x, s, __VA_ARGS__)
#define PerfExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_PERFUTIL, x, s, __VA_ARGS__)
#define PerfExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_PERFUTIL, x, s, __VA_ARGS__)
#define PerfExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_PERFUTIL, x, s, __VA_ARGS__)
#define PerfExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_PERFUTIL, x, s, __VA_ARGS__)
#define PerfExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_PERFUTIL, p, x, e, s, __VA_ARGS__)
#define PerfExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_PERFUTIL, p, x, s, __VA_ARGS__)
#define PerfExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_PERFUTIL, p, x, e, s, __VA_ARGS__)
#define PerfExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_PERFUTIL, p, x, s, __VA_ARGS__)
#define PerfExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_PERFUTIL, e, x, s, __VA_ARGS__)
#define PerfExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_PERFUTIL, g, x, s, __VA_ARGS__)

static BOOL vfHighPerformanceCounter = TRUE;   // assume the system has a high performance counter
static double vdFrequency = 1;


/********************************************************************
 PerfInitialize - initializes internal static variables

********************************************************************/
extern "C" void DAPI PerfInitialize(
    )
{
    LARGE_INTEGER liFrequency = { };

    //
    // check for high perf counter
    //
    if (!::QueryPerformanceFrequency(&liFrequency))
    {
        vfHighPerformanceCounter = FALSE;
        vdFrequency = 1000;  // ticks are measured in milliseconds
    }
    else
    {
        vdFrequency = static_cast<double>(liFrequency.QuadPart);
    }
}


/********************************************************************
 PerfClickTime - resets the clicker, or returns elapsed time since last call

 NOTE: if pliElapsed is NULL, resets the elapsed time
       if pliElapsed is not NULL, returns perf number since last call to PerfClickTime()
********************************************************************/
extern "C" void DAPI PerfClickTime(
    __out_opt LARGE_INTEGER* pliElapsed
    )
{
    static LARGE_INTEGER liStart = { };
    LARGE_INTEGER* pli = pliElapsed;

    if (!pli)  // if elapsed time time was not requested, reset the start time
    {
        pli = &liStart;
    }

    if (vfHighPerformanceCounter)
    {
        ::QueryPerformanceCounter(pli);
    }
    else
    {
        pli->QuadPart = ::GetTickCount();
    }

    if (pliElapsed)
    {
        pliElapsed->QuadPart -= liStart.QuadPart;
    }
}


/********************************************************************
 PerfConvertToSeconds - converts perf number to seconds

********************************************************************/
extern "C" double DAPI PerfConvertToSeconds(
    __in const LARGE_INTEGER* pli
    )
{
    Assert(0 < vdFrequency);
    return pli->QuadPart / vdFrequency;
}
