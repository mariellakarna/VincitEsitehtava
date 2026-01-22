namespace HuonevarausAPI.Models
{
    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Yksilöllinen tunniste
        public string RoomName { get; set; } = string.Empty; // Huoneen nimi
        public DateTimeOffset StartTime { get; set; } // Varauksen alkuaika
        public DateTimeOffset EndTime { get; set; } // Varauksen loppuaika
    }
}
