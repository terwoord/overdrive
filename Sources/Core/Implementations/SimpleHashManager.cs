using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerWoord.OverDriveStorage;
using TerWoord.OverDriveStorage.Utilities;

namespace TerWoord.OverDriveStorage.Implementations
{
    public class SimpleHashManager : IHashManager<uint>
    {
        private class HashPage
        {
            public HashPage(int idx)
            {
                PageLock = new ReaderWriterLocker();
            }

            public LinkedList<HashEntry> HashList;
            public readonly ReaderWriterLocker PageLock;
            public Stream File;
        }

        private struct HashEntry
        {
            public uint CRC32;
            public ulong BlockId;
        }

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("HashManager");
            }
            _disposed = true;
            // don't dispose, so we're sure of no reuse
            GC.SuppressFinalize(this);
            for (int i = 0; i < 256; i++)
            {
                var xPage = _hashPages[i];
                xPage.PageLock.EnterWriteLock();// don't unlock, as we want to make sure nobody is usng it anymore..
                WriteHashesToFile(xPage.HashList, xPage.File);
                xPage.HashList.Clear();
                xPage.File.Close();
            }
        }

        private readonly HashPage[] _hashPages;

        public SimpleHashManager(string baseDir)
        {
            _baseDir = baseDir;
            _hashPages = new HashPage[256];
            for (int i = 0; i < 256; i++)
            {
                var xPage = new HashPage(i);
                xPage.File = new FileStream(Path.Combine(baseDir, i.ToString() + ".hsh"), FileMode.Create);
                var xList = new LinkedList<HashEntry>();
                xPage.HashList = xList;
                _hashPages[i] = xPage;

                ReadHashesFromFile(xPage.HashList, xPage.File);
            }
        }

        private static void ReadHashesFromFile(LinkedList<HashEntry> list, Stream fs)
        {
            var xBuff = new byte[8];
            while (fs.Position < fs.Length)
            {
                if (fs.Read(xBuff, 0, xBuff.Length) != 8)
                {
                    throw new Exception("Insufficient data read!");
                }
                list.AddLast(new HashEntry
                {
                    CRC32 = BitConverter.ToUInt32(xBuff, 0),
                    BlockId = BitConverter.ToUInt32(xBuff, 4)
                });
            }
        }

        private static void WriteHashesToFile(LinkedList<HashEntry> list, Stream fs)
        {
            fs.SetLength(0);
            fs.Position = 0;
            var xItem = list.First;
            while (xItem != null)
            {
                fs.Write(BitConverter.GetBytes(xItem.Value.CRC32), 0, 4);
                fs.Write(BitConverter.GetBytes(xItem.Value.BlockId), 0, 4);
                xItem = xItem.Next;
            }
        }

        public static void Create(string baseDir)
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, true);
            }
            Directory.CreateDirectory(baseDir);

            for (int i = 0; i < 256; i++)
            {
                File.WriteAllBytes(Path.Combine(baseDir, i.ToString() + ".hsh"), new byte[0]);
            }
        }

        private readonly string _baseDir;

        public IEnumerable<ulong> GetAllBlocksWithCRC32(uint crc)
        {
            var xFirstByte = (byte)(crc & 0xFF);
            HashPage xPage;
            xPage = _hashPages[xFirstByte];
            using (xPage.PageLock.EnterReadLock())
            {
                var xListCount = xPage.HashList.Count;
                var xItem = xPage.HashList.First;
                while (xItem != null)
                {
                    var xEntry = xItem.Value;
                    if (xEntry.CRC32 == crc)
                    {
                        yield return xEntry.BlockId;
                    }
                    xItem = xItem.Next;
                }
            }
        }

        public void RemoveBlock(ulong blockId, uint crc)
        {
            HashPage xPage;
            var xFirstByte = (byte)(crc & 0xFF);
            xPage = _hashPages[xFirstByte];
            using (xPage.PageLock.EnterWriteLock())
            {
                var xItem = xPage.HashList.First;
                while (xItem != null)
                {
                    if (xItem.Value.BlockId == blockId)
                    {
                        xPage.HashList.Remove(xItem);
                        break;
                    }
                    xItem = xItem.Next;
                }
            }
        }

        public void AddBlock(ulong blockid, uint crc)
        {
            HashPage xPage;
            var xFirstByte = (byte)(crc & 0xFF);
            xPage = _hashPages[xFirstByte];
            using (xPage.PageLock.EnterWriteLock())
            {
                var xList = xPage.HashList;
                xList.AddLast(new HashEntry
                {
                    BlockId = blockid,
                    CRC32 = crc
                });
            }
        }

        public void Flush()
        {
            for (int i = 0; i < 256; i++)
            {
                var xPage = _hashPages[i];
                using (xPage.PageLock.EnterReadLock())
                {
                    WriteHashesToFile(xPage.HashList, xPage.File);
                }
            }
        }
    }
}