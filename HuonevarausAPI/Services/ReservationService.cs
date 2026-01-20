using HuonevarausAPI.Models;

namespace HuonevarausAPI.Services;

public class ReservationService
{
    private readonly List<Reservation> _reservations = new();

    // Lisää uusi varaus
    public (bool Success, string? Error, Reservation? Reservation) AddReservation(Reservation reservation)
    {
        if (reservation.StartTime >= reservation.EndTime)
            return (false, "Aloitusajan tulee olla ennen lopetusaikaa.", null);

        if (reservation.StartTime < DateTime.UtcNow || reservation.EndTime < DateTime.UtcNow)
            return (false, "Varauksen ajat eivät voi olla menneisyydessä.", null);

        var overlaps = _reservations.Any(r =>
            r.RoomName.Equals(reservation.RoomName, StringComparison.OrdinalIgnoreCase) &&
            r.StartTime < reservation.EndTime &&
            reservation.StartTime < r.EndTime);

        if (overlaps)
            return (false, "Huone on jo varattu kyseiselle aikavälille.", null);

        _reservations.Add(reservation);
        return (true, null, reservation);
    }

    // Poistaa varauksen tunnisteen perusteella (GUID)
    public bool CancelReservation(Guid id)
    {
        var res = _reservations.FirstOrDefault(r => r.Id == id);
        if (res == null) return false;
        _reservations.Remove(res);
        return true;
    }

    // Poistaa varauksen huoneen nimen ja aikavälin perusteella
    public bool CancelReservation(string roomName, DateTime startUtc, DateTime endUtc)
    {
        var res = _reservations.FirstOrDefault(r =>
            r.RoomName.Equals(roomName, StringComparison.OrdinalIgnoreCase) &&
            r.StartTime == startUtc &&
            r.EndTime == endUtc);

        if (res == null) return false;
        _reservations.Remove(res);
        return true;
    }

    // Palauttaa kaikki varaukset tietylle huoneelle
    public IEnumerable<Reservation> GetReservations(string roomName)
    {
        return _reservations.Where(r => r.RoomName.Equals(roomName, StringComparison.OrdinalIgnoreCase));
    }
}