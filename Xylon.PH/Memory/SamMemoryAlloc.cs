/*
 * Process Hacker - 
 *   security accounts manager memory allocation wrapper
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
using System.ComponentModel;
using ProcessHacker.Native.Api;

namespace ProcessHacker.Native
{
    /// <summary>
    /// Represents a memory allocation managed by the Security Accounts Manager (SAM).
    /// </summary>
    public sealed class SamMemoryAlloc : MemoryAlloc
    {
        public SamMemoryAlloc(IntPtr memory)
            : this(memory, true)
        { }

        /// <summary>
        /// Creates a memory allocation from an existing SAM managed allocation. 
        /// </summary>
        /// <param name="memory">A pointer to the allocated memory.</param>
        /// <param name="owned">Whether the memory allocation should be freed automatically.</param>
        public SamMemoryAlloc(IntPtr memory, bool owned)
            : base(memory, owned)
        { }

        protected override void Free()
        {
            Win32.SamFreeMemory(this);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void Resize(int newSize)
        {
            throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int Size
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}
