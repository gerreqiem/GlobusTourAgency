namespace GlobusTourAgency.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public bool IsAdmin => Role == "Администратор";
        public bool IsManager => Role == "Менеджер";
        public bool IsAuthorizedClient => Role == "Авторизированный клиент";
        public bool IsGuest => false; 
    }
}