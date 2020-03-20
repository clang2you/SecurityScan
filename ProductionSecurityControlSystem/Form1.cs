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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            infoLabel.Text = "";
        }

        private const string GET_USER_DATA_SQL = "select job_type from user_data where name='{0}' and passwd='{1}'";

        private string GetUserLoginType(string userName, string passwd) 
        {
            string result = "NOT_FOUND";
            try
            {
                using (SqlConnection sqlCon = new SqlConnection(ConfigHelper.ConfigHelper.SoftConfig.GetDbConfig("SampleShoe")))
                {
                    sqlCon.Open();
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlCon;
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = string.Format(GET_USER_DATA_SQL, userName, passwd);
                    SqlDataReader sqlDr = sqlCmd.ExecuteReader();
                    while (sqlDr.Read())
                    {
                        result = sqlDr[0].ToString();
                    }
                }
            }
            catch 
            {
                result = "ERROR";
            }
            return result;
        }

        private void loginBtn_Click(object sender, EventArgs e)
        {
            infoLabel.ForeColor = Color.Black;
            infoLabel.Text = "";
            if (!string.IsNullOrEmpty(maskedTextBox1.Text) && !string.IsNullOrEmpty(maskedTextBox2.Text))
            {
                string result = GetUserLoginType(maskedTextBox1.Text.Trim(), maskedTextBox2.Text.Trim());
                if (result != "NOT_FOUND" && result != "ERROR")
                {
                    switch (result)
                    {
                        case "IT":
                            ITUser itUser = new ITUser();
                            itUser.Owner = this;
                            this.Hide();
                            itUser.ShowDialog();
                            break;
                        case "TECH":
                            TechUser techUser = new TechUser();
                            techUser.Owner = this;
                            this.Hide();
                            techUser.ShowDialog();
                            break;
                        case "SECURITY":
                            SecurityUser securityUser = new SecurityUser();
                            securityUser.Owner = this;
                            this.Hide();
                            securityUser.ShowDialog();
                            break;
                    }
                }
                else if (result == "ERROR")
                {
                    infoLabel.ForeColor = Color.Red;
                    infoLabel.Text = "Connect to database failed，Please check network or database settings!";
                }
                else
                {
                    infoLabel.ForeColor = Color.Purple;
                    infoLabel.Text = "Username or password incorrect！";
                }
            }
            else 
            {
                infoLabel.ForeColor = Color.Purple;
                infoLabel.Text = "Username or password is empty！";
            }
        }

        private void dbSettingLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            DbSettings dbSettings = new DbSettings();
            dbSettings.ShowDialog();
        }

        public void CleanLoginTextBox() 
        {
            maskedTextBox1.Text = string.Empty;
            maskedTextBox2.Text = string.Empty;
        }

        private void maskedTextBox2_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) 
            {
                infoLabel.ForeColor = Color.Black;
                infoLabel.Text = "";
                if (!string.IsNullOrEmpty(maskedTextBox1.Text) && !string.IsNullOrEmpty(maskedTextBox2.Text))
                {
                    string result = GetUserLoginType(maskedTextBox1.Text.Trim(), maskedTextBox2.Text.Trim());
                    if (result != "NOT_FOUND" && result != "ERROR")
                    {
                        switch (result)
                        {
                            case "IT":
                                ITUser itUser = new ITUser();
                                itUser.Owner = this;
                                this.Hide();
                                itUser.ShowDialog();
                                break;
                            case "TECH":
                                TechUser techUser = new TechUser();
                                techUser.Owner = this;
                                this.Hide();
                                techUser.ShowDialog();
                                break;
                            case "SECURITY":
                                SecurityUser securityUser = new SecurityUser();
                                securityUser.Owner = this;
                                this.Hide();
                                securityUser.ShowDialog();
                                break;
                        }
                    }
                    else if (result == "ERROR")
                    {
                        infoLabel.ForeColor = Color.Red;
                        infoLabel.Text = "Connect to database failed，Please check network or database settings!";
                    }
                    else
                    {
                        infoLabel.ForeColor = Color.Purple;
                        infoLabel.Text = "Username or password incorrect！";
                    }
                }
                else
                {
                    infoLabel.ForeColor = Color.Purple;
                    infoLabel.Text = "Username or password is empty！";
                }
            }
        }
    }
}
