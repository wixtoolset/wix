// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.FileProviders.Physical;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Primitives;

    public class CoreOwinWebServer : IWebServer, IFileProvider
    {
        private Dictionary<string, string> PhysicalPathsByRelativeUrl { get; } = new Dictionary<string, string>();

        private IHost WebHost { get; set; }

        public void AddFiles(Dictionary<string, string> physicalPathsByRelativeUrl)
        {
            foreach (var kvp in physicalPathsByRelativeUrl)
            {
                this.PhysicalPathsByRelativeUrl.Add(kvp.Key, kvp.Value);
            }
        }

        public void Start()
        {
            this.WebHost = Host.CreateDefaultBuilder()
                               .ConfigureWebHostDefaults(webBuilder =>
                               {
                                   // Use localhost instead of * to avoid firewall issues.
                                   webBuilder.UseUrls("http://localhost:9999");
                                   webBuilder.Configure(appBuilder =>
                                   {
                                       appBuilder.UseStaticFiles(new StaticFileOptions
                                       {
                                           FileProvider = this,
                                           RequestPath = "/e2e",
                                           ServeUnknownFileTypes = true,
                                       });
                                   });
                               })
                               .Build();
            this.WebHost.Start();
        }

        public void Dispose()
        {
            var waitTime = TimeSpan.FromSeconds(5);
            this.WebHost?.StopAsync(waitTime).Wait(waitTime);
        }

        public IDirectoryContents GetDirectoryContents(string subpath) => throw new NotImplementedException();

        public IFileInfo GetFileInfo(string subpath)
        {
            if (this.PhysicalPathsByRelativeUrl.TryGetValue(subpath, out var filepath))
            {
                return new PhysicalFileInfo(new FileInfo(filepath));
            }

            return new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter) => throw new NotImplementedException();
    }
}