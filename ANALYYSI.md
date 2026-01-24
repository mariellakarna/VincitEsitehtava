# ANALYYSI

Tässä tiedostossa on esitehtävän loppuyhteenveto sekä analyysiä siitä, miten tekoäly suoriutuu parikoodarin roolissa.
Ensimmäisenä on vastaukseni tehtävänannossa esitettyihin kysymyksiin, ja lopussa on ajatuksiani koottuna tehtävän
suorittamisesta.

## 1. Mitä tekoäly teki hyvin?
Tässä tapauksessa tekoäly osasi oikeasti yllättävän hyvin luoda toimivan koodin antamieni promptien perusteella!
Tekoäly alkaa herkästi pakkaamaan koodia yhteen ja samaan luokkaan mikä saa koodin näyttämään sekavalta ja turhan pitkältä,
mutta nyt se osasi ihan itse luoda järkevän tiedostorakenteen. Se osasi siis jakaa koodin kontrollereihin, serviceihin ja
malleihin ilman, että minun piti erikseen ohjeistaa sitä tekemään niin, josta oikeasti yllätyin.

Toinen asia josta olin positiivisesti yllättynyt oli se, miten hyvin tekoäly ymmärsi suomen kieltä. Ainakin ennen
olen kokenut että englanniksi annetut promptit menevät tekoälyllä paremmin perille. Nyt se sai oikein hyvin kiinni,
mitä tarkoitin prompteissani ja osasi antaa kelpoja vastauksia ja koodia.

Tekoäly osasi myös luoda endpointeista ensimmäisellä yrittämällä toimivat, mikä on mielestäni iso plussa, ettei
niiden kanssa tuskailuun mennyt liiaksi aikaa.

## 2. Mitä tekoäly teki huonosti?
Tekoälyllä jäi huomaamatta joitakin oleellisia yksityiskohtia, kuten se, että se ei ollut osannut luoda itse varmistusta
siitä, onko varattava huone olemassa ennen kuin varaus tehdään. Tämän vuoksi jouduin itse lisäämään koodiin tarkistuksen
kyseistä ongelmaa varten.

Toinen asia, joka ikävystytti hivenen oli se, että ajan käsittely oli tekoälyllä hieman hakusessa. Se oli laittanut
huoneiden varausajan DateTime-tyyppiin, joka sitten osaltaan vaikutti siihen, että koodia piti pyöritellä monessa
eri luokassa ja monessa eri kohtaa, jotta varauslogiikka alkoi toimimaan oikein.

Kolmas painava juttu on se, ettei tekoäly osannut vaatia datan validointia.

## 3. Mitkä olivat tärkeimmät parannukset, jotka teit tekoälyn tuottamaan koodiin ja miksi?
Ensimmäinen helppo parannus suorittaa pois alta oli se, että lisäsin tarkistuksen huoneen olemassaololle ennen 
varauksen tekemistä. Tämä oli tärkeä parannus, sillä APIlle olisi voinut muuten syöttää vaikka millaisia varauksia
ja mistä huoneista ilman että ne kävisivät millään tapaa järkeen. Ongelma oli helppo korjata lisäämällä kontrolleriin
lukuoikeus tietokantaan ennen varauksen tekemistä. Sitten huoneen olemassaolo tarkistetaan Create-metodin alussa
kätevästi tällä tavalla:

```
var roomExists = _db.Rooms.Any(r => r.Name.ToLower() == dto.RoomName.ToLower());
if (!roomExists)
	return BadRequest("Huonetta ei löydy. Varaus voidaan tehdä vain olemassaoleviin huoneisiin.");
```

Toinen helppo parannus jonka tein ihan vain ajatellen ohjelman testattavuutta manuaalisesti oli se, että lisäsin
mahdollisuuden poistaa huoneen varaus antamalla kenttään huoneen nimen ja varauksen aloitus- ja lopetusajan. Tämä
olisi toki onnistunut myös ID:llä, mutta ainakin itse koin varauksen poistamisen helpommaksi tällä tavalla. Se saattaa tehdä
koodista toki hieman pidemmän kuin jos poistaisi vain ID:llä, mutta koen että se on sen arvoista ja helpotti minun testaamistani
huomattavasti.

```
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
```

Kolmas parannus, jonka tein oli se, että lisäsin datan validoinnin. Tämä on mielestäni tärkeä parannus, sillä se varmistaa
että käyttäjä syöttää API:lle oikeanlaista dataa, ja näin on pienennetty huomattavasti sitä mahdollisuutta, että
API menisi aivan solmuun vääränlaisen datan vuoksi.

```
public class ReservationCreateDto
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string RoomName { get; set; } = string.Empty;
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$",
        ErrorMessage = "Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.")]
    public string StartTime { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm"
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$",
        ErrorMessage = "Aikamuoto on virheellinen. Käytä muotoa yyyy-MM-dd HH:mm.")]
    public string EndTime { get; set; } = string.Empty; // "yyyy-MM-dd HH:mm"
}
```

Neljäs merkittävä parannus oli se, että muutin varausajan käsittelyn DateTime-tyypistä vahvasti tyypitetyksi
DateTimeOffset-tyypiksi. Tämä parannus on tärkeä, sillä se helpotti huomattavasti varauslogiikan toteuttamista
ja teki koodista selkeämpää. DateTimeOffset ottaisi huomioon myös aikavyöhykkeet, mutta tässä tapauksessa olen
kovakoodannut ohjelman käyttämään vain Suomen aikavyöhykettä, joten se ei ole ongelma. Isommassa skaalassa homma
tulisi ehdottomasti hoitaa niin, että aikavyöhykkeet otetaan huomioon.

```
    public DateTimeOffset StartTime { get; set; } // Varauksen alkuaika
    public DateTimeOffset EndTime { get; set; } // Varauksen loppuaika
```

## Loppukaneetti ja ajatuksiani
Kaiken kaikkiaan tekoäly suoriutui yllättävän hyvin parikoodarin roolissa. Se osasi luoda (suhteellisen) toimivan koodin,
kunhan vain osasi ohjeistaa sitä tarpeeksi hyvin. Itse olen kokenut parhaimmaksi taktiikaksi promptata tekoälyä
niin, että antaa sille tietoa pienissä määrin antaen kuitenkin tarpeeksi yksityiskohtia. Jos tekoälylle antaisi vaikka
koko tehtävänannon kerralla, se menisi hyvin herkästi solmuun ja alkaisi tekemään virheitä, eikä mikään lopulta
toimisi ollenkaan. Pienissä määrissä tieto on tekoälylle helpompi käsitellä ja se pystyy keskittymään paremmin
siihen, mitä siltä pyydetään. Tällä tavalla on mielestäni tehokkain tapa työskennellä tekoälyn kanssa.

Minulla jäi varmasti huomaamatta useitakin asioita joita tekoälyn luomassa koodissa voisi parantaa, mutta korjasin mielestäni
oleellisimmat tähän hätään. Uskon että kokemuksen karttuessa harjaannun varmasti havaitsemaan, miten mikäkin
koodinpätkä tuotettaisiin mahdollisimman kompaktiksi, yksinkertaiseksi ja tehokkaaksi.

Tykkäsin tästä tehtävästä kovasti, sillä tehtävänanto oli sopivan yksityiskohtainen, että sain todella hyvin kiinni,
mitä tässä haettiin takaa, mutta oli myös varaa tehdä omia pieniä olettamuksia ohjelman toiminnoista. Koen, että tehtävä
simuloi hyvin tulevia työtehtäviä, sillä käsitykseni mukaan tekoälyä käytetään yhä enemmän ja enemmän IT-alan työelämässä,
ja on tärkeää osata työskennellä sen kanssa tehokkaasti. Tekoäly ei tule korvaamaan ihmisiä koodareina, joten tehtävä antoi
hyvät valmiudet tarkastella tekoälyn tuottamaa koodia ja verrata, miten sitä pystyy parantamaan ihmisen toimesta.