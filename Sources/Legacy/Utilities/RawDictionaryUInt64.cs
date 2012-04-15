using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage.Legacy.Utilities
{
    public sealed class RawDictionaryUInt64<T>
    {
        private class Bucket
        {
            public bool ContainsMultipleItems;
            public RawLinkedList<KeyValuePair<ulong, T>> Entries;
            public ulong? EntryKey;
            public T EntryValue;
        }

        public RawDictionaryUInt64(uint bucketCount)
        {
            _bucketCount = bucketCount;
            _buckets = new Bucket[_bucketCount];
        }

        private Bucket[] _buckets;
        public int Count;

        private readonly uint _bucketCount;

        private Bucket GetBucket(ulong key)
        {
            var xIdx = key % _bucketCount;
            var xBucket = _buckets[xIdx];
            if (xBucket == null)
            {
                xBucket = new Bucket();
                _buckets[xIdx] = xBucket;                
            }
            return xBucket;
        }

        public bool Remove(ulong key)                           
        {
            var xBucket = GetBucket(key);
            if (!xBucket.ContainsMultipleItems)
            {
                xBucket.Entries = null;
                var xOld = xBucket.EntryKey == key;
                if (xOld)
                {
                    xBucket.EntryKey = null;
                    Count -= 1;
                }
                return xOld;
            }
            var xItem = xBucket.Entries.First;
            while (xItem != null)
            {
                if (xItem.Value.Key == key)
                {
                    xBucket.Entries.Remove(xItem);
                    Count -= 1;
                    break;
                }

                xItem = xItem.Next;
            }
            xBucket.ContainsMultipleItems = xBucket.Entries.Count>1;
            if (!xBucket.ContainsMultipleItems)
            {
                xBucket.EntryKey = xBucket.Entries.First.Value.Key;
                xBucket.EntryValue = xBucket.Entries.First.Value.Value;
                xBucket.Entries = null;
            }
            return true;
        }

        public bool TryGetValue(ulong key, out T value)
        {
            var xBucket = GetBucket(key);
            if (!xBucket.ContainsMultipleItems)
            {
                value = xBucket.EntryValue;
                return xBucket.EntryKey.HasValue && xBucket.EntryKey == key;
            }
            var xItem = xBucket.Entries.First;
            while (xItem != null)
            {
                if (xItem.Value.Key == key)
                {
                    value = xItem.Value.Value;
                    return true;
                }

                xItem = xItem.Next;
            }
            value = default(T);
            return false;
        }

        public void Add(ulong key, T value)
        {
            var xBucket = GetBucket(key);
            if (xBucket.EntryKey.HasValue)
            {
                xBucket.Entries = new RawLinkedList<KeyValuePair<ulong, T>>();
                xBucket.Entries.AddFirst(new KeyValuePair<ulong, T>(xBucket.EntryKey.Value, xBucket.EntryValue));
                xBucket.EntryKey = null;
                xBucket.ContainsMultipleItems = true;
            }

            if (xBucket.ContainsMultipleItems)
            {
                // todo: do we need to check for duplicates?
                xBucket.Entries.AddLast(new KeyValuePair<ulong, T>(key, value));
                xBucket.ContainsMultipleItems = xBucket.Entries.Count > 1;
                Count += 1;
            }
            else
            {
                xBucket.EntryKey = key;
                xBucket.EntryValue = value;
                Count += 1;
            }
        }

        public void Clear()
        {
            for (uint i = 0; i < _bucketCount; i++)
            {
                if (_buckets[i] == null)
                {
                    continue;
                }
                _buckets[i].Entries = null;
                _buckets[i].EntryKey = null;
                _buckets[i].ContainsMultipleItems = false;
            }
            Count = 0;
        }
    }
}