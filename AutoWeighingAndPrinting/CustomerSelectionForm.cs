using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;



namespace AutoWeighingAndPrinting
{
    public partial class CustomerSelectionForm : Form
    {
        readonly Config config;
        readonly AppSettings appSettings;

        public CustomerSelectionForm()
        {
            InitializeComponent();

            this.ActiveControl = lbCustomer;
            config = new Config();
            appSettings = config.ReadSettings();

        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if(lbCustomer.SelectedItems.Count == 1)
            {
                appSettings.LastCustomerSelected = lbCustomer.SelectedItem.ToString();
                appSettings.PrintingFileName = lbCustomer.SelectedItem.ToString();
                config.WriteSettings(appSettings);

                this.Close();
            }
            else
            {
                MessageBox.Show("Select a customer","ERROR",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
            
        }

        private void CustomerSelectionForm_Load(object sender, EventArgs e)
        {
            string currentFolderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string[] fileNames = Directory.GetFiles(currentFolderPath, "*.prn");
            Array.Sort(fileNames, StringComparer.InvariantCulture);

            foreach(var name in fileNames)
            {
                lbCustomer.Items.Add(Path.GetFileNameWithoutExtension(name));
            }

            bool itemAvailable = false;
            foreach(var item in lbCustomer.Items)
            {
                if(item.ToString() == appSettings.LastCustomerSelected)
                {
                    itemAvailable = true;
                    break;
                }
            }

            if (itemAvailable)
            {
                lbCustomer.SelectedItem = appSettings.LastCustomerSelected;
            }

            appSettings.PrintingFileName = "";
            config.WriteSettings(appSettings);

        }

        private void LbCustomer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter || e.KeyData == Keys.Return)
            {
                e.SuppressKeyPress = true;
                BtnPrint_Click(sender, e);
            }
        }
    }
}
