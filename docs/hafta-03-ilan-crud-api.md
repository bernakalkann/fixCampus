# 3. hafta — Berna Kalkan: İlan CRUD API

Plan belgesindeki Berna görevi, .NET Web API üzerinde ilan oluşturma, listeleme,
detay, güncelleme ve silmedir. İlan web sayfaları Berke'nin; haftanın doğrulama
çalışması ve liderliği Zülfikar'ın sorumluluğundadır. Bu değişiklik API'yi mevcut
veritabanı ve Supabase Bearer/uygulama rolü kontrollerine bağlar.

## Uçlar

Tüm uçlar `Authorization: Bearer <access_token>` ister. Token `/api/auth/giris`
yanıtından alınır. Kullanıcının `Profiller` kaydında `Ogrenci` veya `Yonetici`
rolü bulunmalıdır. MVC çerezi API için tek başına yeterli değildir.

| İşlem | Uç | Başarılı yanıt |
| --- | --- | --- |
| Kategori seçenekleri | GET `/api/kategoriler` | 200, kimlik/ad/tür listesi |
| İlanları listele | GET `/api/ilanlar` | 200, ilan listesi (boşsa `[]`) |
| İlan detayı | GET `/api/ilanlar/{id}` | 200, ilan |
| İlan oluştur | POST `/api/ilanlar` | 201, ilan ve Location başlığı |
| İlan güncelle | PUT `/api/ilanlar/{id}` | 200, güncellenen ilan |
| İlan sil | DELETE `/api/ilanlar/{id}` | 204, boş gövde |

POST ve PUT aynı gövdeyi kullanır. PUT bütün düzenlenebilir alanları ister:

```json
{
  "baslik": "Kütüphanede kayıp anahtarlık",
  "aciklama": "Mavi anahtarlığımı arıyorum.",
  "kategoriId": "GET /api/kategoriler yanıtındaki Kayip türünden UUID",
  "tur": "Kayip",
  "yer": "Merkez kütüphane",
  "bulusmaTarihi": null
}
```

`kategoriId` açıklama metni yerine gerçek UUID ile değiştirilmelidir.
`Sosyal` ilanında aynı türde bir kategori ve saat dilimi içeren tarih gönderilir
(ör. `2030-01-01T15:00:00+03:00`). Tarih UTC'ye dönüştürülerek saklanır.
`Kayip` ve `Buluntu` türlerinde tarih null olmalıdır.

## Kurallar

- Başlık 2–150, açıklama 1–3000, yer 1–200 karakterdir; boş metin reddedilir.
  Metinlerin başındaki ve sonundaki boşluklar kaydetmeden önce temizlenir.
- Tür yalnız `Sosyal`, `Kayip`, `Buluntu` olabilir. Kategori bulunmalı ve türü eşleşmelidir.
- İlan sahibi token kimliğinden, kimlik ve oluşturulma tarihi sunucudan alınır;
  yeni ilan `Aktif` olur. İstek DTO'su sahip, durum veya oluşturulma tarihi kabul etmez.
- Güncelleme ve silme yalnız ilan sahibine açıktır; başka hesap 403 alır.
  Yönetici rolü bu uçlarda sahiplik kontrolünü atlamaz.
- Olmayan ve `Gizli` ilanlar 404 döndürür. Gizli ilanlar listelenmez.
- Liste en yeni kayıttan eskiye sıralanır. Arama, filtre, sayfalama ve İlanlarım
  4. hafta kapsamındadır. Süresi geçen buluşmaları ayıklama 5. hafta kapsamındadır.
- Silme mevcut veritabanı cascade ilişkileriyle katılım, fotoğraf kaydı ve şikayetleri
  de siler. Storage dosyasının silinmesi fotoğraf API'sinin ileriki kapsamıdır.
- Geçersiz gövde 400 `ValidationProblemDetails`, oturumsuz istek 401,
  uygulama rolü olmayan hesap 403 alır. Mevcut yetki altyapısı bağlantı
  doğrulama sorunu için 503 döndürür.

Mevcut tablo modeli yeterli olduğundan yeni migration eklenmedi. Önceki migration'lar
uygulanmış olmalıdır. `docs/hafta-03-kategoriler.sql`, eksik ilan türleri için en az
bir kategori sağlar; tekrar çalıştırılabilir ve mevcut kategorileri değiştirmez.
API kendiliğinden deneme verisi oluşturmaz.

## Swagger ile iki hesaplı kontrol

1. Uygulamayı Development ortamında çalıştırıp `/swagger` adresini açın.
2. İlk hesapla `/api/auth/giris` üzerinden giriş yapıp `access_token` değerini Authorize'a girin.
3. GET `/api/kategoriler` ile uygun kategori kimliğini alın, POST ile ilan oluşturun.
4. Dönen kimlikle liste ve detaydan kaydı okuyun; PUT ile başlığı değiştirin.
5. İkinci hesapla token değiştirip aynı ilana PUT ve DELETE isteği gönderin: 403 beklenir.
6. İlk hesaba dönüp DELETE gönderin: 204; sonra detay: 404 beklenir.
7. Eksik başlık, olmayan kategori, kategori/tür uyuşmazlığı ve tarihsiz sosyal ilanı deneyin: 400 beklenir.

## Otomatik kontroller ve doğrulama durumu

```sh
dotnet build fixCampus.slnx
dotnet run --project tests/FixCampus.AuthChecks
dotnet run --project tests/FixCampus.AuthorizationChecks -- --in-process
dotnet run --project tests/FixCampus.DatabaseChecks -- --model-only
```

1 Kasım 2026 doğrulaması: derleme 0 hata/0 uyarıyla geçti; mevcut Auth kontrolleri,
88 HTTP yetki/OpenAPI kontrolü, ilan DTO kuralları, model/snapshot ve PostgreSQL
migration/rollback SQL kontrolleri geçti. `--in-process`, dış servisleri test
taklitleriyle değiştirir ve gerçek ASP.NET middleware/MVC hattını TCP portu açmadan
çalıştırır; canlı Supabase erişiminin yerine geçmez.

Canlı proje üzerinde salt okunur bağlantı/migration kontrolü:

```sh
dotnet run --project tests/FixCampus.DatabaseChecks -- --live-read-only
```

Bu seçenek mevcut User Secrets ayarlarını kullanır, yalnızca sayıları ve migration
adlarını yazdırır; bağlantı şifresini veya profil bilgilerini yazdırmaz.

1 Kasım 2026 canlı Supabase panel kontrolü: InitialCreate ve AddProfileEducation
migration'ları uygulanmış; 20 profil ve 1 ilan mevcut. Eksik Sosyal ve Kayip
kategorileri eklendi; üç türün de birer kategorisi var. Geçici kayıtlarla üç türde
oluşturma/okuma, güncelleme, gizli filtre, tarih ve kategori FK kısıtları,
katılım/fotoğraf/şikayet cascade silme ve rollback kontrolü geçti. Kontrol sonunda
20 profil, 3 kategori ve mevcut 1 ilan korundu. Tekrarlanabilir SQL:
`docs/hafta-03-canli-sql-kontrol.sql`.

Bu canlı SQL kontrolü, API üzerinden iki hesaplı oturum/sahiplik testinin yerine
geçmez. Bu çalışma ortamı port açmayı ve doğrudan PostgreSQL bağlantısını
engellediğinden canlı Swagger kontrolü için uygulama normal Terminalden açılmalıdır.

Tam PostgreSQL kontrolü şu komutla çalışır:

```sh
dotnet run --project tests/FixCampus.DatabaseChecks
```

Bu test `FIXCAMPUS_TEST_DATABASE` ortam değişkeninde **boş ve atılabilir**,
adı `fixcampus_test` olan ayrı bir PostgreSQL veritabanı ister. Şema/fixture oluşturur
ve test verisi bırakır; ortak Supabase bağlantısı kullanılmaz. Oluşturma, kalıcı
okuma/güncelleme, iki hesapla sahiplik, kategori hataları, UTC tarih, gizli ilan
ve ilişkili kayıt silme senaryolarını kapsar. Bu ortamda Docker/veritabanı erişimi
olmadığından tam PostgreSQL CRUD senaryoları çalıştırılmadı; iki hesaplı canlı
Swagger denemesi de henüz yapılmadı.
