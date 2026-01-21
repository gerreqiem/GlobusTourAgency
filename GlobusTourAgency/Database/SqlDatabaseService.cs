using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using GlobusTourAgency.Models;
using System.IO;
using System.Windows;

namespace GlobusTourAgency.Database
{
    public class SqlDatabaseService
    {
        private readonly string _connectionString;
        public string ConnectionString => _connectionString;
        public SqlDatabaseService()
        {
            _connectionString = @"Server=172.16.1.100,33678;Database=Karabekov;User Id=Karabekov;Password=p5N#dK;TrustServerCertificate=True;";
            Console.WriteLine($"Database service initialized with connection: {_connectionString}");
        }

        public User Authenticate(string login, string password)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    Console.WriteLine($"Attempting to authenticate: {login}");

                    string query = @"
                        SELECT 
                            UserID AS Id,
                            Role,
                            FullName,
                            Login,
                            Password
                        FROM Users 
                        WHERE Login = @login AND Password = @password";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", login);
                        command.Parameters.AddWithValue("@password", password);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var role = reader.IsDBNull(1) ? "" : reader.GetString(1);

                                var validRoles = new[] { "Авторизированный клиент", "Менеджер", "Администратор" };

                                if (!validRoles.Contains(role))
                                {
                                    Console.WriteLine($"Invalid role detected: {role}");
                                    role = "Авторизированный клиент";
                                }

                                var user = new User
                                {
                                    Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    Role = role,
                                    FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    Login = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                    Password = reader.IsDBNull(4) ? "" : reader.GetString(4)
                                };

                                Console.WriteLine($"User authenticated: {user.FullName}, Role: {user.Role}");
                                return user;
                            }
                        }
                    }
                }
                Console.WriteLine("Authentication failed - user not found");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error: {ex.Message}");
                return null;
            }
        }
        public List<Tour> GetAllTours(bool isGuestOrClient = true, string searchText = "",
                                       string countryFilter = "", string sortBy = "По дате (возрастание)")
        {
            var tours = new List<Tour>();

            try
            {
                Console.WriteLine($"=== ЗАГРУЗКА ТУРОВ ===");
                Console.WriteLine($"Режим: {(isGuestOrClient ? "Гость/Клиент" : "Менеджер")}");
                Console.WriteLine($"Поиск: '{searchText}'");
                Console.WriteLine($"Фильтр страны: '{countryFilter}'");
                Console.WriteLine($"Сортировка: '{sortBy}'");

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    t.TourID AS Id,
                    t.TourCode,
                    t.TourName AS Name,
                    c.CountryName AS Country,
                    t.DurationDays,
                    t.StartDate,
                    t.Price,
                    bt.TypeName AS BusType,
                    t.Capacity,
                    t.FreeSeats,
                    ISNULL(t.PhotoFileName, '') AS PhotoFileName,
                    ISNULL(t.Discount, 0) AS Discount
                FROM Tours t
                LEFT JOIN Countries c ON t.CountryID = c.CountryID
                LEFT JOIN BusTypes bt ON t.BusTypeID = bt.BusTypeID
                WHERE 1=1";

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        query += " AND (t.TourName LIKE @search OR c.CountryName LIKE @search)";
                    }

                    if (!string.IsNullOrWhiteSpace(countryFilter) && countryFilter != "Все")
                    {
                        query += " AND c.CountryName = @countryFilter";
                    }

                    if (isGuestOrClient)
                    {
                        query += " AND t.FreeSeats > 0";
                    }

                    string orderBy = sortBy switch
                    {
                        "По дате (возрастание)" => "t.StartDate ASC",
                        "По дате (убывание)" => "t.StartDate DESC",
                        "По цене (возрастание)" => "t.Price ASC",
                        "По цене (убывание)" => "t.Price DESC",
                        "По длительности" => "t.DurationDays DESC",
                        "По названию" => "t.TourName ASC",
                        "По количеству мест" => "t.FreeSeats DESC",
                        _ => "t.StartDate ASC"
                    };

                    query += $" ORDER BY {orderBy}";

                    Console.WriteLine($"SQL запрос: {query}");

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            command.Parameters.AddWithValue("@search", $"%{searchText}%");
                        }

                        if (!string.IsNullOrWhiteSpace(countryFilter) && countryFilter != "Все")
                        {
                            command.Parameters.AddWithValue("@countryFilter", countryFilter);
                        }

                        Console.WriteLine($"Выполняем SQL запрос с параметрами...");

                        using (var reader = command.ExecuteReader())
                        {
                            int count = 0;

                            while (reader.Read())
                            {
                                var tour = new Tour
                                {
                                    Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    TourCode = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                    Name = reader.IsDBNull(2) ? "Нет названия" : reader.GetString(2),
                                    Country = reader.IsDBNull(3) ? "Не указана" : reader.GetString(3),
                                    DurationDays = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                    StartDate = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5),
                                    Price = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                    BusType = reader.IsDBNull(7) ? "Не указан" : reader.GetString(7),
                                    Capacity = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                                    FreeSeats = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                                    PhotoFileName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                    Discount = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11)
                                };

                                tours.Add(tour);
                                count++;

                                Console.WriteLine($"Загружен тур #{count}: {tour.Name}, Цена: {tour.Price}, Дата: {tour.StartDate}");
                            }

                            Console.WriteLine($"=== ИТОГ: Загружено {count} туров ===");
                        }
                    }
                }

                if (tours.Count == 0)
                {
                    Console.WriteLine("В базе нет туров, создаем тестовые данные");
                    tours = CreateTestTours();

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        tours = tours.Where(t =>
                            t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                            t.Country.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if (!string.IsNullOrWhiteSpace(countryFilter) && countryFilter != "Все")
                    {
                        tours = tours.Where(t =>
                            t.Country.Equals(countryFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    tours = SortTours(tours, sortBy);
                }

                return tours;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ОШИБКА: {ex.Message} ===");
                Console.WriteLine(ex.StackTrace);

                var testTours = CreateTestTours();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    testTours = testTours.Where(t =>
                        t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        t.Country.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(countryFilter) && countryFilter != "Все")
                {
                    testTours = testTours.Where(t =>
                        t.Country.Equals(countryFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                testTours = SortTours(testTours, sortBy);

                return testTours;
            }
        }

        private List<Tour> SortTours(List<Tour> tours, string sortBy)
        {
            return sortBy switch
            {
                "По дате (возрастание)" => tours.OrderBy(t => t.StartDate).ToList(),
                "По дате (убывание)" => tours.OrderByDescending(t => t.StartDate).ToList(),
                "По цене (возрастание)" => tours.OrderBy(t => t.Price).ToList(),
                "По цене (убывание)" => tours.OrderByDescending(t => t.Price).ToList(),
                "По длительности" => tours.OrderByDescending(t => t.DurationDays).ToList(),
                "По названию" => tours.OrderBy(t => t.Name).ToList(),
                "По количеству мест" => tours.OrderByDescending(t => t.FreeSeats).ToList(),
                _ => tours.OrderBy(t => t.StartDate).ToList()
            };
        }

        private List<Tour> CreateTestTours()
        {
            Console.WriteLine("Создание тестовых туров...");

            return new List<Tour>
    {
        new Tour
        {
            Id = 1,
            TourCode = 101,
            Name = "Романтическая Италия: Рим, Флоренция, Венеция",
            Country = "Италия",
            DurationDays = 10,
            StartDate = DateTime.Now.AddDays(14),
            Price = 85000,
            BusType = "Стандарт",
            Capacity = 35,
            FreeSeats = 12,
            Discount = 0
        },
        new Tour
        {
            Id = 2,
            TourCode = 102,
            Name = "Париж и Замки Луары",
            Country = "Франция",
            DurationDays = 7,
            StartDate = DateTime.Now.AddDays(21),
            Price = 92500,
            BusType = "Комфорт",
            Capacity = 45,
            FreeSeats = 5,
            Discount = 10
        },
        new Tour
        {
            Id = 3,
            TourCode = 103,
            Name = "Австрийские Альпы: Зальцбург и Инсбрук",
            Country = "Австрия",
            DurationDays = 8,
            StartDate = DateTime.Now.AddDays(30),
            Price = 78300,
            BusType = "Стандарт",
            Capacity = 35,
            FreeSeats = 20,
            Discount = 20 
        },
        new Tour
        {
            Id = 4,
            TourCode = 104,
            Name = "Берлин, Дрезден, Мюнхен",
            Country = "Германия",
            DurationDays = 9,
            StartDate = DateTime.Now.AddDays(45),
            Price = 92000,
            BusType = "Комфорт",
            Capacity = 45,
            FreeSeats = 2,  
            Discount = 5
        },
        new Tour
        {
            Id = 5,
            TourCode = 105,
            Name = "Прага и Карловы Вары",
            Country = "Чехия",
            DurationDays = 6,
            StartDate = DateTime.Now.AddDays(3),
            Price = 58000,
            BusType = "Минивэн",
            Capacity = 16,
            FreeSeats = 10,
            Discount = 0
        }
    };
        }

        public List<string> GetCountries()
        {
            var countries = new List<string>() { "Все" };

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT DISTINCT CountryName 
                        FROM Countries 
                        ORDER BY CountryName";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                countries.Add(reader.GetString(0));
                            }
                        }
                    }
                }
                return countries;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading countries: {ex.Message}");
                return new List<string>() { "Все" };
            }
        }

        public bool CreateRequest(Request request)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string getClientIdQuery = @"
        SELECT UserID 
        FROM Users 
        WHERE FullName = @clientName";

                    int clientId = 0;
                    using (var getClientCommand = new SqlCommand(getClientIdQuery, connection))
                    {
                        getClientCommand.Parameters.AddWithValue("@clientName", request.ClientName);
                        var result = getClientCommand.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            clientId = Convert.ToInt32(result);
                        }
                        else
                        {
                            throw new Exception($"Клиент '{request.ClientName}' не найден в базе");
                        }
                    }

                    string checkQuery = @"
        SELECT FreeSeats, Price, Discount 
        FROM Tours 
        WHERE TourID = @tourId";

                    int freeSeats = 0;
                    decimal tourPrice = 0;
                    decimal discount = 0;

                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@tourId", request.TourId);
                        using (var reader = checkCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                freeSeats = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                tourPrice = reader.IsDBNull(1) ? 0 : reader.GetDecimal(1);
                                discount = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                            }
                        }
                    }

                    if (freeSeats <= 0)
                    {
                        throw new Exception("Нет свободных мест для бронирования");
                    }

                    decimal totalPrice = tourPrice;
                    if (discount > 0)
                    {
                        totalPrice = tourPrice * (1 - discount / 100);
                    }

                    int requestCode = GenerateSequentialRequestCode(connection);

                    string insertQuery = @"
        INSERT INTO Requests (
            RequestCode,
            TourID, 
            ClientID, 
            RequestDate, 
            Status, 
            PeopleCount, 
            TotalPrice,
            Comment
        ) VALUES (
            @requestCode,
            @tourId, 
            @clientId, 
            @requestDate, 
            @status, 
            1, -- PeopleCount по умолчанию 1
            @totalPrice, -- ИСПРАВЛЕНО: передаем рассчитанную цену
            '' -- Comment по умолчанию пустой
        )";

                    string updateSeatsQuery = @"
        UPDATE Tours 
        SET FreeSeats = FreeSeats - 1 
        WHERE TourID = @tourId";

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (var insertCommand = new SqlCommand(insertQuery, connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@requestCode", requestCode);
                                insertCommand.Parameters.AddWithValue("@tourId", request.TourId);
                                insertCommand.Parameters.AddWithValue("@clientId", clientId);
                                insertCommand.Parameters.AddWithValue("@requestDate", request.RequestDate);
                                insertCommand.Parameters.AddWithValue("@status", request.Status ?? "Новая");
                                insertCommand.Parameters.AddWithValue("@totalPrice", totalPrice);

                                insertCommand.ExecuteNonQuery();
                            }

                            using (var updateCommand = new SqlCommand(updateSeatsQuery, connection, transaction))
                            {
                                updateCommand.Parameters.AddWithValue("@tourId", request.TourId);
                                updateCommand.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            Console.WriteLine($"Заявка успешно создана с кодом: {requestCode}, сумма: {totalPrice:N0} руб.");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"Ошибка транзакции: {ex.Message}");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания заявки: {ex.Message}");
                throw;
            }
        }

        private int GenerateSequentialRequestCode(SqlConnection connection)
        {
            try
            {
                string tempTableQuery = @"
            WITH NumberSeries AS (
                SELECT 1 AS Number
                UNION ALL
                SELECT Number + 1 FROM NumberSeries WHERE Number < 1000
            )
            SELECT TOP 1 Number
            FROM NumberSeries
            WHERE Number NOT IN (
                SELECT RequestCode 
                FROM Requests 
                WHERE RequestCode > 0 AND RequestCode <= 1000
            )
            ORDER BY Number";

                using (var command = new SqlCommand(tempTableQuery, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        int availableCode = Convert.ToInt32(result);
                        Console.WriteLine($"Найден свободный код заявки: {availableCode}");
                        return availableCode;
                    }
                }

                string maxCodeQuery = "SELECT ISNULL(MAX(RequestCode), 0) FROM Requests WHERE RequestCode > 0";
                using (var command = new SqlCommand(maxCodeQuery, connection))
                {
                    var result = command.ExecuteScalar();
                    int maxCode = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;

                    int nextCode = maxCode + 1;

                    string checkCodeQuery = "SELECT COUNT(*) FROM Requests WHERE RequestCode = @code";
                    using (var checkCommand = new SqlCommand(checkCodeQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@code", nextCode);
                        var exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;

                        if (exists)
                        {
                            for (int i = nextCode + 1; i <= nextCode + 100; i++)
                            {
                                checkCommand.Parameters["@code"].Value = i;
                                exists = Convert.ToInt32(checkCommand.ExecuteScalar()) > 0;
                                if (!exists)
                                {
                                    Console.WriteLine($"Используем следующий свободный код: {i}");
                                    return i;
                                }
                            }
                        }
                    }

                    Console.WriteLine($"Используем следующий код: {nextCode}");
                    return nextCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка генерации кода заявки: {ex.Message}");

                try
                {
                    string maxIdQuery = "SELECT ISNULL(MAX(RequestID), 0) FROM Requests";
                    using (var command = new SqlCommand(maxIdQuery, connection))
                    {
                        var result = command.ExecuteScalar();
                        int maxId = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                        int simpleCode = maxId + 1000; // Чтобы не пересекаться с обычными кодами
                        Console.WriteLine($"Используем запасной код: {simpleCode}");
                        return simpleCode;
                    }
                }
                catch
                {
                    Random random = new Random();
                    int timeBasedCode = DateTime.Now.Second * 100 + random.Next(1, 99);
                    Console.WriteLine($"Используем временный код: {timeBasedCode}");
                    return timeBasedCode;
                }
            }
        }

        public List<Request> GetAllRequests(string searchText = "", string statusFilter = "Все")
        {
            var requests = new List<Request>();

            try
            {
                Console.WriteLine("=== ВЫПОЛНЕНИЕ ЗАПРОСА ДЛЯ ЗАЯВОК ===");

                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
    SELECT 
        r.RequestID AS Id,
        u.FullName AS ClientName,
        r.TourID,
        t.TourName,
        ISNULL(r.RequestDate, GETDATE()) AS RequestDate,
        r.Status
    FROM Requests r
    INNER JOIN Tours t ON r.TourID = t.TourID
    LEFT JOIN Users u ON r.ClientID = u.UserID
    WHERE 1=1";

                    if (!string.IsNullOrEmpty(searchText))
                    {
                        query += " AND (u.FullName LIKE @search OR t.TourName LIKE @search OR CONVERT(varchar, r.RequestID) LIKE @searchId)";
                    }

                    if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Все")
                    {
                        query += " AND r.Status = @status";
                    }

                    query += " ORDER BY r.RequestDate DESC";

                    Console.WriteLine($"SQL запрос:\n{query}");

                    using (var command = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            command.Parameters.AddWithValue("@search", $"%{searchText}%");
                            command.Parameters.AddWithValue("@searchId", $"%{searchText}%");
                        }

                        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Все")
                        {
                            command.Parameters.AddWithValue("@status", statusFilter);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            int count = 0;
                            Console.WriteLine("Столбцы в результате:");
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.WriteLine($"  [{i}] {reader.GetName(i)}");
                            }

                            while (reader.Read())
                            {
                                var request = new Request
                                {
                                    Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    ClientName = reader.IsDBNull(1) ? "Не указано" : reader.GetString(1),
                                    Phone = "", 
                                    Email = "", 
                                    TourId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                    TourName = reader.IsDBNull(3) ? "Не указан" : reader.GetString(3),
                                    RequestDate = reader.IsDBNull(4) ? DateTime.Now : reader.GetDateTime(4),
                                    Status = reader.IsDBNull(5) ? "Новая" : reader.GetString(5)
                                };

                                requests.Add(request);
                                count++;

                                Console.WriteLine($"Загружена заявка #{count}:");
                                Console.WriteLine($"  ID={request.Id}");
                                Console.WriteLine($"  Клиент={request.ClientName}");
                                Console.WriteLine($"  Тур ID={request.TourId}");
                                Console.WriteLine($"  Тур={request.TourName}");
                                Console.WriteLine($"  Дата={request.RequestDate}");
                                Console.WriteLine($"  Статус={request.Status}");
                            }

                            Console.WriteLine($"=== ИТОГО: Загружено {count} заявок ===");
                        }
                    }
                }
                return requests;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ОШИБКА загрузки заявок ===");
                Console.WriteLine($"Сообщение: {ex.Message}");
                Console.WriteLine($"Стек вызовов:\n{ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                }

                return new List<Request>();
            }
        }

        public bool UpdateRequestStatus(int requestId, string newStatus)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        UPDATE Requests 
                        SET Status = @status 
                        WHERE RequestID = @requestId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@status", newStatus);
                        command.Parameters.AddWithValue("@requestId", requestId);

                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"Updated request {requestId} status to {newStatus}");
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating request status: {ex.Message}");
                return false;
            }
        }

        public Tour GetTourById(int tourId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    t.TourID AS Id,
                    t.TourCode,
                    t.TourName AS Name,
                    c.CountryName AS Country,
                    t.DurationDays,
                    t.StartDate,
                    t.Price,
                    bt.TypeName AS BusType,
                    t.Capacity,
                    t.FreeSeats,
                    ISNULL(t.PhotoFileName, '') AS PhotoFileName,
                    ISNULL(t.Discount, 0) AS Discount
                FROM Tours t
                INNER JOIN Countries c ON t.CountryID = c.CountryID
                INNER JOIN BusTypes bt ON t.BusTypeID = bt.BusTypeID
                WHERE t.TourID = @tourId";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@tourId", tourId);

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return new Tour
                                {
                                    Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                    TourCode = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                                    Name = reader.IsDBNull(2) ? "Нет названия" : reader.GetString(2),
                                    Country = reader.IsDBNull(3) ? "Не указана" : reader.GetString(3),
                                    DurationDays = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                    StartDate = reader.IsDBNull(5) ? DateTime.Now : reader.GetDateTime(5),
                                    Price = reader.IsDBNull(6) ? 0 : reader.GetDecimal(6),
                                    BusType = reader.IsDBNull(7) ? "Не указан" : reader.GetString(7),
                                    Capacity = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                                    FreeSeats = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                                    PhotoFileName = reader.IsDBNull(10) ? "" : reader.GetString(10),
                                    Discount = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11)
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tour by ID: {ex.Message}");
                return null;
            }
        }

        public List<User> GetClients()
        {
            var clients = new List<User>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT 
                            UserID AS Id,
                            Role,
                            FullName,
                            Login,
                            Password
                        FROM Users 
                        WHERE Role = 'Авторизированный клиент'
                        ORDER BY FullName";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            clients.Add(new User
                            {
                                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                Role = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                FullName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Login = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Password = reader.IsDBNull(4) ? "" : reader.GetString(4)
                            });
                        }
                    }
                }
                return clients;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading clients: {ex.Message}");
                return new List<User>();
            }
        }
        public class BusType
        {
            public int BusTypeID { get; set; }
            public string TypeName { get; set; }
            public int Capacity { get; set; }
            public string Description { get; set; }
        }

        public List<BusType> GetAllBusTypes()
        {
            var busTypes = new List<BusType>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT BusTypeID, TypeName, Capacity, Description FROM BusTypes ORDER BY TypeName";

                    using (var command = new SqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            busTypes.Add(new BusType
                            {
                                BusTypeID = reader.GetInt32(0),
                                TypeName = reader.GetString(1),
                                Capacity = reader.GetInt32(2),
                                Description = reader.IsDBNull(3) ? "" : reader.GetString(3)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading bus types: {ex.Message}");
            }

            return busTypes;
        }
        public bool DeleteTour(int tourId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string checkQuery = @"
                SELECT COUNT(*) 
                FROM Requests 
                WHERE TourID = @tourId";

                    using (var checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@tourId", tourId);
                        int requestCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (requestCount > 0)
                        {
                            MessageBox.Show("Нельзя удалить тур, на который есть заявки",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return false;
                        }
                    }

                    string deleteQuery = @"
                DELETE FROM Tours 
                WHERE TourID = @tourId";

                    using (var deleteCommand = new SqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@tourId", tourId);
                        int rowsAffected = deleteCommand.ExecuteNonQuery();

                        Console.WriteLine($"Deleted tour #{tourId}, rows affected: {rowsAffected}");
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting tour: {ex.Message}");
                MessageBox.Show($"Ошибка удаления тура: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

    }
}