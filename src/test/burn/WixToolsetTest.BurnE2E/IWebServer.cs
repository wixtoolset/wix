// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.Collections.Generic;

    public interface IWebServer : IDisposable
    {
        /// <summary>
        /// Registers a collection of relative URLs (the key) with its absolute path to the file (the value).
        /// </summary>
        void AddFiles(Dictionary<string, string> physicalPathsByRelativeUrl);

        /// <summary>
        /// Starts the web server on a new thread.
        /// </summary>
        void Start();
    }
}