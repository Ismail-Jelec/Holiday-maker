# Holiday Maker

Konsolapplikation där en resebyråoperatör kan registrera kunder, söka lediga rum, boka, visa, ändra och avboka bokningar. Data lagras i MySQL och applikationen använder parameteriserade SQL-frågor.

## Filer

- `01_create_tables.sql` – skapar tabellerna
- `02_testdata.sql` – testdata
- `03_queries.sql` – SQL-frågorna som appen använder, testade i DataGrip
- `holiday_maker.png` – E/R-diagram
- `HolidayMaker/` – C#-applikationen

## Skapa databasen

Kräver MySQL 8.4 och .NET SDK 10.

1. Kör `CREATE DATABASE holiday_maker;`
2. Kör `01_create_tables.sql`
3. Kör `02_testdata.sql`

## Kör programmet

1. Ändra användare och lösenord i anslutningssträngen på rad 3 i `HolidayMaker/Program.cs`.
2. Kör:
   ```
   cd HolidayMaker
   dotnet run
   ```
3. Välj en kund (val 2) innan du söker eller bokar. Datum skrivs som `ÅÅÅÅ-MM-DD`.

## Datamodell

- `accommodation` 1 – N `room` (ett boende har många rum, ett rum tillhör ett boende)
- `customer` 1 – N `booking` (en kund kan ha många bokningar)
- `room` 1 – N `booking` (ett rum kan bokas många gånger under olika perioder)

`booking` kopplar ihop kund och rum. Totalpriset sparas inte utan räknas ut som `DATEDIFF(check_out, check_in) * price_per_night`.

## Tester

Sökfrågorna testades först i DataGrip (se `03_queries.sql`), därefter gjordes testerna nedan manuellt i konsolapplikationen.

| Test | Hur | Resultat |
|---|---|---|
| Ledigt rum visas, upptaget filtreras bort | Dubrovnik 10–15 dec: Hotel Ray rum 102 och 103 (bokade) visas inte | ✅ |
| Incheckning samma dag som utcheckning | Hotel Sun rum 202 är bokat 6–10 dec och visas vid sökning 10–15 dec | ✅ |
| Överlappande bokning nekas | Rum-id 1 är bokat 10–13 dec. Bokning 9–14 dec (omsluter) och 11–12 dec (inuti) nekas | ✅ |
| För stort sällskap | 3 personer i rum-id 44 (max 2) nekas | ✅ |
| Filter och sortering | Pool + max 200 m till strand ger bara Hotel Adriatic. Sortering på pris och på omdöme fungerar | ✅ |
| Ändra antal personer | Bokning 4 (max 2): 3 personer nekas, 2 godkänns | ✅ |
| Avbokning frigör rummet | Avbokade bokning 30, rum 41 visas igen i sökningen | ✅ |
| Data finns kvar efter omstart | Bokade, startade om programmet, bokningen visas fortfarande | ✅ |
| Sökning utan resultat | Makarska, 24–27 dec, 4 personer: alla fyra 4-personersrum (rum-id 24, 27, 42, 45) är bokade → "No available rooms found." Samma sökning med 2 personer ger träffar | ✅ |
| Felaktig inmatning | Bokstäver som siffra, fel datumformat, datum utanför december 2026 och utcheckning före incheckning frågas om utan att programmet kraschar | ✅ |
