/*
 * Process Hacker - 
 *   symbols extension functions
 * 
 * Copyright (C) 2009 wj32
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
using System.IO;
using System.Windows.Forms;
using ProcessHacker.Common;
using ProcessHacker.Native.Objects;

namespace ProcessHacker.Native.Symbols
{
    public static class SymbolProviderExtensions
    {
   
        public static void LoadKernelModules(this SymbolProvider symbols)
        {
            // hack for drivers, whose sizes never load properly because of dbghelp.dll's dumb guessing
            symbols.PreloadModules = true;

            // load driver symbols
            foreach (var module in Windows.GetKernelModules())
            {
                try
                {
                    symbols.LoadModule(module.FileName, module.BaseAddress);
                }
                catch (Exception ex)
                {
                    Logging.Log(ex);
                }
            }
        }

        public static void LoadProcessModules(this SymbolProvider symbols, ProcessHandle phandle)
        {
            foreach (var module in phandle.GetModules())
            {
                try
                {
                    symbols.LoadModule(module.FileName, module.BaseAddress, module.Size);
                }
                catch (Exception ex)
                {
                    Logging.Log(ex);
                }
            }
        }

        public static void LoadProcessWow64Modules(this SymbolProvider symbols, int pid)
        {
            using (var buffer = new ProcessHacker.Native.Debugging.DebugBuffer())
            {
                buffer.Query(
                    pid,
                    ProcessHacker.Native.Api.RtlQueryProcessDebugFlags.Modules32 |
                    ProcessHacker.Native.Api.RtlQueryProcessDebugFlags.NonInvasive
                    );

                foreach (var module in buffer.GetModules())
                {
                    try
                    {
                        symbols.LoadModule(module.FileName, module.BaseAddress, module.Size);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(ex);
                    }
                }
            }
        }
    }
}
