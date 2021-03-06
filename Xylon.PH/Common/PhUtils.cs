/*
 * Process Hacker - 
 *   misc. functions
 * 
 * Copyright (C) 2008-2009 wj32
 * 
 * This file is part of Process Hacker.
 * 
 * Process Hacker is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Process Hacker is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Process Hacker.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;
using System.Windows.Forms;
using ProcessHacker.Native;
using ProcessHacker.Native.Api;
using ProcessHacker.Native.Objects;

namespace ProcessHacker.Common
{
    public static class PhUtils
    {
        public static string[] DangerousNames = 
        {
            "csrss.exe", "dwm.exe", "logonui.exe", "lsass.exe", "lsm.exe", "services.exe",
            "smss.exe", "wininit.exe", "winlogon.exe"
        };

      
        /// <summary>
        /// Gets whether the specified process is a system process.
        /// </summary>
        /// <param name="pid">The PID of a process to check.</param>
        /// <returns>Whether the process is a system process.</returns>
        public static bool IsDangerousPid(int pid)
        {
            if (pid == 4)
                return true;

            try
            {
                using (var phandle = new ProcessHandle(pid, OSVersion.MinProcessQueryInfoAccess))
                {
                    foreach (string s in DangerousNames)
                    {
                        if ((Environment.SystemDirectory + "\\" + s).Equals(
                            FileUtils.GetFileName(FileUtils.GetFileName(phandle.GetImageFileName())),
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            { }

            return false;
        }

        /// <summary>
        /// Formats an error message.
        /// </summary>
        /// <param name="operation">
        /// The operation being performed, e.g. "Unable to X"
        /// </param>
        /// <param name="ex">The exception to use.</param>
        /// <returns>A formatted error message.</returns>
        private static string FormatException(string operation, Exception ex)
        {
            if (!string.IsNullOrEmpty(operation))
                return operation + ": " + ex.Message + (ex.InnerException != null ? " (" + ex.InnerException.Message + ")" : "");
            else
                return ex.Message + (ex.InnerException != null ? " (" + ex.InnerException.Message + ")" : "");
        }

        public static string FormatFileInfo(
            string fileName,
            string fileDescription,
            string fileCompanyName,
            string fileVersion,
            int indent
            )
        {
            StringBuilder sb = new StringBuilder();
            string indentStr = new string(' ', indent);

            if (!string.IsNullOrEmpty(fileName))
                sb.AppendLine(indentStr + fileName);

            if (!string.IsNullOrEmpty(fileDescription))
            {
                if (!string.IsNullOrEmpty(fileVersion))
                {
                    sb.AppendLine(indentStr + fileDescription + " " + fileVersion);
                }
                else
                {
                    sb.AppendLine(indentStr + fileDescription);
                }
            }
            else
            {
                sb.AppendLine(indentStr + fileVersion);
            }

            if (!string.IsNullOrEmpty(fileCompanyName))
                sb.AppendLine(indentStr + fileCompanyName);

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        public static string FormatPriorityClass(ProcessPriorityClass priorityClass)
        {
            switch (priorityClass)
            {
                case ProcessPriorityClass.AboveNormal:
                    return "Above Normal";
                case ProcessPriorityClass.BelowNormal:
                    return "Below Normal";
                case ProcessPriorityClass.High:
                    return "High";
                case ProcessPriorityClass.Idle:
                    return "Idle";
                case ProcessPriorityClass.Normal:
                    return "Normal";
                case ProcessPriorityClass.RealTime:
                    return "Realtime";
                case ProcessPriorityClass.Unknown:
                default:
                    return "";
            }
        }

        public static string GetBestUserName(string userName, bool includeDomain)
        {
            if (userName == null)
                return "";

            if (!userName.Contains("\\"))
                return userName;

            string[] split = userName.Split(new char[] { '\\' }, 2);
            string domain = split[0];
            string user = split[1];

            if (includeDomain)
                return domain + "\\" + user;
            else
                return user;
        }

        /// <summary>
        /// Gets an appropriate foreground color to be displayed on top of a 
        /// specified background color.
        /// </summary>
        /// <param name="backColor">The background color.</param>
        /// <returns>
        /// Black if the background color's brightness is above 0.4, otherwise 
        /// White.
        /// </returns>
        public static Color GetForeColor(Color backColor)
        {
            if (backColor.GetBrightness() > 0.4)
                return Color.Black;
            else
                return Color.White;
        }

       

        public static string GetIntegrity(this TokenHandle tokenHandle, out int integrityLevel)
        {
            var groups = tokenHandle.GetGroups();
            string integrity = null;

            integrityLevel = 0;

            for (int i = 0; i < groups.Length; i++)
            {
                if ((groups[i].Attributes & SidAttributes.IntegrityEnabled) != 0)
                {
                    integrity = groups[i].GetFullName(false).Replace(" Mandatory Level", "");

                    if (integrity == "Untrusted")
                        integrityLevel = 0;
                    else if (integrity == "Low")
                        integrityLevel = 1;
                    else if (integrity == "Medium")
                        integrityLevel = 2;
                    else if (integrity == "High")
                        integrityLevel = 3;
                    else if (integrity == "System")
                        integrityLevel = 4;
                    else if (integrity == "Installer")
                        integrityLevel = 5;
                }

                groups[i].Dispose();
            }

            return integrity;
        }

        public static bool IsDotNetProcess(int pid)
        {
            var publish = new Debugger.Core.Wrappers.CorPub.ICorPublish();
            Debugger.Core.Wrappers.CorPub.ICorPublishProcess process = null;

            try
            {
                process = publish.GetProcess(pid);

                if (process == null)
                    return false;

                return process.IsManaged;
            }
            finally
            {
                if (process != null)
                {
                    Debugger.Wrappers.ResourceManager.ReleaseCOMObject(process.WrappedObject, process.GetType());
                }
            }
        }

        public static bool IsEmpty(this IPEndPoint endPoint)
        {
            return endPoint.Address.GetAddressBytes().IsEmpty() && endPoint.Port == 0;
        }

        public static void IsNetworkError(string url)
        {
            if (OSVersion.IsAboveOrEqual(WindowsVersion.Vista))
            {
                IntPtr ndfhandle = IntPtr.Zero;

                HResult ndfCreate = Win32.NdfCreateWebIncident(url, ref ndfhandle);
                ndfCreate.ThrowIf();

                Win32.NdfExecuteDiagnosis(ndfhandle, IntPtr.Zero); //Will throw error if user cancels
                Win32.NdfCloseIncident(ndfhandle);
            }
        }
        
        public static void IsNetworkError(string url, IntPtr hwnd)
        {
            if (OSVersion.IsAboveOrEqual(WindowsVersion.Vista))
            {
                IntPtr ndfhandle = IntPtr.Zero;

                HResult ndfCreate = Win32.NdfCreateWebIncident(url, ref ndfhandle);
                ndfCreate.ThrowIf();

                Win32.NdfExecuteDiagnosis(ndfhandle, hwnd); //Will throw error if user cancels
                Win32.NdfCloseIncident(ndfhandle);
            }
        }

        /// <summary>
        /// Checks if a connection to the URL can be established.
        /// </summary>
        /// <param name="url">URL Address to check</param>
        /// <returns>true if established</returns>
        public static bool IsInternetAddressReachable(string url)
        {
            return Win32.InternetCheckConnection(url, 1, 0);
        }

        /// <summary>
        /// Fast method of checking a connection to the Internet can be established.
        /// </summary>
        /// <returns>True if connected</returns>
        public static bool IsInternetConnected()
        {
            try
            {
                System.Net.IPHostEntry entry = System.Net.Dns.GetHostEntry("www.msftncsi.com");
                return true;
               
                //http://www.msftncsi.com/ncsi.txt 
                //Vista/Win7 Internet Connectivity test address, 
                //Every Vista/Win7 machine uses this for checking Internet Connectivity.
                //Probably the most reliable internet address...
                //More Info: http://technet.microsoft.com/en-us/library/cc766017%28WS.10%29.aspx
            }
            catch
            { return false; }
        }

        /// <summary>
        /// Reliable but slower method of checking if a connection to the Internet can be established.
        /// </summary>
        /// <returns>True if connected</returns>
        public static bool IsInternetConnectedSlow()
        {
            return Win32.InternetCheckConnection("http://www.msftncsi.com", 1, 0);
        }

        /// <summary>
        /// Opens a registry key in the Registry Editor.
        /// </summary>
        /// <param name="keyName">
        /// The path to the registry key, in either abbreviated (HKCU, HKLM, etc.) 
        /// or full (HKEY_CURRENT_USER, etc.) format.
        /// </param>
        public static void OpenKeyInRegedit(string keyName)
        {
            OpenKeyInRegedit(null, keyName);
        }

        /// <summary>
        /// Opens a registry key in the Registry Editor.
        /// </summary>
        /// <param name="window">
        /// The window in which the elevation dialog, if any, should be centered.
        /// </param>
        /// <param name="keyName">
        /// The path to the registry key, in either abbreviated (HKCU, HKLM, etc.) 
        /// or full (HKEY_CURRENT_USER, etc.) format.
        /// </param>
        public static void OpenKeyInRegedit(IWin32Window window, string keyName)
        {
            string lastKey = keyName;

            // Expand the abbreviations.
            if (lastKey.ToLowerInvariant().StartsWith("hkcu"))
                lastKey = "HKEY_CURRENT_USER" + lastKey.Substring(4);
            else if (lastKey.ToLowerInvariant().StartsWith("hku"))
                lastKey = "HKEY_USERS" + lastKey.Substring(3);
            else if (lastKey.ToLowerInvariant().StartsWith("hkcr"))
                lastKey = "HKEY_CLASSES_ROOT" + lastKey.Substring(4);
            else if (lastKey.ToLowerInvariant().StartsWith("hklm"))
                lastKey = "HKEY_LOCAL_MACHINE" + lastKey.Substring(4);

            // Set the last opened key in regedit config. Note that if we are on 
            // Vista, we need to append "Computer\" to the beginning.
            using (var regeditKey =
                Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit",
                    Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree
                    ))
            {
                if (OSVersion.IsAboveOrEqual(WindowsVersion.Vista))
                    regeditKey.SetValue("LastKey", "Computer\\" + lastKey);
                else
                    regeditKey.SetValue("LastKey", lastKey);
            }

            // If we have UAC and we aren't elevated, request that regedit be elevated 
            // and pass the window handle. This is so that we get the elevation 
            // dialog in the center of the specified window because it looks nice.
            // Also, this makes sure we don't throw an exception if the user denies 
            // elevation.
            if (OSVersion.HasUac && Xylon.PH.Objects.Instances.GetElevationPrivilege() == TokenElevationType.Limited)
            {
               StartProgramAdmin(
                    Environment.SystemDirectory + "\\..\\regedit.exe",
                    "",
                    null,
                    ShowWindowType.Normal,
                    window != null ? window.Handle : IntPtr.Zero
                    );
            }
            else
            {
                System.Diagnostics.Process.Start(Environment.SystemDirectory + "\\..\\regedit.exe");
            }
        }

        public static void StartProcessHackerAdmin(string args, MethodInvoker successAction, IntPtr hWnd)
        {
            StartProgramAdmin(ProcessHandle.Current.GetMainModule().FileName,
                args, successAction, ShowWindowType.Show, hWnd);
        }

        public static void StartProgramAdmin(string program, string args,
         MethodInvoker successAction, ShowWindowType showType, IntPtr hWnd)
        {
            var info = new ShellExecuteInfo();

            info.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(info);
            info.lpFile = program;
            info.nShow = showType;
            info.lpVerb = "runas";
            info.lpParameters = args;
            info.hWnd = hWnd;

            if (Win32.ShellExecuteEx(ref info))
            {
                if (successAction != null)
                    successAction();
            }
        }
 


        /// <summary>
        /// Controls whether the UAC shield icon is displayed on the button.
        /// </summary>
        /// <param name="visible">Whether the shield icon is visible.</param>
        public static void SetShieldIcon(this Button button, bool visible)
        {
            Win32.SendMessage(button.Handle, WindowMessage.BcmSetShield, 0, visible ? 1 : 0);
        }

        /// <summary>
        /// Sets the theme of a control.
        /// </summary>
        /// <param name="control">The control to modify.</param>
        /// <param name="theme">A name of a theme.</param>
        public static void SetTheme(this Control control, string theme)
        {
            // Don't set on XP, doesn't look better than without SetWindowTheme.
            if (OSVersion.IsAboveOrEqual(WindowsVersion.Vista))
            {
                Win32.SetWindowTheme(control.Handle, theme, null);
            }
        }

       
        /// <summary>
        /// Notifies the user of an error and asks whether the operation should 
        /// continue.
        /// </summary>
        /// <param name="operation">
        /// The operation being performed, e.g. "Unable to X"
        /// </param>
        /// <param name="ex">The exception to notify the user of.</param>
        /// <returns>
        /// True if the user wants to continue the operation, otherwise false.
        /// </returns>
        public static bool ShowContinueMessage(string operation, Exception ex)
        {
            return MessageBox.Show(
                FormatException(operation, ex),
                "Process Hacker",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Error
                ) == DialogResult.OK;
        }
        
    }
}
