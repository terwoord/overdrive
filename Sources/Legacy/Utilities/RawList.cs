using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage.Legacy
{
    public sealed class RawList<T>
    {
        private T[] _array = new T[InitialCapacity];
        private uint _count = 0;
        private uint _capacity = InitialCapacity;

        public const uint InitialCapacity = 64;

        public RawList()
        {
        }

        public RawList(uint capacity)
        {
            Capacity = capacity;
        }


        public uint Count
        {
            get { return _count; }
            set { _count = value; }
        }

        // MtW: Renamed Size to Capacity, as that's what the .NET alternative is..
        public uint Capacity
        {
            get
            {
                return _capacity;
            }
            set
            {
                if(value!=_capacity)
                {
                    if(value < _count)
                    {
                        throw new Exception("Cannot set capacity smaller than Count!");
                    }
                    _capacity = value;
                    T[] newArray = new T[_capacity];

                    System.Array.Copy(_array, newArray, Math.Min(_array.Length, _count));
                    _array = newArray;
                }
            }
        }

        public T[] Array { get { return _array; } }


        public void Add(T item)
        {
            if (_count == _capacity)
                ExpandArray();

            _array[_count] = item;
            _count++;

        } // function


        public void Insert(uint index, T item)
        {
            if(_count==_capacity)
            {
                ExpandArray();
            }
            System.Array.Copy(_array, index, _array, index + 1, _count - index);
            _array[index] = item;
            _count++;
        } // function

        public int IndexOf(T item)
        {
            return System.Array.IndexOf(_array, item, 0, (int)_count);
        }

        public void Move(uint sourceIndex, uint destinationIndex)
        {
            if(sourceIndex >= _count || destinationIndex>=_count)
            {
                throw new IndexOutOfRangeException();
            }
            // do nothing
            if(sourceIndex==destinationIndex)
            {
                return;
            }
            if (sourceIndex == (_count - 1))
            {
                // move last to somewhere in the middle
                var xItem = _array[sourceIndex];
                Insert(destinationIndex, xItem);
                _count--; // insert increases the count, so decrease it here
                // no need to clean the old item location, as it's overriden by insert
                return;
            }
            if(sourceIndex > destinationIndex)
            {
                var xItem = _array[sourceIndex];
                System.Array.Copy(_array, destinationIndex, _array, destinationIndex + 1, sourceIndex - destinationIndex);
                _array[destinationIndex] = xItem;
                return;
            }
            if(destinationIndex>sourceIndex)
            {
                var xItem = _array[sourceIndex];
                System.Array.Copy(_array, sourceIndex + 1, _array, sourceIndex, destinationIndex - sourceIndex);
                _array[destinationIndex] = xItem;
                return;
            }
            throw new Exception("Situation not handled!");
        } // function


        public bool Remove(T item)
        {
            var xIdx = System.Array.IndexOf<T>(_array, item, 0, (int)_count);
            if(xIdx!=-1)
            {
                RemoveAt((uint)xIdx);
            }
            return xIdx != -1;

        } // function


        public void RemoveAt(uint index)
        {
            if(index >= _capacity)
            {
                throw new IndexOutOfRangeException();
            }
            if (index == (_count - 1))
            {
                // removing last item
                _array[_count - 1] = default(T);
            }
            else
            {
                System.Array.Copy(_array, index + 1, _array, index, _count - index);
            }
            _count--;
        } // function

        public void RemoveLast()
        {
            if(_count>0)
            {
                RemoveAt(_count - 1);
            }
        }


        private void ExpandArray()
        {
            Capacity = _capacity * 2;
        } // function

        public T this[int index]
        {
            get
            {
                if(index >= _count)
                {
                    throw new IndexOutOfRangeException();
                }
                return _array[index];
            }
            set
            {
                if (index >= _count)
                {
                    throw new IndexOutOfRangeException();
                }
                _array[index] = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fastClear">If true, count is reset, but the array still keeps a reference to the values. (use true for vast clearing)</param>
        public void Clear(bool fastClear = false)
        {
            System.Array.Clear(_array, 0, (int)_count);
            _count = 0;
        }


    } // class


} // namespace
