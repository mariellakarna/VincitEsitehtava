# PROMPTIT

T‰m‰ tiedosto sis‰lt‰‰ kaikki ennakkoteht‰v‰n tekemiseen k‰ytt‰m‰ni promptit sek‰ teko‰lyn niihin
generoimat vastaukset. K‰ytin t‰ss‰ GitHub Copilotia ja teko‰lymallina GPT-4:‰‰.

## VAIHE 1: API:N LUONTI TEKOƒLYLLƒ

### Vaihe 1.1 - Suurpiirteiset raamit huonevaraus-API:lle

T‰ss‰ vaiheessa pyysin teko‰ly‰ luomaan minulle yksinkertaiset perusteet ja alkeelliset toiminnot
huonevaraus-API:a varten.

**Kehote:**
Koodaa tarvittavat raamit yksinkertaiselle kokoushuoneiden varausrajapinnalle, joka tarjoaa k‰ytt‰j‰lle seuraavat toiminnot:
ï	Varauksen luonti: Varaa huone tietylle aikav‰lille
ï	Varauksen peruutus: Poista varaus
ï	Varausten katselu: Listaa kaikki tietyn huoneen varaukset Toteuta t‰m‰ mahdollisimman yksinkertaisesti (mutta kuitenkin toimivasti), ja avaa jokaisen vaiheen kohdalla, mit‰ kyseinen vaihe tekee.

**Teko‰lyn vastaus:**
T‰ss‰ on yksinkertaiset raamit kokoushuoneiden varausrajapinnalle ASP.NET 8 Web API -projektissa. Toteutus k‰ytt‰‰ in-memory-tietorakennetta (eli varaukset s‰ilyv‰t vain sovelluksen k‰ynniss‰olon ajan). Jokainen vaihe on avattu selityksin.
1. Luo varausmalli (Reservation)
T‰m‰ luokka kuvaa yksitt‰isen varauksen tietoja.
```
HuonevarausAPI\Models\Reservation.cs
namespace HuonevarausAPI.Models;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Yksilˆllinen tunniste
    public string RoomName { get; set; } = string.Empty; // Huoneen nimi
    public DateTime StartTime { get; set; } // Varauksen alkuaika
    public DateTime EndTime { get; set; } // Varauksen loppuaika
}
```
Selitys:
T‰m‰ malli m‰‰rittelee varauksen kent‰t: tunnisteen, huoneen nimen sek‰ alku- ja loppuajan.

---

2. Luo varauspalvelu (ReservationService)
T‰m‰ palvelu hallinnoi varauksia muistissa.
```
using HuonevarausAPI.Models;

namespace HuonevarausAPI.Services;

public class ReservationService
{
    private readonly List<Reservation> _reservations = new();

    // Lis‰‰ uusi varaus
    public Reservation AddReservation(Reservation reservation)
    {
        _reservations.Add(reservation);
        return reservation;
    }

    // Poistaa varauksen tunnisteen perusteella
    public bool CancelReservation(Guid id)
    {
        var res = _reservations.FirstOrDefault(r => r.Id == id);
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
```
Selitys:
T‰m‰ palvelu tarjoaa metodit varauksen lis‰‰miseen, poistamiseen ja hakemiseen huoneen perusteella.

---

3. Rekisterˆi palvelu Program.cs-tiedostossa
Lis‰‰ palvelu dependency injectioniin.
```
builder.Services.AddSingleton<HuonevarausAPI.Services.ReservationService>();
```
Selitys:
T‰m‰ rivi rekisterˆi ReservationService-palvelun sovelluksen k‰yttˆˆn.

---

4. Luo varauskontrolleri (ReservationsController)
T‰m‰ kontrolleri tarjoaa REST-rajapinnan.
```
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
    public ActionResult<Reservation> Create([FromBody] Reservation reservation)
    {
        if (reservation.StartTime >= reservation.EndTime)
            return BadRequest("EndTime must be after StartTime.");

        var created = _service.AddReservation(reservation);
        return CreatedAtAction(nameof(GetByRoom), new { roomName = created.RoomName }, created);
    }

    // 2. Varauksen peruutus
    [HttpDelete("{id:guid}")]
    public IActionResult Cancel(Guid id)
    {
        var success = _service.CancelReservation(id);
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
```
Selitys:
ï	POST /api/reservations: Luo uuden varauksen.
ï	DELETE /api/reservations/{id}: Poistaa varauksen tunnisteen perusteella.
ï	GET /api/reservations/room/{roomName}: Listaa kaikki varaukset tietylle huoneelle.

---

Yhteenveto
ï	Malli m‰‰rittelee varauksen rakenteen.
ï	Palvelu hallinnoi varauksia muistissa.
ï	Kontrolleri tarjoaa REST-rajapinnan varauksille.
ï	Program.cs rekisterˆi palvelun k‰yttˆˆn.
T‰m‰ rakenne on yksinkertainen, helposti laajennettavissa ja toimii suoraan testattavaksi esimerkiksi Swaggerin kautta.

### Vaihe 1.2 - Toimintalogiikka
T‰ss‰ vaiheessa kehotin teko‰ly‰ lis‰‰m‰‰n perustoimintoihin tarvittavaa logiikkaa, kuten tarkistamaan,
onko annettavat alkamis- ja p‰‰ttymisajat j‰rkevi‰ ja varmistamaan, etteiv‰t ne mene p‰‰llekk‰in.

**Kehote:**
Seuraavaksi ohjelman toimintalogiikka:
ï	Varaukset eiv‰t saa menn‰ p‰‰llekk‰in (kaksi henkilˆ‰ ei voi varata samaa huonetta samaan aikaan).
ï	Varaukset eiv‰t voi sijoittua menneisyyteen.
ï	Aloitusajan t‰ytyy olla ennen lopetusaikaa.

**Teko‰lyn vastaus:**
T‰ss‰ on tarvittavat muutokset ja selitykset, jotta toimintalogiikka t‰yttyy:
1. P‰ivit‰ ReservationService-luokan varauksen lis‰yslogiikka
Lis‰t‰‰n tarkistukset:
ï	P‰‰llekk‰isyyksien esto: Uusi varaus ei saa menn‰ p‰‰llekk‰in olemassa olevien kanssa samassa huoneessa.
ï	Menneisyyden esto: Varauksen alku- ja loppuaika eiv‰t saa olla menneisyydess‰.
ï	Aloitusajan validointi: Alkuajan tulee olla ennen loppuaikaa.
```
using HuonevarausAPI.Models;

namespace HuonevarausAPI.Services;

public class ReservationService
{
    private readonly List<Reservation> _reservations = new();

    // Lis‰‰ uusi varaus
    public (bool Success, string? Error, Reservation? Reservation) AddReservation(Reservation reservation)
    {
        // 1. Aloitusajan t‰ytyy olla ennen lopetusaikaa
        if (reservation.StartTime >= reservation.EndTime)
            return (false, "Aloitusajan tulee olla ennen lopetusaikaa.", null);

        // 2. Varaukset eiv‰t voi sijoittua menneisyyteen
        if (reservation.StartTime < DateTime.UtcNow || reservation.EndTime < DateTime.UtcNow)
            return (false, "Varauksen ajat eiv‰t voi olla menneisyydess‰.", null);

        // 3. Varaukset eiv‰t saa menn‰ p‰‰llekk‰in
        var overlaps = _reservations.Any(r =>
            r.RoomName.Equals(reservation.RoomName, StringComparison.OrdinalIgnoreCase) &&
            r.StartTime < reservation.EndTime &&
            reservation.StartTime < r.EndTime);

        if (overlaps)
            return (false, "Huone on jo varattu kyseiselle aikav‰lille.", null);

        _reservations.Add(reservation);
        return (true, null, reservation);
    }

    // Poistaa varauksen tunnisteen perusteella
    public bool CancelReservation(Guid id)
    {
        var res = _reservations.FirstOrDefault(r => r.Id == id);
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
```
Selitys:
ï	AddReservation palauttaa tuple-muodossa onnistumisen, virheviestin ja varauksen.
ï	Tarkistetaan ensin, ett‰ aloitusaika on ennen lopetusaikaa.
ï	Tarkistetaan, ettei varauksen ajat ole menneisyydess‰ (k‰ytet‰‰n UTC-aikaa).
ï	Tarkistetaan, ettei samalle huoneelle ole p‰‰llekk‰ist‰ varausta.
ï	Jos kaikki tarkistukset menev‰t l‰pi, varaus lis‰t‰‰n.

---

2. P‰ivit‰ kontrollerin varauksen luonti k‰ytt‰m‰‰n uutta logiikkaa
```
[HttpPost]
public ActionResult<Reservation> Create([FromBody] Reservation reservation)
{
    var result = _service.AddReservation(reservation);

    if (!result.Success)
        return BadRequest(result.Error);

    return CreatedAtAction(nameof(GetByRoom), new { roomName = result.Reservation!.RoomName }, result.Reservation);
}
```
Selitys:
Kontrolleri palauttaa virheviestin, jos jokin tarkistuksista ep‰onnistuu.

---

Yhteenveto:
Nyt varauslogiikka est‰‰ p‰‰llekk‰iset varaukset, menneisyyteen sijoittuvat varaukset ja varmistaa,
ett‰ aloitusaika on ennen lopetusaikaa. Kaikki tarkistukset tehd‰‰n palvelutasolla, ja kontrolleri
palauttaa selke‰n virheen k‰ytt‰j‰lle tarvittaessa.

***

T‰ss‰ vaiheessa ohjelmaa k‰ynnist‰ess‰ sain errorin, koska teko‰ly ei ollut osannut huomioida,
ett‰ haettavan huoneen nime‰ kutsuttiin v‰‰r‰nlaisella attribuutilla. Kysyin teko‰lylt‰ virheest‰,
ja se osasi vastata pulmaan oikein ja antaa sopivan koodinp‰tk‰n, jolla ongelma ratkeaa.

**Kehote:**
"'(bool Success, string? Error, Reservation? Reservation)' does not contain a definition for
'RoomName' and no accessible extension method 'RoomName' accepting a first argument of type
'(bool Success, string? Error, Reservation? Reservation)' could be found (are you missing a using
directive or an assembly reference?)"
Mit‰ t‰m‰ tarkoittaa?

**Teko‰lyn vastaus:**
T‰m‰ virheilmoitus tarkoittaa, ett‰ yrit‰t k‰ytt‰‰ ominaisuutta RoomName suoraan olioon, jonka 
tyyppi on tuple (bool Success, string? Error, Reservation? Reservation). Tuplessa ei ole suoraan 
RoomName-ominaisuutta, vaan se sis‰lt‰‰ kent‰n Reservation, joka puolestaan on olio, jolla on 
RoomName.

Esimerkki virheellisest‰ k‰ytˆst‰:

```
var result = _service.AddReservation(reservation);
return CreatedAtAction(nameof(GetByRoom), new { roomName = result.RoomName }, result);
```
T‰ss‰ result.RoomName aiheuttaa virheen, koska result on tuple, ei Reservation.
Oikea tapa: Sinun tulee k‰ytt‰‰ result.Reservation?.RoomName:
```
var result = _service.AddReservation(reservation);
if (!result.Success)
    return BadRequest(result.Error);

return CreatedAtAction(nameof(GetByRoom), new { roomName = result.Reservation!.RoomName }, result.Reservation);
```
Yhteenveto:
ï	Tuplesta haetaan ensin Reservation-kentt‰, ja siit‰ RoomName.
ï	Virhe tulee, jos yrit‰t k‰ytt‰‰ RoomName-ominaisuutta suoraan tupleen.

### Vaihe 1.3 - Tietokannan ja testidatan luonti
T‰ss‰ vaiheessa pyysin teko‰ly‰ luomaan simppelin tietokantayhteyden ja lis‰‰m‰‰n testidataa.

**Kehote:**
Luo nyt tietokanta ja sinne testidataa (eli huoneita), joilla t‰t‰ voi testata.

**Teko‰lyn vastaus:**
T‰ss‰ on yksinkertainen tapa lis‰t‰ tietokanta ja testihuoneet .NET 8 Web API -projektiin Entity 
Framework Corella ja SQLite:lla. N‰in saat huoneet talteen ja testattavaksi.

---

1. Lis‰‰ huonemalli
Luo uusi malli huoneille:
```
namespace HuonevarausAPI.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```
2. Luo tietokantakonteksti
Lis‰‰ EF Core -konteksti, joka sis‰lt‰‰ huoneet ja varaukset:
```
using Microsoft.EntityFrameworkCore;
using HuonevarausAPI.Models;

namespace HuonevarausAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
}
```

---

3. Lis‰‰ tietokantaohjelmointi Program.cs-tiedostoon
Lis‰‰ SQLite-tietokanta ja konteksti DI:hin:
```
using HuonevarausAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Lis‰‰ tietokanta
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=huonevaraus.db"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<HuonevarausAPI.Services.ReservationService>();

var app = builder.Build();

// Testidatan lis‰ys
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Rooms.Any())
    {
        db.Rooms.AddRange(
            new Room { Name = "Neuvotteluhuone 1" },
            new Room { Name = "Neuvotteluhuone 2" },
            new Room { Name = "Neuvotteluhuone 3" }
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

4. Lis‰‰ NuGet-paketit
Asenna n‰m‰ NuGet-paketit projektillesi:
ï	Microsoft.EntityFrameworkCore
ï	Microsoft.EntityFrameworkCore.Sqlite
Voit tehd‰ t‰m‰n Visual Studio:ssa NuGet Package Manager -ikkunasta.

---

Yhteenveto:
ï	Room-malli kuvaa huoneen.
ï	AppDbContext hallinnoi huoneita ja varauksia tietokannassa.
ï	Program.cs luo tietokannan ja lis‰‰ testihuoneet automaattisesti, jos niit‰ ei ole.
ï	K‰ytˆss‰ SQLite, joka ei vaadi erillist‰ palvelinta.
Nyt voit testata huoneiden olemassaoloa ja laajentaa rajapintaa tarpeen mukaan!

### Vaihe 1.4 - Ohjelman yksinkertaistamista ja kevytt‰ viilausta k‰ytt‰j‰yst‰v‰llisemm‰ksi

T‰ss‰ vaiheessa ohjasin teko‰ly‰ muokkaamaan tiettyj‰ ominaisuuksia k‰ytt‰j‰yst‰v‰llisemmiksi.
En sorkkinut k‰ytt‰j‰kokemusta liikaa, jotta koodi pysyy mahdollisimman raakileena ja selke‰n‰
teko‰lyn tuotoksena, mutta ihan vain omaa testaustani helpottaakseni kehotin teko‰ly‰ koodaamaan
ajanvarauksen (ja ajanvarauksen poiston) annettavaksi yksinkertaisemmassa formaatissa, sill‰ 
alkuper‰inen testausformaatti yksilˆllisen tunnisteen perusteella oli liian vaivalloinen
testattavaksi manuaalisesti. Kehotin myˆs teko‰ly‰ k‰sittelem‰‰n aikavyˆhykkeit‰ niin, ett‰ k‰ytt‰j‰
voi antaa varauksen meid‰n ajassamme, eli UTC+2.

**Kehote:**
Muokkaa ohjelmaa niin, ett‰ k‰ytt‰j‰ voi antaa varauksen alkamis- ja loppumisajankohdan
yksinkertaisemmassa muodossa, ja mieluiten Utc+2 aikavyˆhykkeell‰.

**Teko‰lyn vastaus:**
T‰ss‰ on selke‰ ja toimiva tapa toteuttaa pyyntˆsi:
1. Luo DTO-malli varauksen luontia varten
K‰ytt‰j‰ antaa ajan muodossa "yyyy-MM-dd HH:mm" (esim. "2026-01-20 09:00"), joka tulkitaan aikavyˆhykkeell‰ UTC+2.

```namespace HuonevarausAPI.Models;

public class ReservationCreateDto
{
    public string RoomName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm"
    public string EndTime { get; set; } = string.Empty;   // "yyyy-MM-dd HH:mm"
}
```

---

2. P‰ivit‰ kontrollerin Create-metodi k‰ytt‰m‰‰n DTO:ta ja aikavyˆhykkeen k‰sittely‰

```
[HttpPost]
public ActionResult<Reservation> Create([FromBody] ReservationCreateDto dto)
{
    // Oletetaan, ett‰ k‰ytt‰j‰ antaa ajan muodossa "yyyy-MM-dd HH:mm"
    var tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"); // UTC+2 (Suomi)
    if (!DateTime.TryParseExact(dto.StartTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localStart) ||
        !DateTime.TryParseExact(dto.EndTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localEnd))
    {
        return BadRequest("Aikamuoto on virheellinen. K‰yt‰ muotoa yyyy-MM-dd HH:mm.");
    }

    // Muunna paikallinen aika UTC:ksi
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
```

---

3. Lyhyt selitys
ï	K‰ytt‰j‰ antaa ajan selke‰ss‰ muodossa (esim. "2026-01-20 09:00").
ï	Sovellus tulkitsee ajan Suomen aikavyˆhykkeell‰ (UTC+2) ja muuntaa sen UTC:ksi ennen tallennusta.
ï	Tallennus ja logiikka pysyv‰t yksinkertaisina, mutta k‰ytt‰j‰n syˆte on helppo.
---
Muutokset:
ï	Lis‰tty ReservationCreateDto-malli.
ï	Muutettu Create-metodi k‰ytt‰m‰‰n DTO:ta ja aikavyˆhykkeen k‰sittely‰.
Nyt k‰ytt‰j‰ voi antaa varauksen ajat yksinkertaisessa muodossa ja ne tulkitaan automaattisesti UTC+2 aikavyˆhykkeell‰.

**Kehote:**
Varauksen poistaminen ei viel‰ toimi! Koodaa se niin, ett‰ se on yksinkertaisempi k‰ytt‰j‰lle tehd‰.

**Teko‰lyn vastaus:**
Voit tehd‰ varauksen poistamisesta k‰ytt‰j‰lle yksinkertaisempaa sallimalla peruutuksen huoneen nimen ja aikav‰lin perusteella (ilman varauksen tunnistetta). Toteutetaan t‰m‰ lis‰‰m‰ll‰ uusi endpoint, jossa k‰ytt‰j‰ antaa huoneen nimen sek‰ alku- ja loppuajan samassa muodossa kuin varauksen luonnissa.
1. Luo DTO varauksen peruutukselle
```
namespace HuonevarausAPI.Models;

public class ReservationCancelDto
{
    public string RoomName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm"
    public string EndTime { get; set; } = string.Empty;   // "yyyy-MM-dd HH:mm"
}
```

---

2. Lis‰‰ palveluun metodi varauksen poistoon tiedoilla
```
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
```

---

Lis‰‰ uusi endpoint kontrolleriin
```
[HttpDelete]
public IActionResult CancelByDetails([FromBody] ReservationCancelDto dto)
{
    var tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"); // UTC+2 (Suomi)
    if (!DateTime.TryParseExact(dto.StartTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localStart) ||
        !DateTime.TryParseExact(dto.EndTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localEnd))
    {
        return BadRequest("Aikamuoto on virheellinen. K‰yt‰ muotoa yyyy-MM-dd HH:mm.");
    }

    var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, tz);
    var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, tz);

    var success = _service.CancelReservation(dto.RoomName, startUtc, endUtc);
    if (!success) return NotFound();
    return NoContent();
}
```

---

Mit‰ muutettiin ja miksi:
ï	K‰ytt‰j‰ voi nyt peruuttaa varauksen antamalla huoneen nimen ja aikav‰lin, ei tarvitse tiet‰‰ varauksen tunnistetta.
ï	Aikak‰sittely on sama kuin varauksen luonnissa, joten k‰yttˆ on johdonmukaista.
ï	Vanha tunnisteeseen perustuva poisto voi halutessa j‰‰d‰ rinnalle.

Nyt varauksen peruutus onnistuu helposti esimerkiksi seuraavalla pyynnˆll‰:
```
{
  "roomName": "Neuvotteluhuone 1",
  "startTime": "2026-01-20 09:00",
  "endTime": "2026-01-20 10:00"
}
```

**Oma kommentti:**
T‰ss‰ on kokonaisuudessaan kaikki promptit joita k‰ytin esiteht‰v‰n ensimm‰iseen vaiheeseen! Seuraavassa
osassa ovat promptit vaiheesta 2, eli kun refaktoroin koodin itse turvallisemaksi ja korjaan
teko‰lyn mahdollisesti tekemi‰ virheit‰ ja aukkoja.