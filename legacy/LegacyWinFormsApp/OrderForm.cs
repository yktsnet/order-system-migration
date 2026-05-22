using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace LegacyWinFormsApp
{
    public partial class OrderForm : Form
    {
        private string connStr = "Server=192.168.1.10;Database=HANBAI;User Id=sa;Password=p@ssw0rd;";
        private bool isEditMode = false;
        private decimal _taxRate = 0.1m;

        public OrderForm()
        {
            InitializeComponent();
        }

        private void OrderForm_Load(object sender, EventArgs e)
        {
            LoadCategory();
        }

        private void LoadCategory()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    string sql = "SELECT CategoryId, CategoryName FROM M_Category WHERE DeleteFlg=0";
                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    cmbCategory.DisplayMember = "CategoryName";
                    cmbCategory.ValueMember = "CategoryId";
                    cmbCategory.DataSource = dt;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("マスタ読込失敗：" + ex.Message);
                }
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                string sql = "SELECT * FROM Orders WHERE OrderNo='" + txtOrderNo.Text + "'";
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        txtCustomer.Text = dt.Rows[0]["CustomerName"].ToString();
                        txtItemName.Text = dt.Rows[0]["ItemName"].ToString();
                        txtPrice.Text = dt.Rows[0]["Price"].ToString();
                        txtQty.Text = dt.Rows[0]["Qty"].ToString();
                        cmbCategory.SelectedValue = dt.Rows[0]["CategoryId"];
                        CalculateTotal();
                        isEditMode = true;
                    }
                }
                Thread.Sleep(2000);
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラー：" + ex.Message);
            }
        }

        private void CalculateTotal()
        {
            try
            {
                if (string.IsNullOrEmpty(txtPrice.Text) || string.IsNullOrEmpty(txtQty.Text)) return;
                decimal price = decimal.Parse(txtPrice.Text);
                int qty = int.Parse(txtQty.Text);
                decimal sub = price * qty;
                decimal tax = sub * _taxRate;
                decimal total = sub + tax;

                lblSubTotal.Text = sub.ToString("#,##0");
                lblTax.Text = tax.ToString("#,##0");
                lblTotal.Text = total.ToString("#,##0");
                lblTotal.ForeColor = total > 1000000? Color.Red : Color.Black;

                CheckStock(txtItemName.Text);
            }
            catch { }
        }

        private void txtPrice_TextChanged(object sender, EventArgs e) => CalculateTotal();
        private void txtQty_TextChanged(object sender, EventArgs e) => CalculateTotal();

        private void CheckStock(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return;
            string sql = "SELECT CurrentStock FROM M_Stock WHERE ItemName='" + itemName + "'";
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                Thread.Sleep(1500);
                object r = new SqlCommand(sql, conn).ExecuteScalar();
                lblStock.Text = "在庫：" + (r?? "0").ToString();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (cmbCategory.SelectedIndex == -1) { MessageBox.Show("カテゴリを選択してください。"); return; }
            if (string.IsNullOrEmpty(txtItemName.Text)) { MessageBox.Show("商品名を入力してください。"); return; }
            if (MessageBox.Show("登録しますか？", "確認", MessageBoxButtons.YesNo) == DialogResult.No) return;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlTransaction tran = conn.BeginTransaction();
                try
                {
                    string sql;
                    if (isEditMode)
                    {
                        sql = "UPDATE Orders SET CustomerName='" + txtCustomer.Text + "', CategoryId=" + cmbCategory.SelectedValue +
                              ", ItemName='" + txtItemName.Text + "', Price=" + txtPrice.Text + ", Qty=" + txtQty.Text +
                              ", TotalAmount=" + lblTotal.Text.Replace(",", "") + " WHERE OrderNo='" + txtOrderNo.Text + "'";
                    }
                    else
                    {
                        sql = "INSERT INTO Orders (OrderDate, OrderNo, CategoryId, ItemName, Price, Qty, TotalAmount) VALUES ('" +
                              DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + txtOrderNo.Text + "'," +
                              cmbCategory.SelectedValue + ",'" + txtItemName.Text + "'," + txtPrice.Text + "," + txtQty.Text + "," +
                              lblTotal.Text.Replace(",", "") + ")";
                    }
                    new SqlCommand(sql, conn, tran).ExecuteNonQuery();

                    string sqlStock = "UPDATE M_Stock SET CurrentStock = CurrentStock - " + txtQty.Text +
                                      " WHERE ItemName='" + txtItemName.Text + "'";
                    int updated = new SqlCommand(sqlStock, conn, tran).ExecuteNonQuery();
                    if (updated == 0) throw new Exception("在庫が見つかりません。");

                    tran.Commit();
                    MessageBox.Show("正常に登録されました。");
                    isEditMode = false;
                    ClearForm();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    MessageBox.Show("登録エラー：\n" + ex.Message);
                }
            }
        }

        private void ClearForm()
        {
            txtOrderNo.Text = ""; txtCustomer.Text = ""; txtItemName.Text = "";
            txtPrice.Text = "0"; txtQty.Text = "0";
            lblSubTotal.Text = "0"; lblTax.Text = "0"; lblTotal.Text = "0";
            lblStock.Text = "在庫：-"; cmbCategory.SelectedIndex = -1;
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            MessageBox.Show("プリンタ（LPT1）に送信しました。");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("消しますか？", "確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    string sql = "DELETE FROM Orders WHERE OrderNo='" + txtOrderNo.Text + "'";
                    using (SqlConnection conn = new SqlConnection(connStr))
                    {
                        conn.Open();
                        new SqlCommand(sql, conn).ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("削除失敗：" + ex.Message);
                }
            }
        }
    }
}
