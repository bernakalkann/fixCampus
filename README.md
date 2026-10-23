# fixCampus

Kampüs İlan Platformu, sosyal buluşma ve kayıp-buluntu ilanları için ASP.NET Core MVC ve API projesidir. Bu depo, plan belgesindeki görevleri küçük ve çalışan adımlarla geliştirmek için hazırlanmıştır.

## Mevcut adım

23 Ekim 2026 itibarıyla Zülfikar'ın 1. hafta API kod görevi hazırdır: durum endpoint'i, web/mobil bağlantı denemesi için üç örnek ilan ve Swagger/OpenAPI. Çalıştırma kontrollerinin durumu [1. hafta çalışma notunda](docs/hafta-01-api-calisma-notu.md) tutulur.

Örnek ilanlar bellekte sabittir; veritabanına kaydedilmez. Veri modeli, Supabase bağlantısı, migration, kullanıcı oturumu, MVC ekranları ve gerçek ilan CRUD işlemleri bu sürümde yoktur. Berna'nın veri modeli ve Berke'nin web iskeletiyle ortak entegrasyon yapılacaktır. Liderlik videosu ve Drive yüklemesi henüz yapılmış olarak kaydedilmemiştir; tüm ilk haftanın tamamlandığı iddia edilmez.

## Çalıştırma

.NET 10 SDK gerekir. `global.json`, kurulu 10.0 SDK sürümleri içinde uygun sürüme geçişe izin verir. Ekip aynı ana sürümü kullanmalıdır.

Depo kökünde:

```powershell
dotnet restore fixCampus.slnx
dotnet build fixCampus.slnx --no-restore
dotnet run --project src/FixCampus.Web --launch-profile http
```

- [Swagger ekranı](http://localhost:5168/swagger)
- [API durum yanıtı](http://localhost:5168/api/durum)
- [Örnek ilan yanıtı](http://localhost:5168/api/ornek/ilanlar)
- [OpenAPI belgesi](http://localhost:5168/openapi/v1.json)

Swagger ve OpenAPI yalnız `Development` ortamında açılır. Yerel `http` profili bu ortamı ayarlar. Bu adım için Supabase hesabı veya anahtar gerekmez.

PowerShell ile kontrol:

```powershell
Invoke-RestMethod http://localhost:5168/api/durum
```

Beklenen yanıtta `application` değeri `fixCampus`, `status` değeri `ok`, `stage` değeri `api-skeleton` olur. `checkedAtUtc` her istekte güncellenen UTC zamandır.

Örnek ilanlar için:

```powershell
Invoke-RestMethod http://localhost:5168/api/ornek/ilanlar
```

`dataSource` değeri `sample`, `isPersisted` değeri `false`; `items` içinde sosyal buluşma, kayıp ve buluntu için üç örnek bulunur. Yanıtı ekrana bağlamak için [API entegrasyon adımlarını](docs/api-entegrasyonu.md) izleyin. Bu yanıt nihai ilan API'si veya veritabanı modeli değildir.

## Dosyalar

```text
src/FixCampus.Web/
  Program.cs                    Uygulama ve Swagger kurulumu
  Controllers/Api/              HTTP endpointleri
  Contracts/                    İstemciye dönen yanıt modelleri
  Properties/launchSettings.json Yerel çalıştırma profili
docs/
  api-entegrasyonu.md           Rotalar, örnek JSON ve bağlantı denemesi
  hafta-01-api-calisma-notu.md   Gerçekleşen çalışma ve video anlatım taslağı
  gorevler-ve-commit-plani.md    Haftalara göre küçük geliştirme adımları
  arastirma-notlari.md          Teknik kararlar ve resmi kaynaklar
```

Berke'nin MVC ekranları aynı Web projesine, Berna'nın veri modeli ve CRUD çalışmaları API ile ortak kullanılacak katmanlara eklenebilir. İhtiyaç doğdukça klasör ve servis eklenir.

## Çalışma düzeni

Her küçük iş tamamlanıp kontrol edildikten sonra kendi hesabıyla commit edilir. Commit mesajı, yapılan gerçek değişikliği açıklar. Haftalık ilerleme için [görev ve commit planına](docs/gorevler-ve-commit-plani.md) bakın.

Ekip deposu: [bernakalkann/fixCampus](https://github.com/bernakalkann/fixCampus). Bu adımın GitHub'a gönderim durumu çalışma notunda ayrıca belirtilir.

Veritabanı şifresi ve gizli Supabase anahtarları depoya eklenmez. Bağlantı aşamasında .NET User Secrets veya ortam değişkenleri kullanılacaktır. `bin` ve `obj` gibi derleme çıktıları `.gitignore` ile dışarıda tutulur.
