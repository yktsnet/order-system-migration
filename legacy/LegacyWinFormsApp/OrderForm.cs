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
        // ❌ 接続文字列（IPアドレス・パスワード含む）をフィールドに直書き。
        //    ソースコード管理に含まれると認証情報が漏洩する。
        private string connStr = "Server=192.168.1.10;Database=HANBAI;User Id=sa;Password=p@ssw0rd;";

        // ❌ 編集モードの判定をフォームのフィールドで管理。
        //    状態がUIクラスに散在し、ロジックの追跡が困難になる。
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
                    // ❌ パラメータ化クエリを使っていないが、ここはマスタ固定なので実害は少ない。
                    //    ただし後続のメソッドと同じパターンで書かれているため、可読性上の一貫性がない。
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
                // ❌ 文字列結合によるSQL組み立て。
                //    txtOrderNo.Text に SQL メタ文字が含まれると任意のSQL文が実行される（SQLインジェクション）。
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

                // ❌ UIスレッド上で Thread.Sleep を呼び出している。
                //    この間、画面全体が応答不能になる（フリーズ）。
                //    Application.DoEvents() で一時的に操作を受け付けるが、再入可能性の問題を生む。
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

                // ❌ 税計算ロジックがUIクラスに直書き。
                //    税率変更や端数処理の変更がここにしか反映されず、単体テストも不可能。
                decimal tax = sub * _taxRate;
                decimal total = sub + tax;

                lblSubTotal.Text = sub.ToString("#,##0");
                lblTax.Text = tax.ToString("#,##0");
                lblTotal.Text = total.ToString("#,##0");
                lblTotal.ForeColor = total > 1000000? Color.Red : Color.Black;

                // ❌ 合計計算のたびに在庫をDBから取得している（後述 CheckStock）。
                CheckStock(txtItemName.Text);
            }
            catch { }  // ❌ 例外を握り潰している。計算失敗が無音で通過する。
        }

        // ❌ TextChanged イベント（キー入力ごと）から CalculateTotal → CheckStock の順でDB通信が発生する。
        //    商品名を1文字入力するたびにUIスレッドがブロックされる。
        private void txtPrice_TextChanged(object sender, EventArgs e) => CalculateTotal();
        private void txtQty_TextChanged(object sender, EventArgs e) => CalculateTotal();

        private void CheckStock(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return;

            // ❌ 文字列結合によるSQL組み立て（SQLインジェクションリスク）。
            string sql = "SELECT CurrentStock FROM M_Stock WHERE ItemName='" + itemName + "'";
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                // ❌ UIスレッドで同期的にスリープ＋DB通信。入力のたびに1.5秒フリーズする。
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
                        // ❌ 文字列結合によるSQL組み立て（SQLインジェクションリスク）。
                        //    isEditMode フラグで INSERT / UPDATE を分岐しており、
                        //    保存ボタン1つが「登録」と「更新」の両責務を持っている。
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

                    // ❌ 在庫更新SQLも同一メソッド内に混在。
                    //    受注登録・在庫更新・トランザクション制御がすべてUIイベントハンドラに集中している。
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
            // ❌ LPT1（パラレルポート）への直接印刷。
            //    このアプリが特定のWindows端末にしか存在できない根本的な原因。
            //    仮想化・コンテナ化・クラウド移行のいずれも不可能にする制約。
            MessageBox.Show("プリンタ（LPT1）に送信しました。");
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("消しますか？", "確認", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    // ❌ 文字列結合によるSQL（SQLインジェクションリスク）。
                    //    削除時に在庫を復元する処理がない。受注取消と在庫管理が分断されている。
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
