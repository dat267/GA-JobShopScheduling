using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Scheduling
{
    public partial class MainForm : Form
    {

        #region --------Variables--------

        InputBoxProvider provider = new InputBoxProvider();
        Panel boxPanel;
        Random rnd;
        int mac, proc, job;
        bool best;
        GeneticMachine genetik;
        #endregion

        #region --------Related to Form--------

        public MainForm()
        {
            InitializeComponent();
            btnPrepare.Click += btnPrepare_Click;
            btnHideBoxes.Click += btnHideBoxes_Click;
            btnStart.Click += btnStart_Click;
            btnStop.Click += btnStop_Click;
            btnHideRes.Click += btnHideRes_Click;
            panel1.Paint += panel1_Paint;
            btnClearBoxes.Click += btnClearBoxes_Click;
            btnRandom.Click += btnRandom_Click;
            btnSampleData.Click += btnSampleData_Click;
            btnBigSample.Click += btnBigSample_Click;
            this.SizeChanged += MainForm_SizeChanged;
            btnExport.Click += btnExport_Click;
            btnStop.Enabled = false;
            resPanel.VisibleChanged += resPanel_VisibleChanged;
            rnd = new Random();
            SetDoubleBuffered(resPanel);
            SetDoubleBuffered(panel1);
            cbCOTypes.SelectedIndex = 2;
            cbSelTypes.SelectedIndex = 0;
            cbMutTypes.SelectedIndex = 0;
            resPanel.AutoScroll = true;
            resPanel.MouseEnter += resPanel_MouseEnter;
            resPanel.Height = this.Height - btnHideRes.Top - 40;
            this.Load += MainForm_Load;
            btnExportXml.Click += btnExportXml_Click;
            btnLoadXml.Click += btnLoadXml_Click;
            btnSelectPath.Click += btnSelectPath_Click;
        }

        void btnSelectPath_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                textBox1.Text = openFileDialog.FileName;
        }

        void btnLoadXml_Click(object sender, EventArgs e)
        {
            ImportDataFromXml(textBox1.Text);
        }

        void btnExportXml_Click(object sender, EventArgs e)
        {
            ExportDataToXML(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\sch_data.xml");
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            btnPrepare.PerformClick();
        }


        void chkFuzzy_CheckedChanged(object sender, EventArgs e)
        {
            btnPrepare.PerformClick();
        }

        void btnExport_Click(object sender, EventArgs e)
        {
            if (genetik != null && !genetik.Stopped)
                return;
            this.ExportMathModel();
        }

        void resPanel_MouseEnter(object sender, EventArgs e)
        {
            resPanel.Focus();
        }


        void btnSampleData_Click(object sender, EventArgs e)
        {
            nmudjob.Value = 6;
            nmudproc.Value = 6;
            nmudmac.Value = 6;
            createSampleData();
        }

        void btnBigSample_Click(object sender, EventArgs e)
        {
            nmudjob.Value = 10;
            nmudproc.Value = 10;
            nmudmac.Value = 10;
            createBigSample();
        }

        void MainForm_SizeChanged(object sender, EventArgs e)
        {
            if (boxPanel != null)
            {
                boxPanel.Width = this.Width - boxPanel.Left - 40;
            }
            if (resPanel != null)
            {
                if (boxPanel != null)
                    resPanel.Width = boxPanel.Width;
                resPanel.Height = this.Height - btnHideRes.Top - 60;
            }
        }
        #endregion

        #region --------Visibility--------

        void resPanel_VisibleChanged(object sender, EventArgs e)
        {
            btnHideRes.Text = resPanel.Visible ? "<<" : ">>";
        }
        void boxPanel_VisibleChanged(object sender, EventArgs e)
        {
            btnHideBoxes.Text = boxPanel.Visible ? "<<" : ">>";
        }

        #endregion

        #region --------Paint--------

        void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (genetik != null && genetik.Best != null)
            {
                genetik.Best.DrawSchedule(e.Graphics, panel1);
            }
        }
        #endregion

        #region --------Click Events--------
        void btnRandom_Click(object sender, EventArgs e)
        {
            for (int i = 1; i < mac + 1; i++)
            {
                for (int j = 1; j < proc + 1; j++)
                {
                    for (int k = 1; k < job + 1; k++)
                    {
                        string name = "j" + k + "p" + j + "m" + i;
                        int a = rnd.Next(0, 151);
                        boxPanel.Controls["j" + k].Controls["boxer"].Controls[name].Text = (a < 20 ? "X" : a.ToString());
                    }
                }
            }
        }
        void btnClearBoxes_Click(object sender, EventArgs e)
        {
            for (int i = 1; i < mac + 1; i++)
            {
                for (int j = 1; j < proc + 1; j++)
                {
                    for (int k = 1; k < job + 1; k++)
                    {
                        string name = "j" + k + "p" + j + "m" + i;
                        boxPanel.Controls["j" + k].Controls["boxer"].Controls[name].Text = "";
                    }
                }
            }
        }
        void btnHideRes_Click(object sender, EventArgs e)
        {
            resPanel.Visible = !resPanel.Visible && best;
        }

        void btnStop_Click(object sender, EventArgs e)
        {
            genetik.Stop();
            btnExport.Enabled = true;
            btnStop.Enabled = false;
        }
        Stopwatch stp = new Stopwatch();
        void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btnExport.Enabled = false;
                Data.DataTable = getDatas();
                Colors.GenerateRandomHSV(job);
                if (genetik != null && !genetik.Stopped)
                {
                    genetik.Stop();
                    Thread.Sleep(100);
                }

                int popSize = popnmud.Value.ToInt();
                genetik = new GeneticMachine(job, proc, mac, popSize);

                genetik.MutOdd = mutnmud.Value.ToInt();
                genetik.GroupSize = groupnmud.Value.ToInt();

                genetik.SelectionType = (SelectionTypes)cbSelTypes.SelectedIndex;
                genetik.CrossOver = (COTypes)cbCOTypes.SelectedIndex;
                genetik.MutationTypes = (MutationTypes)cbMutTypes.SelectedIndex;

                genetik.BestValueChanged += genetik_BestValueChanged;
                genetik.ProgressChanged += genetik_ProgressChanged;
                best = true;
                resPanel.Visible = true;
                resPanel.Width = boxPanel.Width;
                resPanel.Height = this.Height - btnHideRes.Top - 60;
                boxPanel.Visible = false;
                stp.Reset();
                stp.Start();
                genetik.Start();
                btnStop.Enabled = true;
                btnHideRes.Text = "<<";
            }
            catch
            {
                MessageBox.Show("Data table is wrong");
            }
        }
        void btnHideBoxes_Click(object sender, EventArgs e)
        {
            if (boxPanel != null)
                boxPanel.Visible = !boxPanel.Visible;
        }

        void btnPrepare_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            this.Controls.Remove(boxPanel);
            if (boxPanel != null)
                boxPanel.Dispose();

            mac = nmudmac.Value.ToInt();
            proc = nmudproc.Value.ToInt();
            job = nmudjob.Value.ToInt();

            boxPanel = provider.CreateJobBoxes(mac, proc, job);
            Application.DoEvents();
            boxPanel.Left = btnHideBoxes.Right + 10;
            boxPanel.Top = btnHideBoxes.Top;
            boxPanel.Width = this.Width - boxPanel.Left - 40;
            boxPanel.AutoScroll = false;
            boxPanel.VerticalScroll.Enabled = false;
            boxPanel.VerticalScroll.Visible = false;
            boxPanel.VerticalScroll.Maximum = 0;
            boxPanel.AutoScroll = true;

            boxPanel.VisibleChanged += boxPanel_VisibleChanged;
            this.Controls.Add(boxPanel);
            SetDoubleBuffered(boxPanel);
            resPanel.Visible = false;
            Cursor = Cursors.Arrow;
        }
        #endregion

        #region --------Genetic Events--------

        void genetik_ProgressChanged(object sender, EventArgs e)
        {
            if (genetik != null)
                lblProgress.Text = "%" + genetik.Progress.ToString("0.00");
        }
        //296
        void genetik_BestValueChanged(object sender, EventArgs e)
        {
            if (genetik.Best != null)
            {
                lblSpan.Text = genetik.Best.MakeSpan().ToString();
                lblBestTime.Text = stp.Elapsed.TotalSeconds.ToString("00") + "." + stp.Elapsed.Milliseconds.ToString("00") + " sn";
                //                lblIdleTime.Text = genetik.Best.TotalIdleTime.ToString();
                resPanel.Invalidate();
            }
        }
        #endregion

        #region --------Methods--------

        float[,,] getDatas()
        {
            float[,,] vals = new float[job, proc, mac];
            for (int i = 1; i < mac + 1; i++)
            {
                for (int j = 1; j < proc + 1; j++)
                {
                    for (int k = 1; k < job + 1; k++)
                    {
                        float span = 0;
                        for (int z = 0; z < 3; z++)
                        {
                            int val = 0;
                            string text = getBoxControl(boxPanel, k, j, i).Text;
                            if (text.ToLower() == "x" || string.IsNullOrEmpty(text))
                                val = 10000;
                            else
                                val = text.ToInt();
                            span += val;
                        }
                        vals[k - 1, j - 1, i - 1] = span / 3;
                    }
                }
            }
            return vals;
        }
        Control getBoxControl(Panel boxContainer, int job, int proc, int mac)
        {
            string name = "j" + job + "p" + proc + "m" + mac;
            return boxContainer.Controls["j" + job].Controls["boxer"].Controls[name];
        }

        void createSampleData()
        {
            this.Controls.Remove(boxPanel);
            if (boxPanel != null)
                boxPanel.Dispose();

            mac = 6;
            proc = 6;
            job = 6;

            boxPanel = provider.CreateJobBoxes(mac, proc, job);
            boxPanel.Left = btnHideBoxes.Right + 10;
            boxPanel.Top = btnHideBoxes.Top;
            boxPanel.Width = this.Width - boxPanel.Left - 40;
            boxPanel.AutoScroll = false;
            boxPanel.VerticalScroll.Enabled = false;
            boxPanel.VerticalScroll.Visible = false;
            boxPanel.VerticalScroll.Maximum = 0;
            boxPanel.AutoScroll = true;

            boxPanel.VisibleChanged += boxPanel_VisibleChanged;
            this.Controls.Add(boxPanel);
            SetDoubleBuffered(boxPanel);
            resPanel.Visible = false;

            getBoxControl(boxPanel, 1, 1, 1).Text = "X";
            getBoxControl(boxPanel, 1, 1, 2).Text = "X";
            getBoxControl(boxPanel, 1, 1, 3).Text = "10";
            getBoxControl(boxPanel, 1, 1, 4).Text = "X";
            getBoxControl(boxPanel, 1, 1, 5).Text = "X";
            getBoxControl(boxPanel, 1, 1, 6).Text = "X";
            getBoxControl(boxPanel, 1, 2, 1).Text = "30";
            getBoxControl(boxPanel, 1, 2, 2).Text = "X";
            getBoxControl(boxPanel, 1, 2, 3).Text = "X";
            getBoxControl(boxPanel, 1, 2, 4).Text = "X";
            getBoxControl(boxPanel, 1, 2, 5).Text = "X";
            getBoxControl(boxPanel, 1, 2, 6).Text = "X";
            getBoxControl(boxPanel, 1, 3, 1).Text = "X";
            getBoxControl(boxPanel, 1, 3, 2).Text = "60";
            getBoxControl(boxPanel, 1, 3, 3).Text = "X";
            getBoxControl(boxPanel, 1, 3, 4).Text = "X";
            getBoxControl(boxPanel, 1, 3, 5).Text = "X";
            getBoxControl(boxPanel, 1, 3, 6).Text = "X";
            getBoxControl(boxPanel, 1, 4, 1).Text = "X";
            getBoxControl(boxPanel, 1, 4, 2).Text = "X";
            getBoxControl(boxPanel, 1, 4, 3).Text = "X";
            getBoxControl(boxPanel, 1, 4, 4).Text = "70";
            getBoxControl(boxPanel, 1, 4, 5).Text = "X";
            getBoxControl(boxPanel, 1, 4, 6).Text = "X";
            getBoxControl(boxPanel, 1, 5, 1).Text = "X";
            getBoxControl(boxPanel, 1, 5, 2).Text = "X";
            getBoxControl(boxPanel, 1, 5, 3).Text = "X";
            getBoxControl(boxPanel, 1, 5, 4).Text = "X";
            getBoxControl(boxPanel, 1, 5, 5).Text = "X";
            getBoxControl(boxPanel, 1, 5, 6).Text = "30";
            getBoxControl(boxPanel, 1, 6, 1).Text = "X";
            getBoxControl(boxPanel, 1, 6, 2).Text = "X";
            getBoxControl(boxPanel, 1, 6, 3).Text = "X";
            getBoxControl(boxPanel, 1, 6, 4).Text = "X";
            getBoxControl(boxPanel, 1, 6, 5).Text = "60";
            getBoxControl(boxPanel, 1, 6, 6).Text = "X";
            getBoxControl(boxPanel, 2, 1, 1).Text = "X";
            getBoxControl(boxPanel, 2, 1, 2).Text = "80";
            getBoxControl(boxPanel, 2, 1, 3).Text = "X";
            getBoxControl(boxPanel, 2, 1, 4).Text = "X";
            getBoxControl(boxPanel, 2, 1, 5).Text = "X";
            getBoxControl(boxPanel, 2, 1, 6).Text = "X";
            getBoxControl(boxPanel, 2, 2, 1).Text = "X";
            getBoxControl(boxPanel, 2, 2, 2).Text = "X";
            getBoxControl(boxPanel, 2, 2, 3).Text = "50";
            getBoxControl(boxPanel, 2, 2, 4).Text = "X";
            getBoxControl(boxPanel, 2, 2, 5).Text = "X";
            getBoxControl(boxPanel, 2, 2, 6).Text = "X";
            getBoxControl(boxPanel, 2, 3, 1).Text = "X";
            getBoxControl(boxPanel, 2, 3, 2).Text = "X";
            getBoxControl(boxPanel, 2, 3, 3).Text = "X";
            getBoxControl(boxPanel, 2, 3, 4).Text = "X";
            getBoxControl(boxPanel, 2, 3, 5).Text = "100";
            getBoxControl(boxPanel, 2, 3, 6).Text = "X";
            getBoxControl(boxPanel, 2, 4, 1).Text = "X";
            getBoxControl(boxPanel, 2, 4, 2).Text = "X";
            getBoxControl(boxPanel, 2, 4, 3).Text = "X";
            getBoxControl(boxPanel, 2, 4, 4).Text = "X";
            getBoxControl(boxPanel, 2, 4, 5).Text = "X";
            getBoxControl(boxPanel, 2, 4, 6).Text = "100";
            getBoxControl(boxPanel, 2, 5, 1).Text = "100";
            getBoxControl(boxPanel, 2, 5, 2).Text = "X";
            getBoxControl(boxPanel, 2, 5, 3).Text = "X";
            getBoxControl(boxPanel, 2, 5, 4).Text = "X";
            getBoxControl(boxPanel, 2, 5, 5).Text = "X";
            getBoxControl(boxPanel, 2, 5, 6).Text = "X";
            getBoxControl(boxPanel, 2, 6, 1).Text = "X";
            getBoxControl(boxPanel, 2, 6, 2).Text = "X";
            getBoxControl(boxPanel, 2, 6, 3).Text = "X";
            getBoxControl(boxPanel, 2, 6, 4).Text = "40";
            getBoxControl(boxPanel, 2, 6, 5).Text = "X";
            getBoxControl(boxPanel, 2, 6, 6).Text = "X";
            getBoxControl(boxPanel, 3, 1, 1).Text = "X";
            getBoxControl(boxPanel, 3, 1, 2).Text = "X";
            getBoxControl(boxPanel, 3, 1, 3).Text = "50";
            getBoxControl(boxPanel, 3, 1, 4).Text = "X";
            getBoxControl(boxPanel, 3, 1, 5).Text = "X";
            getBoxControl(boxPanel, 3, 1, 6).Text = "X";
            getBoxControl(boxPanel, 3, 2, 1).Text = "X";
            getBoxControl(boxPanel, 3, 2, 2).Text = "X";
            getBoxControl(boxPanel, 3, 2, 3).Text = "X";
            getBoxControl(boxPanel, 3, 2, 4).Text = "40";
            getBoxControl(boxPanel, 3, 2, 5).Text = "X";
            getBoxControl(boxPanel, 3, 2, 6).Text = "X";
            getBoxControl(boxPanel, 3, 3, 1).Text = "X";
            getBoxControl(boxPanel, 3, 3, 2).Text = "X";
            getBoxControl(boxPanel, 3, 3, 3).Text = "X";
            getBoxControl(boxPanel, 3, 3, 4).Text = "X";
            getBoxControl(boxPanel, 3, 3, 5).Text = "X";
            getBoxControl(boxPanel, 3, 3, 6).Text = "80";
            getBoxControl(boxPanel, 3, 4, 1).Text = "90";
            getBoxControl(boxPanel, 3, 4, 2).Text = "X";
            getBoxControl(boxPanel, 3, 4, 3).Text = "X";
            getBoxControl(boxPanel, 3, 4, 4).Text = "X";
            getBoxControl(boxPanel, 3, 4, 5).Text = "X";
            getBoxControl(boxPanel, 3, 4, 6).Text = "X";
            getBoxControl(boxPanel, 3, 5, 1).Text = "X";
            getBoxControl(boxPanel, 3, 5, 2).Text = "10";
            getBoxControl(boxPanel, 3, 5, 3).Text = "X";
            getBoxControl(boxPanel, 3, 5, 4).Text = "X";
            getBoxControl(boxPanel, 3, 5, 5).Text = "X";
            getBoxControl(boxPanel, 3, 5, 6).Text = "X";
            getBoxControl(boxPanel, 3, 6, 1).Text = "X";
            getBoxControl(boxPanel, 3, 6, 2).Text = "X";
            getBoxControl(boxPanel, 3, 6, 3).Text = "X";
            getBoxControl(boxPanel, 3, 6, 4).Text = "X";
            getBoxControl(boxPanel, 3, 6, 5).Text = "70";
            getBoxControl(boxPanel, 3, 6, 6).Text = "X";
            getBoxControl(boxPanel, 4, 1, 1).Text = "X";
            getBoxControl(boxPanel, 4, 1, 2).Text = "50";
            getBoxControl(boxPanel, 4, 1, 3).Text = "X";
            getBoxControl(boxPanel, 4, 1, 4).Text = "X";
            getBoxControl(boxPanel, 4, 1, 5).Text = "X";
            getBoxControl(boxPanel, 4, 1, 6).Text = "X";
            getBoxControl(boxPanel, 4, 2, 1).Text = "50";
            getBoxControl(boxPanel, 4, 2, 2).Text = "X";
            getBoxControl(boxPanel, 4, 2, 3).Text = "X";
            getBoxControl(boxPanel, 4, 2, 4).Text = "X";
            getBoxControl(boxPanel, 4, 2, 5).Text = "X";
            getBoxControl(boxPanel, 4, 2, 6).Text = "X";
            getBoxControl(boxPanel, 4, 3, 1).Text = "X";
            getBoxControl(boxPanel, 4, 3, 2).Text = "X";
            getBoxControl(boxPanel, 4, 3, 3).Text = "50";
            getBoxControl(boxPanel, 4, 3, 4).Text = "X";
            getBoxControl(boxPanel, 4, 3, 5).Text = "X";
            getBoxControl(boxPanel, 4, 3, 6).Text = "X";
            getBoxControl(boxPanel, 4, 4, 1).Text = "X";
            getBoxControl(boxPanel, 4, 4, 2).Text = "X";
            getBoxControl(boxPanel, 4, 4, 3).Text = "X";
            getBoxControl(boxPanel, 4, 4, 4).Text = "30";
            getBoxControl(boxPanel, 4, 4, 5).Text = "X";
            getBoxControl(boxPanel, 4, 4, 6).Text = "X";
            getBoxControl(boxPanel, 4, 5, 1).Text = "X";
            getBoxControl(boxPanel, 4, 5, 2).Text = "X";
            getBoxControl(boxPanel, 4, 5, 3).Text = "X";
            getBoxControl(boxPanel, 4, 5, 4).Text = "X";
            getBoxControl(boxPanel, 4, 5, 5).Text = "80";
            getBoxControl(boxPanel, 4, 5, 6).Text = "X";
            getBoxControl(boxPanel, 4, 6, 1).Text = "X";
            getBoxControl(boxPanel, 4, 6, 2).Text = "X";
            getBoxControl(boxPanel, 4, 6, 3).Text = "X";
            getBoxControl(boxPanel, 4, 6, 4).Text = "X";
            getBoxControl(boxPanel, 4, 6, 5).Text = "X";
            getBoxControl(boxPanel, 4, 6, 6).Text = "90";
            getBoxControl(boxPanel, 5, 1, 1).Text = "X";
            getBoxControl(boxPanel, 5, 1, 2).Text = "X";
            getBoxControl(boxPanel, 5, 1, 3).Text = "90";
            getBoxControl(boxPanel, 5, 1, 4).Text = "X";
            getBoxControl(boxPanel, 5, 1, 5).Text = "X";
            getBoxControl(boxPanel, 5, 1, 6).Text = "X";
            getBoxControl(boxPanel, 5, 2, 1).Text = "X";
            getBoxControl(boxPanel, 5, 2, 2).Text = "30";
            getBoxControl(boxPanel, 5, 2, 3).Text = "X";
            getBoxControl(boxPanel, 5, 2, 4).Text = "X";
            getBoxControl(boxPanel, 5, 2, 5).Text = "X";
            getBoxControl(boxPanel, 5, 2, 6).Text = "X";
            getBoxControl(boxPanel, 5, 3, 1).Text = "X";
            getBoxControl(boxPanel, 5, 3, 2).Text = "X";
            getBoxControl(boxPanel, 5, 3, 3).Text = "X";
            getBoxControl(boxPanel, 5, 3, 4).Text = "X";
            getBoxControl(boxPanel, 5, 3, 5).Text = "50";
            getBoxControl(boxPanel, 5, 3, 6).Text = "X";
            getBoxControl(boxPanel, 5, 4, 1).Text = "X";
            getBoxControl(boxPanel, 5, 4, 2).Text = "X";
            getBoxControl(boxPanel, 5, 4, 3).Text = "X";
            getBoxControl(boxPanel, 5, 4, 4).Text = "X";
            getBoxControl(boxPanel, 5, 4, 5).Text = "X";
            getBoxControl(boxPanel, 5, 4, 6).Text = "40";
            getBoxControl(boxPanel, 5, 5, 1).Text = "30";
            getBoxControl(boxPanel, 5, 5, 2).Text = "X";
            getBoxControl(boxPanel, 5, 5, 3).Text = "X";
            getBoxControl(boxPanel, 5, 5, 4).Text = "X";
            getBoxControl(boxPanel, 5, 5, 5).Text = "X";
            getBoxControl(boxPanel, 5, 5, 6).Text = "X";
            getBoxControl(boxPanel, 5, 6, 1).Text = "X";
            getBoxControl(boxPanel, 5, 6, 2).Text = "X";
            getBoxControl(boxPanel, 5, 6, 3).Text = "X";
            getBoxControl(boxPanel, 5, 6, 4).Text = "10";
            getBoxControl(boxPanel, 5, 6, 5).Text = "X";
            getBoxControl(boxPanel, 5, 6, 6).Text = "X";
            getBoxControl(boxPanel, 6, 1, 1).Text = "X";
            getBoxControl(boxPanel, 6, 1, 2).Text = "30";
            getBoxControl(boxPanel, 6, 1, 3).Text = "X";
            getBoxControl(boxPanel, 6, 1, 4).Text = "X";
            getBoxControl(boxPanel, 6, 1, 5).Text = "X";
            getBoxControl(boxPanel, 6, 1, 6).Text = "X";
            getBoxControl(boxPanel, 6, 2, 1).Text = "X";
            getBoxControl(boxPanel, 6, 2, 2).Text = "X";
            getBoxControl(boxPanel, 6, 2, 3).Text = "X";
            getBoxControl(boxPanel, 6, 2, 4).Text = "30";
            getBoxControl(boxPanel, 6, 2, 5).Text = "X";
            getBoxControl(boxPanel, 6, 2, 6).Text = "X";
            getBoxControl(boxPanel, 6, 3, 1).Text = "X";
            getBoxControl(boxPanel, 6, 3, 2).Text = "X";
            getBoxControl(boxPanel, 6, 3, 3).Text = "X";
            getBoxControl(boxPanel, 6, 3, 4).Text = "X";
            getBoxControl(boxPanel, 6, 3, 5).Text = "X";
            getBoxControl(boxPanel, 6, 3, 6).Text = "90";
            getBoxControl(boxPanel, 6, 4, 1).Text = "100";
            getBoxControl(boxPanel, 6, 4, 2).Text = "X";
            getBoxControl(boxPanel, 6, 4, 3).Text = "X";
            getBoxControl(boxPanel, 6, 4, 4).Text = "X";
            getBoxControl(boxPanel, 6, 4, 5).Text = "X";
            getBoxControl(boxPanel, 6, 4, 6).Text = "X";
            getBoxControl(boxPanel, 6, 5, 1).Text = "X";
            getBoxControl(boxPanel, 6, 5, 2).Text = "X";
            getBoxControl(boxPanel, 6, 5, 3).Text = "X";
            getBoxControl(boxPanel, 6, 5, 4).Text = "X";
            getBoxControl(boxPanel, 6, 5, 5).Text = "40";
            getBoxControl(boxPanel, 6, 5, 6).Text = "X";
            getBoxControl(boxPanel, 6, 6, 1).Text = "X";
            getBoxControl(boxPanel, 6, 6, 2).Text = "X";
            getBoxControl(boxPanel, 6, 6, 3).Text = "10";
            getBoxControl(boxPanel, 6, 6, 4).Text = "X";
            getBoxControl(boxPanel, 6, 6, 5).Text = "X";
            getBoxControl(boxPanel, 6, 6, 6).Text = "X";

            boxPanel.VisibleChanged += boxPanel_VisibleChanged;
            this.Controls.Add(boxPanel);
            SetDoubleBuffered(boxPanel);
            resPanel.Visible = false;
        }
        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
        {
            //Taxes: Remote Desktop Connection and painting
            //http://blogs.msdn.com/oldnewthing/archive/2006/01/03/508694.aspx
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
                return;

            System.Reflection.PropertyInfo aProp =
                  typeof(System.Windows.Forms.Control).GetProperty(
                        "DoubleBuffered",
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

            aProp.SetValue(c, true, null);
        }

        string exportableDataText()
        {
            float[,,] data = getDatas();
            int tj = data.GetLength(0);
            int tp = data.GetLength(1);
            int tm = data.GetLength(2);

            string text = "";
            for (int j = 0; j < tp; j++)
            {
                for (int k = 0; k < tm; k++)
                {
                    text += space(8) + (j + 1) + "." + (k + 1);
                }
            }
            text += Environment.NewLine;
            for (int i = 0; i < tj; i++)
            {
                text += (i + 1).ToString() + space(8);
                for (int j = 0; j < tp; j++)
                {
                    for (int k = 0; k < tm; k++)
                    {
                        string me = data[i, j, k].ToString();
                        text += data[i, j, k] + space(8 - (me.Length - 3));
                    }
                }
                text += i == tj - 1 ? ";" : Environment.NewLine;
            }
            return text;
        }

        void ExportMathModel()
        {

            #region mathematical model

            string model =
"sets" + Environment.NewLine +
"i jobs           /1*" + job + "/" + Environment.NewLine +
"j processes      /1*" + proc + "/" + Environment.NewLine +
"k machines       /1*" + mac + "/" + Environment.NewLine +
"o orders         /1*" + job * proc + "/" + Environment.NewLine + Environment.NewLine +

"**kumelerin kopyasini al\r\n" +
"alias(i,ii);" + Environment.NewLine +
"alias(j,jj);" + Environment.NewLine +
"alias(k,kk);" + Environment.NewLine +
"alias(o,oo);\r\n" + Environment.NewLine +

"table t(i,j,k) veri tablosu\r\n" + Environment.NewLine +
exportableDataText() + Environment.NewLine + "\r\n" +
"parameter L buyuk sayi /100000/\r\n" + Environment.NewLine +

"binary variables" + Environment.NewLine +
"x(i,j,k) is-process ikilisinin makineye atanma durumu" + Environment.NewLine +
"a(i,j,o) is-process ikilisinin is siralamasindaki konumunu belirten ikili degisken;\r\n" + Environment.NewLine +

"positive variables" + Environment.NewLine +
"s(i,j,k) is-proses ikilisinin k makinesinde calisma zamani: x(i.j.k) = 0 ise s(i.j.k) = 0)" + Environment.NewLine +
"c(i,j,k) is-proses ikilisinin k makinesinde bitis zamani: x(i.j.k) = 0 ise c(i.j.k) = 0;\r\n" + Environment.NewLine +
"variables" + Environment.NewLine +
"z amac fonksiyonu;\r\n" + Environment.NewLine +

"equations" + Environment.NewLine +

"eq1 her is-proses ikilisi bir makineye atanmali" + Environment.NewLine +
"eq2 her is-proses ikilisi bir makineye atanmali " + Environment.NewLine +
"eq3 her sira numarasi sadece bir is-proses ikilisine ait olmali" + Environment.NewLine +
"eq4 proses bitis zamani = baslangic zamani + calisma zamani" + Environment.NewLine +
"eq5 is-proses k makinesinde calismiyorsa s(i.j.k) = c(i.j.k) = 0" + Environment.NewLine +
"eq6 i-j ve ii-jj ayni makinede calisiyorsa ve i-j nin sira numarasi daha buyukse s(i.j.k) >= c(ii.jj.k)" + Environment.NewLine +
"eq7 i-j nin bir onceki prosesinin sira numarasi i-j ninkinden buyuk olamaz" + Environment.NewLine +
"eq8 i-j nin baslama zamani bir onceki prosesinin bitisinden buyuktur" + Environment.NewLine +
"eq9 makespan: tum i-j lerin bitis zamanindan buyuk yada esittir;\r\n" + Environment.NewLine +


"eq1(i,j)..                                       sum(k, x(i,j,k)) =e= 1;" + Environment.NewLine +

"eq2(i,j)..                                       sum(o, a(i,j,o)) =e= 1;" + Environment.NewLine +

"eq3(o)..                                         sum((i,j), a(i,j,o)) =e= 1;" + Environment.NewLine +

"eq4(i,j,k)..                                     c(i,j,k) =g= s(i,j,k) + t(i,j,k) - (1 - x(i,j,k)) * L;" + Environment.NewLine +

"eq5(i,j,k)..                                     s(i,j,k) + c(i,j,k) =l= L * x(i,j,k);" + Environment.NewLine +

"eq6(i,j,o,k,ii,jj,oo)$(ord(oo) lt ord(o))..      s(i,j,k) + L * (4 - a(i,j,o) - a(ii,jj,oo) - x(i,j,k) - x(ii,jj,k)) =g= c(ii,jj,k);" + Environment.NewLine +

"eq7(i,j,o)$(ord(j) gt 1)..                       sum(oo$(ord(oo) ge ord(o)), a(i,j-1,oo)) =l= 1 - a(i,j,o);" + Environment.NewLine +

"eq8(i,j)$(ord(j) gt 1)..                         sum(k, s(i,j,k)) =g= sum(k, c(i,j-1,k));" + Environment.NewLine +

"eq9(i,j)$(ord(j) eq card(j))..                   z =g= sum(k, c(i,j,k));\r\n" + Environment.NewLine +

"model ciz /all/;" + Environment.NewLine +
"option optcr = 0;" + Environment.NewLine +
"solve ciz using mip min z;" + Environment.NewLine +
"display x.l, z.l,a.l,c.l,s.l;";
            #endregion

            string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\scheduling_model.gms";
            TextWriter writer = new StreamWriter(path);
            writer.WriteLine(model);
            writer.Close();
            Process.Start(path);
            MessageBox.Show("Model file has been saved on desktop: scheduling_model.gms");
        }
        static string space(int n)
        {
            string text = "";
            for (int i = 0; i < n; i++)
            {
                text += " ";
            }
            return text;
        }
        #endregion

        void ExportDataToXML(string destpath)
        {
            XDocument doc = new XDocument();
            XElement root = new XElement("data");
            Data.DataTable = getDatas();
            int job = Data.DataTable.GetLength(0);
            int mac = Data.DataTable.GetLength(1);
            int proc = Data.DataTable.GetLength(2);

            for (int i = 0; i < job; i++)
            {
                XElement jobEl = new XElement("job");
                jobEl.Add(new XAttribute("id", i + 1));
                for (int j = 0; j < mac; j++)
                {
                    XElement procEl = new XElement("process");
                    procEl.Add(new XAttribute("id", j + 1));
                    for (int k = 0; k < proc; k++)
                    {
                        XElement macEl = new XElement("machine");
                        macEl.Add(new XAttribute("id", k + 1));
                        macEl.Add(new XAttribute("time", Data.DataTable[i, j, k] == 10000 ? -1 : Data.DataTable[i, j, k]));
                        procEl.Add(macEl);
                    }
                    jobEl.Add(procEl);
                }
                root.Add(jobEl);
            }
            doc.Add(root);
            doc.Save(destpath);
            MessageBox.Show("File has been created\r\n\r\n" + destpath);
        }

        private void createBigSample()
        {
            this.Controls.Remove(boxPanel);
            if (boxPanel != null)
                boxPanel.Dispose();

            mac = 10;
            proc = 10;
            job = 10;

            boxPanel = provider.CreateJobBoxes(mac, proc, job);
            boxPanel.Left = btnHideBoxes.Right + 10;
            boxPanel.Top = btnHideBoxes.Top;
            boxPanel.Width = this.Width - boxPanel.Left - 40;
            boxPanel.AutoScroll = false;
            boxPanel.VerticalScroll.Enabled = false;
            boxPanel.VerticalScroll.Visible = false;
            boxPanel.VerticalScroll.Maximum = 0;
            boxPanel.AutoScroll = true;

            boxPanel.VisibleChanged += boxPanel_VisibleChanged;
            this.Controls.Add(boxPanel);
            SetDoubleBuffered(boxPanel);
            resPanel.Visible = false;

            getBoxControl(boxPanel, 1, 1, 1).Text = "X";
            getBoxControl(boxPanel, 1, 1, 2).Text = "X";
            getBoxControl(boxPanel, 1, 1, 3).Text = "X";
            getBoxControl(boxPanel, 1, 1, 4).Text = "X";
            getBoxControl(boxPanel, 1, 1, 5).Text = "88";
            getBoxControl(boxPanel, 1, 1, 6).Text = "X";
            getBoxControl(boxPanel, 1, 1, 7).Text = "X";
            getBoxControl(boxPanel, 1, 1, 8).Text = "X";
            getBoxControl(boxPanel, 1, 1, 9).Text = "X";
            getBoxControl(boxPanel, 1, 1, 10).Text = "X";
            getBoxControl(boxPanel, 1, 2, 1).Text = "X";
            getBoxControl(boxPanel, 1, 2, 2).Text = "X";
            getBoxControl(boxPanel, 1, 2, 3).Text = "X";
            getBoxControl(boxPanel, 1, 2, 4).Text = "X";
            getBoxControl(boxPanel, 1, 2, 5).Text = "X";
            getBoxControl(boxPanel, 1, 2, 6).Text = "X";
            getBoxControl(boxPanel, 1, 2, 7).Text = "X";
            getBoxControl(boxPanel, 1, 2, 8).Text = "X";
            getBoxControl(boxPanel, 1, 2, 9).Text = "68";
            getBoxControl(boxPanel, 1, 2, 10).Text = "X";
            getBoxControl(boxPanel, 1, 3, 1).Text = "X";
            getBoxControl(boxPanel, 1, 3, 2).Text = "X";
            getBoxControl(boxPanel, 1, 3, 3).Text = "X";
            getBoxControl(boxPanel, 1, 3, 4).Text = "X";
            getBoxControl(boxPanel, 1, 3, 5).Text = "X";
            getBoxControl(boxPanel, 1, 3, 6).Text = "X";
            getBoxControl(boxPanel, 1, 3, 7).Text = "94";
            getBoxControl(boxPanel, 1, 3, 8).Text = "X";
            getBoxControl(boxPanel, 1, 3, 9).Text = "X";
            getBoxControl(boxPanel, 1, 3, 10).Text = "X";
            getBoxControl(boxPanel, 1, 4, 1).Text = "X";
            getBoxControl(boxPanel, 1, 4, 2).Text = "X";
            getBoxControl(boxPanel, 1, 4, 3).Text = "X";
            getBoxControl(boxPanel, 1, 4, 4).Text = "X";
            getBoxControl(boxPanel, 1, 4, 5).Text = "X";
            getBoxControl(boxPanel, 1, 4, 6).Text = "99";
            getBoxControl(boxPanel, 1, 4, 7).Text = "X";
            getBoxControl(boxPanel, 1, 4, 8).Text = "X";
            getBoxControl(boxPanel, 1, 4, 9).Text = "X";
            getBoxControl(boxPanel, 1, 4, 10).Text = "X";
            getBoxControl(boxPanel, 1, 5, 1).Text = "X";
            getBoxControl(boxPanel, 1, 5, 2).Text = "67";
            getBoxControl(boxPanel, 1, 5, 3).Text = "X";
            getBoxControl(boxPanel, 1, 5, 4).Text = "X";
            getBoxControl(boxPanel, 1, 5, 5).Text = "X";
            getBoxControl(boxPanel, 1, 5, 6).Text = "X";
            getBoxControl(boxPanel, 1, 5, 7).Text = "X";
            getBoxControl(boxPanel, 1, 5, 8).Text = "X";
            getBoxControl(boxPanel, 1, 5, 9).Text = "X";
            getBoxControl(boxPanel, 1, 5, 10).Text = "X";
            getBoxControl(boxPanel, 1, 6, 1).Text = "X";
            getBoxControl(boxPanel, 1, 6, 2).Text = "X";
            getBoxControl(boxPanel, 1, 6, 3).Text = "89";
            getBoxControl(boxPanel, 1, 6, 4).Text = "X";
            getBoxControl(boxPanel, 1, 6, 5).Text = "X";
            getBoxControl(boxPanel, 1, 6, 6).Text = "X";
            getBoxControl(boxPanel, 1, 6, 7).Text = "X";
            getBoxControl(boxPanel, 1, 6, 8).Text = "X";
            getBoxControl(boxPanel, 1, 6, 9).Text = "X";
            getBoxControl(boxPanel, 1, 6, 10).Text = "X";
            getBoxControl(boxPanel, 1, 7, 1).Text = "X";
            getBoxControl(boxPanel, 1, 7, 2).Text = "X";
            getBoxControl(boxPanel, 1, 7, 3).Text = "X";
            getBoxControl(boxPanel, 1, 7, 4).Text = "X";
            getBoxControl(boxPanel, 1, 7, 5).Text = "X";
            getBoxControl(boxPanel, 1, 7, 6).Text = "X";
            getBoxControl(boxPanel, 1, 7, 7).Text = "X";
            getBoxControl(boxPanel, 1, 7, 8).Text = "X";
            getBoxControl(boxPanel, 1, 7, 9).Text = "X";
            getBoxControl(boxPanel, 1, 7, 10).Text = "77";
            getBoxControl(boxPanel, 1, 8, 1).Text = "X";
            getBoxControl(boxPanel, 1, 8, 2).Text = "X";
            getBoxControl(boxPanel, 1, 8, 3).Text = "X";
            getBoxControl(boxPanel, 1, 8, 4).Text = "X";
            getBoxControl(boxPanel, 1, 8, 5).Text = "X";
            getBoxControl(boxPanel, 1, 8, 6).Text = "X";
            getBoxControl(boxPanel, 1, 8, 7).Text = "X";
            getBoxControl(boxPanel, 1, 8, 8).Text = "99";
            getBoxControl(boxPanel, 1, 8, 9).Text = "X";
            getBoxControl(boxPanel, 1, 8, 10).Text = "X";
            getBoxControl(boxPanel, 1, 9, 1).Text = "86";
            getBoxControl(boxPanel, 1, 9, 2).Text = "X";
            getBoxControl(boxPanel, 1, 9, 3).Text = "X";
            getBoxControl(boxPanel, 1, 9, 4).Text = "X";
            getBoxControl(boxPanel, 1, 9, 5).Text = "X";
            getBoxControl(boxPanel, 1, 9, 6).Text = "X";
            getBoxControl(boxPanel, 1, 9, 7).Text = "X";
            getBoxControl(boxPanel, 1, 9, 8).Text = "X";
            getBoxControl(boxPanel, 1, 9, 9).Text = "X";
            getBoxControl(boxPanel, 1, 9, 10).Text = "X";
            getBoxControl(boxPanel, 1, 10, 1).Text = "X";
            getBoxControl(boxPanel, 1, 10, 2).Text = "X";
            getBoxControl(boxPanel, 1, 10, 3).Text = "X";
            getBoxControl(boxPanel, 1, 10, 4).Text = "92";
            getBoxControl(boxPanel, 1, 10, 5).Text = "X";
            getBoxControl(boxPanel, 1, 10, 6).Text = "X";
            getBoxControl(boxPanel, 1, 10, 7).Text = "X";
            getBoxControl(boxPanel, 1, 10, 8).Text = "X";
            getBoxControl(boxPanel, 1, 10, 9).Text = "X";
            getBoxControl(boxPanel, 1, 10, 10).Text = "X";
            getBoxControl(boxPanel, 2, 1, 1).Text = "X";
            getBoxControl(boxPanel, 2, 1, 2).Text = "X";
            getBoxControl(boxPanel, 2, 1, 3).Text = "X";
            getBoxControl(boxPanel, 2, 1, 4).Text = "X";
            getBoxControl(boxPanel, 2, 1, 5).Text = "X";
            getBoxControl(boxPanel, 2, 1, 6).Text = "72";
            getBoxControl(boxPanel, 2, 1, 7).Text = "X";
            getBoxControl(boxPanel, 2, 1, 8).Text = "X";
            getBoxControl(boxPanel, 2, 1, 9).Text = "X";
            getBoxControl(boxPanel, 2, 1, 10).Text = "X";
            getBoxControl(boxPanel, 2, 2, 1).Text = "X";
            getBoxControl(boxPanel, 2, 2, 2).Text = "X";
            getBoxControl(boxPanel, 2, 2, 3).Text = "X";
            getBoxControl(boxPanel, 2, 2, 4).Text = "50";
            getBoxControl(boxPanel, 2, 2, 5).Text = "X";
            getBoxControl(boxPanel, 2, 2, 6).Text = "X";
            getBoxControl(boxPanel, 2, 2, 7).Text = "X";
            getBoxControl(boxPanel, 2, 2, 8).Text = "X";
            getBoxControl(boxPanel, 2, 2, 9).Text = "X";
            getBoxControl(boxPanel, 2, 2, 10).Text = "X";
            getBoxControl(boxPanel, 2, 3, 1).Text = "X";
            getBoxControl(boxPanel, 2, 3, 2).Text = "X";
            getBoxControl(boxPanel, 2, 3, 3).Text = "X";
            getBoxControl(boxPanel, 2, 3, 4).Text = "X";
            getBoxControl(boxPanel, 2, 3, 5).Text = "X";
            getBoxControl(boxPanel, 2, 3, 6).Text = "X";
            getBoxControl(boxPanel, 2, 3, 7).Text = "69";
            getBoxControl(boxPanel, 2, 3, 8).Text = "X";
            getBoxControl(boxPanel, 2, 3, 9).Text = "X";
            getBoxControl(boxPanel, 2, 3, 10).Text = "X";
            getBoxControl(boxPanel, 2, 4, 1).Text = "X";
            getBoxControl(boxPanel, 2, 4, 2).Text = "X";
            getBoxControl(boxPanel, 2, 4, 3).Text = "X";
            getBoxControl(boxPanel, 2, 4, 4).Text = "X";
            getBoxControl(boxPanel, 2, 4, 5).Text = "75";
            getBoxControl(boxPanel, 2, 4, 6).Text = "X";
            getBoxControl(boxPanel, 2, 4, 7).Text = "X";
            getBoxControl(boxPanel, 2, 4, 8).Text = "X";
            getBoxControl(boxPanel, 2, 4, 9).Text = "X";
            getBoxControl(boxPanel, 2, 4, 10).Text = "X";
            getBoxControl(boxPanel, 2, 5, 1).Text = "X";
            getBoxControl(boxPanel, 2, 5, 2).Text = "X";
            getBoxControl(boxPanel, 2, 5, 3).Text = "94";
            getBoxControl(boxPanel, 2, 5, 4).Text = "X";
            getBoxControl(boxPanel, 2, 5, 5).Text = "X";
            getBoxControl(boxPanel, 2, 5, 6).Text = "X";
            getBoxControl(boxPanel, 2, 5, 7).Text = "X";
            getBoxControl(boxPanel, 2, 5, 8).Text = "X";
            getBoxControl(boxPanel, 2, 5, 9).Text = "X";
            getBoxControl(boxPanel, 2, 5, 10).Text = "X";
            getBoxControl(boxPanel, 2, 6, 1).Text = "X";
            getBoxControl(boxPanel, 2, 6, 2).Text = "X";
            getBoxControl(boxPanel, 2, 6, 3).Text = "X";
            getBoxControl(boxPanel, 2, 6, 4).Text = "X";
            getBoxControl(boxPanel, 2, 6, 5).Text = "X";
            getBoxControl(boxPanel, 2, 6, 6).Text = "X";
            getBoxControl(boxPanel, 2, 6, 7).Text = "X";
            getBoxControl(boxPanel, 2, 6, 8).Text = "X";
            getBoxControl(boxPanel, 2, 6, 9).Text = "66";
            getBoxControl(boxPanel, 2, 6, 10).Text = "X";
            getBoxControl(boxPanel, 2, 7, 1).Text = "92";
            getBoxControl(boxPanel, 2, 7, 2).Text = "X";
            getBoxControl(boxPanel, 2, 7, 3).Text = "X";
            getBoxControl(boxPanel, 2, 7, 4).Text = "X";
            getBoxControl(boxPanel, 2, 7, 5).Text = "X";
            getBoxControl(boxPanel, 2, 7, 6).Text = "X";
            getBoxControl(boxPanel, 2, 7, 7).Text = "X";
            getBoxControl(boxPanel, 2, 7, 8).Text = "X";
            getBoxControl(boxPanel, 2, 7, 9).Text = "X";
            getBoxControl(boxPanel, 2, 7, 10).Text = "X";
            getBoxControl(boxPanel, 2, 8, 1).Text = "X";
            getBoxControl(boxPanel, 2, 8, 2).Text = "82";
            getBoxControl(boxPanel, 2, 8, 3).Text = "X";
            getBoxControl(boxPanel, 2, 8, 4).Text = "X";
            getBoxControl(boxPanel, 2, 8, 5).Text = "X";
            getBoxControl(boxPanel, 2, 8, 6).Text = "X";
            getBoxControl(boxPanel, 2, 8, 7).Text = "X";
            getBoxControl(boxPanel, 2, 8, 8).Text = "X";
            getBoxControl(boxPanel, 2, 8, 9).Text = "X";
            getBoxControl(boxPanel, 2, 8, 10).Text = "X";
            getBoxControl(boxPanel, 2, 9, 1).Text = "X";
            getBoxControl(boxPanel, 2, 9, 2).Text = "X";
            getBoxControl(boxPanel, 2, 9, 3).Text = "X";
            getBoxControl(boxPanel, 2, 9, 4).Text = "X";
            getBoxControl(boxPanel, 2, 9, 5).Text = "X";
            getBoxControl(boxPanel, 2, 9, 6).Text = "X";
            getBoxControl(boxPanel, 2, 9, 7).Text = "X";
            getBoxControl(boxPanel, 2, 9, 8).Text = "94";
            getBoxControl(boxPanel, 2, 9, 9).Text = "X";
            getBoxControl(boxPanel, 2, 9, 10).Text = "X";
            getBoxControl(boxPanel, 2, 10, 1).Text = "X";
            getBoxControl(boxPanel, 2, 10, 2).Text = "X";
            getBoxControl(boxPanel, 2, 10, 3).Text = "X";
            getBoxControl(boxPanel, 2, 10, 4).Text = "X";
            getBoxControl(boxPanel, 2, 10, 5).Text = "X";
            getBoxControl(boxPanel, 2, 10, 6).Text = "X";
            getBoxControl(boxPanel, 2, 10, 7).Text = "X";
            getBoxControl(boxPanel, 2, 10, 8).Text = "X";
            getBoxControl(boxPanel, 2, 10, 9).Text = "X";
            getBoxControl(boxPanel, 2, 10, 10).Text = "63";
            getBoxControl(boxPanel, 3, 1, 1).Text = "X";
            getBoxControl(boxPanel, 3, 1, 2).Text = "X";
            getBoxControl(boxPanel, 3, 1, 3).Text = "X";
            getBoxControl(boxPanel, 3, 1, 4).Text = "X";
            getBoxControl(boxPanel, 3, 1, 5).Text = "X";
            getBoxControl(boxPanel, 3, 1, 6).Text = "X";
            getBoxControl(boxPanel, 3, 1, 7).Text = "X";
            getBoxControl(boxPanel, 3, 1, 8).Text = "X";
            getBoxControl(boxPanel, 3, 1, 9).Text = "X";
            getBoxControl(boxPanel, 3, 1, 10).Text = "83";
            getBoxControl(boxPanel, 3, 2, 1).Text = "X";
            getBoxControl(boxPanel, 3, 2, 2).Text = "X";
            getBoxControl(boxPanel, 3, 2, 3).Text = "X";
            getBoxControl(boxPanel, 3, 2, 4).Text = "X";
            getBoxControl(boxPanel, 3, 2, 5).Text = "X";
            getBoxControl(boxPanel, 3, 2, 6).Text = "X";
            getBoxControl(boxPanel, 3, 2, 7).Text = "X";
            getBoxControl(boxPanel, 3, 2, 8).Text = "X";
            getBoxControl(boxPanel, 3, 2, 9).Text = "61";
            getBoxControl(boxPanel, 3, 2, 10).Text = "X";
            getBoxControl(boxPanel, 3, 3, 1).Text = "83";
            getBoxControl(boxPanel, 3, 3, 2).Text = "X";
            getBoxControl(boxPanel, 3, 3, 3).Text = "X";
            getBoxControl(boxPanel, 3, 3, 4).Text = "X";
            getBoxControl(boxPanel, 3, 3, 5).Text = "X";
            getBoxControl(boxPanel, 3, 3, 6).Text = "X";
            getBoxControl(boxPanel, 3, 3, 7).Text = "X";
            getBoxControl(boxPanel, 3, 3, 8).Text = "X";
            getBoxControl(boxPanel, 3, 3, 9).Text = "X";
            getBoxControl(boxPanel, 3, 3, 10).Text = "X";
            getBoxControl(boxPanel, 3, 4, 1).Text = "X";
            getBoxControl(boxPanel, 3, 4, 2).Text = "65";
            getBoxControl(boxPanel, 3, 4, 3).Text = "X";
            getBoxControl(boxPanel, 3, 4, 4).Text = "X";
            getBoxControl(boxPanel, 3, 4, 5).Text = "X";
            getBoxControl(boxPanel, 3, 4, 6).Text = "X";
            getBoxControl(boxPanel, 3, 4, 7).Text = "X";
            getBoxControl(boxPanel, 3, 4, 8).Text = "X";
            getBoxControl(boxPanel, 3, 4, 9).Text = "X";
            getBoxControl(boxPanel, 3, 4, 10).Text = "X";
            getBoxControl(boxPanel, 3, 5, 1).Text = "X";
            getBoxControl(boxPanel, 3, 5, 2).Text = "X";
            getBoxControl(boxPanel, 3, 5, 3).Text = "X";
            getBoxControl(boxPanel, 3, 5, 4).Text = "X";
            getBoxControl(boxPanel, 3, 5, 5).Text = "X";
            getBoxControl(boxPanel, 3, 5, 6).Text = "X";
            getBoxControl(boxPanel, 3, 5, 7).Text = "64";
            getBoxControl(boxPanel, 3, 5, 8).Text = "X";
            getBoxControl(boxPanel, 3, 5, 9).Text = "X";
            getBoxControl(boxPanel, 3, 5, 10).Text = "X";
            getBoxControl(boxPanel, 3, 6, 1).Text = "X";
            getBoxControl(boxPanel, 3, 6, 2).Text = "X";
            getBoxControl(boxPanel, 3, 6, 3).Text = "X";
            getBoxControl(boxPanel, 3, 6, 4).Text = "X";
            getBoxControl(boxPanel, 3, 6, 5).Text = "X";
            getBoxControl(boxPanel, 3, 6, 6).Text = "85";
            getBoxControl(boxPanel, 3, 6, 7).Text = "X";
            getBoxControl(boxPanel, 3, 6, 8).Text = "X";
            getBoxControl(boxPanel, 3, 6, 9).Text = "X";
            getBoxControl(boxPanel, 3, 6, 10).Text = "X";
            getBoxControl(boxPanel, 3, 7, 1).Text = "X";
            getBoxControl(boxPanel, 3, 7, 2).Text = "X";
            getBoxControl(boxPanel, 3, 7, 3).Text = "X";
            getBoxControl(boxPanel, 3, 7, 4).Text = "X";
            getBoxControl(boxPanel, 3, 7, 5).Text = "X";
            getBoxControl(boxPanel, 3, 7, 6).Text = "X";
            getBoxControl(boxPanel, 3, 7, 7).Text = "X";
            getBoxControl(boxPanel, 3, 7, 8).Text = "78";
            getBoxControl(boxPanel, 3, 7, 9).Text = "X";
            getBoxControl(boxPanel, 3, 7, 10).Text = "X";
            getBoxControl(boxPanel, 3, 8, 1).Text = "X";
            getBoxControl(boxPanel, 3, 8, 2).Text = "X";
            getBoxControl(boxPanel, 3, 8, 3).Text = "X";
            getBoxControl(boxPanel, 3, 8, 4).Text = "X";
            getBoxControl(boxPanel, 3, 8, 5).Text = "85";
            getBoxControl(boxPanel, 3, 8, 6).Text = "X";
            getBoxControl(boxPanel, 3, 8, 7).Text = "X";
            getBoxControl(boxPanel, 3, 8, 8).Text = "X";
            getBoxControl(boxPanel, 3, 8, 9).Text = "X";
            getBoxControl(boxPanel, 3, 8, 10).Text = "X";
            getBoxControl(boxPanel, 3, 9, 1).Text = "X";
            getBoxControl(boxPanel, 3, 9, 2).Text = "X";
            getBoxControl(boxPanel, 3, 9, 3).Text = "55";
            getBoxControl(boxPanel, 3, 9, 4).Text = "X";
            getBoxControl(boxPanel, 3, 9, 5).Text = "X";
            getBoxControl(boxPanel, 3, 9, 6).Text = "X";
            getBoxControl(boxPanel, 3, 9, 7).Text = "X";
            getBoxControl(boxPanel, 3, 9, 8).Text = "X";
            getBoxControl(boxPanel, 3, 9, 9).Text = "X";
            getBoxControl(boxPanel, 3, 9, 10).Text = "X";
            getBoxControl(boxPanel, 3, 10, 1).Text = "X";
            getBoxControl(boxPanel, 3, 10, 2).Text = "X";
            getBoxControl(boxPanel, 3, 10, 3).Text = "X";
            getBoxControl(boxPanel, 3, 10, 4).Text = "77";
            getBoxControl(boxPanel, 3, 10, 5).Text = "X";
            getBoxControl(boxPanel, 3, 10, 6).Text = "X";
            getBoxControl(boxPanel, 3, 10, 7).Text = "X";
            getBoxControl(boxPanel, 3, 10, 8).Text = "X";
            getBoxControl(boxPanel, 3, 10, 9).Text = "X";
            getBoxControl(boxPanel, 3, 10, 10).Text = "X";
            getBoxControl(boxPanel, 4, 1, 1).Text = "X";
            getBoxControl(boxPanel, 4, 1, 2).Text = "X";
            getBoxControl(boxPanel, 4, 1, 3).Text = "X";
            getBoxControl(boxPanel, 4, 1, 4).Text = "X";
            getBoxControl(boxPanel, 4, 1, 5).Text = "X";
            getBoxControl(boxPanel, 4, 1, 6).Text = "X";
            getBoxControl(boxPanel, 4, 1, 7).Text = "X";
            getBoxControl(boxPanel, 4, 1, 8).Text = "94";
            getBoxControl(boxPanel, 4, 1, 9).Text = "X";
            getBoxControl(boxPanel, 4, 1, 10).Text = "X";
            getBoxControl(boxPanel, 4, 2, 1).Text = "X";
            getBoxControl(boxPanel, 4, 2, 2).Text = "X";
            getBoxControl(boxPanel, 4, 2, 3).Text = "68";
            getBoxControl(boxPanel, 4, 2, 4).Text = "X";
            getBoxControl(boxPanel, 4, 2, 5).Text = "X";
            getBoxControl(boxPanel, 4, 2, 6).Text = "X";
            getBoxControl(boxPanel, 4, 2, 7).Text = "X";
            getBoxControl(boxPanel, 4, 2, 8).Text = "X";
            getBoxControl(boxPanel, 4, 2, 9).Text = "X";
            getBoxControl(boxPanel, 4, 2, 10).Text = "X";
            getBoxControl(boxPanel, 4, 3, 1).Text = "X";
            getBoxControl(boxPanel, 4, 3, 2).Text = "61";
            getBoxControl(boxPanel, 4, 3, 3).Text = "X";
            getBoxControl(boxPanel, 4, 3, 4).Text = "X";
            getBoxControl(boxPanel, 4, 3, 5).Text = "X";
            getBoxControl(boxPanel, 4, 3, 6).Text = "X";
            getBoxControl(boxPanel, 4, 3, 7).Text = "X";
            getBoxControl(boxPanel, 4, 3, 8).Text = "X";
            getBoxControl(boxPanel, 4, 3, 9).Text = "X";
            getBoxControl(boxPanel, 4, 3, 10).Text = "X";
            getBoxControl(boxPanel, 4, 4, 1).Text = "X";
            getBoxControl(boxPanel, 4, 4, 2).Text = "X";
            getBoxControl(boxPanel, 4, 4, 3).Text = "X";
            getBoxControl(boxPanel, 4, 4, 4).Text = "X";
            getBoxControl(boxPanel, 4, 4, 5).Text = "99";
            getBoxControl(boxPanel, 4, 4, 6).Text = "X";
            getBoxControl(boxPanel, 4, 4, 7).Text = "X";
            getBoxControl(boxPanel, 4, 4, 8).Text = "X";
            getBoxControl(boxPanel, 4, 4, 9).Text = "X";
            getBoxControl(boxPanel, 4, 4, 10).Text = "X";
            getBoxControl(boxPanel, 4, 5, 1).Text = "X";
            getBoxControl(boxPanel, 4, 5, 2).Text = "X";
            getBoxControl(boxPanel, 4, 5, 3).Text = "X";
            getBoxControl(boxPanel, 4, 5, 4).Text = "54";
            getBoxControl(boxPanel, 4, 5, 5).Text = "X";
            getBoxControl(boxPanel, 4, 5, 6).Text = "X";
            getBoxControl(boxPanel, 4, 5, 7).Text = "X";
            getBoxControl(boxPanel, 4, 5, 8).Text = "X";
            getBoxControl(boxPanel, 4, 5, 9).Text = "X";
            getBoxControl(boxPanel, 4, 5, 10).Text = "X";
            getBoxControl(boxPanel, 4, 6, 1).Text = "X";
            getBoxControl(boxPanel, 4, 6, 2).Text = "X";
            getBoxControl(boxPanel, 4, 6, 3).Text = "X";
            getBoxControl(boxPanel, 4, 6, 4).Text = "X";
            getBoxControl(boxPanel, 4, 6, 5).Text = "X";
            getBoxControl(boxPanel, 4, 6, 6).Text = "X";
            getBoxControl(boxPanel, 4, 6, 7).Text = "75";
            getBoxControl(boxPanel, 4, 6, 8).Text = "X";
            getBoxControl(boxPanel, 4, 6, 9).Text = "X";
            getBoxControl(boxPanel, 4, 6, 10).Text = "X";
            getBoxControl(boxPanel, 4, 7, 1).Text = "X";
            getBoxControl(boxPanel, 4, 7, 2).Text = "X";
            getBoxControl(boxPanel, 4, 7, 3).Text = "X";
            getBoxControl(boxPanel, 4, 7, 4).Text = "X";
            getBoxControl(boxPanel, 4, 7, 5).Text = "X";
            getBoxControl(boxPanel, 4, 7, 6).Text = "66";
            getBoxControl(boxPanel, 4, 7, 7).Text = "X";
            getBoxControl(boxPanel, 4, 7, 8).Text = "X";
            getBoxControl(boxPanel, 4, 7, 9).Text = "X";
            getBoxControl(boxPanel, 4, 7, 10).Text = "X";
            getBoxControl(boxPanel, 4, 8, 1).Text = "76";
            getBoxControl(boxPanel, 4, 8, 2).Text = "X";
            getBoxControl(boxPanel, 4, 8, 3).Text = "X";
            getBoxControl(boxPanel, 4, 8, 4).Text = "X";
            getBoxControl(boxPanel, 4, 8, 5).Text = "X";
            getBoxControl(boxPanel, 4, 8, 6).Text = "X";
            getBoxControl(boxPanel, 4, 8, 7).Text = "X";
            getBoxControl(boxPanel, 4, 8, 8).Text = "X";
            getBoxControl(boxPanel, 4, 8, 9).Text = "X";
            getBoxControl(boxPanel, 4, 8, 10).Text = "X";
            getBoxControl(boxPanel, 4, 9, 1).Text = "X";
            getBoxControl(boxPanel, 4, 9, 2).Text = "X";
            getBoxControl(boxPanel, 4, 9, 3).Text = "X";
            getBoxControl(boxPanel, 4, 9, 4).Text = "X";
            getBoxControl(boxPanel, 4, 9, 5).Text = "X";
            getBoxControl(boxPanel, 4, 9, 6).Text = "X";
            getBoxControl(boxPanel, 4, 9, 7).Text = "X";
            getBoxControl(boxPanel, 4, 9, 8).Text = "X";
            getBoxControl(boxPanel, 4, 9, 9).Text = "X";
            getBoxControl(boxPanel, 4, 9, 10).Text = "63";
            getBoxControl(boxPanel, 4, 10, 1).Text = "X";
            getBoxControl(boxPanel, 4, 10, 2).Text = "X";
            getBoxControl(boxPanel, 4, 10, 3).Text = "X";
            getBoxControl(boxPanel, 4, 10, 4).Text = "X";
            getBoxControl(boxPanel, 4, 10, 5).Text = "X";
            getBoxControl(boxPanel, 4, 10, 6).Text = "X";
            getBoxControl(boxPanel, 4, 10, 7).Text = "X";
            getBoxControl(boxPanel, 4, 10, 8).Text = "X";
            getBoxControl(boxPanel, 4, 10, 9).Text = "67";
            getBoxControl(boxPanel, 4, 10, 10).Text = "X";
            getBoxControl(boxPanel, 5, 1, 1).Text = "X";
            getBoxControl(boxPanel, 5, 1, 2).Text = "X";
            getBoxControl(boxPanel, 5, 1, 3).Text = "X";
            getBoxControl(boxPanel, 5, 1, 4).Text = "69";
            getBoxControl(boxPanel, 5, 1, 5).Text = "X";
            getBoxControl(boxPanel, 5, 1, 6).Text = "X";
            getBoxControl(boxPanel, 5, 1, 7).Text = "X";
            getBoxControl(boxPanel, 5, 1, 8).Text = "X";
            getBoxControl(boxPanel, 5, 1, 9).Text = "X";
            getBoxControl(boxPanel, 5, 1, 10).Text = "X";
            getBoxControl(boxPanel, 5, 2, 1).Text = "X";
            getBoxControl(boxPanel, 5, 2, 2).Text = "X";
            getBoxControl(boxPanel, 5, 2, 3).Text = "X";
            getBoxControl(boxPanel, 5, 2, 4).Text = "X";
            getBoxControl(boxPanel, 5, 2, 5).Text = "88";
            getBoxControl(boxPanel, 5, 2, 6).Text = "X";
            getBoxControl(boxPanel, 5, 2, 7).Text = "X";
            getBoxControl(boxPanel, 5, 2, 8).Text = "X";
            getBoxControl(boxPanel, 5, 2, 9).Text = "X";
            getBoxControl(boxPanel, 5, 2, 10).Text = "X";
            getBoxControl(boxPanel, 5, 3, 1).Text = "X";
            getBoxControl(boxPanel, 5, 3, 2).Text = "X";
            getBoxControl(boxPanel, 5, 3, 3).Text = "X";
            getBoxControl(boxPanel, 5, 3, 4).Text = "X";
            getBoxControl(boxPanel, 5, 3, 5).Text = "X";
            getBoxControl(boxPanel, 5, 3, 6).Text = "X";
            getBoxControl(boxPanel, 5, 3, 7).Text = "X";
            getBoxControl(boxPanel, 5, 3, 8).Text = "X";
            getBoxControl(boxPanel, 5, 3, 9).Text = "X";
            getBoxControl(boxPanel, 5, 3, 10).Text = "82";
            getBoxControl(boxPanel, 5, 4, 1).Text = "X";
            getBoxControl(boxPanel, 5, 4, 2).Text = "X";
            getBoxControl(boxPanel, 5, 4, 3).Text = "X";
            getBoxControl(boxPanel, 5, 4, 4).Text = "X";
            getBoxControl(boxPanel, 5, 4, 5).Text = "X";
            getBoxControl(boxPanel, 5, 4, 6).Text = "X";
            getBoxControl(boxPanel, 5, 4, 7).Text = "X";
            getBoxControl(boxPanel, 5, 4, 8).Text = "X";
            getBoxControl(boxPanel, 5, 4, 9).Text = "95";
            getBoxControl(boxPanel, 5, 4, 10).Text = "X";
            getBoxControl(boxPanel, 5, 5, 1).Text = "99";
            getBoxControl(boxPanel, 5, 5, 2).Text = "X";
            getBoxControl(boxPanel, 5, 5, 3).Text = "X";
            getBoxControl(boxPanel, 5, 5, 4).Text = "X";
            getBoxControl(boxPanel, 5, 5, 5).Text = "X";
            getBoxControl(boxPanel, 5, 5, 6).Text = "X";
            getBoxControl(boxPanel, 5, 5, 7).Text = "X";
            getBoxControl(boxPanel, 5, 5, 8).Text = "X";
            getBoxControl(boxPanel, 5, 5, 9).Text = "X";
            getBoxControl(boxPanel, 5, 5, 10).Text = "X";
            getBoxControl(boxPanel, 5, 6, 1).Text = "X";
            getBoxControl(boxPanel, 5, 6, 2).Text = "X";
            getBoxControl(boxPanel, 5, 6, 3).Text = "67";
            getBoxControl(boxPanel, 5, 6, 4).Text = "X";
            getBoxControl(boxPanel, 5, 6, 5).Text = "X";
            getBoxControl(boxPanel, 5, 6, 6).Text = "X";
            getBoxControl(boxPanel, 5, 6, 7).Text = "X";
            getBoxControl(boxPanel, 5, 6, 8).Text = "X";
            getBoxControl(boxPanel, 5, 6, 9).Text = "X";
            getBoxControl(boxPanel, 5, 6, 10).Text = "X";
            getBoxControl(boxPanel, 5, 7, 1).Text = "X";
            getBoxControl(boxPanel, 5, 7, 2).Text = "X";
            getBoxControl(boxPanel, 5, 7, 3).Text = "X";
            getBoxControl(boxPanel, 5, 7, 4).Text = "X";
            getBoxControl(boxPanel, 5, 7, 5).Text = "X";
            getBoxControl(boxPanel, 5, 7, 6).Text = "X";
            getBoxControl(boxPanel, 5, 7, 7).Text = "95";
            getBoxControl(boxPanel, 5, 7, 8).Text = "X";
            getBoxControl(boxPanel, 5, 7, 9).Text = "X";
            getBoxControl(boxPanel, 5, 7, 10).Text = "X";
            getBoxControl(boxPanel, 5, 8, 1).Text = "X";
            getBoxControl(boxPanel, 5, 8, 2).Text = "X";
            getBoxControl(boxPanel, 5, 8, 3).Text = "X";
            getBoxControl(boxPanel, 5, 8, 4).Text = "X";
            getBoxControl(boxPanel, 5, 8, 5).Text = "X";
            getBoxControl(boxPanel, 5, 8, 6).Text = "68";
            getBoxControl(boxPanel, 5, 8, 7).Text = "X";
            getBoxControl(boxPanel, 5, 8, 8).Text = "X";
            getBoxControl(boxPanel, 5, 8, 9).Text = "X";
            getBoxControl(boxPanel, 5, 8, 10).Text = "X";
            getBoxControl(boxPanel, 5, 9, 1).Text = "X";
            getBoxControl(boxPanel, 5, 9, 2).Text = "X";
            getBoxControl(boxPanel, 5, 9, 3).Text = "X";
            getBoxControl(boxPanel, 5, 9, 4).Text = "X";
            getBoxControl(boxPanel, 5, 9, 5).Text = "X";
            getBoxControl(boxPanel, 5, 9, 6).Text = "X";
            getBoxControl(boxPanel, 5, 9, 7).Text = "X";
            getBoxControl(boxPanel, 5, 9, 8).Text = "67";
            getBoxControl(boxPanel, 5, 9, 9).Text = "X";
            getBoxControl(boxPanel, 5, 9, 10).Text = "X";
            getBoxControl(boxPanel, 5, 10, 1).Text = "X";
            getBoxControl(boxPanel, 5, 10, 2).Text = "86";
            getBoxControl(boxPanel, 5, 10, 3).Text = "X";
            getBoxControl(boxPanel, 5, 10, 4).Text = "X";
            getBoxControl(boxPanel, 5, 10, 5).Text = "X";
            getBoxControl(boxPanel, 5, 10, 6).Text = "X";
            getBoxControl(boxPanel, 5, 10, 7).Text = "X";
            getBoxControl(boxPanel, 5, 10, 8).Text = "X";
            getBoxControl(boxPanel, 5, 10, 9).Text = "X";
            getBoxControl(boxPanel, 5, 10, 10).Text = "X";
            getBoxControl(boxPanel, 6, 1, 1).Text = "X";
            getBoxControl(boxPanel, 6, 1, 2).Text = "99";
            getBoxControl(boxPanel, 6, 1, 3).Text = "X";
            getBoxControl(boxPanel, 6, 1, 4).Text = "X";
            getBoxControl(boxPanel, 6, 1, 5).Text = "X";
            getBoxControl(boxPanel, 6, 1, 6).Text = "X";
            getBoxControl(boxPanel, 6, 1, 7).Text = "X";
            getBoxControl(boxPanel, 6, 1, 8).Text = "X";
            getBoxControl(boxPanel, 6, 1, 9).Text = "X";
            getBoxControl(boxPanel, 6, 1, 10).Text = "X";
            getBoxControl(boxPanel, 6, 2, 1).Text = "X";
            getBoxControl(boxPanel, 6, 2, 2).Text = "X";
            getBoxControl(boxPanel, 6, 2, 3).Text = "X";
            getBoxControl(boxPanel, 6, 2, 4).Text = "X";
            getBoxControl(boxPanel, 6, 2, 5).Text = "81";
            getBoxControl(boxPanel, 6, 2, 6).Text = "X";
            getBoxControl(boxPanel, 6, 2, 7).Text = "X";
            getBoxControl(boxPanel, 6, 2, 8).Text = "X";
            getBoxControl(boxPanel, 6, 2, 9).Text = "X";
            getBoxControl(boxPanel, 6, 2, 10).Text = "X";
            getBoxControl(boxPanel, 6, 3, 1).Text = "X";
            getBoxControl(boxPanel, 6, 3, 2).Text = "X";
            getBoxControl(boxPanel, 6, 3, 3).Text = "X";
            getBoxControl(boxPanel, 6, 3, 4).Text = "X";
            getBoxControl(boxPanel, 6, 3, 5).Text = "X";
            getBoxControl(boxPanel, 6, 3, 6).Text = "64";
            getBoxControl(boxPanel, 6, 3, 7).Text = "X";
            getBoxControl(boxPanel, 6, 3, 8).Text = "X";
            getBoxControl(boxPanel, 6, 3, 9).Text = "X";
            getBoxControl(boxPanel, 6, 3, 10).Text = "X";
            getBoxControl(boxPanel, 6, 4, 1).Text = "X";
            getBoxControl(boxPanel, 6, 4, 2).Text = "X";
            getBoxControl(boxPanel, 6, 4, 3).Text = "X";
            getBoxControl(boxPanel, 6, 4, 4).Text = "X";
            getBoxControl(boxPanel, 6, 4, 5).Text = "X";
            getBoxControl(boxPanel, 6, 4, 6).Text = "X";
            getBoxControl(boxPanel, 6, 4, 7).Text = "66";
            getBoxControl(boxPanel, 6, 4, 8).Text = "X";
            getBoxControl(boxPanel, 6, 4, 9).Text = "X";
            getBoxControl(boxPanel, 6, 4, 10).Text = "X";
            getBoxControl(boxPanel, 6, 5, 1).Text = "X";
            getBoxControl(boxPanel, 6, 5, 2).Text = "X";
            getBoxControl(boxPanel, 6, 5, 3).Text = "X";
            getBoxControl(boxPanel, 6, 5, 4).Text = "X";
            getBoxControl(boxPanel, 6, 5, 5).Text = "X";
            getBoxControl(boxPanel, 6, 5, 6).Text = "X";
            getBoxControl(boxPanel, 6, 5, 7).Text = "X";
            getBoxControl(boxPanel, 6, 5, 8).Text = "X";
            getBoxControl(boxPanel, 6, 5, 9).Text = "80";
            getBoxControl(boxPanel, 6, 5, 10).Text = "X";
            getBoxControl(boxPanel, 6, 6, 1).Text = "X";
            getBoxControl(boxPanel, 6, 6, 2).Text = "X";
            getBoxControl(boxPanel, 6, 6, 3).Text = "80";
            getBoxControl(boxPanel, 6, 6, 4).Text = "X";
            getBoxControl(boxPanel, 6, 6, 5).Text = "X";
            getBoxControl(boxPanel, 6, 6, 6).Text = "X";
            getBoxControl(boxPanel, 6, 6, 7).Text = "X";
            getBoxControl(boxPanel, 6, 6, 8).Text = "X";
            getBoxControl(boxPanel, 6, 6, 9).Text = "X";
            getBoxControl(boxPanel, 6, 6, 10).Text = "X";
            getBoxControl(boxPanel, 6, 7, 1).Text = "X";
            getBoxControl(boxPanel, 6, 7, 2).Text = "X";
            getBoxControl(boxPanel, 6, 7, 3).Text = "X";
            getBoxControl(boxPanel, 6, 7, 4).Text = "X";
            getBoxControl(boxPanel, 6, 7, 5).Text = "X";
            getBoxControl(boxPanel, 6, 7, 6).Text = "X";
            getBoxControl(boxPanel, 6, 7, 7).Text = "X";
            getBoxControl(boxPanel, 6, 7, 8).Text = "69";
            getBoxControl(boxPanel, 6, 7, 9).Text = "X";
            getBoxControl(boxPanel, 6, 7, 10).Text = "X";
            getBoxControl(boxPanel, 6, 8, 1).Text = "X";
            getBoxControl(boxPanel, 6, 8, 2).Text = "X";
            getBoxControl(boxPanel, 6, 8, 3).Text = "X";
            getBoxControl(boxPanel, 6, 8, 4).Text = "X";
            getBoxControl(boxPanel, 6, 8, 5).Text = "X";
            getBoxControl(boxPanel, 6, 8, 6).Text = "X";
            getBoxControl(boxPanel, 6, 8, 7).Text = "X";
            getBoxControl(boxPanel, 6, 8, 8).Text = "X";
            getBoxControl(boxPanel, 6, 8, 9).Text = "X";
            getBoxControl(boxPanel, 6, 8, 10).Text = "62";
            getBoxControl(boxPanel, 6, 9, 1).Text = "X";
            getBoxControl(boxPanel, 6, 9, 2).Text = "X";
            getBoxControl(boxPanel, 6, 9, 3).Text = "X";
            getBoxControl(boxPanel, 6, 9, 4).Text = "79";
            getBoxControl(boxPanel, 6, 9, 5).Text = "X";
            getBoxControl(boxPanel, 6, 9, 6).Text = "X";
            getBoxControl(boxPanel, 6, 9, 7).Text = "X";
            getBoxControl(boxPanel, 6, 9, 8).Text = "X";
            getBoxControl(boxPanel, 6, 9, 9).Text = "X";
            getBoxControl(boxPanel, 6, 9, 10).Text = "X";
            getBoxControl(boxPanel, 6, 10, 1).Text = "88";
            getBoxControl(boxPanel, 6, 10, 2).Text = "X";
            getBoxControl(boxPanel, 6, 10, 3).Text = "X";
            getBoxControl(boxPanel, 6, 10, 4).Text = "X";
            getBoxControl(boxPanel, 6, 10, 5).Text = "X";
            getBoxControl(boxPanel, 6, 10, 6).Text = "X";
            getBoxControl(boxPanel, 6, 10, 7).Text = "X";
            getBoxControl(boxPanel, 6, 10, 8).Text = "X";
            getBoxControl(boxPanel, 6, 10, 9).Text = "X";
            getBoxControl(boxPanel, 6, 10, 10).Text = "X";
            getBoxControl(boxPanel, 7, 1, 1).Text = "X";
            getBoxControl(boxPanel, 7, 1, 2).Text = "X";
            getBoxControl(boxPanel, 7, 1, 3).Text = "X";
            getBoxControl(boxPanel, 7, 1, 4).Text = "X";
            getBoxControl(boxPanel, 7, 1, 5).Text = "X";
            getBoxControl(boxPanel, 7, 1, 6).Text = "X";
            getBoxControl(boxPanel, 7, 1, 7).Text = "X";
            getBoxControl(boxPanel, 7, 1, 8).Text = "50";
            getBoxControl(boxPanel, 7, 1, 9).Text = "X";
            getBoxControl(boxPanel, 7, 1, 10).Text = "X";
            getBoxControl(boxPanel, 7, 2, 1).Text = "X";
            getBoxControl(boxPanel, 7, 2, 2).Text = "86";
            getBoxControl(boxPanel, 7, 2, 3).Text = "X";
            getBoxControl(boxPanel, 7, 2, 4).Text = "X";
            getBoxControl(boxPanel, 7, 2, 5).Text = "X";
            getBoxControl(boxPanel, 7, 2, 6).Text = "X";
            getBoxControl(boxPanel, 7, 2, 7).Text = "X";
            getBoxControl(boxPanel, 7, 2, 8).Text = "X";
            getBoxControl(boxPanel, 7, 2, 9).Text = "X";
            getBoxControl(boxPanel, 7, 2, 10).Text = "X";
            getBoxControl(boxPanel, 7, 3, 1).Text = "X";
            getBoxControl(boxPanel, 7, 3, 2).Text = "X";
            getBoxControl(boxPanel, 7, 3, 3).Text = "X";
            getBoxControl(boxPanel, 7, 3, 4).Text = "X";
            getBoxControl(boxPanel, 7, 3, 5).Text = "97";
            getBoxControl(boxPanel, 7, 3, 6).Text = "X";
            getBoxControl(boxPanel, 7, 3, 7).Text = "X";
            getBoxControl(boxPanel, 7, 3, 8).Text = "X";
            getBoxControl(boxPanel, 7, 3, 9).Text = "X";
            getBoxControl(boxPanel, 7, 3, 10).Text = "X";
            getBoxControl(boxPanel, 7, 4, 1).Text = "X";
            getBoxControl(boxPanel, 7, 4, 2).Text = "X";
            getBoxControl(boxPanel, 7, 4, 3).Text = "X";
            getBoxControl(boxPanel, 7, 4, 4).Text = "96";
            getBoxControl(boxPanel, 7, 4, 5).Text = "X";
            getBoxControl(boxPanel, 7, 4, 6).Text = "X";
            getBoxControl(boxPanel, 7, 4, 7).Text = "X";
            getBoxControl(boxPanel, 7, 4, 8).Text = "X";
            getBoxControl(boxPanel, 7, 4, 9).Text = "X";
            getBoxControl(boxPanel, 7, 4, 10).Text = "X";
            getBoxControl(boxPanel, 7, 5, 1).Text = "95";
            getBoxControl(boxPanel, 7, 5, 2).Text = "X";
            getBoxControl(boxPanel, 7, 5, 3).Text = "X";
            getBoxControl(boxPanel, 7, 5, 4).Text = "X";
            getBoxControl(boxPanel, 7, 5, 5).Text = "X";
            getBoxControl(boxPanel, 7, 5, 6).Text = "X";
            getBoxControl(boxPanel, 7, 5, 7).Text = "X";
            getBoxControl(boxPanel, 7, 5, 8).Text = "X";
            getBoxControl(boxPanel, 7, 5, 9).Text = "X";
            getBoxControl(boxPanel, 7, 5, 10).Text = "X";
            getBoxControl(boxPanel, 7, 6, 1).Text = "X";
            getBoxControl(boxPanel, 7, 6, 2).Text = "X";
            getBoxControl(boxPanel, 7, 6, 3).Text = "X";
            getBoxControl(boxPanel, 7, 6, 4).Text = "X";
            getBoxControl(boxPanel, 7, 6, 5).Text = "X";
            getBoxControl(boxPanel, 7, 6, 6).Text = "X";
            getBoxControl(boxPanel, 7, 6, 7).Text = "X";
            getBoxControl(boxPanel, 7, 6, 8).Text = "X";
            getBoxControl(boxPanel, 7, 6, 9).Text = "97";
            getBoxControl(boxPanel, 7, 6, 10).Text = "X";
            getBoxControl(boxPanel, 7, 7, 1).Text = "X";
            getBoxControl(boxPanel, 7, 7, 2).Text = "X";
            getBoxControl(boxPanel, 7, 7, 3).Text = "66";
            getBoxControl(boxPanel, 7, 7, 4).Text = "X";
            getBoxControl(boxPanel, 7, 7, 5).Text = "X";
            getBoxControl(boxPanel, 7, 7, 6).Text = "X";
            getBoxControl(boxPanel, 7, 7, 7).Text = "X";
            getBoxControl(boxPanel, 7, 7, 8).Text = "X";
            getBoxControl(boxPanel, 7, 7, 9).Text = "X";
            getBoxControl(boxPanel, 7, 7, 10).Text = "X";
            getBoxControl(boxPanel, 7, 8, 1).Text = "X";
            getBoxControl(boxPanel, 7, 8, 2).Text = "X";
            getBoxControl(boxPanel, 7, 8, 3).Text = "X";
            getBoxControl(boxPanel, 7, 8, 4).Text = "X";
            getBoxControl(boxPanel, 7, 8, 5).Text = "X";
            getBoxControl(boxPanel, 7, 8, 6).Text = "99";
            getBoxControl(boxPanel, 7, 8, 7).Text = "X";
            getBoxControl(boxPanel, 7, 8, 8).Text = "X";
            getBoxControl(boxPanel, 7, 8, 9).Text = "X";
            getBoxControl(boxPanel, 7, 8, 10).Text = "X";
            getBoxControl(boxPanel, 7, 9, 1).Text = "X";
            getBoxControl(boxPanel, 7, 9, 2).Text = "X";
            getBoxControl(boxPanel, 7, 9, 3).Text = "X";
            getBoxControl(boxPanel, 7, 9, 4).Text = "X";
            getBoxControl(boxPanel, 7, 9, 5).Text = "X";
            getBoxControl(boxPanel, 7, 9, 6).Text = "X";
            getBoxControl(boxPanel, 7, 9, 7).Text = "52";
            getBoxControl(boxPanel, 7, 9, 8).Text = "X";
            getBoxControl(boxPanel, 7, 9, 9).Text = "X";
            getBoxControl(boxPanel, 7, 9, 10).Text = "X";
            getBoxControl(boxPanel, 7, 10, 1).Text = "X";
            getBoxControl(boxPanel, 7, 10, 2).Text = "X";
            getBoxControl(boxPanel, 7, 10, 3).Text = "X";
            getBoxControl(boxPanel, 7, 10, 4).Text = "X";
            getBoxControl(boxPanel, 7, 10, 5).Text = "X";
            getBoxControl(boxPanel, 7, 10, 6).Text = "X";
            getBoxControl(boxPanel, 7, 10, 7).Text = "X";
            getBoxControl(boxPanel, 7, 10, 8).Text = "X";
            getBoxControl(boxPanel, 7, 10, 9).Text = "X";
            getBoxControl(boxPanel, 7, 10, 10).Text = "71";
            getBoxControl(boxPanel, 8, 1, 1).Text = "X";
            getBoxControl(boxPanel, 8, 1, 2).Text = "X";
            getBoxControl(boxPanel, 8, 1, 3).Text = "X";
            getBoxControl(boxPanel, 8, 1, 4).Text = "X";
            getBoxControl(boxPanel, 8, 1, 5).Text = "98";
            getBoxControl(boxPanel, 8, 1, 6).Text = "X";
            getBoxControl(boxPanel, 8, 1, 7).Text = "X";
            getBoxControl(boxPanel, 8, 1, 8).Text = "X";
            getBoxControl(boxPanel, 8, 1, 9).Text = "X";
            getBoxControl(boxPanel, 8, 1, 10).Text = "X";
            getBoxControl(boxPanel, 8, 2, 1).Text = "X";
            getBoxControl(boxPanel, 8, 2, 2).Text = "X";
            getBoxControl(boxPanel, 8, 2, 3).Text = "X";
            getBoxControl(boxPanel, 8, 2, 4).Text = "X";
            getBoxControl(boxPanel, 8, 2, 5).Text = "X";
            getBoxControl(boxPanel, 8, 2, 6).Text = "X";
            getBoxControl(boxPanel, 8, 2, 7).Text = "73";
            getBoxControl(boxPanel, 8, 2, 8).Text = "X";
            getBoxControl(boxPanel, 8, 2, 9).Text = "X";
            getBoxControl(boxPanel, 8, 2, 10).Text = "X";
            getBoxControl(boxPanel, 8, 3, 1).Text = "X";
            getBoxControl(boxPanel, 8, 3, 2).Text = "X";
            getBoxControl(boxPanel, 8, 3, 3).Text = "X";
            getBoxControl(boxPanel, 8, 3, 4).Text = "82";
            getBoxControl(boxPanel, 8, 3, 5).Text = "X";
            getBoxControl(boxPanel, 8, 3, 6).Text = "X";
            getBoxControl(boxPanel, 8, 3, 7).Text = "X";
            getBoxControl(boxPanel, 8, 3, 8).Text = "X";
            getBoxControl(boxPanel, 8, 3, 9).Text = "X";
            getBoxControl(boxPanel, 8, 3, 10).Text = "X";
            getBoxControl(boxPanel, 8, 4, 1).Text = "X";
            getBoxControl(boxPanel, 8, 4, 2).Text = "X";
            getBoxControl(boxPanel, 8, 4, 3).Text = "51";
            getBoxControl(boxPanel, 8, 4, 4).Text = "X";
            getBoxControl(boxPanel, 8, 4, 5).Text = "X";
            getBoxControl(boxPanel, 8, 4, 6).Text = "X";
            getBoxControl(boxPanel, 8, 4, 7).Text = "X";
            getBoxControl(boxPanel, 8, 4, 8).Text = "X";
            getBoxControl(boxPanel, 8, 4, 9).Text = "X";
            getBoxControl(boxPanel, 8, 4, 10).Text = "X";
            getBoxControl(boxPanel, 8, 5, 1).Text = "X";
            getBoxControl(boxPanel, 8, 5, 2).Text = "71";
            getBoxControl(boxPanel, 8, 5, 3).Text = "X";
            getBoxControl(boxPanel, 8, 5, 4).Text = "X";
            getBoxControl(boxPanel, 8, 5, 5).Text = "X";
            getBoxControl(boxPanel, 8, 5, 6).Text = "X";
            getBoxControl(boxPanel, 8, 5, 7).Text = "X";
            getBoxControl(boxPanel, 8, 5, 8).Text = "X";
            getBoxControl(boxPanel, 8, 5, 9).Text = "X";
            getBoxControl(boxPanel, 8, 5, 10).Text = "X";
            getBoxControl(boxPanel, 8, 6, 1).Text = "X";
            getBoxControl(boxPanel, 8, 6, 2).Text = "X";
            getBoxControl(boxPanel, 8, 6, 3).Text = "X";
            getBoxControl(boxPanel, 8, 6, 4).Text = "X";
            getBoxControl(boxPanel, 8, 6, 5).Text = "X";
            getBoxControl(boxPanel, 8, 6, 6).Text = "94";
            getBoxControl(boxPanel, 8, 6, 7).Text = "X";
            getBoxControl(boxPanel, 8, 6, 8).Text = "X";
            getBoxControl(boxPanel, 8, 6, 9).Text = "X";
            getBoxControl(boxPanel, 8, 6, 10).Text = "X";
            getBoxControl(boxPanel, 8, 7, 1).Text = "X";
            getBoxControl(boxPanel, 8, 7, 2).Text = "X";
            getBoxControl(boxPanel, 8, 7, 3).Text = "X";
            getBoxControl(boxPanel, 8, 7, 4).Text = "X";
            getBoxControl(boxPanel, 8, 7, 5).Text = "X";
            getBoxControl(boxPanel, 8, 7, 6).Text = "X";
            getBoxControl(boxPanel, 8, 7, 7).Text = "X";
            getBoxControl(boxPanel, 8, 7, 8).Text = "85";
            getBoxControl(boxPanel, 8, 7, 9).Text = "X";
            getBoxControl(boxPanel, 8, 7, 10).Text = "X";
            getBoxControl(boxPanel, 8, 8, 1).Text = "62";
            getBoxControl(boxPanel, 8, 8, 2).Text = "X";
            getBoxControl(boxPanel, 8, 8, 3).Text = "X";
            getBoxControl(boxPanel, 8, 8, 4).Text = "X";
            getBoxControl(boxPanel, 8, 8, 5).Text = "X";
            getBoxControl(boxPanel, 8, 8, 6).Text = "X";
            getBoxControl(boxPanel, 8, 8, 7).Text = "X";
            getBoxControl(boxPanel, 8, 8, 8).Text = "X";
            getBoxControl(boxPanel, 8, 8, 9).Text = "X";
            getBoxControl(boxPanel, 8, 8, 10).Text = "X";
            getBoxControl(boxPanel, 8, 9, 1).Text = "X";
            getBoxControl(boxPanel, 8, 9, 2).Text = "X";
            getBoxControl(boxPanel, 8, 9, 3).Text = "X";
            getBoxControl(boxPanel, 8, 9, 4).Text = "X";
            getBoxControl(boxPanel, 8, 9, 5).Text = "X";
            getBoxControl(boxPanel, 8, 9, 6).Text = "X";
            getBoxControl(boxPanel, 8, 9, 7).Text = "X";
            getBoxControl(boxPanel, 8, 9, 8).Text = "X";
            getBoxControl(boxPanel, 8, 9, 9).Text = "95";
            getBoxControl(boxPanel, 8, 9, 10).Text = "X";
            getBoxControl(boxPanel, 8, 10, 1).Text = "X";
            getBoxControl(boxPanel, 8, 10, 2).Text = "X";
            getBoxControl(boxPanel, 8, 10, 3).Text = "X";
            getBoxControl(boxPanel, 8, 10, 4).Text = "X";
            getBoxControl(boxPanel, 8, 10, 5).Text = "X";
            getBoxControl(boxPanel, 8, 10, 6).Text = "X";
            getBoxControl(boxPanel, 8, 10, 7).Text = "X";
            getBoxControl(boxPanel, 8, 10, 8).Text = "X";
            getBoxControl(boxPanel, 8, 10, 9).Text = "X";
            getBoxControl(boxPanel, 8, 10, 10).Text = "79";
            getBoxControl(boxPanel, 9, 1, 1).Text = "94";
            getBoxControl(boxPanel, 9, 1, 2).Text = "X";
            getBoxControl(boxPanel, 9, 1, 3).Text = "X";
            getBoxControl(boxPanel, 9, 1, 4).Text = "X";
            getBoxControl(boxPanel, 9, 1, 5).Text = "X";
            getBoxControl(boxPanel, 9, 1, 6).Text = "X";
            getBoxControl(boxPanel, 9, 1, 7).Text = "X";
            getBoxControl(boxPanel, 9, 1, 8).Text = "X";
            getBoxControl(boxPanel, 9, 1, 9).Text = "X";
            getBoxControl(boxPanel, 9, 1, 10).Text = "X";
            getBoxControl(boxPanel, 9, 2, 1).Text = "X";
            getBoxControl(boxPanel, 9, 2, 2).Text = "X";
            getBoxControl(boxPanel, 9, 2, 3).Text = "X";
            getBoxControl(boxPanel, 9, 2, 4).Text = "X";
            getBoxControl(boxPanel, 9, 2, 5).Text = "X";
            getBoxControl(boxPanel, 9, 2, 6).Text = "X";
            getBoxControl(boxPanel, 9, 2, 7).Text = "71";
            getBoxControl(boxPanel, 9, 2, 8).Text = "X";
            getBoxControl(boxPanel, 9, 2, 9).Text = "X";
            getBoxControl(boxPanel, 9, 2, 10).Text = "X";
            getBoxControl(boxPanel, 9, 3, 1).Text = "X";
            getBoxControl(boxPanel, 9, 3, 2).Text = "X";
            getBoxControl(boxPanel, 9, 3, 3).Text = "X";
            getBoxControl(boxPanel, 9, 3, 4).Text = "81";
            getBoxControl(boxPanel, 9, 3, 5).Text = "X";
            getBoxControl(boxPanel, 9, 3, 6).Text = "X";
            getBoxControl(boxPanel, 9, 3, 7).Text = "X";
            getBoxControl(boxPanel, 9, 3, 8).Text = "X";
            getBoxControl(boxPanel, 9, 3, 9).Text = "X";
            getBoxControl(boxPanel, 9, 3, 10).Text = "X";
            getBoxControl(boxPanel, 9, 4, 1).Text = "X";
            getBoxControl(boxPanel, 9, 4, 2).Text = "X";
            getBoxControl(boxPanel, 9, 4, 3).Text = "X";
            getBoxControl(boxPanel, 9, 4, 4).Text = "X";
            getBoxControl(boxPanel, 9, 4, 5).Text = "X";
            getBoxControl(boxPanel, 9, 4, 6).Text = "X";
            getBoxControl(boxPanel, 9, 4, 7).Text = "X";
            getBoxControl(boxPanel, 9, 4, 8).Text = "85";
            getBoxControl(boxPanel, 9, 4, 9).Text = "X";
            getBoxControl(boxPanel, 9, 4, 10).Text = "X";
            getBoxControl(boxPanel, 9, 5, 1).Text = "X";
            getBoxControl(boxPanel, 9, 5, 2).Text = "66";
            getBoxControl(boxPanel, 9, 5, 3).Text = "X";
            getBoxControl(boxPanel, 9, 5, 4).Text = "X";
            getBoxControl(boxPanel, 9, 5, 5).Text = "X";
            getBoxControl(boxPanel, 9, 5, 6).Text = "X";
            getBoxControl(boxPanel, 9, 5, 7).Text = "X";
            getBoxControl(boxPanel, 9, 5, 8).Text = "X";
            getBoxControl(boxPanel, 9, 5, 9).Text = "X";
            getBoxControl(boxPanel, 9, 5, 10).Text = "X";
            getBoxControl(boxPanel, 9, 6, 1).Text = "X";
            getBoxControl(boxPanel, 9, 6, 2).Text = "X";
            getBoxControl(boxPanel, 9, 6, 3).Text = "90";
            getBoxControl(boxPanel, 9, 6, 4).Text = "X";
            getBoxControl(boxPanel, 9, 6, 5).Text = "X";
            getBoxControl(boxPanel, 9, 6, 6).Text = "X";
            getBoxControl(boxPanel, 9, 6, 7).Text = "X";
            getBoxControl(boxPanel, 9, 6, 8).Text = "X";
            getBoxControl(boxPanel, 9, 6, 9).Text = "X";
            getBoxControl(boxPanel, 9, 6, 10).Text = "X";
            getBoxControl(boxPanel, 9, 7, 1).Text = "X";
            getBoxControl(boxPanel, 9, 7, 2).Text = "X";
            getBoxControl(boxPanel, 9, 7, 3).Text = "X";
            getBoxControl(boxPanel, 9, 7, 4).Text = "X";
            getBoxControl(boxPanel, 9, 7, 5).Text = "76";
            getBoxControl(boxPanel, 9, 7, 6).Text = "X";
            getBoxControl(boxPanel, 9, 7, 7).Text = "X";
            getBoxControl(boxPanel, 9, 7, 8).Text = "X";
            getBoxControl(boxPanel, 9, 7, 9).Text = "X";
            getBoxControl(boxPanel, 9, 7, 10).Text = "X";
            getBoxControl(boxPanel, 9, 8, 1).Text = "X";
            getBoxControl(boxPanel, 9, 8, 2).Text = "X";
            getBoxControl(boxPanel, 9, 8, 3).Text = "X";
            getBoxControl(boxPanel, 9, 8, 4).Text = "X";
            getBoxControl(boxPanel, 9, 8, 5).Text = "X";
            getBoxControl(boxPanel, 9, 8, 6).Text = "58";
            getBoxControl(boxPanel, 9, 8, 7).Text = "X";
            getBoxControl(boxPanel, 9, 8, 8).Text = "X";
            getBoxControl(boxPanel, 9, 8, 9).Text = "X";
            getBoxControl(boxPanel, 9, 8, 10).Text = "X";
            getBoxControl(boxPanel, 9, 9, 1).Text = "X";
            getBoxControl(boxPanel, 9, 9, 2).Text = "X";
            getBoxControl(boxPanel, 9, 9, 3).Text = "X";
            getBoxControl(boxPanel, 9, 9, 4).Text = "X";
            getBoxControl(boxPanel, 9, 9, 5).Text = "X";
            getBoxControl(boxPanel, 9, 9, 6).Text = "X";
            getBoxControl(boxPanel, 9, 9, 7).Text = "X";
            getBoxControl(boxPanel, 9, 9, 8).Text = "X";
            getBoxControl(boxPanel, 9, 9, 9).Text = "93";
            getBoxControl(boxPanel, 9, 9, 10).Text = "X";
            getBoxControl(boxPanel, 9, 10, 1).Text = "X";
            getBoxControl(boxPanel, 9, 10, 2).Text = "X";
            getBoxControl(boxPanel, 9, 10, 3).Text = "X";
            getBoxControl(boxPanel, 9, 10, 4).Text = "X";
            getBoxControl(boxPanel, 9, 10, 5).Text = "X";
            getBoxControl(boxPanel, 9, 10, 6).Text = "X";
            getBoxControl(boxPanel, 9, 10, 7).Text = "X";
            getBoxControl(boxPanel, 9, 10, 8).Text = "X";
            getBoxControl(boxPanel, 9, 10, 9).Text = "X";
            getBoxControl(boxPanel, 9, 10, 10).Text = "97";
            getBoxControl(boxPanel, 10, 1, 1).Text = "X";
            getBoxControl(boxPanel, 10, 1, 2).Text = "X";
            getBoxControl(boxPanel, 10, 1, 3).Text = "X";
            getBoxControl(boxPanel, 10, 1, 4).Text = "50";
            getBoxControl(boxPanel, 10, 1, 5).Text = "X";
            getBoxControl(boxPanel, 10, 1, 6).Text = "X";
            getBoxControl(boxPanel, 10, 1, 7).Text = "X";
            getBoxControl(boxPanel, 10, 1, 8).Text = "X";
            getBoxControl(boxPanel, 10, 1, 9).Text = "X";
            getBoxControl(boxPanel, 10, 1, 10).Text = "X";
            getBoxControl(boxPanel, 10, 2, 1).Text = "59";
            getBoxControl(boxPanel, 10, 2, 2).Text = "X";
            getBoxControl(boxPanel, 10, 2, 3).Text = "X";
            getBoxControl(boxPanel, 10, 2, 4).Text = "X";
            getBoxControl(boxPanel, 10, 2, 5).Text = "X";
            getBoxControl(boxPanel, 10, 2, 6).Text = "X";
            getBoxControl(boxPanel, 10, 2, 7).Text = "X";
            getBoxControl(boxPanel, 10, 2, 8).Text = "X";
            getBoxControl(boxPanel, 10, 2, 9).Text = "X";
            getBoxControl(boxPanel, 10, 2, 10).Text = "X";
            getBoxControl(boxPanel, 10, 3, 1).Text = "X";
            getBoxControl(boxPanel, 10, 3, 2).Text = "82";
            getBoxControl(boxPanel, 10, 3, 3).Text = "X";
            getBoxControl(boxPanel, 10, 3, 4).Text = "X";
            getBoxControl(boxPanel, 10, 3, 5).Text = "X";
            getBoxControl(boxPanel, 10, 3, 6).Text = "X";
            getBoxControl(boxPanel, 10, 3, 7).Text = "X";
            getBoxControl(boxPanel, 10, 3, 8).Text = "X";
            getBoxControl(boxPanel, 10, 3, 9).Text = "X";
            getBoxControl(boxPanel, 10, 3, 10).Text = "X";
            getBoxControl(boxPanel, 10, 4, 1).Text = "X";
            getBoxControl(boxPanel, 10, 4, 2).Text = "X";
            getBoxControl(boxPanel, 10, 4, 3).Text = "X";
            getBoxControl(boxPanel, 10, 4, 4).Text = "X";
            getBoxControl(boxPanel, 10, 4, 5).Text = "X";
            getBoxControl(boxPanel, 10, 4, 6).Text = "X";
            getBoxControl(boxPanel, 10, 4, 7).Text = "X";
            getBoxControl(boxPanel, 10, 4, 8).Text = "X";
            getBoxControl(boxPanel, 10, 4, 9).Text = "67";
            getBoxControl(boxPanel, 10, 4, 10).Text = "X";
            getBoxControl(boxPanel, 10, 5, 1).Text = "X";
            getBoxControl(boxPanel, 10, 5, 2).Text = "X";
            getBoxControl(boxPanel, 10, 5, 3).Text = "X";
            getBoxControl(boxPanel, 10, 5, 4).Text = "X";
            getBoxControl(boxPanel, 10, 5, 5).Text = "X";
            getBoxControl(boxPanel, 10, 5, 6).Text = "X";
            getBoxControl(boxPanel, 10, 5, 7).Text = "X";
            getBoxControl(boxPanel, 10, 5, 8).Text = "56";
            getBoxControl(boxPanel, 10, 5, 9).Text = "X";
            getBoxControl(boxPanel, 10, 5, 10).Text = "X";
            getBoxControl(boxPanel, 10, 6, 1).Text = "X";
            getBoxControl(boxPanel, 10, 6, 2).Text = "X";
            getBoxControl(boxPanel, 10, 6, 3).Text = "X";
            getBoxControl(boxPanel, 10, 6, 4).Text = "X";
            getBoxControl(boxPanel, 10, 6, 5).Text = "X";
            getBoxControl(boxPanel, 10, 6, 6).Text = "X";
            getBoxControl(boxPanel, 10, 6, 7).Text = "X";
            getBoxControl(boxPanel, 10, 6, 8).Text = "X";
            getBoxControl(boxPanel, 10, 6, 9).Text = "X";
            getBoxControl(boxPanel, 10, 6, 10).Text = "96";
            getBoxControl(boxPanel, 10, 7, 1).Text = "X";
            getBoxControl(boxPanel, 10, 7, 2).Text = "X";
            getBoxControl(boxPanel, 10, 7, 3).Text = "X";
            getBoxControl(boxPanel, 10, 7, 4).Text = "X";
            getBoxControl(boxPanel, 10, 7, 5).Text = "X";
            getBoxControl(boxPanel, 10, 7, 6).Text = "X";
            getBoxControl(boxPanel, 10, 7, 7).Text = "58";
            getBoxControl(boxPanel, 10, 7, 8).Text = "X";
            getBoxControl(boxPanel, 10, 7, 9).Text = "X";
            getBoxControl(boxPanel, 10, 7, 10).Text = "X";
            getBoxControl(boxPanel, 10, 8, 1).Text = "X";
            getBoxControl(boxPanel, 10, 8, 2).Text = "X";
            getBoxControl(boxPanel, 10, 8, 3).Text = "X";
            getBoxControl(boxPanel, 10, 8, 4).Text = "X";
            getBoxControl(boxPanel, 10, 8, 5).Text = "81";
            getBoxControl(boxPanel, 10, 8, 6).Text = "X";
            getBoxControl(boxPanel, 10, 8, 7).Text = "X";
            getBoxControl(boxPanel, 10, 8, 8).Text = "X";
            getBoxControl(boxPanel, 10, 8, 9).Text = "X";
            getBoxControl(boxPanel, 10, 8, 10).Text = "X";
            getBoxControl(boxPanel, 10, 9, 1).Text = "X";
            getBoxControl(boxPanel, 10, 9, 2).Text = "X";
            getBoxControl(boxPanel, 10, 9, 3).Text = "X";
            getBoxControl(boxPanel, 10, 9, 4).Text = "X";
            getBoxControl(boxPanel, 10, 9, 5).Text = "X";
            getBoxControl(boxPanel, 10, 9, 6).Text = "59";
            getBoxControl(boxPanel, 10, 9, 7).Text = "X";
            getBoxControl(boxPanel, 10, 9, 8).Text = "X";
            getBoxControl(boxPanel, 10, 9, 9).Text = "X";
            getBoxControl(boxPanel, 10, 9, 10).Text = "X";
            getBoxControl(boxPanel, 10, 10, 1).Text = "X";
            getBoxControl(boxPanel, 10, 10, 2).Text = "X";
            getBoxControl(boxPanel, 10, 10, 3).Text = "96";
            getBoxControl(boxPanel, 10, 10, 4).Text = "X";
            getBoxControl(boxPanel, 10, 10, 5).Text = "X";
            getBoxControl(boxPanel, 10, 10, 6).Text = "X";
            getBoxControl(boxPanel, 10, 10, 7).Text = "X";
            getBoxControl(boxPanel, 10, 10, 8).Text = "X";
            getBoxControl(boxPanel, 10, 10, 9).Text = "X";
            getBoxControl(boxPanel, 10, 10, 10).Text = "X";

            boxPanel.VisibleChanged += boxPanel_VisibleChanged;
            this.Controls.Add(boxPanel);
            SetDoubleBuffered(boxPanel);
            resPanel.Visible = false;
        }

        void CreateBoxes(int j, int p, int m)
        {
            this.Controls.Remove(boxPanel);
            if (boxPanel != null)
                boxPanel.Dispose();

            boxPanel = provider.CreateJobBoxes(m, p, j);
            boxPanel.Left = btnHideBoxes.Right + 10;
            boxPanel.Top = btnHideBoxes.Top;
            boxPanel.Width = this.Width - boxPanel.Left - 40;
            boxPanel.AutoScroll = false;
            boxPanel.VerticalScroll.Enabled = false;
            boxPanel.VerticalScroll.Visible = false;
            boxPanel.VerticalScroll.Maximum = 0;
            boxPanel.AutoScroll = true;

            boxPanel.VisibleChanged += boxPanel_VisibleChanged;
            this.Controls.Add(boxPanel);
            SetDoubleBuffered(boxPanel);
            resPanel.Visible = false;
        }
        void ImportDataFromXml(string file)
        {
            if (!File.Exists(file))
            {
                MessageBox.Show("The file cannot exist in the path!", "HATA");
                return;
            }
            Cursor = Cursors.WaitCursor;
            Application.DoEvents();
            XDocument doc = XDocument.Load(file);
            IEnumerable<XElement> jobEls = doc.Root.Elements("job");
            IEnumerable<XElement> procEls = doc.Root.Element("job").Elements("process");
            IEnumerable<XElement> macEls = doc.Root.Element("job").Element("process").Elements("machine");
            nmudjob.Value = job = jobEls.Count();
            nmudproc.Value = proc = procEls.Count();
            nmudmac.Value = mac = macEls.Count();
            CreateBoxes(job, proc, mac);
            Application.DoEvents();
            int a = 0;
            int b = 0;
            int c = 0;
            foreach (XElement _job in jobEls)
            {
                b = 0;
                foreach (XElement _proc in _job.Elements("process"))
                {
                    c = 0;
                    foreach (XElement _mac in _proc.Elements("machine"))
                    {
                        float time = float.Parse(_mac.Attribute("time").Value);
                        getBoxControl(boxPanel, a + 1, b + 1, c + 1).Text = (time == -1 ? "X" : time.ToString());
                        c++;
                    }
                    b++;
                }
                a++;
            }
            Cursor = Cursors.Arrow;
        }
    }
    public static class ExtMethods
    {
        public static int ToInt(this decimal val)
        {
            return Convert.ToInt32(val);
        }
        public static int ToInt(this string text)
        {
            return Int32.Parse(text);
        }
        public static string ConvertIfInteger(this float number)
        {
            if (number == (int)number)
                return ((int)number).ToString("0");
            else
                return number.ToString("0.00");
        }
    }
}
