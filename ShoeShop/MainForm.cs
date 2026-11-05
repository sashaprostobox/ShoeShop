using MySql.Data.MySqlClient;
using ShoeShop.Classes;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System;

namespace ShoeShop
{
    public partial class MainForm : Form
    {
        // Строка подключения (проверь пароль от root)
        private readonly string connectionString = "Server=localhost;Database=SH_store;Uid=root;Pwd=;Charset=utf8;";

        public MainForm()
        {
            InitializeComponent();
            LoadData();       // Загружаем товары при старте
            LoadCategories(); // Загружаем категории в комбобокс
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Вывод ФИО в правый верхний угол (в зеленую шапку)
            if (Manager.IsAuthorized)
                lblUser.Text = Manager.FullName;
            else
                lblUser.Text = "Гость";
        }

        // Загрузка категорий в ComboBox
        private void LoadCategories()
        {
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("Все категории");

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT category FROM Category ORDER BY category";
                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cmbCategory.Items.Add(reader["category"].ToString());
                    }
                }
            }
            cmbCategory.SelectedIndex = 0; // Ставим "Все категории"
        }

        // Главный метод: загрузка данных с учетом поиска, фильтра и сортировки
        private void LoadData()
        {
            string searchText = txtSearch.Text.Trim(); // Твой TextBox поиска
            string category = cmbCategory.SelectedIndex > 0 ? cmbCategory.SelectedItem.ToString() : "";
            string sort = cmbSort.SelectedItem?.ToString() ?? "По умолчанию";

            // SQL запрос: достаем товары, склеиваем с категориями и производителями, считаем остаток
            string query = @"
                SELECT p.id, p.product_name, c.category, m.munufacture, p.price, 
                       COALESCE(SUM(si.col), 0) AS total_stock,
                       p.image
                FROM Products p
                JOIN Category c ON p.id_category = c.id
                JOIN Munufactures m ON p.id_munufacture = m.id
                LEFT JOIN Stock_Items si ON p.id = si.id_product
                WHERE 1=1 ";

            // Поиск по названию или описанию
            if (!string.IsNullOrEmpty(searchText))
                query += " AND (p.product_name LIKE @search OR p.description LIKE @search) ";

            // Фильтр по категории
            if (!string.IsNullOrEmpty(category))
                query += " AND c.category = @category ";

            // Группировка обязательна из-за SUM()
            query += " GROUP BY p.id, p.product_name, c.category, m.munufacture, p.price, p.image ";

            // Сортировка
            if (sort == "Цена (по возрастанию)")
                query += " ORDER BY p.price ASC ";
            else if (sort == "Цена (по убыванию)")
                query += " ORDER BY p.price DESC ";
            else
                query += " ORDER BY p.product_name ASC ";

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        // Подставляем параметры в запрос, чтобы защититься от SQL-инъекций
                        if (!string.IsNullOrEmpty(searchText))
                            cmd.Parameters.AddWithValue("@search", "%" + searchText + "%");
                        if (!string.IsNullOrEmpty(category))
                            cmd.Parameters.AddWithValue("@category", category);

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            dgvProducts.Rows.Clear(); // Очищаем таблицу перед заполнением

                            foreach (DataRow row in dt.Rows)
                            {
                                int totalStock = Convert.ToInt32(row["total_stock"]);
                                string qtyText = totalStock > 5 ? "много" : "мало";

                                // Загрузка картинки (если нет, ставим заглушку)
                                string imagePath = row["image"].ToString();
                                Image img = Properties.Resources.picture; // Заглушка

                                if (!string.IsNullOrEmpty(imagePath))
                                {
                                    string fullPath = System.IO.Path.Combine(Application.StartupPath, "Resources", imagePath);
                                    if (System.IO.File.Exists(fullPath))
                                        img = Image.FromFile(fullPath);
                                }

                                // Добавляем строку в DataGridView
                                int rowIndex = dgvProducts.Rows.Add(
                                    img,
                                    row["product_name"].ToString(),
                                    row["category"].ToString(),
                                    row["munufacture"].ToString(),
                                    Convert.ToDecimal(row["price"]).ToString("C2"), // Валюта
                                    qtyText
                                );

                                // Подсветка красным, если остаток <= 3 (как в задании)
                                if (totalStock <= 3)
                                {
                                    dgvProducts.Rows[rowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 128, 128);
                                }

                                // Сохраняем ID товара в скрытое поле строки (для перехода)
                                dgvProducts.Rows[rowIndex].Tag = row["id"];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // === ОБРАБОТЧИКИ СОБЫТИЙ (чтобы поиск и фильтр работали сразу) ===

        private void txtSearch_TextChanged(object sender, EventArgs e) { LoadData(); }
        private void cmbCategory_SelectedIndexChanged(object sender, EventArgs e) { LoadData(); }
        private void cmbSort_SelectedIndexChanged(object sender, EventArgs e) { LoadData(); }

        // Переход на карточку товара при двойном клике
        private void dgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                // Проверка на гостя
                if (!Manager.IsAuthorized)
                {
                    MessageBox.Show("Для оформления заказа необходимо авторизоваться!", "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int productId = Convert.ToInt32(dgvProducts.Rows[e.RowIndex].Tag);
                ProductForm productForm = new ProductForm(productId);
                productForm.ShowDialog();
                LoadData(); // Обновляем остатки после заказа
            }
        }
    }
}