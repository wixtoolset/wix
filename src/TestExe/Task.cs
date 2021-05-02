// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace TestExe
{
    public abstract class Task
    {
        public string data;

        public Task(string Data)
        {
            this.data = Data;
        }

        public abstract void RunTask();

    }

    public class ExitCodeTask : Task
    {
        public ExitCodeTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            // this task does nothing.  Just stores data about what exit code to return.
        }
    }

    public class SleepTask : Task
    {
        public SleepTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            int milliseconds = int.Parse(this.data);
            Console.WriteLine("Starting to sleep for {0} milliseconds", milliseconds);
            System.Threading.Thread.Sleep(milliseconds);
        }
    }

    public class SleepRandomTask : Task
    {
        public SleepRandomTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            int low = int.Parse(this.data.Split(new string[] { ":" }, 2, StringSplitOptions.None)[0]);
            int high = int.Parse(this.data.Split(new string[] { ":" }, 2, StringSplitOptions.None)[1]);

            Random r = new Random();
            int milliseconds = r.Next(high - low) + low;
            Console.WriteLine("Starting to sleep for {0} milliseconds", milliseconds);
            System.Threading.Thread.Sleep(milliseconds);
        }
    }

    public class LargeFileTask : Task
    {
        public LargeFileTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            string[] tokens = this.data.Split(new char[] { '|' }, 2);
            string filePath = System.Environment.ExpandEnvironmentVariables(tokens[0]);
            long size = long.Parse(tokens[1]);
            using (var stream = File.Create(filePath))
            {
                stream.Seek(size - 1, SeekOrigin.Begin);
                stream.WriteByte(1);
            }
        }
    }

    public class LogTask : Task
    {
        string[] argsUsed;
        public LogTask(string Data, string[] args)
            : base(Data)
        {
            this.argsUsed = args;
        }

        public override void RunTask()
        {
            string logFile = "";
            string argsUsedString = "";

            foreach (string a in this.argsUsed)
            {
                argsUsedString += a + " ";
            }

            try
            {
                logFile = System.Environment.ExpandEnvironmentVariables(this.data);
                Console.WriteLine("creating log file: " + logFile);
                StreamWriter textFile = File.CreateText(logFile);
                textFile.WriteLine("This is a log file created by TestExe.exe");
                textFile.WriteLine("Args used: " + argsUsedString);
                textFile.Close();
            }
            catch
            {
                Console.WriteLine("creating a log file failed for: {0}", logFile);
            }

        }
    }

    public class FileExistsTask : Task
    {
        public FileExistsTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            string fileToExist = System.Environment.ExpandEnvironmentVariables(this.data);

            if (!String.IsNullOrEmpty(fileToExist))
            {
                Console.WriteLine("Waiting for this file to exist: \"" + fileToExist + "\"");
                while (!System.IO.File.Exists(fileToExist))
                {
                    System.Threading.Thread.Sleep(250);
                }
                Console.WriteLine("Found: \"" + fileToExist + "\"");
            }

        }
    }

    public class TaskParser
    {

        public static List<Task> ParseTasks(string[] args)
        {
            List<Task> tasks = new List<Task>();

            try
            {
                // for invalid args.  return empty list
                if (args.Length % 2 == 0)
                {
                    Task t;

                    for (int i = 0; i < args.Length; i += 2)
                    {
                        switch (args[i].ToLower())
                        {
                            case "/ec":
                                t = new ExitCodeTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/s":
                                t = new SleepTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/sr":
                                t = new SleepRandomTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/lf":
                                t = new LargeFileTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/log":
                                t = new LogTask(args[i + 1], args);
                                tasks.Add(t);
                                break;
                            case "/fe":
                                t = new FileExistsTask(args[i + 1]);
                                tasks.Add(t);
                                break;
#if NET35
                            case "/pinfo":
                                t = new ProcessInfoTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/regw":
                                t = new RegistryWriterTask(args[i + 1]);
                                tasks.Add(t);
                                break;
                            case "/regd":
                                t = new RegistryDeleterTask(args[i + 1]);
                                tasks.Add(t);
                                break;
#endif

                            default:
                                Console.WriteLine("Error: Invalid switch specified.");
                                return new List<Task>();
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error: Invalid switch data specified.  Couldn't parse the data.");
                return new List<Task>();
            }

            return tasks;
        }
    }
}
