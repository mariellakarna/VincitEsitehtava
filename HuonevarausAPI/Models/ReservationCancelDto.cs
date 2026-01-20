namespace HuonevarausAPI.Models
{
    public class ReservationCancelDto
    {
        public string RoomName { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm"
        public string EndTime { get; set; } = string.Empty;   // "yyyy-MM-dd HH:mm"
    }
}
