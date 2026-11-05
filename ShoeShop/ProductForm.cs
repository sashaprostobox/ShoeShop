using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ShoeShop
{
    public partial class ProductForm : Form
    {
        private readonly string connectionString = "Server=localhost;Database=SH_store;Uid=root;Pwd=;Charset=utf8;";
        private int _productId;
        private decimal _price;

        // Конструктор, который принимает ID товара
        public ProductForm(int productId)
        {
            InitializeComponent();
            _productId = productId;
        }

        // Загрузка данных при открытии формы
        private void ProductForm_Load(object sender, EventArgs e)
        {
            LoadProductData();
        }

        private void LoadProductData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT p.*, c.category, m.munufacture 
                        FROM Products p
                        JOIN Category c ON p.id_category = c.id
                        JOIN Munufactures m ON p.id_munufacture = m.id
                        WHERE p.id = @id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", _productId);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                this.Text = reader["product_name"].ToString();
                                lblName.Text = reader["product_name"].ToString();
                                lblCategory.Text = reader["category"].ToString();
                                lblManufacture.Text = reader["munufacture"].ToString();
                                lblComposition.Text = reader["sostav"].ToString();
                                lblDescription.Text = reader["description"].ToString();
                                _price = Convert.ToDecimal(reader["price"]);
                                lblPrice.Text = _price.ToString("C2");

                                // Загрузка картинки
                                string imagePath = reader["image"].ToString();
                                if (!string.IsNullOrEmpty(imagePath))
                                {
                                    string fullPath = System.IO.Path.Combine(Application.StartupPath, "Resources", imagePath);
                                    if (System.IO.File.Exists(fullPath))
                                        picProduct.Image = Image.FromFile(fullPath);
                                }
                            }
                        }
                    }

                    // Загрузка размеров
                    LoadSizes(conn);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки товара: " + ex.Message);
            }
        }

        private void LoadSizes(MySqlConnection conn)
        {
            string query = @"
                SELECT s.id, s.size, si.col 
                FROM Stock_Items si
                JOIN Sizes s ON si.id_size = s.id
                WHERE si.id_product = @id AND si.col > 0";

            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", _productId);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    cmbSize.Items.Clear();
                    while (reader.Read())
                    {
                        cmbSize.Items.Add(new SizeItem
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Size = Convert.ToDouble(reader["size"]),
                            Stock = Convert.ToInt32(reader["col"])
                        });
                    }
                }
            }
            if (cmbSize.Items.Count > 0)
                cmbSize.SelectedIndex = 0;
        }

        private void btnAddToCart_Click(object sender, EventArgs e)
        {
            if (cmbSize.SelectedItem == null)
            {
                MessageBox.Show("Выберите размер!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SizeItem selectedSize = (SizeItem)cmbSize.SelectedItem;
            int quantity = (int)numQuantity.Value;

            if (quantity > selectedSize.Stock)
            {
                MessageBox.Show($"На складе доступно только {selectedSize.Stock} пар.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Обновление остатков на складе
                    string updateStock = "UPDATE Stock_Items SET col = col - @qty WHERE id_product = @id AND id_size = @sizeId";
                    using (MySqlCommand updateCmd = new MySqlCommand(updateStock, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@qty", quantity);
                        updateCmd.Parameters.AddWithValue("@id", _productId);
                        updateCmd.Parameters.AddWithValue("@sizeId", selectedSize.Id);
                        updateCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Товар успешно добавлен в заказ!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close(); // Закрываем форму после заказа
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при оформлении: " + ex.Message);
            }
        }
    }

    // Вспомогательный класс для размеров (чтобы ComboBox понимал, что показывать)
    public class SizeItem
    {
        public int Id { get; set; }
        public double Size { get; set; }
        public int Stock { get; set; }
        public override string ToString() => $"{Size} (в наличии: {Stock})";
    }
}