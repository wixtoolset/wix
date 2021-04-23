// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BuildTasks
{
    using System;
    using System.Globalization;

    class BuildException : Exception
    {
        public BuildException()
        {
        }

        public BuildException(string message) : base(message)
        {
        }

        public BuildException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BuildException(string format, params string[] args) : this(String.Format(CultureInfo.CurrentCulture, format, args))
        {
        }
    }
}
