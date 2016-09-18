using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using MigrationRunner.Helpers;
using MigrationRunner.Models;

namespace MigrationRunner
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            _openFileDialog = new OpenFileDialog() { Filter = @"Migration dll file|*.dll" };
            ListSqlInstances();
            LoadSettings();
        }

        protected override void OnLoad(EventArgs e)
        {

            var pictureBtn = new PictureBox { Size = new Size(30, txtPassword.ClientSize.Height + 2), Cursor = Cursors.Hand, Image = global::MigrationRunner.Properties.Resources.icon_eye_128, SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom, TabIndex = 9, TabStop = false };
            pictureBtn.Location = new Point(txtPassword.ClientSize.Width - pictureBtn.Width, -1);
            pictureBtn.MouseDown += OnPasswordSeeEnable;
            pictureBtn.MouseUp += OnPasswordSeeEnable;
            txtPassword.Controls.Add(pictureBtn);
            SendMessage(txtPassword.Handle, 0xd3, (IntPtr)2, (IntPtr)(pictureBtn.Width << 16));
            base.OnLoad(e);
        }

        private void OnPasswordSeeEnable(object sender, MouseEventArgs e)
        {
            txtPassword.PasswordChar = txtPassword.PasswordChar.Equals('\0') ? '*' : '\0';
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        private string _assemblyPath;
        private string _server;
        private string _database;
        private string _user;
        private string _password;
        private readonly OpenFileDialog _openFileDialog;

        private void btnMigrationUp_Click(object sender, EventArgs e)
        {
            ReloadSettings();
            if (InvalidateSettings()) return;
            txtOutput.Text = string.Empty;
            var announcer = new FluentMigrator.Runner.Announcers.TextWriterAnnouncer(s => txtOutput.Text += s);
            var assembly = Assembly.LoadFile(_assemblyPath);
            var migrationContext = new FluentMigrator.Runner.Initialization.RunnerContext(announcer)
            {
                Namespace = assembly.GetTypes().First(a => a.Name.ToLower().StartsWith("step")).Namespace,
                TransactionPerSession = false,
                ApplicationContext = "sqlserver",
                Targets = new[] { _assemblyPath },
            };

            var options = new FluentMigrator.Runner.Processors.ProcessorOptions()
            {
                PreviewOnly = false,
                Timeout = 600
            };

            var connectionString = $"Data Source={_server};User ID={_user};Password={_password};Database={_database};";
            var factory = new FluentMigrator.Runner.Processors.SqlServer.SqlServerProcessorFactory();
            try
            {
                var task = Task.Factory.StartNew(() =>
                {
                    using (var processor = factory.Create(connectionString, announcer, options))
                    {
                        btnMigrationUp.Text = @"Running..";
                        this.Enable(false);
                        txtOutput.Enabled = true;
                        var runner = new FluentMigrator.Runner.MigrationRunner(assembly, migrationContext, processor);
                        runner.MigrateUp(true);
                    }

                });
                task.ContinueWith((success) =>
                {
                    btnMigrationUp.Text = @"Migration Up";
                    this.Enable(true);
                    MessageBox.Show(@"Database Migrate succeeded!!", @"Success", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    txtOutput.Text += @"Succeeded!!";
                }, TaskContinuationOptions.NotOnFaulted);
                task.ContinueWith((fault) =>
               {
                   btnMigrationUp.Text = @"Migration Up";
                   this.Enable(true);
                   MessageBox.Show($@"Database Migrate failed with {fault?.Exception?.Message}!!", @"ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                   txtOutput.Text += $@"Failed!! {Environment.NewLine} {fault?.Exception?.Message} {Environment.NewLine} {fault?.Exception?.InnerException?.Message}";
               }, TaskContinuationOptions.OnlyOnFaulted);

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, @"Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool InvalidateSettings()
        {

            if (string.IsNullOrEmpty(_assemblyPath))
            {
                MessageBox.Show(@"Assembly path required", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }

            if (string.IsNullOrEmpty(_server))
            {
                MessageBox.Show(@"Server name required", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            if (string.IsNullOrEmpty(_database))
            {
                MessageBox.Show(@"Database name required", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            if (string.IsNullOrEmpty(_user))
            {
                MessageBox.Show(@"Sql login user required", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return true;
            }
            if (string.IsNullOrEmpty(_password))
            {
                MessageBox.Show(@"Sql login password required!!", @"Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return true;
            }

            return false;
        }


        private void LoadSettings()
        {
            txtUsername.Text = Properties.Settings.Default.User;
            txtDatabase.Text = Properties.Settings.Default.Database;
            var server = Properties.Settings.Default.Server;
            if (!string.IsNullOrEmpty(server))
            {
                var index = cmbServer.FindStringExact(server);
                if (index == -1) cmbServer.Text = server; else cmbServer.SelectedIndex = index;
            }
            txtPassword.Text = Properties.Settings.Default.Password;
            ReloadSettings();
        }

        private void ReloadSettings()
        {
            _user = txtUsername.Text;
            var comboboxItem = cmbServer.SelectedItem as ComboboxItem;
            if (comboboxItem == null)
            {
                if (!string.IsNullOrEmpty(cmbServer.Text)) _server = cmbServer.Text;
            }
            else _server = comboboxItem.Value.ToString();
            _assemblyPath = _openFileDialog.FileName;
            _database = txtDatabase.Text;
            _password = txtPassword.Text;
        }

        private void btnLoadMigration_Click(object sender, EventArgs e)
        {

            var result = _openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                _assemblyPath = _openFileDialog.FileName;
                txtAssemblyPath.Text = _assemblyPath;
            }
        }

        private void saveConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadSettings();
            Properties.Settings.Default.User = _user;
            Properties.Settings.Default.Database = _database;
            Properties.Settings.Default.Server = _server;
            Properties.Settings.Default.Password = _password;
            Properties.Settings.Default.Save();
            MessageBox.Show(@"Configuration save succeeded!!", @"Success", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void ListSqlInstances()
        {
            var registryView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
            {
                var instanceKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL", false);
                if (instanceKey == null) return;

                foreach (var instanceName in instanceKey.GetValueNames())
                {
                    var item = new ComboboxItem { Text = Environment.MachineName + @"\" + instanceName, Value = Environment.MachineName + @"\" + instanceName };
                    cmbServer.Items.Add(item);
                }
            }
        }

        private void cmbServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            var comboboxItem = cmbServer.SelectedItem as ComboboxItem;
            if (comboboxItem != null)
                _server = comboboxItem.Value.ToString();
        }
    }
}
