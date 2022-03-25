// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;

namespace DutilTests
{
    public ref class DUtil
    {
    public:
        [Fact]
        Void DUtilTraceErrorSourceFiltersOnTraceLevel()
        {
            DutilInitialize(&DutilTestTraceError);

            CallDutilTraceErrorSource();

            Dutil_TraceSetLevel(REPORT_DEBUG, FALSE);

            Action^ action = gcnew Action(this, &DUtil::CallDutilTraceErrorSource);
            // xxxxx The following line is ambiguous when used with the current version of Xunit.
            // xxxxx Xunit::Assert::Throws<Exception^>(action);
            // xxxxx It has therefore been, at least temporarily, replaced by the next line of code.
            // xxxxx See the comments in WixBuildTools.WixAssert for details.

            WixBuildTools::TestSupport::WixAssert::Throws<Exception^>(action);

            DutilUninitialize();
        }

    private:
        void CallDutilTraceErrorSource()
        {
            Dutil_TraceErrorSource(__FILE__, __LINE__, REPORT_DEBUG, DUTIL_SOURCE_EXTERNAL, E_FAIL, "Error message");
        }
    };
}
