// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const LPCWSTR DAY_OF_WEEK[] = { L"Sun", L"Mon", L"Tue", L"Wed", L"Thu", L"Fri", L"Sat" };
const LPCWSTR MONTH_OF_YEAR[] = { L"None", L"Jan", L"Feb", L"Mar", L"Apr", L"May", L"Jun", L"Jul", L"Aug", L"Sep", L"Oct", L"Nov", L"Dec" };
enum TIME_PARSER { DayOfWeek, DayOfMonth, MonthOfYear, Year, Hours, Minutes, Seconds, TimeZone };
enum TIME_PARSERRFC3339 { RFC3339_Year, RFC3339_Month, RFC3339_Day, RFC3339_Hours, RFC3339_Minutes, RFC3339_Seconds, RFC3339_TimeZone };

// prototypes
static HRESULT DayFromString(
    __in_z LPCWSTR wzDay,
    __out WORD* pwDayOfWeek
    );
static HRESULT MonthFromString(
    __in_z LPCWSTR wzMonth,
    __out WORD* pwMonthOfYear
    );


/********************************************************************
 TimeFromString - converts string to FILETIME

*******************************************************************/
extern "C" HRESULT DAPI TimeFromString(
    __in_z LPCWSTR wzTime,
    __out FILETIME* pFileTime
    )
{
    Assert(wzTime && pFileTime);

    HRESULT hr = S_OK;
    LPWSTR pwzTime = NULL;

    SYSTEMTIME sysTime = { };
    TIME_PARSER timeParser = DayOfWeek;

    LPCWSTR pwzStart = NULL;
    LPWSTR pwzEnd = NULL;

    hr = StrAllocString(&pwzTime, wzTime, 0);
    ExitOnFailure(hr, "Failed to copy time.");

    pwzStart = pwzEnd = pwzTime;
    while (pwzEnd && *pwzEnd)
    {
        if (L',' == *pwzEnd || L' ' == *pwzEnd || L':' == *pwzEnd)
        {
            *pwzEnd = L'\0'; // null terminate
            ++pwzEnd;

            while (L' ' == *pwzEnd)
            {
                ++pwzEnd; // and skip past the blank space
            }

            switch (timeParser)
            {
                case DayOfWeek:
                    hr = DayFromString(pwzStart, &sysTime.wDayOfWeek);
                    ExitOnFailure(hr, "Failed to convert string to day: %ls", pwzStart);
                    break;

                case DayOfMonth:
                    sysTime.wDay = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case MonthOfYear:
                    hr = MonthFromString(pwzStart, &sysTime.wMonth);
                    ExitOnFailure(hr, "Failed to convert to month: %ls", pwzStart);
                    break;

                case Year:
                    sysTime.wYear = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case Hours:
                    sysTime.wHour = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case Minutes:
                    sysTime.wMinute = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case Seconds:
                    sysTime.wSecond = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case TimeZone:
                    // TODO: do something with this in the future, but this should only hit outside of the while loop.
                    break;

                default:
                    break;
            }

            pwzStart = pwzEnd;
            timeParser = (TIME_PARSER)((int)timeParser + 1);
        }

        ++pwzEnd;
    }


    if (!::SystemTimeToFileTime(&sysTime, pFileTime))
    {
        ExitWithLastError(hr, "Failed to convert system time to file time.");
    }

LExit:
    ReleaseStr(pwzTime);

    return hr;
}

/********************************************************************
 TimeFromString3339 - converts string formated in accorance with RFC3339 to FILETIME
 http://tools.ietf.org/html/rfc3339
*******************************************************************/
extern "C" HRESULT DAPI TimeFromString3339(
    __in_z LPCWSTR wzTime,
    __out FILETIME* pFileTime
    )
{
    Assert(wzTime && pFileTime);

    HRESULT hr = S_OK;
    LPWSTR pwzTime = NULL;

    SYSTEMTIME sysTime = { };
    TIME_PARSERRFC3339 timeParser = RFC3339_Year;

    LPCWSTR pwzStart = NULL;
    LPWSTR pwzEnd = NULL;

    hr = StrAllocString(&pwzTime, wzTime, 0);
    ExitOnFailure(hr, "Failed to copy time.");

    pwzStart = pwzEnd = pwzTime;
    while (pwzEnd && *pwzEnd)
    {
        if (L'T' == *pwzEnd || L':' == *pwzEnd || L'-' == *pwzEnd)
        {
            *pwzEnd = L'\0'; // null terminate
            ++pwzEnd;

            switch (timeParser)
            {
                case RFC3339_Year:
                    sysTime.wYear = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case RFC3339_Month:
                    sysTime.wMonth = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case RFC3339_Day:
                    sysTime.wDay = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case RFC3339_Hours:
                    sysTime.wHour = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case RFC3339_Minutes:
                    sysTime.wMinute = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case RFC3339_Seconds:
                    sysTime.wSecond = (WORD)wcstoul(pwzStart, NULL, 10);
                    break;

                case RFC3339_TimeZone:
                    // TODO: do something with this in the future, but this should only hit outside of the while loop.
                    break;

                default:
                    break;
            }

            pwzStart = pwzEnd;
            timeParser = (TIME_PARSERRFC3339)((int)timeParser + 1);
        }

        ++pwzEnd;
    }


    if (!::SystemTimeToFileTime(&sysTime, pFileTime))
    {
        ExitWithLastError(hr, "Failed to convert system time to file time.");
    }

LExit:
    ReleaseStr(pwzTime);

    return hr;
}
/****************************************************************************
TimeCurrentTime - gets the current time in string format

****************************************************************************/
extern "C" HRESULT DAPI TimeCurrentTime(
    __deref_out_z LPWSTR* ppwz,
    __in BOOL fGMT
    )
{
    SYSTEMTIME st;

    if (fGMT)
    {
        ::GetSystemTime(&st);
    }
    else
    {
        SYSTEMTIME stGMT;
        TIME_ZONE_INFORMATION tzi;

        ::GetTimeZoneInformation(&tzi);
        ::GetSystemTime(&stGMT);
        ::SystemTimeToTzSpecificLocalTime(&tzi, &stGMT, &st);
    }

    return StrAllocFormatted(ppwz, L"%02d:%02d:%02d", st.wHour, st.wMinute, st.wSecond);
}


/****************************************************************************
TimeCurrentDateTime - gets the current date and time in string format,
  per format described in RFC 3339
****************************************************************************/
extern "C" HRESULT DAPI TimeCurrentDateTime(
    __deref_out_z LPWSTR* ppwz,
    __in BOOL fGMT
    )
{
    SYSTEMTIME st;

    ::GetSystemTime(&st);

    return TimeSystemDateTime(ppwz, &st, fGMT);
}


/****************************************************************************
TimeSystemDateTime - converts the provided system time struct to string format,
  per format described in RFC 3339
****************************************************************************/
extern "C" HRESULT DAPI TimeSystemDateTime(
    __deref_out_z LPWSTR* ppwz,
    __in const SYSTEMTIME *pst,
    __in BOOL fGMT
    )
{
    DWORD dwAbsBias = 0;

    if (fGMT)
    {
        return StrAllocFormatted(ppwz, L"%04hu-%02hu-%02huT%02hu:%02hu:%02huZ", pst->wYear, pst->wMonth, pst->wDay, pst->wHour, pst->wMinute, pst->wSecond);
    }
    else
    {
        SYSTEMTIME st;
        TIME_ZONE_INFORMATION tzi;

        ::GetTimeZoneInformation(&tzi);
        ::SystemTimeToTzSpecificLocalTime(&tzi, pst, &st);
        dwAbsBias = abs(tzi.Bias);

        return StrAllocFormatted(ppwz, L"%04hu-%02hu-%02huT%02hu:%02hu:%02hu%c%02u:%02u", st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, 0 >= tzi.Bias ? L'+' : L'-', dwAbsBias / 60, dwAbsBias % 60);
    }
}


/****************************************************************************
TimeSystemToDateTimeString - converts the provided system time struct to
  string format representing date and time for the specified locale
****************************************************************************/
HRESULT DAPI TimeSystemToDateTimeString(
    __deref_out_z LPWSTR* ppwz,
    __in const SYSTEMTIME* pst,
    __in LCID locale
    )
{
    HRESULT hr = S_OK;
    const WCHAR * DATE_FORMAT = L"MMM dd',' yyyy',' ";
    const WCHAR * TIME_FORMAT = L"hh':'mm':'ss tt";
    int iLenDate = 0;
    int iLenTime = 0;

    iLenDate = ::GetDateFormatW(locale, 0, pst, DATE_FORMAT, NULL, 0);
    if (0 >= iLenDate)
    {
        ExitWithLastError(hr, "Failed to get date format with NULL");
    }

    iLenTime = ::GetTimeFormatW(locale, 0, pst, TIME_FORMAT, NULL, 0);
    if (0 >= iLenTime)
    {
        ExitWithLastError(hr, "Failed to get time format with NULL");
    }

    // Between both lengths we account for 2 null terminators, and only need one, so we subtract one
    hr = StrAlloc(ppwz, iLenDate + iLenTime - 1);
    ExitOnFailure(hr, "Failed to allocate string");

    if (!::GetDateFormatW(locale, 0, pst, DATE_FORMAT, *ppwz, iLenDate))
    {
        ExitWithLastError(hr, "Failed to get date format with buffer");
    }
    // Space to separate them
    (*ppwz)[iLenDate - 1] = ' ';

    if (!::GetTimeFormatW(locale, 0, pst, TIME_FORMAT, (*ppwz) + iLenDate - 1, iLenTime))
    {
        ExitWithLastError(hr, "Failed to get time format with buffer");
    }

LExit:
    return hr;
}

/********************************************************************
 DayFromString - converts string to day

*******************************************************************/
static HRESULT DayFromString(
    __in_z LPCWSTR wzDay,
    __out WORD* pwDayOfWeek
    )
{
    HRESULT hr = E_INVALIDARG; // assume we won't find a matching name

    for (WORD i = 0; i < countof(DAY_OF_WEEK); ++i)
    {
        if (0 == lstrcmpW(wzDay, DAY_OF_WEEK[i]))
        {
            *pwDayOfWeek = i;
            hr = S_OK;
            break;
        }
    }

    return hr;
}


/********************************************************************
 MonthFromString - converts string to month

*******************************************************************/
static HRESULT MonthFromString(
    __in_z LPCWSTR wzMonth,
    __out WORD* pwMonthOfYear
    )
{
    HRESULT hr = E_INVALIDARG; // assume we won't find a matching name

    for (WORD i = 0; i < countof(MONTH_OF_YEAR); ++i)
    {
        if (0 == lstrcmpW(wzMonth, MONTH_OF_YEAR[i]))
        {
            *pwMonthOfYear = i;
            hr = S_OK;
            break;
        }
    }

    return hr;
}
