// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sdk.TestData.CustomActionProject
{
    using WixToolset.Dtf.WindowsInstaller;

    public class CustomActions
    {
        [CustomAction]
        public static ActionResult TestCustomAction(Session session)
        {
            return ActionResult.Success;
        }
    }
}
