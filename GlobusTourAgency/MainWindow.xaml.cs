using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using GlobusTourAgency.Database;
using GlobusTourAgency.Models;

namespace GlobusTourAgency
{
    public partial class MainWindow : Window
    {
        private readonly SqlDatabaseService _database;
        private User _currentUser;
        private bool _isGuest;
        private ObservableCollection<Tour> _tours;

        public MainWindow(User user = null)
        {
            InitializeComponent();
            _database = new SqlDatabaseService();
            _currentUser = user;
            _isGuest = user == null;
            _tours = new ObservableCollection<Tour>();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeUI();
            LoadTours();
        }

        private void InitializeUI()
        {
            if (_isGuest)
            {
                UserInfoText.Text = "Гостевой доступ";
                ManagerPanel.Visibility = Visibility.Collapsed;
                ManagerControlsPanel.Visibility = Visibility.Collapsed;
                LogoutButton.Visibility = Visibility.Visible;
            }
            else if (_currentUser != null)
            {
                UserInfoText.Text = $"Вы вошли как: {_currentUser.FullName} ({_currentUser.Role})";
                LogoutButton.Visibility = Visibility.Visible;

                if (_currentUser.IsAuthorizedClient)
                {
                    ManagerPanel.Visibility = Visibility.Collapsed;      
                    ManagerControlsPanel.Visibility = Visibility.Collapsed; 
                }
                else if (_currentUser.IsManager)
                {
                    ManagerPanel.Visibility = Visibility.Visible;         
                    ManagerControlsPanel.Visibility = Visibility.Visible; 

                    CreateTourButton.Visibility = Visibility.Collapsed;

                    BusManagementButton.Visibility = Visibility.Visible;
                    RequestsButton.Visibility = Visibility.Visible;
                }
                else if (_currentUser.IsAdmin)
                {
                    ManagerPanel.Visibility = Visibility.Visible;         
                    ManagerControlsPanel.Visibility = Visibility.Visible;  

                    CreateTourButton.Visibility = Visibility.Visible;
                    BusManagementButton.Visibility = Visibility.Visible;
                    RequestsButton.Visibility = Visibility.Visible;
                }
                else
                {
                    ManagerPanel.Visibility = Visibility.Collapsed;
                    ManagerControlsPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void LoadTours()
        {
            try
            {
                ToursPanel.Children.Clear();
                _tours.Clear();

                bool useFilters = false;
                string searchText = "";
                string countryFilter = "";
                string sortBy = "По дате (возрастание)";

                if (_currentUser != null && (_currentUser.IsManager || _currentUser.IsAdmin))
                {
                    useFilters = true;

                    searchText = SearchTextBox?.Text ?? "";

                    if (CountryFilterComboBox.SelectedItem is ComboBoxItem countryItem)
                    {
                        countryFilter = countryItem.Content.ToString();
                    }

                    if (SortComboBox.SelectedItem is ComboBoxItem sortItem)
                    {
                        sortBy = sortItem.Content.ToString();
                    }
                }

                var tours = _database.GetAllTours(
                    isGuestOrClient: !useFilters,  
                    searchText: searchText,
                    countryFilter: countryFilter,
                    sortBy: sortBy
                );

                if (tours == null || tours.Count == 0)
                {
                    ToursPanel.Children.Add(new TextBlock
                    {
                        Text = "Нет доступных туров",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        Margin = new Thickness(0, 20, 0, 0),
                        Foreground = Brushes.Gray
                    });
                    return;
                }

                foreach (var tour in tours)
                {
                    _tours.Add(tour);
                    AddTourCard(tour);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки туров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddTourCard(Tour tour)
        {
            var border = new Border
            {
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                Background = GetTourBackground(tour),
                Width = 280,
                CornerRadius = new CornerRadius(5)
            };

            var stackPanel = new StackPanel();

            var imageBorder = new Border
            {
                Height = 120,
                Margin = new Thickness(0, 0, 0, 10),
                CornerRadius = new CornerRadius(3),
                ClipToBounds = true,
                Background = Brushes.LightGray
            };

            try
            {
                string imagePath = tour.PhotoPath;
                if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();

                        imageBorder.Child = new Image
                        {
                            Source = bitmap,
                            Stretch = Stretch.UniformToFill
                        };
                        imageBorder.Background = Brushes.Transparent;
                    }
                    catch
                    {
                        ShowDefaultImage(imageBorder);
                    }
                }
                else
                {
                    ShowDefaultImage(imageBorder);
                }
            }
            catch
            {
                ShowDefaultImage(imageBorder);
            }

            stackPanel.Children.Add(imageBorder);

            AddTourInfo(stackPanel, tour);

            AddBookingButton(stackPanel, tour);

            if (_currentUser != null && (_currentUser.IsManager || _currentUser.IsAdmin))
            {
                AddManagementButtons(stackPanel, tour);
            }

            border.Child = stackPanel;
            ToursPanel.Children.Add(border);
        }

        private void ShowDefaultImage(Border imageBorder)
        {
            imageBorder.Child = new TextBlock
            {
                Text = "Фото тура",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.DarkGray
            };
        }

        private void AddTourInfo(StackPanel stackPanel, Tour tour)
        {
            stackPanel.Children.Add(new TextBlock
            {
                Text = tour.Name,
                FontSize = 14,
                FontWeight = tour.IsStartingSoon ? FontWeights.Bold : FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                Height = 40,
                Margin = new Thickness(0, 0, 0, 5)
            });

            stackPanel.Children.Add(new TextBlock { Text = $"Страна: {tour.Country}", Margin = new Thickness(0, 0, 0, 2) });
            stackPanel.Children.Add(new TextBlock { Text = $"Продолжительность: {tour.DurationDays} дней", Margin = new Thickness(0, 0, 0, 2) });
            stackPanel.Children.Add(new TextBlock { Text = $"Дата начала: {tour.FormattedStartDate}", FontWeight = tour.IsStartingSoon ? FontWeights.Bold : FontWeights.Normal, Margin = new Thickness(0, 0, 0, 2) });
            stackPanel.Children.Add(new TextBlock { Text = $"Цена: {tour.FormattedPrice}", FontWeight = FontWeights.Bold, Foreground = Brushes.Green, Margin = new Thickness(0, 5, 0, 2) });
            stackPanel.Children.Add(new TextBlock { Text = $"Места: {tour.FreeSeatsInfo}", Margin = new Thickness(0, 0, 0, 2) });
            stackPanel.Children.Add(new TextBlock { Text = $"Тип автобуса: {tour.BusType}", Margin = new Thickness(0, 0, 0, 2) });

            if (tour.Discount > 0)
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"Скидка: {tour.Discount}%",
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Red,
                    Margin = new Thickness(0, 5, 0, 5)
                });
            }

            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Статус: {tour.DaysUntilStart}",
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        private void AddBookingButton(StackPanel stackPanel, Tour tour)
        {
            if (!_isGuest && _currentUser != null &&
                (_currentUser.IsAuthorizedClient || _currentUser.IsManager || _currentUser.IsAdmin))
            {
                var bookButton = new Button
                {
                    Content = "Забронировать",
                    Margin = new Thickness(0, 5, 0, 0),
                    Background = Brushes.Green,
                    Foreground = Brushes.White,
                    Tag = tour.Id,
                    Height = 30
                };
                bookButton.Click += BookButton_Click;
                stackPanel.Children.Add(bookButton);
            }
        }

        private void AddManagementButtons(StackPanel stackPanel, Tour tour)
        {
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            if (_currentUser.IsAdmin)
            {
                var editButton = new Button
                {
                    Content = "Редактировать",
                    Tag = tour.Id,
                    Margin = new Thickness(5, 0, 5, 0),
                    Width = 120,
                    Height = 25,
                    Background = Brushes.Orange,
                    Foreground = Brushes.White,
                    FontSize = 12
                };
                editButton.Click += EditTourButton_Click;
                buttonsPanel.Children.Add(editButton);
            }

            if (_currentUser.IsAdmin)
            {
                var deleteButton = new Button
                {
                    Content = "Удалить",
                    Tag = tour.Id,
                    Margin = new Thickness(5, 0, 5, 0),
                    Width = 100,
                    Height = 25,
                    Background = Brushes.Red,
                    Foreground = Brushes.White,
                    FontSize = 12
                };
                deleteButton.Click += DeleteTourButton_Click;
                buttonsPanel.Children.Add(deleteButton);
            }

            if (buttonsPanel.Children.Count > 0)
            {
                stackPanel.Children.Add(buttonsPanel);
            }
        }

        private Brush GetTourBackground(Tour tour)
        {
            if (tour.IsSpecialOffer)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD700"));
            else if (tour.IsFewSeats)
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB6C1"));
            return Brushes.White;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadTours();
        }

        private void CountryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTours();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadTours();
        }

        private void BookButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int tourId)
            {
                try
                {
                    var tour = _database.GetTourById(tourId);
                    if (tour == null)
                    {
                        MessageBox.Show("Тур не найден!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (tour.FreeSeats <= 0)
                    {
                        MessageBox.Show("Извините, мест больше нет!", "Нет мест",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var bookingWindow = new BookingWindow(tourId, _currentUser, _database);
                    bookingWindow.Owner = this;
                    var result = bookingWindow.ShowDialog();

                    if (result == true)
                    {
                        LoadTours();
                        MessageBox.Show("Заявка успешно создана!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка бронирования: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CreateTourButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || !_currentUser.IsAdmin)
            {
                MessageBox.Show("Доступно только для администраторов",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var createTourWindow = new CreateTourWindow(_database);
                createTourWindow.Owner = this;
                var result = createTourWindow.ShowDialog();

                if (result == true)
                {
                    LoadTours();
                    MessageBox.Show("Тур успешно создан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания тура: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BusManagementButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || (!_currentUser.IsManager && !_currentUser.IsAdmin))
            {
                MessageBox.Show("Доступно только для менеджеров и администраторов",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var busWindow = new BusManagementWindow(_database);
                busWindow.Owner = this;
                busWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия управления автобусами: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RequestsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || (!_currentUser.IsManager && !_currentUser.IsAdmin))
            {
                MessageBox.Show("Доступно только для менеджеров и администраторов",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var requestsWindow = new RequestsWindow(_currentUser);
                requestsWindow.Owner = this;
                requestsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия заявок: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditTourButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || !_currentUser.IsAdmin)
            {
                MessageBox.Show("Доступно только для администраторов",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button button && button.Tag is int tourId)
            {
                try
                {
                    var tour = _database.GetTourById(tourId);
                    if (tour == null)
                    {
                        MessageBox.Show("Тур не найден!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var editTourWindow = new EditTourWindow(_database, tour);
                    editTourWindow.Owner = this;
                    var result = editTourWindow.ShowDialog();

                    if (result == true)
                    {
                        LoadTours();
                        MessageBox.Show("Тур успешно обновлен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка редактирования тура: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteTourButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || !_currentUser.IsAdmin)
            {
                MessageBox.Show("Доступно только для администраторов",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button button && button.Tag is int tourId)
            {
                try
                {
                    var result = MessageBox.Show($"Вы уверены, что хотите удалить данный тур?",
                        "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        bool success = _database.DeleteTour(tourId);

                        if (success)
                        {
                            MessageBox.Show($"Тур успешно удален",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadTours();
                        }
                        else
                        {
                            MessageBox.Show("Ошибка при удалении тура",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления тура: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }
}