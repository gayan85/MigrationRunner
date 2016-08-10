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

namespace MigrationRunner
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            _openFileDialog = new OpenFileDialog() { Filter = @"Migration dll file|*.dll" };
            LoadSettings();
        }

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
            var announcer = new FluentMigrator.Runner.Announcers.TextWriterAnnouncer(s => System.Diagnostics.Debug.WriteLine(s));
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
                using (var processor = factory.Create(connectionString, announcer, options))
                {
                    var runner = new FluentMigrator.Runner.MigrationRunner(assembly, migrationContext, processor);
                    runner.MigrateUp(true);
                    MessageBox.Show(@"Database Migrate succeeded!!", @"Success", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                }
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
            txtServer.Text = Properties.Settings.Default.Server;
            txtPassword.Text = Properties.Settings.Default.Password;
            ReloadSettings();
        }

        private void ReloadSettings()
        {
            _user = txtUsername.Text;
            _server = txtServer.Text;
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
            Console.WriteLine(result);
        }

        private void saveConfigurationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ReloadSettings();
            Properties.Settings.Default.User = _user;
            Properties.Settings.Default.Database = _database;
            Properties.Settings.Default.Server = _server;
            Properties.Settings.Default.Password = _password;
            Properties.Settings.Default.Save();
            MessageBox.Show(@"Configuration save succeeded!!");
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
