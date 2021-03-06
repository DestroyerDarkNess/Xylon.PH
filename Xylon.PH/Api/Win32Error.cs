/*
 * Process Hacker - 
 *   Win32 error codes
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
using System.Collections.Generic;
using System.Text;
using ProcessHacker.Common;

namespace ProcessHacker.Native.Api
{
    /// <summary>
    /// A Win32 error code.
    /// </summary>
    public enum Win32Error : uint
    {
        Success = 0x0,
        InvalidFunction = 0x1,
        FileNotFound = 0x2,
        PathNotFound = 0x3,
        TooManyOpenFiles = 0x4,
        AccessDenied = 0x5,
        InvalidHandle = 0x6,
        ArenaTrashed = 0x7,
        NotEnoughMemory = 0x8,
        InvalidBlock = 0x9,
        BadEnvironment = 0xa,
        BadFormat = 0xb,
        InvalidAccess = 0xc,
        InvalidData = 0xd,
        OutOfMemory = 0xe,
        InvalidDrive = 0xf,
        CurrentDirectory = 0x10,
        NotSameDevice = 0x11,
        NoMoreFiles = 0x12,
        WriteProtect = 0x13,
        BadUnit = 0x14,
        NotReady = 0x15,
        BadCommand = 0x16,
        Crc = 0x17,
        BadLength = 0x18,
        Seek = 0x19,
        NotDosDisk = 0x1a,
        SectorNotFound = 0x1b,
        OutOfPaper = 0x1c,
        WriteFault = 0x1d,
        ReadFault = 0x1e,
        GenFailure = 0x1f,
        SharingViolation = 0x20,
        LockViolation = 0x21,
        WrongDisk = 0x22,
        SharingBufferExceeded = 0x24,
        HandleEof = 0x26,
        HandleDiskFull = 0x27,
        NotSupported = 0x32,
        RemNotList = 0x33,
        DupName = 0x34,
        BadNetPath = 0x35,
        NetworkBusy = 0x36,
        DevNotExist = 0x37,
        TooManyCmds = 0x38,
        FileExists = 0x50,
        CannotMake = 0x52,
        AlreadyAssigned = 0x55,
        InvalidPassword = 0x56,
        InvalidParameter = 0x57,
        NetWriteFault = 0x58,
        NoProcSlots = 0x59,
        TooManySemaphores = 0x64,
        ExclSemAlreadyOwned = 0x65,
        SemIsSet = 0x66,
        TooManySemRequests = 0x67,
        InvalidAtInterruptTime = 0x68,
        SemOwnerDied = 0x69,
        SemUserLimit = 0x6a,
        Cancelled = 0x4c7
    }

    public static class Win32ErrorExtensions
    {
        public static HResult GetHResult(this Win32Error errorCode)
        {
            int error = (int)errorCode;

            if ((error & 0x80000000) == 0x80000000)
                return (HResult)error;

            return (HResult)(0x80070000 | (uint)(error & 0xffff));
        }

        public static string GetMessage(this Win32Error errorCode)
        {
            String255 buffer = new String255();

            unsafe
            {
                if ((buffer.Length = (byte)Win32.FormatMessage(
                    0x3200,
                    IntPtr.Zero,
                    (int)errorCode,
                    0,
                    new IntPtr(buffer.Buffer),
                    String255.MaximumLength,
                    IntPtr.Zero
                    )) == 0)
                    return "Unknown error (0x" + ((int)errorCode).ToString("x") + ")";

                String255 result = new String255();

                for (int i = 0; i < buffer.Length; i++)
                {
                    char c = buffer.Buffer[i];

                    if (!char.IsLetterOrDigit(c) &&
                        !char.IsPunctuation(c) &&
                        !char.IsSymbol(c) &&
                        !char.IsWhiteSpace(c))
                        break;

                    result.Append(c);
                }

                return result.ToString().Replace("\r\n", "");
            }
        }
    }
}
