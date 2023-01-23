
using FileWatcherEx;

namespace Demo
{
    public partial class Form1 : Form
    {
        private FileSystemWatcherEx _fw = new();

        public Form1()
        {
            InitializeComponent();
        }



        private void BtnStart_Click(object sender, EventArgs e)
        {
            _fw = new FileSystemWatcherEx(txtPath.Text.Trim(), FW_OnLog);

            _fw.OnRenamed += FW_OnRenamed;
            _fw.OnCreated += FW_OnCreated;
            _fw.OnDeleted += FW_OnDeleted;
            _fw.OnChanged += FW_OnChanged;
            _fw.OnError += FW_OnError;

            _fw.SynchronizingObject = this;
            _fw.IncludeSubdirectories = true;

            _fw.Start();

            btnStart.Enabled = false;
            btnSelectFolder.Enabled = false;
            txtPath.Enabled = false;
            btnStop.Enabled = true;
        }

        private void FW_OnError(object? sender, ErrorEventArgs e)
        {
            if (txtConsole.InvokeRequired)
            {
                txtConsole.Invoke(FW_OnError, sender, e);
            }
            else
            {
                txtConsole.Text += "[ERROR]: " + e.GetException().Message + "\r\n";
            }
        }

        private void FW_OnChanged(object? sender, FileChangedEvent e)
        {
            txtConsole.Text += string.Format("[cha] {0} | {1}",
                Enum.GetName(typeof(ChangeType), e.ChangeType),
                e.FullPath) + "\r\n";
        }

        private void FW_OnDeleted(object? sender, FileChangedEvent e)
        {
            txtConsole.Text += string.Format("[del] {0} | {1}",
                Enum.GetName(typeof(ChangeType), e.ChangeType),
                e.FullPath) + "\r\n";
        }

        private void FW_OnCreated(object? sender, FileChangedEvent e)
        {
            txtConsole.Text += string.Format("[cre] {0} | {1}",
                Enum.GetName(typeof(ChangeType), e.ChangeType),
                e.FullPath) + "\r\n";
        }

        private void FW_OnRenamed(object? sender, FileChangedEvent e)
        {
            txtConsole.Text += string.Format("[ren] {0} | {1} ----> {2}",
                Enum.GetName(typeof(ChangeType), e.ChangeType),
                e.OldFullPath,
                e.FullPath) + "\r\n";
        }

        private void FW_OnLog(string value)
        {
            txtConsole.Text += $@"[log] {value}" + "\r\n";
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _fw.Stop();

            btnStart.Enabled = true;
            btnSelectFolder.Enabled = true;
            txtPath.Enabled = true;
            btnStop.Enabled = false;
        }


        private void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            var fb = new FolderBrowserDialog();


            if (fb.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = fb.SelectedPath;

                _fw.Stop();
                _fw.Dispose();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _fw.Dispose();
        }
    }
}