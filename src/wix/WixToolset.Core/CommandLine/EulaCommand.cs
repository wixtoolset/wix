// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.CommandLine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class EulaCommand : BaseCommandLineCommand
    {
        private const string EulaFolderName = ".wix";
        private const string EulaFileSuffix = "-osmf-eula.txt";
        private static readonly string RequiredEulaId = "wix" + SomeVerInfo.Major;

        private enum EulaSubcommand
        {
            Accept
        }

        public EulaCommand(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.EulaValues = new HashSet<string>(StringComparer.Ordinal);
        }

        private IMessaging Messaging { get; }

        private EulaSubcommand? Subcommand { get; set; }

        private HashSet<string> EulaValues { get; }

        public override CommandLineHelp GetCommandLineHelp()
        {
            return new CommandLineHelp("Accept the WiX Toolset EULA.", "eula accept <eulaId> [eulaId ...]")
            {
                Commands = new[]
                {
                    new CommandLineHelpCommand("accept", "Accept the WiX OSMF EULA. This versions eulaId = " + RequiredEulaId)
                }
            };
        }

        public override Task<int> ExecuteAsync(CancellationToken _)
        {
            if (!this.Subcommand.HasValue)
            {
                this.Messaging.Write(ErrorMessages.CommandLineCommandRequired("eula"));
                return Task.FromResult(this.Messaging.LastErrorNumber);
            }

            if (this.Subcommand == EulaSubcommand.Accept)
            {
                if (this.EulaValues.Count == 0)
                {
                    this.Messaging.Write(CoreErrors.ExpectedArgument("accept"));
                    return Task.FromResult(this.Messaging.LastErrorNumber);
                }

                try
                {
                    if (!EulaCommand.Accept(this.EulaValues))
                    {
                        this.Messaging.Write(CoreErrors.InvalidEulaAcceptanceValue(this.EulaValues));
                        return Task.FromResult(this.Messaging.LastErrorNumber);
                    }
                }
                catch (Exception e)
                {
                    this.Messaging.Write(ErrorMessages.UnexpectedException(e));
                    return Task.FromResult(this.Messaging.LastErrorNumber);
                }
            }

            return Task.FromResult(0);
        }

        public override bool TryParseArgument(ICommandLineParser parser, string argument)
        {
            if (!parser.IsSwitch(argument))
            {
                if (!this.Subcommand.HasValue)
                {
                    if (!Enum.TryParse(argument, true, out EulaSubcommand subcommand))
                    {
                        return false;
                    }

                    this.Subcommand = subcommand;
                }
                else
                {
                    this.EulaValues.Add(argument);
                }

                return true;
            }

            return false;
        }

        internal static bool IsAccepted(HashSet<string> acceptedEulaIds)
        {
            if (AcceptedThisVersionsEulaId(acceptedEulaIds))
            {
                return true;
            }

            var path = GetEulaFilePath();
            return File.Exists(path);
        }

        private static bool Accept(HashSet<string> acceptedEulaIds)
        {
            if (!AcceptedThisVersionsEulaId(acceptedEulaIds))
            {
                return false;
            }

            var path = GetEulaFilePath();
            if (!File.Exists(path))
            {
                var acceptedEulaBytes = CalculateAcceptedEula();

                var directory = Path.GetDirectoryName(path);
                Directory.CreateDirectory(directory);

                File.WriteAllBytes(path, acceptedEulaBytes);
            }

            return true;
        }

        private static bool AcceptedThisVersionsEulaId(HashSet<string> acceptedEulaIds)
        {
            return acceptedEulaIds.Contains(EulaCommand.RequiredEulaId) == true;
        }

        private static byte[] CalculateAcceptedEula()
        {
            // Canonical payload using pure newlines.
            var payload = $"Accepted: {DateTime.UtcNow:O}\n" +
                          $"Version: {SomeVerInfo.InformationalVersion}+{SomeVerInfo.ShortSha}\n";

            byte[] hashBytes;
            using (var sha256 = SHA256.Create())
            {
                var payloadBytes = Encoding.UTF8.GetBytes(payload);
                hashBytes = sha256.ComputeHash(payloadBytes);
            }

            var hashString = new StringBuilder(hashBytes.Length * 2);

            foreach (var b in hashBytes)
            {
                hashString.Append(b.ToString("x2"));
            }

            payload += $"Hash: SHA256:{hashString}\n";

            return Encoding.UTF8.GetBytes(payload);
        }

        private static string GetEulaFilePath()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var directory = Path.Combine(userProfile, EulaFolderName);

            return Path.Combine(directory, $"{EulaCommand.RequiredEulaId}{EulaFileSuffix}");
        }
    }
}
