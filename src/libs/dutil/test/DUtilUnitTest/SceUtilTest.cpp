// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#include <sqlce_oledb.h>
#include <sceutil.h>

using namespace System;
using namespace Xunit;
using namespace WixTest;

#define ASSIGN_INDEX_STRUCT(a, b, c) {a.wzName = c; a.rgColumns = b; a.cColumns = countof(b);};

namespace DutilTests
{
    enum TABLES
    {
        TABLE_A,
        TABLE_COUNT
    };

    enum TABLE_A_COLUMNS
    {
        TABLE_A_KEY,
        TABLE_A_BINARY,
        TABLE_A_DWORD,
        TABLE_A_QWORD,
        TABLE_A_BOOL,
        TABLE_A_STRING,
        TABLE_A_DWORD_NULLABLE,
        TABLE_A_INITIAL_COLUMNS,

        TABLE_A_EXTRA_STRING = TABLE_A_INITIAL_COLUMNS,
        TABLE_A_FINAL_COLUMNS
    };

    struct TableARowValue
    {
        DWORD dwAutoGenKey;

        BYTE *pbBinary;
        DWORD cBinary;

        DWORD dw;
        DWORD64 qw;
        BOOL f;
        LPWSTR scz;

        BOOL fNullablePresent;
        DWORD dwNullable;

        BOOL fSchemaV2;
        LPWSTR sczExtra;
    };

    public ref class SceUtil
    {
    public:
        void ReleaseSceSchema(SCE_DATABASE_SCHEMA *pdsSchema)
        {
            DWORD dwTable;

            for (dwTable = 0; dwTable < pdsSchema->cTables; ++dwTable)
            {
                ReleaseNullMem(pdsSchema->rgTables[dwTable].rgColumns);
                ReleaseNullMem(pdsSchema->rgTables[dwTable].rgIndexes);
            }

            ReleaseMem(pdsSchema->rgTables);

            return;
        }

        void SetupSchema(SCE_DATABASE_SCHEMA *pSchema, BOOL fIncludeExtended)
        {
            pSchema->cTables = TABLE_COUNT;
            pSchema->rgTables = static_cast<SCE_TABLE_SCHEMA*>(MemAlloc(TABLE_COUNT * sizeof(SCE_TABLE_SCHEMA), TRUE));
            NativeAssert::True(pSchema->rgTables != NULL);

            pSchema->rgTables[TABLE_A].wzName = L"TableA";
            pSchema->rgTables[TABLE_A].cColumns = fIncludeExtended ? TABLE_A_FINAL_COLUMNS : TABLE_A_INITIAL_COLUMNS;
            pSchema->rgTables[TABLE_A].cIndexes = 2;

            for (DWORD i = 0; i < pSchema->cTables; ++i)
            {
                pSchema->rgTables[i].rgColumns = static_cast<SCE_COLUMN_SCHEMA*>(MemAlloc(sizeof(SCE_COLUMN_SCHEMA) * pSchema->rgTables[i].cColumns, TRUE));
                NativeAssert::True(pSchema->rgTables[i].rgColumns != NULL);

                pSchema->rgTables[i].rgIndexes = static_cast<SCE_INDEX_SCHEMA*>(MemAlloc(sizeof(SCE_COLUMN_SCHEMA) * pSchema->rgTables[i].cIndexes, TRUE));
                NativeAssert::True(pSchema->rgTables[i].rgIndexes != NULL);
            }

            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_KEY].wzName = L"Key";
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_KEY].dbtColumnType = DBTYPE_I4;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_KEY].fPrimaryKey = TRUE;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_KEY].fAutoIncrement = TRUE;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_BINARY].wzName = L"Binary";
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_BINARY].dbtColumnType = DBTYPE_BYTES;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_DWORD].wzName = L"Dword";
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_DWORD].dbtColumnType = DBTYPE_I4;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_QWORD].wzName = L"Qword";
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_QWORD].dbtColumnType = DBTYPE_I8;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_BOOL].wzName = L"Bool";
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_BOOL].dbtColumnType = DBTYPE_BOOL;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_STRING].wzName = L"String";
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_STRING].dbtColumnType = DBTYPE_WSTR;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_DWORD_NULLABLE].wzName = L"Nullable";
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_DWORD_NULLABLE].dbtColumnType = DBTYPE_I4;
            pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_DWORD_NULLABLE].fNullable = TRUE;

            if (fIncludeExtended)
            {
                pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_EXTRA_STRING].wzName = L"ExtraString";
                pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_EXTRA_STRING].dbtColumnType = DBTYPE_WSTR;
                pSchema->rgTables[TABLE_A].rgColumns[TABLE_A_EXTRA_STRING].fNullable = TRUE;
            }

            static DWORD rgdwTableA_Index1[] = { TABLE_A_DWORD, TABLE_A_STRING, TABLE_A_QWORD };
            static DWORD rgdwTableA_Index2[] = { TABLE_A_DWORD, TABLE_A_STRING };

            ASSIGN_INDEX_STRUCT(pSchema->rgTables[TABLE_A].rgIndexes[0], rgdwTableA_Index1, L"Dword_String_Qword");
            ASSIGN_INDEX_STRUCT(pSchema->rgTables[TABLE_A].rgIndexes[1], rgdwTableA_Index2, L"Dword_String");
        }

        void SetStructValues(TableARowValue *pValue, BYTE *pbBinary, DWORD cBinary, DWORD dw, DWORD64 qw, BOOL f, LPWSTR scz,  DWORD *pdw, LPWSTR sczExtra)
        {
            pValue->pbBinary = pbBinary;
            pValue->cBinary = cBinary;
            pValue->dw = dw;
            pValue->qw = qw;
            pValue->f = f;
            pValue->scz = scz;

            if (pdw)
            {
                pValue->fNullablePresent = TRUE;
                pValue->dwNullable = *pdw;
            }
            else
            {
                pValue->fNullablePresent = FALSE;
            }

            if (sczExtra)
            {
                pValue->fSchemaV2 = TRUE;
                pValue->sczExtra = sczExtra;
            }
            else
            {
                pValue->fSchemaV2 = FALSE;
            }
        }

        void AssertStructValuesSame(TableARowValue *pValueExpected, TableARowValue *pValueOther)
        {
            NativeAssert::Equal(pValueExpected->cBinary, pValueOther->cBinary);
            NativeAssert::True(0 == memcmp(pValueExpected->pbBinary, pValueOther->pbBinary, pValueOther->cBinary));

            NativeAssert::Equal(pValueExpected->dw, pValueOther->dw);
            NativeAssert::Equal(pValueExpected->qw, pValueOther->qw);
            NativeAssert::Equal(pValueExpected->f, pValueOther->f);
            NativeAssert::True(0 == wcscmp(pValueExpected->scz, pValueOther->scz));
            
            NativeAssert::Equal(pValueExpected->fNullablePresent, pValueOther->fNullablePresent);
            if (pValueExpected->fNullablePresent)
            {
                NativeAssert::Equal(pValueExpected->dwNullable, pValueOther->dwNullable);
            }

            NativeAssert::Equal(pValueExpected->fSchemaV2, pValueOther->fSchemaV2);
            if (pValueExpected->fSchemaV2)
            {
                NativeAssert::True(0 == wcscmp(pValueExpected->sczExtra, pValueOther->sczExtra));
            }
        }

        void InsertRow(SCE_DATABASE *pDatabase, TableARowValue *pValue, BOOL fRollback)
        {
            HRESULT hr = S_OK;
            SCE_ROW_HANDLE sceRow = NULL;

            hr = SceBeginTransaction(pDatabase);
            NativeAssert::Succeeded(hr, "Failed to begin transaction");

            hr = ScePrepareInsert(pDatabase, TABLE_A, &sceRow);
            NativeAssert::Succeeded(hr, "Failed to prepare to insert row");

            hr = SceSetColumnBinary(sceRow, TABLE_A_BINARY, pValue->pbBinary, pValue->cBinary);
            NativeAssert::Succeeded(hr, "Failed to set binary value");

            hr = SceSetColumnDword(sceRow, TABLE_A_DWORD, pValue->dw);
            NativeAssert::Succeeded(hr, "Failed to set dword value");

            hr = SceSetColumnQword(sceRow, TABLE_A_QWORD, pValue->qw);
            NativeAssert::Succeeded(hr, "Failed to set qword value");

            hr = SceSetColumnBool(sceRow, TABLE_A_BOOL, pValue->f);
            NativeAssert::Succeeded(hr, "Failed to set bool value");

            hr = SceSetColumnString(sceRow, TABLE_A_STRING, pValue->scz);
            NativeAssert::Succeeded(hr, "Failed to set string value");
        
            if (pValue->fNullablePresent)
            {
                hr = SceSetColumnDword(sceRow, TABLE_A_DWORD_NULLABLE, pValue->dwNullable);
                NativeAssert::Succeeded(hr, "Failed to set dword value");
            }
            else
            {
                hr = SceSetColumnNull(sceRow, TABLE_A_DWORD_NULLABLE);
                NativeAssert::Succeeded(hr, "Failed to set null value");
            }

            if (pValue->fSchemaV2)
            {
                hr = SceSetColumnString(sceRow, TABLE_A_EXTRA_STRING, pValue->sczExtra);
                NativeAssert::Succeeded(hr, "Failed to set extra string value");
            }
        
            hr = SceFinishUpdate(sceRow);
            NativeAssert::Succeeded(hr, "Failed to finish insert");

            if (fRollback)
            {
                hr = SceRollbackTransaction(pDatabase);
                NativeAssert::Succeeded(hr, "Failed to rollback transaction");
            }
            else
            {
                hr = SceCommitTransaction(pDatabase);
                NativeAssert::Succeeded(hr, "Failed to commit transaction");

                hr = SceGetColumnDword(sceRow, TABLE_A_KEY, &pValue->dwAutoGenKey);
                NativeAssert::Succeeded(hr, "Failed to get autogen key after insert");

                NativeAssert::True(pValue->dwAutoGenKey != 0);
            }

            ReleaseSceRow(sceRow);
        }

        void VerifyRow(TableARowValue *pExpectedValue, SCE_ROW_HANDLE sceRow)
        {
            HRESULT hr = S_OK;
            TableARowValue value = {};

            hr = SceGetColumnBinary(sceRow, TABLE_A_BINARY, &value.pbBinary, &value.cBinary);
            NativeAssert::Succeeded(hr, "Failed to get binary value from result row");

            hr = SceGetColumnDword(sceRow, TABLE_A_DWORD, &value.dw);
            NativeAssert::Succeeded(hr, "Failed to get dword value from result row");

            hr = SceGetColumnQword(sceRow, TABLE_A_QWORD, &value.qw);
            NativeAssert::Succeeded(hr, "Failed to get qword value from result row");

            hr = SceGetColumnBool(sceRow, TABLE_A_BOOL, &value.f);
            NativeAssert::Succeeded(hr, "Failed to get bool value from result row");

            hr = SceGetColumnString(sceRow, TABLE_A_STRING, &value.scz);
            NativeAssert::Succeeded(hr, "Failed to get string value from result row");

            hr = SceGetColumnDword(sceRow, TABLE_A_DWORD_NULLABLE, &value.dwNullable);
            if (hr == E_NOTFOUND)
            {
                value.fNullablePresent = FALSE;
                hr = S_OK;
            }
            else
            {
                NativeAssert::Succeeded(hr, "Failed to get string value from result row");
                value.fNullablePresent = TRUE;
            }

            if (pExpectedValue->fSchemaV2)
            {
                value.fSchemaV2 = TRUE;
                hr = SceGetColumnString(sceRow, TABLE_A_EXTRA_STRING, &value.sczExtra);
                NativeAssert::Succeeded(hr, "Failed to get extra string value from result row");
            }

            AssertStructValuesSame(pExpectedValue, &value);

            ReleaseNullMem(value.pbBinary);
            ReleaseNullStr(value.scz);
        }

        void VerifyQuery(TableARowValue **rgExpectedValues, DWORD cExpectedValues, SCE_QUERY_RESULTS_HANDLE queryResults)
        {
            HRESULT hr = S_OK;
            SCE_ROW_HANDLE sceRow = NULL;

            for (DWORD i = 0; i < cExpectedValues; ++i)
            {
                hr = SceGetNextResultRow(queryResults, &sceRow);
                NativeAssert::Succeeded(hr, "Failed to get next result row");

                VerifyRow(rgExpectedValues[i], sceRow);
                ReleaseNullSceRow(sceRow);
            }

            // No more results
            NativeAssert::True(NULL == queryResults || FAILED(SceGetNextResultRow(queryResults, &sceRow)));
        }

        void TestIndex(SCE_DATABASE *pDatabase)
        {
            HRESULT hr = S_OK;
            BYTE binary1[50] = { 0x80, 0x70 };
            BYTE binary2[40] = { 0x90, 0xAB };
            BYTE binary3[40] = { 0x85, 0x88 };
            DWORD dwValue1 = 0x55555555, dwValue2 = 0x88888888;
            TableARowValue value1 = {}, value2 = {}, value3 = {}, value4 = {}, value5 = {};
            SCE_QUERY_HANDLE query = NULL;
            SCE_QUERY_RESULTS_HANDLE results = NULL;

            SetStructValues(&value1, static_cast<BYTE *>(binary1), sizeof(binary1), 3, 1, TRUE, L"zzz", &dwValue1, NULL);
            SetStructValues(&value2, static_cast<BYTE *>(binary2), sizeof(binary2), 3, 2, TRUE, L"yyy", &dwValue2, NULL);
            SetStructValues(&value3, static_cast<BYTE *>(binary3), sizeof(binary3), 3, 3, TRUE, L"xxx", NULL, NULL);
            SetStructValues(&value4, static_cast<BYTE *>(binary2), sizeof(binary2), 4, 4, TRUE, L"xyz", &dwValue2, NULL);
            SetStructValues(&value5, static_cast<BYTE *>(binary3), sizeof(binary3), 3, 1, TRUE, L"yyy", &dwValue2, NULL);

            // Rollback an insert to confirm the insert doesn't happen and database can still be interacted with normally afterwards
            InsertRow(pDatabase, &value1, TRUE);

            InsertRow(pDatabase, &value1, FALSE);
            InsertRow(pDatabase, &value2, FALSE);
            InsertRow(pDatabase, &value3, FALSE);
            InsertRow(pDatabase, &value4, FALSE);
            InsertRow(pDatabase, &value5, FALSE);

            NativeAssert::True(value1.dwAutoGenKey != value2.dwAutoGenKey);

            // Test setting 1 column
            hr = SceBeginQuery(pDatabase, TABLE_A, 0, &query);
            NativeAssert::Succeeded(hr, "Failed to begin query");

            hr = SceSetQueryColumnDword(query, 3);
            NativeAssert::Succeeded(hr, "Failed to set query column dword");

            hr = SceRunQueryRange(&query, &results);
            NativeAssert::Succeeded(hr, "Failed to run query");
            NativeAssert::True(query == NULL);

            TableARowValue *sortedAfterQuery1[] = { &value3, &value5, &value2, &value1 };
            VerifyQuery(sortedAfterQuery1, _countof(sortedAfterQuery1), results);
            ReleaseNullSceQueryResults(results);

            // Test setting 2 columns, third column is unspecified so results are sorted by it
            hr = SceBeginQuery(pDatabase, TABLE_A, 0, &query);
            NativeAssert::Succeeded(hr, "Failed to begin query");

            hr = SceSetQueryColumnDword(query, 3);
            NativeAssert::Succeeded(hr, "Failed to set query column dword");

            hr = SceSetQueryColumnString(query, L"yyy");
            NativeAssert::Succeeded(hr, "Failed to set query column dword");

            hr = SceRunQueryRange(&query, &results);
            NativeAssert::Succeeded(hr, "Failed to run query");
            NativeAssert::True(query == NULL);

            TableARowValue *sortedAfterQuery2[] = { &value5, &value2 };
            VerifyQuery(sortedAfterQuery2, _countof(sortedAfterQuery2), results);
            ReleaseNullSceQueryResults(results);

            // Test setting 2 columns, third column of index is unspecified so results are sorted by it
            hr = SceBeginQuery(pDatabase, TABLE_A, 0, &query);
            NativeAssert::Succeeded(hr, "Failed to begin query");

            hr = SceSetQueryColumnDword(query, 3);
            NativeAssert::Succeeded(hr, "Failed to set query column dword");

            hr = SceSetQueryColumnString(query, L"yyy");
            NativeAssert::Succeeded(hr, "Failed to set query column dword");

            hr = SceRunQueryRange(&query, &results);
            NativeAssert::Succeeded(hr, "Failed to run query");
            NativeAssert::True(query == NULL);

            TableARowValue *sortedAfterQuery3[] = { &value5, &value2 };
            VerifyQuery(sortedAfterQuery3, _countof(sortedAfterQuery3), results);
            ReleaseNullSceQueryResults(results);

            // Test setting 2 columns in a different (2 column) index, so there is no 3rd column in index to sort by
            hr = SceBeginQuery(pDatabase, TABLE_A, 1, &query);
            NativeAssert::Succeeded(hr, "Failed to begin query");

            hr = SceSetQueryColumnDword(query, 3);
            NativeAssert::Succeeded(hr, "Failed to set query column dword");

            hr = SceSetQueryColumnString(query, L"yyy");
            NativeAssert::Succeeded(hr, "Failed to set query column dword");

            hr = SceRunQueryRange(&query, &results);
            NativeAssert::Succeeded(hr, "Failed to run query");
            NativeAssert::True(query == NULL);

            TableARowValue *sortedAfterQuery4[] = { &value2, &value5 };
            VerifyQuery(sortedAfterQuery4, _countof(sortedAfterQuery4), results);
            ReleaseNullSceQueryResults(results);
        }

        void TestReadWriteSchemaV2(SCE_DATABASE *pDatabase)
        {
            HRESULT hr = S_OK;
            BYTE binary1[40] = { 0x55, 0x44 };
            DWORD dwValue1 = 58;
            TableARowValue value1 = {};
            SCE_QUERY_HANDLE query = NULL;
            SCE_ROW_HANDLE row = NULL;

            SetStructValues(&value1, static_cast<BYTE *>(binary1), sizeof(binary1), 5, 1, TRUE, L"zzz", &dwValue1, L"newextrastring");

            InsertRow(pDatabase, &value1, FALSE);

            // Test setting 1 column
            hr = SceBeginQuery(pDatabase, TABLE_A, 0, &query);
            NativeAssert::Succeeded(hr, "Failed to begin query");

            hr = SceSetQueryColumnDword(query, 5);
            NativeAssert::Succeeded(hr, "Failed to set query column dword");

            hr = SceRunQueryExact(&query, &row);
            NativeAssert::Succeeded(hr, "Failed to run query exact");

            VerifyRow(&value1, row);
        }

        [Fact]
        void SceUtilTest()
        {
            HRESULT hr = S_OK;
            BOOL fComInitialized = FALSE;
            LPWSTR sczDbPath = NULL;
            SCE_DATABASE *pDatabase = NULL;
            SCE_DATABASE_SCHEMA schema1 = {};
            SCE_DATABASE_SCHEMA schema2 = {};

            try
            {
                hr = ::CoInitialize(0);
                NativeAssert::Succeeded(hr, "Failed to initialize COM");
                fComInitialized = TRUE;

                SetupSchema(&schema1, FALSE);
                SetupSchema(&schema2, TRUE);

                hr = PathExpand(&sczDbPath, L"%TEMP%\\SceUtilTest\\UnitTest.sdf", PATH_EXPAND_ENVIRONMENT);
                NativeAssert::Succeeded(hr, "Failed to get path to test database");

                FileEnsureDelete(sczDbPath);

                hr = SceEnsureDatabase(sczDbPath, L"sqlceoledb40.dll", L"Test", 1, &schema1, &pDatabase);
                NativeAssert::Succeeded(hr, "Failed to ensure database schema");

                TestIndex(pDatabase);

                hr = SceCloseDatabase(pDatabase);
                pDatabase = NULL;
                NativeAssert::Succeeded(hr, "Failed to close database");

                // Add column to schema
                hr = SceEnsureDatabase(sczDbPath, L"sqlceoledb40.dll", L"Test", 1, &schema2, &pDatabase);
                NativeAssert::Succeeded(hr, "Failed to ensure database schema");

                TestReadWriteSchemaV2(pDatabase);
            }
            finally
            {
                ReleaseSceSchema(&schema1);
                ReleaseSceSchema(&schema2);

                if (NULL != pDatabase)
                {
                    hr = SceCloseDatabase(pDatabase);
                    NativeAssert::Succeeded(hr, "Failed to close database");
                }
                ReleaseStr(sczDbPath);

                if (fComInitialized)
                {
                    ::CoUninitialize();
                }
            }
        }
    };
}
