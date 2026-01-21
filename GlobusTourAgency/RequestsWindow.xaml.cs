using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GlobusTourAgency.Database;
using GlobusTourAgency.Models;

namespace GlobusTourAgency
{
    public partial class RequestsWindow : Window
    {
        private readonly SqlDatabaseService _database;
        private ObservableCollection<Request> _allRequests;
        private ObservableCollection<Request> _filteredRequests;
        private bool _sortAscending = true;
        private User _currentUser;

        public RequestsWindow(User currentUser = null)
        {
            InitializeComponent();
            _database = new SqlDatabaseService();
            _currentUser = currentUser;

            if (_currentUser != null)
            {
                CreateRequestButton.Visibility = _currentUser.IsAdmin ? Visibility.Visible : Visibility.Collapsed;

                bool canManageRequests = _currentUser.IsManager || _currentUser.IsAdmin;
                ConfirmButton.Visibility = canManageRequests ? Visibility.Visible : Visibility.Collapsed;
                RejectButton.Visibility = canManageRequests ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                CreateRequestButton.Visibility = Visibility.Collapsed;
                ConfirmButton.Visibility = Visibility.Collapsed;
                RejectButton.Visibility = Visibility.Collapsed;
            }

            LoadRequests();
        }

        private void LoadRequests()
        {
            try
            {
                Console.WriteLine("=== ЗАГРУЗКА ЗАЯВОК ===");

                var requests = _database.GetAllRequests();
                Console.WriteLine($"Получено заявок из БД: {requests?.Count ?? 0}");

                _allRequests = new ObservableCollection<Request>(requests);
                _filteredRequests = new ObservableCollection<Request>(_allRequests);

                RequestsDataGrid.ItemsSource = _filteredRequests;
                UpdateButtonsState();

                if (_allRequests.Count == 0)
                {
                    Console.WriteLine("ВНИМАНИЕ: Список заявок пуст!");
                }
                else
                {
                    foreach (var request in _allRequests.Take(5))
                    {
                        Console.WriteLine($"Заявка #{request.Id}: {request.ClientName}, Тур: {request.TourName}, Статус: {request.Status}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ОШИБКА загрузки заявок: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateButtonsState()
        {
            bool hasSelection = RequestsDataGrid.SelectedItem != null;
            ViewDetailsButton.IsEnabled = hasSelection;

            if (hasSelection && RequestsDataGrid.SelectedItem is Request selectedRequest)
            {
                ConfirmButton.IsEnabled = selectedRequest.Status == "Новая";
                RejectButton.IsEnabled = selectedRequest.Status == "Новая";
            }
            else
            {
                ConfirmButton.IsEnabled = false;
                RejectButton.IsEnabled = false;
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _allRequests.AsEnumerable();

                if (!string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    string searchText = SearchTextBox.Text.ToLower();
                    filtered = filtered.Where(r =>
                        r.ClientName.ToLower().Contains(searchText) ||
                        r.Id.ToString().Contains(searchText) ||
                        r.TourName.ToLower().Contains(searchText));
                }

                if (StatusFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string status = selectedItem.Content.ToString();
                    if (status != "Все")
                    {
                        filtered = filtered.Where(r => r.Status == status);
                    }
                }

                _filteredRequests.Clear();
                foreach (var request in filtered)
                {
                    _filteredRequests.Add(request);
                }

                Console.WriteLine($"Отфильтровано заявок: {_filteredRequests.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SortByDateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _sortAscending = !_sortAscending;

                var sorted = _sortAscending
                    ? _filteredRequests.OrderBy(r => r.RequestDate).ToList()
                    : _filteredRequests.OrderByDescending(r => r.RequestDate).ToList();

                _filteredRequests.Clear();
                foreach (var request in sorted)
                {
                    _filteredRequests.Add(request);
                }

                SortByDateButton.Content = _sortAscending
                    ? "Сортировать по дате ↑"
                    : "Сортировать по дате ↓";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сортировки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RequestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsState();
        }

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequestsDataGrid.SelectedItem is Request selectedRequest)
            {
                try
                {
                    string details = $"Детали заявки #{selectedRequest.Id}\n\n" +
                                    $"Клиент: {selectedRequest.ClientName}\n" +
                                    $"Тур: {selectedRequest.TourName}\n" +
                                    $"ID тура: {selectedRequest.TourId}\n" +
                                    $"Дата заявки: {selectedRequest.FormattedRequestDate}\n" +
                                    $"Статус: {selectedRequest.Status}";

                    MessageBox.Show(details, "Детали заявки",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отображения деталей: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequestsDataGrid.SelectedItem is Request selectedRequest)
            {
                try
                {
                    var tour = _database.GetTourById(selectedRequest.TourId);
                    if (tour != null && tour.FreeSeats <= 0)
                    {
                        MessageBox.Show("Невозможно подтвердить заявку: нет свободных мест в туре.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    bool success = _database.UpdateRequestStatus(selectedRequest.Id, "Подтверждена");

                    if (success)
                    {
                        selectedRequest.Status = "Подтверждена";

                        var index = _filteredRequests.IndexOf(selectedRequest);
                        if (index >= 0)
                        {
                            _filteredRequests[index] = selectedRequest;
                        }

                        MessageBox.Show("Заявка успешно подтверждена!",
                            "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                        UpdateButtonsState();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при подтверждении заявки.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подтверждения заявки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            if (RequestsDataGrid.SelectedItem is Request selectedRequest)
            {
                try
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите отклонить заявку #{selectedRequest.Id}?",
                        "Подтверждение отклонения", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        bool success = _database.UpdateRequestStatus(selectedRequest.Id, "Отклонена");

                        if (success)
                        {
                            selectedRequest.Status = "Отклонена";

                            var index = _filteredRequests.IndexOf(selectedRequest);
                            if (index >= 0)
                            {
                                _filteredRequests[index] = selectedRequest;
                            }

                            MessageBox.Show("Заявка успешно отклонена!",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                            UpdateButtonsState();
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при отклонении заявки.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отклонения заявки: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || !_currentUser.IsAdmin)
            {
                MessageBox.Show("Создание заявок доступно только для администраторов",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var createRequestWindow = new CreateRequestWindow(_database);
                createRequestWindow.Owner = this;
                var result = createRequestWindow.ShowDialog();

                if (result == true)
                {
                    LoadRequests();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия окна создания заявки: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}