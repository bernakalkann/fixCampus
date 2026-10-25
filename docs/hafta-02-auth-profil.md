# Hafta 2 — Supabase Auth ve profil API (Berna)

Kimlik bilgileri Supabase Auth tarafından yönetilir. Uygulama parola saklamaz.
Bu değişiklik Berna'nın API kapsamıdır; MVC giriş/kayıt formları Berke'nin görevinde kalır.

## Yerel ayarlar

Mevcut veritabanı bağlantısı ve ilk migration kurulmuş olmalıdır (bkz. supabase-ilk-migration-demo.md).

```sh
dotnet user-secrets set "Supabase:Url" "https://PROJE.supabase.co" --project src/FixCampus.Web
dotnet user-secrets set "Supabase:PublishableKey" "SUPABASE_PUBLISHABLE_KEY" --project src/FixCampus.Web
```

Publishable key veya eski anon key kullanılabilir; service_role anahtarı kullanılmaz.
Supabase Authentication ayarlarında e-posta/parola sağlayıcısını açın. Proje planına
uygun yerel gösterimde Confirm email kapalıdır. E-posta onayının etkin olduğu başka
bir ortamda kullanıcı önce e-postasındaki bağlantıyı açmalıdır. Site URL ve izin verilen
redirect adreslerini kendi geliştirme/üretim adreslerinize göre ayarlayın.

## Endpointler

| Yöntem | Yol | JSON / davranış |
|---|---|---|
| POST | /api/auth/kayit | `{"email":"student@example.com","password":"guclu-parola","adSoyad":"Berna Kalkan"}` |
| POST | /api/auth/giris | `{"email":"student@example.com","password":"guclu-parola"}` |
| POST | /api/auth/yenile | `{"refreshToken":"..."}` |
| POST | /api/auth/cikis | Bearer token; mevcut oturumun refresh tokenlarını iptal eder |
| GET | /api/profil | Bearer token; kendi profilini döndürür; henüz oluşturulmadıysa 404 |
| PUT | /api/profil | Bearer token ve `{"adSoyad":"Berna Kalkan"}`; kendi profilini oluşturur/günceller |

Giriş/yenileme yanıtı Supabase oturum biçimindedir (`access_token`, `refresh_token`,
`expires_in`, `user`). E-posta onayı gereken kayıtta henüz oturum oluşmayabilir.
Kayıt adı Supabase user_metadata içinde saklanır; e-posta onayı ve girişten sonra
PUT /api/profil ile public.Profiller kaydı oluşturulmalıdır. Aynı çağrı sonraki
profil düzenlemelerinde de kullanılır. Yeni profil daima Ogrenci olur; mevcut rol korunur.
Rol kontrolü/yönetici politikaları Zülfikar'ın ayrı görevidir; istemci metadatası rol sayılmaz.

Yetkili isteklerde `Authorization: Bearer ACCESS_TOKEN` gönderin. Sunucu tokenı
Supabase /auth/v1/user üzerinden doğrular; istemciden gelen kullanıcı ID'sine güvenmez.
Kimliksiz veya geçersiz tokenlı profil isteği 401 döndürür. İsim 2–100 karakterdir.
Çıkışta istemci kendi tokenlarını da silmelidir. Supabase gereği önceden verilmiş
access token süresi dolana kadar geçerli olabilir; logout refresh oturumunu kapatır.
Auth işlemleri IP başına dakikada 10 istekle sınırlıdır (429). Reverse proxy arkasında
IP forwarding yalnızca güvenilen proxyler için ayrıca yapılandırılmalıdır.

## Kontrol

```sh
dotnet build fixCampus.slnx
dotnet run --project tests/FixCampus.AuthChecks
```

AuthChecks harici servise bağlanmadan istek yollarını, kayıt metadatasını, hata
kodlarını, çıkış tokenını ve eksik yapılandırmayı doğrular. Canlı kabul testi:

1. Kayıt ol, e-posta onayını tamamla ve giriş yap.
2. Access token ile GET profil: ilk kullanımda 404 bekle.
3. PUT profil ile adını kaydet, GET ile aynı kimlik ve Ogrenci rolünü doğrula.
4. İkinci kullanıcı ile aynı işlemi yap; profillerin birbirinden bağımsız olduğunu doğrula.
5. Tokensız/geçersiz token ile GET/PUT: 401 bekle.
6. Yenile ile yeni oturum al; çıkış yap, istemci tokenlarını sil.

Canlı test gerçek Supabase yapılandırması gerektirir. Bu değişiklik şemayı değiştirmez;
mevcut auth.users → Profiller kimlik ilişkisini kullanır.

## Canlı doğrulama — 25 Ekim 2026

Yerel web uygulaması ve ortak Supabase üzerinde kullanıcı tarafından çalıştırılan
kabul testlerinin çıktısı incelendi: kayıt ve giriş, profil oluşturma/okuma,
güncellemenin tekrar okunması, ilk adın geri yüklenmesi, tokensız ve geçersiz
tokenlı erişimin reddedilmesi, boş ad doğrulaması, ikinci hesap oluşturma,
istemciden gönderilen başka kimliğin ve yönetici rolünün uygulanmaması, ilk
hesabın değişmemesi, oturum yenileme, yenilenen tokenla erişim, iki hesaptan
çıkış ve çıkıştan sonra refresh tokenın reddedilmesi geçti. Testler mevcut
access tokenın çıkış anında iptal edildiğini iddia etmez. Test hesapları ayrı
deneme kayıtlarıdır; parolalar ve tokenlar bu belgeye/depo dosyalarına eklenmedi.
