/*
 * Process Hacker - 
 *   circular buffer
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

/* If enabled, the sizes of circular buffers are limited to (and rounded up to) 
 * powers of two, increasing performance.
 */
#define POWER_OF_TWO_SIZE

using System;
using System.Collections.Generic;

namespace ProcessHacker.Common
{
    /// <summary>
    /// Provides methods for manipulating a circular buffer. A circular buffer 
    /// is a fixed-size array where old elements will be automatically deleted 
    /// as new elements are added.
    /// </summary>
    /// <remarks>
    /// This data structure is not thread-safe. You must provide your own 
    /// synchronization if more than one thread reads from or writes to the 
    /// buffer.
    /// </remarks>
    /// <example>
    /// Ten-element circular buffer:
    /// Data array:     [4] [3] [2] [1] [0] [9] [8] [7] [6] [5]
    ///                                      ^ most recent data
    ///                                      ^ index
    /// </example>
    public class CircularBuffer<T> : IList<T>
    {
        private int _size;
#if POWER_OF_TWO_SIZE
        private int _sizeMinusOne;
#endif
        private int _count;
        private int _index;
        private T[] _data;

        /// <summary>
        /// Creates a new circular buffer of the specified size.
        /// </summary>
        /// <param name="size">The size of the buffer.</param>
        public CircularBuffer(int size)
        {
            /*
             * [ ] [ ] [ ] [ ] [ ] [ ] [ ] [ ] [ ] [ ]
             *  ^ _index
             */

#if POWER_OF_TWO_SIZE
            _size = size.RoundUpTwo();
            _sizeMinusOne = _size - 1;
#else
            _size = size;
#endif

            _count = 0;
            _index = 0;
            _data = new T[_size];
        }

        public CircularBuffer(System.IO.Stream s)
        {
            byte[] buf = new byte[sizeof(int)];

            s.Read(buf, 0, 4);
            _size = buf.ToInt32();
#if POWER_OF_TWO_SIZE
            _sizeMinusOne = _size - 1;
#endif
            s.Read(buf, 0, 4);
            _count = buf.ToInt32();
            s.Read(buf, 0, 4);
            _index = buf.ToInt32();

            _data = new T[_size];

            int i = 0;
            int length = Buffer.ByteLength(_data);

            buf = new byte[256];

            while (length > 0)
            {
                int readLength = length > buf.Length ? buf.Length : length;

                s.Read(buf, 0, readLength);
                length -= readLength;
                Buffer.BlockCopy(buf, 0, _data, i, readLength);
                i += readLength;
            }
        }

        /// <summary>
        /// Gets or sets an element in the buffer. This is guaranteed to 
        /// never throw an exception.
        /// </summary>
        /// <param name="index">
        /// A zero-based index into the buffer. Index 0 contains the 
        /// most recently added item, and higher positive indicies 
        /// access less recent items. Index -1 contains the least recently 
        /// added item, and lower negative indicies access more recent 
        /// items.
        /// </param>
        public T this[int index]
        {
            get
            {
                /*
                 * For example, if _index = 6 and index = 5:
                 * 
                 * [5] [4] [3] [2] [1] [0] [9] [8] [7] [6]
                 *                          ^ _index
                 *      ^ (_index + index) mod _size = 11 mod 10 = 1
                 */

#if POWER_OF_TWO_SIZE
                // See the comment in Add for more details on power-of-two modulus.
                return _data[(_index + index) & _sizeMinusOne];
#else
                // See the comment in Add for more details on modulus.
                return _data[(((_index + index) % _size) + _size) % _size];
#endif
            }
            set
            {
#if POWER_OF_TWO_SIZE
                // See the comment in Add for more details on power-of-two modulus.
                _data[(_index + index) & _sizeMinusOne] = value;
#else
                // See the comment in Add for more details.
                _data[(((_index + index) % _size) + _size) % _size] = value;
#endif
            }
        }

        /// <summary>
        /// Gets the number of elements stored in the buffer.
        /// </summary>
        public int Count
        {
            get { return _count; }
            private set { _count = value; }
        }

        /// <summary>
        /// Gets the maximum number of elements that can be stored in 
        /// the buffer.
        /// </summary>
        public int Size
        {
            get { return _size; }
        }

        /// <summary>
        /// Adds an element to the buffer. If the maximum buffer size 
        /// has been reached, the least recently added element will 
        /// be erased by the new element.
        /// </summary>
        /// <param name="value">The element to add.</param>
        public void Add(T value)
        {
            /*
             * To add an item to the circular buffer the index is 
             * decremented and a modulus is performed on it to ensure 
             * it is not negative.
             * 
             * [5] [4] [3] [2] [1] [0] [9] [8] [7] [6]
             *                          ^ _index (6)
             * When the new element x is added:
             * [5] [4] [3] [2] [1] [x] [9] [8] [7] [6]
             *                      ^ _index (5)
             *
             * Another example:
             * [9] [8] [7] [6] [5] [4] [3] [2] [1] [0]
             *  ^ _index (0)
             * When the new element x is added:
             * [9] [8] [7] [6] [5] [4] [3] [2] [1] [x]
             *                                      ^ _index (9)
             *                                        = -1 mod 10 = 9
             */

#if POWER_OF_TWO_SIZE
            /* Power-of-two modulus can be performed quickly by ANDing the 
             * value by the power-of-two minus one. This even works for 
             * negative numbers.
             */
            _data[_index = ((_index - 1) & _sizeMinusOne)] = value;
#else
            /* The C# modulus operator produces a result which has the 
             * same sign as the dividend. For circular array access,
             * we want the result to have the same sign as the divisor.
             * We do this by using r = ((i % t) + t) % t where i is 
             * the index (possibly negative) and t is the size of the 
             * array.
             */
            _data[_index = (((_index - 1) % _size) + _size) % _size] = value;
#endif

            if (_count < _size)
                _count++;
        }

        /// <summary>
        /// Resizes the circular buffer.
        /// </summary>
        /// <param name="newSize">The new maximum buffer size.</param>
        public void Resize(int newSize)
        {
            newSize = newSize.RoundUpTwo();

            // If we're not actually resizing the thing...
            if (newSize == _size)
                return;

            T[] newArray = new T[newSize];
            int tailSize = (_size - _index) % _size;
            int headSize = this.Count - tailSize;

            /* 
             * The tail contains the most recent data.
             * [3] [2] [1] [0] [ ] [8] [7] [6] [5] [4]
             * [ ... head ... ]    [ ..... tail ..... ]
             *                      ^ _index (5)
             * tailSize = _size - _index = 5
             * headSize = _count - tailSize = 9 - 5 = 4
             */

            // If the new buffer is bigger than the current one.
            if (newSize > _size)
            {
                /* 
                 * Copy the tail, then the head.
                 * This means that the tail will now be at the front.
                 * [8] [7] [6] [5] [4] [3] [2] [1] [0] [ ] [ ] [ ]
                 * [ ..... tail ..... ][ ... head ... ]
                 *  ^ _index (0)
                 */
                Array.Copy(_data, _index, newArray, 0, tailSize);
                Array.Copy(_data, 0, newArray, tailSize, headSize);
                _index = 0;
            }
            // If the new buffer is smaller than the current one.
            else if (newSize < _size)
            {
                // If the new buffer is smaller than (or equal to) the tail size.
                if (tailSize >= newSize)
                {
                    /* 
                     * Copy only a part of the tail because we don't have enough room.
                     * [8] [7] [6]
                     * [ . tail . ]
                     *  ^ _index (0)
                     */ 
                    Array.Copy(_data, _index, newArray, 0, newSize);
                    _index = 0;
                }
                // If the new buffer is bigger than the tail size.
                else
                {
                    /* 
                     * Copy the tail in full, then copy a part of the head.
                     * [8] [7] [6] [5] [4] [3] [2]
                     * [ ..... tail ..... ][ head ]
                     *  ^ _index (0)
                     */
                    Array.Copy(_data, _index, newArray, 0, tailSize);
                    Array.Copy(_data, 0, newArray, tailSize, newSize - tailSize);
                    _index = 0;
                }

                // The number of elements obviously can't be bigger than the 
                // buffer size.
                if (this.Count > newSize)
                    this.Count = newSize;
            }

            _data = newArray;
            _size = newSize;
            _sizeMinusOne = _size - 1;
        }

        public void Save(System.IO.Stream s)
        {
            s.Write(_size.GetBytes(), 0, sizeof(int));
            s.Write(_count.GetBytes(), 0, sizeof(int));
            s.Write(_index.GetBytes(), 0, sizeof(int));

            int i = 0;
            int length = Buffer.ByteLength(_data);
            byte[] buf = new byte[256];

            while (length > 0)
            {
                int writeLength = length > buf.Length ? buf.Length : length;

                Buffer.BlockCopy(_data, i, buf, 0, writeLength);
                length -= writeLength;
                s.Write(buf, 0, writeLength);
                i += writeLength;
            }
        }

        /// <summary>
        /// Converts the buffer to an array.
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] newArray = new T[this.Count];

            this.CopyTo(newArray, 0);

            return newArray;
        }

        #region IList<T> Members

        /// <summary>
        /// Gets the index of the specified element in the array.
        /// </summary>
        /// <param name="item">The element to search for.</param>
        /// <returns>A positive index if the element was found. Otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            for (int i = 0; i < this.Count; i++)
                if (this[i].Equals(item))
                    return i;

            return -1;
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region ICollection<T> Members

        /// <summary>
        /// Clears the buffer.
        /// </summary>
        public void Clear()
        {
            // Just set the number of elements to zero.
            this.Count = 0;
        }

        /// <summary>
        /// Gets whether the buffer contains the specified element.
        /// </summary>
        /// <param name="item">The element to search for.</param>
        /// <returns>Whether the element is present.</returns>
        public bool Contains(T item)
        {
            return this.IndexOf(item) != -1;
        }

        /// <summary>
        /// Copies the elements of the buffer to the specified array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The index of the destination array at which to begin copying.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            /*
             * We have to make sure we don't copy unused elements.
             * [2] [1] [0] [ ] [ ] [ ] [6] [5] [4] [3]
             * [ . head . ]            [ ... tail ... ]
             *                          ^ _index (6)
             * tailSize = _size - _index = 10 - 6 = 4
             * headSize = _count - tailSize = 7 - 4 = 3
             */
            int tailSize = _size - _index;
            int headSize = this.Count - tailSize;

            // Copy the tail, then the head.
            Array.Copy(_data, _index, array, arrayIndex, tailSize);
            Array.Copy(_data, 0, array, arrayIndex + tailSize, headSize);
        }

        /// <summary>
        /// Gets whether the buffer is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// This method is not supported.
        /// </summary>
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable<T> Members

        /// <summary>
        /// Gets an enumerator for the buffer.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
                yield return this[i];
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Gets an enumerator for the buffer.
        /// </summary>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < this.Count; i++)
                yield return this[i];
        }

        #endregion
    }
}
