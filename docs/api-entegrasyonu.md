# İlk hafta API bağlantı denemesi

23 Ekim 2026 sürümü, web ve mobil geliştirmesine başlanırken aynı örnek JSON yanıtının alınmasını sağlar. Yanıt bellekte hazırlanır; veri modeli ve gerçek ilan API'si Berna'nın çalışmasıyla ayrıca netleşecektir.

## Çalıştırma ve rotalar

.NET 10 SDK kurulu bilgisayarda depo kökünden:

```powershell
dotnet restore fixCampus.slnx
dotnet build fixCampus.slnx --no-restore
dotnet run --project src/FixCampus.Web --launch-profile http
```

Yerel adres `http://localhost:5168` olur.

| Rota | Sonuç |
|---|---|
| `GET /api/durum` | Uygulamanın yanıt verdiğini ve kontrolün UTC zamanını bildirir; veritabanını denetlemez. |
| `GET /api/ornek/ilanlar` | Üç sabit örnek ilanı JSON olarak döndürür. Kayıt eklemez. |
| `GET /swagger` | Development ortamındaki etkileşimli API ekranı. |
| `GET /openapi/v1.json` | Development ortamındaki OpenAPI belgesi. |

Bu adımda giriş veya Supabase anahtarı gerekmez. Örnek endpoint yalnız GET sağlar; gerçek oluşturma, güncelleme ve silme işlemleri eklenmemiştir.

## Tam örnek yanıt

`GET http://localhost:5168/api/ornek/ilanlar`:

```json
{
  "dataSource": "sample",
  "isPersisted": false,
  "items": [
    {
      "id": "11111111-1111-4111-8111-111111111111",
      "type": "social",
      "title": "[Örnek] Kütüphanede çalışma grubu",
      "description": "API bağlantısını denemek için hazırlanmış örnek sosyal buluşma ilanı.",
      "location": "Örnek kampüs kütüphanesi",
      "startsAtUtc": "2026-10-25T11:00:00+00:00"
    },
    {
      "id": "22222222-2222-4222-8222-222222222222",
      "type": "lost",
      "title": "[Örnek] Kayıp su matarası",
      "description": "Kayıp ilanı kartını denemek için hazırlanmış örnek kayıt.",
      "location": "Örnek kampüs yemekhanesi",
      "startsAtUtc": null
    },
    {
      "id": "33333333-3333-4333-8333-333333333333",
      "type": "found",
      "title": "[Örnek] Bulunan defter",
      "description": "Buluntu ilanı kartını denemek için hazırlanmış örnek kayıt.",
      "location": "Örnek öğrenci merkezi",
      "startsAtUtc": null
    }
  ]
}
```

| Alan | Anlamı |
|---|---|
| `dataSource`, `isPersisted` | Verinin örnek olduğunu ve kalıcı olarak kaydedilmediğini belirtir. |
| `items` | Kartlara bağlanacak kayıt listesi. |
| `id` | Bu örnek için sabit UUID; veritabanında kayıt bulunduğu anlamına gelmez. |
| `type` | `social`: sosyal buluşma; `lost`: kayıp; `found`: buluntu. |
| `title`, `description`, `location` | Kart başlığı, açıklaması ve metin olarak yeri. |
| `startsAtUtc` | Buluşma zamanı; kayıp ve buluntu örneklerinde `null`. |

Buluşma tarihi sabit bir örnektir: 25 Ekim 2026 saat 11.00 UTC, İstanbul'da saat 14.00. Tarih geçse de bu örnek yanıt değişmez. Aktif akıştan süreye göre gizleme, 5. haftanın ayrı görevidir.

## Yanıtı ekrana bağlama

1. Örnek URL'ye GET isteği gönderip yanıtı JSON olarak çöz.
2. Üst nesnenin `items` listesini kartlarda göster; yanıtın kendisini doğrudan liste sayma.
3. Her kartta `title`, `description`, `location` ve tür etiketini göster. `startsAtUtc` doluysa tarih olarak çözümleyip ekranın saat dilimine dönüştür; `null` olduğunda tarih alanını gösterme.
4. Deneme ekranında verinin örnek olduğunu belirt. HTTP hatasını başarı yanıtı gibi işlememe davranışını bağlantı sırasında kontrol et.

Aynı bilgisayardaki sunucu taraflı MVC denemesi bu yerel URL'ye GET isteği gönderebilir. MVC ekranının hazırlanması Berke'nin görevidir; bu sürümde ekran veya Flutter istemcisi eklenmemiştir.

`localhost`, isteği gönderen cihazın kendisini ifade eder. Buradaki adres uygulamanın çalıştığı bilgisayar içindir. Android Emulator, bilgisayarın loopback ağına `10.0.2.2` özel adresiyle erişebilir; mobil denemede temel adres `http://10.0.2.2:5168` olur. [Android'in resmi ağ belgesi](https://developer.android.com/studio/run/emulator-networking-address) bu adresi açıklar. `appsettings.Development.json`, bu Host değerini yalnız geliştirme ortamında kabul eder. Yerel HTTP kontrolünde `Host: 10.0.2.2:5168` ile 200, tanımsız Host değeriyle 400 alındı. Flutter emülatöründen bağlantı henüz denenmedi; mobil uygulamanın ağ ve yerel HTTP ayarları bağlantı sırasında doğrulanmalıdır. Fiziksel cihaz bağlantısı ayrıca yapılandırılır.

CORS politikası eklenmemiştir; farklı origin üzerinden çalışan tarayıcı arayüzü için ihtiyaç doğduğunda izin verilecek origin birlikte belirlenir.
