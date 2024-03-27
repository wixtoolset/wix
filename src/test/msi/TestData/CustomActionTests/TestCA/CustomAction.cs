// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.TestCA
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Dtf.WindowsInstaller;

    public class CustomActions
    {
        [CustomAction]
        public static ActionResult ImmediateCA(Session session)
        {
            session.Log("Begin ImmediateCA");

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult DeferredCA(Session session)
        {
            session.Log("Begin DeferredCA");

            return ActionResult.Success;
        }
    }
}
