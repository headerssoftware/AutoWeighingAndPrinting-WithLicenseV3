using System;
using System.Windows.Forms;
using AutoWeighingAndPrinting.Helpers;

namespace AutoWeighingAndPrinting
{
    public partial class frmEnterLicenseKeyPopup : Form
    {
        private string licensekey; // field
        private bool isLicenseKeyVerified;

        private readonly Config config;
        private AppSettings appSettings;
        private readonly LicenseStatus licenseStatus;

        public LicenseDetail LicenseDetailResult { get; set; } = null;

        public bool IsLicenseKeyVerified
        {
            get { return isLicenseKeyVerified; }
            set { isLicenseKeyVerified = value; }
        }
        public frmEnterLicenseKeyPopup(AppSettings appSettings, LicenseStatus licenseStatus,DateTime? licenseExpiryDate)
        {
            InitializeComponent();
            config = new Config();
            this.appSettings = appSettings;
            this.licenseStatus = licenseStatus;

            if(appSettings.LicenseKey == null)
            {
                tbCurrentLicense.Text = "-";
            }
            else
            {
                tbCurrentLicense.Text = appSettings.LicenseKey.ToString();

            }


            if (licenseStatus == LicenseStatus.Expired)
            {
                lblLicenseStatus.Text = $"Your license got expired on {licenseExpiryDate?.ToString("dd-MM-yyyy")}, please enter the new license key to continue.";
            }
            else if (licenseStatus == LicenseStatus.NotValid)
            {
                lblLicenseStatus.Text = $"Your current license is not valid, please enter the valid license key to continue.";
            }
            else if (licenseStatus == LicenseStatus.ChangeLicense)
            {
                if(licenseExpiryDate?.Date < DateTime.Now.Date)
                {
                    lblLicenseStatus.Text = $"Your license got expired on {licenseExpiryDate?.ToString("dd-MM-yyyy")}, please enter the new license key to continue.";
                }
                else
                {
                    lblLicenseStatus.Text = $"Your current license will expire on {licenseExpiryDate?.ToString("dd-MM-yyyy")}.";
                }
               
            }
            else
            {
                lblLicenseStatus.Text = "Please enter your purchased license to continue.";
            }
        }

        private void tbLicenseKey_TextChanged(object sender, EventArgs e)
        {
            this.AcceptButton = btnSubmit;
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tbLicenseKey.Text))
                {
                    this.Cursor = Cursors.WaitCursor;
                    licensekey = tbLicenseKey.Text;

                    //Check License key is valid
                    var appLicensevalidator = new AppLicenseValidator();
                    LicenseDetail licenseDetail = appLicensevalidator.GetAppLicenseDetails(licensekey);

                    if (licenseDetail == null)
                    {
                        this.Cursor = Cursors.Default;
                        MessageBox.Show("Not a valid License Key!. Please enter a valid License Key to continue with the app!", "License Key Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        tbLicenseKey.Focus();
                        return;
                    }
                    else if(!licenseDetail.IsLicenseValid)
                    {
                        this.Cursor = Cursors.Default;
                        MessageBox.Show("Not a valid License Key!. Please enter a valid License Key to continue with the app!", "License Key Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        tbLicenseKey.Focus();
                        return;
                    }
                    else
                    {
                        DateTime licenseLastDate = licenseDetail.ExpiryDate;

                        if (licenseLastDate.Date < DateTime.Now.Date)
                        {
                            this.Cursor = Cursors.Default;
                            MessageBox.Show($"Entered License Key already got expired on {licenseLastDate.ToString("dd-MM-yyyy")}!. Please enter a valid License Key to continue with the app!", "License Key Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            tbLicenseKey.Focus();                          
                            return;
                        }
                        else
                        {
                            //Update the license key to the config file
                            appSettings.LicenseKey = licensekey;
                            config.WriteSettings(appSettings);

                            this.Cursor = Cursors.Default;
                            MessageBox.Show($"License Key updated successfully!. The License Key is valid upto {licenseLastDate.ToString("dd-MM-yyyy")}", "License Key", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            isLicenseKeyVerified = true;
                            LicenseDetailResult = licenseDetail;
                            this.Close();
                        }
                    }


                }
                else
                {
                    MessageBox.Show("Please enter the License Key to continue with the app!", "License Key Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    tbLicenseKey.Focus();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Error updating the License Key!", "License Key", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void frmEnterLicenseKeyPopup_FormClosing(object sender, FormClosingEventArgs e)
        {

            if(!isLicenseKeyVerified && licenseStatus!= LicenseStatus.ChangeLicense)
            {
                DialogResult dialogResult = MessageBox.Show("Are you want to close the License key popup?. You can't continue without entering the license key!", "License Key Required", MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
           

            
        }
    }
}
