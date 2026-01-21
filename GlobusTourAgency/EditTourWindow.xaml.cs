using System;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using GlobusTourAgency.Database;
using GlobusTourAgency.Models;

namespace GlobusTourAgency
{
    public partial class EditTourWindow : Window
    {
        private readonly SqlDatabaseService _database;
        private readonly Tour _tour;
        private readonly Models.Tour _originalTour;

        public EditTourWindow(SqlDatabaseService database, Models.Tour tour)
        {
            InitializeComponent();
            _database = database;
            _originalTour = tour;
            LoadData();
            FillForm();
        }

        private void LoadData()
        {
            try
            {
                var countries = _database.GetCountries();
                var countryList = countries.Where(c => c != "Все").ToList();
                CountryComboBox.ItemsSource = countryList;

                string[] busTypes = { "Стандарт", "Комфорт", "Люкс", "Минивэн", "Двухэтажный" };
                BusTypeComboBox.ItemsSource = busTypes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillForm()
        {
            if (_originalTour != null)
            {
                TourCodeTextBox.Text = _originalTour.TourCode.ToString();
                TourNameTextBox.Text = _originalTour.Name;

                if (CountryComboBox.Items.Contains(_originalTour.Country))
                {
                    CountryComboBox.SelectedItem = _originalTour.Country;
                }
                else
                {
                    CountryComboBox.SelectedIndex = 0;
                }

                DurationTextBox.Text = _originalTour.DurationDays.ToString();
                StartDatePicker.SelectedDate = _originalTour.StartDate;
                PriceTextBox.Text = _originalTour.Price.ToString();
                DiscountTextBox.Text = _originalTour.Discount.ToString();

                if (BusTypeComboBox.Items.Contains(_originalTour.BusType))
                {
                    BusTypeComboBox.SelectedItem = _originalTour.BusType;
                }
                else
                {
                    BusTypeComboBox.SelectedIndex = 0;
                }

                CapacityTextBox.Text = _originalTour.Capacity.ToString();
                FreeSeatsTextBox.Text = _originalTour.FreeSeats.ToString();
                PhotoFileNameTextBox.Text = _originalTour.PhotoFileName;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
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

                bool success = UpdateTourInDatabase();

                if (success)
                {
                    MessageBox.Show("Тур успешно обновлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при обновлении тура", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления тура: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool UpdateTourInDatabase()
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
                        UPDATE Tours SET 
                            TourName = @tourName, 
                            CountryID = @countryId, 
                            DurationDays = @duration, 
                            StartDate = @startDate, 
                            Price = @price, 
                            BusTypeID = @busTypeId, 
                            Capacity = @capacity, 
                            FreeSeats = @freeSeats, 
                            PhotoFileName = @photoFileName, 
                            Discount = @discount
                        WHERE TourID = @tourId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@tourId", _originalTour.Id);
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