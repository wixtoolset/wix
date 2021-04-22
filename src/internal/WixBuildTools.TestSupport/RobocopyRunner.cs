// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    public class RobocopyRunner : ExternalExecutable
    {
        private static readonly RobocopyRunner Instance = new RobocopyRunner();

        private RobocopyRunner() : base("robocopy") { }

        public static ExternalExecutableResult Execute(string args)
        {
            return Instance.Run(args);
        }
    }
}
