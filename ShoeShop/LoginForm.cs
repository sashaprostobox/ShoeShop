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
using ShoeShop.Classes;

namespace ShoeShop
{
    public partial class LoginForm : Form
    {
        // Твоя строка подключения
        private readonly string connectionString = "Server=localhost;Database=SH_store;Uid=root;Pwd=;Charset=utf8;";

        public LoginForm()
        {
            InitializeComponent(); // Эта строчка нужна, чтобы загрузились кнопки с формы
        }

        // Обработчик нажатия на кнопку "Войти"
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string login = txtLogin.Text.Trim();
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин пользователя", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT id, fio, login FROM Users WHERE login = @login";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@login", login);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Сохраняем данные пользователя
                                Manager.IdUser = Convert.ToInt32(reader["id"]);
                                Manager.FullName = reader["fio"].ToString();
                                Manager.Login = reader["login"].ToString();

                                // Открываем главную форму
                                MainForm mainForm = new MainForm();
                                this.Hide();
                                mainForm.ShowDialog();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Пользователь не найден!\nПроверьте правильность ввода логина.", "Ошибка авторизации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к БД:\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Обработчик кнопки "Войти как гость"
        private void btnGuest_Click(object sender, EventArgs e)
        {
            Manager.Login = null;
            Manager.FullName = "Гость";

            MainForm mainForm = new MainForm();
            this.Hide();
            mainForm.ShowDialog();
            this.Close();
        }

        // Пустой обработчик (чтобы не было ошибки, если он привязан)
        private void LoginForm_Load(object sender, EventArgs e)
        {
        }

        private void LoginForm_Click(object sender, EventArgs e)
        {

        }
    }
}