// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http.Symbols
{
    /// <summary>
    /// Must match constants in httpcerts.cpp
    /// </summary>
    public enum CertificateType
    {
        SniSsl = 0,
        IpSsl = 1,
    }
}
