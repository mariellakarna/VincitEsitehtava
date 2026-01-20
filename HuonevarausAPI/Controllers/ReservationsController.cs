using Microsoft.AspNetCore.Mvc;
using HuonevarausAPI.Models;
using HuonevarausAPI.Services;

namespace HuonevarausAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService _service;

    public ReservationsController(ReservationService service)
    {
        _service = service;
    }

    // 1. Varauksen luonti
    [HttpPost]
    public ActionResult<Reservation> Create([FromBody] ReservationCreateDto dto)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"); // UTC+2 (Suomi)
        if (!DateTime.TryParseExact(dto.StartTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localStart) ||
            !DateTime.TryParseExact(dto.EndTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localEnd))
        {
            return BadRequest("Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.");
        }

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);

        var reservation = new Reservation
        {
            RoomName = dto.RoomName,
            StartTime = startUtc,
            EndTime = endUtc
        };

        var result = _service.AddReservation(reservation);

        if (!result.Success)
            return BadRequest(result.Error);

        return CreatedAtAction(nameof(GetByRoom), new { roomName = result.Reservation!.RoomName }, result.Reservation);
    }

    // 2a. Varauksen peruutus GUID:lla
    [HttpDelete("{id:guid}")]
    public IActionResult Cancel(Guid id)
    {
        var success = _service.CancelReservation(id);
        if (!success) return NotFound();
        return NoContent();
    }

    // 2b. Varauksen peruutus huoneen tiedoilla
    [HttpDelete]
    public IActionResult CancelByDetails([FromBody] ReservationCancelDto dto)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"); // UTC+2 (Suomi)
        if (!DateTime.TryParseExact(dto.StartTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localStart) ||
            !DateTime.TryParseExact(dto.EndTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localEnd))
        {
            return BadRequest("Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.");
        }

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);

        var success = _service.CancelReservation(dto.RoomName, startUtc, endUtc);
        if (!success) return NotFound();
        return NoContent();
    }

    // 3. Varausten katselu tietylle huoneelle
    [HttpGet("room/{roomName}")]
    public ActionResult<IEnumerable<Reservation>> GetByRoom(string roomName)
    {
        var reservations = _service.GetReservations(roomName);
        return Ok(reservations);
    }
}