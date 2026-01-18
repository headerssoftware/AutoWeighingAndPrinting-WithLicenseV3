using System;
using System.Data.SQLite;
using System.Drawing;
using System.Windows.Forms;
using AutoWeighingAndPrinting.Data;

namespace AutoWeighingAndPrinting
{
    public partial class AddChildPartForm : Form
    {
        private string newReferenceNumber; // field
        public string NewReferenceNumber   // property
        {
            get { return newReferenceNumber; }
            set { newReferenceNumber = value; }
        }

        private bool isNewChildPartAdded;

        public bool IsNewChildPartAdded
        {
            get { return isNewChildPartAdded; }
            set { isNewChildPartAdded = value; }
        }

        readonly Config config;
        readonly AppSettings appSettings;
        readonly string connectionString;

        public AddChildPartForm()
        {
            InitializeComponent();
            IsNewChildPartAdded = false;
            lblMessage.Text = "";

            config = new Config();
            appSettings = config.ReadSettings();
            connectionString = @"DataSource=" + appSettings.DBFilePath + ";foreign keys=true;";
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


        private void BtnAddPartNumber_Click(object sender, EventArgs e)
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

                byte[] imgByteArr = null;

                if (partNumber == string.Empty || description == string.Empty || defaultQty == string.Empty || totalWeight == string.Empty
                    || partWeight == string.Empty || tolPercent == string.Empty || tolPartWeight == string.Empty
                    || negativeTolPercent == string.Empty || negativeTolPartWeight == string.Empty || positiveTolPercent == string.Empty || positiveTolPartWeight == string.Empty || referenceNumber == string.Empty || expiryDays == string.Empty)
                {
                    MessageBox.Show("Please fill all the fields", "Data Empty Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                this.Cursor = Cursors.WaitCursor;

                //Check Reference Number Exist
                string commandText0 = @"SELECT COUNT(reference_number) FROM [1.childpart] WHERE reference_number=@REFERENCENUMBER";

                SQLiteParameter[] parameters0 = new SQLiteParameter[1];

                parameters0[0] = new SQLiteParameter("@REFERENCENUMBER", referenceNumber);

                int referenceNumberCount = Convert.ToInt32(SqlLiteHelper.ExecuteScalar(connectionString, commandText0, parameters0));

                if (referenceNumberCount > 0)
                {
                    MessageBox.Show("Reference Number Already Exists.", "Repeat Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Cursor = Cursors.Default;
                    return;
                }

                //Check Part Number Exist
                commandText0 = @"SELECT COUNT(child_part_number) FROM [1.childpart] WHERE child_part_number=@PARTNUMBER";
                SQLiteParameter[] parameters1 = new SQLiteParameter[1];
                parameters1[0] = new SQLiteParameter("@PARTNUMBER", partNumber);

                int partNumberCount = Convert.ToInt32(SqlLiteHelper.ExecuteScalar(connectionString, commandText0, parameters1));

                if (partNumberCount > 0)
                {
                    MessageBox.Show("Part Number Already Exists.", "Repeat Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Cursor = Cursors.Default;
                    return;
                }


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

                commandText0 = @"INSERT INTO [1.childpart]('child_part_number','child_part_description','default_qty','total_weight','part_weight','tol_percent','tol_part_weight','negative_tol_percent','negative_tol_part_weight','positive_tol_percent','positive_tol_part_weight','image','reference_number','expiry_days') 
                                    VALUES (@PARTNUMBER,@DESCRIPTION,@DEFAULTQTY,@TOTALWEIGHT,@PARTWEIGHT,@TOLPERCENT,@TOLPARTWEIGHT,@NEGATIVETOLPERCENT,@NEGATIVETOLPARTWEIGHT,@POSITIVETOLPERCENT,@POSITIVETOLPARTWEIGHT,@IMAGE,@REFERENCENUMBER,@EXPIRYDAYS)";

                SQLiteParameter[] parameters2 = new SQLiteParameter[14];
                parameters2[0] = new SQLiteParameter("@PARTNUMBER", partNumber);
                parameters2[1] = new SQLiteParameter("@DESCRIPTION", description);
                parameters2[2] = new SQLiteParameter("@DEFAULTQTY", defaultQty);
                parameters2[3] = new SQLiteParameter("@TOTALWEIGHT", totalWeight);
                parameters2[4] = new SQLiteParameter("@PARTWEIGHT", partWeight);
                parameters2[5] = new SQLiteParameter("@TOLPERCENT", tolPercent);
                parameters2[6] = new SQLiteParameter("@TOLPARTWEIGHT", tolPartWeight);
                parameters2[7] = new SQLiteParameter("@NEGATIVETOLPERCENT", negativeTolPercent);
                parameters2[8] = new SQLiteParameter("@NEGATIVETOLPARTWEIGHT", negativeTolPartWeight);
                parameters2[9] = new SQLiteParameter("@POSITIVETOLPERCENT", positiveTolPercent);
                parameters2[10] = new SQLiteParameter("@POSITIVETOLPARTWEIGHT", positiveTolPartWeight);
                parameters2[11] = new SQLiteParameter("@IMAGE", imgByteArr);
                parameters2[12] = new SQLiteParameter("@REFERENCENUMBER", referenceNumber);
                parameters2[13] = new SQLiteParameter("@EXPIRYDAYS", expiryDays);

                SqlLiteHelper.ExecuteNonQuery(connectionString, commandText0, parameters2);

                this.NewReferenceNumber = referenceNumber;
                this.IsNewChildPartAdded = true;
                MessageBox.Show("Data Added!");

                //Reset the fields
                BtnReset_Click(sender, e);

                con.Close();
                this.Cursor = Cursors.Default;

            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;

                con.Close();
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            tbReferenceNumber.Text = tbExpiryDays.Text = string.Empty;
            tbPartNumber.Text = tbDescription.Text = string.Empty;
            tbDefaultQty.Text = tbTotalWeight.Text = tbPartWeight.Text = tbTolPercent.Text = tbTolPartWeight.Text = string.Empty;
            tbNegativeTolPercent.Text = tbNegativeTolPartWeight.Text = tbPositiveTolPercent.Text = tbPositiveTolPartWeight.Text = string.Empty;

            pbImage.Image = null;
            tbReferenceNumber.Focus();
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

        private void TbDefaultQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            // only allow integers
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
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

        private void TbPartWeight_TextChanged(object sender, EventArgs e)
        {
            TbTolPercent_TextChanged(sender, e);
            TbNegativeTolPercent_TextChanged(sender, e);
            TbPositiveTolPercent_TextChanged(sender, e);
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

        private void AddChildPartForm_Load(object sender, EventArgs e)
        {
            tbNegativeTolPercent.KeyUp += ProcessToleranceChange;
            tbPositiveTolPercent.KeyUp += ProcessToleranceChange;
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


    }
}
