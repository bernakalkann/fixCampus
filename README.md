# fixCampus

Kampüs öğrencilerinin sosyal buluşma ve kayıp-buluntu ilanları paylaşabileceği web ve mobil platform projesi.

## Mevcut durum

ASP.NET Core API iskeleti, uygulama durum endpointi, örnek ilan yanıtı ve Swagger/OpenAPI hazırdır. Web arayüzü, Flutter mobil uygulaması ve veritabanı entegrasyonu geliştirme aşamalarında eklenecektir.

Örnek ilanlar sabit deneme verileridir; veritabanına kaydedilmez. Bu sürümü çalıştırmak için Supabase hesabı veya anahtar gerekmez.

## Gereksinim

.NET 10 SDK. `global.json`, kurulu 10.0 SDK sürümleri içinde uygun sürüme geçişe izin verir.

## Çalıştırma

Depo kökünde:

```powershell
dotnet restore fixCampus.slnx
dotnet build fixCampus.slnx --no-restore
dotnet run --project src/FixCampus.Web --launch-profile http
```

Uygulama `http://localhost:5168` adresinde çalışır.

## API

| Adres | Açıklama |
|---|---|
| [GET /api/durum](http://localhost:5168/api/durum) | Uygulamanın çalıştığını bildirir; veritabanı bağlantı testi yapmaz. |
| [GET /api/ornek/ilanlar](http://localhost:5168/api/ornek/ilanlar) | Sosyal buluşma, kayıp ve buluntu için üç örnek kayıt döndürür. |
| [/swagger](http://localhost:5168/swagger) | Etkileşimli API ekranı. |
| [/openapi/v1.json](http://localhost:5168/openapi/v1.json) | OpenAPI belgesi. |

Swagger ve OpenAPI yalnız `Development` ortamında açılır. Yerel `http` profili bu ortamı ayarlar.

PowerShell ile kontrol:

```powershell
Invoke-RestMethod http://localhost:5168/api/durum
Invoke-RestMethod http://localhost:5168/api/ornek/ilanlar
```

Örnek ilan yanıtında `dataSource` değeri `sample`, `isPersisted` değeri `false` olur. Kayıtlar `items` listesinde bulunur. Bu endpoint yalnız GET kabul eder.

## Proje yapısı

```text
src/FixCampus.Web/
  Program.cs                     Uygulama ve Swagger kurulumu
  Controllers/Api/               HTTP endpointleri
  Contracts/                     İstemciye dönen yanıt modelleri
  Properties/launchSettings.json  Yerel çalıştırma profili
```
