using System;
using System.Linq;
using System.Windows;
using GlobusTourAgency.Database;
using GlobusTourAgency.Models;

namespace GlobusTourAgency
{
    public partial class CreateRequestWindow : Window
    {
        private readonly SqlDatabaseService _database;
        private Models.Tour _selectedTour;

        public CreateRequestWindow(SqlDatabaseService database)
        {
            InitializeComponent();
            _database = database;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var clients = _database.GetClients();
                ClientComboBox.ItemsSource = clients;

                var tours = _database.GetAllTours();
                TourComboBox.ItemsSource = tours.Where(t => t.FreeSeats > 0).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TourComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TourComboBox.SelectedItem is Models.Tour selectedTour)
            {
                _selectedTour = selectedTour;

                string tourInfo = $"Информация о туре:\n" +
                                 $"Название: {selectedTour.Name}\n" +
                                 $"Страна: {selectedTour.Country}\n" +
                                 $"Дата: {selectedTour.FormattedStartDate}\n" +
                                 $"Продолжительность: {selectedTour.DurationDays} дней\n" +
                                 $"Цена: {selectedTour.FormattedPrice}\n" +
                                 $"Свободных мест: {selectedTour.FreeSeats}/{selectedTour.Capacity}";

                TourInfoText.Text = tourInfo;
            }
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ClientComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите клиента",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (TourComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тур",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var client = (User)ClientComboBox.SelectedItem;
                var tour = (Models.Tour)TourComboBox.SelectedItem;

                if (tour.FreeSeats <= 0)
                {
                    MessageBox.Show("Нет свободных мест для выбранного тура",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var request = new Request
                {
                    ClientName = client.FullName,
                    Phone = "", 
                    Email = "", 
                    TourId = tour.Id,
                    TourName = tour.Name,
                    RequestDate = DateTime.Now,
                    Status = "Новая"
                };

                bool success = _database.CreateRequest(request);

                if (success)
                {
                    MessageBox.Show("Заявка успешно создана!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при создании заявки",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания заявки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}