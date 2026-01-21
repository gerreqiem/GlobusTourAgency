using System;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using GlobusTourAgency.Database;

namespace GlobusTourAgency
{
    public partial class CreateTourWindow : Window
    {
        private readonly SqlDatabaseService _database;

        public CreateTourWindow(SqlDatabaseService database)
        {
            InitializeComponent();
            _database = database;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var countries = _database.GetCountries();
                var countryList = countries.Where(c => c != "Все").ToList();
                CountryComboBox.ItemsSource = countryList;
                CountryComboBox.SelectedIndex = 0;

                StartDatePicker.SelectedDate = DateTime.Now.AddDays(30);

                LoadBusTypes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadBusTypes()
        {
            try
            {
                string[] busTypes = { "Стандарт", "Комфорт", "Люкс", "Минивэн", "Двухэтажный" };
                BusTypeComboBox.ItemsSource = busTypes;
                BusTypeComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки типов автобусов: {ex.Message}");
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TourNameTextBox.Text))
                {
                    MessageBox.Show("Введите название тура", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TourNameTextBox.Focus();
                    return;
                }

                if (!int.TryParse(TourCodeTextBox.Text, out int tourCode))
                {
                    MessageBox.Show("Введите корректный код тура", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TourCodeTextBox.Focus();
                    return;
                }

                if (CountryComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите страну", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(DurationTextBox.Text, out int duration) || duration <= 0)
                {
                    MessageBox.Show("Введите корректную длительность (больше 0 дней)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    DurationTextBox.Focus();
                    return;
                }

                if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
                {
                    MessageBox.Show("Введите корректную цену", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PriceTextBox.Focus();
                    return;
                }

                if (!decimal.TryParse(DiscountTextBox.Text, out decimal discount) || discount < 0 || discount > 100)
                {
                    MessageBox.Show("Введите корректную скидку (от 0 до 100%)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    DiscountTextBox.Focus();
                    return;
                }

                if (!int.TryParse(CapacityTextBox.Text, out int capacity) || capacity <= 0)
                {
                    MessageBox.Show("Введите корректную вместимость", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    CapacityTextBox.Focus();
                    return;
                }

                if (!int.TryParse(FreeSeatsTextBox.Text, out int freeSeats) || freeSeats < 0)
                {
                    MessageBox.Show("Введите корректное количество свободных мест", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    FreeSeatsTextBox.Focus();
                    return;
                }

                if (freeSeats > capacity)
                {
                    MessageBox.Show("Свободных мест не может быть больше общей вместимости", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    FreeSeatsTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhotoFileNameTextBox.Text))
                {
                    var result = MessageBox.Show("Не указано имя файла фото. Продолжить?", "Предупреждение",
                        MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;
                }

                bool success = CreateTourInDatabase();

                if (success)
                {
                    MessageBox.Show("Тур успешно создан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при создании тура", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания тура: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CreateTourInDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(_database.ConnectionString))
                {
                    connection.Open();

                    string countryName = CountryComboBox.SelectedItem.ToString();
                    int countryId = GetCountryId(connection, countryName);

                    string busTypeName = BusTypeComboBox.SelectedItem.ToString();
                    int busTypeId = GetBusTypeId(connection, busTypeName);

                    string query = @"
                        INSERT INTO Tours (
                            TourCode, 
                            TourName, 
                            CountryID, 
                            DurationDays, 
                            StartDate, 
                            Price, 
                            BusTypeID, 
                            Capacity, 
                            FreeSeats, 
                            PhotoFileName, 
                            Discount
                        ) VALUES (
                            @tourCode, 
                            @tourName, 
                            @countryId, 
                            @duration, 
                            @startDate, 
                            @price, 
                            @busTypeId, 
                            @capacity, 
                            @freeSeats, 
                            @photoFileName, 
                            @discount
                        )";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@tourCode", int.Parse(TourCodeTextBox.Text));
                        command.Parameters.AddWithValue("@tourName", TourNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@countryId", countryId);
                        command.Parameters.AddWithValue("@duration", int.Parse(DurationTextBox.Text));
                        command.Parameters.AddWithValue("@startDate", StartDatePicker.SelectedDate.Value);
                        command.Parameters.AddWithValue("@price", decimal.Parse(PriceTextBox.Text));
                        command.Parameters.AddWithValue("@busTypeId", busTypeId);
                        command.Parameters.AddWithValue("@capacity", int.Parse(CapacityTextBox.Text));
                        command.Parameters.AddWithValue("@freeSeats", int.Parse(FreeSeatsTextBox.Text));
                        command.Parameters.AddWithValue("@photoFileName", PhotoFileNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@discount", decimal.Parse(DiscountTextBox.Text));

                        int rowsAffected = command.ExecuteNonQuery();

                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка записи в базу данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private int GetCountryId(SqlConnection connection, string countryName)
        {
            string query = "SELECT CountryID FROM Countries WHERE CountryName = @countryName";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@countryName", countryName);
                var result = command.ExecuteScalar();

                return result != null ? Convert.ToInt32(result) : 1;
            }
        }

        private int GetBusTypeId(SqlConnection connection, string busTypeName)
        {
            string query = "SELECT BusTypeID FROM BusTypes WHERE TypeName = @busTypeName";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@busTypeName", busTypeName);
                var result = command.ExecuteScalar();

                return result != null ? Convert.ToInt32(result) : 1; 
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}