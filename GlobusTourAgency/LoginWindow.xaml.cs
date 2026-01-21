using System.Windows;
using GlobusTourAgency.Database;
using GlobusTourAgency.Models;

namespace GlobusTourAgency
{
    public partial class LoginWindow : Window
    {
        private readonly SqlDatabaseService _database;

        public LoginWindow()
        {
            InitializeComponent();
            _database = new SqlDatabaseService();
        }

        private void GuestLoginButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = new MainWindow(null); 
            mainWindow.Show();
            this.Close();
        }

        private void ManagerLoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            User user = _database.Authenticate(login, password);

            if (user != null)
            {
                if (user.IsAdmin || user.IsManager || user.IsAuthorizedClient)
                {
                    var mainWindow = new MainWindow(user);
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("У вас нет доступа к системе. Обратитесь к администратору.",
                        "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}