# Profil bölüm ve sınıf bilgileri

`GET /api/profil` ve `PUT /api/profil` oturum açmış kullanıcının `bolum` ve
`sinif` alanlarını döndürür. Mobil modelle uyumlu olarak ikisi de opsiyonel metindir.
Bölüm en fazla 200, sınıf en fazla 30 karakter olabilir (ör. `Hazırlık`, `4. Sınıf`).

```json
{
  "adSoyad": "Berna Kalkan",
  "bolum": "Bilgisayar Mühendisliği",
  "sinif": "4. Sınıf"
}
```

PUT sırasında gönderilmeyen veya `null` olan alanlar mevcut değerlerini korur.
Yalnız `adSoyad` gönderen eski istemciler çalışmaya devam eder. Boş metin `""`
göndermek ilgili metni temizler. Başta ve sonda kalan boşluklar kırpılır.
İlk profil oluşturulurken gönderilmeyen alanlar veritabanında `NULL` olur.
Rol, kullanıcı kimliği ve oluşturulma tarihi mevcut profil güncellenirken korunur.

## Veritabanını güncelleme

API'nin yeni sürümünü çalıştırmadan önce, Supabase bağlantısı tanımlı ortamda
repo kökünden migration uygulanmalıdır:

```sh
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet ef database update --project src/FixCampus.Web
```

`20261029150000_AddProfileEducation` yalnız iki nullable sütun ekler; mevcut kayıtları değiştirmez.
Kayıt endpoint'i bu değişikliğin kapsamında değildir. Kayıt/giriş sonrası PUT ile bilgiler saklanabilir.
Flutter gönderme tarafını Berke kontrol edecektir.

## Kontroller

```sh
dotnet run --project tests/FixCampus.DatabaseChecks -- --model-only
```

Gerçek GET/PUT ve veri kalıcılığı kontrolleri için boş `fixcampus_test` PostgreSQL
veritabanına işaret eden `FIXCAMPUS_TEST_DATABASE` değişkenini ayarlayın:

```sh
dotnet run --project tests/FixCampus.DatabaseChecks
```

Bu test migration, GET/PUT, eski istemci, tek alan güncelleme, null/boş metin,
yeni profil ve kullanıcılar arası ayrımı kontrol eder; ortak Supabase veritabanında çalıştırılmaz.
