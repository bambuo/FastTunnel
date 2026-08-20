<div align="center">

<img src="images/logo.png" width="150" align=center />

## FastTunnel

[![License](https://img.shields.io/badge/license-Apache%202-green.svg)](https://www.apache.org/licenses/LICENSE-2.0)
[![Build status](https://github.com/bambuo/FastTunnel/actions/workflows/dotnetcore.yml/badge.svg)](https://github.com/bambuo/FastTunnel/actions)
[![Nuget](https://img.shields.io/nuget/v/FastTunnel.Core)](https://www.nuget.org/packages/FastTunnel.Core/)

[中文文档](README.md) | [English](README.en.md)

</div>

## Kio estas FastTunnel

FastTunnel estas alt-efikeca, transplatforma ilo por retpenetrado. Per ĝi vi povas elmeti internajn retajn servojn al la publika reto, por vi mem aŭ por iu ajn.

- **TCP / UDP porda plusendado**: aliru iun ajn internan servon (mysql, redis, ssh, fora labortablo ktp.)
- **TTT-tuneloj**: aliru internajn retpaĝajn servojn per propraj domajnoj / subdomajnoj (ofte uzata por disvolvo de WeChat)
- **Enkonstruita administra panelo**: ĵetono-administrado, tunelaj agordoj, retaj klientoj, sistemaj agordoj kaj reviziaj protokoloj
- Male al aliaj penetradaj iloj, FastTunnel celas esti facile etendebla kaj facile prizorgebla kadro. Vi povas konstrui vian propran penetradan aplikaĵon per la NuGet-pakaĵo `FastTunnel.Core`.

> ⚠️ Kiam vi elmetas pordon 3389 (fora labortablo), certigu, ke via sistempasvorto estas sufiĉe forta por preventi neaŭtorizitan aliron.

## Trajtoj

- [x] Fora aliro al komputiloj en interna reto (Windows / Linux / Mac)
- [x] Aliro al internaj retpaĝaj servoj per propra domajno / subdomajno
- [x] TCP / UDP porda plusendado (dudirekta UDP-datagrama plusendo)
- [x] Pluraj domajnoj ligitaj al internaj servoj
- [x] Aŭtentigo de klientoj per ĵetono (ĵetonoj kreiĝas en la administra panelo; neaŭtorizitaj ĵetonoj estas rifuzitaj)
- [x] Servilo administras tunelajn agordojn: nula agordo ĉe la kliento, ĉio administrata en la panelo, ŝanĝoj efikas tuj
- [x] Klienta medi-raporto (sistemo / CPU / memoro / .NET-versio)
- [x] Vida sistemagordado (radika domajno, plusenda ŝaltilo, JWT ktp.; varmŝargiĝas post konservo)
- [x] Operaciaj reviziaj protokoloj
- [ ] p2p penetrado

## Arkitekturo

```mermaid
flowchart TB
    subgraph Public[Publika Reto]
        User[Publika Uzanto]
    end

    subgraph Server[Servilo · Publika IP]
        Listener[Pordaj Aŭskultantoj<br/>TCP / UDP Plusendo]
        Route[YARP Domajnaj Itineroj<br/>TTT-Tuneloj]
        API[Administra API + Administra UI]
        DB[(SQLite<br/>Ĵetonoj / Tunelaj Agordoj / Reviziaj Protokoloj)]
    end

    subgraph Intranet[Interna Reto]
        Client[FastTunnel Kliento]
        MySQL[(MySQL)]
        Redis[(Redis)]
        Web[Interna Retpaĝo]
    end

    User -->|Aliro per IP:Porto| Listener
    User -->|Aliro per subdomajno| Route
    Listener <-->|WebSocket-Tunelo| Client
    Route <-->|WebSocket-Tunelo| Client
    Client --> MySQL
    Client --> Redis
    Client --> Web
    API --> DB
    Listener --> DB
    Route --> DB
```

**Ensaluta kaj Agordo-Livera Fluado**

```mermaid
sequenceDiagram
    participant Admin as Administra Panelo
    participant API as Servilo
    participant DB as Datumbazo
    participant Client as Interna Kliento
    participant Svc as Interna Servo

    Admin->>API: Kreu ĵetonon / agordu tunelojn
    API->>DB: Konservu (ĵetono, porda plusendo, TTT-tuneloj)
    Client->>API: Konektiĝu al la servilo (kun ĵetono)
    API->>DB: Kontrolu ĵetonon, legu tunelajn agordojn
    API->>API: Kreu pordajn aŭskultantojn (TCP/UDP) kaj domajnajn itinerojn
    API-->>Client: Liveru la tunelan agordaron
    Note over Client: La kliento tenas la agordaron kaj konektiĝas al internaj servoj kiam plusendaj instrukcioj alvenas
    User->>API: Aliru servilan pordon / subdomajnon
    API-->>Client: Plusenda instrukcio (kun interna adreso)
    Client->>Svc: Konektiĝu al la interna servo kaj pontigu datumojn
```

| Projekto | Priskribo |
|---|---|
| `FastTunnel.Server` | Servilo (uzigita sur maŝino kun publika IP), gastigas administran API-on kaj administran UI-on |
| `FastTunnel.Client` | Kliento (uzigita sur internaj maŝinoj), aktive konektiĝas al la servilo |
| `FastTunnel.Core` | Kerno-kadra biblioteko (publikigita al NuGet por sekundara disvolvo) |
| `FastTunnel.Core.Client` | Klienta kerno-biblioteko |
| `FastTunnel.Api` | Administra API (ĵetonoj, tuneloj, retaj klientoj, sistemaj agordoj, reviziaj protokoloj) |
| `FastTunnel.Admin` | Administra UI (Vue 3 + Arco Design) |

## Ekrankopioj

**Ĉefpanelo**

![Ĉefpanelo](images/screenshots/dashboard.png)

**Ĵetonoj** (kreiĝas en la administra panelo; klientoj devas porti validan ĵetonon por ensaluti)

![Ĵetonoj](images/screenshots/tokens.png)

**TTT-Tuneloj**

![TTT-Tuneloj](images/screenshots/web-tunnels.png)

**Porda Plusendado** (TCP / UDP)

![Porda Plusendado](images/screenshots/forward-tunnels.png)

**Retaj Klientoj** (aktivaj konektoj kun medi-informo)

![Retaj Klientoj](images/screenshots/clients.png)

**Reviziaj Protokoloj**

![Reviziaj Protokoloj](images/screenshots/audit-logs.png)

**Sistemaj Agordoj**

![Sistemaj Agordoj](images/screenshots/settings.png)

## Rapida Komenco

### 1. Uzigu la servilon

```bash
# Disvolvo (aŭskultas je http://*:1270 defaŭlte)
dotnet run --project FastTunnel.Server

# Aŭ publikigu
./publish.sh
```

Servilaj datumoj estas konservitaj en `data/fasttunnel.db` (SQLite, sub la aplikaĵa dosierujo: kontoj, ĵetonoj, tunelaj agordoj, reviziaj protokoloj).

### 2. Inicializu la administran panelon

Malfermu `http://servila-ip:1270` en retumilo. Unuafoje la agorda paĝo kreas la administran konton (TOTP-dufaktora kontrolo subtenata).

### 3. Kreu ĵetonon kaj tunelojn

1. **Ĵetonoj** → kredu ĵetonon (ekz. `ft-demo-token`)
2. **TTT-Tuneloj** → kredu (subdomajno + interna servo-adreso, ligita al ĵetono)
3. **Porda Plusendado** → kredu (fora pordo + interna adreso + TCP/UDP-protokolo, ligita al ĵetono)

Tunelaj ŝanĝoj efikas tuj; se la cela kliento estas eksterreta, la agordo aplikiĝas aŭtomate kiam la kliento ensalutas.

### 4. Agordu kaj startigu la klienton

Nur tri agordoj necesas en `FastTunnel.Client/appsettings.json`:

```json
{
  "FastTunnel": {
    "Server": {
      "ServerAddr": "servila-ip-aŭ-domajno",
      "ServerPort": 1270
    },
    "Token": "ft-demo-token"
  }
}
```

```bash
dotnet run --project FastTunnel.Client
```

Post konekto, la servilo aŭtomate kreas pordajn aŭskultantojn kaj domajnajn itinerojn laŭ la administraj agordoj. Aliru internajn servojn per `servila-ip:fora-pordo` aŭ `subdomajno.radika-domajno:1270`.

### 5. Sistemaj agordoj

La paĝo **Sistemaj Agordoj** administras servilajn parametrojn (varmŝargiĝas post konservo):

- **Ŝalti pordan plusendadon**: kiam malŝaltita, la servilo ĉesas trakti pordan plusendadon
- **Radika domajno**: subdomajna sufikso por TTT-tuneloj (ekz. `test.cc`)
- **JWT-aŭtentigo**: parametroj de la ensaluta ĵetono de la administra panelo (ŝanĝoj postulas servilan rekomencon)

## Sekurecaj Notoj

- La defaŭlta JWT-subskriba ŝlosilo estas enkonstruita defaŭlto; ŝanĝu ĝin en Sistemaj Agordoj por produktado
- Ĵetonoj kaj tunelaj agordoj estas konservitaj en la servila datumbazo; protektu servilan aliron
- Uzu fortajn pasvortojn kiam vi elmetas pordojn kiel 3389 / 22

## Permesilo

Apache License 2.0
