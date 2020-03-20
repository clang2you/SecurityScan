using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace ProductionSecurityControlSystem
{
    public partial class ITUser : Form
    {
        private Reader.ReaderMethod reader;
        private ReaderSetting m_curSetting = new ReaderSetting();
        private int m_nReceiveFlag = 0;
        private InventoryBuffer m_curInventoryBuffer = new InventoryBuffer();
        private OperateTagBuffer m_curOperateTagBuffer = new OperateTagBuffer();
        //实时盘存次数
        private int m_nTotal = 0;
        //实时盘存锁定操作
        private bool m_bLockTab = false;
        private volatile bool m_nSessionPhaseOpened = false;
        //是否显示串口监控数据
        private bool m_bDisplayLog = false;

        //盘存操作前，需要先设置工作天线，用于标识当前是否在执行盘存操作
        private bool m_bInventory = false;

        //列表更新频率
        private int m_nRealRate = 20;

        private bool testOrScan = false;

        private DataTable realDt = new DataTable("InventoryRealTable");

        public ITUser()
        {
            InitializeComponent();
            disconnectBtn.Enabled = false;
            setControls(false);
            reader = new Reader.ReaderMethod();

            if(!string.IsNullOrEmpty(ConfigHelper.ConfigHelper.SoftConfig.GetSoftConfig("reader_ip")))
            {
                ipAddressTb.IpAddressStr = ConfigHelper.ConfigHelper.SoftConfig.GetSoftConfig("reader_ip");
            }
            CheckBox[] antennas = new CheckBox[4];
            antennas[0] = cbRealWorkant1;
            antennas[1] = cbRealWorkant2;
            antennas[2] = cbRealWorkant3;
            antennas[3] = cbRealWorkant4;

            for (int i = 0; i < 4; i++) 
            {
                string keyName = "antenna" + (i + 1).ToString();
                if (ConfigHelper.ConfigHelper.SoftConfig.GetSoftConfig(keyName) == "True")
                {
                    antennas[i].Checked = true;
                }
                else 
                {
                    antennas[i].Checked = false;
                }
            }

                //回调函数
                reader.AnalyCallback = AnalyData;
            reader.ReceiveCallback = ReceiveData;
            reader.SendCallback = SendData;
        }

        private void CreateNewRealInventoryDataTable() 
        {
            realDt = new DataTable("InventoryRealTable");
            realDt.Columns.Add("Name");
            realDt.Columns.Add("EPC");
            realDt.Columns.Add("ScanCount");
            realDt.Columns.Add("LastScanTime");
        }

        private void ReceiveData(byte[] btAryReceiveData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btAryReceiveData, 0, btAryReceiveData.Length);

                WriteLog(logRichTextBox, strLog, 1);
            }
        }

        private void timerInventory_Tick(object sender, EventArgs e)
        {
            m_nReceiveFlag++;
            if (m_nReceiveFlag >= 5)
            {
                RunLoopInventroy();
                m_nReceiveFlag = 0;
            }
        }

        private void totalTimeDisplay(object sender, EventArgs e)
        {
            TimeSpan sp = DateTime.Now - m_curInventoryBuffer.dtStartInventory;
            int totalTime = sp.Minutes * 60 * 1000 + sp.Seconds * 1000 + sp.Milliseconds;
            ledReal5.Text = totalTime.ToString();
            //RefreshInventoryReal(0x00);
        }

        private void SendData(byte[] btArySendData)
        {
            if (m_bDisplayLog)
            {
                string strLog = CCommondMethod.ByteArrayToString(btArySendData, 0, btArySendData.Length);

                WriteLog(logRichTextBox, strLog, 0);
            }
        }

        private void AnalyData(Reader.MessageTran msgTran)
        {
            m_nReceiveFlag = 0;
            if (msgTran.PacketType != 0xA0)
            {
                return;
            }
            switch (msgTran.Cmd)
            {
                case 0x69:
                    //ProcessSetProfile(msgTran);
                    break;
                case 0x6A:
                    //ProcessGetProfile(msgTran);
                    break;
                case 0x71:
                    //设置串口波特率
                    //ProcessSetUartBaudrate(msgTran);
                    break;
                case 0x72:
                    //取读卡器固件版本
                    //ProcessGetFirmwareVersion(msgTran);
                    break;
                case 0x73:
                    //设置RS485读卡器地址
                    //ProcessSetReadAddress(msgTran);
                    break;
                case 0x74:
                    ProcessSetWorkAntenna(msgTran);
                    break;
                case 0x75:
                    ProcessGetWorkAntenna(msgTran);
                    break;
                case 0x76:
                    ProcessSetOutputPower(msgTran);
                    break;
                case 0x97:
                case 0x77:
                    ProcessGetOutputPower(msgTran);
                    break;
                case 0x78:
                    //设置射频标准
                    //ProcessSetFrequencyRegion(msgTran);
                    break;
                case 0x79:
                    //取当前设定射频标准
                    //ProcessGetFrequencyRegion(msgTran);
                    break;
                case 0x7A:
                    ProcessSetBeeperMode(msgTran);
                    break;
                case 0x7B:
                    //ProcessGetReaderTemperature(msgTran);
                    break;
                case 0x7C:
                    //ProcessSetDrmMode(msgTran);
                    break;
                case 0x7D:
                    //ProcessGetDrmMode(msgTran);
                    break;
                case 0x7E:
                    //ProcessGetImpedanceMatch(msgTran);
                    break;
                case 0x60:
                    //ProcessReadGpioValue(msgTran);
                    break;
                case 0x61:
                    //ProcessWriteGpioValue(msgTran);
                    break;
                case 0x62:
                    ProcessSetAntDetector(msgTran);
                    break;
                case 0x63:
                    ProcessGetAntDetector(msgTran);
                    break;
                case 0x67:
                    ProcessSetReaderIdentifier(msgTran);
                    break;
                case 0x68:
                    ProcessGetReaderIdentifier(msgTran);
                    break;

                case 0x80:
                    //ProcessInventory(msgTran);
                    break;
                case 0x81:
                    ProcessReadTag(msgTran);
                    break;
                case 0x82:
                case 0x94:
                    //写标签相关
                    //ProcessWriteTag(msgTran);
                    break;
                case 0x83:
                    //锁定标签
                    //ProcessLockTag(msgTran);
                    break;
                case 0x84:
                    //销毁标签
                    //ProcessKillTag(msgTran);
                    break;
                case 0x85:
                    //选定标签相关
                    //ProcessSetAccessEpcMatch(msgTran);
                    break;
                case 0x86:
                    //选定标签相关
                    //ProcessGetAccessEpcMatch(msgTran);
                    break;

                case 0x89:
                case 0x8B:
                    ProcessInventoryReal(msgTran);
                    break;
                case 0x8A:
                    //快速天线盘存
                    //ProcessFastSwitch(msgTran);
                    break;
                case 0x8D:
                    //ProcessSetMonzaStatus(msgTran);
                    break;
                case 0x8E:
                    //ProcessGetMonzaStatus(msgTran);
                    break;
                case 0x90:
                    //缓存模式盘存
                    //ProcessGetInventoryBuffer(msgTran);
                    break;
                case 0x91:
                    //缓存盘存相关
                    //ProcessGetAndResetInventoryBuffer(msgTran);
                    break;
                case 0x92:
                    //缓存盘存相关
                    //ProcessGetInventoryBufferTagCount(msgTran);
                    break;
                case 0x93:
                    //缓存盘存
                    //ProcessResetInventoryBuffer(msgTran);
                    break;
                case 0x98:
                    //ProcessTagMask(msgTran);
                    break;
                case 0xb0:
                    //ProcessInventoryISO18000(msgTran);
                    break;
                case 0xb1:
                    //ProcessReadTagISO18000(msgTran);
                    break;
                case 0xb2:
                    //ProcessWriteTagISO18000(msgTran);
                    break;
                case 0xb3:
                    //ProcessLockTagISO18000(msgTran);
                    break;
                case 0xb4:
                    //ProcessQueryISO18000(msgTran);
                    break;
                case 0xE1:
                    //ProcessUntraceable(msgTran);
                    break;
                default:
                    break;
            }
        }

        const string GET_SHOE_NAME_BY_EPC = "select name from shoedata where EPC = '{0}'";

        private string GetSampleShoesNameByEPC(string EPC) 
        {
            try
            {
                string result = "N/A";
                using (SqlConnection sqlCon = new SqlConnection(ConfigHelper.ConfigHelper.SoftConfig.GetDbConfig("SampleShoe")))
                {
                    sqlCon.Open();
                    SqlCommand sqlCmd = new SqlCommand();
                    sqlCmd.Connection = sqlCon;
                    sqlCmd.CommandType = CommandType.Text;
                    sqlCmd.CommandText = string.Format(GET_SHOE_NAME_BY_EPC, EPC);
                    SqlDataReader sqlDr = sqlCmd.ExecuteReader();
                    while (sqlDr.Read())
                    {
                        if (sqlDr[0].GetType() != typeof(DBNull)) 
                        {
                            result = sqlDr[0].ToString();
                        }
                         
                    }
                }
                return result;
            }
            catch (Exception e) 
            {
                MessageBox.Show(e.Message);
                return "Error";
            }
        }

        private delegate void RefreshInventoryRealUnsafe(byte btCmd);
        private void RefreshInventoryReal(byte btCmd)
        {
            if (this.InvokeRequired)
            {
                RefreshInventoryRealUnsafe InvokeRefresh = new RefreshInventoryRealUnsafe(RefreshInventoryReal);
                this.Invoke(InvokeRefresh, new object[] { btCmd });
            }
            else
            {
                switch (btCmd)
                {
                    case 0x89:
                    case 0x8B:
                        {
                            int nTagCount = m_curInventoryBuffer.dtTagTable.Rows.Count;
                            int nTotalRead = m_nTotal;// m_curInventoryBuffer.dtTagDetailTable.Rows.Count;
                            TimeSpan ts = m_curInventoryBuffer.dtEndInventory - m_curInventoryBuffer.dtStartInventory;
                            int nTotalTime = ts.Minutes * 60 * 1000 + ts.Seconds * 1000 + ts.Milliseconds;
                            int nCaculatedReadRate = 0;
                            int nCommandDuation = 0;

                            if (m_curInventoryBuffer.nReadRate == 0) //读写器没有返回速度前软件测速度
                            {
                                if (nTotalTime > 0)
                                {
                                    nCaculatedReadRate = (nTotalRead * 1000 / nTotalTime);
                                }
                            }
                            else
                            {
                                nCommandDuation = m_curInventoryBuffer.nDataCount * 1000 / m_curInventoryBuffer.nReadRate;
                                nCaculatedReadRate = m_curInventoryBuffer.nReadRate;
                            }

                            //列表用变量
                            int nEpcCount = 0;
                            int nEpcLength = m_curInventoryBuffer.dtTagTable.Rows.Count;

                            ledReal1.Text = nTagCount.ToString();
                            ledReal2.Text = nCaculatedReadRate.ToString();

                            ledReal5.Text = nTotalTime.ToString();
                            ledReal3.Text = nTotalRead.ToString();
                            ledReal4.Text = nCommandDuation.ToString();  //实际的命令执行时间
                            tbRealMaxRssi.Text = (m_curInventoryBuffer.nMaxRSSI - 129).ToString() + "dBm";
                            tbRealMinRssi.Text = (m_curInventoryBuffer.nMinRSSI - 129).ToString() + "dBm";
                            //lbRealTagCount.Text = "标签EPC号列表（不重复）： " + nTagCount.ToString() + "个";

                            if (testOrScan)
                            {
                                nEpcCount = lvRealList.Items.Count;


                                if (nEpcCount < nEpcLength)
                                {
                                    DataRow row = m_curInventoryBuffer.dtTagTable.Rows[nEpcLength - 1];

                                    ListViewItem item = new ListViewItem();
                                    item.Text = (nEpcCount + 1).ToString();
                                    item.SubItems.Add(row[2].ToString());
                                    item.SubItems.Add(row[0].ToString());
                                    //item.SubItems.Add(row[5].ToString());

                                    item.SubItems.Add(row[7].ToString() + "  /  " + row[8].ToString() + "  /  " + row[9].ToString() + "  /  " + row[10]);

                                    item.SubItems.Add((Convert.ToInt32(row[4]) - 129).ToString() + "dBm");

                                    item.SubItems.Add(row[15].ToString());
                                    item.SubItems.Add(row[6].ToString());

                                    lvRealList.Items.Add(item);
                                }

                                //更新列表中读取的次数
                                if (m_nTotal % m_nRealRate == 1)
                                {
                                    int nIndex = 0;
                                    foreach (DataRow row in m_curInventoryBuffer.dtTagTable.Rows)
                                    {
                                        ListViewItem item;
                                        item = lvRealList.Items[nIndex];
                                        //item.SubItems[3].Text = row[5].ToString();

                                        item.SubItems[3].Text = (row[7].ToString() + "  /  " + row[8].ToString() + "  /  " + row[9].ToString() + "  /  " + row[10]);

                                        item.SubItems[4].Text = (Convert.ToInt32(row[4]) - 129).ToString() + "dBm";

                                        if (m_nSessionPhaseOpened)
                                        {
                                            item.SubItems[5].Text = row[15].ToString();
                                            item.SubItems[6].Text = row[6].ToString();
                                        }
                                        else
                                        {
                                            item.SubItems[6].Text = row[6].ToString();
                                        }

                                        nIndex++;
                                    }
                                }
                            }
                            else 
                            {
                                if (realDt.Columns.Count == 0) 
                                {
                                    CreateNewRealInventoryDataTable();
                                }

                                nEpcCount = realDt.Rows.Count;

                                if (nEpcCount < nEpcLength) 
                                {
                                    DataRow row = m_curInventoryBuffer.dtTagTable.Rows[nEpcLength - 1];
                                    
                                }
                            }
                        }
                        break;


                    case 0x00:
                    case 0x01:
                        {
                            m_bLockTab = false;

                            //TimeSpan ts = m_curInventoryBuffer.dtEndInventory - m_curInventoryBuffer.dtStartInventory;
                            //int nTotalTime = ts.Minutes * 60 * 1000 + ts.Seconds * 1000 + ts.Milliseconds;

                            //ledReal5.Text = nTotalTime.ToString();

                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void SetMaxMinRSSI(int nRSSI)
        {
            if (m_curInventoryBuffer.nMaxRSSI < nRSSI)
            {
                m_curInventoryBuffer.nMaxRSSI = nRSSI;
            }

            if (m_curInventoryBuffer.nMinRSSI == 0)
            {
                m_curInventoryBuffer.nMinRSSI = nRSSI;
            }
            else if (m_curInventoryBuffer.nMinRSSI > nRSSI)
            {
                m_curInventoryBuffer.nMinRSSI = nRSSI;
            }
        }

        private string GetFreqString(byte btFreq)
        {
            string strFreq = string.Empty;

            if (m_curSetting.btRegion == 4)
            {
                float nExtraFrequency = btFreq * m_curSetting.btUserDefineFrequencyInterval * 10;
                float nstartFrequency = ((float)m_curSetting.nUserDefineStartFrequency) / 1000;
                float nStart = nstartFrequency + nExtraFrequency / 1000;
                string strTemp = nStart.ToString("0.000");
                return strTemp;
            }
            else
            {
                if (btFreq < 0x07)
                {
                    float nStart = 865.00f + Convert.ToInt32(btFreq) * 0.5f;

                    string strTemp = nStart.ToString("0.00");

                    return strTemp;
                }
                else
                {
                    float nStart = 902.00f + (Convert.ToInt32(btFreq) - 7) * 0.5f;

                    string strTemp = nStart.ToString("0.00");

                    return strTemp;
                }
            }
        }

        private void ProcessInventoryReal(Reader.MessageTran msgTran)
        {
            string strCmd = "";
            if (msgTran.Cmd == 0x89)
            {
                strCmd = "实时盘存";
            }
            if (msgTran.Cmd == 0x8B)
            {
                strCmd = "自定义Session和Inventoried Flag盘存";
            }
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;
                WriteLog(logRichTextBox, strLog, 1);

                m_curInventoryBuffer.dtEndInventory = DateTime.Now;

                RefreshInventoryReal(0x89);
                RunLoopInventroy();
            }
            else if (msgTran.AryData.Length == 7)
            {
                m_curInventoryBuffer.nReadRate = Convert.ToInt32(msgTran.AryData[1]) * 256 + Convert.ToInt32(msgTran.AryData[2]);
                m_curInventoryBuffer.nDataCount = Convert.ToInt32(msgTran.AryData[3]) * 256 * 256 * 256 + Convert.ToInt32(msgTran.AryData[4]) * 256 * 256 + Convert.ToInt32(msgTran.AryData[5]) * 256 + Convert.ToInt32(msgTran.AryData[6]);

                m_curInventoryBuffer.dtEndInventory = DateTime.Now;

                WriteLog(logRichTextBox, strCmd, 0);
                RefreshInventoryReal(0x89);
                RunLoopInventroy();
            }
            else
            {
                m_nTotal++;
                int nLength = msgTran.AryData.Length;

                int nEpcLength = nLength - 4;
                if (m_nSessionPhaseOpened)
                {
                    nEpcLength = nLength - 6;
                }

                //Add inventory list
                string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, nEpcLength);
                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 1, 2);
                string strRSSI = string.Empty;

                if (m_nSessionPhaseOpened)
                {
                    SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nLength - 3] & 0x7F));
                    strRSSI = (msgTran.AryData[nLength - 3] & 0x7F).ToString();
                }
                else
                {
                    SetMaxMinRSSI(Convert.ToInt32(msgTran.AryData[nLength - 1] & 0x7F));
                    strRSSI = (msgTran.AryData[nLength - 1] & 0x7F).ToString();
                }

                byte btTemp = msgTran.AryData[0];
                byte btAntId = (byte)((btTemp & 0x03) + 1);
                string strPhase = string.Empty;
                if (m_nSessionPhaseOpened)
                {
                    if ((msgTran.AryData[nLength - 3] & 0x80) != 0) btAntId += 4;
                    strPhase = CCommondMethod.ByteArrayToString(msgTran.AryData, nLength - 2, 2);
                }
                else
                {
                    if ((msgTran.AryData[nLength - 1] & 0x80) != 0) btAntId += 4;
                }

                m_curInventoryBuffer.nCurrentAnt = (int)btAntId;
                string strAntId = btAntId.ToString();
                byte btFreq = (byte)(btTemp >> 2);

                string strFreq = GetFreqString(btFreq);

                DataRow[] drs = m_curInventoryBuffer.dtTagTable.Select(string.Format("COLEPC = '{0}'", strEPC));
                if (drs.Length == 0)
                {
                    DataRow row1 = m_curInventoryBuffer.dtTagTable.NewRow();
                    row1[0] = strPC;
                    row1[2] = strEPC;
                    row1[4] = strRSSI;
                    row1[5] = "1";
                    row1[6] = strFreq;
                    row1[7] = "0";
                    row1[8] = "0";
                    row1[9] = "0";
                    row1[10] = "0";
                    row1[11] = "0";
                    row1[12] = "0";
                    row1[13] = "0";
                    row1[14] = "0";
                    row1[15] = strPhase;
                    switch (btAntId)
                    {
                        case 0x01:
                            {
                                row1[7] = "1";
                            }
                            break;
                        case 0x02:
                            {
                                row1[8] = "1";
                            }
                            break;
                        case 0x03:
                            {
                                row1[9] = "1";
                            }
                            break;
                        case 0x04:
                            {
                                row1[10] = "1";
                            }
                            break;
                        case 0x05:
                            {
                                row1[11] = "1";
                            }
                            break;
                        case 0x06:
                            {
                                row1[12] = "1";
                            }
                            break;
                        case 0x07:
                            {
                                row1[13] = "1";
                            }
                            break;
                        case 0x08:
                            {
                                row1[14] = "1";
                            }
                            break;
                        default:
                            break;
                    }

                    m_curInventoryBuffer.dtTagTable.Rows.Add(row1);
                    m_curInventoryBuffer.dtTagTable.AcceptChanges();
                }
                else
                {
                    foreach (DataRow dr in drs)
                    {
                        dr.BeginEdit();
                        int nTemp = 0;

                        dr[4] = strRSSI;
                        //dr[5] = (Convert.ToInt32(dr[5]) + 1).ToString();
                        nTemp = Convert.ToInt32(dr[5]);
                        dr[5] = (nTemp + 1).ToString();
                        dr[6] = strFreq;
                        dr[15] = strPhase;
                        switch (btAntId)
                        {
                            case 0x01:
                                {
                                    //dr[7] = (Convert.ToInt32(dr[7]) + 1).ToString();
                                    nTemp = Convert.ToInt32(dr[7]);
                                    dr[7] = (nTemp + 1).ToString();
                                }
                                break;
                            case 0x02:
                                {
                                    //dr[8] = (Convert.ToInt32(dr[8]) + 1).ToString();
                                    nTemp = Convert.ToInt32(dr[8]);
                                    dr[8] = (nTemp + 1).ToString();
                                }
                                break;
                            case 0x03:
                                {
                                    //dr[9] = (Convert.ToInt32(dr[9]) + 1).ToString();
                                    nTemp = Convert.ToInt32(dr[9]);
                                    dr[9] = (nTemp + 1).ToString();
                                }
                                break;
                            case 0x04:
                                {
                                    //dr[10] = (Convert.ToInt32(dr[10]) + 1).ToString();
                                    nTemp = Convert.ToInt32(dr[10]);
                                    dr[10] = (nTemp + 1).ToString();
                                }
                                break;
                            case 0x05:
                                {
                                    //dr[7] = (Convert.ToInt32(dr[7]) + 1).ToString();
                                    nTemp = Convert.ToInt32(dr[11]);
                                    dr[11] = (nTemp + 1).ToString();
                                }
                                break;
                            case 0x06:
                                {
                                    //dr[8] = (Convert.ToInt32(dr[8]) + 1).ToString();
                                    nTemp = Convert.ToInt32(dr[12]);
                                    dr[12] = (nTemp + 1).ToString();
                                }
                                break;
                            case 0x07:
                                {
                                    //dr[9] = (Convert.ToInt32(dr[9]) + 1).ToString();
                                    nTemp = Convert.ToInt32(dr[13]);
                                    dr[13] = (nTemp + 1).ToString();
                                }
                                break;
                            case 0x08:
                                {
                                    //dr[10] = (Convert.ToInt32(dr[10]) + 1).ToString();
                                    nTemp = Convert.ToInt32(dr[14]);
                                    dr[14] = (nTemp + 1).ToString();
                                }
                                break;
                            default:
                                break;
                        }

                        dr.EndEdit();
                    }
                    m_curInventoryBuffer.dtTagTable.AcceptChanges();
                }

                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RefreshInventoryReal(0x89);
            }
        }

        private void ProcessReadTag(Reader.MessageTran msgTran)
        {
            string strCmd = "读标签";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                string strLog = strCmd + "失败，失败原因： " + strErrorCode;

                WriteLog(logRichTextBox, strLog, 1);
            }
            else
            {
                int nLen = msgTran.AryData.Length;
                int nDataLen = Convert.ToInt32(msgTran.AryData[nLen - 3]);
                int nEpcLen = Convert.ToInt32(msgTran.AryData[2]) - nDataLen - 4;

                string strPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 3, 2);
                string strEPC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5, nEpcLen);
                string strCRC = CCommondMethod.ByteArrayToString(msgTran.AryData, 5 + nEpcLen, 2);
                string strData = CCommondMethod.ByteArrayToString(msgTran.AryData, 7 + nEpcLen, nDataLen);

                byte byTemp = msgTran.AryData[nLen - 2];
                byte byAntId = (byte)((byTemp & 0x03) + 1);
                string strAntId = byAntId.ToString();

                string strReadCount = msgTran.AryData[nLen - 1].ToString();

                DataRow row = m_curOperateTagBuffer.dtTagTable.NewRow();
                row[0] = strPC;
                row[1] = strCRC;
                row[2] = strEPC;
                row[3] = strData;
                row[4] = nDataLen.ToString();
                row[5] = strAntId;
                row[6] = strReadCount;

                m_curOperateTagBuffer.dtTagTable.Rows.Add(row);
                m_curOperateTagBuffer.dtTagTable.AcceptChanges();
                RefreshOpTag(0x81);
                WriteLog(logRichTextBox, strCmd, 0);
            }
        }

        private delegate void RefreshOpTagUnsafe(byte btCmd);
        private void RefreshOpTag(byte btCmd)
        {
            if (this.InvokeRequired)
            {
                RefreshOpTagUnsafe InvokeRefresh = new RefreshOpTagUnsafe(RefreshOpTag);
                this.Invoke(InvokeRefresh, new object[] { btCmd });
            }
            else
            {
                switch (btCmd)
                {
                    case 0x81:
                    case 0x82:
                    case 0x83:
                    case 0x84:
                        break;
                    case 0x86:
                        break;
                    default:
                        break;
                }
            }
        }
        
        private void ProcessGetReaderIdentifier(Reader.MessageTran msgTran)
        {
            string strCmd = "读取读写器识别标记";
            string strErrorCode = string.Empty;
            short i;
            string readerIdentifier = "";

            if (msgTran.AryData.Length == 12)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                for (i = 0; i < 12; i++)
                {
                    readerIdentifier = readerIdentifier + string.Format("{0:X2}", msgTran.AryData[i]) + " ";


                }
                m_curSetting.btReaderIdentifier = readerIdentifier;
                RefreshReadSetting(0x68);

                WriteLog(logRichTextBox, strCmd, 0);
                return;
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);
        }

        private void ProcessSetReaderIdentifier(Reader.MessageTran msgTran)
        {
            string strCmd = "设置读写器识别标记";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(logRichTextBox, strCmd, 0);
                    return;
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);
        }

        private void ProcessGetAntDetector(Reader.MessageTran msgTran)
        {
            string strCmd = "读取天线连接检测阈值";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btAntDetector = msgTran.AryData[0];

                RefreshReadSetting(0x63);
                WriteLog(logRichTextBox, strCmd, 0);
                return;
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);
        }

        private void ProcessSetAntDetector(Reader.MessageTran msgTran)
        {
            string strCmd = "设置天线连接检测阈值";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(logRichTextBox, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);
        }

        private void ProcessSetBeeperMode(Reader.MessageTran msgTran)
        {
            string strCmd = "设置蜂鸣器模式";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(logRichTextBox, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);
        }

        private void ProcessGetOutputPower(Reader.MessageTran msgTran)
        {
            string strCmd = "取得输出功率";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btOutputPower = msgTran.AryData[0];

                RefreshReadSetting(0x77);
                WriteLog(logRichTextBox, strCmd, 0);
                return;
            }
            else if (msgTran.AryData.Length == 8)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btOutputPowers = msgTran.AryData;

                RefreshReadSetting(0x97);
                WriteLog(logRichTextBox, strCmd, 0);
                return;
            }
            else if (msgTran.AryData.Length == 4)
            {
                m_curSetting.btReadId = msgTran.ReadId;
                m_curSetting.btOutputPowers = msgTran.AryData;

                RefreshReadSetting(0x77);
                WriteLog(logRichTextBox, strCmd, 0);
                return;
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);
        }

        private void ProcessSetOutputPower(Reader.MessageTran msgTran)
        {
            string strCmd = "设置输出功率";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(logRichTextBox, strCmd, 0);

                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);
        }


        private void ProcessSetWorkAntenna(Reader.MessageTran msgTran)
        {
            int intCurrentAnt = 0;
            intCurrentAnt = m_curSetting.btWorkAntenna + 1;
            string strCmd = "设置工作天线成功,当前工作天线: 天线" + intCurrentAnt.ToString();

            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x10)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    WriteLog(logRichTextBox, strCmd, 0);

                    //校验是否盘存操作
                    if (m_bInventory)
                    {
                        RunLoopInventroy();
                    }
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);

            if (m_bInventory)
            {
                m_curInventoryBuffer.nCommond = 1;
                m_curInventoryBuffer.dtEndInventory = DateTime.Now;
                RunLoopInventroy();
            }
        }

        private void ProcessGetWorkAntenna(Reader.MessageTran msgTran)
        {
            string strCmd = "取得工作天线";
            string strErrorCode = string.Empty;

            if (msgTran.AryData.Length == 1)
            {
                if (msgTran.AryData[0] == 0x00 || msgTran.AryData[0] == 0x01 || msgTran.AryData[0] == 0x02 || msgTran.AryData[0] == 0x03
                    || msgTran.AryData[0] == 0x04 || msgTran.AryData[0] == 0x05 || msgTran.AryData[0] == 0x06 || msgTran.AryData[0] == 0x07)
                {
                    m_curSetting.btReadId = msgTran.ReadId;
                    m_curSetting.btWorkAntenna = msgTran.AryData[0];

                    RefreshReadSetting(0x75);
                    WriteLog(logRichTextBox, strCmd, 0);
                    return;
                }
                else
                {
                    strErrorCode = CCommondMethod.FormatErrorCode(msgTran.AryData[0]);
                }
            }
            else
            {
                strErrorCode = "未知错误";
            }

            string strLog = strCmd + "失败，失败原因： " + strErrorCode;
            WriteLog(logRichTextBox, strLog, 1);
        }

        private delegate void RunLoopInventoryUnsafe();
        private void RunLoopInventroy()
        {
            if (this.InvokeRequired)
            {
                RunLoopInventoryUnsafe InvokeRunLoopInventory = new RunLoopInventoryUnsafe(RunLoopInventroy);
                this.Invoke(InvokeRunLoopInventory, new object[] { });
            }
            else
            {
                //校验盘存是否所有天线均完成
                if (m_curInventoryBuffer.nIndexAntenna < m_curInventoryBuffer.lAntenna.Count - 1 || m_curInventoryBuffer.nCommond == 0)
                {
                    if (m_curInventoryBuffer.nCommond == 0)
                    {
                        m_curInventoryBuffer.nCommond = 1;

                        if (m_curInventoryBuffer.bLoopInventoryReal)
                        {
                            //m_bLockTab = true;
                            //btnInventory.Enabled = false;
                            if (m_curInventoryBuffer.bLoopCustomizedSession)//自定义Session和Inventoried Flag 
                            {
                                //reader.CustomizedInventory(m_curSetting.btReadId, m_curInventoryBuffer.btSession, m_curInventoryBuffer.btTarget, m_curInventoryBuffer.btRepeat); 
                                reader.CustomizedInventoryV2(m_curSetting.btReadId, m_curInventoryBuffer.CustomizeSessionParameters.ToArray());
                            }
                            else //实时盘存
                            {
                                reader.InventoryReal(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);

                            }
                        }
                        else
                        {
                            if (m_curInventoryBuffer.bLoopInventory)
                                reader.Inventory(m_curSetting.btReadId, m_curInventoryBuffer.btRepeat);
                        }
                    }
                    else
                    {
                        m_curInventoryBuffer.nCommond = 0;
                        m_curInventoryBuffer.nIndexAntenna++;

                        byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                        reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                        m_curSetting.btWorkAntenna = btWorkAntenna;
                    }
                }
                //校验是否循环盘存
                else if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_curInventoryBuffer.nIndexAntenna = 0;
                    m_curInventoryBuffer.nCommond = 0;

                    byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                    reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                    m_curSetting.btWorkAntenna = btWorkAntenna;
                }
            }
        }

        private delegate void WriteLogUnSafe(CustomControl.LogRichTextBox logRichTxt, string strLog, int nType);

        public void WriteLog(CustomControl.LogRichTextBox logRichTxt, string strLog, int nType)
        {
            if (this.InvokeRequired)
            {
                WriteLogUnSafe InvokeWriteLog = new WriteLogUnSafe(WriteLog);
                this.Invoke(InvokeWriteLog, new object[] { logRichTxt, strLog, nType });
            }
            else
            {
                if (nType == 0)
                {
                    logRichTxt.AppendTextEx(strLog, Color.Indigo);
                }
                else
                {
                    logRichTxt.AppendTextEx(strLog, Color.Red);
                }

                if (clearOperationLog.Checked)
                {
                    if (logRichTxt.Lines.Length > 50)
                    {
                        logRichTxt.Clear();
                    }
                }

                logRichTxt.Select(logRichTxt.TextLength, 0);
                logRichTxt.ScrollToCaret();
            }
        }

        private delegate void RefreshReadSettingUnsafe(byte btCmd);
        private void RefreshReadSetting(byte btCmd)
        {
            if (this.InvokeRequired)
            {
                RefreshReadSettingUnsafe InvokeRefresh = new RefreshReadSettingUnsafe(RefreshReadSetting);
                this.Invoke(InvokeRefresh, new object[] { btCmd });
            }
            else
            {
                switch (btCmd)
                {
                    case 0x6A:
                        break;
                    case 0x68:
                        break;
                    case 0x72:
                        break;
                    case 0x75:
                        break;
                    case 0x77:
                        {
                            if (m_curSetting.btOutputPower != 0 && m_curSetting.btOutputPowers == null)
                            {
                                signal1tb.Text = m_curSetting.btOutputPower.ToString();
                                signal2tb.Text = m_curSetting.btOutputPower.ToString();
                                signal3tb.Text = m_curSetting.btOutputPower.ToString();
                                signal4tb.Text = m_curSetting.btOutputPower.ToString();

                                m_curSetting.btOutputPower = 0;
                                m_curSetting.btOutputPowers = null;
                            }
                            else if (m_curSetting.btOutputPowers != null)
                            {
                                signal1tb.Text = m_curSetting.btOutputPowers[0].ToString();
                                signal2tb.Text = m_curSetting.btOutputPowers[1].ToString();
                                signal3tb.Text = m_curSetting.btOutputPowers[2].ToString();
                                signal4tb.Text = m_curSetting.btOutputPowers[3].ToString();

                                m_curSetting.btOutputPower = 0;
                                m_curSetting.btOutputPowers = null;
                            }
                        }
                        break;
                    case 0x97:
                        break;
                    case 0x79:
                        break;
                    case 0x7B:
                        break;
                    case 0x7D:
                        break;
                    case 0x7E:
                        break;
                    case 0x8E:
                        //Impinj Monza 标签专属
                        break;
                    case 0x60:
                        //读写 GPIO 相关
                        break;
                    case 0x63:
                        //回波损耗阈值
                        break;
                    case 0x98:
                        getMaskInitStatus();
                        break;
                    default:
                        break;
                }
            }
        }

        private void getMaskInitStatus()
        {
            byte[] maskValue = new byte[m_curSetting.btsGetTagMask.Length - 8];
            for (int i = 0; i < maskValue.Length; i++)
            {
                maskValue[i] = m_curSetting.btsGetTagMask[i + 7];
            }
            CCommondMethod.ByteArrayToString(maskValue, 0, maskValue.Length);
            ListViewItem item = new ListViewItem();
            item.Text = m_curSetting.btsGetTagMask[0].ToString();
            if (m_curSetting.btsGetTagMask[2] == 0)
            {
                item.SubItems.Add("S0");
            }
            else if (m_curSetting.btsGetTagMask[2] == 1)
            {
                item.SubItems.Add("S1");
            }
            else if (m_curSetting.btsGetTagMask[2] == 2)
            {
                item.SubItems.Add("S2");
            }
            else if (m_curSetting.btsGetTagMask[2] == 3)
            {
                item.SubItems.Add("S3");
            }
            else
            {
                item.SubItems.Add("SL");
            }

            item.SubItems.Add("0x0" + m_curSetting.btsGetTagMask[3].ToString());
            if (m_curSetting.btsGetTagMask[4] == 0)
            {
                item.SubItems.Add("Reserve");
            }
            else if (m_curSetting.btsGetTagMask[4] == 1)
            {
                item.SubItems.Add("EPC");
            }
            else if (m_curSetting.btsGetTagMask[4] == 2)
            {
                item.SubItems.Add("TID");
            }
            else
            {
                item.SubItems.Add("USER");
            }
            item.SubItems.Add(CCommondMethod.ByteArrayToString(new byte[] { m_curSetting.btsGetTagMask[5] }, 0, 1).ToString());
            item.SubItems.Add(CCommondMethod.ByteArrayToString(new byte[] { m_curSetting.btsGetTagMask[6] }, 0, 1).ToString());
            item.SubItems.Add(CCommondMethod.ByteArrayToString(maskValue, 0, maskValue.Length).ToString());
            //listView2.Items.Add(item);
        }

        private void ITUser_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            tabPage1.Enabled = false;
            ConnectToReader();
        }

        private void ConnectToReader()
        {
            try
            {
                //将IP地址写入配置文件
                if (ipAddressTb.IpAddressStr != ConfigHelper.ConfigHelper.SoftConfig.GetSoftConfig("reader_ip"))
                {
                    ConfigHelper.ConfigHelper.SoftConfig.SetSoftConfig("reader_ip", ipAddressTb.IpAddressStr);
                }
                //处理Tcp连接读写器
                string strException = string.Empty;
                IPAddress ipAddress = IPAddress.Parse(ipAddressTb.IpAddressStr);
                int nPort = 4001;

                int nRet = reader.ConnectServer(ipAddress, nPort, out strException);
                if (nRet != 0)
                {
                    string strLog = "连接读写器失败，失败原因： " + strException;
                    WriteLog(logRichTextBox, strLog, 1);

                    return;
                }
                else
                {
                    string strLog = "连接读写器 " + ipAddressTb.IpAddressStr + "@" + nPort.ToString();
                    WriteLog(logRichTextBox, strLog, 0);
                }

                //处理界面元素是否有效
                setControls(true);

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void setControls(bool isConnected) 
        {
            if (isConnected)
            {
                disconnectBtn.Enabled = true;
                btRealTimeInventory.Enabled = true;
                connectBtn.Enabled = false;
                beepSilentRadio.Enabled = true;
                beepWhenInventoryRadio.Enabled = true;
                beepWhenReadingRadio.Enabled = true;
                beepSetBtn.Enabled = true;
                signal1tb.Enabled = true;
                signal2tb.Enabled = true;
                signal3tb.Enabled = true;
                signal4tb.Enabled = true;
                readSignalBtn.Enabled = true;
                setSignalBtn.Enabled = true;
            }
            else 
            {
                startStopServiceBtn.ForeColor = Color.DarkBlue;
                startStopServiceBtn.Text = "开始扫描记录";
                btRealTimeInventory.Enabled = false;
                disconnectBtn.Enabled = false;
                connectBtn.Enabled = true;
                beepSilentRadio.Enabled = false;
                beepWhenInventoryRadio.Enabled = false;
                beepWhenReadingRadio.Enabled = false;
                beepSetBtn.Enabled = false;
                signal1tb.Enabled = false;
                signal2tb.Enabled = false;
                signal3tb.Enabled = false;
                signal4tb.Enabled = false;
                readSignalBtn.Enabled = false;
                setSignalBtn.Enabled = false;
            }
        }

        private void disconnectBtn_Click(object sender, EventArgs e)
        {
            DisconnectReader();
            tabPage1.Enabled = true;
        }

        private void DisconnectReader() 
        {
            //处理断开Tcp连接读写器
            reader.SignOut();

            //处理界面元素是否有效
            setControls(false);
        }

        private void beepSetBtn_Click(object sender, EventArgs e)
        {
            byte btBeeperMode = 0xFF;

            if (beepSilentRadio.Checked)
            {
                btBeeperMode = 0x00;
            }
            else if (beepWhenInventoryRadio.Checked)
            {
                btBeeperMode = 0x01;
            }
            else if (beepWhenReadingRadio.Checked)
            {
                btBeeperMode = 0x02;
            }
            else
            {
                return;
            }

            reader.SetBeeperMode(m_curSetting.btReadId, btBeeperMode);
            m_curSetting.btBeeperMode = btBeeperMode;
        }

        private void readSignalBtn_Click(object sender, EventArgs e)
        {

                reader.GetOutputPowerFour(m_curSetting.btReadId);
        }

        private void btRealTimeInventory_Click(object sender, EventArgs e)
        {
            testOrScan = true;
            StartInventory();
        }

        private void StartInventory() 
        {
            try
            {
                m_curInventoryBuffer.ClearInventoryPar();

                m_curInventoryBuffer.btRepeat = Convert.ToByte("1");

                m_curInventoryBuffer.bLoopCustomizedSession = false;

                if (cbRealWorkant1.Checked)
                {
                    m_curInventoryBuffer.lAntenna.Add(0x00);
                }
                if (cbRealWorkant2.Checked)
                {
                    m_curInventoryBuffer.lAntenna.Add(0x01);
                }
                if (cbRealWorkant3.Checked)
                {
                    m_curInventoryBuffer.lAntenna.Add(0x02);
                }
                if (cbRealWorkant4.Checked)
                {
                    m_curInventoryBuffer.lAntenna.Add(0x03);
                }
                if (m_curInventoryBuffer.lAntenna.Count == 0)
                {
                    MessageBox.Show("请至少选择一个天线");
                    return;
                }
                //默认循环发送命令
                if (m_curInventoryBuffer.bLoopInventory)
                {
                    m_bInventory = false;
                    m_curInventoryBuffer.bLoopInventory = false;
                    btRealTimeInventory.BackColor = Color.WhiteSmoke;
                    btRealTimeInventory.ForeColor = Color.DarkBlue;
                    btRealTimeInventory.Text = "开始扫描";
                    startStopServiceBtn.ForeColor = Color.DarkBlue;
                    startStopServiceBtn.Text = "开始扫描记录";
                    timerInventory.Enabled = false;

                    totalTime.Enabled = false;
                    return;
                }
                else
                {
                    m_bInventory = true;
                    m_curInventoryBuffer.bLoopInventory = true;
                    btRealTimeInventory.BackColor = Color.DarkBlue;
                    btRealTimeInventory.ForeColor = Color.White;
                    btRealTimeInventory.Text = "停止扫描";
                    startStopServiceBtn.ForeColor = Color.Red;
                    startStopServiceBtn.Text = "停止扫描记录";
                }

                m_curInventoryBuffer.bLoopInventoryReal = true;

                m_curInventoryBuffer.ClearInventoryRealResult();
                lvRealList.Items.Clear();
                lvRealList.Items.Clear();
                tbRealMaxRssi.Text = "0";
                tbRealMinRssi.Text = "0";
                m_nTotal = 0;


                byte btWorkAntenna = m_curInventoryBuffer.lAntenna[m_curInventoryBuffer.nIndexAntenna];
                reader.SetWorkAntenna(m_curSetting.btReadId, btWorkAntenna);
                m_curSetting.btWorkAntenna = btWorkAntenna;

                timerInventory.Enabled = true;

                totalTime.Enabled = true;

            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }  
        }

        private void startStopServiceBtn_Click(object sender, EventArgs e)
        {
            if (startStopServiceBtn.Text == "开始扫描记录")
            {
                ConnectToReader();
                StartInventory();
                tabPage2.Enabled = false;
            }
            else 
            {
                StartInventory();
                DisconnectReader();
                tabPage2.Enabled = true;
            }
        }

        private void cbRealWorkant1_CheckedChanged(object sender, EventArgs e)
        {
            ConfigHelper.ConfigHelper.SoftConfig.SetSoftConfig("antenna1", cbRealWorkant1.Checked.ToString());
        }

        private void cbRealWorkant2_CheckedChanged(object sender, EventArgs e)
        {
            ConfigHelper.ConfigHelper.SoftConfig.SetSoftConfig("antenna2", cbRealWorkant2.Checked.ToString());
        }

        private void cbRealWorkant3_CheckedChanged(object sender, EventArgs e)
        {
            ConfigHelper.ConfigHelper.SoftConfig.SetSoftConfig("antenna3", cbRealWorkant3.Checked.ToString());
        }

        private void cbRealWorkant4_CheckedChanged(object sender, EventArgs e)
        {
            ConfigHelper.ConfigHelper.SoftConfig.SetSoftConfig("antenna4", cbRealWorkant4.Checked.ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1 loginForm;
            loginForm = (Form1)this.Owner;
            loginForm.Show();
            loginForm.CleanLoginTextBox();
            this.FormClosed -= ITUser_FormClosed;
            this.Close();
        }
    }
}
