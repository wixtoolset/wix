// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.StaticFiles;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.FileProviders.Physical;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Primitives;

    public class CoreOwinWebServer : IWebServer, IFileProvider
    {
        const string StaticFileBasePath = "/e2e";

        private Dictionary<string, string> PhysicalPathsByRelativeUrl { get; } = new Dictionary<string, string>();

        private IHost WebHost { get; set; }

        public bool DisableHeadResponses { get; set; }
        public bool DisableRangeRequests { get; set; }

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
                                       appBuilder.Use(this.CustomStaticFileMiddleware);
                                       appBuilder.UseStaticFiles(new StaticFileOptions
                                       {
                                           FileProvider = this,
                                           RequestPath = StaticFileBasePath,
                                           ServeUnknownFileTypes = true,
                                           OnPrepareResponse = this.OnPrepareStaticFileResponse,
                                       });
                                   });
                               })
                               .Build();
            this.WebHost.Start();
        }

        private async Task CustomStaticFileMiddleware(HttpContext context, Func<Task> next)
        {
            if (!this.DisableRangeRequests || (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)))
            {
                await next();
                return;
            }

            // Only send Content-Length header.
            // Don't support range requests.
            // https://github.com/dotnet/aspnetcore/blob/60abfafe32a4692f9dc4a172665524f163b10012/src/Middleware/StaticFiles/src/StaticFileMiddleware.cs
            if (!context.Request.Path.StartsWithSegments(StaticFileBasePath, out var subpath))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var fileInfo = this.GetFileInfo(subpath);
            if (!fileInfo.Exists)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var responseHeaders = context.Response.GetTypedHeaders();
            var fileLength = fileInfo.Length;
            responseHeaders.ContentLength = fileLength;

            this.OnPrepareStaticFileResponse(new StaticFileResponseContext(context, fileInfo));

            if (HttpMethods.IsGet(context.Request.Method))
            {
                await context.Response.SendFileAsync(fileInfo, 0, fileLength);
            }
        }

        private void OnPrepareStaticFileResponse(StaticFileResponseContext obj)
        {
            if (this.DisableHeadResponses && obj.Context.Request.Method == "HEAD")
            {
                obj.Context.Response.StatusCode = 404;
            }
        }

        public void Dispose()
        {
            var waitTime = TimeSpan.FromSeconds(5);
            this.WebHost?.StopAsync(waitTime).Wait(waitTime);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            throw new NotImplementedException();
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            if (this.PhysicalPathsByRelativeUrl.TryGetValue(subpath, out var filepath))
            {
                return new PhysicalFileInfo(new FileInfo(filepath));
            }

            return new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            throw new NotImplementedException();
        }
    }
}