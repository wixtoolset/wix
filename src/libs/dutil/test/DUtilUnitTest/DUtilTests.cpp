// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;
using namespace WixBuildTools::TestSupport::XunitExtensions;

namespace DutilTests
{
    [Collection("Dutil_TraceErrorSource")]
    public ref class DUtil
    {
    public:
        [SkippableFact]
        void DUtilTraceErrorSourceFiltersOnTraceLevel()
        {
            DutilInitialize(&DutilTestTraceError);

            try
            {
                CallDutilTraceErrorSource();

                Dutil_TraceSetLevel(REPORT_DEBUG, FALSE);

                Exception^ traceErrorException = nullptr;

                try
                {
                    CallDutilTraceErrorSource();
                }
                catch (Exception^ e)
                {
                    traceErrorException = e;
                }

                if (traceErrorException == nullptr)
                {
                    WixAssert::Skip("Dutil_TraceErrorSource did not call the registered callback.");
                }
                else
                {
                    WixAssert::StringEqual("hr = 0x80004005, message = Error message", traceErrorException->Message, false);
                }
            }
            finally
            {
                DutilUninitialize();
            }
        }

    private:
        void CallDutilTraceErrorSource()
        {
            Dutil_TraceErrorSource(__FILE__, __LINE__, REPORT_DEBUG, DUTIL_SOURCE_EXTERNAL, E_FAIL, "Error message");
        }
    };

    [CollectionDefinition("Dutil_TraceErrorSource", DisableParallelization = true)]
    public ref class Dutil_TraceErrorSourceCollectionDefinition
    {
    };
}
