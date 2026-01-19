using AutoWeighingAndPrinting.Helpers;
using System.IO;
using System.Windows.Forms;

namespace AutoWeighingAndPrinting
{
    public partial class SizeSelectionForm : Form
    {
        readonly Config config;
        readonly AppSettings appSettings;
        private string prnRootPath;

        public SizeSelectionForm()
        {
            InitializeComponent();

            this.ActiveControl = lbLabelSize;
            config = new Config();
            appSettings = config.ReadSettings();
        }

        private void SizeSelectionForm_Load(object sender, System.EventArgs e)
        {
           
            prnRootPath = PrnHelper.PrnRootPath;

            if (!Directory.Exists(prnRootPath))
            {
                MessageBox.Show("PRN files folder not found", "ERROR");
                Close();
                return;
            }

            lbLabelSize.Items.Clear();
            foreach (var dir in Directory.GetDirectories(prnRootPath))
            {
                lbLabelSize.Items.Add(Path.GetFileName(dir));
            }

            // Restore last selected size
            bool itemAvailable = false;
            foreach (var item in lbLabelSize.Items)
            {
                if (item.ToString() == appSettings.SelectedSize)
                {
                    itemAvailable = true;
                    break;
                }
            }

            if (itemAvailable)
            {
                lbLabelSize.SelectedItem = appSettings.SelectedSize;
            }
        }

        private void btnNext_Click(object sender, System.EventArgs e)
        {
            if (lbLabelSize.SelectedItem == null)
            {
                MessageBox.Show("Select a size", "ERROR");
                return;
            }

            appSettings.SelectedSize = lbLabelSize.SelectedItem.ToString();
            config.WriteSettings(appSettings);

            using (CustomerSelectionForm customerForm = new CustomerSelectionForm())
            {
                customerForm.ShowDialog();
            }

            Close();
        }

        private void lbLabelSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter || e.KeyData == Keys.Return)
            {
                e.SuppressKeyPress = true;
                btnNext_Click(sender, e);
            }
        }
    }
}
