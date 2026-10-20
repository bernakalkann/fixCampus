# API görevleri için araştırma notları

Bu notlar 20 Ekim 2026 tarihinde plan belgesindeki Zülfikar görevleri için hazırlanmıştır. İlk API iskeleti dışındaki maddeler sonraki geliştirmelerin tasarım notlarıdır; uygulanmış özellik değildir.

## İlk adımın yapısı

Planın seçimi ASP.NET Core MVC ve kendi yazılan API'dir. Tek Web projesindeki controller yapısı, ileride MVC ekranlarının aynı uygulamaya eklenmesine uygundur. Mevcut bilgisayarda .NET 10 SDK bulunduğundan başlangıç bu sürümle hazırlandı; ekip ve ders ortamı aynı sürümü kullanmalıdır.

OpenAPI belgesi .NET'in `AddOpenApi` ve `MapOpenApi` desteğiyle üretilir. Swagger UI, bu belgeyi tarayıcıda inceleyip istek göndermeyi sağlar. Microsoft'un rehberi bu iki bileşenin birlikte kullanımını açıklar: [OpenAPI ve Swagger UI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0).

`Microsoft.OpenApi` sürümü doğrudan 2.12.0 olarak sabitlendi. İlk restore işlemi, geçişli gelen 2.0.0 sürümü için NU1903 uyarısı verdi; [Microsoft'un güvenlik bildiriminde](https://github.com/microsoft/OpenAPI.NET/security/advisories/GHSA-v5pm-xwqc-g5wc) 2.x dalında düzeltmenin 2.7.5 ve sonrasında bulunduğu belirtiliyor.

## Rol kontrolünü eklerken

Supabase token'ındaki `role=authenticated`, veritabanı erişim rolüdür; uygulamadaki Yönetici rolü değildir. Kullanıcının değiştirebildiği `user_metadata` da yetki kaynağı olmayacak. Berna'nın Auth akışı doğrulanmış kullanıcı kimliğini sağlayacak; Öğrenci/Yönetici yetkisi korunan profil kaydından okunacak. [Supabase rol yönetimi](https://supabase.com/docs/guides/api/custom-claims-and-role-based-access-control-rbac), [metadata ve RLS](https://supabase.com/docs/guides/database/postgres/row-level-security).

Token doğrulama yöntemi, Supabase projesinin imzalama anahtarına göre seçilecek. Asimetrik anahtarlarda JWKS; eski HS256 yapılandırmasında Auth sunucusunun `/auth/v1/user` doğrulaması değerlendirilecek. Projedeki gerçek ayarla denenmeden yalnız token içeriği okunarak yetki verilmeyecek. [Supabase JWT](https://supabase.com/docs/guides/auth/jwts).

EF Core ile doğrudan PostgreSQL bağlantısı, son kullanıcının Supabase oturumunu kendiliğinden RLS yetkisine dönüştürmez. API'de sahiplik ve rol denetimi ayrıca yapılacak. Veritabanı şifresi ve sunucu anahtarları yapılandırmada tutulacak. [Supabase RLS](https://supabase.com/docs/guides/database/postgres/row-level-security), [anahtar türleri](https://supabase.com/docs/guides/getting-started/api-keys).

## Doğrulama ve hata akışları

CRUD isteklerine ayrı DTO'lar ve DataAnnotations eklenebilir. Alanlar arası kategori/tarih kuralları ayrıca denetlenir. `[ApiController]`, geçersiz modele `400 ValidationProblemDetails` döndürebilir. MVC formu bu hataları ilgili alanlarda gösterecek. [Microsoft doğrulama rehberi](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0).

İlanlarım sorgusundaki kullanıcı kimliği oturumdan alınacak. Süre kuralında UTC ve denetlenebilir saat kaynağı kullanılacak; bitiş anı ile geçmiş kayıtların korunması ayrıca denenecek. Bunlar plana uygulanacak proje kararlarıdır.

Oturumsuz istek 401, yetkisiz işlem 403, bulunamayan ilan 404, alan hatası 400 olarak ele alınabilir. Beklenmeyen hatada teknik ayrıntı veya anahtar istemciye gönderilmeyecek. [Microsoft API hata yönetimi](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0).

## Fotoğraf API'sini eklerken

Önerilen başlangıç sınırı ilan başına tek fotoğraf ve en fazla 5 MiB'dır. Bu boyut proje kararıdır; geliştirme öncesinde ekiple kesinleştirilecek. Uzantı ve Content-Type yanında dosya imzası da kontrol edilecek. Yükleme/silme yetkisi sunucuda ilan sahibine göre denetlenecek.

Private bucket, erişim politikaları veya süreli imzalı bağlantıyla kontrollü okumaya uygundur. [Supabase bucket erişim modeli](https://supabase.com/docs/guides/storage/buckets/fundamentals). Storage ve PostgreSQL aynı transaction'ı paylaşmadığından, yarıda kalan yüklemenin temizlenmesi ve eşzamanlı iki yüklemede tek fotoğraf kısıtı ayrıca geliştirilecek.
