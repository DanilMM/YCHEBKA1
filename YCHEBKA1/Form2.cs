using Npgsql; // Библиотека для работы с PostgreSQL
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace YCHEBKA1
{
    public partial class Form2 : Form
    {
        NpgsqlConnection conn; // Подключение к базе данных
        NpgsqlDataAdapter adapter; // Адаптер для связи с таблицами
        DataTable dt; // Таблица данных для отображения в DataGridView

        // Словарь для хранения данных подстановки (справочников)
        Dictionary<string, Dictionary<int, string>> lookupData = new Dictionary<string, Dictionary<int, string>>();

        public Form2()
        {
            InitializeComponent(); // Инициализация формы

            // Настройка подключения
            string connection = "Host=localhost;Username=postgres;Password=18273645;Database=master_pol";
            conn = new NpgsqlConnection(connection);

            // Добавление доступных таблиц в ComboBox
            comboBox1.Items.AddRange(new object[] { "продажи", "продукция", "партнеры" });

            // Назначение обработчика события при выборе элемента
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
        }

        // Обработка выбора таблицы из ComboBox
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string table = comboBox1.Text; // Получаем имя таблицы

            LoadLookupTables(); // Загрузка справочников

            string query = GetSelectQuery(table); // Формируем SQL-запрос
            adapter = new NpgsqlDataAdapter(query, conn);
            NpgsqlCommandBuilder builder = new NpgsqlCommandBuilder(adapter); // Автоматическая генерация команд INSERT/UPDATE/DELETE

            dt = new DataTable(); // Создаём новую таблицу
            adapter.Fill(dt); // Заполняем данными

            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = dt; // Привязываем таблицу к DataGridView

            HidePrimaryKey(); // Скрытие поля "id"
            ReplaceForeignKeyColumns(table); // Замена внешних ключей выпадающими списками

            // Обработка добавления новых строк
            dataGridView1.DefaultValuesNeeded -= dataGridView1_DefaultValuesNeeded;
            dataGridView1.DefaultValuesNeeded += dataGridView1_DefaultValuesNeeded;
        }

        // Обработка кнопки "Сохранить"
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Получение SQL-команд из CommandBuilder
                NpgsqlCommandBuilder builder = new NpgsqlCommandBuilder(adapter);
                adapter.UpdateCommand = builder.GetUpdateCommand();
                adapter.InsertCommand = builder.GetInsertCommand();
                adapter.DeleteCommand = builder.GetDeleteCommand();

                adapter.Update(dt); // Применение изменений к базе
                MessageBox.Show("Изменения сохранены.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }

        // Обработка кнопки "Удалить"
        private void button2_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow != null)
            {
                try
                {
                    // Удаление выбранной строки
                    dataGridView1.Rows.RemoveAt(dataGridView1.CurrentRow.Index);
                    NpgsqlCommandBuilder builder = new NpgsqlCommandBuilder(adapter);
                    adapter.Update(dt); // Синхронизация с БД
                    MessageBox.Show("Запись удалена.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Выберите строку для удаления.");
            }
        }

        // Возвращает SELECT-запрос для указанной таблицы
        private string GetSelectQuery(string table)
        {
            if (table == "продажи")
                return "SELECT * FROM продажи";
            if (table == "продукция")
                return "SELECT * FROM продукция";
            if (table == "партнеры")
                return "SELECT * FROM партнеры";

            return "SELECT * FROM " + table;
        }

        // Скрытие первичного ключа (id)
        private void HidePrimaryKey()
        {
            if (dataGridView1.Columns.Contains("id"))
            {
                dataGridView1.Columns["id"].Visible = false;
            }
        }

        // Загрузка всех справочников в память
        private void LoadLookupTables()
        {
            lookupData["партнеры"] = LoadLookup("партнеры", "id", "наименование");
            lookupData["продукция"] = LoadLookup("продукция", "id", "наименование");
            lookupData["типы_продукции"] = LoadLookup("типы_продукции", "id", "тип");
            lookupData["типы_партнеров"] = LoadLookup("типы_партнеров", "id", "тип");
        }

        // Загрузка одного справочника в словарь
        private Dictionary<int, string> LoadLookup(string table, string keyCol, string valCol)
        {
            Dictionary<int, string> dict = new Dictionary<int, string>();
            string query = $"SELECT {keyCol}, {valCol} FROM {table}";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dict.Add(reader.GetInt32(0), reader.GetString(1));
                    }
                }
                conn.Close();
            }

            return dict;
        }

        // Замена внешних ключей на выпадающие списки в DataGridView
        private void ReplaceForeignKeyColumns(string table)
        {
            Dictionary<string, string> fkMap = new Dictionary<string, string>();

            // Карта внешних ключей для каждой таблицы
            if (table == "продажи")
            {
                fkMap.Add("продукция_id", "продукция");
                fkMap.Add("партнер_id", "партнеры");
            }
            else if (table == "продукция")
            {
                fkMap.Add("тип_id", "типы_продукции");
            }
            else if (table == "партнеры")
            {
                fkMap.Add("тип_id", "типы_партнеров");
            }

            // Замена столбцов с внешними ключами на ComboBox'ы
            foreach (var kvp in fkMap)
            {
                string column = kvp.Key;
                string lookupTable = kvp.Value;

                if (!dt.Columns.Contains(column)) continue;

                DataGridViewComboBoxColumn combo = new DataGridViewComboBoxColumn
                {
                    Name = column,
                    DataPropertyName = column,
                    HeaderText = column.Replace("_id", ""),
                    DataSource = new BindingSource(lookupData[lookupTable], null),
                    DisplayMember = "Value",
                    ValueMember = "Key",
                    FlatStyle = FlatStyle.Flat
                };

                int index = dataGridView1.Columns[column].Index;
                dataGridView1.Columns.Remove(column);
                dataGridView1.Columns.Insert(index, combo);
            }
        }

        // Установка значений по умолчанию при добавлении новых строк
        private void dataGridView1_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            string table = comboBox1.Text;

            if (table == "продажи")
            {
                if (lookupData["продукция"].Count > 0)
                    e.Row.Cells["продукция_id"].Value = lookupData["продукция"].First().Key;
                if (lookupData["партнеры"].Count > 0)
                    e.Row.Cells["партнер_id"].Value = lookupData["партнеры"].First().Key;
            }
            else if (table == "продукция")
            {
                if (lookupData["типы_продукции"].Count > 0)
                    e.Row.Cells["тип_id"].Value = lookupData["типы_продукции"].First().Key;
            }
            else if (table == "партнеры")
            {
                if (lookupData["типы_партнеров"].Count > 0)
                    e.Row.Cells["тип_id"].Value = lookupData["типы_партнеров"].First().Key;
            }
        }
    }
}
