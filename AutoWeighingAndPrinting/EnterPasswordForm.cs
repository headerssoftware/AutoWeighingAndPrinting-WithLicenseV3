using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;

namespace AutoWeighingAndPrinting
{
    public partial class EnterPasswordForm : Form
    {
        readonly Config config;
        readonly AppSettings appSettings;
        readonly string connectionString;

        private bool isPasswordCorrect;

        public bool IsPasswordCorrect
        {
            get { return isPasswordCorrect; }
            set { isPasswordCorrect = value; }
        }


        public EnterPasswordForm()
        {
            InitializeComponent();
            IsPasswordCorrect = false;
            tbPassword.Clear();
            tbPassword.Focus();


            config = new Config();
            appSettings = config.ReadSettings();
            connectionString = @"DataSource=" + appSettings.DBFilePath + ";foreign keys=true;";

        }


        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                string enteredPassword = tbPassword.Text;

                string sql = @"SELECT password as PASSWORD FROM [7.users] WHERE username='ADMIN'";
                DataTable dataTable = new DataTable();
                using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                {
                    dataAdapter.Fill(dataTable);
                }

                string dbPassword = dataTable.Rows[0]["PASSWORD"].ToString();   //password in database

                //comparing the passwords with case sensitive
                IsPasswordCorrect = enteredPassword.Equals(dbPassword, System.StringComparison.Ordinal);

                this.Cursor = Cursors.Default;

                if (IsPasswordCorrect == false)
                {
                    MessageBox.Show("Wrong Password. Please try again", "Password Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    tbPassword.Clear();
                    tbPassword.Focus();
                }
                else
                {
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TbPassword_TextChanged(object sender, EventArgs e)
        {
            this.AcceptButton = btnSubmit;
        }
    }
}
