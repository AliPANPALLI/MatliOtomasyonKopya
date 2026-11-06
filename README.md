# MatliOtomasyon

> Bu README dönüşüm sürecinde adım adım tamamlanıyor. Tam yapı (mimari, servis haritası, kurulum, loglama, kodlama standartları) Faz 1 / Adım 1.1'de eklenecek.

## Klasör yapısı

```
MatliOtomasyon/
├── MatliOtomasyon.sln
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
└── tests/
```

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

**Sıradaki taşımalar:** worker'lar `Workers/<Name>/` altına (Adım 1.5–1.6), gateway `Gateways/MatliOtomasyon.Gateway`'e (Adım 1.7), `SharedKernel` ise `BuildingBlocks/MatliOtomasyon.Shared` altında 4 katmana (Adım 1.8) taşınacak.

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
