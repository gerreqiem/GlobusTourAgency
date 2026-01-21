namespace GlobusTourAgency.Models
{
    public class Request
    {
        public int Id { get; set; }
        public string ClientName { get; set; }
        public string Phone { get; set; } = ""; 
        public string Email { get; set; } = "";
        public int TourId { get; set; }
        public string TourName { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string FormattedRequestDate => RequestDate.ToString("dd.MM.yyyy HH:mm");
    }
}