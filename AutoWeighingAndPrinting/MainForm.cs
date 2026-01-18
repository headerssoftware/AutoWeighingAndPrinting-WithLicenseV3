using System;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using AutoWeighingAndPrinting.Data;
using AutoWeighingAndPrinting.Helpers;
using DocumentFormat.OpenXml.Wordprocessing;
using Color = System.Drawing.Color;

namespace AutoWeighingAndPrinting
{
    public partial class MainForm : Form
    {
        bool _continueReading = false;
        delegate void SetTextCallback(double value, string text);


        bool enableChildPartsWeighing = false;
        bool enableAssemblyPartsWeighing = false;

        string REPEAT_ReferenceNumber = "";
        string REPEAT_RequiredQuantity = "";
        string REPEAT_ChildPalletWeight = "";

        string REPEAT_AssemblyPartNumber = "";
        string REPEAT_AssemPalletWeight = "";

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr hwnd, ref int lpdwProcessId);

        readonly Config config;
        AppSettings appSettings;
        readonly string connectionString;


        [Obsolete]
        public MainForm()
        {
            InitializeComponent();

            config = new Config();
            appSettings = config.ReadSettings();
            connectionString = @"DataSource=" + appSettings.DBFilePath + ";foreign keys=true;";

        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            if (!CheckAppLicense())
            {
                this.Dispose();
                return;
            }

            DateTime historyDeletedDate = appSettings.HistoryDeletedDate;
            if (historyDeletedDate.Date >= DateTime.Now.Date)
            {
                //Do nothing
            }
            else
            {
                Cursor = Cursors.WaitCursor;

                try
                {
                    //Delete the history from database based on Delete History Days Value
                    DateTime deleteBeforeDate = DateTime.Now.AddDays(-appSettings.DeleteHistoryDays);


                    using (var conn = new SQLiteConnection(connectionString, true))
                    {
                        // Be sure you already created the Person Table!
                        conn.Open();
                        using (var cmd = new SQLiteCommand(conn))
                        {
                            using (var transaction = conn.BeginTransaction())
                            {
                                cmd.CommandText =
                                    @"DELETE FROM [4.childpart_history] where date_time <= @date_time;" +
                                    @"DELETE FROM [5.assembly_history] where date_time <= @date_time;";
                                cmd.Parameters.AddWithValue("@date_time", deleteBeforeDate.ToString("yyyy-MM-dd 23:59:59"));

                                cmd.ExecuteNonQuery();

                                transaction.Commit();
                            }
                        }
                        conn.Close();
                    }

                    appSettings.HistoryDeletedDate = DateTime.Now.Date;
                    config.WriteSettings(appSettings);
                }
                catch (Exception ex)
                {
                    Cursor = Cursors.Default;
                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);

                }
                Cursor = Cursors.Default;
            }
        }

        private bool ShowEnterLicenseKeyDialog(LicenseStatus licenseStatus,DateTime? licenseExpiryDate)
        {
            frmEnterLicenseKeyPopup licenseKeyPopup = new frmEnterLicenseKeyPopup(appSettings, licenseStatus, licenseExpiryDate);
            licenseKeyPopup.ShowDialog();
            if (licenseKeyPopup.IsLicenseKeyVerified)
            {
                appSettings = config.ReadSettings();
                SetLicenseInfoText(licenseKeyPopup.LicenseDetailResult);
                return true;
            }
            else
            {
                return false;
            }
        }
        private bool CheckAppLicense()
        {
            try
            {
                string licensekey = appSettings.LicenseKey;

                if (string.IsNullOrEmpty(licensekey))
                {
                    return ShowEnterLicenseKeyDialog(LicenseStatus.NewUser, null) ;
                }

                var appLicensevalidator = new AppLicenseValidator();

                LicenseDetail licenseDetail = appLicensevalidator.GetAppLicenseDetails(licensekey);

                if (licenseDetail == null)
                {
                    return ShowEnterLicenseKeyDialog(LicenseStatus.NotValid, null);
                }
                else if (!licenseDetail.IsLicenseValid)
                {
                    return ShowEnterLicenseKeyDialog(LicenseStatus.NotValid, null);
                }
                else if (licenseDetail.DaysLeft < 0)
                {
                    return ShowEnterLicenseKeyDialog(LicenseStatus.Expired, licenseDetail.ExpiryDate);
                }
                else
                {
                    return SetLicenseInfoText(licenseDetail);
                }
            }
            catch(Exception ex)
            {               
                MessageBox.Show(ex.Message, "Error Checking App License", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
           

        }

        private bool SetLicenseInfoText(LicenseDetail licenseDetail)
        {
            var daysLeft = licenseDetail.DaysLeft;

            DateTime ExpiryDate = licenseDetail.ExpiryDate;

            if (daysLeft == 0)
            {
                lblLicenseExpirationInfo.ForeColor = Color.Red;
                lblLicenseExpirationInfo.Text = $"Your License will expire today ({ExpiryDate.ToString("dd-MM-yyyy")}) ";
                lblLicenseExpirationInfo.Visible = true;
            }
            else if (daysLeft == 1)
            {
                lblLicenseExpirationInfo.ForeColor = Color.Red;
                lblLicenseExpirationInfo.Text = $"Your License will expire in {daysLeft} day ({ExpiryDate.ToString("dd-MM-yyyy")}) ";
                lblLicenseExpirationInfo.Visible = true;

            }
            else if (daysLeft <= 15)
            {
                lblLicenseExpirationInfo.ForeColor = Color.DarkOrange;
                lblLicenseExpirationInfo.Text = $"Your License will expire in {daysLeft} days ({ExpiryDate.ToString("dd-MM-yyyy")}) ";
                lblLicenseExpirationInfo.Visible = true;

            }
            else
            {
                lblLicenseExpirationInfo.ForeColor = Color.Black;
                lblLicenseExpirationInfo.Text = $"Your License is valid till {ExpiryDate.ToString("dd-MM-yyyy")} ";
                lblLicenseExpirationInfo.Visible = true;
            }

            return true;
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

        [Obsolete]
        private void BtnMasterData_Click(object sender, EventArgs e)
        {
            MasterDataForm masterDataForm = new MasterDataForm();
            masterDataForm.ShowDialog();
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {

            SettingsForm settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
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

                appSettings = config.ReadSettings();
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
                _serialPort.ReadTimeout = 4000;

                // try to open the selected port:
                try
                {
                    // If serial port is not opened, then open it

                    if (!_serialPort.IsOpen)
                    {
                        _continueReading = true;
                        btnConnect.Text = "Disconnect";

                        _serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(SerialPort_DataReceived);
                        _serialPort.Open();

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
            if (!_serialPort.IsOpen || (_continueReading == false))
                return; // We can't receive data if the port has already been closed.  This prevents IO Errors from being half way through receiving data when the port is closed.

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

        private void BtnChildPartSubmit_Click(object sender, EventArgs e)
        {
            enableChildPartsWeighing = false;
            if (tbReferenceNumber.Text == string.Empty || tbRequiredQty.Text == string.Empty)
            {
                MessageBox.Show("Enter the reference number and required quantity", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;

                string referenceNumber = tbReferenceNumber.Text;

                DataTable dtChildParts = new DataTable();
                string sql = @"SELECT reference_number as ReferenceNumber,childpart_id as ID, child_part_number as PartNumber, child_part_description as Description, part_weight as PartWeight,
                                tol_percent AS TolerancePercent, tol_part_weight as TolerancePartWeight, negative_tol_percent as NegativeTolPercent, negative_tol_part_weight as NegativeTolPartWeight, 
                                positive_tol_percent as PositiveTolPercent, positive_tol_part_weight as PositiveTolPartWeight, image as Image,expiry_days as ExpiryDays FROM [1.childpart] where reference_number=@REFERENCENUMBER order by ReferenceNumber";
                using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                {
                    dataAdapter.SelectCommand.Parameters.AddWithValue("@REFERENCENUMBER", referenceNumber);
                    dataAdapter.Fill(dtChildParts);
                }

                if (dtChildParts.Rows.Count == 0)
                {
                    ResetChildPartsWeighing();

                    MessageBox.Show("Reference Number is not found", "Error: Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Cursor = Cursors.Default;
                    tbReferenceNumber.Focus();
                    tbReferenceNumber.SelectionStart = tbReferenceNumber.Text.Length;

                    return;
                }
                else if (dtChildParts.Rows.Count == 1)
                {
                    tbChildPartNumber.Text = dtChildParts.Rows[0]["PartNumber"].ToString();
                    tbChildPartDesc.Text = dtChildParts.Rows[0]["Description"].ToString();
                    tbDBPartWeight.Text = dtChildParts.Rows[0]["PartWeight"].ToString();
                    tbTolPercent.Text = dtChildParts.Rows[0]["TolerancePercent"].ToString();
                    tbNegativeTolPercent.Text = dtChildParts.Rows[0]["NegativeTolPercent"].ToString() + " %";
                    tbNegativeTolPartWeight.Text = dtChildParts.Rows[0]["NegativeTolPartWeight"].ToString();
                    tbTolPercent.Text = dtChildParts.Rows[0]["TolerancePercent"].ToString();
                    tbTolPartWeight.Text = dtChildParts.Rows[0]["TolerancePartWeight"].ToString();
                    tbPositiveTolPercent.Text = dtChildParts.Rows[0]["PositiveTolPercent"].ToString() + " %";
                    tbPositiveTolPartWeight.Text = dtChildParts.Rows[0]["PositiveTolPartWeight"].ToString();
                    tbExpiryDays.Text = dtChildParts.Rows[0]["ExpiryDays"].ToString();

                    if (dtChildParts.Rows[0]["Image"] == null || dtChildParts.Rows[0]["Image"] == DBNull.Value)
                    {
                        pbImage.Image = null;
                    }
                    else
                    {
                        pbImage.Image = Image.FromStream(new MemoryStream((byte[])dtChildParts.Rows[0]["Image"]));
                    }

                    int requiredQty = Convert.ToInt32(tbRequiredQty.Text);
                    double weightInKG = Convert.ToDouble(dtChildParts.Rows[0]["PartWeight"].ToString());

                    double requiredWeight = requiredQty * weightInKG;
                    lblRequiredWeight.Text = requiredWeight.ToString("0.0000");

                    double negativeTolPartWeight = Convert.ToDouble(tbNegativeTolPartWeight.Text);
                    double minimumWeight = requiredWeight - negativeTolPartWeight;
                    lblMinimumWeight.Text = minimumWeight.ToString("0.0000");

                    double positiveTolPartWeight = Convert.ToDouble(tbPositiveTolPartWeight.Text);
                    double maximumWeight = requiredWeight + positiveTolPartWeight;
                    lblMaximumWeight.Text = maximumWeight.ToString("0.0000");

                    REPEAT_ReferenceNumber = referenceNumber;
                    REPEAT_RequiredQuantity = requiredQty.ToString();

                    enableChildPartsWeighing = true;
                    LblWeight_TextChanged(sender, e);
                }

                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void tbReferenceNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter || e.KeyData == Keys.Return)
            {
                e.SuppressKeyPress = true;
                if (tbReferenceNumber.Text != string.Empty)
                {
                    tbRequiredQty.Focus();
                    tbRequiredQty.SelectionStart = tbRequiredQty.Text.Length;
                }
            }
        }


        private void TbRequiredQty_TextChanged(object sender, EventArgs e)
        {
            ResetChildPartsWeighing();
            this.AcceptButton = btnChildPartSubmit;
        }

        private void TbRequiredQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            //Allow only integers
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void BtnSendToPrint_EnabledChanged(object sender, EventArgs e)
        {
            if (btnSendToPrint.Enabled == true)
            {
                btnSendToPrint.BackColor = Color.GreenYellow;
            }
            else
            {
                btnSendToPrint.BackColor = Color.Red;
            }
        }
        private void tbReferenceNumber_TextChanged(object sender, EventArgs e)
        {
            ResetChildPartsWeighing();
            tbRequiredQty.Text = "";
            this.AcceptButton = null;
        }

        private void ResetChildPartsWeighing()
        {
            tbChildPartNumber.Text = "";
            tbChildPartDesc.Text = "";
            tbDBPartWeight.Text = "";
            tbNegativeTolPartWeight.Text = tbNegativeTolPercent.Text = "";
            tbTolPercent.Text = tbTolPartWeight.Text = "";
            tbPositiveTolPercent.Text = tbPositiveTolPartWeight.Text = "";
            pbImage.Image = null;
            tbExpiryDays.Text = "";

            lblMinimumWeight.Text = "0.0000";
            lblRequiredWeight.Text = "0.0000";
            lblMaximumWeight.Text = "0.0000";
            btnSendToPrint.Enabled = false;
        }

        private void ResetAssemblyPartsWeighing()
        {

            tbAssemblyDesc.Text = "";
            tbSerialNo.Text = "";
            tbCurrentSerialNo.Text = "";

            tbAssemblyChildPart.Text = "";
            tbAssemChildPartDesc.Text = "";
            tbAssemblyChildPartQty.Text = "";
            tbAssemDBPartWeight.Text = "";
            tbAssemNegativeTolPercent.Text = tbAssemNegativeTolPartWeight.Text = "";
            tbAssemTolPercent.Text = tbAssemTolPartWeight.Text = "";
            tbAssemPositiveTolPercent.Text = tbAssemPositiveTolPartWeight.Text = "";

            pbAssemChildPartImage.Image = null;


            dgvAssemChildPartsList.DataSource = null;
            lblAssemMinimumWeight.Text = "0.0000";
            lblAssemRequiredWeight.Text = "0.0000";
            lblAssemMaximumWeight.Text = "0.0000";
            btnAssemSendToPrint.Enabled = false;
        }

        private void BtnSendToPrint_Click(object sender, EventArgs e)
        {
            if (btnSendToPrint.Enabled)
            {
                string referenceNumber = tbReferenceNumber.Text;

                string partNumber = tbChildPartNumber.Text;
                string description = tbChildPartDesc.Text;
                string quantity = tbRequiredQty.Text;
                string netWeight = lblWeight.Text;

                double netWeightValue = Convert.ToDouble(netWeight.Replace(" KG", ""));
                double quantityValue = Convert.ToDouble(quantity);
                string partWeight = (netWeightValue / quantityValue).ToString("0.000000") + " KG";


                string negativeTolPercent = tbNegativeTolPercent.Text;
                string negativeTolPartWeight = tbNegativeTolPartWeight.Text;

                string DBPartWeight = tbDBPartWeight.Text;

                string positiveTolPercent = tbPositiveTolPercent.Text;
                string positiveTolPartWeight = tbPositiveTolPartWeight.Text;

                string expiryDays = string.IsNullOrEmpty(tbExpiryDays.Text) ? "0" : tbExpiryDays.Text;
                string expiryDate = DateTime.Now.AddDays(Convert.ToInt32(expiryDays)).ToString("dd-MMM-yyyy");


                //if DataGridView is empty, create a columns and assign it
                if (dgvChildPartsPrinting.Columns.Count == 0)
                {
                    var column0 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "ReferenceNumber",
                        Name = "ReferenceNumber",
                        ReadOnly = true
                    };

                    var column1 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "PartNumber",
                        Name = "PartNumber",
                        ReadOnly = true
                    };

                    var column2 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "Description",
                        Name = "Description",
                        ReadOnly = true
                    };

                    var column3 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "Quantity",
                        Name = "Quantity",
                        ReadOnly = true
                    };

                    var column4 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "PartWeight",
                        Name = "PartWeight",
                        ReadOnly = true
                    };


                    var column5 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "NetWeight",
                        Name = "NetWeight",
                        ReadOnly = true
                    };

                    var column6 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "BatchNo",
                        Name = "BatchNo"
                    };

                    var column7 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "OtherInfo",
                        Name = "OtherInfo"
                    };

                    var column8 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "NegativeTolPercent",
                        Name = "NegativeTolPercent",
                        ReadOnly = true
                    };

                    var column9 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "NegativeTolPartWeight",
                        Name = "NegativeTolPartWeight",
                        ReadOnly = true
                    };

                    var column10 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "DBPartWeight",
                        Name = "DBPartWeight",
                        ReadOnly = true
                    };

                    var column11 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "PositiveTolPercent",
                        Name = "PositiveTolPercent",
                        ReadOnly = true
                    };

                    var column12 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "PositiveTolPartWeight",
                        Name = "PositiveTolPartWeight",
                        ReadOnly = true
                    };

                    var column13 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "ExpiryDays",
                        Name = "ExpiryDays",
                        ReadOnly = true
                    };

                    var column14 = new DataGridViewTextBoxColumn
                    {
                        HeaderText = "ExpiryDate",
                        Name = "ExpiryDate",
                        ReadOnly = true
                    };

                    dgvChildPartsPrinting.Columns.AddRange(new DataGridViewColumn[]
                    { column0,column1,column2,column3,column4, column5, column6,column7,column8, column9,column10,column11,column12,column13,column14 });

                }

                int rowId = dgvChildPartsPrinting.Rows.Add();

                // Grab the new row!
                DataGridViewRow row = dgvChildPartsPrinting.Rows[rowId];

                // Add the data
                row.Cells["ReferenceNumber"].Value = referenceNumber;
                row.Cells["PartNumber"].Value = partNumber;
                row.Cells["Description"].Value = description;
                row.Cells["Quantity"].Value = quantity;
                row.Cells["PartWeight"].Value = partWeight;
                row.Cells["NetWeight"].Value = netWeight;
                row.Cells["BatchNo"].Value = "";
                row.Cells["OtherInfo"].Value = "";
                row.Cells["NegativeTolPercent"].Value = negativeTolPercent;
                row.Cells["NegativeTolPartWeight"].Value = negativeTolPartWeight;
                row.Cells["DBPartWeight"].Value = DBPartWeight;
                row.Cells["PositiveTolPercent"].Value = positiveTolPercent;
                row.Cells["PositiveTolPartWeight"].Value = positiveTolPartWeight;
                row.Cells["ExpiryDays"].Value = expiryDays;
                row.Cells["ExpiryDate"].Value = expiryDate;

                foreach (DataGridViewColumn column in dgvChildPartsPrinting.Columns)
                {
                    column.SortMode = DataGridViewColumnSortMode.NotSortable;
                }

                for (int i = 0; i < dgvChildPartsPrinting.Columns.Count; i++)
                {
                    if (dgvChildPartsPrinting.Columns[i].ReadOnly == true)
                    {
                        dgvChildPartsPrinting.Columns[i].DefaultCellStyle.BackColor = Color.LightGray;
                        dgvChildPartsPrinting.Columns[i].DefaultCellStyle.SelectionBackColor = Color.LightGray;
                        dgvChildPartsPrinting.Columns[i].DefaultCellStyle.SelectionForeColor = Color.Black;
                    }
                }

                CalculateChildGrossWeight();
                CalculateChildTotalNetWeight();

                ResetChildPartsWeighing();
                tbRequiredQty.Text = "";
                tbReferenceNumber.Text = "";
                tbReferenceNumber.Focus();

            }
        }

        void CalculateChildTotalNetWeight()
        {
            double totalNetWeight = 0;
            if (dgvChildPartsPrinting.Rows.Count > 0)
            {
                for (int i = 0; i < dgvChildPartsPrinting.Rows.Count; i++)
                {
                    totalNetWeight += Convert.ToDouble(dgvChildPartsPrinting.Rows[i].Cells["NetWeight"].Value.ToString().Replace(" KG", ""));
                }
            }
            lblChildTotalNetWeight.Text = totalNetWeight.ToString("0.0000");
        }

        void CalculateChildGrossWeight()
        {
            double palletWeight = 0;
            double totalNetWeight = 0;

            if (txtChildPalletWeight.Text.Length > 0)
            {
                palletWeight = Convert.ToDouble(txtChildPalletWeight.Text);
            }

            if (dgvChildPartsPrinting.Rows.Count > 0)
            {
                for (int i = 0; i < dgvChildPartsPrinting.Rows.Count; i++)
                {
                    totalNetWeight += Convert.ToDouble(dgvChildPartsPrinting.Rows[i].Cells["NetWeight"].Value.ToString().Replace(" KG", ""));
                }
            }
            double GrossWeight = palletWeight + totalNetWeight;
            lblChildGrossWeight.Text = GrossWeight.ToString("0.0000");
        }

        private void LblWeight_TextChanged(object sender, EventArgs e)
        {

            if (lblWeight.Text == "Disconnected" || lblWeight.Text == "Timeout Error" || lblWeight.Text == "IO Error" || lblWeight.Text == "Invalid Operation" || lblWeight.Text.StartsWith("Exception"))
            {
                btnSendToPrint.Enabled = btnAssemSendToPrint.Enabled = false;
                btnSettings.Enabled = true;
            }
            else
            {
                btnSettings.Enabled = false;

                if (enableChildPartsWeighing)
                {
                    string weight = lblWeight.Text;
                    double currentWeight = Convert.ToDouble(weight.Replace(" KG", ""));
                    double minimumWeight = Convert.ToDouble(lblMinimumWeight.Text);
                    double maximumWeight = Convert.ToDouble(lblMaximumWeight.Text);

                    if (currentWeight >= minimumWeight && currentWeight <= maximumWeight && currentWeight > 0)
                    {
                        btnSendToPrint.Enabled = true;
                    }
                    else
                    {
                        btnSendToPrint.Enabled = false;
                    }


                }
                else
                {
                    btnSendToPrint.Enabled = false;
                }


                if (enableAssemblyPartsWeighing)
                {
                    string weight = lblWeight.Text;
                    double currentWeight = Convert.ToDouble(weight.Replace(" KG", ""));
                    double minimumWeight = Convert.ToDouble(lblAssemMinimumWeight.Text);
                    double maximumWeight = Convert.ToDouble(lblAssemMaximumWeight.Text);
                    if (currentWeight >= minimumWeight && currentWeight <= maximumWeight && currentWeight > 0)
                    {
                        btnAssemSendToPrint.Enabled = true;
                    }
                    else
                    {
                        btnAssemSendToPrint.Enabled = false;
                    }
                }
                else
                {
                    btnAssemSendToPrint.Enabled = false;
                }
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Escape)    //New Entry
            {
                if (tcMainForm.SelectedTab == tpChildPartWeighing)
                {
                    tbReferenceNumber.Text = "";
                    tbRequiredQty.Text = "";
                    ResetChildPartsWeighing();
                    tbReferenceNumber.Focus();
                }

                if (tcMainForm.SelectedTab == tpAssemblyPartsWeighing)
                {
                    tbAssemblyPartNumber.Text = "";
                    ResetAssemblyPartsWeighing();
                    tbAssemblyPartNumber.Focus();
                }
            }

            if (e.KeyCode == Keys.F1)       //Repeat
            {
                if (tcMainForm.SelectedTab == tpChildPartWeighing)
                {
                    tbReferenceNumber.Text = REPEAT_ReferenceNumber;
                    tbRequiredQty.Text = REPEAT_RequiredQuantity;
                    BtnChildPartSubmit_Click(sender, e);
                }

                if (tcMainForm.SelectedTab == tpAssemblyPartsWeighing)
                {
                    tbAssemblyPartNumber.Text = REPEAT_AssemblyPartNumber;
                    BtnAssemblyPartSubmit_Click(sender, e);
                }
            }

            //Repeat Pallet weight
            if (e.KeyCode == Keys.F3)
            {
                if (tcMainForm.SelectedTab == tpChildPartWeighing)
                {
                    txtChildPalletWeight.Text = REPEAT_ChildPalletWeight;
                }

                if (tcMainForm.SelectedTab == tpAssemblyPartsWeighing)
                {
                    txtAssemPalletWeight.Text = REPEAT_AssemPalletWeight;
                }
            }

            //Get Pallet Weight
            if (e.KeyCode == Keys.F12)
            {
                if (tcMainForm.SelectedTab == tpChildPartWeighing)
                {
                    BtnChildPalletWeight_Click(sender, e);
                }

                if (tcMainForm.SelectedTab == tpAssemblyPartsWeighing)
                {
                    BtnAssemPalletWeight_Click(sender, e);
                }
            }



            //Send to Print Button
            if (e.KeyCode == Keys.F4)
            {
                if (tcMainForm.SelectedTab == tpChildPartWeighing)
                {
                    if (btnSendToPrint.Enabled)
                    {
                        BtnSendToPrint_Click(sender, e);
                    }
                }

                if (tcMainForm.SelectedTab == tpAssemblyPartsWeighing)
                {
                    if (btnAssemSendToPrint.Enabled)
                    {
                        BtnAssemSendToPrint_Click(sender, e);
                    }
                }
            }

            //Print button
            if (e.KeyCode == Keys.F8)
            {
                if (tcMainForm.SelectedTab == tpChildPartWeighing)
                {
                    BtnPrintChildPart_Click(sender, e);
                }

                if (tcMainForm.SelectedTab == tpAssemblyPartsWeighing)
                {
                    BtnPrintAssembly_Click(sender, e);
                }
            }


        }

        private void BtnClearPrinting_Click(object sender, EventArgs e)
        {
            dgvChildPartsPrinting.Rows.Clear();
            dgvChildPartsPrinting.Columns.Clear();
            CalculateChildGrossWeight();
            CalculateChildTotalNetWeight();
        }


        private void BtnAssemblyPartSubmit_Click(object sender, EventArgs e)
        {
            if (tbAssemblyPartNumber.Text == string.Empty)
            {
                MessageBox.Show("Enter the part number", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                this.Cursor = Cursors.WaitCursor;

                string assemblyPartNumber = tbAssemblyPartNumber.Text;

                DataTable dtAssemblyPart = new DataTable();
                string sql = @"SELECT assembly_id as ID, assembly_part_number as AssemblyPartNumber, assembly_part_description as Description FROM [2.assembly] where assembly_part_number=@PARTNUMBER";
                using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                {
                    dataAdapter.SelectCommand.Parameters.AddWithValue("@PARTNUMBER", assemblyPartNumber);
                    dataAdapter.Fill(dtAssemblyPart);
                }

                if (dtAssemblyPart.Rows.Count == 0)
                {
                    ResetAssemblyPartsWeighing();

                    MessageBox.Show("Assembly Part Number is not found", "Error: Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Cursor = Cursors.Default;
                    tbAssemblyPartNumber.Focus();
                    tbAssemblyPartNumber.SelectionStart = tbAssemblyPartNumber.Text.Length;

                    return;
                }
                else if (dtAssemblyPart.Rows.Count == 1)
                {
                    tbAssemblyDesc.Text = dtAssemblyPart.Rows[0]["Description"].ToString();
                    REPEAT_AssemblyPartNumber = assemblyPartNumber;
                    dgvAssemChildPartsList.DataSource = null;

                    DataTable dtChildParts = new DataTable();

                    sql = @"Select  [3.assembly_childpart].s_no as [S.No],
                                    [1.childpart].childpart_id as ID,
                                    [1.childpart].child_part_number as PartNumber,
                                    [1.childpart].child_part_description as Description,
                                    [3.assembly_childpart].no_of_qty as NoOfQty,
                                    [1.childpart].part_weight as PartWeight,
                                    [1.childpart].tol_percent AS TolerancePercent,
                                    [1.childpart].tol_part_weight as TolerancePartWeight,
                                    [1.childpart].negative_tol_percent as NegativeTolPercent,
                                    [1.childpart].negative_tol_part_weight as NegativeTolPartWeight,
                                    [1.childpart].positive_tol_percent as PositiveTolPercent,
                                    [1.childpart].positive_tol_part_weight as PositiveTolPartWeight,
                                    [1.childpart].image as Image
                                From
                                    [2.assembly] Inner Join
                                    [3.assembly_childpart] On [2.assembly].assembly_id = [3.assembly_childpart].assembly_id Inner Join
                                    [1.childpart] On [1.childpart].childpart_id = [3.assembly_childpart].childpart_id
                                Where
                                    [2.assembly].assembly_part_number = @PARTNUMBER
                                Order By
                                    [3.assembly_childpart].s_no";
                    using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                    using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                    {
                        dataAdapter.SelectCommand.Parameters.AddWithValue("@PARTNUMBER", assemblyPartNumber);
                        dataAdapter.Fill(dtChildParts);
                    }

                    dtChildParts.Columns.Add("NetWeight", typeof(string)).SetOrdinal(6);

                    dgvAssemChildPartsList.DataSource = dtChildParts;
                    dgvAssemChildPartsList.Columns["ID"].Visible = false;
                    dgvAssemChildPartsList.Columns["Image"].Visible = false;


                    dgvAssemChildPartsList.ClearSelection();

                    foreach (DataGridViewColumn column in dgvAssemChildPartsList.Columns)
                    {
                        column.SortMode = DataGridViewColumnSortMode.NotSortable;
                    }
                    //if (dgvAssemChildPartsList.Rows.Count > 0)
                    //{
                    //    tbSerialNo.Text = dgvAssemChildPartsList.Rows[0].Cells["S.No"].Value.ToString();
                    //    BtnSerialNo_Click(sender, e);
                    //}
                    tbSerialNo.Clear();
                    tbSerialNo.Focus();
                    tbSerialNo.SelectionStart = tbSerialNo.Text.Length;


                }

                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TbAssemblyPartNumber_TextChanged(object sender, EventArgs e)
        {
            ResetAssemblyPartsWeighing();
            this.AcceptButton = btnAssemblyPartSubmit;
        }

        private void TbSerialNo_TextChanged(object sender, EventArgs e)
        {
            tbAssemblyChildPart.Text = "";
            tbAssemChildPartDesc.Text = "";
            tbAssemblyChildPartQty.Text = "";
            tbAssemDBPartWeight.Text = "";
            tbAssemNegativeTolPercent.Text = tbAssemNegativeTolPartWeight.Text = "";
            tbAssemTolPercent.Text = tbAssemTolPartWeight.Text = "";
            tbAssemPositiveTolPercent.Text = tbAssemPositiveTolPartWeight.Text = "";


            pbAssemChildPartImage.Image = null;


            lblAssemMinimumWeight.Text = "0.0000";
            lblAssemRequiredWeight.Text = "0.0000";
            lblAssemMaximumWeight.Text = "0.0000";
            btnAssemSendToPrint.Enabled = false;

            this.AcceptButton = btnSerialNo;
        }

        private void BtnSerialNo_Click(object sender, EventArgs e)
        {
            enableAssemblyPartsWeighing = false;
            if (tbSerialNo.Text == string.Empty)
            {
                MessageBox.Show("Enter the S.No", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (dgvAssemChildPartsList.DataSource == null || dgvAssemChildPartsList.Rows.Count == 0)
            {
                MessageBox.Show("Child Parts List are empty.", "Error: No Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                string serialNoInput = tbSerialNo.Text;
                tbCurrentSerialNo.Text = serialNoInput;
                tbSerialNo.Clear();
                bool isSerialNoFound = false;
                for (int i = 0; i < dgvAssemChildPartsList.Rows.Count; i++)
                {
                    string serialNo = dgvAssemChildPartsList.Rows[i].Cells["S.No"].Value.ToString();
                    if (serialNo == serialNoInput)
                    {
                        isSerialNoFound = true;
                        tbAssemblyChildPart.Text = dgvAssemChildPartsList.Rows[i].Cells["PartNumber"].Value.ToString();
                        tbAssemChildPartDesc.Text = dgvAssemChildPartsList.Rows[i].Cells["Description"].Value.ToString();
                        tbAssemblyChildPartQty.Text = dgvAssemChildPartsList.Rows[i].Cells["NoOfQty"].Value.ToString();
                        tbAssemDBPartWeight.Text = dgvAssemChildPartsList.Rows[i].Cells["PartWeight"].Value.ToString();
                        tbAssemNegativeTolPercent.Text = dgvAssemChildPartsList.Rows[i].Cells["NegativeTolPercent"].Value.ToString();
                        tbAssemNegativeTolPartWeight.Text = dgvAssemChildPartsList.Rows[i].Cells["NegativeTolPartWeight"].Value.ToString();
                        tbAssemPositiveTolPercent.Text = dgvAssemChildPartsList.Rows[i].Cells["PositiveTolPercent"].Value.ToString();
                        tbAssemPositiveTolPartWeight.Text = dgvAssemChildPartsList.Rows[i].Cells["PositiveTolPartWeight"].Value.ToString();
                        tbAssemTolPercent.Text = dgvAssemChildPartsList.Rows[i].Cells["TolerancePercent"].Value.ToString();
                        tbAssemTolPartWeight.Text = dgvAssemChildPartsList.Rows[i].Cells["TolerancePartWeight"].Value.ToString();
                        if (dgvAssemChildPartsList.Rows[i].Cells["Image"].Value != DBNull.Value)
                        {
                            var data = (Byte[])(dgvAssemChildPartsList.Rows[i].Cells["Image"].Value);
                            var stream = new MemoryStream(data);
                            pbAssemChildPartImage.Image = Image.FromStream(stream);
                        }
                        else
                        {
                            pbAssemChildPartImage.Image = null;
                        }


                        int requiredQty = Convert.ToInt32(tbAssemblyChildPartQty.Text);

                        double partWeight = Convert.ToDouble(tbAssemDBPartWeight.Text);

                        double requiredWeight = requiredQty * partWeight;
                        lblAssemRequiredWeight.Text = requiredWeight.ToString("0.0000");

                        double negativeTolPartWeight = Convert.ToDouble(tbAssemNegativeTolPartWeight.Text);
                        double minimumWeight = requiredWeight - negativeTolPartWeight;
                        lblAssemMinimumWeight.Text = minimumWeight.ToString("0.0000");

                        double positiveTolPartWeight = Convert.ToDouble(tbAssemPositiveTolPartWeight.Text);
                        double maximumWeight = requiredWeight + positiveTolPartWeight;
                        lblAssemMaximumWeight.Text = maximumWeight.ToString("0.0000");

                        enableAssemblyPartsWeighing = true;
                        LblWeight_TextChanged(sender, e);
                    }
                }
                if (!isSerialNoFound)
                {
                    tbAssemblyChildPart.Text = "";
                    tbAssemChildPartDesc.Text = "";
                    tbAssemblyChildPartQty.Text = "";
                    tbAssemDBPartWeight.Text = "";
                    tbNegativeTolPercent.Text = tbNegativeTolPartWeight.Text = tbTolPercent.Text = tbTolPartWeight.Text = "";
                    tbPositiveTolPercent.Text = tbPositiveTolPartWeight.Text = "";

                    pbAssemChildPartImage.Image = null;

                    MessageBox.Show("Serial No is not found");
                }
            }


        }



        private void BtnAssemSendToPrint_EnabledChanged(object sender, EventArgs e)
        {
            if (btnAssemSendToPrint.Enabled == true)
            {
                btnAssemSendToPrint.BackColor = Color.GreenYellow;
            }
            else
            {
                btnAssemSendToPrint.BackColor = Color.Red;
            }
        }

        private void TcMainForm_SelectedIndexChanged(object sender, EventArgs e)
        {
            enableAssemblyPartsWeighing = false;
            enableChildPartsWeighing = false;

            if (tcMainForm.SelectedTab == tpChildPartWeighing)
            {
                tbReferenceNumber.Text = "";
                tbReferenceNumber.Focus();
                txtChildPalletWeight.Text = "0.0000";
            }

            if (tcMainForm.SelectedTab == tpAssemblyPartsWeighing)
            {
                tbAssemblyPartNumber.Text = "";
                tbAssemblyPartNumber.Focus();
                txtAssemPalletWeight.Text = "0.0000";
            }
        }

        private void BtnAssemSendToPrint_Click(object sender, EventArgs e)
        {
            if (btnAssemSendToPrint.Enabled)
            {
                if (dgvAssemChildPartsList.DataSource != null)
                {
                    //string serialNoInput = tbSerialNo.Text;
                    string serialNoInput = tbCurrentSerialNo.Text;

                    for (int i = 0; i < dgvAssemChildPartsList.Rows.Count; i++)
                    {
                        string serialNo = dgvAssemChildPartsList.Rows[i].Cells["S.No"].Value.ToString();
                        if (serialNo == serialNoInput)
                        {
                            dgvAssemChildPartsList.Rows[i].Cells["NetWeight"].Value = lblWeight.Text.Replace(" KG", "");
                            //If it is equal, then change the backgroundcolor to green
                            dgvAssemChildPartsList.Rows[i].DefaultCellStyle.SelectionBackColor = Color.GreenYellow;
                            dgvAssemChildPartsList.Rows[i].DefaultCellStyle.BackColor = Color.GreenYellow;

                            CalculateAssemGrossWeight();

                            //if ((i + 1) != dgvAssemChildPartsList.Rows.Count)
                            //{
                            //    string nextSerialNo = dgvAssemChildPartsList.Rows[i + 1].Cells["S.No"].Value.ToString();
                            //    tbSerialNo.Text = nextSerialNo;
                            //    tbSerialNo.Focus();
                            //    tbSerialNo.SelectionStart = tbSerialNo.Text.Length;
                            //    BtnSerialNo_Click(sender, e);
                            //}

                            tbSerialNo.Clear();
                            tbSerialNo.Focus();

                            break;
                        }
                    }

                }



            }
        }

        private void CalculateAssemGrossWeight()
        {
            double palletWeight = 0;
            double totalNetWeight = 0;

            if (txtAssemPalletWeight.Text.Length > 0)
            {
                palletWeight = Convert.ToDouble(txtAssemPalletWeight.Text);
            }

            for (int i = 0; i < dgvAssemChildPartsList.Rows.Count; i++)
            {
                if (dgvAssemChildPartsList.Rows[i].DefaultCellStyle.SelectionBackColor == Color.GreenYellow)
                {
                    totalNetWeight += Convert.ToDouble(dgvAssemChildPartsList.Rows[i].Cells["NetWeight"].Value.ToString());
                }
            }

            double GrossWeight = palletWeight + totalNetWeight;
            lblAssemTotalNetWeight.Text = totalNetWeight.ToString("0.0000");
            lblAssemGrossWeight.Text = GrossWeight.ToString("0.0000");
        }

        private void DgvAssemChildPartsList_DataSourceChanged(object sender, EventArgs e)
        {
            if (dgvAssemChildPartsList.DataSource == null || dgvAssemChildPartsList.Rows.Count == 0)
            {
                lblAssemTotalNetWeight.Text = "0.0000";
                lblAssemGrossWeight.Text = "0.0000";
            }

        }

        private void BtnPrintChildPart_Click(object sender, EventArgs e)
        {
            if (dgvChildPartsPrinting.Rows.Count > 0)
            {
                CustomerSelectionForm customerForm = new CustomerSelectionForm();
                customerForm.ShowDialog();

                appSettings = config.ReadSettings();
                if (appSettings.PrintingFileName == "")
                    return;

                try
                {
                    string outputSourcePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    string outputFileName = appSettings.PrintingFileName + ".prn";
                    string outputTargetPath = Path.GetTempPath();

                    // Use Path class to manipulate file and directory paths.
                    string outputSourceFile = Path.Combine(outputSourcePath, outputFileName);
                    string outputDestFile = Path.Combine(outputTargetPath, outputFileName);

                    PrintDialog pd = new PrintDialog
                    {
                        PrinterSettings = new PrinterSettings(),
                        UseEXDialog = true
                    };


                    string savedPrinter = appSettings.PrinterName;

                    foreach (string printerName in PrinterSettings.InstalledPrinters)
                    {
                        if (savedPrinter == printerName)
                        {
                            pd.PrinterSettings.PrinterName = savedPrinter;
                        }
                    }

                    if (DialogResult.OK == pd.ShowDialog(this))
                    {
                        this.Cursor = Cursors.WaitCursor;
                        appSettings.PrinterName = pd.PrinterSettings.PrinterName;
                        config.WriteSettings(appSettings);

                        DataTable dtChildPartsPrinting = new DataTable();
                        bool enableSavePrintingHistory = appSettings.EnablePrintingHistory;

                        if (enableSavePrintingHistory)
                        {
                            //Create a datatable column matching with sqlite database
                            dtChildPartsPrinting.Columns.Add(new DataColumn("date_time", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("customer_name", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("reference_number", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("child_part_number", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("child_part_description", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("quantity", typeof(Int32)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("part_weight", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("net_weight", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("pallet_weight", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("gross_weight", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("batch_no", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("other_info", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("no_of_packet", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("negative_tol_percent", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("negative_tol_part_weight", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("db_part_weight", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("positive_tol_percent", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("positive_tol_part_weight", typeof(string)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("expiry_days", typeof(Int32)));
                            dtChildPartsPrinting.Columns.Add(new DataColumn("expiry_date", typeof(string)));
                        }


                        string customerName = appSettings.PrintingFileName;
                        string date = DateTime.Now.ToString("dd/MMM/yyyy");
                        string palletWeight = txtChildPalletWeight.Text;
                        REPEAT_ChildPalletWeight = txtChildPalletWeight.Text;
                        string grossWeight = lblChildGrossWeight.Text;
                        string totalBoxCount = dgvChildPartsPrinting.Rows.Count.ToString();

                        for (int i = 0; i < dgvChildPartsPrinting.Rows.Count; i++)
                        {
                            string referenceNumber = dgvChildPartsPrinting.Rows[i].Cells["ReferenceNumber"].Value.ToString();
                            string partNo = dgvChildPartsPrinting.Rows[i].Cells["PartNumber"].Value.ToString();
                            string partName = dgvChildPartsPrinting.Rows[i].Cells["Description"].Value.ToString();
                            string quantity = dgvChildPartsPrinting.Rows[i].Cells["Quantity"].Value.ToString();
                            string partWeight = dgvChildPartsPrinting.Rows[i].Cells["PartWeight"].Value.ToString();
                            string partInfo = "Weight Per Part";
                            string partInfoValue = partWeight;
                            string netWeight = dgvChildPartsPrinting.Rows[i].Cells["NetWeight"].Value.ToString();
                            string batchNo = dgvChildPartsPrinting.Rows[i].Cells["BatchNo"].Value.ToString();
                            string otherInfo = dgvChildPartsPrinting.Rows[i].Cells["OtherInfo"].Value.ToString();
                            string negativeTolPercent = dgvChildPartsPrinting.Rows[i].Cells["NegativeTolPercent"].Value.ToString();
                            string negativeTolPartWeight = dgvChildPartsPrinting.Rows[i].Cells["NegativeTolPartWeight"].Value.ToString();

                            string DBPartWeight = dgvChildPartsPrinting.Rows[i].Cells["DBPartWeight"].Value.ToString();

                            string positiveTolPercent = dgvChildPartsPrinting.Rows[i].Cells["PositiveTolPercent"].Value.ToString();
                            string positiveTolPartWeight = dgvChildPartsPrinting.Rows[i].Cells["PositiveTolPartWeight"].Value.ToString();
                            int expiryDays = Convert.ToInt32(dgvChildPartsPrinting.Rows[i].Cells["ExpiryDays"].Value);
                            string expiryDate = dgvChildPartsPrinting.Rows[i].Cells["ExpiryDate"].Value.ToString();

                            // To copy a folder's contents to a new location:
                            // Create a new target folder, if necessary.
                            if (!System.IO.Directory.Exists(outputTargetPath))
                            {
                                System.IO.Directory.CreateDirectory(outputTargetPath);
                            }
                            // To copy a file to another location and 
                            // overwrite the destination file if it already exists.
                            System.IO.File.Copy(outputSourceFile, outputDestFile, true);

                            //Replace the code with data
                            string text = File.ReadAllText(outputDestFile);
                            text = text.Replace("{CUSTOMERNAME}", customerName);
                            text = text.Replace("{PARTNO}", partNo);
                            text = text.Replace("{PARTNAME}", partName);
                            text = text.Replace("{QUANTITY}", quantity);
                            text = text.Replace("{PARTINFO}", partInfo);
                            text = text.Replace("{PARTINFOVALUE}", partInfoValue);
                            text = text.Replace("{NETWEIGHT}", netWeight);
                            text = text.Replace("{PALLETWEIGHT}", palletWeight);
                            text = text.Replace("{GROSSWEIGHT}", grossWeight);
                            text = text.Replace("{DATE}", date);
                            text = text.Replace("{BOXNO}", (i + 1).ToString());
                            text = text.Replace("{TOTALBOXCOUNT}", totalBoxCount);
                            text = text.Replace("{BATCHNO}", batchNo);
                            text = text.Replace("{OTHERINFO}", otherInfo);
                            text = text.Replace("{NEGATIVETOLPERCENT}", negativeTolPercent);
                            text = text.Replace("{NEGATIVETOLPARTWEIGHT}", negativeTolPartWeight);
                            text = text.Replace("{DBPARTWEIGHT}", DBPartWeight);
                            text = text.Replace("{POSITIVETOLPERCENT}", positiveTolPercent);
                            text = text.Replace("{POSITIVETOLPARTWEIGHT}", positiveTolPartWeight);

                            //New columns on printing labels
                            text = text.Replace("{REFERENCENUMBER}", referenceNumber);
                            text = text.Replace("{EXPIRYDAYS}", expiryDays.ToString());
                            text = text.Replace("{EXPIRYDATE}", DateTime.Now.AddDays(expiryDays).ToString("dd/MMM/yyyy"));

                            File.WriteAllText(outputDestFile, text);

                            SendTextFileToPrinter(outputDestFile, pd.PrinterSettings.PrinterName);

                            File.Delete(outputDestFile);

                            if (enableSavePrintingHistory)
                            {
                                //Add rows to datatable and assign the value for each column
                                DataRow row = dtChildPartsPrinting.NewRow();
                                row["date_time"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                row["customer_name"] = customerName;
                                row["reference_number"] = referenceNumber;
                                row["child_part_number"] = partNo;
                                row["child_part_description"] = partName;
                                row["quantity"] = quantity;
                                row["part_weight"] = partWeight;
                                row["net_weight"] = netWeight;
                                row["pallet_weight"] = palletWeight;
                                row["gross_weight"] = grossWeight;
                                row["batch_no"] = batchNo;
                                row["other_info"] = otherInfo;
                                row["no_of_packet"] = (i + 1).ToString() + "/" + totalBoxCount;
                                row["negative_tol_percent"] = negativeTolPercent;
                                row["negative_tol_part_weight"] = negativeTolPartWeight;
                                row["db_part_weight"] = DBPartWeight;
                                row["positive_tol_percent"] = positiveTolPercent;
                                row["positive_tol_part_weight"] = positiveTolPartWeight;
                                row["expiry_days"] = expiryDays;
                                row["expiry_date"] = expiryDate;
                                dtChildPartsPrinting.Rows.Add(row);
                            }
                        }

                        //Update the batch number to the childpart master
                        if (dtChildPartsPrinting.Rows.Count > 0)
                        {
                            string commandText_ = "",
                                refernceNumber_ = "",
                                batchNumber_;

                            for (var i = 0; i < dtChildPartsPrinting.Rows.Count; i++)
                            {
                                refernceNumber_ = dtChildPartsPrinting.Rows[i]["reference_number"].ToString();

                                batchNumber_ = dtChildPartsPrinting.Rows[i]["batch_no"].ToString() ?? "";

                                commandText_ = @"UPDATE [1.childpart] SET batch_no = @BATCHNUMBER WHERE reference_number=@REFERENCENUMBER";

                                SQLiteParameter[] parameters_ = new SQLiteParameter[2];
                                parameters_[0] = new SQLiteParameter("@BATCHNUMBER", batchNumber_);
                                parameters_[1] = new SQLiteParameter("@REFERENCENUMBER", refernceNumber_);

                                SqlLiteHelper.ExecuteNonQuery(connectionString, commandText_, parameters_);
                            }
                        }

                        if (enableSavePrintingHistory && dtChildPartsPrinting.Rows.Count > 0)
                        {
                            //Bulk insert datatable in sqlite db
                            using (var conn = new SQLiteConnection(connectionString, true))
                            {
                                // Be sure you already created the Person Table!

                                conn.Open();

                                using (var cmd = new SQLiteCommand(conn))
                                {
                                    using (var transaction = conn.BeginTransaction())
                                    {
                                        for (var i = 0; i < dtChildPartsPrinting.Rows.Count; i++)
                                        {
                                            cmd.CommandText =
                                                @"INSERT INTO [4.childpart_history] (date_time, customer_name, reference_number , child_part_number, child_part_description,
                                                quantity, part_weight, net_weight, pallet_weight, gross_weight, batch_no, other_info, no_of_packet,
                                                negative_tol_percent, negative_tol_part_weight, db_part_weight, positive_tol_percent, positive_tol_part_weight, expiry_days, expiry_date)" +
                                                @" VALUES (@date_time, @customer_name, @reference_number, @child_part_number, @child_part_description,
                                                @quantity, @part_weight, @net_weight, @pallet_weight, @gross_weight, @batch_no, @other_info, @no_of_packet,
                                                @negative_tol_percent, @negative_tol_part_weight, @db_part_weight, @positive_tol_percent, @positive_tol_part_weight, @expiry_days, @expiry_date);";
                                            cmd.Parameters.AddWithValue("@date_time", dtChildPartsPrinting.Rows[i]["date_time"].ToString());
                                            cmd.Parameters.AddWithValue("@customer_name", dtChildPartsPrinting.Rows[i]["customer_name"].ToString());
                                            cmd.Parameters.AddWithValue("@reference_number", dtChildPartsPrinting.Rows[i]["reference_number"].ToString());
                                            cmd.Parameters.AddWithValue("@child_part_number", dtChildPartsPrinting.Rows[i]["child_part_number"].ToString());
                                            cmd.Parameters.AddWithValue("@child_part_description", dtChildPartsPrinting.Rows[i]["child_part_description"].ToString());
                                            cmd.Parameters.AddWithValue("@quantity", dtChildPartsPrinting.Rows[i]["quantity"].ToString());
                                            cmd.Parameters.AddWithValue("@part_weight", dtChildPartsPrinting.Rows[i]["part_weight"].ToString());
                                            cmd.Parameters.AddWithValue("@net_weight", dtChildPartsPrinting.Rows[i]["net_weight"].ToString());
                                            cmd.Parameters.AddWithValue("@pallet_weight", dtChildPartsPrinting.Rows[i]["pallet_weight"].ToString() + " KG");
                                            cmd.Parameters.AddWithValue("@gross_weight", dtChildPartsPrinting.Rows[i]["gross_weight"].ToString() + " KG");
                                            cmd.Parameters.AddWithValue("@batch_no", dtChildPartsPrinting.Rows[i]["batch_no"].ToString());
                                            cmd.Parameters.AddWithValue("@other_info", dtChildPartsPrinting.Rows[i]["other_info"].ToString());
                                            cmd.Parameters.AddWithValue("@no_of_packet", dtChildPartsPrinting.Rows[i]["no_of_packet"].ToString());
                                            cmd.Parameters.AddWithValue("@negative_tol_percent", dtChildPartsPrinting.Rows[i]["negative_tol_percent"].ToString());
                                            cmd.Parameters.AddWithValue("@negative_tol_part_weight", dtChildPartsPrinting.Rows[i]["negative_tol_part_weight"].ToString() + " KG");
                                            cmd.Parameters.AddWithValue("@db_part_weight", dtChildPartsPrinting.Rows[i]["db_part_weight"].ToString() + " KG");
                                            cmd.Parameters.AddWithValue("@positive_tol_percent", dtChildPartsPrinting.Rows[i]["positive_tol_percent"].ToString());
                                            cmd.Parameters.AddWithValue("@positive_tol_part_weight", dtChildPartsPrinting.Rows[i]["positive_tol_part_weight"].ToString() + " KG");
                                            cmd.Parameters.AddWithValue("@expiry_days", dtChildPartsPrinting.Rows[i]["expiry_days"].ToString());
                                            cmd.Parameters.AddWithValue("@expiry_date", dtChildPartsPrinting.Rows[i]["expiry_date"].ToString());
                                            cmd.ExecuteNonQuery();
                                        }

                                        transaction.Commit();
                                    }
                                }


                                conn.Close();
                            }
                            MessageBox.Show("Data saved successfully in database", "SUCCESS", MessageBoxButtons.OK, MessageBoxIcon.None);
                        }
                        //Clear all entries

                        txtChildPalletWeight.Text = lblChildTotalNetWeight.Text = lblChildGrossWeight.Text = "0.0000";
                        tbReferenceNumber.Text = "";
                        tbRequiredQty.Text = "";
                        ResetChildPartsWeighing();
                        BtnClearPrinting_Click(sender, e);
                        tbReferenceNumber.Focus();

                        this.Cursor = Cursors.Default;
                    }


                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;

                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


            }
        }

        private void BtnPrintAssembly_Click(object sender, EventArgs e)
        {
            if (dgvAssemChildPartsList.Rows.Count > 0)
            {
                bool isAllEntryGreen = true;

                foreach (DataGridViewRow row in dgvAssemChildPartsList.Rows)
                {
                    if (row.DefaultCellStyle.BackColor != Color.GreenYellow)
                    {
                        isAllEntryGreen = false;
                        break;
                    }
                }
                if (!isAllEntryGreen)
                {
                    MessageBox.Show("One or more Child Part is not weighed.\nMake sure all entries are GREEN", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                CustomerSelectionForm customerForm = new CustomerSelectionForm();
                customerForm.ShowDialog();

                if (appSettings.PrintingFileName == "")
                    return;

                try
                {
                    string outputSourcePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    string outputFileName = appSettings.PrintingFileName + ".prn";
                    string outputTargetPath = Path.GetTempPath();

                    // Use Path class to manipulate file and directory paths.
                    string outputSourceFile = Path.Combine(outputSourcePath, outputFileName);
                    string outputDestFile = Path.Combine(outputTargetPath, outputFileName);

                    PrintDialog pd = new PrintDialog
                    {
                        PrinterSettings = new PrinterSettings(),
                        UseEXDialog = true
                    };


                    string savedPrinter = appSettings.PrinterName;

                    foreach (string printerName in PrinterSettings.InstalledPrinters)
                    {
                        if (savedPrinter == printerName)
                        {
                            pd.PrinterSettings.PrinterName = savedPrinter;
                        }
                    }

                    if (DialogResult.OK == pd.ShowDialog(this))
                    {
                        this.Cursor = Cursors.WaitCursor;
                        appSettings.PrinterName = pd.PrinterSettings.PrinterName;
                        config.WriteSettings(appSettings);

                        string customerName = appSettings.PrintingFileName;
                        string date = DateTime.Now.ToString("dd/MMM/yyyy");
                        string palletWeight = txtAssemPalletWeight.Text + " KG";
                        REPEAT_AssemPalletWeight = txtAssemPalletWeight.Text;
                        string grossWeight = lblAssemGrossWeight.Text + " KG";

                        string partNo = tbAssemblyPartNumber.Text;
                        string partName = tbAssemblyDesc.Text;
                        string quantity = "1";
                        string netWeight = lblAssemTotalNetWeight.Text + " KG";
                        string partInfo = "Childparts Count";
                        string partInfoValue = dgvAssemChildPartsList.Rows.Count.ToString();


                        // To copy a folder's contents to a new location:
                        // Create a new target folder, if necessary.
                        if (!System.IO.Directory.Exists(outputTargetPath))
                        {
                            System.IO.Directory.CreateDirectory(outputTargetPath);
                        }
                        // To copy a file to another location and 
                        // overwrite the destination file if it already exists.
                        System.IO.File.Copy(outputSourceFile, outputDestFile, true);

                        //Replace the code with data
                        string text = File.ReadAllText(outputDestFile);
                        text = text.Replace("{CUSTOMERNAME}", customerName);
                        text = text.Replace("{PARTNO}", partNo);
                        text = text.Replace("{PARTNAME}", partName);
                        text = text.Replace("{QUANTITY}", quantity);
                        text = text.Replace("{PARTINFO}", partInfo);
                        text = text.Replace("{PARTINFOVALUE}", partInfoValue);
                        text = text.Replace("{NETWEIGHT}", netWeight);
                        text = text.Replace("{DATE}", date);
                        text = text.Replace("{BATCHNO}", "");
                        text = text.Replace("{OTHERINFO}", "");
                        text = text.Replace("{BOXNO}", "1");
                        text = text.Replace("{TOTALBOXCOUNT}", "1");
                        text = text.Replace("{PALLETWEIGHT}", palletWeight);
                        text = text.Replace("{GROSSWEIGHT}", grossWeight);
                        text = text.Replace("{NEGATIVETOLPERCENT}", "");
                        text = text.Replace("{NEGATIVETOLPARTWEIGHT}", "");
                        text = text.Replace("{DBPARTWEIGHT}", "");
                        text = text.Replace("{POSITIVETOLPERCENT}", "");
                        text = text.Replace("{POSITIVETOLPARTWEIGHT}", "");


                        File.WriteAllText(outputDestFile, text);

                        SendTextFileToPrinter(outputDestFile, pd.PrinterSettings.PrinterName);

                        File.Delete(outputDestFile);



                        if (appSettings.EnablePrintingHistory)
                        {
                            //Insert the assembly part number and get the ID
                            using (var conn = new SQLiteConnection(connectionString, true))
                            {
                                // Be sure you already created the Person Table!
                                conn.Open();
                                using (var cmd = new SQLiteCommand(conn))
                                {
                                    using (var transaction = conn.BeginTransaction())
                                    {
                                        cmd.CommandText =
                                            @"INSERT INTO [5.assembly_history] (date_time,customer_name, assembly_part_number, assembly_description,pallet_weight,gross_weight)" +
                                            @" VALUES (@date_time, @customer_name, @assembly_part_number, @assembly_description,@pallet_weight,@gross_weight);";
                                        cmd.Parameters.AddWithValue("@date_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                        cmd.Parameters.AddWithValue("@customer_name", customerName);
                                        cmd.Parameters.AddWithValue("@assembly_part_number", partNo);
                                        cmd.Parameters.AddWithValue("@assembly_description", partName);
                                        cmd.Parameters.AddWithValue("@pallet_weight", palletWeight + " KG");
                                        cmd.Parameters.AddWithValue("@gross_weight", grossWeight + " KG");

                                        cmd.ExecuteNonQuery();

                                        transaction.Commit();
                                    }
                                }
                                conn.Close();
                            }


                            int lastInsertRowID = 0;
                            //Insert the assembly part number and get the ID
                            using (var conn = new SQLiteConnection(connectionString, true))
                            {
                                // Be sure you already created the Person Table!

                                conn.Open();

                                using (var cmd = new SQLiteCommand(conn))
                                {
                                    using (var transaction = conn.BeginTransaction())
                                    {
                                        cmd.CommandText = @"select seq from sqlite_sequence where name='5.assembly_history'; ";
                                        lastInsertRowID = Convert.ToInt32(cmd.ExecuteScalar());
                                        transaction.Commit();
                                    }
                                }
                                conn.Close();
                            }

                            DataTable dtAssemChildPartsHistory = new DataTable();
                            dtAssemChildPartsHistory.Columns.Add(new DataColumn("assembly_history_id", typeof(Int32)));
                            dtAssemChildPartsHistory.Columns.Add(new DataColumn("s_no", typeof(Int32)));
                            dtAssemChildPartsHistory.Columns.Add(new DataColumn("child_part_number", typeof(string)));
                            dtAssemChildPartsHistory.Columns.Add(new DataColumn("child_part_description", typeof(string)));
                            dtAssemChildPartsHistory.Columns.Add(new DataColumn("quantity", typeof(Int32)));
                            dtAssemChildPartsHistory.Columns.Add(new DataColumn("part_weight", typeof(string)));
                            dtAssemChildPartsHistory.Columns.Add(new DataColumn("net_weight", typeof(string)));

                            for (int i = 0; i < dgvAssemChildPartsList.Rows.Count; i++)
                            {
                                DataRow dr = dtAssemChildPartsHistory.NewRow();
                                dr["assembly_history_id"] = lastInsertRowID;
                                dr["s_no"] = dgvAssemChildPartsList.Rows[i].Cells["S.No"].Value.ToString();
                                dr["child_part_number"] = dgvAssemChildPartsList.Rows[i].Cells["PartNumber"].Value.ToString();
                                dr["child_part_description"] = dgvAssemChildPartsList.Rows[i].Cells["Description"].Value.ToString();
                                dr["quantity"] = dgvAssemChildPartsList.Rows[i].Cells["NoOfQty"].Value.ToString();
                                dr["part_weight"] = dgvAssemChildPartsList.Rows[i].Cells["PartWeight"].Value.ToString() + " KG";
                                dr["net_weight"] = dgvAssemChildPartsList.Rows[i].Cells["NetWeight"].Value.ToString() + " KG";
                                dtAssemChildPartsHistory.Rows.Add(dr);
                            }

                            //Bulk insert datatable in sqlite db
                            using (var conn = new SQLiteConnection(connectionString, true))
                            {
                                conn.Open();

                                using (var cmd = new SQLiteCommand(conn))
                                {
                                    using (var transaction = conn.BeginTransaction())
                                    {
                                        for (var i = 0; i < dtAssemChildPartsHistory.Rows.Count; i++)
                                        {
                                            cmd.CommandText =
                                                @"INSERT INTO [6.assembly_childpart_history] (assembly_history_id, s_no, child_part_number, child_part_description,quantity, part_weight, net_weight)" +
                                                @" VALUES (@assembly_history_id, @s_no, @child_part_number, @child_part_description,@quantity, @part_weight, @net_weight);";
                                            cmd.Parameters.AddWithValue("@assembly_history_id", dtAssemChildPartsHistory.Rows[i]["assembly_history_id"].ToString());
                                            cmd.Parameters.AddWithValue("@s_no", dtAssemChildPartsHistory.Rows[i]["s_no"].ToString());
                                            cmd.Parameters.AddWithValue("@child_part_number", dtAssemChildPartsHistory.Rows[i]["child_part_number"].ToString());
                                            cmd.Parameters.AddWithValue("@child_part_description", dtAssemChildPartsHistory.Rows[i]["child_part_description"].ToString());
                                            cmd.Parameters.AddWithValue("@quantity", dtAssemChildPartsHistory.Rows[i]["quantity"].ToString());
                                            cmd.Parameters.AddWithValue("@part_weight", dtAssemChildPartsHistory.Rows[i]["part_weight"].ToString());
                                            cmd.Parameters.AddWithValue("@net_weight", dtAssemChildPartsHistory.Rows[i]["net_weight"].ToString());
                                            cmd.ExecuteNonQuery();
                                        }

                                        transaction.Commit();
                                    }
                                }


                                conn.Close();
                            }
                            MessageBox.Show("Data saved successfully in database", "SUCCESS", MessageBoxButtons.OK, MessageBoxIcon.None);
                        }


                        //Clear all entries
                        txtAssemPalletWeight.Text = lblAssemTotalNetWeight.Text = lblAssemGrossWeight.Text = "0.0000";
                        tbAssemblyPartNumber.Text = "";
                        ResetAssemblyPartsWeighing();
                        tbAssemblyPartNumber.Focus();

                        this.Cursor = Cursors.Default;
                    }
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;

                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }


            }
        }

        public static bool SendTextFileToPrinter(string szFileName, string printerName)
        {
            var sb = new StringBuilder();

            using (var sr = new StreamReader(szFileName, Encoding.Default))
            {
                while (!sr.EndOfStream)
                {
                    sb.AppendLine(sr.ReadLine());
                }
            }

            return RawPrinterHelper.SendStringToPrinter(printerName, sb.ToString());
        }


        private void BtnAssemPalletWeight_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text == "Disconnect" && lblWeight.Text != "Disconnected")
            {
                double palletWeight = Convert.ToDouble(lblWeight.Text.Replace(" KG", ""));
                if (palletWeight < 0)
                {
                    MessageBox.Show("Pallet weight cannot be negative", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    txtAssemPalletWeight.Text = lblWeight.Text.Replace(" KG", "");
                }
            }
            else
            {
                txtAssemPalletWeight.Text = "0.0000";
            }
        }

        private void BtnChildPalletWeight_Click(object sender, EventArgs e)
        {
            if (btnConnect.Text == "Disconnect" && lblWeight.Text != "Disconnected")
            {
                double palletWeight = Convert.ToDouble(lblWeight.Text.Replace(" KG", ""));
                if (palletWeight < 0)
                {
                    MessageBox.Show("Pallet weight cannot be negative", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    txtChildPalletWeight.Text = lblWeight.Text.Replace(" KG", "");
                }
            }
            else
            {
                txtChildPalletWeight.Text = "0.0000";
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
            }
        }

        private void TxtChildPalletWeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if (e.KeyChar == '.' && (sender as TextBox).Text.IndexOf('.') > -1)
            {
                e.Handled = true;
            }
        }

        private void TxtChildPalletWeight_TextChanged(object sender, EventArgs e)
        {
            CalculateChildGrossWeight();
        }

        private void TxtAssemPalletWeight_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if (e.KeyChar == '.' && (sender as TextBox).Text.IndexOf('.') > -1)
            {
                e.Handled = true;
            }
        }

        private void TxtAssemPalletWeight_TextChanged(object sender, EventArgs e)
        {
            CalculateAssemGrossWeight();
        }

        private void TbSerialNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
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

        private void pbLicenseinfo_MouseHover(object sender, EventArgs e)
        {
            pbLicenseinfo.BorderStyle = BorderStyle.FixedSingle;
            new ToolTip().SetToolTip(pbLicenseinfo, "License Info");
        }

        private void pbLicenseinfo_MouseLeave(object sender, EventArgs e)
        {
            pbLicenseinfo.BorderStyle = BorderStyle.None;
        }

        private void pbLicenseinfo_Click(object sender, EventArgs e)
        {
            try
            {
                string licensekey = appSettings.LicenseKey;

                if (!string.IsNullOrEmpty(licensekey))
                {
                    var appLicensevalidator = new AppLicenseValidator();
                    LicenseDetail licenseDetail = appLicensevalidator.GetAppLicenseDetails(licensekey);
                    ShowEnterLicenseKeyDialog(LicenseStatus.ChangeLicense, licenseDetail.ExpiryDate);
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Checking App License", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
