using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReaderB;

namespace ProductionSecurityControlSystem.UHFReaderHelper
{
    public class ReaderHelper
    {
        private int comPort;
        private int openResult;
        private int fCmdRet;
        private int comPortIndex;
        private string fInventory_EPC_List;
        private byte comAdr;
        private byte baud;
        private bool portIsOpen;

        public string EPCResult { get; set; }

        public ReaderHelper() 
        {
            openResult = 30;
            fCmdRet = 30;
            comPort = 0;
            comAdr = 0xff;
            baud = 5;
            portIsOpen = false;
        }

        public void ReadingEPC() 
        {
            if (!portIsOpen)
            {
                OpenPort(ref comPort, ref comAdr, ref baud, ref comPortIndex);
                Inventory(ref comAdr);
            }
            else 
            {
                Inventory(ref comAdr);
            }
        }

        private void OpenPort(ref int port, ref byte fComAdr, ref byte baud, ref int frmComPortIndex) 
        {
            try
            {
                openResult = StaticClassReaderB.AutoOpenComPort(ref port, ref fComAdr, baud, ref frmComPortIndex);
                //comPortIndex = frmComPortIndex;

                if (openResult != 0)
                {
                    StaticClassReaderB.CloseSpecComPort(comPortIndex);
                    portIsOpen = false;
                    throw new Exception("Open COM is error.");
                }

                byte powerDbm = Convert.ToByte(ConfigHelper.ConfigHelper.SoftConfig.GetSoftConfig("ReaderPower"));
                fCmdRet = StaticClassReaderB.SetPowerDbm(ref fComAdr, powerDbm, frmComPortIndex);
                if (fCmdRet != 0) 
                {
                    StaticClassReaderB.CloseSpecComPort(comPortIndex);
                    portIsOpen = false;
                    throw new Exception("Initial Power is error!");
                }
                portIsOpen = true;
            }
            catch (Exception error) 
            {
                throw error;
            }
        }

        public void CloseReaderComPort() 
        {
            if (comPortIndex != null)
            {
                StaticClassReaderB.CloseSpecComPort(comPortIndex);
            }
            else 
            {
                throw new Exception("Close COM is error.");
            }
        }

        private void Inventory(ref byte fComAdr) 
        {
            int cardNum = 0;
            int totalLen = 0;
            int EPClen, m;
            byte[] EPC = new byte[5000];
            int CardIndex;
            string temps;
            string sEPC;
            //fIsInventoryScan = true;
            byte AdrTID = 0;
            byte LenTID = 0;
            byte TIDFlag = 0;
            string EtagID = "";

            fCmdRet = StaticClassReaderB.Inventory_G2(ref fComAdr, AdrTID, LenTID,TIDFlag, EPC, ref totalLen, ref cardNum, comPortIndex);
            if ((fCmdRet == 1) | (fCmdRet == 2) | (fCmdRet == 3) | (fCmdRet == 4) | (fCmdRet == 0xFB))//代表已查找结束
            {
                byte[] daw = new byte[totalLen];
                Array.Copy(EPC , daw, totalLen);
                temps = ByteArrayToHexString(daw);
                fInventory_EPC_List = temps;
                m = 0;

                if (cardNum == 0) 
                {
                    return;
                }
                for (CardIndex = 0; CardIndex < cardNum; CardIndex++) 
                {
                    EPClen = daw[m];
                    sEPC = temps.Substring(m * 2 + 2, EPClen * 2);
                    EPCResult = sEPC;
                    EtagID = sEPC;
                }
            }
        }

        private string ByteArrayToHexString(byte[] data)  //convert bytearray to HEX
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();
        }
    }
}
