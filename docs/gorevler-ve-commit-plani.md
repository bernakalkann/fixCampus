# Zülfikar için görev ve commit planı

Bu plan, `Kampus_Ilan_Platformu_Plan_Belgesi_Taslak (1)-4.docx` içindeki 9 haftalık dağılımı esas alır. Her satır ileride gerçekten geliştirilecek bir işi gösterir; tamamlanmış çalışma kaydı değildir. Bugünkü kapsam yalnız API iskeletidir.

## Her oturumun düzeni

1. Haftanın sırasındaki küçük işi seçip ilgili konuyu öğren.
2. Tek davranışı geliştir; çalıştır ve beklenen sonucu kontrol et.
3. Yalnız o işe ait dosyaları inceleyerek stage et ve açıklayıcı commit at.
4. Ekipte kararlaştırılan dala kendi hesabınla push et.
5. Haftalık rapora commit bağlantısı ve kısa gösterim sonucunu ekle.

Bir sonraki dilim, önceki dilim çalışınca geliştirilir. Sonraki haftaların kodları bugünden hazırlanıp daha sonra dağıtılmaz. Commit tarihleri gerçek çalışma tarihidir. Tüm üyeler plandaki haftalık katkıyı kendi GitHub hesaplarıyla gönderir.

## Haftalara göre dilimler

Aşağıdaki commit mesajları öneridir; yalnız ilgili iş tamamlandığında kullanılır.

| Hafta | Sende olan görev | Sırayla geliştirilecek küçük dilimler | Kontrol edilecek sonuç |
|---|---|---|---|
| 1 | API iskeleti | `feat: API iskeletini ve durum endpointini ekle` → `feat: web ve mobil icin ornek ilan yanitini ekle` → `docs: API entegrasyon adimlarini acikla` | Swagger açılır; durum yanıtı alınır; sonraki dilimde örnek veri açıkça örnek olarak sunulur. |
| 2 | Rol kontrolü | `feat: dogrulanmis oturumdan kullanici kimligini al` → `feat: ogrenci ve yonetici politikalarini ekle` → `test: yetkisiz erisim senaryolarini dogrula` | Oturumsuz istek 401; öğrencinin yönetim isteği 403; yönetici yetkili işlemi yapabilir. |
| 3 | Doğrulama | `feat: ilan isteklerine alan dogrulama ekle` → `feat: kategori ve tarih kurallarini dogrula` → `test: ilan dogrulama hatalarini denetle` | Geçersiz başlık, kategori ve tarih kaydedilmez; alan hataları anlaşılır döner. |
| 4 | İlanlarım | `feat: ilanlarim sorgusunu kullaniciya gore filtrele` → `feat: ilanlarim ekranini bagla` → `test: iki hesapla ilanlarim erisimini dogrula` | Her hesap kendi ilanlarını görür; kullanıcı kimliği formdan alınmaz. |
| 5 | Süre kuralı | `feat: aktif bulusmalara UTC sure filtresi ekle` → `test: bitis ani ve gecmis ilanlari denetle` | Süresi dolan ilan aktif akışta kaybolur; kayıt ve sahibinin erişimi korunur. |
| 6 | Fotoğraf API | `feat: fotograf turu ve boyutunu dogrula` → `feat: private Storage yuklemesini ekle` → `feat: tek fotograf ve sahiplik kurallarini ekle` → `test: fotograf erisimini ve hata temizligini denetle` | Yalnız sahibi tek uygun fotoğraf yükler; uygunsuz dosya reddedilir; okuma yetkisi denetlenir. |
| 7 | Şikâyet bildirim formu | `feat: ilan bildirim formunu ekle` → `feat: bildirim formunu APIye bagla` → `test: bildirim formunun hata akisini denetle` | Form, Berna ile kararlaştırılan endpoint'e bağlanır; başarı, boş neden ve tekrar bildirim sonuçları gösterilir. |
| 8 | Hata akışları | `fix: API hata yanitlarini arayuzde goster` → `fix: baglanti ve oturum hatalarini ele al` → `docs: hata kontrol senaryolarini ekle` | Bağlantı hatası, 401, 403, 404 ve geçersiz formda kullanıcı ne yapacağını anlar. |
| 9 | API kontrolü | `docs: Swagger gosterim senaryolarini ekle` → bulunan her hata için ayrı `fix: ...` → `docs: API son kontrol sonucunu kaydet` | Öğrenci, yönetici ve iki hesap senaryoları denenir; başka bilgisayarda kurulum birlikte kontrol edilir. |

## Ekipteki sınırlar

- **Berna:** veri modeli, Auth/profil, CRUD ve sorgular, katılım ve yönetim API'si. Sen mevcut işlere rol, doğrulama ve ilgili kuralları eklersin; paralel üyelik veya CRUD sistemi kurmazsın.
- **Berke:** web iskeleti, giriş/ilan/arama formları, buluşma formu ve yönetici paneli. İlanlarım ve bildirim formu için ortak tasarım ve endpointler üzerinde anlaşılır.
- **Zülfikar:** bu belgedeki görevler ve 1–3. haftalarda liderlik. İlk iskeletin hazır olması ortak veritabanı veya migration'ın bittiği anlamına gelmez.

2. haftada rol kontrolü, Berna'nın doğruladığı Auth kimliğini ve korunan profil rolünü kullanacak. 3. haftada doğrulama, Berna'nın CRUD endpointlerine eklenecek. 7. haftada yönetim API'sini ve panelini tekrar geliştirmek yerine bildirim formu bağlanacak.

## Liderlik ve gösterim

Plandaki çalışma dönemi 21 Ekim–23 Aralık 2026; vize haftası 9 çalışma haftasına dahil değil. Vize haftasının tam tarihi belgede olmadığı için haftaların takvim tarihleri burada yeniden atanmadı.

1–3. haftalarda lider Zülfikar'dır. Haftanın gerçek görev paylaşımını ve çalışan uygulamayı anlatan en az 5 dakikalık video, pazar 23.59'a kadar Drive'a yüklenir. Dosya adı `HaftaNN_KampusIlanPlatformu` olur. İlk sınıf gösterimi 18 Kasım, ikinci gösterim 23 Aralık 2026'dır.

Haftalık kayıtta şunlar bulunur: yapılan iş, commit bağlantısı, çalıştırılan kontrol, gösterilebilir sonuç, eksik iş ve sonraki küçük adım. İlk haftanın videosunda API iskeleti anlatılır; ilerideki özellikler plan olarak belirtilir.

## Commit örneği

Depo kökünde, yalnız ilgili iş gerçekten bittikten sonra:

```powershell
git status --short
git diff
git add src/FixCampus.Web/Controllers/Api/StatusController.cs
git diff --cached
git commit -m "feat: API durum endpointini ekle"
```

Bu örnek ilk commitin yeniden atılması için değildir; ilerideki küçük değişikliklerde aynı kontrol sırası izlenir. Push yapılacak dal ekipçe kararlaştırılır; gönderimden önce yeni ekip katkıları kontrol edilir.
