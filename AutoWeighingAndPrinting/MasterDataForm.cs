using System;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using PS = AutoWeighingAndPrinting.Properties.Settings;

//For excel sheet
using ClosedXML.Excel;


namespace AutoWeighingAndPrinting
{
    public partial class MasterDataForm : Form
    {
        readonly Config config;
        readonly AppSettings appSettings;
        readonly string connectionString;

        [Obsolete]
        public MasterDataForm()
        {
            InitializeComponent();
            config = new Config();
            appSettings = config.ReadSettings();
            //string value from app.config
            connectionString = @"DataSource=" + appSettings.DBFilePath + ";foreign keys=true;";


        }


        string ChildPartID = null;

        DataTable dtViewChildPartsHistory = null;



        private void BtnViewChildParts_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                DataTable dtViewChildParts = new DataTable();
                string sql = @"SELECT childpart_id as ID, reference_number as ReferenceNumber,child_part_number as PartNumber, child_part_description as Description, default_qty as DefaultQty, total_weight as TotalWeight,
                                part_weight as PartWeight, tol_percent as TolerancePercent, tol_part_weight as TolerancePartWeight,
                                negative_tol_percent as NegativeTolerancePercent, negative_tol_part_weight as NegativeTolerancePartWeight,
                                positive_tol_percent as PositiveTolerancePercent, positive_tol_part_weight as PositiveTolerancePartWeight,
                                image as Image, expiry_days as ExpiryDays,batch_no as LastBatchNumber FROM [1.childpart] order by ReferenceNumber";
                using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                {
                    dataAdapter.Fill(dtViewChildParts);
                }

                dgvViewChildParts.DataSource = dtViewChildParts;
                dgvViewChildParts.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;


                dgvViewChildParts.Columns["ID"].Visible = false;
                dgvViewChildParts.Columns["Image"].Visible = false;

                for (int i = 1; i < dgvViewChildParts.Columns.Count; i++)
                {
                    dgvViewChildParts.Columns[i].ReadOnly = true;
                }

                dgvViewChildParts.ClearSelection();
                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                dgvViewChildParts.DataSource = null;
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddChildParts_Click(object sender, EventArgs e)
        {
            AddChildPartForm addChildPartForm = new AddChildPartForm();
            addChildPartForm.ShowDialog();
            if (addChildPartForm.IsNewChildPartAdded)
            {
                BtnViewChildParts_Click(sender, e);
                foreach (DataGridViewRow row in dgvViewChildParts.Rows)
                {
                    if (row.Cells["ReferenceNumber"].Value.ToString().Equals(addChildPartForm.NewReferenceNumber))
                    {
                        int rowIndex = row.Index;
                        dgvViewChildParts.Rows[rowIndex].Selected = true;
                        break;
                    }
                }

                addChildPartForm.IsNewChildPartAdded = false;
            }
        }

        private void DgvViewChildParts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvViewChildParts.DataSource != null && dgvViewChildParts.SelectedRows.Count == 1)
            {

                if (dgvViewChildParts.SelectedRows[0].Cells["Image"].Value != DBNull.Value)
                {
                    var data = (Byte[])(dgvViewChildParts.SelectedRows[0].Cells["Image"].Value);
                    var stream = new MemoryStream(data);
                    pbImage.Image = Image.FromStream(stream);
                }
                else
                {
                    pbImage.Image = null;
                }
            }
            else
            {
                pbImage.Image = null;
            }
        }

        private void BtnEditChildPart_Click(object sender, EventArgs e)
        {
            if (dgvViewChildParts.SelectedRows.Count == 1 && dgvViewChildParts.DataSource != null)
            {
                EnterPasswordForm enterPasswordForm = new EnterPasswordForm();
                enterPasswordForm.ShowDialog();

                if (enterPasswordForm.IsPasswordCorrect)
                {
                    EditChildPartForm editChildPartForm = new EditChildPartForm(dgvViewChildParts.SelectedRows[0]);
                    editChildPartForm.ShowDialog();
                    if (editChildPartForm.IsChildPartUpdated)
                    {
                        BtnViewChildParts_Click(sender, e);
                        editChildPartForm.IsChildPartUpdated = false;
                    }
                }
            }
            else
            {
                MessageBox.Show("Select the required row to open update form");
            }
        }

        private void BtnDeleteChildPart_Click(object sender, EventArgs e)
        {
            bool isCheckboxSelected = false;

            //Check if CheckBox are selected
            if (dgvViewChildParts.DataSource != null && dgvViewChildParts.Rows.Count >= 1)
            {
                for (int i = 0; i < dgvViewChildParts.Rows.Count; i++)
                {
                    if (dgvViewChildParts.Rows[i].Cells[0].Value != null)
                    {
                        bool cellValue = (bool)dgvViewChildParts.Rows[i].Cells[0].Value;
                        if (cellValue)
                        {
                            isCheckboxSelected = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a checkbox to delete the data", "Error: None Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (isCheckboxSelected)
            {
                //Check if password is correct, else return
                EnterPasswordForm enterPasswordForm = new EnterPasswordForm();
                enterPasswordForm.ShowDialog();

                if (!enterPasswordForm.IsPasswordCorrect)
                {
                    return;
                }
            }

            //Check if child parts are assocaited with Assembly Part number
            bool isChildPartPresentInAssembly = false;
            if (isCheckboxSelected)
            {

                for (int i = 0; i < dgvViewChildParts.Rows.Count; i++)
                {
                    if (dgvViewChildParts.Rows[i].Cells[0].Value != null)
                    {
                        bool cellValue = (bool)dgvViewChildParts.Rows[i].Cells[0].Value;
                        if (cellValue)
                        {
                            SQLiteConnection con = new SQLiteConnection(connectionString, true);
                            try
                            {
                                int childpart_id = Convert.ToInt32(dgvViewChildParts.Rows[i].Cells["ID"].Value.ToString());

                                this.Cursor = Cursors.WaitCursor;

                                con.Open();

                                SQLiteCommand cmd = new SQLiteCommand(con)
                                {

                                    // If this sql request return false
                                    CommandText = @"SELECT childpart_id FROM [3.assembly_childpart] WHERE childpart_id=@ID"
                                };
                                cmd.Parameters.AddWithValue("@ID", childpart_id);
                                int count = Convert.ToInt32(cmd.ExecuteScalar());
                                con.Close();

                                if (count == 0)
                                {

                                }
                                else
                                {
                                    this.Cursor = Cursors.Default;
                                    isChildPartPresentInAssembly = true;
                                    MessageBox.Show(@"Selected Child Part Number[s] are associated with Assembly Part Number and cannot be deleted." +
                                                    "\nRemove the Child Part Number from the associated Assembly Part and then try again", "Delete Restricted", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }

                                this.Cursor = Cursors.Default;

                            }
                            catch (Exception ex)
                            {
                                this.Cursor = Cursors.Default;

                                con.Close();
                                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a checkbox to delete the data", "Error: None Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            //If Child Parts are not present in Assembly Parts, then delete the child parts
            if (!isChildPartPresentInAssembly)
            {
                DialogResult dr = MessageBox.Show("Are you sure to delete the selected child part numbers?", "Delete Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    //delete row from database or datagridview...


                    this.Cursor = Cursors.WaitCursor;
                    SQLiteConnection con = new SQLiteConnection(connectionString, true);
                    con.Open();

                    for (int i = 0; i < dgvViewChildParts.Rows.Count; i++)
                    {
                        if (dgvViewChildParts.Rows[i].Cells[0].Value != null)
                        {
                            bool cellValue = (bool)dgvViewChildParts.Rows[i].Cells[0].Value;
                            if (cellValue)
                            {
                                try
                                {
                                    int childpart_id = Convert.ToInt32(dgvViewChildParts.Rows[i].Cells["ID"].Value.ToString());

                                    SQLiteCommand cmd = new SQLiteCommand(con)
                                    {
                                        //Continue with the child part deletion
                                        CommandText = @"DELETE FROM [1.childpart] where childpart_id=@ID"
                                    };
                                    cmd.Parameters.AddWithValue("@ID", childpart_id);
                                    cmd.ExecuteNonQuery();

                                }
                                catch (Exception ex)
                                {
                                    this.Cursor = Cursors.Default;

                                    con.Close();
                                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                    }
                    con.Close();

                    this.Cursor = Cursors.Default;
                    BtnViewChildParts_Click(sender, e);
                    MessageBox.Show("Data Deleted Succcessfully", "Delete Report", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
            }
            else
            {
                MessageBox.Show("Select a checkbox to delete the data", "Error: None Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void BtnAddAssembly_Click(object sender, EventArgs e)
        {
            SQLiteConnection con = new SQLiteConnection(connectionString, true);
            try
            {

                string partNumber = tbAssemblyPartNumber.Text;
                string description = tbAssemblyDesc.Text;


                if (partNumber == string.Empty || description == string.Empty)
                {
                    MessageBox.Show("Please fill all the fields", "Data Empty Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                this.Cursor = Cursors.WaitCursor;

                con.Open();
                SQLiteCommand cmd = new SQLiteCommand(con)
                {

                    // If this sql request return false
                    CommandText = @"SELECT assembly_part_number FROM [2.assembly] WHERE assembly_part_number=@PARTNUMBER"
                };
                cmd.Parameters.AddWithValue("@PARTNUMBER", partNumber);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {

                    cmd.CommandText = @"INSERT INTO [2.assembly]('assembly_part_number','assembly_part_description') 
                                    VALUES (@PARTNUMBER,@DESCRIPTION)";
                    cmd.Parameters.AddWithValue("@PARTNUMBER", partNumber);
                    cmd.Parameters.AddWithValue("@DESCRIPTION", description);

                    cmd.ExecuteNonQuery();

                    BtnViewAssemblyParts_Click(sender, e);

                    foreach (DataGridViewRow row in dgvAssemblyParts.Rows)
                    {
                        if (row.Cells["AssemblyPartNumber"].Value.ToString().Equals(partNumber))
                        {
                            dgvAssemblyParts.Rows[row.Index].Selected = true;
                            break;
                        }
                    }
                    MessageBox.Show("Data Added!");

                    //Reset the fields
                    tbAssemblyPartNumber.Text = tbAssemblyDesc.Text = "";
                    tbChildPart.Focus();


                }
                else
                {
                    MessageBox.Show("Part Number Already Exists.", "Repeat Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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

        private void BtnViewAssemblyParts_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                DataTable dtViewAssemblyParts = new DataTable();
                string sql = @"SELECT assembly_id as ID, assembly_part_number as AssemblyPartNumber, assembly_part_description as Description FROM [2.assembly] order by AssemblyPartNumber";
                using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                {
                    using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                    {
                        dataAdapter.Fill(dtViewAssemblyParts);
                        con.Close();
                    }
                }
                dgvAssemblyParts.DataSource = dtViewAssemblyParts;
                dgvAssemblyParts.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                dgvAssemblyParts.Columns["Description"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvAssemblyParts.Columns["AssemblyPartNumber"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;

                dgvAssemblyParts.Columns["ID"].Visible = false;

                for (int i = 1; i < dgvAssemblyParts.Columns.Count; i++)
                {
                    dgvAssemblyParts.Columns[i].ReadOnly = true;
                }

                dgvAssemblyParts.ClearSelection();
                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                dgvAssemblyParts.DataSource = null;
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvAssemblyParts_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAssemblyParts.DataSource != null && dgvAssemblyParts.SelectedRows.Count == 1)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;

                    string assemblyID = dgvAssemblyParts.SelectedRows[0].Cells["ID"].Value.ToString();

                    DataTable dtViewChildParts = new DataTable();
                    string sql = @"Select
                                        [3.assembly_childpart].s_no as [S.No],
                                        [1.childpart].childpart_id as ChildPartID,
                                        [1.childpart].child_part_number as PartNumber,
                                        [1.childpart].child_part_description as Description,
                                        [1.childpart].part_weight as PartWeight,
                                        [3.assembly_childpart].no_of_qty as NoOfQty
                                    From
                                        [2.assembly] Inner Join
                                        [3.assembly_childpart] On [2.assembly].assembly_id = [3.assembly_childpart].assembly_id Inner Join
                                        [1.childpart] On [1.childpart].childpart_id = [3.assembly_childpart].childpart_id
                                    Where
                                        [2.assembly].assembly_id = " + assemblyID + @"
                                    Order By
                                        [3.assembly_childpart].s_no";

                    using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                    {
                        using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                        {

                            dataAdapter.Fill(dtViewChildParts);
                            con.Close();
                        }
                    }

                    dgvChildPartsFromAssembly.DataSource = dtViewChildParts;
                    dgvChildPartsFromAssembly.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
                    dgvChildPartsFromAssembly.Columns["NoOfQty"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                    dgvChildPartsFromAssembly.Columns["ChildPartID"].Visible = false;

                    for (int i = 1; i < dgvChildPartsFromAssembly.Columns.Count; i++)
                    {
                        dgvChildPartsFromAssembly.Columns[i].ReadOnly = true;
                    }

                    dgvChildPartsFromAssembly.ClearSelection();
                    this.Cursor = Cursors.Default;
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    dgvChildPartsFromAssembly.DataSource = null;
                    dgvChildPartsFromAssembly.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else
            {
                dgvChildPartsFromAssembly.DataSource = null;
                dgvChildPartsFromAssembly.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            }
        }

        private void TbAssemblyDesc_TextChanged(object sender, EventArgs e)
        {
            this.AcceptButton = btnAddAssembly;
        }

        private void BtnDeleteAssemblyPart_Click(object sender, EventArgs e)
        {
            bool isCheckboxSelected = false;

            //Check if CheckBox are selected
            if (dgvAssemblyParts.DataSource != null && dgvAssemblyParts.Rows.Count >= 1)
            {
                for (int i = 0; i < dgvAssemblyParts.Rows.Count; i++)
                {
                    if (dgvAssemblyParts.Rows[i].Cells[0].Value != null)
                    {
                        bool cellValue = (bool)dgvAssemblyParts.Rows[i].Cells[0].Value;
                        if (cellValue)
                        {
                            isCheckboxSelected = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a checkbox to delete the data", "Error: None Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (isCheckboxSelected)
            {
                //Check if password is correct, else return
                EnterPasswordForm enterPasswordForm = new EnterPasswordForm();
                enterPasswordForm.ShowDialog();

                if (!enterPasswordForm.IsPasswordCorrect)
                {
                    return;
                }
            }


            if (isCheckboxSelected)
            {
                DialogResult dr = MessageBox.Show("Are you sure to delete the selected assembly part numbers?", "Delete Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    //delete row from database


                    this.Cursor = Cursors.WaitCursor;
                    SQLiteConnection con = new SQLiteConnection(connectionString, true);
                    con.Open();

                    for (int i = 0; i < dgvAssemblyParts.Rows.Count; i++)
                    {
                        if (dgvAssemblyParts.Rows[i].Cells[0].Value != null)
                        {
                            bool cellValue = (bool)dgvAssemblyParts.Rows[i].Cells[0].Value;
                            if (cellValue)
                            {
                                try
                                {
                                    int assemblyID = Convert.ToInt32(dgvAssemblyParts.Rows[i].Cells["ID"].Value.ToString());

                                    SQLiteCommand cmd = new SQLiteCommand(con)
                                    {
                                        //Continue with the child part deletion
                                        CommandText = @"DELETE FROM [2.assembly] where assembly_id=@ID"
                                    };
                                    cmd.Parameters.AddWithValue("@ID", assemblyID);
                                    cmd.ExecuteNonQuery();

                                }
                                catch (Exception ex)
                                {
                                    this.Cursor = Cursors.Default;

                                    con.Close();
                                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                    }
                    con.Close();

                    this.Cursor = Cursors.Default;
                    BtnViewAssemblyParts_Click(sender, e);
                    MessageBox.Show("Data Deleted Succcessfully", "Delete Report", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
            }
            else
            {
                MessageBox.Show("Select a checkbox to delete the data", "Error: None Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TbChildPart_TextChanged(object sender, EventArgs e)
        {
            tbChildPartDesc.Text = "";
            tbWeight.Text = "";
            tbTolerance.Text = "";
            tbNoOfQty.Text = "";
            tbSNo.Text = "";
            this.AcceptButton = btnSearch;
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;

                DataTable dtViewChildParts = new DataTable();
                string sql = @"SELECT childpart_id as ID, child_part_number as PartNumber, child_part_description as Description, part_weight as PartWeight,
                                tol_percent AS TolerancePercent FROM [1.childpart] where child_part_number='" + tbChildPart.Text + "' order by PartNumber";
                using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                {
                    dataAdapter.Fill(dtViewChildParts);
                }
                if (dtViewChildParts.Rows.Count == 1)
                {
                    ChildPartID = dtViewChildParts.Rows[0]["ID"].ToString();
                    tbChildPartDesc.Text = dtViewChildParts.Rows[0]["Description"].ToString();
                    tbWeight.Text = dtViewChildParts.Rows[0]["PartWeight"].ToString();
                    tbTolerance.Text = dtViewChildParts.Rows[0]["TolerancePercent"].ToString();

                    this.AcceptButton = null;

                    tbNoOfQty.Clear();
                    tbSNo.Clear();

                    tbNoOfQty.Focus();
                }
                else
                {
                    ChildPartID = null;
                    tbChildPartDesc.Text = "";
                    tbWeight.Text = "";
                    tbTolerance.Text = "";
                    tbNoOfQty.Text = "";
                    tbSNo.Text = "";
                    MessageBox.Show("This child part number is not found.", "Error: Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }


                this.Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                ChildPartID = null;
                tbChildPartDesc.Text = "";
                tbWeight.Text = "";
                tbTolerance.Text = "";
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddChildPartToAssembly_Click(object sender, EventArgs e)
        {
            tbChildPart.Text = tbChildPart.Text.Trim();
            if (tbChildPart.Text == "" || tbNoOfQty.Text == "" || tbSNo.Text == "")
            {
                MessageBox.Show("Input cannot be empty", "Empty error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (dgvAssemblyParts.DataSource != null && dgvAssemblyParts.SelectedRows.Count == 1)
            {
                bool isChildPartAlreadyAdded = false;
                bool isSNoAlreadyAdded = false;
                int rowIndex = -1;
                if (dgvChildPartsFromAssembly.DataSource == null || dgvChildPartsFromAssembly.Rows.Count == 0)
                {
                    isChildPartAlreadyAdded = false;
                    isSNoAlreadyAdded = false;
                }
                else
                {
                    //Check if child part  and S.NO is already present in dataGridView
                    for (int i = 0; i < dgvChildPartsFromAssembly.Rows.Count; i++)
                    {
                        string dgvChildPartNumber = dgvChildPartsFromAssembly.Rows[i].Cells["PartNumber"].Value.ToString();
                        if (dgvChildPartNumber == tbChildPart.Text)
                        {
                            isChildPartAlreadyAdded = true;
                            rowIndex = i;
                            break;
                        }


                        string dgvSerialNo = dgvChildPartsFromAssembly.Rows[i].Cells["S.No"].Value.ToString();
                        if (dgvSerialNo == tbSNo.Text)
                        {
                            isSNoAlreadyAdded = true;
                            rowIndex = i;
                            break;
                        }
                    }


                }

                if (isChildPartAlreadyAdded)
                {
                    dgvChildPartsFromAssembly.Rows[rowIndex].Selected = true;
                    MessageBox.Show("This child part number is already added to the selected Assembly Part Number", "Repeat Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (isSNoAlreadyAdded)
                {
                    dgvChildPartsFromAssembly.Rows[rowIndex].Selected = true;
                    MessageBox.Show("This Serial No is already added to the selected Assembly Part Number", "Repeat Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    //Add the childPart to the assembly part number in database

                    SQLiteConnection con = new SQLiteConnection(connectionString, true);

                    try
                    {
                        string assemblyID = dgvAssemblyParts.SelectedRows[0].Cells["ID"].Value.ToString();
                        string childpartID = ChildPartID;
                        int NoOfQty = Convert.ToInt32(tbNoOfQty.Text);
                        int serialNo = Convert.ToInt32(tbSNo.Text);

                        this.Cursor = Cursors.WaitCursor;

                        con.Open();
                        SQLiteCommand cmd = new SQLiteCommand(con)
                        {
                            CommandText = @"INSERT INTO [3.assembly_childpart]('assembly_id','childpart_id','no_of_qty','s_no') 
                                    VALUES (@ASSEMBLYID,@CHILDPARTID,@NOOFQTY,@SNO)"
                        };
                        cmd.Parameters.AddWithValue("@ASSEMBLYID", assemblyID);
                        cmd.Parameters.AddWithValue("@CHILDPARTID", childpartID);
                        cmd.Parameters.AddWithValue("@NOOFQTY", NoOfQty);
                        cmd.Parameters.AddWithValue("@SNO", serialNo);

                        cmd.ExecuteNonQuery();
                        con.Close();

                        DgvAssemblyParts_SelectionChanged(sender, e);
                        foreach (DataGridViewRow row in dgvChildPartsFromAssembly.Rows)
                        {
                            if (row.Cells["ChildPartID"].Value.ToString().Equals(childpartID))
                            {
                                dgvChildPartsFromAssembly.Rows[row.Index].Selected = true;
                                break;
                            }
                        }
                        MessageBox.Show("Data Added!");

                        //Reset the fields
                        tbChildPart.Text = "";
                        tbChildPart.Focus();


                        this.Cursor = Cursors.Default;

                    }
                    catch (Exception ex)
                    {
                        this.Cursor = Cursors.Default;

                        con.Close();
                        MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
            }
            else
            {
                MessageBox.Show("Select the Assembly Part Number.", "Error: None selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRemoveChildParts_Click(object sender, EventArgs e)
        {
            bool isCheckboxSelected = false;

            //Check if CheckBox are selected
            if (dgvChildPartsFromAssembly.DataSource != null && dgvChildPartsFromAssembly.Rows.Count >= 1)
            {
                for (int i = 0; i < dgvChildPartsFromAssembly.Rows.Count; i++)
                {
                    if (dgvChildPartsFromAssembly.Rows[i].Cells[0].Value != null)
                    {
                        bool cellValue = (bool)dgvChildPartsFromAssembly.Rows[i].Cells[0].Value;
                        if (cellValue)
                        {
                            isCheckboxSelected = true;
                            break;
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a checkbox to remove the data", "Error: None Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (isCheckboxSelected)
            {
                //Check if password is correct, else return
                EnterPasswordForm enterPasswordForm = new EnterPasswordForm();
                enterPasswordForm.ShowDialog();

                if (!enterPasswordForm.IsPasswordCorrect)
                {
                    return;
                }
            }

            if (isCheckboxSelected)
            {
                DialogResult dr = MessageBox.Show("Are you sure want to remove the selected child part numbers?", "Delete Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr == DialogResult.Yes)
                {
                    //delete row from database


                    this.Cursor = Cursors.WaitCursor;
                    SQLiteConnection con = new SQLiteConnection(connectionString, true);
                    con.Open();

                    for (int i = 0; i < dgvChildPartsFromAssembly.Rows.Count; i++)
                    {
                        if (dgvChildPartsFromAssembly.Rows[i].Cells[0].Value != null)
                        {
                            bool cellValue = (bool)dgvChildPartsFromAssembly.Rows[i].Cells[0].Value;
                            if (cellValue)
                            {
                                try
                                {
                                    int assemblyID = Convert.ToInt32(dgvAssemblyParts.SelectedRows[0].Cells["ID"].Value.ToString());
                                    int childpartID = Convert.ToInt32(dgvChildPartsFromAssembly.Rows[i].Cells["ChildPartID"].Value.ToString());
                                    SQLiteCommand cmd = new SQLiteCommand(con)
                                    {
                                        //Continue with the child part deletion
                                        CommandText = @"DELETE FROM [3.assembly_childpart] where assembly_id=@ASSEMBLYID and childpart_id=@CHILDPARTID"
                                    };
                                    cmd.Parameters.AddWithValue("@ASSEMBLYID", assemblyID);
                                    cmd.Parameters.AddWithValue("@CHILDPARTID", childpartID);
                                    cmd.ExecuteNonQuery();

                                }
                                catch (Exception ex)
                                {
                                    this.Cursor = Cursors.Default;

                                    con.Close();
                                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                    }
                    con.Close();

                    this.Cursor = Cursors.Default;
                    DgvAssemblyParts_SelectionChanged(sender, e);
                    MessageBox.Show("Data Deleted Succcessfully", "Delete Report", MessageBoxButtons.OK, MessageBoxIcon.None);
                }
            }
            else
            {
                MessageBox.Show("Select a checkbox to delete the data", "Error: None Selection", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void TbNoOfQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }


        }

        private void DgvViewChildParts_DataSourceChanged(object sender, EventArgs e)
        {
            if (dgvViewChildParts.DataSource == null)
            {
                lblChildPartRowCount.Text = "Row Count: 0";
            }
            else
            {
                lblChildPartRowCount.Text = "Row Count: " + dgvViewChildParts.Rows.Count.ToString();
            }
        }

        private void DgvAssemblyParts_DataSourceChanged(object sender, EventArgs e)
        {
            if (dgvAssemblyParts.DataSource == null)
            {
                lblAssemblyRowCount.Text = "Row Count: 0";
            }
            else
            {
                lblAssemblyRowCount.Text = "Row Count: " + dgvAssemblyParts.Rows.Count.ToString();
            }
        }

        private void DgvChildPartsFromAssembly_DataSourceChanged(object sender, EventArgs e)
        {
            if (dgvChildPartsFromAssembly.DataSource == null)
            {
                lblAssemChildRowCount.Text = "Row Count: 0";
            }
            else
            {
                lblAssemChildRowCount.Text = "Row Count: " + dgvChildPartsFromAssembly.Rows.Count.ToString();
            }
        }

        private void TbSNo_TextChanged(object sender, EventArgs e)
        {
            this.AcceptButton = btnAddChildPartToAssembly;
        }

        private void TbNoOfQty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter || e.KeyData == Keys.Return)
            {
                e.SuppressKeyPress = true;
                tbSNo.Focus();
            }
        }

        private void TbSNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void BtnChangePassword_Click(object sender, EventArgs e)
        {
            string oldPassword = tbOldPassword.Text;
            string newPassword = tbNewPassword.Text;

            if (oldPassword == string.Empty || newPassword == string.Empty)
            {
                MessageBox.Show("Values cannot be empty. Please check the password", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            else if (oldPassword == newPassword)
            {
                MessageBox.Show("New Password cannot be same as Old Password. Please change the password", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                tbNewPassword.Text = "";
                tbNewPassword.Focus();
                return;
            }
            else
            {
                try
                {
                    bool result = false;

                    DataTable dtUsers = new DataTable();
                    string sql = @"SELECT password as PASSWORD FROM [7.users] where USERNAME='ADMIN'";
                    using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                    using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                    {
                        dataAdapter.Fill(dtUsers);
                    }
                    if (dtUsers.Rows.Count == 1)
                    {
                        string dbPassword = dtUsers.Rows[0]["PASSWORD"].ToString();   //password in database
                        //comparing the passwords with case sensitive
                        result = dbPassword.Equals(oldPassword, System.StringComparison.Ordinal);

                        if (result)
                        {
                            //Update new password in DB
                            SQLiteConnection con = new SQLiteConnection(connectionString, true);

                            con.Open();
                            using (SQLiteCommand cmd = new SQLiteCommand(con)
                            {
                                CommandText = @"UPDATE [7.users] SET password = @PASSWORD
                                                     WHERE username = 'ADMIN'"
                            })
                            {
                                cmd.Parameters.AddWithValue("@PASSWORD", newPassword);
                                cmd.ExecuteNonQuery();
                            }
                            con.Close();

                            tbOldPassword.Text = tbNewPassword.Text = "";
                            MessageBox.Show("Password Updated Successfully.");
                        }
                        else
                        {
                            //Password error
                            MessageBox.Show("Old Password is wrong.", "Password Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            tbOldPassword.Text = "";

                            tbOldPassword.Focus();
                        }
                    }
                    else
                    {
                        MessageBox.Show("ADMIN User not found in database.", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("DB Error.\nPlease contact the software vendor.\n" + ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void TcMasterData_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tcMasterData.SelectedTab == tpChangePassword)
            {
                tbOldPassword.Text = "";
                tbNewPassword.Text = "";
                tbOldPassword.Focus();
            }
        }

        private void MasterDataForm_Load(object sender, EventArgs e)
        {
            bool enablePrintingHistory = appSettings.EnablePrintingHistory;
            if(!enablePrintingHistory)
            {
                tcMasterData.TabPages.Remove(tpViewPrintingHistory);
            }
        }

        private void BtnChildSubmit_Click(object sender, EventArgs e)
        {
            DateTime fromDate = dtpChildFromDate.Value.Date;
            DateTime toDate = dtpChildToDate.Value.Date;

            if (fromDate > toDate)
            {
                dgvChildPartsHistory.DataSource = null;
                MessageBox.Show("[FROM DATE] is greater than [TO DATE] which is invalid");
            }
            else
            {

                try
                {
                    this.Cursor = Cursors.WaitCursor;

                    dtViewChildPartsHistory = new DataTable();
                    string sql = @"SELECT childpart_history_id as ID, strftime('%d-%m-%Y %H:%M:%S',date_time) as DateAndTime, customer_name as CustomerName,
                                child_part_number as PartNumber, child_part_description as Description, 
                                quantity as Quantity,part_weight as PartWeight, net_weight as NetWeight,
                                pallet_weight as PalletWeight, gross_weight as GrossWeight, batch_no as BatchNo, other_info as OtherInfo,
                                no_of_packet as NoOfPacket, negative_tol_percent as NegativeTolerancePercent, negative_tol_part_weight as NegativeTolerancePartWeight,
                                db_part_weight as DBPartWeight, positive_tol_percent as PositiveTolerancePercent, positive_tol_part_weight as PositiveTolerancePartWeight
                                FROM [4.childpart_history] where date_time>= '" + fromDate.ToString("yyyy-MM-dd") + " 00:00:00' and date_time<='" + toDate.ToString("yyyy-MM-dd") + " 23:59:59' order by date_time";
                    using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                    using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                    {
                        dataAdapter.Fill(dtViewChildPartsHistory);
                    }

                    dgvChildPartsHistory.DataSource = dtViewChildPartsHistory;
                    dgvChildPartsHistory.Columns["ID"].Visible = false;

                    for (int i = 0; i < dgvChildPartsHistory.Columns.Count; i++)
                    {
                        dgvChildPartsHistory.Columns[i].ReadOnly = true;
                    }

                    dgvChildPartsHistory.ClearSelection();
                    this.Cursor = Cursors.Default;
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    dgvChildPartsHistory.DataSource = null;
                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DgvChildPartsHistory_DataSourceChanged(object sender, EventArgs e)
        {
            if(dgvChildPartsHistory.DataSource==null)
            {
                lblChildHistoryRowCount.Text = "Row Count: 0";
            }
            else
            {
                lblChildHistoryRowCount.Text = "Row Count: " + dgvChildPartsHistory.Rows.Count;
            }
        }

        [Obsolete]
        private void BtnChildExportToExcel_Click(object sender, EventArgs e)
        {
            if(dtViewChildPartsHistory == null || dtViewChildPartsHistory.Rows.Count <= 0)
            {
                MessageBox.Show("Report is empty. Export To Excel is invalid", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                Cursor = Cursors.WaitCursor;

                DataTable dt = dtViewChildPartsHistory;
                dt.Columns.Remove("ID");

                //Exporting to Excel
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string folderPath = desktop + "\\AutoWeighing ExcelReport\\ChildPartsHistory\\";

                DateTime from_date = dtpChildFromDate.Value;
                DateTime to_date = dtpChildToDate.Value;
                string fromdate = from_date.ToString("dd.MM.yyyy");
                string todate = to_date.ToString("dd.MM.yyyy");

                string filename = "Exported from " + fromdate + " to " + todate;

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                using (XLWorkbook wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("ChildParts Printed History");
                    ws.AutoFilter.Enabled = false;
                    ws.Cell(1, 1).InsertTable(dt);
                    ws.Worksheet.Table("Table1").ShowAutoFilter = false;
                    ws.Columns().AdjustToContents();

                    wb.SaveAs(folderPath + filename + ".xlsx");
                }

                Cursor = Cursors.Default;

                MessageBox.Show("Exported in Desktop/AutoWeighing ExcelReport folder");
            }
        }

        private void BtnAssemSubmit_Click(object sender, EventArgs e)
        {
            DateTime fromDate = dtpAssemFromDate.Value.Date;
            DateTime toDate = dtpAssemToDate.Value.Date;

            if (fromDate > toDate)
            {
                dgvAssemPartsHistory.DataSource = null;
                MessageBox.Show("[FROM DATE] is greater than [TO DATE] which is invalid");
            }
            else
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;

                    DataTable dtViewAssemPartsHistory = new DataTable();
                    string sql = @"SELECT assembly_history_id as ID, strftime('%d-%m-%Y %H:%M:%S',date_time) as DateAndTime, customer_name as CustomerName,
                                assembly_part_number as PartNumber, assembly_description as Description, pallet_weight as PalletWeight, gross_weight as GrossWeight
                                FROM [5.assembly_history] where date_time>= '" + fromDate.ToString("yyyy-MM-dd") + " 00:00:00' and date_time<='" + toDate.ToString("yyyy-MM-dd") + " 23:59:59' order by date_time";
                    using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                    using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                    {
                        dataAdapter.Fill(dtViewAssemPartsHistory);
                    }

                    dgvAssemPartsHistory.DataSource = dtViewAssemPartsHistory;
                    dgvAssemPartsHistory.Columns["ID"].Visible = false;

                    for (int i = 0; i < dgvAssemPartsHistory.Columns.Count; i++)
                    {
                        dgvAssemPartsHistory.Columns[i].ReadOnly = true;
                    }

                    dgvAssemPartsHistory.ClearSelection();
                    this.Cursor = Cursors.Default;
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    dgvAssemPartsHistory.DataSource = null;
                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void DgvAssemPartsHistory_DataSourceChanged(object sender, EventArgs e)
        {
            if (dgvAssemPartsHistory.DataSource == null)
            {
                lblAssemHistoryRowCount.Text = "Row Count: 0";
            }
            else
            {
                lblAssemHistoryRowCount.Text = "Row Count: " + dgvAssemPartsHistory.Rows.Count.ToString();
            }
        }

        private void DgvAssemPartsHistory_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAssemPartsHistory.DataSource != null && dgvAssemPartsHistory.SelectedRows.Count == 1)
            {
                try
                {
                    this.Cursor = Cursors.WaitCursor;

                    string assemblyHistoryID = dgvAssemPartsHistory.SelectedRows[0].Cells["ID"].Value.ToString();

                    DataTable dtViewChildPartsHistory = new DataTable();
                    string sql = @"Select
                                        s_no as [S.No],
                                        child_part_number as PartNumber,
                                        child_part_description as Description,
                                        quantity as Quantity,
                                        part_weight as PartWeight,
                                        net_weight as NetWeight
                                    From
                                        [6.assembly_childpart_history]
                                    Where
                                        assembly_history_id = " + assemblyHistoryID + @"
                                    Order By
                                        s_no";

                    using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                    {
                        using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                        {

                            dataAdapter.Fill(dtViewChildPartsHistory);
                            con.Close();
                        }
                    }

                    dgvAssemChildPartsHistory.DataSource = dtViewChildPartsHistory;

                    for (int i = 1; i < dgvAssemChildPartsHistory.Columns.Count; i++)
                    {
                        dgvAssemChildPartsHistory.Columns[i].ReadOnly = true;
                    }

                    dgvAssemChildPartsHistory.ClearSelection();
                    this.Cursor = Cursors.Default;
                }
                catch (Exception ex)
                {
                    this.Cursor = Cursors.Default;
                    dgvAssemChildPartsHistory.DataSource = null;
                    MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

            }
            else
            {
                dgvAssemChildPartsHistory.DataSource = null;
            }
        }

        [Obsolete]
        private void BtnAssemExportToExcel_Click(object sender, EventArgs e)
        {
            if (dgvAssemPartsHistory.DataSource == null || dgvAssemPartsHistory.Rows.Count <= 0)
            {
                MessageBox.Show("Report is empty. Export To Excel is invalid", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                Cursor = Cursors.WaitCursor;

                DataTable dtAssemblyPartsHistory = new DataTable();
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("DateAndTime", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("CustomerName", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("AssemblyPartNumber", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("AssemblyDescription", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("PalletWeight", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("GrossWeight", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("S.No", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("ChildPartNumber", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("ChildPartDescription", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("Quantity", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("PartWeight", typeof(string)));
                dtAssemblyPartsHistory.Columns.Add(new DataColumn("NetWeight", typeof(string)));

                for (int i = 0; i < dgvAssemPartsHistory.Rows.Count; i++)
                {
                    int assemblyHistoryId = Convert.ToInt32(dgvAssemPartsHistory.Rows[i].Cells["ID"].Value.ToString());
                    DataTable dtAssemChildPartHistory = new DataTable();
                    string sql = @"SELECT s_no as [S.No], child_part_number as ChildPartNumber, child_part_description as ChildPartDescription, quantity as Quantity, part_weight as PartWeight, net_weight as NetWeight
                                FROM [6.assembly_childpart_history] where assembly_history_id= '" + assemblyHistoryId + "' order by s_no";
                    using (SQLiteConnection con = new SQLiteConnection(connectionString, true))
                    using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(sql, con))
                    {
                        dataAdapter.Fill(dtAssemChildPartHistory);
                    }

                    for (int j = 0; j < dtAssemChildPartHistory.Rows.Count; j++)
                    {
                        DataRow dr = dtAssemblyPartsHistory.NewRow();
                        if(j==0)
                        {
                            dr["DateAndTime"] = dgvAssemPartsHistory.Rows[i].Cells["DateAndTime"].Value.ToString();
                            dr["CustomerName"] = dgvAssemPartsHistory.Rows[i].Cells["CustomerName"].Value.ToString();
                            dr["AssemblyPartNumber"] = dgvAssemPartsHistory.Rows[i].Cells["PartNumber"].Value.ToString();
                            dr["AssemblyDescription"] = dgvAssemPartsHistory.Rows[i].Cells["Description"].Value.ToString();
                            dr["PalletWeight"] = dgvAssemPartsHistory.Rows[i].Cells["PalletWeight"].Value.ToString();
                            dr["GrossWeight"] = dgvAssemPartsHistory.Rows[i].Cells["GrossWeight"].Value.ToString();
                        }
                        dr["S.No"] = dtAssemChildPartHistory.Rows[j]["S.No"].ToString();
                        dr["ChildPartNumber"] = dtAssemChildPartHistory.Rows[j]["ChildPartNumber"].ToString();
                        dr["ChildPartDescription"] = dtAssemChildPartHistory.Rows[j]["ChildPartDescription"].ToString();
                        dr["Quantity"] = dtAssemChildPartHistory.Rows[j]["Quantity"].ToString();
                        dr["PartWeight"] = dtAssemChildPartHistory.Rows[j]["PartWeight"].ToString();
                        dr["NetWeight"] = dtAssemChildPartHistory.Rows[j]["NetWeight"].ToString();

                        dtAssemblyPartsHistory.Rows.Add(dr);
                    }
                    DataRow emptyRow = dtAssemblyPartsHistory.NewRow();
                    dtAssemblyPartsHistory.Rows.Add(emptyRow);
                }


                //Exporting to Excel
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string folderPath = desktop + "\\AutoWeighing ExcelReport\\AssemblyPartsHistory\\";

                DateTime from_date = dtpAssemFromDate.Value;
                DateTime to_date = dtpAssemToDate.Value;
                string fromdate = from_date.ToString("dd.MM.yyyy");
                string todate = to_date.ToString("dd.MM.yyyy");

                string filename = "Exported from " + fromdate + " to " + todate;

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                using (XLWorkbook wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("Assembly Parts Printed History");
                    ws.AutoFilter.Enabled = false;
                    ws.Cell(1, 1).InsertTable(dtAssemblyPartsHistory);
                    ws.Worksheet.Table("Table1").ShowAutoFilter = false;
                    ws.Columns().AdjustToContents();

                    wb.SaveAs(folderPath + filename + ".xlsx");
                }

                Cursor = Cursors.Default;

                MessageBox.Show("Exported in [Desktop/AutoWeighing ExcelReport] folder");
                
            }
        }
    }
}
