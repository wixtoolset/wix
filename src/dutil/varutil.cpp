// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

struct VARIABLE_ENUM_STRUCT
{
};

struct VARIABLES_STRUCT
{
};

const int VARIABLE_ENUM_HANDLE_BYTES = sizeof(VARIABLE_ENUM_STRUCT);
const int VARIABLES_HANDLE_BYTES = sizeof(VARIABLES_STRUCT);

// function definitions

/********************************************************************
VarCreate - creates a variables group.
********************************************************************/
extern "C" HRESULT DAPI VarCreate(
    __out_bcount(VARIABLES_HANDLE_BYTES) VARIABLES_HANDLE* ppVariables
    )
{
    UNREFERENCED_PARAMETER(ppVariables);
    return E_NOTIMPL;
}

/********************************************************************
VarDestroy - destroys a variables group, accepting an optional callback
             to help free the variable contexts.
********************************************************************/
extern "C" void DAPI VarDestroy(
    __in_bcount(VARIABLES_HANDLE_BYTES) VARIABLES_HANDLE pVariables,
    __in_opt PFN_FREEVARIABLECONTEXT vpfFreeVariableContext
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(vpfFreeVariableContext);
}

/********************************************************************
VarFreeValue - frees a variable value.
********************************************************************/
extern "C" void DAPI VarFreeValue(
    __in VARIABLE_VALUE* pValue
    )
{
    UNREFERENCED_PARAMETER(pValue);
}

/********************************************************************
VarEscapeString - escapes special characters in wzIn so that it can
                  be used in conditions or variable values.
********************************************************************/
extern "C" HRESULT DAPI VarEscapeString(
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    )
{
    UNREFERENCED_PARAMETER(wzIn);
    UNREFERENCED_PARAMETER(psczOut);
    return E_NOTIMPL;
}

/********************************************************************
VarFormatString - similar to MsiFormatRecord.
********************************************************************/
extern "C" HRESULT DAPI VarFormatString(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzIn);
    UNREFERENCED_PARAMETER(psczOut);
    UNREFERENCED_PARAMETER(pcchOut);
    return E_NOTIMPL;
}

/********************************************************************
VarGetFormatted - gets the formatted value of a single variable.
********************************************************************/
extern "C" HRESULT DAPI VarGetFormatted(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(psczValue);
    return E_NOTIMPL;
}

/********************************************************************
VarGetNumeric - gets the numeric value of a variable.  If the type of
                the variable is not numeric, it will attempt to
                convert the value into a number.
********************************************************************/
extern "C" HRESULT DAPI VarGetNumeric(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(pllValue);
    return E_NOTIMPL;
}

/********************************************************************
VarGetString - gets the unformatted string value of a variable.  If
               the type of the variable is not string, it will
               convert the value to a string.
********************************************************************/
extern "C" HRESULT DAPI VarGetString(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(psczValue);
    return E_NOTIMPL;
}

/********************************************************************
VarGetVersion - gets the version value of a variable.  If the type of
                the variable is not version, it will attempt to
                convert the value into a version.
********************************************************************/
extern "C" HRESULT DAPI VarGetVersion(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(pqwValue);
    return E_NOTIMPL;
}

/********************************************************************
VarGetValue - gets the value of a variable along with its metadata.
********************************************************************/
extern "C" HRESULT DAPI VarGetValue(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
}

/********************************************************************
VarSetNumeric - sets the value of the variable to a number, the type
                of the variable to numeric, and adds the variable to
                the group if necessary.
********************************************************************/
extern "C" HRESULT DAPI VarSetNumeric(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(llValue);
    return E_NOTIMPL;
}

/********************************************************************
VarSetString - sets the value of the variable to a string, the type
               of the variable to string, and adds the variable to
               the group if necessary.
********************************************************************/
extern "C" HRESULT DAPI VarSetString(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(wzValue);
    return E_NOTIMPL;
}

/********************************************************************
VarSetVersion - sets the value of the variable to a version, the type
                of the variable to version, and adds the variable to
                the group if necessary.
********************************************************************/
extern "C" HRESULT DAPI VarSetVersion(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(qwValue);
    return E_NOTIMPL;
}

/********************************************************************
VarSetValue - sets the value of the variable along with its metadata.
              Also adds the variable to the group if necessary.
********************************************************************/
extern "C" HRESULT DAPI VarSetValue(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in VARIABLE_VALUE* pValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzVariable);
    UNREFERENCED_PARAMETER(pValue);
    return E_NOTIMPL;
}

/********************************************************************
VarStartEnum - starts the enumeration of the variable group.  There
               is no guarantee for the order of the variable enumeration.

NOTE: caller is responsible for calling VarFinishEnum even if function fails
********************************************************************/
extern "C" HRESULT DAPI VarStartEnum(
    __in VARIABLES_HANDLE pVariables,
    __out_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE* ppEnum,
    __out VARIABLE_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(ppEnum);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
}

/********************************************************************
VarNextVariable - continues the enumeration of the variable group. It
                  will fail if any variables were added or removed
                  during the enumeration.

NOTE: caller is responsible for calling VarFinishEnum even if function fails
********************************************************************/
extern "C" HRESULT DAPI VarNextVariable(
    __in_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE pEnum,
    __out VARIABLE_VALUE** ppValue
    )
{
    UNREFERENCED_PARAMETER(pEnum);
    UNREFERENCED_PARAMETER(ppValue);
    return E_NOTIMPL;
}

/********************************************************************
VarFinishEnum - cleans up resources used for the enumeration.
********************************************************************/
extern "C" void DAPI VarFinishEnum(
    __in_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE pEnum
    )
{
    UNREFERENCED_PARAMETER(pEnum);
}
