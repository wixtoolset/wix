// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.TestSupport
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Win32.SafeHandles;

    public abstract class ExternalExecutable
    {
        private readonly string exePath;

        protected ExternalExecutable(string exePath)
        {
            this.exePath = exePath;
        }

        protected ExternalExecutableResult Run(string args, bool mergeErrorIntoOutput = false, string workingDirectory = null)
        {
            // https://github.com/dotnet/runtime/issues/58492
            // Process.Start doesn't currently support starting a process with a long path,
            // but the way to support long paths doesn't support searching for the executable if it was a relative path.
            // Avoid the managed way of doing this even if the target isn't a long path to help verify that the native way works.
            if (!Path.IsPathRooted(this.exePath))
            {
                return this.RunManaged(args, mergeErrorIntoOutput, workingDirectory);
            }

            // https://web.archive.org/web/20150331190801/https://support.microsoft.com/en-us/kb/190351
            var commandLine = $"\"{this.exePath}\" {args}";
            var currentDirectory = workingDirectory ?? Path.GetDirectoryName(this.exePath);
            if (String.IsNullOrEmpty(currentDirectory))
            {
                currentDirectory = null;
            }
            var processInfo = new PROCESS_INFORMATION();
            var startInfo = new STARTUPINFOW
            {
                cb = Marshal.SizeOf(typeof(STARTUPINFOW)),
                dwFlags = StartupInfoFlags.STARTF_FORCEOFFFEEDBACK | StartupInfoFlags.STARTF_USESTDHANDLES,
                hStdInput = GetStdHandle(StdHandleType.STD_INPUT_HANDLE),
            };
            SafeFileHandle hStdOutputParent = null;
            SafeFileHandle hStdErrorParent = null;

            try
            {
                CreatePipeForProcess(out hStdOutputParent, out startInfo.hStdOutput);

                if (!mergeErrorIntoOutput)
                {
                    CreatePipeForProcess(out hStdErrorParent, out startInfo.hStdError);
                }
                else
                {
                    if (!DuplicateHandle(GetCurrentProcess(), startInfo.hStdOutput, GetCurrentProcess(), out startInfo.hStdError, 0, true, DuplicateHandleOptions.DUPLICATE_SAME_ACCESS))
                    {
                        throw new Win32Exception();
                    }
                }

                if (!CreateProcessW(this.exePath, commandLine, IntPtr.Zero, IntPtr.Zero, true, CreateProcessFlags.CREATE_NO_WINDOW, IntPtr.Zero,
                                    currentDirectory, ref startInfo, ref processInfo))
                {
                    throw new Win32Exception();
                }

                startInfo.Dispose();

                return GetResultFromNative(mergeErrorIntoOutput, hStdOutputParent, hStdErrorParent, processInfo.hProcess, this.exePath, args);
            }
            finally
            {
                hStdErrorParent?.Dispose();
                hStdOutputParent?.Dispose();

                startInfo.Dispose();
                processInfo.Dispose();
            }
        }

        private static ExternalExecutableResult GetResultFromNative(bool mergeErrorIntoOutput, SafeFileHandle hStdOutputParent, SafeFileHandle hStdErrorParent, IntPtr hProcess, string fileName, string args)
        {
            using (var outputStream = new StreamReader(new FileStream(hStdOutputParent, FileAccess.Read)))
            using (var errorStream = mergeErrorIntoOutput ? null : new StreamReader(new FileStream(hStdErrorParent, FileAccess.Read)))
            {
                var outputTask = Task.Run(() => ReadProcessStreamLines(outputStream));
                var errorTask = Task.Run(() => ReadProcessStreamLines(errorStream));

                while (!outputTask.Wait(100) || !errorTask.Wait(100)) { Task.Yield(); }
                var standardOutput = outputTask.Result;
                var standardError = errorTask.Result;

                if (WaitForSingleObject(hProcess, -1) != 0)
                {
                    throw new Win32Exception();
                }

                if (!GetExitCodeProcess(hProcess, out var exitCode))
                {
                    throw new Win32Exception();
                }

                return new ExternalExecutableResult
                {
                    ExitCode = exitCode,
                    StandardError = standardError,
                    StandardOutput = standardOutput,
                    FileName = fileName,
                    Arguments = args,
                };
            }
        }

        private static string[] ReadProcessStreamLines(StreamReader streamReader)
        {
            if (streamReader == null)
            {
                return null;
            }

            var lines = new List<string>();
            while (true)
            {
                var line = streamReader.ReadLine();
                if (line == null)
                {
                    break;
                }

                lines.Add(line);
            }

            return lines.ToArray();
        }

        protected ExternalExecutableResult RunManaged(string args, bool mergeErrorIntoOutput = false, string workingDirectory = null)
        {
            var startInfo = new ProcessStartInfo(this.exePath, args)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(this.exePath),
            };

            using (var process = Process.Start(startInfo))
            {
                // This implementation of merging the streams does not guarantee that lines are retrieved in the same order that they were written.
                // If the process is simultaneously writing to both streams, this is impossible to do anyway.
                var standardOutput = new ConcurrentQueue<string>();
                var standardError = mergeErrorIntoOutput ? standardOutput : new ConcurrentQueue<string>();

                process.ErrorDataReceived += (s, e) => { if (e.Data != null) { standardError.Enqueue(e.Data); } };
                process.OutputDataReceived += (s, e) => { if (e.Data != null) { standardOutput.Enqueue(e.Data); } };

                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                process.WaitForExit();

                return new ExternalExecutableResult
                {
                    ExitCode = process.ExitCode,
                    StandardError = mergeErrorIntoOutput ? null : standardError.ToArray(),
                    StandardOutput = standardOutput.ToArray(),
                    FileName = this.exePath,
                    Arguments = args,
                };
            }
        }

        // This is internal because it assumes backslashes aren't used as escape characters and there aren't any double quotes.
        internal static string CombineArguments(IEnumerable<string> arguments)
        {
            if (arguments == null)
            {
                return null;
            }

            var sb = new StringBuilder();

            foreach (var arg in arguments)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                if (arg.IndexOf(' ') > -1)
                {
                    sb.Append("\"");
                    sb.Append(arg);
                    sb.Append("\"");
                }
                else
                {
                    sb.Append(arg);
                }
            }

            return sb.ToString();
        }

        private static void CreatePipeForProcess(out SafeFileHandle hReadPipe, out IntPtr hWritePipe)
        {
            var securityAttributes = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf(typeof(SECURITY_ATTRIBUTES)),
                bInheritHandle = true,
            };

            if (!CreatePipe(out var hReadTemp, out hWritePipe, ref securityAttributes, 0))
            {
                throw new Win32Exception();
            }

            // Only the handle passed to the process should be inheritable, so have to duplicate the other handle to get an uninheritable one.
            if (!DuplicateHandle(GetCurrentProcess(), hReadTemp, GetCurrentProcess(), out var hReadPipePtr, 0, false, DuplicateHandleOptions.DUPLICATE_CLOSE_SOURCE | DuplicateHandleOptions.DUPLICATE_SAME_ACCESS))
            {
                throw new Win32Exception();
            }

            hReadPipe = new SafeFileHandle(hReadPipePtr, true);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private extern static IntPtr GetStdHandle(StdHandleType nStdHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool CreateProcessW(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            CreateProcessFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFOW lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private extern static IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool GetExitCodeProcess(IntPtr hHandle, out int lpExitCode);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private extern static int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private extern static bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, DuplicateHandleOptions dwOptions);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFOW
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public StartupInfoFlags dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;

            public void Dispose()
            {
                // This makes assumptions based on how it's used above.
                if (this.hStdError != IntPtr.Zero)
                {
                    CloseHandle(this.hStdError);
                    this.hStdError = IntPtr.Zero;
                }

                if (this.hStdOutput != IntPtr.Zero)
                {
                    CloseHandle(this.hStdOutput);
                    this.hStdOutput = IntPtr.Zero;
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;

            public void Dispose()
            {
                if (this.hProcess != IntPtr.Zero)
                {
                    CloseHandle(this.hProcess);
                    this.hProcess = IntPtr.Zero;
                }

                if (this.hThread != IntPtr.Zero)
                {
                    CloseHandle(this.hThread);
                    this.hThread = IntPtr.Zero;
                }
            }
        }

        private enum StdHandleType
        {
            STD_INPUT_HANDLE = -10,
            STD_OUTPUT_HANDLE = -11,
            STD_ERROR_HANDLE = -12,
        }

        [Flags]
        private enum CreateProcessFlags
        {
            None = 0x0,
            CREATE_NO_WINDOW = 0x08000000,
        }

        [Flags]
        private enum StartupInfoFlags
        {
            None = 0x0,
            STARTF_FORCEOFFFEEDBACK = 0x80,
            STARTF_USESTDHANDLES = 0x100,
        }

        private enum DuplicateHandleOptions
        {
            DUPLICATE_CLOSE_SOURCE = 1,
            DUPLICATE_SAME_ACCESS = 2,
        }
    }
}
