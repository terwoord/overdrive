using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections.Concurrent;
using System.IO;

namespace PerfTester
{
    public partial class BackupTestForm : Form
    {
        private const int MaxStats = 20;
        public BackupTestForm()
        {
            InitializeComponent();
            if (Environment.MachineName.Equals("tw-vms1", StringComparison.InvariantCultureIgnoreCase))
            {
                tboxDir.Text = @"c:\data\ISOs\";
            }
            else
            {
                tboxDir.Text = @"c:\temp\pack\";
            }

            backupTestFormBindingSource.DataSource = this;

            #region design data
            mStatistics.Add(new Tuple<ulong, double>(1, 1.3));
            mStatistics.Add(new Tuple<ulong, double>(2, 1.5));
            mStatistics.Add(new Tuple<ulong, double>(3, 1.9));
            mStatistics.Add(new Tuple<ulong, double>(4, 6));
            mStatistics.Add(new Tuple<ulong, double>(5, 1.2));
            mStatistics.Add(new Tuple<ulong, double>(6, 0.9));
            mStatistics.Add(new Tuple<ulong, double>(7, 12.3));
            mStatistics.Add(new Tuple<ulong, double>(8, 5));
            mStatistics.Add(new Tuple<ulong, double>(9, 6));
            #endregion design data

        }

        private BindingList<Tuple<ulong, double>> mStatistics = new BindingList<Tuple<ulong, double>>();

        public BindingList<Tuple<ulong, double>> Statistics
        {
            get
            {
                return mStatistics;
            }
        }

        private ConcurrentQueue<Tuple<ulong, double>> mQueuedStats = new ConcurrentQueue<Tuple<ulong, double>>();

        private void btnStart_Click(object sender, EventArgs e)
        {
            mStatistics.Clear();
            while (!mQueuedStats.IsEmpty)
            {
                Tuple<ulong, double> xItem;
                mQueuedStats.TryDequeue(out xItem);
            }

            backgroundWorker.RunWorkerAsync(Tuple.Create(tboxDir.Text, cboxForceCreate.Checked));
            btnCancel.Enabled = true;
            //mStatistics.Add(new Tuple<ulong, ulong, double>((ulong)mStatistics.Count, 38, 3));
            //chart1.DataBind();
        }

        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancel.Enabled = false;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            backgroundWorker.CancelAsync();
        }

        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            Tuple<ulong, double> xStat;
            while (mQueuedStats.TryDequeue(out xStat))
            {
                mStatistics.Add(xStat);
                if (mStatistics.Count >= MaxStats)
                {
                    mStatistics.RemoveAt(0);
                }
            }

            chart1.DataBind();
        }
    }
}
