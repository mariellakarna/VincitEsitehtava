namespace HuonevarausAPI.Models
{
    public class Reservation
    {
        public Guid Id { get; set; } = Guid.NewGuid(); // Yksilöllinen tunniste
        public string RoomName { get; set; } = string.Empty; // Huoneen nimi
        public DateTime StartTime { get; set; } // Varauksen alkuaika
        public DateTime EndTime { get; set; } // Varauksen loppuaika
    }
}
