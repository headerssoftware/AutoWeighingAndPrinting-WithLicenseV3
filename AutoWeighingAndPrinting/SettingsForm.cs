using System;
using System.Windows.Forms;
using System.IO.Ports;
using System.Drawing;

namespace AutoWeighingAndPrinting
{
    public partial class SettingsForm : Form
    {
        bool _continueReading = false;
        delegate void SetTextCallback(double value, string text);

        readonly Config config;
        readonly AppSettings appSettings;

        public SettingsForm()
        {
            InitializeComponent();
            config = new Config();
            appSettings = config.ReadSettings();
        }

        private void SetText(double value, string message)
        {
            try
            {
                if (message != "SUCCESS")
                {
                    lblWeight.Text = message;           // Show error message
                    lblWeight.BackColor = Color.Red;
                }
                else
                {
                    int divideValue = Convert.ToInt32(appSettings.DivideValue);
                    double weight = value / divideValue;
                    lblWeight.Text = weight.ToString("0.0000") + " KG";
                    lblWeight.BackColor = Color.Cyan;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error - Set Text", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            //tbPortName.Text = PS.Default.PortName;
            //cbBaudRate.Text = PS.Default.BaudRate.ToString();
            //cbParity.Text = PS.Default.Parity.ToString();
            //cbDataBits.Text = PS.Default.DataBits.ToString();
            //cbStopBits.Text = PS.Default.StopBits.ToString();
            //tbNewLine.Text = PS.Default.NewLine;
            //tbDivide.Text = PS.Default.DivideValue.ToString();
            //tbSQLiteDBPath.Text = PS.Default.DBFilePath.ToString();

            tbPortName.Text = appSettings.PortName;
            cbBaudRate.Text = appSettings.BaudRate.ToString();
            cbParity.Text = appSettings.Parity;
            cbDataBits.Text = appSettings.DataBits.ToString();
            cbStopBits.Text = appSettings.StopBits;
            tbNewLine.Text = appSettings.NewLine;
            tbDivide.Text = appSettings.DivideValue;
            tbSQLiteDBPath.Text = appSettings.DBFilePath;
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;

            btnConnect.Enabled = false;

            if (btnConnect.Text == "Connect")
            {

                // If serial port is open, then close it
                if (_serialPort.IsOpen)
                {
                    _serialPort.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(SerialPort_DataReceived);

                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    _serialPort.BaseStream.Flush();
                    _serialPort.BaseStream.Dispose();

                    
                    _serialPort.Close();
                    _serialPort.Dispose();
                }

                //PS.Default.PortName = tbPortName.Text;
                //PS.Default.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                //PS.Default.Parity = (Parity)Enum.Parse(typeof(Parity), cbParity.Text, true);
                //PS.Default.DataBits = Convert.ToInt32(cbDataBits.Text);
                //PS.Default.StopBits = (StopBits)Enum.Parse(typeof(StopBits), cbStopBits.Text, true);
                //PS.Default.NewLine = tbNewLine.Text;
                //PS.Default.DivideValue = Convert.ToInt32(tbDivide.Text);
                //PS.Default.Save();

                appSettings.PortName = tbPortName.Text;
                appSettings.BaudRate = Convert.ToInt32(cbBaudRate.Text);
                appSettings.Parity = cbParity.Text;
                appSettings.DataBits = Convert.ToInt32(cbDataBits.Text);
                appSettings.StopBits = cbStopBits.Text;
                appSettings.NewLine = tbNewLine.Text;
                appSettings.DivideValue = tbDivide.Text;
                appSettings.DBFilePath = tbSQLiteDBPath.Text;
                config.WriteSettings(appSettings);

                _serialPort.PortName = appSettings.PortName;
                _serialPort.BaudRate = appSettings.BaudRate;
                _serialPort.Parity = (Parity)Enum.Parse(typeof(Parity), appSettings.Parity);
                _serialPort.DataBits = appSettings.DataBits;
                _serialPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), appSettings.StopBits);
                if (appSettings.NewLine == "none")
                {
                    _serialPort.NewLine = "\r\n";
                }
                else
                {
                    _serialPort.NewLine = appSettings.NewLine;
                }
                _serialPort.ReadTimeout = 8000;

                // try to open the selected port:
                try
                {
                    // If serial port is not opened, then open it

                    if (!_serialPort.IsOpen)
                    {
                       
                        _continueReading = true;
                        gbPortSettings.Enabled = false;
                        btnConnect.Text = "Disconnect";

                        _serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(SerialPort_DataReceived);
                        _serialPort.Open();

                        //ProcessReceivedData();
                    }
                }
                catch
                {
                    _continueReading = false;
                    Cursor = Cursors.Default;
                    MessageBox.Show("Serial port " + _serialPort.PortName + " cannot be opened!", "RS232 tester", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
            else if (btnConnect.Text == "Disconnect")
            {
                Cursor = Cursors.WaitCursor;

                try
                {
                    _continueReading = false;

                    if (_serialPort.IsOpen)
                    {
                        _serialPort.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(SerialPort_DataReceived);

                        _serialPort.DiscardInBuffer();
                        _serialPort.DiscardOutBuffer();

                        _serialPort.BaseStream.Flush();
                        _serialPort.BaseStream.Dispose();

                        
                        _serialPort.Close();
                        _serialPort.Dispose();
                    }


                    lblWeight.Text = "Disconnected";
                    lblWeight.BackColor = Color.Red;

                    gbPortSettings.Enabled = true;
                    btnConnect.Text = "Connect";

                }
                catch (Exception ex)
                {
                    _continueReading = false;
                    Cursor = Cursors.Default;
                    MessageBox.Show("Serial port " + _serialPort.PortName + " cannot be Closed!\n" + ex.Message, "RS232 tester", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }

            Cursor = Cursors.Default;

            btnConnect.Enabled = true;

        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!_serialPort.IsOpen || (_continueReading == false)) return; // We can't receive data if the port has already been closed.  This prevents IO Errors from being half way through receiving data when the port is closed.

            string InputData = "";
            double value = 0;
            string NumbersOnlyString = "";

            try
            {
                if (_serialPort.IsOpen && _continueReading)
                {
                    InputData = _serialPort.ReadLine();
                    InputData = InputData.Trim();

                    if (InputData[0] == '-')
                        NumbersOnlyString += InputData[0];

                    for (int i = 0; i < InputData.Length; i++)
                    {
                        if (InputData[i] >= '0' && InputData[i] <= '9')
                            NumbersOnlyString += InputData[i];
                    }

                    if (NumbersOnlyString.Length > 0)
                    {
                        value = double.Parse(NumbersOnlyString);
                    }

                    BeginInvoke(new SetTextCallback(SetText), new object[] { value, "SUCCESS" });
                }

            }
            catch (System.TimeoutException)
            {
                BeginInvoke(new SetTextCallback(SetText), new object[] { 0, "Timeout Error" });
            }
            catch (System.IO.IOException)
            {
                BeginInvoke(new SetTextCallback(SetText), new object[] { 0, "IO Error" });
            }
            catch (System.InvalidOperationException)
            {
                BeginInvoke(new SetTextCallback(SetText), new object[] { 0, "Invalid Operation" });
            }
            catch (Exception ex)
            {
                BeginInvoke(new SetTextCallback(SetText), new object[] { 0, "Exception: " + ex.Message });
            }
        }


        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    _serialPort.BaseStream.Flush();
                    _serialPort.BaseStream.Dispose();

                    _serialPort.DataReceived -= new System.IO.Ports.SerialDataReceivedEventHandler(SerialPort_DataReceived);
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
            }
            catch
            {
                MessageBox.Show("Serial port " + _serialPort.PortName + " cannot be Closed!", "RS232 tester", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            btnConnect.Text = "Connect";
        }

        private void TbDivide_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Allow only integers
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            // open file dialog   
            OpenFileDialog open = new OpenFileDialog
            {
                // image filters  
                Filter = "SQLite DB File(*.db;)|*.db"
            };
            if (open.ShowDialog() == DialogResult.OK)
            {
                tbSQLiteDBPath.Text = open.FileName;
                appSettings.DBFilePath = open.FileName;
                config.WriteSettings(appSettings);


            }
        }
    }
}
