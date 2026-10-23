# Birinci hafta API çalışma notu

**Tarih:** 23 Ekim 2026

**Proje:** fixCampus Kampüs İlan Platformu

**Sorumlu:** Zülfikar Durak

Bu not, ilk haftanın Zülfikar'a ait API kod çalışmasını kaydeder. Ekipteki diğer işler ve liderlik videosu ayrıca takip edilir.

## Hazırlanan kod ve belgeler

- .NET 10 Web API iskeleti, uygulama durum endpoint'i ve Development ortamında Swagger/OpenAPI.
- Sosyal buluşma, kayıp ve buluntu için üç sabit kayıt dönen `GET /api/ornek/ilanlar`. Yanıt `dataSource=sample` ve `isPersisted=false` alanlarıyla örnek olduğunu bildirir.
- Yerel çalıştırma, tam JSON yanıtı, alan açıklamaları ve ekranlara bağlama adımlarını içeren API entegrasyon belgesi.

Örnek yanıt veritabanına bağlanmaz. Supabase bağlantısı, migration, Auth, roller, gerçek CRUD ve web/mobil ekranları yapılmış olarak raporlanmaz. Berna veri modeli ve veritabanı tarafını, Berke web iskeletini kendi katkılarıyla geliştirecektir.

## Kontrol ve gönderim durumu

| Kontrol | Durum |
|---|---|
| `dotnet build fixCampus.slnx --no-restore` | Başarılı; 0 uyarı, 0 hata. |
| `GET /api/durum` | HTTP 200. |
| `GET /api/ornek/ilanlar` | HTTP 200; üç kayıt, üç tür, `dataSource=sample`, `isPersisted=false`. |
| Örnek tarih ve boş tarih alanları | Sosyal örnekte `2026-10-25T11:00:00+00:00`; kayıp ve buluntuda `null` doğrulandı. |
| Swagger ve OpenAPI | Swagger HTML/JavaScript HTTP 200; OpenAPI 3.1.1 belgesinde iki GET rotası görüldü. |
| `POST /api/ornek/ilanlar` | HTTP 405; endpoint yalnız GET kabul ediyor. |
| Geliştirme ortamında Host filtresi | `10.0.2.2:5168` ile HTTP 200; tanımsız Host değeriyle HTTP 400. |
| Küçük kod commitleri | [API iskeleti 84439b4](https://github.com/bernakalkann/fixCampus/commit/84439b4) ve [örnek ilan yanıtı 97ff997](https://github.com/bernakalkann/fixCampus/commit/97ff997). Entegrasyon ve çalışma belgeleri ayrı belge commit'inde tutulur. |
| [Ekip deposuna](https://github.com/bernakalkann/fixCampus) push | İki kod commit'i 23 Ekim 2026 tarihinde `main` dalına başarıyla gönderildi. |

Bu kontroller yerel bilgisayarda yapıldı. Flutter emülatörü, fiziksel cihaz, Supabase bağlantısı ve gerçek hesaplarla entegrasyon denenmedi.

## Liderlik videosu anlatım taslağı

Bu bölüm en az beş dakikalık video için hazırlıktır; video çekildiği veya Drive'a yüklendiği anlamına gelmez.

| Süre | Gösterilecek ve anlatılacak konu |
|---|---|
| 0.00–1.00 | Projenin sosyal buluşma ve kayıp-buluntu kapsamı; Berna veri modeli, Berke web iskeleti, Zülfikar API iskeleti görev paylaşımı. Diğer üyelerin yalnız gerçekten tamamladıkları işler anlatılır. |
| 1.00–2.00 | Çözüm, API controller ve yanıt modeli klasörleri; uygulamayı terminalden çalıştırma. |
| 2.00–3.00 | Swagger'da `api/durum` isteğini gönderme; bunun veritabanı bağlantı testi olmadığını açıklama. |
| 3.00–4.00 | Örnek ilan isteği; üç ilan türü, `items`, `isPersisted=false` ve sabit UTC tarihi. |
| 4.00–5.00 | Entegrasyon belgesinden JSON alanları ve kartlara bağlama adımları; küçük commitlerin gerçek değişikliklerini gösterme. |
| 5.00–5.30 | Eksik ortak işler, sonraki adım ve 2. haftadaki rol kontrolünün Berna'nın Auth çalışmasına bağımlılığı. |

Plandaki video adı `Hafta01_KampusIlanPlatformu` biçimindedir; lider pazar 23.59'a kadar ortak Drive klasörüne yükler. Bu notta video ve yükleme bekleyen işlerdir.

## Sonraki adım

Berna ve Berke ile örnek yanıt üzerinden bağlantı denemesi yapılacak; nihai veri modeli ve endpointler birlikte netleştirilecek. 2. hafta, doğrulanmış oturum kimliği sağlandıktan sonra öğrenci/yönetici rol kontrolü geliştirilecek. İlk haftanın tüm ekip işlerinin tamamlandığına, bu API çalışması tek başına kanıt sayılmaz.
