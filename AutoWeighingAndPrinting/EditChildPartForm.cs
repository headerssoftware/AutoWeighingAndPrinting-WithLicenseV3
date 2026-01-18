using System;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using AutoWeighingAndPrinting.Data;

namespace AutoWeighingAndPrinting
{
    public partial class EditChildPartForm : Form
    {
        readonly DataGridViewRow dataGridViewRow = null;

        readonly Config config;
        readonly AppSettings appSettings;
        readonly string connectionString;

        private bool isChildPartUpdated;

        public bool IsChildPartUpdated
        {
            get { return isChildPartUpdated; }
            set { isChildPartUpdated = value; }
        }

        public EditChildPartForm(DataGridViewRow dgvRow)
        {
            InitializeComponent();
            IsChildPartUpdated = false;

            config = new Config();
            appSettings = config.ReadSettings();
            connectionString = @"DataSource=" + appSettings.DBFilePath + ";foreign keys=true;";

            dataGridViewRow = dgvRow;

            tbReferenceNumber.Text = dataGridViewRow.Cells["ReferenceNumber"].Value.ToString();
            tbReferenceNumber.Enabled = false;

            tbExpiryDays.Text = dataGridViewRow.Cells["ExpiryDays"].Value.ToString();
            tbSideInfo.Text = dataGridViewRow.Cells["SideInfo"].Value.ToString();

            tbPartNumber.Text = dataGridViewRow.Cells["PartNumber"].Value.ToString();
            tbPartNumber.Enabled = false;

            tbDescription.Text = dataGridViewRow.Cells["Description"].Value.ToString();
            tbDescription.Focus();
            tbDescription.SelectionStart = tbDescription.Text.Length;

            tbDefaultQty.Text = dataGridViewRow.Cells["DefaultQty"].Value.ToString();
            tbTotalWeight.Text = dataGridViewRow.Cells["TotalWeight"].Value.ToString();

            // Part weight textbox is automatically calculated

            tbTolPercent.Text = dataGridViewRow.Cells["TolerancePercent"].Value.ToString();
            //TolPartWeight  is automatically calculated

            tbNegativeTolPercent.Text = dataGridViewRow.Cells["NegativeTolerancePercent"].Value.ToString();

            tbPositiveTolPercent.Text = dataGridViewRow.Cells["PositiveTolerancePercent"].Value.ToString();

            if (dataGridViewRow.Cells["Image"].Value != DBNull.Value)
            {
                var data = (Byte[])(dataGridViewRow.Cells["Image"].Value);
                var stream = new MemoryStream(data);
                pbImage.Image = Image.FromStream(stream);
            }
            else
            {
                pbImage.Image = null;
            }

        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            // open file dialog   
            OpenFileDialog open = new OpenFileDialog
            {
                // image filters  
                Filter = "Image Files(*.jpg; *.jpeg; *.png)|*.jpg; *.jpeg; *.png"
            };
            if (open.ShowDialog() == DialogResult.OK)
            {
                // display image in picture box  
                pbImage.Image = new Bitmap(open.FileName);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            pbImage.Image = null;

        }


        private void BtnUpdatePartNumber_Click(object sender, EventArgs e)
        {
            SQLiteConnection con = new SQLiteConnection(connectionString, true);

            try
            {

                string partNumber = tbPartNumber.Text;
                string description = tbDescription.Text;
                string defaultQty = tbDefaultQty.Text;
                string totalWeight = tbTotalWeight.Text;
                string partWeight = tbPartWeight.Text;
                string tolPercent = tbTolPercent.Text;
                string tolPartWeight = tbTolPartWeight.Text;
                string negativeTolPercent = tbNegativeTolPercent.Text;
                string negativeTolPartWeight = tbNegativeTolPartWeight.Text;
                string positiveTolPercent = tbPositiveTolPercent.Text;
                string positiveTolPartWeight = tbPositiveTolPartWeight.Text;
                string referenceNumber = tbReferenceNumber.Text;
                string expiryDays = tbExpiryDays.Text;
                string sideInfo = tbSideInfo.Text;

                byte[] imgByteArr = null;

                if (partNumber == string.Empty || description == string.Empty || defaultQty == string.Empty || totalWeight == string.Empty
                    || partWeight == string.Empty || tolPercent == string.Empty || tolPartWeight == string.Empty
                    || negativeTolPercent == string.Empty || negativeTolPartWeight == string.Empty || positiveTolPercent == string.Empty || positiveTolPartWeight == string.Empty || referenceNumber == string.Empty || expiryDays == string.Empty)
                {
                    MessageBox.Show("Please fill all the fields", "Data Empty Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                this.Cursor = Cursors.WaitCursor;


                if (pbImage.Image == null)
                {
                    imgByteArr = null;
                }
                else
                {
                    Image img = pbImage.Image;
                    ImageConverter converter = new ImageConverter();
                    imgByteArr = (byte[])converter.ConvertTo(img, typeof(byte[]));
                }
                string commandText0 = @"UPDATE [1.childpart] SET child_part_description = @DESCRIPTION,
                                                default_qty = @DEFAULTQTY, total_weight = @TOTALWEIGHT,
                                                part_weight = @PARTWEIGHT, tol_percent = @TOLERANCEPERCENT, tol_part_weight = @TOLERANCEPARTWEIGHT,
                                                negative_tol_percent = @NEGATIVETOLPERCENT, negative_tol_part_weight = @NEGATIVETOLPARTWEIGHT,
                                                positive_tol_percent = @POSITIVETOLPERCENT, positive_tol_part_weight = @POSITIVETOLPARTWEIGHT,
                                                image = @IMAGE,expiry_days=@EXPIRYDAYS ,side_info = @SIDEINFO
                                                WHERE reference_number = @REFERENCENUMBER";

                SQLiteParameter[] parameters0 = new SQLiteParameter[14];
                parameters0[0] = new SQLiteParameter("@DESCRIPTION", description);
                parameters0[1] = new SQLiteParameter("@DEFAULTQTY", defaultQty);
                parameters0[2] = new SQLiteParameter("@TOTALWEIGHT", totalWeight);
                parameters0[3] = new SQLiteParameter("@PARTWEIGHT", partWeight);
                parameters0[4] = new SQLiteParameter("@TOLERANCEPERCENT", tolPercent);
                parameters0[5] = new SQLiteParameter("@TOLERANCEPARTWEIGHT", tolPartWeight);
                parameters0[6] = new SQLiteParameter("@NEGATIVETOLPERCENT", negativeTolPercent);
                parameters0[7] = new SQLiteParameter("@NEGATIVETOLPARTWEIGHT", negativeTolPartWeight);
                parameters0[8] = new SQLiteParameter("@POSITIVETOLPERCENT", positiveTolPercent);
                parameters0[9] = new SQLiteParameter("@POSITIVETOLPARTWEIGHT", positiveTolPartWeight);
                parameters0[10] = new SQLiteParameter("@IMAGE", imgByteArr);
                parameters0[11] = new SQLiteParameter("@REFERENCENUMBER", referenceNumber);
                parameters0[12] = new SQLiteParameter("@EXPIRYDAYS", expiryDays);
                parameters0[13] = new SQLiteParameter("@SIDEINFO", sideInfo);

                SqlLiteHelper.ExecuteNonQuery(connectionString, commandText0, parameters0);

                IsChildPartUpdated = true;

                this.Cursor = Cursors.Default;

                MessageBox.Show("Data Updated!");
                this.Close();
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;

                con.Close();
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }


        private void TbWeight_KeyPress(object sender, KeyPressEventArgs e)
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

        private void TbTolerance_KeyPress(object sender, KeyPressEventArgs e)
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

        private void TbDefaultQty_TextChanged(object sender, EventArgs e)
        {
            if (tbDefaultQty.Text.StartsWith("0"))
            {
                tbDefaultQty.Text = "";
            }

            if (tbDefaultQty.Text == string.Empty || tbTotalWeight.Text == string.Empty)
            {
                tbPartWeight.Text = "";
            }
            else
            {
                double totalWeight = Convert.ToDouble(tbTotalWeight.Text);
                int defaultQty = Convert.ToInt32(tbDefaultQty.Text);
                tbPartWeight.Text = (totalWeight / defaultQty).ToString("0.000000");
                lblMessage.Text = "";
            }
        }

        private void TbTotalWeight_TextChanged(object sender, EventArgs e)
        {
            if (tbDefaultQty.Text == string.Empty || tbTotalWeight.Text == string.Empty)
            {
                tbPartWeight.Text = "";
            }
            else
            {
                double totalWeight = Convert.ToDouble(tbTotalWeight.Text);
                int defaultQty = Convert.ToInt32(tbDefaultQty.Text);
                tbPartWeight.Text = (totalWeight / defaultQty).ToString("0.000000");
                lblMessage.Text = "";
            }
        }

        private void TbTolPercent_TextChanged(object sender, EventArgs e)
        {
            if (tbPartWeight.Text == string.Empty)
            {
                lblMessage.Text = "* Enter Qty and Total Weight";
                tbTolPercent.Text = "";
            }

            if (tbTolPercent.Text == string.Empty)
            {
                lblTolerance.Text = "% of Part Weight (KG)";
                tbTolPartWeight.Text = "";

                tbNegativeTolPercent.Text = tbPositiveTolPercent.Text = "";
            }

            if (tbPartWeight.Text != string.Empty && tbTolPercent.Text != string.Empty)
            {
                double tolerancePercent = Convert.ToDouble(tbTolPercent.Text);
                if (tolerancePercent >= 100)
                {
                    lblMessage.Text = "* Tolerance Percent must be less than 100%";
                    tbTolPercent.Text = "";
                }
                else
                {
                    lblMessage.Text = "";
                    double partWeight = Convert.ToDouble(tbPartWeight.Text);
                    double tolerancePartWeight = (tolerancePercent * partWeight) / 100;
                    lblTolerance.Text = tolerancePercent + "% of Part Weight (KG)";
                    tbTolPartWeight.Text = tolerancePartWeight.ToString("0.000000");

                    // If part weight is less than 1 gram, then set negative tolerance to 0%
                    if (partWeight < 0.001)
                    {
                        tbNegativeTolPercent.Text = "0";
                        tbPositiveTolPercent.Text = tbTolPercent.Text;

                        tbNegativeTolPercent.Enabled = tbPositiveTolPercent.Enabled = false;
                    }
                    else
                    {
                        double DivideToleranceBy2 = tolerancePercent / 2;

                        tbNegativeTolPercent.Text = DivideToleranceBy2.ToString();
                        tbPositiveTolPercent.Text = DivideToleranceBy2.ToString();

                        tbNegativeTolPercent.Enabled = tbPositiveTolPercent.Enabled = true;

                    }
                }
            }
            if (tbPartWeight.Text != string.Empty && tbTolPercent.Text != string.Empty && tbNegativeTolPercent.Text == "0")
            {
                double partWeight = Convert.ToDouble(tbPartWeight.Text);
                if (partWeight < 0.001)
                {
                    lblMessage.Text = "* If the part weight is less than 1 Gram then Negative Tolerance must be 0%";
                }

            }
        }

        private void TbDefaultQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            // only allow integers
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TbTotalWeight_KeyPress(object sender, KeyPressEventArgs e)
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


        private void TbPartWeight_TextChanged(object sender, EventArgs e)
        {
            TbTolPercent_TextChanged(sender, e);
            TbNegativeTolPercent_TextChanged(sender, e);
            TbPositiveTolPercent_TextChanged(sender, e);
        }

        private void TbTolPercent_KeyPress(object sender, KeyPressEventArgs e)
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

        private void TbNegativeTolPercent_KeyPress(object sender, KeyPressEventArgs e)
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

        private void TbNegativeTolPercent_TextChanged(object sender, EventArgs e)
        {
            if (tbNegativeTolPercent.Text != string.Empty)
            {
                double partWeight = Convert.ToDouble(tbPartWeight.Text);

                if (partWeight < 0.001)
                {
                    lblMessage.Text = "* If the part weight is less than 1 Gram then Negative Tolerance must be 0%";
                }
                else
                {
                    lblMessage.Text = "";
                }


                double negativeTolerance = Convert.ToDouble(tbNegativeTolPercent.Text);

                lblNegativeTolerance.Text = negativeTolerance + "% of Part Weight (KG)";
                double negativeTolPartWeight = (negativeTolerance * partWeight) / 100;
                tbNegativeTolPartWeight.Text = negativeTolPartWeight.ToString("0.000000");

            }
            else
            {
                lblNegativeTolerance.Text = "% of Part Weight (KG)";
                tbNegativeTolPartWeight.Text = "";
            }
        }

        private void TbPositiveTolPercent_KeyPress(object sender, KeyPressEventArgs e)
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

        private void tbExpiryDays_KeyPress(object sender, KeyPressEventArgs e)
        {
            // only allow integers
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void TbPositiveTolPercent_TextChanged(object sender, EventArgs e)
        {
            if (tbPositiveTolPercent.Text != string.Empty)
            {
                lblMessage.Text = "";
                double partWeight = Convert.ToDouble(tbPartWeight.Text);
                double positiveTolerance = Convert.ToDouble(tbPositiveTolPercent.Text);

                lblPositiveTolerance.Text = positiveTolerance + "% of Part Weight (KG)";
                double positiveTolPartWeight = (positiveTolerance * partWeight) / 100;
                tbPositiveTolPartWeight.Text = positiveTolPartWeight.ToString("0.000000");
            }
            else
            {
                lblPositiveTolerance.Text = "% of Part Weight (KG)";
                tbPositiveTolPartWeight.Text = "";
            }
        }

        private void ProcessToleranceChange(object sender, KeyEventArgs ka)
        {
            if (sender == tbNegativeTolPercent)
            {
                if (tbTolPercent.Text != string.Empty && tbNegativeTolPercent.Text != string.Empty)
                {

                    double tolerancePercent = Convert.ToDouble(tbTolPercent.Text);
                    double negativeTolPercent = Convert.ToDouble(tbNegativeTolPercent.Text);
                    if (negativeTolPercent <= tolerancePercent)
                    {
                        double positiveTolPercent = tolerancePercent - negativeTolPercent;
                        tbPositiveTolPercent.Text = positiveTolPercent.ToString();
                        lblMessage.Text = "";
                    }
                    else
                    {
                        lblMessage.Text = "* Negative Tolerance Value must be less than Tolerance Percent Value";
                        tbNegativeTolPercent.Text = tbPositiveTolPercent.Text = "";
                    }

                }
                else if (tbTolPercent.Text != string.Empty && tbNegativeTolPercent.Text == string.Empty)
                {
                    tbNegativeTolPercent.Text = tbPositiveTolPercent.Text = "";
                }
            }

            else if (sender == tbPositiveTolPercent)
            {
                if (tbTolPercent.Text != string.Empty && tbPositiveTolPercent.Text != string.Empty)
                {
                    double tolerancePercent = Convert.ToDouble(tbTolPercent.Text);
                    double positiveTolPercent = Convert.ToDouble(tbPositiveTolPercent.Text);
                    if (positiveTolPercent <= tolerancePercent)
                    {
                        double negativeTolPercent = tolerancePercent - positiveTolPercent;
                        tbNegativeTolPercent.Text = negativeTolPercent.ToString();
                        lblMessage.Text = "";
                    }
                    else
                    {
                        lblMessage.Text = "* Positive Tolerance Value must be less than Tolerance Percent Value";
                        tbNegativeTolPercent.Text = tbPositiveTolPercent.Text = "";
                    }
                }
                else if (tbTolPercent.Text != string.Empty && tbPositiveTolPercent.Text == string.Empty)
                {
                    tbNegativeTolPercent.Text = tbPositiveTolPercent.Text = "";
                }
            }
        }


        private void EditChildPartForm_Load(object sender, EventArgs e)
        {
            tbNegativeTolPercent.KeyUp += ProcessToleranceChange;
            tbPositiveTolPercent.KeyUp += ProcessToleranceChange;
        }


    }
}
