using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Npgsql; // Подключение библиотеки для работы с PostgreSQL

namespace YCHEBKA1
{
    public partial class Form1 : Form
    {
        // Объявление объекта подключения к базе данных
        private NpgsqlConnection connection;

        // Строка подключения к базе данных PostgreSQL
        private string connectionString = "Host=localhost;Username=postgres;Password=18273645;Database=master_pol";

        public Form1()
        {
            InitializeComponent(); // Инициализация компонентов формы

            // Создание нового подключения (локальная переменная, не используется позже — можно исправить)
            NpgsqlConnection connection = new NpgsqlConnection(connectionString);
            connection.Open(); // Открытие соединения с базой данных
            // Здесь нужно закрыть соединение или использовать его как глобальное, иначе может возникнуть утечка ресурсов
        }

        // Метод для аутентификации пользователя
        private bool AuthenticateUser(string username, string password)
        {
            // Повторное объявление строки подключения (можно использовать поле класса вместо этого)
            string connectionString = "Host=localhost;Username=postgres;Password=18273645;Database=master_pol";

            // Создание подключения к базе данных в блоке using (автоматическое освобождение ресурсов)
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open(); // Открытие соединения

                // SQL-запрос для проверки наличия пользователя с заданным логином и паролем
                string query = "SELECT COUNT(*) FROM db_users WHERE user_login = @Username AND user_password = @Password";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    // Параметры SQL-запроса, защищают от SQL-инъекций
                    command.Parameters.AddWithValue("@Username", username);
                    command.Parameters.AddWithValue("@Password", password);

                    // Получение количества найденных записей
                    int count = Convert.ToInt32(command.ExecuteScalar());

                    // Возвращаем true, если найден хотя бы один пользователь
                    return count > 0;
                }
            }
        }

        // Обработчик нажатия кнопки входа
        private void button1_Click(object sender, EventArgs e)
        {
            // Получение логина и пароля из текстовых полей формы
            string username = textBox1.Text;
            string password = textBox2.Text;

            // Вызов метода аутентификации
            if (AuthenticateUser(username, password))
            {
                // Успешная аутентификация
                MessageBox.Show("Успешная аутентификация! Добро пожаловать, " + username + "!");
                Form f2 = new Form2(); // Создание второй формы
                this.Hide(); // Скрытие текущей формы
                f2.Show(); // Отображение второй формы
            }
            else
            {
                // Ошибка аутентификации
                MessageBox.Show("Ошибка аутентификации. Пользователь с такими данными не найден.");
            }
        }
    }
}
