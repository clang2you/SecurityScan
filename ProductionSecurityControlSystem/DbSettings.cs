using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProductionSecurityControlSystem
{
    public partial class DbSettings : Form
    {
        public DbSettings()
        {
            InitializeComponent();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            ConfigHelper.ConfigHelper.SoftConfig.SetDbConfig(connStrTb.Text, "SampleShoe");
            RestartMe();
        }

        private static void RestartMe()
        {
            Application.ExitThread();
            Application.Exit();
            Application.Restart();
            Process.GetCurrentProcess().Kill();
        }
    }
}
