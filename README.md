# Vincit esitehtävä: Huoneenvarausrajapinta

Tässä README-tiedostossa ovat kirjattuna ohjeet huoneenvaraus-API:n käyttöönottoon. Tiedosto sisältää alkuolettamukset jotka tein ennen kuin aloitin toteuttamaan ohjelmaa,
NuGet Packaget jotka tarvitaan ohjelman toimintaan sekä kompaktin ohjeistuksen ohjelman endpointeista ja testaamisesta.

## Alkuoletukset:
- Palvelu käyttää Suomen aikavyöhykettä
- Palvelu käyttää suomenkielistä aakkostoa
- Kevyt in-memory tietokanta SQLitenä, jotta testausympäristön pystytys pysyy yksinkertaisena
- Ei yksikkö- tai integraatiotestejä, jotta testausympäristön pystytys pysyy yksinkertaisena
- Ei frontendiä, koska painotettiin nimenomaan API:n laatua ja dokumentointia

## Vaadittavat NuGet Packaget
- Microsoft.EntityFrameworkCore
- Microsoft.EntityFrameworkCore.Tools
- Microsoft.EntityFrameworkCore.SQLite

## Endpointit
POST: Create
- Luo uuden huonevarauksen antamalla huoneen ja varausajan tiedot.

DELETE: Cancel
- Poistaa huonevarauksen GUID:in avulla.

DELETE: CancelByDetails
- Poistaa huonevarauksen huoneen ja varausajan tiedoilla.

GET: GetByRoom
- Listaa haetun huoneen varaukset

## Ohjeet testaukseen
Huoneen varaamista voi testata antamalla varattavan huoneen nimi sekä varauksen alkamis- ja loppumisajankohta. Alla esimerkki:

```
{
  "roomName": "Neuvotteluhuone 1",
  "startTime": "2026-02-23 09:00",
  "endTime": "2026-02-23 10:30"
}
```

Huoneen varaukset voi nähdä antamalla tarkasteltavan huoneen nimen, vaikkapa äsken annetun varauksen näkee kirjoittamalla Swaggerin hakukenttään "Neuvotteluhuone 1".

Varauksen voi poistaa id:n perusteella tai antamalla varatun huoneen tiedot.
