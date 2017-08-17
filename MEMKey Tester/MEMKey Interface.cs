using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;


namespace MEMKey_Tester_Interface
{
    public partial class MEMKeyProgrammer : Form
    {
        //global declarations 
        SerialPort comMEM;
        static byte[] rxData;
        static int rxFlag = 0;
        static int[] RowsColsControl;
        static int[] KeyControl;
        static int PinConflictFlag = 0;
        static int txSent = 0;
        static int comTimeout = 0;
        static string[] PCATlookup;

        struct txData
        {
            public string message;
            public byte[] txPacket;
            public int responseSize;
        }

        List<txData> txList = new List<txData>();

        public MEMKeyProgrammer()
        {
            InitializeComponent();
        }

        private void MEMKeyProgrammer_Load(object sender, EventArgs e)
        {
            comMEM = new SerialPort();
            RowsColsControl = new int[9];
            KeyControl = new int[20];
            rxData = new byte[10];
            KeyControlSet();
            RCControlSet();
            PCATlookup = new string[106] {"A","B","C","D","E","F","G","H","I","J","K","L","M","N","O","P","Q","R","S","T","U","V","W","X","Y","Z",
            "0)","1!","2@","3#","4$","5%","6^","7&","8*","9(","`~","-_","=+","[{","]}","\\|",";:","'\"",",<",".>","/?","Esc","Bksp","Tab","Caps","Enter",
            "Space","F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12","Ins","Del","Home","End","PG up","PG dn","L arr","U arr","D arr",
            "R arr","0","1","2","3","4","5","6","7","8","9",".del","+","-","*","/","Num Etr","Num Lck","Prt Scr","Scl Lck","Pause","L Sht","R Sht","L Ctrl",
            "R Ctrl","L Alt","R Alt","key a","key b","L Wndw","R Wndw","Menu"};
        }
        //*************************************************************************
        //                           Com Port Group
        //*************************************************************************
        private void cboComPort_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnComRefresh_Click(object sender, EventArgs e)
        {
            cboComPort.Items.Clear();
            foreach (string name in SerialPort.GetPortNames())
            {
                cboComPort.Items.Add(name);
            }
            if (cboComPort.Items.Count > 0)
            {
                cboComPort.SelectedIndex = 0;
            }
        }

        private void btnOpenCom_Click(object sender, EventArgs e)
        {
            // Open MODEM => PIC Serial Port
            if (cboComPort.SelectedItem != null)
            {
                comMEM.PortName = cboComPort.SelectedItem.ToString();
                comMEM.BaudRate = 2400;
                comMEM.Parity = Parity.None;
                comMEM.StopBits = StopBits.One;
                comMEM.DataBits = 8;
                comMEM.Handshake = Handshake.None;
                comMEM.RtsEnable = true;
                comMEM.DtrEnable = true;

                comMEM.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);

                try
                {
                    comMEM.Open();

                    if (comMEM.IsOpen)
                    {
                        cboComPort.Enabled = false;
                        btnComRefresh.Enabled = false;
                        btnOpenCom.Enabled = false;
                        btnCloseCom.Enabled = true;
                    }
                    else
                    {
                        lbLog.Items.Insert(0, "Failed to open port " + comMEM.PortName);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    lbLog.Items.Insert(0, "Failed to open port!\n\r" + comMEM.PortName + " appears to be opened in another application!");
                    return;
                }
            }
            else
            {
                lbLog.Items.Insert(0, "Select a com port");
            }
        }

        private void btnCloseCom_Click(object sender, EventArgs e)
        {
            if (comMEM.IsOpen)
            {

                comMEM.Close();
                cboComPort.Enabled = true;
                btnOpenCom.Enabled = true;
                btnCloseCom.Enabled = false;
            }
        }

        private static void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            rxData = new byte[sp.BytesToRead];
            sp.Read(rxData, 0, sp.BytesToRead);
            
            rxFlag = 1;
            return;
        }

        //**************************************************************************
        //                     Debounce Group
        //**************************************************************************

        private void nudDebounce_ValueChanged(object sender, EventArgs e)
        {
            if ((nudDebounce.Value % (decimal)2.5) != 0)
            {
                nudDebounce.Value = nudDebounce.Value - (nudDebounce.Value % (decimal)2.5);
            }
            tbarDebounce.Value = (int)(nudDebounce.Value / (decimal)2.5);
        }

        private void tbarDebounce_ValueChanged(object sender, EventArgs e)
        {
            nudDebounce.Value = (decimal)tbarDebounce.Value * (decimal)2.5;
        }

        private void tbarDebounce_Scroll(object sender, EventArgs e)
        {
            nudDebounce.Value = (decimal)tbarDebounce.Value * (decimal)2.5;
        }

        //**************************************************************************
        //                     Typematic Group
        //**************************************************************************

        private void nudTypeDelay_ValueChanged(object sender, EventArgs e)
        {
            if ((nudTypeDelay.Value % (decimal)2.5) != 0)
            {
                nudTypeDelay.Value = nudTypeDelay.Value - (nudTypeDelay.Value % (decimal)2.5);
            }
            tbarTypeDelay.Value = (int)(nudTypeDelay.Value / (decimal)2.5);
        }

        private void tbarTypeDelay_ValueChanged(object sender, EventArgs e)
        {
            nudTypeDelay.Value = (decimal)tbarTypeDelay.Value * (decimal)2.5;
        }

        private void tbarTypeDelay_Scroll(object sender, EventArgs e)
        {
            nudTypeDelay.Value = (decimal)tbarTypeDelay.Value * (decimal)2.5;
        }

        private void nudTypeRate_ValueChanged(object sender, EventArgs e)
        {
            if ((nudTypeRate.Value % (decimal)2.5) != 0)
            {
                nudTypeRate.Value = nudTypeRate.Value - (nudTypeRate.Value % (decimal)2.5);
            }
            tbarTypeRate.Value = (int)(nudTypeRate.Value / (decimal)2.5);
        }

        private void tbarTypeRate_ValueChanged(object sender, EventArgs e)
        {
            nudTypeRate.Value = (decimal)tbarTypeRate.Value * (decimal)2.5;
        }

        private void tbarTypeRate_Scroll(object sender, EventArgs e)
        {
            nudTypeRate.Value = (decimal)tbarTypeRate.Value * (decimal)2.5;
        }
        //**************************************************************************
        //                     Program Button
        //**************************************************************************
        private void btnProgramMEMKey_Click(object sender, EventArgs e)
        {
            if(comMEM.IsOpen)
            {
                if (chbProgramRC.Checked == true)
                {
                    RCControlSet();
                    KeyControlSet();

                    for (int x = 0; x < 9; x++)
                    {
                        for (int y = x+1; y < 9; y++)
                        {
                            if (RowsColsControl[x] == RowsColsControl[y])
                            {
                                if ((x < 5) && (y < 5) && (PinConflictFlag == 0))
                                {
                                    lbLog.Items.Insert(0, string.Format("Row {0} and Row {1} are assigned to the same pin.", x, y));
                                    PinConflictFlag = 1;
                                }
                                if ((x < 5) && (y >= 5) && (PinConflictFlag == 0))
                                {
                                    lbLog.Items.Insert(0, string.Format("Row {0} and Column {1} are assigned to the same pin.", x, y - 4));
                                    PinConflictFlag = 1;
                                }
                                if ((x >= 5) && (PinConflictFlag == 0))
                                {
                                    lbLog.Items.Insert(0, string.Format("Column {0} and Column {1} are assigned to the same pin.", x - 4, y - 4));
                                    PinConflictFlag = 1;
                                }
                            }
                        }
                    }
                }
                if (PinConflictFlag == 0)
                {
                    if (chbProgramDebounce.Checked)
                    {
                        txData txTemp = new txData();
                        txTemp.txPacket = new byte[2];
                        txTemp.message = "Debounce Value Update";
                        txTemp.txPacket[0] = 0x04;
                        txTemp.txPacket[1] = (byte)tbarDebounce.Value;
                        txTemp.responseSize = 0;

                        txList.Add(txTemp);
                    }
                    if (chbProgramType.Checked)
                    {
                        txData txTemp = new txData();
                        txTemp.txPacket = new byte[3];
                        txTemp.message = "Typmatic Values Update";
                        txTemp.txPacket[0] = 0x02;
                        txTemp.txPacket[1] = (byte)tbarTypeDelay.Value;
                        txTemp.txPacket[2] = (byte)tbarTypeRate.Value;

                        txList.Add(txTemp);
                    }
                    if (chbProgramRC.Checked)
                    {
                        txData txTemp = new txData();
                        txTemp.txPacket = new byte[10];
                        txTemp.message = "Row/Column Update";
                        txTemp.txPacket[0] = 0x06;
                        for (int x = 0; x < 9; x++)
                        {
                            txTemp.txPacket[x+1] = (byte)RowsColsControl[x];
                        }

                        txList.Add(txTemp);
                    }
                    if (chbProgramKeys.Checked)
                    {
                        for (int x = 0; x < 20; x++)
                        {
                            txData txTemp = new txData();
                            txTemp.txPacket = new byte[3];
                            txTemp.message = string.Format("Key{0} Update", x);
                            if (chbPCAT.Checked)
                            {
                                txTemp.txPacket[0] = 0x0C;
                            }
                            else
                            {
                                txTemp.txPacket[0] = 0x0A;
                            }
                            txTemp.txPacket[1] = (byte)x;
                            txTemp.txPacket[2] = (byte)KeyControl[x];

                            txList.Add(txTemp);
                        }    
                    }
                    if (chbProgramConfig.Checked)
                    {
                        txData txTemp = new txData();
                        txTemp.txPacket = new byte[2];
                        txTemp.message = "Config Byte Update";
                        txTemp.txPacket[0] = 0x0E;
                        if (chbEnableAutoResponse.Checked)
                        {
                            txTemp.txPacket[1] += 2;
                        }
                        if (chbEnableType.Checked)
                        {
                            txTemp.txPacket[1] += 4;
                        }

                        txList.Add(txTemp);
                    }
                }
                else
                {
                    PinConflictFlag = 0;
                }
            }
            else
            {
                lbLog.Items.Insert(0, "Connect to a Com Port");
            }
        }
        //**************************************************************************
        //                     Reset Button
        //**************************************************************************
        private void btnResetMemkey_Click(object sender, EventArgs e)
        {           
            if (comMEM.IsOpen)
            {
                txList.Clear();
                txData txTemp = new txData();
                txTemp.txPacket = new byte[1];
                txTemp.message = "Reset MEMKey";
                txTemp.txPacket[0] = 0x11;

                txList.Add(txTemp);
            }
            else
            {
                lbLog.Items.Insert(0, "Connect to a Com Port");
            }
        }

        public void RCControlSet()
        {
            RowsColsControl[0] = cboRow0.SelectedIndex;
            RowsColsControl[1] = cboRow1.SelectedIndex;
            RowsColsControl[2] = cboRow2.SelectedIndex;
            RowsColsControl[3] = cboRow3.SelectedIndex;
            RowsColsControl[4] = cboRow4.SelectedIndex;
            RowsColsControl[5] = cboColumn0.SelectedIndex;
            RowsColsControl[6] = cboColumn1.SelectedIndex;
            RowsColsControl[7] = cboColumn2.SelectedIndex;
            RowsColsControl[8] = cboColumn3.SelectedIndex;
        }

        public void KeyControlLoad()
        {
            nudKey0.Value = KeyControl[0];
            nudKey1.Value = KeyControl[1];
            nudKey2.Value = KeyControl[2];
            nudKey3.Value = KeyControl[3];
            nudKey4.Value = KeyControl[4];
            nudKey5.Value = KeyControl[5];
            nudKey6.Value = KeyControl[6];
            nudKey7.Value = KeyControl[7];
            nudKey8.Value = KeyControl[8];
            nudKey9.Value = KeyControl[9];
            nudKey10.Value = KeyControl[10];
            nudKey11.Value = KeyControl[11];
            nudKey12.Value = KeyControl[12];
            nudKey13.Value = KeyControl[13];
            nudKey14.Value = KeyControl[14];
            nudKey15.Value = KeyControl[15];
            nudKey16.Value = KeyControl[16];
            nudKey17.Value = KeyControl[17];
            nudKey18.Value = KeyControl[18];
            nudKey19.Value = KeyControl[19];
        }
        public void KeyControlSet()
        {
            try
            {
                KeyControl[0] = (int)nudKey0.Value;
                KeyControl[1] = (int)nudKey1.Value;
                KeyControl[2] = (int)nudKey2.Value;
                KeyControl[3] = (int)nudKey3.Value;
                KeyControl[4] = (int)nudKey4.Value;
                KeyControl[5] = (int)nudKey5.Value;
                KeyControl[6] = (int)nudKey6.Value;
                KeyControl[7] = (int)nudKey7.Value;
                KeyControl[8] = (int)nudKey8.Value;
                KeyControl[9] = (int)nudKey9.Value;
                KeyControl[10] = (int)nudKey10.Value;
                KeyControl[11] = (int)nudKey11.Value;
                KeyControl[12] = (int)nudKey12.Value;
                KeyControl[13] = (int)nudKey13.Value;
                KeyControl[14] = (int)nudKey14.Value;
                KeyControl[15] = (int)nudKey15.Value;
                KeyControl[16] = (int)nudKey16.Value;
                KeyControl[17] = (int)nudKey17.Value;
                KeyControl[18] = (int)nudKey18.Value;
                KeyControl[19] = (int)nudKey19.Value;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception Message: " + ex.Message);
            }
        }
        //**************************************************************************
        //                        Save File Drop Down Option
        //**************************************************************************
        private void saveMEMKeySettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RCControlSet();
            KeyControlSet();

            for (int x = 0; x < 9; x++)
            {
                for (int y = x + 1; y < 9; y++)
                {
                    if (RowsColsControl[x] == RowsColsControl[y])
                    {
                        if ((x < 5) && (y < 5) && (PinConflictFlag == 0))
                        {
                            lbLog.Items.Insert(0, string.Format("Row {0} and Row {1} are assigned to the same pin.", x, y));
                            PinConflictFlag = 1;
                        }
                        if ((x < 5) && (y >= 5) && (PinConflictFlag == 0))
                        {
                            lbLog.Items.Insert(0, string.Format("Row {0} and Column {1} are assigned to the same pin.", x, y - 4));
                            PinConflictFlag = 1;
                        }
                        if ((x >= 5) && (PinConflictFlag == 0))
                        {
                            lbLog.Items.Insert(0, string.Format("Column {0} and Column {1} are assigned to the same pin.", x - 4, y - 4));
                            PinConflictFlag = 1;
                        }
                    }
                }
            }

            if (PinConflictFlag == 0)
            {
                // Displays a SaveFileDialog so the user can save the Image  
                // assigned to Button2.  
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Text file|*.txt";
                saveFileDialog1.Title = "Save Location";
                saveFileDialog1.ShowDialog();

                // Create a string array with the lines of text
                string[] lines = new string[39];
                for (int i = 0; i < 29; i++)
                {
                    if (i < 20)
                    {
                        lines[i] = string.Format("{0}", KeyControl[i]);
                    }
                    else if (i < 29)
                    {
                        lines[i] = string.Format("{0}", RowsColsControl[i - 20]);
                    }
                }
                lines[29] = string.Format("{0}", tbarDebounce.Value);
                lines[30] = string.Format("{0}", tbarTypeDelay.Value);
                lines[31] = string.Format("{0}", tbarTypeRate.Value);
                lines[32] = string.Format("{0}", chbEnableAutoResponse.Checked);
                lines[33] = string.Format("{0}", chbEnableType.Checked);
                lines[34] = string.Format("{0}", chbProgramDebounce.Checked);
                lines[35] = string.Format("{0}", chbProgramKeys.Checked);
                lines[36] = string.Format("{0}", chbProgramRC.Checked);
                lines[37] = string.Format("{0}", chbProgramType.Checked);
                lines[38] = string.Format("{0}", chbProgramConfig.Checked);

                // Set a variable to the Desktop path.
                string docpath = saveFileDialog1.FileName;

                // Write the string array to a new file named "WriteLines.txt".
                using (StreamWriter outputFile = new StreamWriter(docpath))
                {
                    foreach (string element in lines)
                        outputFile.WriteLine(element);
                }
            }
            else
            {
                PinConflictFlag = 0;
            }
        }
        //**************************************************************************
        //                     Load File Drop Down Option
        //**************************************************************************
        private void loadMEMKeySettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Text file|*.txt";
            openFileDialog1.Title = "Load File";
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName != "")
            {
                string[] lines = System.IO.File.ReadAllLines(openFileDialog1.FileName);
                if (lines.Length == 39)
                {
                    nudKey0.Value = decimal.Parse(lines[0]);
                    nudKey1.Value = decimal.Parse(lines[1]);
                    nudKey2.Value = decimal.Parse(lines[2]);
                    nudKey3.Value = decimal.Parse(lines[3]);
                    nudKey4.Value = decimal.Parse(lines[4]);
                    nudKey5.Value = decimal.Parse(lines[5]);
                    nudKey6.Value = decimal.Parse(lines[6]);
                    nudKey7.Value = decimal.Parse(lines[7]);
                    nudKey8.Value = decimal.Parse(lines[8]);
                    nudKey9.Value = decimal.Parse(lines[9]);
                    nudKey10.Value = decimal.Parse(lines[10]);
                    nudKey11.Value = decimal.Parse(lines[11]);
                    nudKey12.Value = decimal.Parse(lines[12]);
                    nudKey13.Value = decimal.Parse(lines[13]);
                    nudKey14.Value = decimal.Parse(lines[14]);
                    nudKey15.Value = decimal.Parse(lines[15]);
                    nudKey16.Value = decimal.Parse(lines[16]);
                    nudKey17.Value = decimal.Parse(lines[17]);
                    nudKey18.Value = decimal.Parse(lines[18]);
                    nudKey19.Value = decimal.Parse(lines[19]);

                    cboRow0.SelectedIndex = int.Parse(lines[20]);
                    cboRow1.SelectedIndex = int.Parse(lines[21]);
                    cboRow2.SelectedIndex = int.Parse(lines[22]);
                    cboRow3.SelectedIndex = int.Parse(lines[23]);
                    cboRow4.SelectedIndex = int.Parse(lines[24]);
                    cboColumn0.SelectedIndex = int.Parse(lines[25]);
                    cboColumn1.SelectedIndex = int.Parse(lines[26]);
                    cboColumn2.SelectedIndex = int.Parse(lines[27]);
                    cboColumn3.SelectedIndex = int.Parse(lines[28]);

                    tbarDebounce.Value = int.Parse(lines[29]);
                    tbarTypeDelay.Value = int.Parse(lines[30]);
                    tbarTypeRate.Value = int.Parse(lines[31]);

                    chbEnableAutoResponse.Checked = bool.Parse(lines[32]);
                    chbEnableType.Checked = bool.Parse(lines[33]);
                    chbProgramDebounce.Checked = bool.Parse(lines[34]);
                    chbProgramKeys.Checked = bool.Parse(lines[35]);
                    chbProgramRC.Checked = bool.Parse(lines[36]);
                    chbProgramType.Checked = bool.Parse(lines[37]);
                    chbProgramConfig.Checked = bool.Parse(lines[38]);
                }
                else
                {
                    MessageBox.Show("File is not formatted correctly");
                }
            }
        }
        //**************************************************************************
        //                     Reset Values Drop Down Option
        //**************************************************************************
        private void resetAllValuesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cboColumn0.SelectedItem = null;
            cboColumn1.SelectedItem = null;
            cboColumn2.SelectedItem = null;
            cboColumn3.SelectedItem = null;
            cboRow0.SelectedItem = null;
            cboRow1.SelectedItem = null;
            cboRow2.SelectedItem = null;
            cboRow3.SelectedItem = null;
            cboRow4.SelectedItem = null;

            nudKey0.Value = 0;
            nudKey1.Value = 0;
            nudKey2.Value = 0;
            nudKey3.Value = 0;
            nudKey4.Value = 0;
            nudKey5.Value = 0;
            nudKey6.Value = 0;
            nudKey7.Value = 0;
            nudKey8.Value = 0;
            nudKey9.Value = 0;
            nudKey10.Value = 0;
            nudKey11.Value = 0;
            nudKey12.Value = 0;
            nudKey13.Value = 0;
            nudKey14.Value = 0;
            nudKey15.Value = 0;
            nudKey16.Value = 0;
            nudKey17.Value = 0;
            nudKey18.Value = 0;
            nudKey19.Value = 0;

            nudDebounce.Value = (decimal)2.5;
            nudTypeDelay.Value = (decimal)2.5;
            nudTypeRate.Value = (decimal)2.5;

            chbEnableAutoResponse.Checked = true;
            chbEnableType.Checked = true;
            chbProgramDebounce.Checked = true;
            chbProgramKeys.Checked = true;
            chbProgramRC.Checked = true;
            chbProgramType.Checked = true;
            chbProgramConfig.Checked = true;
        }
        //**************************************************************************
        //                     Default Values Drop Down Option
        //**************************************************************************
        private void setDefaultValuesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            cboColumn0.SelectedIndex = 4;
            cboColumn1.SelectedIndex = 5;
            cboColumn2.SelectedIndex = 6;
            cboColumn3.SelectedIndex = 7;
            cboRow0.SelectedIndex = 0;
            cboRow1.SelectedIndex = 1;
            cboRow2.SelectedIndex = 2;
            cboRow3.SelectedIndex = 3;
            cboRow4.SelectedIndex = 8;

            nudKey0.Value = 0;
            nudKey1.Value = 1;
            nudKey2.Value = 2;
            nudKey3.Value = 3;
            nudKey4.Value = 4;
            nudKey5.Value = 5;
            nudKey6.Value = 6;
            nudKey7.Value = 7;
            nudKey8.Value = 8;
            nudKey9.Value = 9;
            nudKey10.Value = 10;
            nudKey11.Value = 11;
            nudKey12.Value = 12;
            nudKey13.Value = 13;
            nudKey14.Value = 14;
            nudKey15.Value = 15;
            nudKey16.Value = 16;
            nudKey17.Value = 17;
            nudKey18.Value = 18;
            nudKey19.Value = 19;

            nudDebounce.Value = (decimal)10;
            nudTypeDelay.Value = (decimal)500;
            nudTypeRate.Value = (decimal)100;

            chbEnableAutoResponse.Checked = true;
            chbEnableType.Checked = true;
            chbProgramDebounce.Checked = true;
            chbProgramKeys.Checked = true;
            chbProgramRC.Checked = true;
            chbProgramType.Checked = true;
            chbProgramConfig.Checked = true;
        }
        //**************************************************************************
        //             Read out the Values the memkey is currently using
        //**************************************************************************
        private void btnReadMEMKey_Click(object sender, EventArgs e)
        {
            try
            {
                if (comMEM.IsOpen)
                {
                    txData txTemp;

                    //read key vals
                    for (int x = 0; x < 20; x++)
                    {
                        txTemp = new txData();
                        txTemp.txPacket = new byte[2];
                        txTemp.message = string.Format("Read Key{0} Value", x);
                        txTemp.responseSize = 1;
                        if (chbPCAT.Checked)
                        {
                            txTemp.txPacket[0] = 0x0D;
                        }
                        else
                        {
                            txTemp.txPacket[0] = 0x0B;
                        }
                        txTemp.txPacket[1] = (byte)x;
                        txList.Add(txTemp);
                    }

                    //read row col vals 
                    txTemp = new txData();
                    txTemp.txPacket = new byte[1];
                    txTemp.message = "Read Row/Column Assignments";
                    txTemp.responseSize = 9;
                    txTemp.txPacket[0] = 0x07;

                    txList.Add(txTemp);

                    //read typematic  
                    txTemp = new txData();
                    txTemp.txPacket = new byte[1];
                    txTemp.message = "Read Typematic Values";
                    txTemp.responseSize = 2;
                    txTemp.txPacket[0] = 0x03;

                    txList.Add(txTemp);

                    //read debounce  
                    txTemp = new txData();
                    txTemp.txPacket = new byte[1];
                    txTemp.message = "Read Debounce Value";
                    txTemp.responseSize = 1;
                    txTemp.txPacket[0] = 0x05;

                    txList.Add(txTemp);

                    //read control byte 
                    txTemp = new txData();
                    txTemp.txPacket = new byte[1];
                    txTemp.message = "Read Config Byte";
                    txTemp.responseSize = 1;
                    txTemp.txPacket[0] = 0x0F;

                    txList.Add(txTemp);
                }
                else
                {
                    lbLog.Items.Insert(0, "Connect to a Com Port");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception Message: " + ex.Message);
            }

        }

        private void nudKey0_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey0.Value < 105)
            {
                lblPCATkey0.Text = PCATlookup[(int)nudKey0.Value];
            }
            else
            {
                lblPCATkey0.Text = null;
            }
        }

        private void nudKey1_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey1.Value < 105)
            {
                lblPCATkey1.Text = PCATlookup[(int)nudKey1.Value];
            }
            else
            {
                lblPCATkey1.Text = null;
            }
        }

        private void nudKey2_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey2.Value < 105)
            {
                lblPCATkey2.Text = PCATlookup[(int)nudKey2.Value];
            }
            else
            {
                lblPCATkey2.Text = null;
            }
        }

        private void nudKey3_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey3.Value < 105)
            {
                lblPCATkey3.Text = PCATlookup[(int)nudKey3.Value];
            }
            else
            {
                lblPCATkey3.Text = null;
            }
        }

        private void nudKey4_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey4.Value < 105)
            {
                lblPCATkey4.Text = PCATlookup[(int)nudKey4.Value];
            }
            else
            {
                lblPCATkey4.Text = null;
            }
        }

        private void nudKey5_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey5.Value < 105)
            {
                lblPCATkey5.Text = PCATlookup[(int)nudKey5.Value];
            }
            else
            {
                lblPCATkey5.Text = null;
            }
        }

        private void nudKey6_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey6.Value < 105)
            {
                lblPCATkey6.Text = PCATlookup[(int)nudKey6.Value];
            }
            else
            {
                lblPCATkey6.Text = null;
            }
        }

        private void nudKey7_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey7.Value < 105)
            {
                lblPCATkey7.Text = PCATlookup[(int)nudKey7.Value];
            }
            else
            {
                lblPCATkey7.Text = null;
            }
        }

        private void nudKey8_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey8.Value < 105)
            {
                lblPCATkey8.Text = PCATlookup[(int)nudKey8.Value];
            }
            else
            {
                lblPCATkey8.Text = null;
            }
        }

        private void nudKey9_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey9.Value < 105)
            {
                lblPCATkey9.Text = PCATlookup[(int)nudKey9.Value];
            }
            else
            {
                lblPCATkey9.Text = null;
            }
        }

        private void nudKey10_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey10.Value < 105)
            {
                lblPCATkey10.Text = PCATlookup[(int)nudKey10.Value];
            }
            else
            {
                lblPCATkey10.Text = null;
            }
        }

        private void nudKey11_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey11.Value < 105)
            {
                lblPCATkey11.Text = PCATlookup[(int)nudKey11.Value];
            }
            else
            {
                lblPCATkey11.Text = null;
            }
        }

        private void nudKey12_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey12.Value < 105)
            {
                lblPCATkey12.Text = PCATlookup[(int)nudKey12.Value];
            }
            else
            {
                lblPCATkey12.Text = null;
            }
        }

        private void nudKey13_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey13.Value < 105)
            {
                lblPCATkey13.Text = PCATlookup[(int)nudKey13.Value];
            }
            else
            {
                lblPCATkey13.Text = null;
            }
        }

        private void nudKey14_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey14.Value < 105)
            {
                lblPCATkey14.Text = PCATlookup[(int)nudKey14.Value];
            }
            else
            {
                lblPCATkey14.Text = null;
            }
        }

        private void nudKey15_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey15.Value < 105)
            {
                lblPCATkey15.Text = PCATlookup[(int)nudKey15.Value];
            }
            else
            {
                lblPCATkey15.Text = null;
            }
        }

        private void nudKey16_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey1.Value < 105)
            {
                lblPCATkey16.Text = PCATlookup[(int)nudKey16.Value];
            }
            else
            {
                lblPCATkey16.Text = null;
            }
        }

        private void nudKey17_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey17.Value < 105)
            {
                lblPCATkey17.Text = PCATlookup[(int)nudKey17.Value];
            }
            else
            {
                lblPCATkey17.Text = null;
            }
        }

        private void nudKey18_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey18.Value < 105)
            {
                lblPCATkey18.Text = PCATlookup[(int)nudKey18.Value];
            }
            else
            {
                lblPCATkey18.Text = null;
            }
        }

        private void nudKey19_ValueChanged(object sender, EventArgs e)
        {
            if (nudKey19.Value < 105)
            {
                lblPCATkey19.Text = PCATlookup[(int)nudKey19.Value];
            }
            else
            {
                lblPCATkey19.Text = null;
            }
        }

        private void parseTMR_Tick(object sender, EventArgs e)
        {
            if (comMEM.IsOpen)
            {
                if (txList.Count != 0)
                {
                    if (comTimeout < 10)
                    {
                        if ((txSent == 0) && (rxFlag == 0)) //Send next packet
                        {
                            txSent = 1;
                            comTimeout = 0;
                            comMEM.DiscardInBuffer();
                            lbLog.Items.Insert(0,txList[0].message);
                            if (txList[0].responseSize != 0)
                            {
                                comMEM.ReceivedBytesThreshold = txList[0].responseSize;
                            }
                            comMEM.Write(txList[0].txPacket, 0, txList[0].txPacket.Length);
                        }
                        if ((txSent == 0) && (rxFlag == 1)) //Ignore
                        {
                            rxFlag = 0;
                        }
                        if (txList[0].responseSize != 0)
                        {
                            if ((txSent == 1) && (rxFlag == 0)) //increment timeout
                            {
                                comTimeout++;
                            }
                            if ((txSent == 1) && (rxFlag == 1)) //Parse RX
                            {
                                switch (txList[0].txPacket[0])
                                {
                                    case 0x03:
                                        tbarTypeDelay.Value = rxData[0];
                                        tbarTypeRate.Value = rxData[1];
                                        break;
                                    case 0x05:
                                        tbarDebounce.Value = rxData[0];
                                        break;
                                    case 0x07:
                                        cboRow0.SelectedIndex = rxData[0];
                                        cboRow1.SelectedIndex = rxData[1];
                                        cboRow2.SelectedIndex = rxData[2];
                                        cboRow3.SelectedIndex = rxData[3];
                                        cboRow4.SelectedIndex = rxData[4];
                                        cboColumn0.SelectedIndex = rxData[5];
                                        cboColumn1.SelectedIndex = rxData[6];
                                        cboColumn2.SelectedIndex = rxData[7];
                                        cboColumn3.SelectedIndex = rxData[8];
                                        break;
                                    case 0x0B:
                                        KeyControlSet();
                                        KeyControl[txList[0].txPacket[1]] = rxData[0];
                                        KeyControlLoad();
                                        break;
                                    case 0x0D:
                                        KeyControlSet();
                                        KeyControl[txList[0].txPacket[1]] = rxData[0];
                                        KeyControlLoad();
                                        break;
                                    case 0x0F:
                                        if ((rxData[0] & 0x02) == 0)
                                        {
                                            chbEnableAutoResponse.Checked = false;
                                        }
                                        else
                                        {
                                            chbEnableAutoResponse.Checked = true;
                                        }
                                        if ((rxData[0] & 0x04) == 0)
                                        {
                                            chbEnableType.Checked = false;
                                        }
                                        else
                                        {
                                            chbEnableType.Checked = true;
                                        }
                                        break;
                                    default:
                                        lbLog.Items.Insert(0,string.Format("{0},{1},{2}", rxData[0], rxData[1], rxData[2]));
                                        break;
                                }
                                txList.RemoveAt(0);
                                txSent = 0;
                                rxFlag = 0;
                            }
                        }
                        else
                        {
                            txSent = 0;
                            rxFlag = 0;
                            txList.RemoveAt(0);
                        }

                    }
                    else
                    {
                        comTimeout = 0;
                        lbLog.Items.Insert(0,"Timeout");
                        txList.Clear();
                    }
                }
            }
        }
    }
}
