using System;
using System.Windows;
using GlobusTourAgency.Database;
using GlobusTourAgency.Models;

namespace GlobusTourAgency
{
    public partial class BookingWindow : Window
    {
        private readonly int _tourId;
        private readonly User _currentUser;
        private readonly SqlDatabaseService _database;

        public BookingWindow(int tourId, User currentUser, SqlDatabaseService database)
        {
            InitializeComponent();
            _tourId = tourId;
            _currentUser = currentUser;
            _database = database;

            if (_currentUser != null)
            {
                ClientNameTextBox.Text = _currentUser.FullName;
            }
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(ClientNameTextBox.Text))
                {
                    MessageBox.Show("Введите ФИО клиента", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var request = new Request
                {
                    ClientName = ClientNameTextBox.Text,
                    Phone = "", 
                    Email = "", 
                    TourId = _tourId,
                    RequestDate = DateTime.Now,
                    Status = "Новая"
                };

                bool success = _database.CreateRequest(request);

                if (success)
                {
                    MessageBox.Show("Заявка успешно создана!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при создании заявки", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}