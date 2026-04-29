# Restaurant System - Restaurantski Sistem

## Opis projekta

Sistem za upravljanje restoranom koji podržava distribuiranu obradu porudžbina, upravljanje stolovima i komunikaciju između osoblja preko TCP/UDP protokola.

## Arhitektura

Projekat je organizovan u Clean Architecture strukturu:

### Core (Domain sloj)
- **Models**: Osnovni entiteti sistema
  - `Sto` - predstavlja sto u restoranu
  - `Porudzbina` - predstavlja pojedinačnu stavku porudžbine
  - `InfoKlijenta` - informacije o klijentima (osoblju)
- **Repositories**: Interfejsi za pristup podacima
- **Services**: Interfejsi servisa
- **Enums**: Enumeracije (TipOsoblja, Status, Kategorija, itd.)

### Infrastructure
- **Mreza**: Implementacija mrežne komunikacije
  - `DirektorijumKlijenata` - registar klijenata sistema
- **Repositories**: Konkretne implementacije repozitorijuma
  - `RepozitorijumStolova` - upravljanje stolovima
  - `RepozitorijumHrane` - red čekanja za hranu
  - `RepozitorijumPica` - red čekanja za piće
  - `RepozitorijumKonobara` - upravljanje konobarima
- **Helpers**: Pomoćne klase
  - `PokretacKlijenata` - automatsko pokretanje klijentskih procesa

### Application
- **Services**: Konkretne implementacije servisa
  - `ServisSlanjaNaPripremu` - distribuira porudžbine kuvarima/barmenima
  - `ServisNotifikacija` - notifikacije osoblja

### Server
Centralni server koji upravlja:
- TCP komunikacijom za registraciju klijenata (port 5000)
- TCP komunikacijom za notifikacije (port 5001)
- UDP komunikacijom za dodeljivanje stolova (port 4000)
- UDP komunikacijom za oslobađanje stolova (port 4001)
- TCP komunikacijom za račune (port 4002)
- UDP komunikacijom za proveru rezervacija (port 4003)
- UDP komunikacijom za notifikacije menadžeru (port 4010)
- TCP komunikacijom za porudžbine (port 15000)
- TCP komunikacijom za dostave (port 4011)

### Klijenti
- **StaffClient (Konobar)**: Zauzeće stolova, pravljenje porudžbina, dostava, naplaćivanje
- **CustomerClient (Kuvar)**: Priprema hrane
- **BartenderClient (Barmen)**: Priprema pića
- **ManagerClient (Menadžer)**: Pravljenje rezervacija, provera rezervacija

## Mrežni protokoli

### TCP (garantovana isporuka)
- Registracija klijenata
- Slanje porudžbina
- Notifikacije o završenim porudžbinama
- Zahtevi za račun

### UDP (brza komunikacija)
- Dodeljivanje stolova
- Oslobađanje stolova

## Stack i batch procesovanje (Zadatak 5)

Server koristi **STEK (Stack - LIFO strukturu)** za čuvanje porudžbina:
- Svaka porudžbina se dodaje na odgovarajući stek (hrana ili piće)
- **Kada stek dostigne 5 stavki**, sve se grupišu i šalju kuvaru/barmenu
- Koristi se `ConcurrentStack<Porudzbina>` za thread-safety
- Batch procesovanje omogućava efikasniju distribuaciju
- Thread-safe operacije preko `lock` mehanizma

**Primer toka:**
1. Konobar naručuje 3 stavke hrane → dodaju se na stek (stek: 3)
2. Drugi konobar naručuje 2 stavke hrane → dodaju se na stek (stek: 5)
3. **Server detektuje da stek ima ≥5 stavki** → uzima 5 sa vrha steka
4. Batch od 5 porudžbina se šalje kuvaru
5. Proces se ponavlja



## Kako pokrenuti

1. Kompajlirati solution u Visual Studio
2. Pokrenuti Server.exe
3. Server automatski pokreće klijentske procese:
   - 2 konobara
   - 1 kuvara
   - 1 barmena
   - 1 menadžera

## Dijagram klasa

```
Core (Domain)
├── Models
│   ├── Sto
│   ├── Porudzbina
│   ├── InfoKlijenta
│   └── Rezervacija
├── Repositories (interfaces)
└── Services (interfaces)

Infrastructure
├── Mreza
│   └── DirektorijumKlijenata
├── Repositories (implementations)
│   ├── RepozitorijumStolova
│   ├── RepozitorijumHrane
│   ├── RepozitorijumPica
│   ├── RepozitorijumKonobara
│   └── RepozitorijumMenadzera
└── Helpers
    └── PokretacKlijenata

Application
└── Services (implementations)
    ├── ServisSlanjaNaPripremu
    ├── ServisNotifikacija
    ├── ServisUpravljanjaMenadzerom
    └── ServisOslobadjanjaStolovaMenadzer

Server
└── CentralniServer

Clients
├── StaffClient (Konobar)
├── CustomerClient (Kuvar)
├── BartenderClient (Barmen)
└── ManagerClient (Menadžer)
```

## Tehnologije

- C# .NET Framework 4.7.2
- TCP/UDP Socket programiranje
- Binary serialization
- Konkurentne kolekcije (ConcurrentDictionary, BlockingCollection)
- Multi-threading


