using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace ProductionSecurityControlSystem
{
    public partial class TechUser : Form
    {
        UHFReaderHelper.ReaderHelper reader;
        public TechUser()
        {
            InitializeComponent();
             reader = new UHFReaderHelper.ReaderHelper();
             button1.ForeColor = Color.DarkGreen;
        }

        private void TechUser_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.ForeColor == Color.DarkGreen)
            {
                textBox1.Text = "";
                button1.ForeColor = Color.DarkRed;
                button1.Text = "取消读取";
                button2.Enabled = false;
                toolStripStatusLabel1.Text = "开始读取标签EPC...";
                if (reader != null)
                {
                    if (!string.IsNullOrEmpty(reader.EPCResult))
                    {
                        reader.EPCResult = string.Empty;
                    }
                }
                if (timer1.Enabled == false)
                {
                    timer1.Enabled = true;   //enable timer
                    timer1.Interval = 200;
                }
                else
                {
                    timer1.Enabled = false;
                }
            }
            else 
            {
                timer1.Stop();
                timer1.Enabled = false;
                button1.ForeColor = Color.DarkGreen;
                button1.Text = "读取EPC";
                button2.Enabled = true;
                toolStripStatusLabel1.Text = "已取消读取EPC！";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                toolStripStatusLabel1.Text = "正在读取标签EPC，请将标签置于读卡器。";
                if (reader != null)
                {
                    reader.ReadingEPC();
                    textBox1.Text = reader.EPCResult;
                }
                else 
                {
                    reader = new UHFReaderHelper.ReaderHelper();
                    reader.ReadingEPC();
                    textBox1.Text = reader.EPCResult;
                }
            }
            catch (Exception error) 
            {
                reader = null;
                button1.Enabled = true;
                toolStripStatusLabel1.Text = error.Message;
            }
        }

        private void InitialEtag() 
        {
            if (reader != null) 
            {
                if (reader.EPCResult.Length > 8) 
                {
                    try
                    {
                        using (SqlConnection sqlCon = new SqlConnection(ConfigHelper.ConfigHelper.SoftConfig.GetDbConfig("SampleShoe"))) 
                        {
                            string check_last_id = "";
                        }
                    }
                    catch (Exception e) 
                    {
                        throw e;
                    }
                }
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text)) 
            {
                timer1.Stop();
                timer1.Enabled = false;
                button1.ForeColor = Color.DarkGreen;
                button1.Text = "读取EPC";
                button2.Enabled = true;
                toolStripStatusLabel1.Text = "读取标签EPC成功！";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            reader.CloseReaderComPort();
            reader = null;
            Form1 loginForm = (Form1)this.Owner;
            loginForm.Show();
            loginForm.CleanLoginTextBox();
            this.FormClosed -= TechUser_FormClosed;
            this.Close();
        }
    }
}
