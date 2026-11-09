import 'package:flutter_test/flutter_test.dart';
import 'package:fixcampus_mobile/models/models.dart';

void main() {
  group('Hafta 1 Veri Modelleri Testleri (Berna)', () {
    test('Profil modeli JSON serileştirme ve enum dönüşümü', () {
      final now = DateTime.utc(2026, 10, 24);
      final profil = Profil(
        id: 'usr-1',
        adSoyad: 'Berke Mahmut Acar',
        rol: KullaniciRolu.ogrenci,
        olusturulmaTarihi: now,
      );

      final json = profil.toJson();
      expect(json['id'], 'usr-1');
      expect(json['adSoyad'], 'Berke Mahmut Acar');
      expect(json['rol'], 'Ogrenci');

      final fromJson = Profil.fromJson(json);
      expect(fromJson.id, profil.id);
      expect(fromJson.adSoyad, profil.adSoyad);
      expect(fromJson.rol, KullaniciRolu.ogrenci);
    });

    test('Kategori modeli JSON serileştirme', () {
      const kategori = Kategori(
        id: 'cat-1',
        ad: 'Ders & Çalışma',
        tur: IlanTuru.sosyal,
      );

      final json = kategori.toJson();
      expect(json['id'], 'cat-1');
      expect(json['ad'], 'Ders & Çalışma');
      expect(json['tur'], 'Sosyal');

      final fromJson = Kategori.fromJson(json);
      expect(fromJson.id, kategori.id);
      expect(fromJson.ad, kategori.ad);
      expect(fromJson.tur, IlanTuru.sosyal);
    });

    test('Ilan modeli ve IlanDurumu enum dönüşümü', () {
      final now = DateTime.utc(2026, 10, 24, 12, 0);
      final ilan = Ilan(
        id: 'ilan-1',
        sahipId: 'usr-1',
        kategoriId: 'cat-1',
        baslik: 'Algoritmalar Birlikte Çalışma',
        aciklama: 'Kütüphanede algoritma soruları çözeceğiz.',
        tur: IlanTuru.sosyal,
        yer: 'Merkez Kütüphane',
        bulusmaTarihi: now,
        durum: IlanDurumu.aktif,
        olusturulmaTarihi: now,
      );

      final json = ilan.toJson();
      expect(json['tur'], 'Sosyal');
      expect(json['durum'], 'Aktif');

      final fromJson = Ilan.fromJson(json);
      expect(fromJson.baslik, ilan.baslik);
      expect(fromJson.tur, IlanTuru.sosyal);
      expect(fromJson.durum, IlanDurumu.aktif);
      expect(fromJson.bulusmaTarihi, now);
    });

    test('Katilim modeli JSON serileştirme', () {
      final now = DateTime.utc(2026, 10, 24, 14, 30);
      final katilim = Katilim(
        id: 'kat-1',
        ilanId: 'ilan-1',
        profilId: 'usr-1',
        katilimTarihi: now,
      );

      final json = katilim.toJson();
      expect(json['id'], 'kat-1');
      expect(json['ilanId'], 'ilan-1');
      expect(json['profilId'], 'usr-1');

      final fromJson = Katilim.fromJson(json);
      expect(fromJson.id, katilim.id);
      expect(fromJson.katilimTarihi, now);
    });

    test('Sikayet modeli JSON serileştirme', () {
      final now = DateTime.utc(2026, 10, 24, 15, 0);
      final sikayet = Sikayet(
        id: 'sik-1',
        ilanId: 'ilan-1',
        bildirenId: 'usr-2',
        neden: 'Yanıltıcı ilan içeriği',
        durum: SikayetDurumu.bekliyor,
        olusturulmaTarihi: now,
      );

      final json = sikayet.toJson();
      expect(json['neden'], 'Yanıltıcı ilan içeriği');
      expect(json['durum'], 'Bekliyor');

      final fromJson = Sikayet.fromJson(json);
      expect(fromJson.durum, SikayetDurumu.bekliyor);
    });

    test('IlanFotografi modeli JSON serileştirme', () {
      final now = DateTime.utc(2026, 10, 24, 16, 0);
      final foto = IlanFotografi(
        id: 'foto-1',
        ilanId: 'ilan-1',
        dosyaYolu: 'uploads/ilan1.jpg',
        yuklemeTarihi: now,
      );

      final json = foto.toJson();
      expect(json['dosyaYolu'], 'uploads/ilan1.jpg');

      final fromJson = IlanFotografi.fromJson(json);
      expect(fromJson.dosyaYolu, foto.dosyaYolu);
    });
  });
}

