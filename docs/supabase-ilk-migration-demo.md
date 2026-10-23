# Supabase bağlantısı, ilk migration ve tek ilan demosu

Bu katkı ilk hafta veri modelinin eksik bağlantı ve çalışan örnek kısmını tamamlar.
Mevcut sabit /api/ornek/ilanlar uç noktası değişmedi. Yeni örnek uç noktası
yalnız veritabanından okur; kayıt yoksa sabit veriyle başarı dönmez.

## 1. Gerekenler

- .NET 10 SDK.
- Ekipte ortak kullanılan Supabase projesi ve veritabanı bağlantı bilgisi.
- Supabase Authentication > Users bölümünde mevcut bir demo kullanıcı UUID'si.
  Kullanıcı yoksa ekip sahibi bir demo kullanıcı oluşturmalı. Parola repoya yazılmaz.

Supabase projesinin Connect panelinden doğrudan bağlantı veya session pooler
bilgilerini al. Host, port ve kullanıcı adını panelden aynen kullan; migration
için transaction pooler seçme. Bağlantı bir PostgreSQL bağlantısıdır;
Supabase URL/anon key bunun yerine geçmez.

## 2. Gizli ayarları bilgisayarında tanımla

Terminali güncel fixCampus deposunun kökünde aç:

```sh
dotnet user-secrets set "ConnectionStrings:Supabase" "Host=YOUR_HOST;Port=YOUR_PORT;Database=postgres;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=VerifyFull" --project src/FixCampus.Web
dotnet user-secrets set "Demo:ProfileId" "YOUR_AUTH_USER_UUID" --project src/FixCampus.Web
```

YOUR_* alanlarını gerçek değerlerle değiştir. Gerçek bağlantıyı appsettings.json,
README, ekran görüntüsü veya videoya koyma.
Alternatif ortam değişkenleri: ConnectionStrings__Supabase ve Demo__ProfileId.
Migration için tablo oluşturma yetkili sunucu bağlantısı kullanılır.

## 3. İlk migration'ı uygula

```sh
dotnet tool restore --tool-manifest dotnet-tools.json
dotnet restore fixCampus.slnx
dotnet build fixCampus.slnx --no-restore
dotnet ef migrations has-pending-model-changes --project src/FixCampus.Web
dotnet ef migrations script --project src/FixCampus.Web
dotnet ef database update --project src/FixCampus.Web
```

Migration altı tabloyu, foreign key'leri, indeksleri ve kontrol kısıtlarını oluşturur.
Model snapshot sonraki migration'ların başlangıç noktasıdır.
Migration zaten depodadır; yeniden migrations add InitialCreate çalıştırma.
Ortak veritabanında bir ekip üyesinin database update çalıştırması yeterlidir.
Var olan elle oluşturulmuş aynı adlı tablolar varsa önce ekiple durumu değerlendir;
bu migration onları silmez veya otomatik olarak üzerine yazmaz.

Tablolarda RLS etkinleştirilir; anon/authenticated erişim politikası verilmez.
EF Core sunucu bağlantısı tablo sahibi/uygun sunucu yetkisiyle çalışır.
Mobil istemci veritabanı parolasını kullanmaz.

## 4. Tek örnek ilanı oluştur ve göster

```sh
dotnet run --project src/FixCampus.Web --launch-profile http -- --Demo:Seed=true
```

Komut önce verilen UUID'nin auth.users içinde var olduğunu denetler. Gerekirse
Ogrenci rolünde profil ve Genel/Buluntu kategorisi oluşturur, sabit kimlikli
tek demo ilanını ekler. Tekrar çalıştırınca aynı ilanı çoğaltmaz ve mevcut
profil rolünü değiştirmez. Örnek kayıt zamanı UTC'dir.
Bu işlem açıkça istendiğinde ve yalnız Development ortamında çalışır.
Migration uygulamaz; önce 3. adım tamamlanmalıdır.

- Görsel demo: http://localhost:5168/veritabani-ornek
- JSON yanıtı: http://localhost:5168/api/veritabani/ornek-ilan
- Swagger: http://localhost:5168/swagger

Yanıtta dataSource=database ve isPersisted=true bulunur. Kaynağın gerçekten
veritabanı olduğunu kanıtlamak için Supabase Table Editor'de Ilanlar tablosundaki
demo kaydının Baslik alanını değiştir, sayfada İlanı yenile'ye bas; yeni başlık
görünmelidir. Kayıt kimliği: bced9190-2de7-41db-b13b-d94511c44402.

Sonraki normal çalıştırmalarda:

```sh
dotnet run --project src/FixCampus.Web --launch-profile http
```

Bağlantı eksikse örnek API 503, kayıt yok/gizliyse 404 döner.
Bağlantı/migration hatasında 503 döner; yanıt şifre veya hata ayrıntısı içermez.
Demo sayfası ve uç noktası Production ortamında kapalıdır.

## Doğrulama durumu

Hazırlama ortamında .NET SDK yok ve Docker erişimi kapalıydı. Canlı Supabase
bağlantı bilgisi de bulunmadığından derleme, migration uygulama ve canlı demo
burada çalıştırılarak doğrulanmadı. Bunlar tamamlandı şeklinde sunulmamalıdır.

İsteğe bağlı entegrasyon kontrolü için sadece boş, geçici PostgreSQL veritabanı
adı fixcampus_test olmalıdır. Ortak Supabase projesine yönlendirme:

```sh
export FIXCAMPUS_TEST_DATABASE='Host=localhost;Database=fixcampus_test;Username=YOUR_USER;Password=YOUR_PASSWORD'
dotnet run --project tests/FixCampus.DatabaseChecks
```

Kontrol migration/snapshot uyumunu, iki seed çağrısında tek ilan kalmasını,
kategorinin okunmasını, veritabanında değişen başlığın yansımasını ve gizli
ilanın gösterilmemesini sınar. Test deneme verilerini bırakır; ikinci çalıştırmadan
önce yalnız bu geçici test veritabanını yeniden boş oluştur.
