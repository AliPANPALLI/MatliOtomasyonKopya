# MatliOtomasyon

> Bu README dönüşüm sürecinde adım adım tamamlanıyor. Tam yapı (mimari, servis haritası, kurulum, loglama, kodlama standartları) Faz 1 / Adım 1.1'de eklenecek.

## Klasör yapısı

```
MatliOtomasyon/
├── MatliOtomasyon.sln
├── Gateways/
│   └── MatliOtomasyon.Gateway   Tek dış giriş noktası (Ocelot); Faz 7'de YARP'a geçecek
├── Identity/                 Kimlik doğrulama (Faz 4'te tek proje MatliOtomasyon.Identity olacak)
│   ├── Authentication.API
│   ├── Authentication.Application
│   ├── Authentication.Domain
│   └── Authentication.Infrastructure
├── Services/                 Her bounded context kendi klasöründe, 4 katmanlı
│   └── <Service>/
│       ├── <Service>.API
│       ├── <Service>.Application
│       ├── <Service>.Domain
│       └── <Service>.Infrastructure
├── Workers/                  Her arka plan worker'ı kendi klasöründe, 3 katmanlı
│   └── <Name>/
│       ├── <Name>.Worker            Host: Program.cs, BackgroundService döngüleri, appsettings
│       ├── <Name>.Application       Models/ (Node-RED/SAP veri modelleri)
│       └── <Name>.Infrastructure    Caching/, Sap/, Persistence/; veri erişim paketleri ve diğer servislere referanslar
└── tests/
```

Worker katman kuralları:

- `.Worker` sadece kendi `.Application` ve `.Infrastructure` projelerine referans verir. Veri erişim paketleri (EF Core, SqlClient, FastMember, Npgsql, RabbitMQ, Redis) ve diğer servislerin projelerine giden referanslar `.Infrastructure`'dadır.
- `.Application` hiçbir projeye referans vermez.
- Worker'lar birbirine referans vermez.
- Bazı worker'larda (AlarmNotification, EmailDispatch, SmsDispatch, VoiceCallDispatch, EnergyMonitoringAlarm, HegCoolingAlarm, PoultryHouseEnergyLog, ServiceHealthMonitor) `.Application` ve `.Infrastructure` şimdilik boş iskelet; iş mantığı `BackgroundService` sınıflarından ayrıştırıldıkça dolacak.

Klasör adları, proje adları ve namespace'ler İngilizcedir; özel adlar (Matli, Kayislar1, Rahmiye, VerimGida, SAP, Netgsm, ITG, HEG...) olduğu gibi kalır. Solution klasörleri disk yapısıyla birebir aynıdır.

| Servis | Eski adı | Sorumluluk |
|---|---|---|
| `PoultryHouseScada` | KumesScada | Kümes SCADA izleme, raporlar, nekropsi, yumurtlama |
| `WaterLineScada` | SuHatScada | Su hattı/su kaynağı SCADA izleme ve çizimler |
| `EnergyMonitoring` | EnerjiIzleme | Enerji izleme ve raporları |
| `PanelIdentification` | PanoKimliklendirme | Pano, makine, bölüm kimliklendirme ve onay süreçleri |
| `SharedApp` | SharedApp | Ortak tanımlar: lokasyon, şirket, form/menü, yetkiler, alarm logları, bildirim takibi |
| `Ticket` | Ticket | Ticket yönetimi |
| `WeighbridgeTracking` | KantarTakip | Kantar takibi ve SAP kantar entegrasyonu |
| `HegCoolingScada` | HegSogutmaScada | HEG soğutma SCADA ve alarmları |
| `MachineMonitoring` | MakineIzleme | Makine izleme |
| `ObjectMonitoring` | NesneIzleme | Nesne izleme |
| `EnvironmentMonitoringScada` | OrtamIzlemeScada | Ortam izleme SCADA |
| `ProjectTracking` | ProjeTakip | Proje takibi |
| `SiloTemperatureMonitoring` | SiloSicaklikIzleme | Silo sıcaklık izleme |
| `MobileUpdate` | MobilGuncelleme | Mobil uygulama sürüm dağıtımı |
| `ITG` | ITG | ITG Edge sıcaklık toplama (kendi iç katman yapısıyla) |

Gateway route'ları (`/kumesscada/...` gibi) ve veritabanı adları (`OTM_KumesScada` gibi) bu adımda değişmedi; frontend etkilenmez.

| Worker | Eski adı | Görevi |
|---|---|---|
| `AlarmNotification` | AlarmWorker | Kümes, su hattı ve enerji alarmlarını değerlendirip bildirir |
| `EnergyMonitoringAlarm` | EnerjiIzlemeAlarmWorker | Enerji izleme alarmları |
| `HegCoolingAlarm` | HegSogutmaAlarmWorker | HEG soğutma alarmlarını değerlendirip bildirir |
| `Kayislar1`, `Kuyucak1`, `Osmancali1` | Kayislar1Worker, Kuyucak1Worker, Osmancali1Worker | Tesis kümeslerinin Node-RED/PLC verisini okur |
| `Kayislar1Log`, `Kuyucak1Log`, `Osmancali1Log` | Kayislar1LogWorker, Kuyucak1LogWorker, Osmancali1LogWorker | Tesis kümes verisini periyodik log olarak kaydeder |
| `RahmiyeFacility`, `RahmiyeFacilityLog` | RahmiyeTesisWorker, RahmiyeTesisLogWorker | Rahmiye tesisinin canlı verisi ve log kaydı |
| `RahmiyeYtk`, `VerimGidaYtk` | RahmiyeYTKWorker, VerimGidaYTKWorker | YTK rapor verisi |
| `VerimGidaPiPoultryHouse`, `VerimGidaPiPoultryHouseLog` | VerimGidaPiKumesWorker, VerimGidaPiKumesLogWorker | VerimGida kümes verisi ve log kaydı |
| `WaterMonitoring`, `WaterMonitoringLog` | SuIzlemeWorker, SuIzlemeLogWorker | Su izleme lokasyonlarının canlı verisi ve log kaydı |
| `PoultryHouseEnergyLog` | KumesEnerjiLogKayitWorker | Kümes enerji tüketimi log kaydı |
| `EmailDispatch`, `SmsDispatch`, `VoiceCallDispatch` | MailGonderimWorker, SmsGonderimWorker, MesajGonderimWorker | Bekleyen e-posta, SMS ve sesli arama bildirimlerini gönderir |
| `SapEmployeeSync` | SapPersonelWorker | SAP personel listesinden kullanıcıların sicil numarasını eşler |
| `ServiceHealthMonitor` | ServisKontrolWorker | Worker'ların çalışma durumunu kontrol eder |
| `ItgTemperatureIngestion` | Servisler/ItgTemperatureIngestionWorker | ITG sıcaklık telemetrisini RabbitMQ'dan alıp kaydeder |

Worker'ların DB'ye yazdığı servis durum adları (örneğin `"SuIzlemeWorker"`) ve `UserSecretsId` değerleri bilerek eski haliyle bırakıldı: servis izleme ekranı bu adlara bakıyor, yerel secret'lar da bu kimliklere bağlı.

**Sıradaki taşıma:** `SharedKernel`, `BuildingBlocks/MatliOtomasyon.Shared` altında 4 katmana (Domain, Application, Infrastructure, Web) bölünecek (Adım 1.8).

## Güvenlik notları

### Authentication API yetkilendirme kuralları

Authentication API'de yetki işaretlenmemiş her uç kimlik doğrulaması ister (fallback policy). Kimlik doğrulaması olmadan çağrılabilen uçlar açıkça `[AllowAnonymous]` ile işaretlidir ve listesi testlerle sabitlenmiştir:

| Uç | Neden anonim |
|---|---|
| `SignIn`, `VerifyOtp` | Giriş akışı |
| `NewToken` | Süresi dolmuş token ile yenileme; imza ayrıca doğrulanır |
| `ResetPassword`, `CompletePasswordReset` | Şifre sıfırlama akışı |
| `tab-close` | Tarayıcı sekme kapanırken header gönderemez; gövdedeki token'ın imzası doğrulanır |
| `heartbeat`, `page-visit` | Süresi dolmuş istemciler de gönderebilir; kimliği doğrulanmamış istekler yok sayılır |

Kullanıcı yönetimi uçları SharedApp'teki **Kullanıcı Yönetimi** formunun yetkisine bağlıdır. Yetki, isteği yapan kullanıcının kendi token'ıyla SharedApp'ten okunur ve 60 saniye bellekte tutulur.

| Politika | Uçlar | Gereken yetki |
|---|---|---|
| `UserManagement.Write` | `CreateUser`, `EditUser`, `DeleteUser`, `SendEmailForNewUser`, `SendSmsForNewUser` | Formda Yazma |
| `UserManagement.Read` | `GetAllUserConnectSiteLogs`, `GetAllvwPageVisits` | Forma erişim |
| `UserManagement.ReadOrInternalService` | `GetAllUsers` | Forma erişim veya iç servis anahtarı |

`GetUser` ile kullanıcı kendi kaydını her zaman görebilir. Başka bir kullanıcının kaydı için `UserManagement.Read` gerekir.

### Gerekli ayarlar

| Ayar | Nerede | Açıklama |
|---|---|---|
| `UserManagement__FormLink` | Authentication API | Kullanıcı Yönetimi sayfasının Angular route'u (SharedApp'teki ModulLink + FormLink). Boşsa kullanıcı yönetimi uçları herkese kapalıdır. |
| `InternalServices__AuthenticationApiKey` | Authentication API **ve** SharedApp API | Aynı değer iki serviste de tanımlı olmalı. SharedApp kullanıcı listesini bu anahtarla çeker. Boşsa iç servis erişimi kapalıdır. |

İç servis anahtarı geçici bir çözümdür; Faz 7'de OpenIddict `client_credentials` akışıyla değiştirilecek.

### Testler

```powershell
dotnet build MatliOtomasyon.sln
dotnet run --project tests\Authentication.SecurityChecks\Authentication.SecurityChecks.csproj --no-build
```

Makinede .NET 9 runtime yoksa, aynı oturumda önce `$env:DOTNET_ROLL_FORWARD = 'Major'` ayarlanarak testler daha yeni bir runtime'da çalıştırılabilir.
