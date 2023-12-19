// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.Win32;

namespace TestExe
{
    public class ProcessInfoTask : Task
    {
        public ProcessInfoTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            try
            {
                string processInfoXml = "";

                // Get information about the process and who is running it
                Process thisProc = Process.GetCurrentProcess();
                string username = thisProc.StartInfo.EnvironmentVariables["username"].ToString();

                int parentProcId = GetParentProcess(thisProc.Id);
                Process parentProc = Process.GetProcessById(parentProcId);
                string parentUsername = parentProc.StartInfo.EnvironmentVariables["username"].ToString();

                int grandparentProcId = GetParentProcess(parentProc.Id);
                Process grandparentProc = Process.GetProcessById(grandparentProcId);
                string grandparentUsername = grandparentProc.StartInfo.EnvironmentVariables["username"].ToString();

                processInfoXml += "<ProcessInfo>";
                processInfoXml += "  <ProcessName>" + thisProc.ProcessName + "</ProcessName>";
                processInfoXml += "  <Id>" + thisProc.Id.ToString() + "</Id>";
                processInfoXml += "  <SessionId>" + thisProc.SessionId.ToString() + "</SessionId>";
                processInfoXml += "  <MachineName>" + thisProc.MachineName + "</MachineName>";
                // this stuff isn't set since we didn't start the process and tell it what to use.  So don't bother 
                //processInfoXml += "  <StartInfo>";
                //processInfoXml += "    <FileName>" + thisProc.StartInfo.FileName + "</FileName>";
                //processInfoXml += "    <UserName>" + thisProc.StartInfo.UserName + "</UserName>";
                //processInfoXml += "    <WorkingDirectory>" + thisProc.StartInfo.WorkingDirectory + "</WorkingDirectory>";
                //processInfoXml += "    <Arguments>" + thisProc.StartInfo.Arguments + "</Arguments>";
                //processInfoXml += "  </StartInfo>";
                processInfoXml += "  <StartTime>" + thisProc.StartTime.ToString() + "</StartTime>";
                processInfoXml += "  <Username>" + username + "</Username>";
                processInfoXml += "  <ParentProcess>";
                processInfoXml += "    <ProcessName>" + parentProc.ProcessName + "</ProcessName>";
                processInfoXml += "    <Id>" + parentProc.Id.ToString() + "</Id>";
                processInfoXml += "    <StartTime>" + parentProc.StartTime.ToString() + "</StartTime>";
                processInfoXml += "    <Username>" + parentUsername + "</Username>";
                processInfoXml += "  </ParentProcess>";
                processInfoXml += "  <GrandparentProcess>";
                processInfoXml += "    <ProcessName>" + grandparentProc.ProcessName + "</ProcessName>";
                processInfoXml += "    <Id>" + grandparentProc.Id.ToString() + "</Id>";
                processInfoXml += "    <StartTime>" + grandparentProc.StartTime.ToString() + "</StartTime>";
                processInfoXml += "    <Username>" + grandparentUsername + "</Username>";
                processInfoXml += "  </GrandparentProcess>";
                processInfoXml += "</ProcessInfo>";

                string logFile = System.Environment.ExpandEnvironmentVariables(this.data);
                Console.WriteLine("Creating Process Info data file: " + logFile);
                StreamWriter textFile = File.CreateText(logFile);
                textFile.WriteLine(processInfoXml);
                textFile.Close();
            }
            catch (Exception eX)
            {
                Console.WriteLine("Creating Process Info data file failed");
                Console.WriteLine(eX.Message);
            }


        }

        private static int GetParentProcess(int Id)
        {
            int parentPid = 0;
            using (ManagementObject mo = new ManagementObject("win32_process.handle='" + Id.ToString() + "'"))
            {
                mo.Get();
                parentPid = Convert.ToInt32(mo["ParentProcessId"]);
            }
            return parentPid;
        }
    }

    /// <summary>
    /// Task class that will create a registry key and write a name and value in it
    /// </summary>
    public class RegistryWriterTask : Task
    {
        private string hive;
        private string keyPath;
        private string[] keyPathArray;
        private string name;
        private RegistryValueKind regValueKind;
        private object value;

        public RegistryWriterTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            if (this.parseRegKeyNameTypeValue(System.Environment.ExpandEnvironmentVariables(this.data)))
            {
                RegistryKey rk = Registry.LocalMachine;

                if (this.hive == "HKCU") { rk = Microsoft.Win32.Registry.CurrentUser; }
                if (this.hive == "HKCC") { rk = Microsoft.Win32.Registry.CurrentConfig; }
                if (this.hive == "HKLM") { rk = Microsoft.Win32.Registry.LocalMachine; }

                foreach (string key in this.keyPathArray)
                {
                    rk = rk.CreateSubKey(key, RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                rk.SetValue(this.name, this.value, this.regValueKind);
                Console.WriteLine("Created registry key: '{0}' name: '{1}' value: '{2}' of type: '{3}'",
                    this.hive + "\\" + this.keyPath,
                    this.name,
                    this.value.ToString(),
                    this.regValueKind.ToString());
            }
            else
            {
                Console.WriteLine("Unable to write registry key.");
            }

        }

        private bool parseRegKeyNameTypeValue(string delimittedData)
        {
            string[] splitString = delimittedData.Split(new string[] { "," }, StringSplitOptions.None);
            if (splitString.Length != 4)
            {
                Console.WriteLine("Invalid regkey. Unable to parse key,name,type,value from: \"" + delimittedData + "\"");
                return false;
            }
            else
            {
                this.keyPath = splitString[0];
                this.name = splitString[1];
                string datatype = splitString[2];
                if (datatype == "DWord")
                {
                    this.value = UInt32.Parse(splitString[3]);
                }
                else if (datatype == "QWord")
                {
                    this.value = UInt64.Parse(splitString[3]);
                }
                else
                {
                    this.value = splitString[3];
                }

                if (this.keyPath.ToUpper().StartsWith("HKLM\\"))
                {
                    this.hive = "HKLM";
                    this.keyPath = this.keyPath.Replace("HKLM\\", "");
                }
                else if (this.keyPath.ToUpper().StartsWith("HKCC\\"))
                {
                    this.hive = "HKCC";
                    this.keyPath = this.keyPath.Replace("HKCC\\", "");
                }
                else if (this.keyPath.ToUpper().StartsWith("HKCU\\"))
                {
                    this.hive = "HKCU";
                    this.keyPath = this.keyPath.Replace("HKCU\\", "");
                }
                else
                {
                    Console.WriteLine("Invalid regkey. Unable to determin hive.  regkey must start with either: [HKLM], [HKCU], or [HKCC]");
                    return false;
                }
                this.keyPathArray = this.keyPath.Split(new string[] { "\\" }, StringSplitOptions.None);

                try
                {
                    this.regValueKind = (RegistryValueKind)System.Enum.Parse(typeof(RegistryValueKind), datatype);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Invalid datatype. It must be: String, DWord, or QWord (case sensitive)");
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// Task class that will delete a registry key value or registry key and all of its children
    /// </summary>
    public class RegistryDeleterTask : Task
    {
        private string hive;
        private string keyPath;
        private string[] keyPathArray;
        private string name;

        public RegistryDeleterTask(string Data) : base(Data) { }

        public override void RunTask()
        {
            if (this.parseRegKeyName(System.Environment.ExpandEnvironmentVariables(this.data)))
            {
                try
                {
                    RegistryKey rk = Registry.LocalMachine;

                    if (this.hive == "HKCU") { rk = Microsoft.Win32.Registry.CurrentUser; }
                    if (this.hive == "HKCC") { rk = Microsoft.Win32.Registry.CurrentConfig; }
                    if (this.hive == "HKLM") { rk = Microsoft.Win32.Registry.LocalMachine; }

                    RegistryKey rkParent = null;
                    foreach (string key in this.keyPathArray)
                    {
                        rkParent = rk;
                        rk = rk.OpenSubKey(key, true);
                    }

                    if (String.IsNullOrEmpty(this.name))
                    {
                        // delete the key and all of its children
                        string subkeyToDelete = this.keyPathArray[this.keyPathArray.Length - 1];
                        rkParent.DeleteSubKeyTree(subkeyToDelete);
                        Console.WriteLine("Deleted registry key: '{0}'", this.hive + "\\" + this.keyPath);
                    }
                    else
                    {
                        // just delete this value
                        rk.DeleteValue(this.name);
                        Console.WriteLine("Deleted registry key: '{0}' name: '{1}'", this.hive + "\\" + this.keyPath, this.name);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to delete registry key: '{0}'", this.hive + "\\" + this.keyPath);
                    Console.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Unable to delete registry key.");
            }

        }

        private bool parseRegKeyName(string delimittedData)
        {
            string[] splitString = delimittedData.Split(new string[] { "," }, StringSplitOptions.None);

            if (splitString.Length > 2)
            {
                Console.WriteLine("Unable to parse registry key and name.");
                return false;
            }

            this.keyPath = splitString[0];
            if (splitString.Length == 2)
            {
                this.name = splitString[1];
            }

            if (this.keyPath.ToUpper().StartsWith("HKLM\\"))
            {
                this.hive = "HKLM";
                this.keyPath = this.keyPath.Replace("HKLM\\", "");
            }
            else if (this.keyPath.ToUpper().StartsWith("HKCC\\"))
            {
                this.hive = "HKCC";
                this.keyPath = this.keyPath.Replace("HKCC\\", "");
            }
            else if (this.keyPath.ToUpper().StartsWith("HKCU\\"))
            {
                this.hive = "HKCU";
                this.keyPath = this.keyPath.Replace("HKCU\\", "");
            }
            else
            {
                Console.WriteLine("Invalid regkey. Unable to determine hive.  regkey must start with either: [HKLM], [HKCU], or [HKCC]");
                return false;
            }
            this.keyPathArray = this.keyPath.Split(new string[] { "\\" }, StringSplitOptions.None);
            return true;
        }
    }
}
#endif
