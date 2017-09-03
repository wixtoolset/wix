#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

#define ReleaseVariables(vh) if (vh) { VarDestroy(vh, NULL); }
#define ReleaseVariableValue(v) if (v) { VarFreeValue(v); }
#define ReleaseNullVariables(vh) if (vh) { VarDestroy(vh, NULL); vh = NULL; }
#define ReleaseNullVariableValue(v) if (v) { VarFreeValue(v); v = NULL; }

typedef void* VARIABLE_ENUM_HANDLE;
typedef void* VARIABLES_HANDLE;
typedef const void* C_VARIABLES_HANDLE;

extern const int VARIABLE_ENUM_HANDLE_BYTES;
extern const int VARIABLES_HANDLE_BYTES;

typedef void(*PFN_FREEVARIABLECONTEXT)(
    __in LPVOID pvContext
    );

typedef enum VARIABLE_VALUE_TYPE
{
    VARIABLE_VALUE_TYPE_NONE,
    VARIABLE_VALUE_TYPE_NUMERIC,
    VARIABLE_VALUE_TYPE_STRING,
    VARIABLE_VALUE_TYPE_VERSION,
} VARIABLE_VALUE_TYPE;

typedef struct _VARIABLE_VALUE
{
    VARIABLE_VALUE_TYPE type;
    union
    {
        LONGLONG llValue;
        DWORD64 qwValue;
        LPWSTR sczValue;
    };
    BOOL fHidden;
    LPVOID pvContext;
} VARIABLE_VALUE;

HRESULT DAPI VarCreate(
    __out_bcount(VARIABLES_HANDLE_BYTES) VARIABLES_HANDLE* ppVariables
    );
void DAPI VarDestroy(
    __in_bcount(VARIABLES_HANDLE_BYTES) VARIABLES_HANDLE pVariables,
    __in_opt PFN_FREEVARIABLECONTEXT vpfFreeVariableContext
    );
void DAPI VarFreeValue(
    __in VARIABLE_VALUE* pValue
    );
HRESULT DAPI VarEscapeString(
    __in_z LPCWSTR wzIn,
    __out_z LPWSTR* psczOut
    );
HRESULT DAPI VarFormatString(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzIn,
    __out_z_opt LPWSTR* psczOut,
    __out_opt DWORD* pcchOut
    );
HRESULT DAPI VarGetFormatted(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
HRESULT DAPI VarGetNumeric(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    );
HRESULT DAPI VarGetString(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out_z LPWSTR* psczValue
    );
HRESULT DAPI VarGetVersion(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64* pqwValue
    );
HRESULT DAPI VarGetValue(
    __in C_VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __out VARIABLE_VALUE** ppValue
    );
HRESULT DAPI VarSetNumeric(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    );
HRESULT DAPI VarSetString(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    );
HRESULT DAPI VarSetVersion(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in DWORD64 qwValue
    );
HRESULT DAPI VarSetValue(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzVariable,
    __in VARIABLE_VALUE* pValue
    );
HRESULT DAPI VarStartEnum(
    __in VARIABLES_HANDLE pVariables,
    __out_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE* ppEnum,
    __out VARIABLE_VALUE** ppValue
    );
HRESULT DAPI VarNextVariable(
    __in_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE pEnum,
    __out VARIABLE_VALUE** ppValue
    );
void DAPI VarFinishEnum(
    __in_bcount(VARIABLE_ENUM_HANDLE_BYTES) VARIABLE_ENUM_HANDLE pEnum
    );

#if defined(__cplusplus)
}
#endif
