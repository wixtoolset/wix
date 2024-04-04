// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.TestCA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using WixToolset.Dtf.WindowsInstaller;

    public class CustomActions
    {
        [CustomAction]
        public static ActionResult ImmediateCA(Session session)
        {
            session.Log("Begin ImmediateCA");

            var path = Path.Combine(Path.GetTempPath(), "ImmediateCA.txt");
            WriteNow(path);

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult ExecuteImmediateCA(Session session)
        {
            session.Log("Begin ExecuteImmediateCA");

            var path = Path.Combine(Path.GetTempPath(), "ExecuteImmediateCA.txt");
            WriteNow(path);

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult ImpersonatedDeferredCA(Session session)
        {
            session.Log("Begin ImpersonatedDeferredCA");

            var path = Path.Combine(Path.GetTempPath(), "ImpersonatedDeferredCA.txt");
            WriteNow(path);

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult DeferredCA(Session session)
        {
            session.Log("Begin DeferredCA");

            WriteNow(@"C:\Windows\Installer\DeferredCA.txt");

            return ActionResult.Success;
        }

        private static void WriteNow(string path)
        {
            using (var file = File.Create(path))
            {
                var now = Encoding.UTF8.GetBytes(DateTime.Now.ToString("o"));
                file.Write(now, 0, now.Length);
            }
        }
    }
}
