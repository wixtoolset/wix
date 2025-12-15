// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    public interface ILogger
    {
        bool HasLoggedErrors { get; }

        void LogError(string message);

        void LogWarning(string message);
    }
}
