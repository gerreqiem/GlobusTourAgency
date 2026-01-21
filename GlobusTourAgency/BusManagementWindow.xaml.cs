using System;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GlobusTourAgency.Database;
using GlobusTourAgency.Models;

namespace GlobusTourAgency
{
    public partial class BusManagementWindow : Window
    {
        private readonly SqlDatabaseService _database;
        private ObservableCollection<BusType> _buses;

        public class BusType
        {
            public int BusTypeID { get; set; }
            public string TypeName { get; set; }
            public int Capacity { get; set; }
            public string Description { get; set; }
        }

        public BusManagementWindow(SqlDatabaseService database)
        {
            InitializeComponent();
            _database = database;
            _buses = new ObservableCollection<BusType>();
            LoadBuses();
        }

        private void LoadBuses()
        {
            try
            {
                _buses.Clear();

                using (var connection = new SqlConnection(_database.ConnectionString))
                {
                    connection.Open();

                    string query = "SELECT BusTypeID, TypeName, Capacity, Description FROM BusTypes ORDER BY TypeName";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _buses.Add(new BusType
                            {
                                BusTypeID = reader.GetInt32(0),
                                TypeName = reader.GetString(1),
                                Capacity = reader.GetInt32(2),
                                Description = reader.IsDBNull(3) ? "" : reader.GetString(3)
                            });
                        }
                    }
                }

                BusesDataGrid.ItemsSource = _buses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки автобусов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddBusButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addBusWindow = new AddEditBusWindow(_database, null);
                addBusWindow.Owner = this;
                var result = addBusWindow.ShowDialog();

                if (result == true)
                {
                    LoadBuses();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления автобуса: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditBusButton_Click(object sender, RoutedEventArgs e)
        {
            if (BusesDataGrid.SelectedItem is BusType selectedBus)
            {
                try
                {
                    var editBusWindow = new AddEditBusWindow(_database, selectedBus);
                    editBusWindow.Owner = this;
                    var result = editBusWindow.ShowDialog();

                    if (result == true)
                    {
                        LoadBuses();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка редактирования автобуса: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите автобус для редактирования",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteBusButton_Click(object sender, RoutedEventArgs e)
        {
            if (BusesDataGrid.SelectedItem is BusType selectedBus)
            {
                try
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите удалить автобус '{selectedBus.TypeName}'?",
                        "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        bool success = DeleteBusFromDatabase(selectedBus.BusTypeID);

                        if (success)
                        {
                            MessageBox.Show($"Автобус '{selectedBus.TypeName}' успешно удален",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadBuses();
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при удалении автобуса",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления автобуса: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Выберите автобус для удаления",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool DeleteBusFromDatabase(int busTypeId)
        {
            try
            {
                using (var connection = new SqlConnection(_database.ConnectionString))
                {
                    connection.Open();

                    string checkQuery = "SELECT COUNT(*) FROM Tours WHERE BusTypeID = @busTypeId";
                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@busTypeId", busTypeId);
                        int tourCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (tourCount > 0)
                        {
                            MessageBox.Show("Нельзя удалить автобус, который используется в турах",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }

                    string deleteQuery = "DELETE FROM BusTypes WHERE BusTypeID = @busTypeId";
                    using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@busTypeId", busTypeId);
                        int rowsAffected = deleteCommand.ExecuteNonQuery();

                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления из БД: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}