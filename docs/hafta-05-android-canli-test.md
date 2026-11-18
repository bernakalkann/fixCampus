# Android canlı kullanıcı testi — 17 Kasım 2026

**Ortam:** Pixel 7 Android emülatörü, Flutter Android debug APK, Mac'te çalışan yerel .NET API ve projenin Supabase ortamı. Akış iki öğrenci hesabıyla uygulama arayüzünden elle denendi.

| Akış | Gözlenen sonuç |
| --- | --- |
| Kayıt ve giriş | Öğrenci hesabı oluşturuldu ve uygulamaya giriş yapıldı. |
| İlan yayınlama | Kayıp ilanı kaydedildi. Sosyal buluşma oluşturuldu ve katılım testi için açıldı. |
| Buluşmaya katılma | İkinci hesap **Geliyorum**'a bastığında sayaç 0'dan 1'e çıktı. |
| Katılımı geri alma | İkinci hesap **Katılımı Geri Al**'a bastığında sayaç tekrar 0'a düştü. |
| Katılımcı listesini görüntüleme | İlan sahibi sosyal buluşmanın detayındaki **Katılımcı sayısı** düğmesine dokundu. **Katılımcılar** listesi açıldı ve katılımcının ad soyad, bölüm ve sınıf bilgileri görüntülendi. |

**Sonuç:** Canlı katılım, geri alma (0 → 1 → 0) ve ilan sahibi hesabıyla katılımcı listesini görüntüleme senaryoları geçti.

## 17 Kasım canlı testinde kaydedilen bulgular

- Kayıt formunda Ad Soyad ve Bölüm alanlarına Türkçe karakter girilemedi. Emülatör klavyesi ve uygulama giriş doğrulaması ayrı ayrı kontrol edilmeli.
- Uygulama ilk açıldığında ilanlar hemen görünmedi. Akışın kendiliğinden yüklenip yüklenmediği ve beklerken hangi durumun gösterildiği kontrol edilmeli.
- Sosyal ilan formunda geçerli bir tarih seçildikten sonra önceki kırmızı tarih uyarısı ekranda kalabiliyor. Tarih değişince uyarı güncellenmeli.

## 18 Kasım 2026 geri bildirim düzeltmeleri

- İlk oturum yüklemesi tamamlanmadan sayfalı istek başlatılmaz. Başlangıç yüklemesi ikinci bir sayfa sorgusu üretmez. Bekleme sırasında “İlanlar yükleniyor…” metni ve ilerleme göstergesi görünür.
- Katıl / Katılımı Geri Al yanıtı mevcut kartın sayısını ve katılım durumunu günceller. Katılım için tüm sayfa silinip yeniden sorgulanmaz. Yenileme hatasında aynı oturumun görünen kartları korunur. Hesap, filtre, sayfa ve süre değişimlerinde eski sonuçların temizlenmesi sürer.
- Tarih doğrulaması genel API hatasından ayrıldı. Yeni tarih seçildiğinde tarih uyarısı yeniden hesaplanır. Tür değişiminde tarih uyarısı temizlenir.
- Detay katılım düğmesi, profil doğrulaması devam ederken veya oturum doğrulanamadığında kapalıdır. İşleyici de aynı kontrolü yapar.
- Harita konumuna dokunulunca panel kapanır. Harita açılmazsa hata ana ekranda gösterilir.
- Katılımcı listesi için 403 mesajı ilan sahibinin görüntüleme yetkisini açıklar. API bu uçta yalnızca ilan sahibine izin verir.
- Ad Soyad ve Bölüm alanlarında Türkçe karakter engelleyen filtre bulunmadı. Kayıt ekranında “Çağrı Şenışık Öztürk” ve “Bilgisayar Mühendisliği” değerlerinin korunması otomatik testle doğrulandı. Fiziksel/emülatör klavye düzeni için canlı yeniden deneme bekleniyor. Emülatörde Türkçe ekran klavyesiyle ı, İ, ş, ğ, ü, ö, ç karakterleri ayrıca denenmeli.
- StickerSlot sonradan verilecek görseller için alan ayırır. AvatarStack ve AppSwitch sonraki ekranların ortak bileşenleridir. Bildirimler menüsü henüz iskelettir.
- Ortak .NET LiveChecks akışına katılımcı listesinin OpenAPI kaydı, 401/403, ilan sahibi için 200, katılım sonrası liste içeriği, geri alma sonrası boş liste ve bulunamayan ilan kontrolleri eklendi. Bu senaryonun canlı çalıştırılması iki test hesabıyla ayrıca yapılmalı.

17 Kasım gözlemleri tarihsel kayıt olarak korundu. Yukarıdaki kod değişiklikleri cihazdaki canlı tekrarın yerine geçmez.
