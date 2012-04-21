using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using TerWoord.OverDriveStorage;
using TerWoord.OverDriveStorage.Implementations;
using ByteConverter = TerWoord.OverDriveStorage.Utilities.ByteConverter;

namespace PerfTester
{
    partial class BackupTestForm
    {
        private double mTotalSeconds;
        private ulong mTotalSize;
        private ulong mTotalSizeProcessed;
        private static readonly Guid ConfigId = new Guid("{54124FAE-9CFB-47E3-A487-41FB787DEB5F}");
        private ulong mCurIdx;

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            const string xOutputDir = @"c:\ODSStore";
            const uint xStepBlockCount = (10 * 1024 * 1024) / 2048;
            mCurIdx = 0;
            var xArg = (Tuple<string, bool>)e.Argument;
            var xDir = xArg.Item1;
            if (xArg.Item2)
            {
                if (Directory.Exists(xOutputDir))
                {
                    Directory.Delete(xOutputDir, true);
                }
            }

            mTotalSize = 0;

            #region calculate total size

            foreach (var xFile in Directory.GetFiles(xDir, "*.*", SearchOption.TopDirectoryOnly))
            {
                var xFileInfo = new FileInfo(xFile);
                mTotalSize += (ulong)xFileInfo.Length;
            }

            #endregion calculate total size

            CheckForIllegalCrossThreadCalls = true;
            bool xCreated;
            var xDedupStore = OpenDedupStoreAndCreateIfNotExist(xOutputDir, out xCreated);

            using (xDedupStore)
            {
                Console.Write("Preloading caches...");
                var xSW = new Stopwatch();
                xSW.Start();
                ((IBlockStore)xDedupStore).PreloadCaches();
                xSW.Stop();
                Console.WriteLine("Done in {0}", xSW.Elapsed);
                xSW.Reset();
                ((IBlockStore)xDedupStore).Id = "Dedup";
                if (xCreated)
                {
                    #region store files

                    using (var xConfigWriter = new StreamWriter(Path.Combine(xOutputDir, "Config.xml")))
                    {
                        using (var xConfig = XmlWriter.Create(xConfigWriter))
                        {
                            xConfig.WriteStartDocument();
                            xConfig.WriteStartElement("config");
                            var xCurrentBlock = 0UL;
                            foreach (var xFile in Directory.GetFiles(xDir, "*.*", SearchOption.TopDirectoryOnly))
                            {
                                xConfig.WriteStartElement("File");
                                xConfig.WriteAttributeString("OriginalName", xFile);
                                using (var xFS = new FileStream(xFile, FileMode.Open))
                                {
                                    xConfig.WriteAttributeString("SizeInBytes", xFS.Length.ToString());
                                    xConfig.WriteAttributeString("StartBlock", xCurrentBlock.ToString());
                                    xConfig.WriteEndElement();

                                    xSW.Reset();
                                    Console.WriteLine("Storing '{0}'...", xFile);

                                    xSW.Start();
                                    var xBuff = new byte[xDedupStore.BlockSize];
                                    var xBuffSegment = new ArraySegment<byte>(xBuff);
                                    while (xFS.Position < xFS.Length)
                                    {
                                        var xRead = xFS.Read(xBuff, 0, xBuff.Length);
                                        mTotalSize += (ulong)xRead;
                                        if (xRead != xBuff.Length)
                                        {
                                            if (xFS.Position != xFS.Length)
                                            {
                                                throw new Exception("Failed to read a full block!");
                                            }
                                            Array.Clear(xBuff, xRead, xBuff.Length - xRead);
                                        }

                                        xDedupStore.Store(xCurrentBlock, xBuffSegment);
                                        xCurrentBlock++;

                                        if (xCurrentBlock % xStepBlockCount == 0)
                                        {
                                            xSW.Stop();
                                            mQueuedStats.Enqueue(new Tuple<ulong, double>(mCurIdx, xSW.Elapsed.TotalSeconds));
                                            var xProgress = Math.Round((mTotalSize / (double)mTotalSizeProcessed) * 100);
                                            if (double.IsInfinity(xProgress) || double.IsNaN(xProgress))
                                            {
                                                xProgress = 0;
                                            }
                                            backgroundWorker.ReportProgress((int)xProgress);
                                            mTotalSeconds += xSW.Elapsed.TotalSeconds;
                                            mCurIdx++;
                                            xSW.Reset();
                                            xSW.Start();
                                        }
                                    }
                                    xSW.Stop();
                                    mTotalSeconds += xSW.Elapsed.TotalSeconds;
                                }
                            }

                            xConfig.WriteEndElement();
                            xConfig.WriteEndDocument();
                            xConfig.Flush();
                        }
                    }

                    #endregion store files

                    Console.Write("Flushing all writes...");
                    xSW.Reset();
                    xSW.Start();
                    ((IBlockStore)xDedupStore).Flush();
                    xSW.Stop();
                    mTotalSeconds += xSW.Elapsed.TotalSeconds;
                    Console.WriteLine("Done");
                }
                else
                {
                    throw new Exception("Verifying not yet implemented!");

                    #region verify data

                    //var xFiles = new List<KeyValuePair<Guid, string>>();
                    //using (var xOdbfs = new ODBFSImpl(xDedupStore))
                    //{
                    //    #region read config
                    //    var xDoc = ReadConfig(xOdbfs);
                    //    foreach (XmlNode xFileElem in xDoc.DocumentElement.ChildNodes)
                    //    {
                    //        if (xFileElem.LocalName != "File")
                    //        {
                    //            continue;
                    //        }
                    //        xFiles.Add(new KeyValuePair<Guid, string>(new Guid(xFileElem.Attributes["Id"].Value), xFileElem.Attributes["OriginalName"].Value));
                    //    }
                    //    #endregion read config

                    //    foreach (var xFileItem in xFiles)
                    //    {
                    //        Console.WriteLine("Verifying {0}", xFileItem.Value);
                    //        using (var xFSFile = new FileStream(xFileItem.Value, FileMode.Open))
                    //        {
                    //            using (var xStoreBlock = xOdbfs.OpenBlock(xFileItem.Key))
                    //            {
                    //                var xBuffFile = new byte[xStoreBlock.BlockSize];
                    //                var xBuffStore = new byte[xStoreBlock.BlockSize];
                    //                var xBuffStoreSeg = new ArraySegment<byte>(xBuffStore);

                    //                var xBlock = 0UL;
                    //                var xMismatch = false;

                    //                while (xFSFile.Position < xFSFile.Length)
                    //                {
                    //                    var xRead = xFSFile.Read(xBuffFile, 0, (int)xStoreBlock.BlockSize);
                    //                    if (xRead != xStoreBlock.BlockSize)
                    //                    {
                    //                        if (xFSFile.Position != xFSFile.Length)
                    //                        {
                    //                            throw new Exception("Didn't read full data block!");
                    //                        }
                    //                        Array.Clear(xBuffFile, xRead, (int)(xStoreBlock.BlockSize - 1));
                    //                    }
                    //                    xStoreBlock.Retrieve(xBlock, xBuffStoreSeg);

                    //                    if (!DeduplicatingBlockStore.CompareBlocks(xBuffFile, xBuffStore))
                    //                    {
                    //                        Console.WriteLine("    Mismatch in block {0}!", xBlock);
                    //                        xMismatch = true;
                    //                        break;
                    //                    }

                    //                    xBlock++;
                    //                }
                    //                if (!xMismatch)
                    //                {
                    //                    Console.WriteLine("    Done");
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    #endregion verify data
                }
            }
            Console.WriteLine("Done");
            var xTotalMBs = ((double)mTotalSize) / (1024 * 1024);
            var xMBperSec = xTotalMBs / mTotalSeconds;
            Console.WriteLine("Speed: {0} MB/s", xMBperSec);
            MessageBox.Show(String.Format("Total MBs: {0}\r\nTime: {1}\r\nSpeed: {2}",
                xTotalMBs, mTotalSeconds, xMBperSec));
        }

        private static IBlockManagingStore OpenDedupStoreAndCreateIfNotExist(string dataStoreDir, out bool created)
        {
            created = false;
            if (Directory.Exists(dataStoreDir))
            {
                return OpenDedupStore(dataStoreDir);
                //var xFS = new FileStream(Path.Combine(dataStoreDir, "data.bin"), FileMode.Open);
                //return new SimpleStreamBlockStore(xFS, 2048);
            }
            else
            {
                //Directory.CreateDirectory(dataStoreDir);
                created = true;
                return CreateDedupStore(dataStoreDir);
                //var xFS = new FileStream(Path.Combine(dataStoreDir, "data.bin"), FileMode.Create);
                //xFS.SetLength(1 * 1024 * 1024 * 1024L);
                //return new SimpleStreamBlockStore(xFS, 2048);
            }
        }

        private static IBlockManagingStore OpenDedupStore(string dataStoreDir)
        {
            // for now hardcode these values
            const uint BlockSize = 2048;
            long StoreSize = 6 * 1024 * 1024 * 1024L;
            if (Environment.MachineName.Equals("tw-vms1", StringComparison.InvariantCultureIgnoreCase))
            {
                StoreSize = 50 * 1024 * 1024 * 1024L;
            }
            var xRawBlockStoreFS = new FileStream(Path.Combine(dataStoreDir, "RawBlocks.bin"), FileMode.Open);
            var xRawBlockStore = new SimpleStreamBlockStore(xRawBlockStoreFS, BlockSize);

            var xVirtualBlockManagerFS = new FileStream(Path.Combine(dataStoreDir, "VirtualBlockBitmap.bin"), FileMode.Open);
            if (xVirtualBlockManagerFS.Length != (long)(xRawBlockStore.BlockCount / 8))
            {
                throw new Exception("VirtualBlockBitmap.bin file size mismatch!");
            }

            var xVirtualBlockManager = new BitmapBlockManager(xVirtualBlockManagerFS, (ulong)(xVirtualBlockManagerFS.Length / BlockSize), xRawBlockStore.BlockSize);

            var xVirtualBlockStoreFS = new FileStream(Path.Combine(dataStoreDir, "VirtualBlocks.bin"), FileMode.Open);
            if (xVirtualBlockStoreFS.Length != StoreSize)
            {
                throw new Exception("VirtualBlocks.bin file size mismatch!");
            }
            var xVirtualBlockStore = new SimpleStreamBlockStore(xVirtualBlockStoreFS, BlockSize);

            var xRawBlockManagerFS = new FileStream(Path.Combine(dataStoreDir, "RawBlockBitmap.bin"), FileMode.Open);
            if (xRawBlockManagerFS.Length != (long)(xRawBlockStore.BlockCount / 8))
            {
                throw new Exception("RawBlockBitmap.bin file size mismatch!");
            }
            var xRawBlockManager = new BitmapBlockManager(xRawBlockManagerFS, (ulong)(xRawBlockManagerFS.Length / BlockSize), xRawBlockStore.BlockSize);

            var xRawBlockUsageCounterFS = new FileStream(Path.Combine(dataStoreDir, "RawBlockUsageCounts.bin"), FileMode.Open);
            if (xRawBlockUsageCounterFS.Length != (long)(xRawBlockStore.BlockCount * 8))
            {
                throw new Exception("RawBlockUsageCounts.bin file size mismatch!");
            }
            var xRawBlockUsageCounterStore = new SimpleStreamBlockStore(xRawBlockUsageCounterFS, BlockSize);
            var xRawBlockUsageCounter = new SimpleUsageCountStore(xRawBlockUsageCounterStore);

            var xHashManager = new SimpleHashManager(Path.Combine(dataStoreDir, "Hashes"));

            var xVirtualBlockCount = (xVirtualBlockStore.BlockCount * xVirtualBlockStore.BlockSize) / 8;

            //return new ExperimentalDeduplicatingBlockStore(xVirtualBlockManager, xVirtualBlockStoreCache, xRawBlockStore, xRawBlockManager, xVirtualBlockCount, xRawBlockUsageCounter, xHashManager
            //    , Path.Combine(dataStoreDir, "BatchCache"));
            return new DeduplicatingBlockStore(xVirtualBlockManager, xVirtualBlockStore, xRawBlockStore, xRawBlockManager, xVirtualBlockCount, xRawBlockUsageCounter, xHashManager);
        }

        private static IBlockManagingStore CreateDedupStore(string dataStoreDir)
        {
            const uint BlockSize = 2048;
            long StoreSize = 6 * 1024 * 1024 * 1024L;
            if (Environment.MachineName.Equals("tw-vms1", StringComparison.InvariantCultureIgnoreCase))
            {
                StoreSize = 50 * 1024 * 1024 * 1024L;
            }
            if (Directory.Exists(dataStoreDir))
            {
                Directory.Delete(dataStoreDir, true);
            }
            Directory.CreateDirectory(dataStoreDir);
            var xRawBlockStoreFS = new FileStream(Path.Combine(dataStoreDir, "RawBlocks.bin"), FileMode.Create);
            xRawBlockStoreFS.SetLength(StoreSize);
            var xRawBlockStore = new SimpleStreamBlockStore(xRawBlockStoreFS, BlockSize);
            //var xRawBlockStoreCache = new ExperimentalReadWriteCachingBlockStore(xRawBlockStore, BlockSize);
            //var xRawBlockStoreFS = new FileStream(Path.Combine(dataStoreDir, "RawBlocks.bin"), FileMode.Create);
            //xRawBlockStoreFS.SetLength(StoreSize);
            //var xRawBlockStore = new SimpleStreamBlockStore(xRawBlockStoreFS, BlockSize);
            //var xRawBlockStoreCache = new ReadCachingBlockStore(xRawBlockStore, 128 * BlockSize);

            var xVirtualBlockManagerFS = new FileStream(Path.Combine(dataStoreDir, "VirtualBlockBitmap.bin"), FileMode.Create);
            xVirtualBlockManagerFS.SetLength((long)(xRawBlockStore.BlockCount / 8));

            var xVirtualBlockManager = new BitmapBlockManager(xVirtualBlockManagerFS, (ulong)(xVirtualBlockManagerFS.Length / BlockSize), xRawBlockStore.BlockSize);

            var xVirtualBlockStoreFS = new FileStream(Path.Combine(dataStoreDir, "VirtualBlocks.bin"), FileMode.Create);
            xVirtualBlockStoreFS.SetLength(StoreSize);
            var xVirtualBlockStore = new SimpleStreamBlockStore(xVirtualBlockStoreFS, 8);

            var xRawBlockManagerFS = new FileStream(Path.Combine(dataStoreDir, "RawBlockBitmap.bin"), FileMode.Create);
            xRawBlockManagerFS.SetLength((long)(xRawBlockStore.BlockCount / 8));
            var xRawBlockManager = new BitmapBlockManager(xRawBlockManagerFS, (ulong)(xRawBlockManagerFS.Length / BlockSize), xRawBlockStore.BlockSize);

            var xRawBlockUsageCounterFS = new FileStream(Path.Combine(dataStoreDir, "RawBlockUsageCounts.bin"), FileMode.Create);
            xRawBlockUsageCounterFS.SetLength((long)(xRawBlockStore.BlockCount * 8));
            var xRawBlockUsageCounterStore = new SimpleStreamBlockStore(xRawBlockUsageCounterFS, BlockSize);
            var xRawBlockUsageCounter = new SimpleUsageCountStore(xRawBlockUsageCounterStore);

            SimpleHashManager.Create(Path.Combine(dataStoreDir, "Hashes"));
            var xHashManager = new SimpleHashManager(Path.Combine(dataStoreDir, "Hashes"));

            var xVirtualBlockCount = (xVirtualBlockStore.BlockCount * xVirtualBlockStore.BlockSize) / 8;

            //return new ExperimentalDeduplicatingBlockStore(xVirtualBlockManager, xVirtualBlockStoreCache, xRawBlockStore, xRawBlockManager, xVirtualBlockCount, xRawBlockUsageCounter, xHashManager
            //    , Path.Combine(dataStoreDir, "BatchCache"));
            return new DeduplicatingBlockStore(xVirtualBlockManager, xVirtualBlockStore, xRawBlockStore, xRawBlockManager, xVirtualBlockCount, xRawBlockUsageCounter, xHashManager);
        }
    }
}