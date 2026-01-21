using System;
using System.Data.SqlClient;
using System.Windows;
using GlobusTourAgency.Database;

namespace GlobusTourAgency
{
    public partial class AddEditBusWindow : Window
    {
        private readonly SqlDatabaseService _database;
        private readonly BusManagementWindow.BusType _bus;
        private readonly bool _isEditMode;

        public AddEditBusWindow(SqlDatabaseService database, BusManagementWindow.BusType bus)
        {
            InitializeComponent();
            _database = database;
            _bus = bus;
            _isEditMode = bus != null;

            InitializeWindow();
        }

        private void InitializeWindow()
        {
            if (_isEditMode)
            {
                TitleText.Text = "Редактирование автобуса";
                TypeNameTextBox.Text = _bus.TypeName;
                CapacityTextBox.Text = _bus.Capacity.ToString();
                DescriptionTextBox.Text = _bus.Description;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TypeNameTextBox.Text))
                {
                    MessageBox.Show("Введите тип автобуса", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TypeNameTextBox.Focus();
                    return;
                }

                if (!int.TryParse(CapacityTextBox.Text, out int capacity) || capacity <= 0)
                {
                    MessageBox.Show("Введите корректную вместимость", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    CapacityTextBox.Focus();
                    return;
                }

                bool success;
                if (_isEditMode)
                {
                    success = UpdateBusInDatabase();
                }
                else
                {
                    success = AddBusToDatabase();
                }

                if (success)
                {
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка сохранения автобуса", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AddBusToDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(_database.ConnectionString))
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO BusTypes (TypeName, Capacity, Description) 
                        VALUES (@typeName, @capacity, @description)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@typeName", TypeNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@capacity", int.Parse(CapacityTextBox.Text));
                        command.Parameters.AddWithValue("@description", DescriptionTextBox.Text.Trim());

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления в БД: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool UpdateBusInDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(_database.ConnectionString))
                {
                    connection.Open();

                    string query = @"
                        UPDATE BusTypes 
                        SET TypeName = @typeName, 
                            Capacity = @capacity, 
                            Description = @description 
                        WHERE BusTypeID = @busTypeId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@busTypeId", _bus.BusTypeID);
                        command.Parameters.AddWithValue("@typeName", TypeNameTextBox.Text.Trim());
                        command.Parameters.AddWithValue("@capacity", int.Parse(CapacityTextBox.Text));
                        command.Parameters.AddWithValue("@description", DescriptionTextBox.Text.Trim());

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления в БД: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}