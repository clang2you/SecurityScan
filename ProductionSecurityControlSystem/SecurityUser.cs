using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProductionSecurityControlSystem
{
    public partial class SecurityUser : Form
    {
        public SecurityUser()
        {
            InitializeComponent();
        }

        private void SecurityUser_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }
    }
}
