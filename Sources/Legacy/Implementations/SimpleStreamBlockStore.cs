using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TerWoord.OverDriveStorage.Legacy.Implementations
{               
    public class SimpleStreamBlockStore: BaseBlockStore
    {
        private readonly Stream mBackend;

        private readonly uint mBlockSize;
        private readonly ulong mBlockCount;

        public SimpleStreamBlockStore(Stream backend, uint blockSize)
        {
            if (backend == null)
            {
                throw new ArgumentNullException("backend");
            }
            mBlockSize = blockSize;
            mBackend = backend;
            if (mBackend.Length % blockSize != 0)
            {
                throw new Exception("File size must be multiple of BlockSize!");
            }
            mBlockCount = (ulong)(mBackend.Length / blockSize);
        }

        public SimpleStreamBlockStore(string file, uint blockSize)
            : this(new FileStream(file, FileMode.Open), blockSize)
        {
        }

        protected override void DoDispose()
        {
            mBackend.Close();
        }

        public override void Store(ulong index, ArraySegment<byte> buffer)
        {
            if (index >= mBlockCount)
            {
                throw new IndexOutOfRangeException();
            }
            if (buffer.Count != mBlockSize)
            {
                throw new Exception("Buffer is not of correct size");
            }
            lock (mBackend)
            {
                mBackend.Position = (long)(index * mBlockSize);
                mBackend.Write(buffer.Array, buffer.Offset, buffer.Count);
            }
        }

        public override void Store(ulong[] indices, ArraySegment<byte> buffer)
        {
            if (buffer.Count != (mBlockSize * indices.Length))
            {
                throw new Exception("Buffer is not of correct size");
            }
            bool xIndicesFollowUp = true;
            var xFirst = indices[0];
            for (uint i = 1; i < indices.Length; i++)
            {
                if (indices[i] != (xFirst + i))
                {
                    xIndicesFollowUp = false;
                    break;
                }
            }
            lock (mBackend)
            {
                if (!xIndicesFollowUp)
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        mBackend.Position = (long)(indices[i] * mBlockSize);
                        mBackend.Write(buffer.Array, (int)(buffer.Offset + (i * mBlockSize)), (int)mBlockSize);
                    }
                }
                else
                {
                    mBackend.Position = (long)xFirst * mBlockSize;
                    mBackend.Write(buffer.Array, buffer.Offset, (int)(indices.Length * mBlockSize));
                }
            }
        }

        public override void Retrieve(ulong index, ArraySegment<byte> buffer)
        {
            if(index >=mBlockCount)
            {
                throw new IndexOutOfRangeException();
            }
            if(buffer.Count != mBlockSize)
            {
                throw new Exception("Buffer is not of correct size");
            }
            lock (mBackend)
            {
                mBackend.Position = (long)(index * mBlockSize);
                mBackend.Read(buffer.Array, buffer.Offset, buffer.Count);
            }
        }

        public override uint BlockSize
        {
            get
            {
                return mBlockSize;
            }
        }

        public override ulong BlockCount
        {
            get
            {
                return mBlockCount;
            }
        }

        public override void DumpCacheInfo(StreamWriter output, string linePrefix)
        {
        }
    }
}