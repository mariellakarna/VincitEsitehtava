# PROMPTIT

Tämä tiedosto sisältää kaikki ennakkotehtävän tekemiseen käyttämäni promptit sekä tekoälyn niihin
generoimat vastaukset. Käytin tässä GitHub Copilotia ja tekoälymallina GPT-4:ää.

## VAIHE 1: API:N LUONTI TEKOÄLYLLÄ

### Vaihe 1.1 - Suurpiirteiset raamit huonevaraus-API:lle

Tässä vaiheessa pyysin tekoälyä luomaan minulle yksinkertaiset perusteet ja alkeelliset toiminnot
huonevaraus-API:a varten.

**Kehote:**
Koodaa tarvittavat raamit yksinkertaiselle kokoushuoneiden varausrajapinnalle, joka tarjoaa käyttäjälle seuraavat toiminnot:
•	Varauksen luonti: Varaa huone tietylle aikavälille
•	Varauksen peruutus: Poista varaus
•	Varausten katselu: Listaa kaikki tietyn huoneen varaukset Toteuta tämä mahdollisimman yksinkertaisesti (mutta kuitenkin toimivasti), ja avaa jokaisen vaiheen kohdalla, mitä kyseinen vaihe tekee.

**Tekoälyn vastaus:**
Tässä on yksinkertaiset raamit kokoushuoneiden varausrajapinnalle ASP.NET 8 Web API -projektissa. Toteutus käyttää in-memory-tietorakennetta (eli varaukset säilyvät vain sovelluksen käynnissäolon ajan). Jokainen vaihe on avattu selityksin.
1. Luo varausmalli (Reservation)
Tämä luokka kuvaa yksittäisen varauksen tietoja.
```
HuonevarausAPI\Models\Reservation.cs
namespace HuonevarausAPI.Models;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Yksilöllinen tunniste
    public string RoomName { get; set; } = string.Empty; // Huoneen nimi
    public DateTime StartTime { get; set; } // Varauksen alkuaika
    public DateTime EndTime { get; set; } // Varauksen loppuaika
}
```
Selitys:
Tämä malli määrittelee varauksen kentät: tunnisteen, huoneen nimen sekä alku- ja loppuajan.

---

2. Luo varauspalvelu (ReservationService)
Tämä palvelu hallinnoi varauksia muistissa.
```
using HuonevarausAPI.Models;

namespace HuonevarausAPI.Services;

public class ReservationService
{
    private readonly List<Reservation> _reservations = new();

    // Lisää uusi varaus
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
Tämä palvelu tarjoaa metodit varauksen lisäämiseen, poistamiseen ja hakemiseen huoneen perusteella.

---

3. Rekisteröi palvelu Program.cs-tiedostossa
Lisää palvelu dependency injectioniin.
```
builder.Services.AddSingleton<HuonevarausAPI.Services.ReservationService>();
```
Selitys:
Tämä rivi rekisteröi ReservationService-palvelun sovelluksen käyttöön.

---

4. Luo varauskontrolleri (ReservationsController)
Tämä kontrolleri tarjoaa REST-rajapinnan.
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
•	POST /api/reservations: Luo uuden varauksen.
•	DELETE /api/reservations/{id}: Poistaa varauksen tunnisteen perusteella.
•	GET /api/reservations/room/{roomName}: Listaa kaikki varaukset tietylle huoneelle.

---

Yhteenveto
•	Malli määrittelee varauksen rakenteen.
•	Palvelu hallinnoi varauksia muistissa.
•	Kontrolleri tarjoaa REST-rajapinnan varauksille.
•	Program.cs rekisteröi palvelun käyttöön.
Tämä rakenne on yksinkertainen, helposti laajennettavissa ja toimii suoraan testattavaksi esimerkiksi Swaggerin kautta.

### Vaihe 1.2 - Toimintalogiikka
Tässä vaiheessa kehotin tekoälyä lisäämään perustoimintoihin tarvittavaa logiikkaa, kuten tarkistamaan,
onko annettavat alkamis- ja päättymisajat järkeviä ja varmistamaan, etteivät ne mene päällekkäin.

**Kehote:**
Seuraavaksi ohjelman toimintalogiikka:
•	Varaukset eivät saa mennä päällekkäin (kaksi henkilöä ei voi varata samaa huonetta samaan aikaan).
•	Varaukset eivät voi sijoittua menneisyyteen.
•	Aloitusajan täytyy olla ennen lopetusaikaa.

**Tekoälyn vastaus:**
Tässä on tarvittavat muutokset ja selitykset, jotta toimintalogiikka täyttyy:
1. Päivitä ReservationService-luokan varauksen lisäyslogiikka
Lisätään tarkistukset:
•	Päällekkäisyyksien esto: Uusi varaus ei saa mennä päällekkäin olemassa olevien kanssa samassa huoneessa.
•	Menneisyyden esto: Varauksen alku- ja loppuaika eivät saa olla menneisyydessä.
•	Aloitusajan validointi: Alkuajan tulee olla ennen loppuaikaa.
```
using HuonevarausAPI.Models;

namespace HuonevarausAPI.Services;

public class ReservationService
{
    private readonly List<Reservation> _reservations = new();

    // Lisää uusi varaus
    public (bool Success, string? Error, Reservation? Reservation) AddReservation(Reservation reservation)
    {
        // 1. Aloitusajan täytyy olla ennen lopetusaikaa
        if (reservation.StartTime >= reservation.EndTime)
            return (false, "Aloitusajan tulee olla ennen lopetusaikaa.", null);

        // 2. Varaukset eivät voi sijoittua menneisyyteen
        if (reservation.StartTime < DateTime.UtcNow || reservation.EndTime < DateTime.UtcNow)
            return (false, "Varauksen ajat eivät voi olla menneisyydessä.", null);

        // 3. Varaukset eivät saa mennä päällekkäin
        var overlaps = _reservations.Any(r =>
            r.RoomName.Equals(reservation.RoomName, StringComparison.OrdinalIgnoreCase) &&
            r.StartTime < reservation.EndTime &&
            reservation.StartTime < r.EndTime);

        if (overlaps)
            return (false, "Huone on jo varattu kyseiselle aikavälille.", null);

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
•	AddReservation palauttaa tuple-muodossa onnistumisen, virheviestin ja varauksen.
•	Tarkistetaan ensin, että aloitusaika on ennen lopetusaikaa.
•	Tarkistetaan, ettei varauksen ajat ole menneisyydessä (käytetään UTC-aikaa).
•	Tarkistetaan, ettei samalle huoneelle ole päällekkäistä varausta.
•	Jos kaikki tarkistukset menevät läpi, varaus lisätään.

---

2. Päivitä kontrollerin varauksen luonti käyttämään uutta logiikkaa
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
Kontrolleri palauttaa virheviestin, jos jokin tarkistuksista epäonnistuu.

---

Yhteenveto:
Nyt varauslogiikka estää päällekkäiset varaukset, menneisyyteen sijoittuvat varaukset ja varmistaa,
että aloitusaika on ennen lopetusaikaa. Kaikki tarkistukset tehdään palvelutasolla, ja kontrolleri
palauttaa selkeän virheen käyttäjälle tarvittaessa.

***

**Oma kommentti:**
Tässä vaiheessa ohjelmaa käynnistäessä sain errorin, koska tekoäly ei ollut osannut huomioida,
että haettavan huoneen nimeä kutsuttiin vääränlaisella attribuutilla. Kysyin tekoälyltä virheestä,
ja se osasi vastata pulmaan oikein ja antaa sopivan koodinpätkän, jolla ongelma ratkeaa.

**Kehote:**
"'(bool Success, string? Error, Reservation? Reservation)' does not contain a definition for
'RoomName' and no accessible extension method 'RoomName' accepting a first argument of type
'(bool Success, string? Error, Reservation? Reservation)' could be found (are you missing a using
directive or an assembly reference?)"
Mitä tämä tarkoittaa?

**Tekoälyn vastaus:**
Tämä virheilmoitus tarkoittaa, että yrität käyttää ominaisuutta RoomName suoraan olioon, jonka 
tyyppi on tuple (bool Success, string? Error, Reservation? Reservation). Tuplessa ei ole suoraan 
RoomName-ominaisuutta, vaan se sisältää kentän Reservation, joka puolestaan on olio, jolla on 
RoomName.

Esimerkki virheellisestä käytöstä:

```
var result = _service.AddReservation(reservation);
return CreatedAtAction(nameof(GetByRoom), new { roomName = result.RoomName }, result);
```
Tässä result.RoomName aiheuttaa virheen, koska result on tuple, ei Reservation.
Oikea tapa: Sinun tulee käyttää result.Reservation?.RoomName:
```
var result = _service.AddReservation(reservation);
if (!result.Success)
    return BadRequest(result.Error);

return CreatedAtAction(nameof(GetByRoom), new { roomName = result.Reservation!.RoomName }, result.Reservation);
```
Yhteenveto:
•	Tuplesta haetaan ensin Reservation-kenttä, ja siitä RoomName.
•	Virhe tulee, jos yrität käyttää RoomName-ominaisuutta suoraan tupleen.

### Vaihe 1.3 - Tietokannan ja testidatan luonti
Tässä vaiheessa pyysin tekoälyä luomaan simppelin tietokantayhteyden ja lisäämään testidataa.

**Kehote:**
Luo nyt tietokanta ja sinne testidataa (eli huoneita), joilla tätä voi testata.

**Tekoälyn vastaus:**
Tässä on yksinkertainen tapa lisätä tietokanta ja testihuoneet .NET 8 Web API -projektiin Entity 
Framework Corella ja SQLite:lla. Näin saat huoneet talteen ja testattavaksi.

---

1. Lisää huonemalli
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
Lisää EF Core -konteksti, joka sisältää huoneet ja varaukset:
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

3. Lisää tietokantaohjelmointi Program.cs-tiedostoon
Lisää SQLite-tietokanta ja konteksti DI:hin:
```
using HuonevarausAPI.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Lisää tietokanta
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=huonevaraus.db"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<HuonevarausAPI.Services.ReservationService>();

var app = builder.Build();

// Testidatan lisäys
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

4. Lisää NuGet-paketit
Asenna nämä NuGet-paketit projektillesi:
•	Microsoft.EntityFrameworkCore
•	Microsoft.EntityFrameworkCore.Sqlite
Voit tehdä tämän Visual Studio:ssa NuGet Package Manager -ikkunasta.

---

Yhteenveto:
•	Room-malli kuvaa huoneen.
•	AppDbContext hallinnoi huoneita ja varauksia tietokannassa.
•	Program.cs luo tietokannan ja lisää testihuoneet automaattisesti, jos niitä ei ole.
•	Käytössä SQLite, joka ei vaadi erillistä palvelinta.
Nyt voit testata huoneiden olemassaoloa ja laajentaa rajapintaa tarpeen mukaan!

### Vaihe 1.4 - Ohjelman yksinkertaistamista ja kevyttä viilausta käyttäjäystävällisemmäksi

Tässä vaiheessa ohjasin tekoälyä muokkaamaan tiettyjä ominaisuuksia käyttäjäystävällisemmiksi.
En sorkkinut käyttäjäkokemusta liikaa, jotta koodi pysyy mahdollisimman raakileena ja selkeänä
tekoälyn tuotoksena, mutta ihan vain omaa testaustani helpottaakseni kehotin tekoälyä koodaamaan
ajanvarauksen (ja ajanvarauksen poiston) annettavaksi yksinkertaisemmassa formaatissa, sillä 
alkuperäinen testausformaatti yksilöllisen tunnisteen perusteella oli liian vaivalloinen
testattavaksi manuaalisesti. Kehotin myös tekoälyä käsittelemään aikavyöhykkeitä niin, että käyttäjä
voi antaa varauksen meidän ajassamme, eli UTC+2.

**Kehote:**
Muokkaa ohjelmaa niin, että käyttäjä voi antaa varauksen alkamis- ja loppumisajankohdan
yksinkertaisemmassa muodossa, ja mieluiten Utc+2 aikavyöhykkeellä.

**Tekoälyn vastaus:**
Tässä on selkeä ja toimiva tapa toteuttaa pyyntösi:
1. Luo DTO-malli varauksen luontia varten
Käyttäjä antaa ajan muodossa "yyyy-MM-dd HH:mm" (esim. "2026-01-20 09:00"), joka tulkitaan aikavyöhykkeellä UTC+2.

```namespace HuonevarausAPI.Models;

public class ReservationCreateDto
{
    public string RoomName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm"
    public string EndTime { get; set; } = string.Empty;   // "yyyy-MM-dd HH:mm"
}
```

---

2. Päivitä kontrollerin Create-metodi käyttämään DTO:ta ja aikavyöhykkeen käsittelyä

```
[HttpPost]
public ActionResult<Reservation> Create([FromBody] ReservationCreateDto dto)
{
    // Oletetaan, että käyttäjä antaa ajan muodossa "yyyy-MM-dd HH:mm"
    var tz = TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time"); // UTC+2 (Suomi)
    if (!DateTime.TryParseExact(dto.StartTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localStart) ||
        !DateTime.TryParseExact(dto.EndTime, "yyyy-MM-dd HH:mm", null, System.Globalization.DateTimeStyles.None, out var localEnd))
    {
        return BadRequest("Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.");
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
•	Käyttäjä antaa ajan selkeässä muodossa (esim. "2026-01-20 09:00").
•	Sovellus tulkitsee ajan Suomen aikavyöhykkeellä (UTC+2) ja muuntaa sen UTC:ksi ennen tallennusta.
•	Tallennus ja logiikka pysyvät yksinkertaisina, mutta käyttäjän syöte on helppo.
---
Muutokset:
•	Lisätty ReservationCreateDto-malli.
•	Muutettu Create-metodi käyttämään DTO:ta ja aikavyöhykkeen käsittelyä.
Nyt käyttäjä voi antaa varauksen ajat yksinkertaisessa muodossa ja ne tulkitaan automaattisesti UTC+2 aikavyöhykkeellä.

**Kehote:**
Varauksen poistaminen ei vielä toimi! Koodaa se niin, että se on yksinkertaisempi käyttäjälle tehdä.

**Tekoälyn vastaus:**
Voit tehdä varauksen poistamisesta käyttäjälle yksinkertaisempaa sallimalla peruutuksen huoneen nimen ja aikavälin perusteella (ilman varauksen tunnistetta). Toteutetaan tämä lisäämällä uusi endpoint, jossa käyttäjä antaa huoneen nimen sekä alku- ja loppuajan samassa muodossa kuin varauksen luonnissa.
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

2. Lisää palveluun metodi varauksen poistoon tiedoilla
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

Lisää uusi endpoint kontrolleriin
```
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
```

---

Mitä muutettiin ja miksi:
•	Käyttäjä voi nyt peruuttaa varauksen antamalla huoneen nimen ja aikavälin, ei tarvitse tietää varauksen tunnistetta.
•	Aikakäsittely on sama kuin varauksen luonnissa, joten käyttö on johdonmukaista.
•	Vanha tunnisteeseen perustuva poisto voi halutessa jäädä rinnalle.

Nyt varauksen peruutus onnistuu helposti esimerkiksi seuraavalla pyynnöllä:
```
{
  "roomName": "Neuvotteluhuone 1",
  "startTime": "2026-01-20 09:00",
  "endTime": "2026-01-20 10:00"
}
```

**Oma kommentti:**
Tässä on kokonaisuudessaan kaikki promptit joita käytin esitehtävän ensimmäiseen vaiheeseen! Seuraavassa
osassa ovat promptit vaiheesta 2, eli kun refaktoroin koodin itse turvallisemaksi ja korjaan
tekoälyn mahdollisesti tekemiä virheitä ja aukkoja.

## VAIHE 2: KOODIN REFAKTOROINTI OMIN KÄSIN

Tässä vaiheessa testailin koodia itsekseni tutkien, mitkä kohdat kaipaavat parannusta. Testasin manuaalisesti
ensiksi varauksen luontia virheellisillä syötteillä ja huomasin, että tekoäly ei ollut osannut ottaa
huomioon sitä, että huonetta ei välttämättä ole olemassa. Lisäsin siis tarkistuksen, että varattava huone
on olemassa, ennen kuin varaus hyväksytään.

**Kehote:**
Ohjelmassa on se ongelma, että se antaa lisätä varauksen huoneeseen, jota ei ole olemassa tietokannassa.
Kokeilin sitä niin, että varasin huoneen nimeltä "Neuvotteluhuone 4" ja ohjelma antoi minun tehdä niin.
Korjaa siis ohjelmaa, että varauksia pystyy tekemään vain olemassaoleviin huoneisiin.

**Tekoälyn vastaus:**
Korjaus vaatii, että varauksen luonnin yhteydessä tarkistetaan, löytyykö huone tietokannasta. Tämä tehdään kontrollerissa, koska tietokantayhteys (AppDbContext) on käytettävissä dependency injectionin kautta. ReservationService ei tällä hetkellä käytä tietokantaa, vaan pitää varaukset muistissa.
Lisää huoneen olemassaolon tarkistus ReservationsController-luokan Create-metodiin:
```
using Microsoft.AspNetCore.Mvc;
using HuonevarausAPI.Models;
using HuonevarausAPI.Services;
using HuonevarausAPI.Data; // Lisää tämä

namespace HuonevarausAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly ReservationService _service;
    private readonly AppDbContext _db; // Lisää tämä

    public ReservationsController(ReservationService service, AppDbContext db) // Lisää AppDbContext parametri
    {
        _service = service;
        _db = db;
    }

    // ... (muut metodit ennallaan)

    [HttpPost]
    public ActionResult<Reservation> Create([FromBody] ReservationCreateDto dto)
    {
        if (!TryParseLocalTime(dto.StartTime, out var start) ||
            !TryParseLocalTime(dto.EndTime, out var end))
        {
            return BadRequest("Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.");
        }

        if (start >= end)
            return BadRequest("Aloitusajan tulee olla ennen lopetusaikaa.");

        // TARKISTUS: Onko huone olemassa?
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

    // ... (muut metodit ennallaan)
}
```
Muutokset:
•	Lisätty AppDbContext kontrollerin konstruktorin parametreihin ja talletettu kenttään.
•	Lisätty huoneen olemassaolon tarkistus ennen varauksen luontia.
•	Jos huonetta ei löydy, palautetaan virheviesti.
Nyt varauksia voi tehdä vain huoneisiin, jotka löytyvät tietokannasta.

**Oma kommentti:**
Läpinäkyvyyden vuoksi, käytin myös tekoälyä pienenä apuna korjaamaan duplikaattikoodit pois Create- ja CancelByDetails
metodeista, mutta tässä vaiheessa vähän sekoilin GitHubin ja projektitiedostojen kanssa, joten siitä käymäni keskustelu
tekoälyn kanssa hävisi taivaan tuuliin enkä ollut vielä dokumentoinut sitä tänne :( Idea siinä kuitenkin oli
poistaa duplikaattikoodit erilliseen apumetodiin, joka hoitaa ajanparsimisen ja aikavyöhykkeen käsittelyn, jotta
sitä ei tehdä miljoonaa kertaa Createssa ja CancelByDetailsissa erikseen.