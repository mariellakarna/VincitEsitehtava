using Microsoft.AspNetCore.Mvc;
using HuonevarausAPI.Models;
using HuonevarausAPI.Services;
using HuonevarausAPI.Data;

namespace HuonevarausAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService _service;
    private readonly AppDbContext _db;

    public ReservationsController(ReservationService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    // Tehdään ajan parsetukselle oma funktio, jotta ei tule turhia duplikaatteja
    private static bool TryParseLocalTime(string input, out DateTimeOffset result)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"); // UTC+2 (Suomi)
        if (DateTimeOffset.TryParseExact(input, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localTime))
        {
            // Asetetaan oikea offset
            var offset = tz.GetUtcOffset(localTime.DateTime);
            result = new DateTimeOffset(localTime.DateTime, offset);
            return true;
        }
        result = default;
        return false;
    }

    // Uusi, validoinnin ja parsetuksen yhdistävä metodi
    private ActionResult<(DateTimeOffset Start, DateTimeOffset End)> ValidateAndParseTimes(object dto, string startProp, string endProp)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var dtoType = dto.GetType();
        var startStr = dtoType.GetProperty(startProp)?.GetValue(dto) as string;
        var endStr = dtoType.GetProperty(endProp)?.GetValue(dto) as string;

        if (!TryParseLocalTime(startStr ?? "", out var start) ||
            !TryParseLocalTime(endStr ?? "", out var end))
        {
            return BadRequest("Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.");
        }

        if (start >= end)
            return BadRequest("Aloitusajan tulee olla ennen lopetusaikaa.");

        return (start, end);
    }

    // 1. Varauksen luonti
    [HttpPost]
    public ActionResult<Reservation> Create([FromBody] ReservationCreateDto dto)
    {
        // tässä kutsutaan ajanparsimismetodia, jotta parsimiskoodia ei tehdä itse HTTP-endpointissa kömpelösti
        var timeResult = ValidateAndParseTimes(dto, nameof(dto.StartTime), nameof(dto.EndTime));
        if (timeResult.Result != null)
            return timeResult.Result;

        var (start, end) = timeResult.Value;

        // Korjaus: Tarkistetaan, että huone on olemassa
        var roomExists = _db.Rooms.Any(r => r.Name.ToLower() == dto.RoomName.ToLower());
        if (!roomExists)
            return BadRequest("Huonetta ei löydy. Varaus voidaan tehdä vain olemassaoleviin huoneisiin.");

        var reservation = new Reservation
        {
            RoomName = dto.RoomName,
            StartTime = start,
            EndTime = end
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
        // kutsutaan ajanparsimismetodia
        var timeResult = ValidateAndParseTimes(dto, nameof(dto.StartTime), nameof(dto.EndTime));
        if (timeResult.Result != null)
            return timeResult.Result;

        var (start, end) = timeResult.Value;

        var success = _service.CancelReservation(dto.RoomName, start, end);
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