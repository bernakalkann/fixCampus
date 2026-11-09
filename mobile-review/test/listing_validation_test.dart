import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/security/listing_mutation_policy.dart';
import 'package:fixcampus_mobile/security/role_session_policy.dart';
import 'package:fixcampus_mobile/validation/listing_validator.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  const validator = ListingValidator();
  const categories = [
    Kategori(id: 'social', ad: 'Spor', tur: IlanTuru.sosyal),
    Kategori(id: 'lost', ad: 'Eşya', tur: IlanTuru.kayip),
    Kategori(id: 'found', ad: 'Defter', tur: IlanTuru.buluntu),
  ];
  for (final value in <String?>[null, '', ' \t\n ']) {
    test('Boş alanlar reddedilir: ${value.toString()}', () {
      expect(validator.title(value), isNotNull);
      expect(validator.description(value), isNotNull);
      expect(validator.location(value), isNotNull);
    });
  }
  test('Türkçe metin ve kenar boşlukları geçerli içeriği engellemez', () {
    expect(validator.title('  Çalışma grubu  '), isNull);
    expect(validator.description('Öğrencilerle buluşalım.'), isNull);
    expect(validator.location('Kütüphane'), isNull);
  });
  test('Eksik, silinmiş ve farklı türdeki kategori reddedilir', () {
    for (final id in <String?>[null, '', ' ', 'deleted', 'lost']) {
      expect(
        validator.category(id, type: IlanTuru.sosyal, categories: categories),
        isNotNull,
      );
    }
    expect(
      validator.category(
        'social',
        type: IlanTuru.sosyal,
        categories: categories,
      ),
      isNull,
    );
    expect(
      validator.category(
        'social',
        type: IlanTuru.kayip,
        categories: categories,
      ),
      isNotNull,
    );
    expect(
      validator.category('social', type: IlanTuru.sosyal, categories: const []),
      isNotNull,
    );
  });
  test('Her ilan türü yalnızca kendi kategorisini kullanır', () {
    for (final type in [IlanTuru.kayip, IlanTuru.buluntu]) {
      for (final id in ['lost', 'found']) {
        expect(
          validator.category(id, type: type, categories: categories),
          id == (type == IlanTuru.kayip ? 'lost' : 'found')
              ? isNull
              : isNotNull,
        );
      }
    }
  });

  test('Metin sınırlarının altı, üstü ve trim davranışı', () {
    expect(validator.title('a'), isNotNull);
    expect(validator.title('ab'), isNull);
    expect(validator.title('a' * 150), isNull);
    expect(validator.title('a' * 151), isNotNull);
    expect(validator.title('  ab  '), isNull);
    expect(validator.description('a' * 3000), isNull);
    expect(validator.description('a' * 3001), isNotNull);
    expect(validator.location('a' * 200), isNull);
    expect(validator.location('a' * 201), isNotNull);
  });
  test('Sosyal ilan tarihi boş, geçmiş veya şimdi olamaz', () {
    final now = DateTime.utc(2026, 11, 3);
    for (final date in [null, now.subtract(const Duration(seconds: 1)), now]) {
      expect(validator.meetingDate(IlanTuru.sosyal, date, now: now), isNotNull);
    }
    expect(
      validator.meetingDate(
        IlanTuru.sosyal,
        now.add(const Duration(seconds: 1)),
        now: now,
      ),
      isNull,
    );
    expect(validator.meetingDate(IlanTuru.kayip, null, now: now), isNull);
    expect(validator.meetingDate(IlanTuru.buluntu, null, now: now), isNull);
  });

  final student = Profil(
    id: 'owner',
    adSoyad: 'Öğrenci',
    rol: KullaniciRolu.ogrenci,
    olusturulmaTarihi: DateTime.utc(2026),
  );
  final listing = Ilan(
    id: 'listing',
    sahipId: 'owner',
    kategoriId: 'social',
    baslik: 'Çalışma',
    aciklama: 'Buluşma',
    tur: IlanTuru.sosyal,
    yer: 'Kampüs',
    bulusmaTarihi: null,
    durum: IlanDurumu.aktif,
    olusturulmaTarihi: DateTime.utc(2026),
  );
  const policy = ListingMutationPolicy();
  RoleAccess access(
    ListingMutation action, {
    bool session = true,
    bool loading = false,
    bool error = false,
    Profil? profile,
    bool missingProfile = false,
    Ilan? target,
  }) => policy.evaluate(
    action: action,
    hasSession: session,
    isLoading: loading,
    profile: missingProfile ? null : (profile ?? student),
    hasSessionError: error,
    listing: target,
  );

  for (final action in ListingMutation.values) {
    test('$action için SDK oturumu tek başına yetmez', () {
      expect(
        access(action, session: false, target: listing),
        RoleAccess.signInRequired,
      );
      expect(
        access(action, loading: true, target: listing),
        RoleAccess.checking,
      );
      expect(
        access(action, missingProfile: true, target: listing),
        RoleAccess.profileRequired,
      );
      expect(
        access(action, error: true, target: listing),
        RoleAccess.profileRequired,
      );
      expect(
        access(
          action,
          profile: student.copyWith(id: ' '),
          target: listing,
        ),
        RoleAccess.profileRequired,
      );
    });
  }
  test('Doğrulanmış kullanıcı ilan oluşturabilir', () {
    expect(access(ListingMutation.create), RoleAccess.allowed);
  });
  for (final action in [ListingMutation.update, ListingMutation.delete]) {
    test('$action yalnızca doğrulanmış ilan sahibine açılır', () {
      expect(access(action, target: listing), RoleAccess.allowed);
      expect(
        access(
          action,
          target: listing,
          profile: student.copyWith(id: 'other'),
        ),
        RoleAccess.forbidden,
      );
      expect(access(action), RoleAccess.forbidden);
      expect(
        access(
          action,
          target: listing,
          profile: student.copyWith(id: 'other', rol: KullaniciRolu.yonetici),
        ),
        RoleAccess.forbidden,
      );
      expect(
        access(
          action,
          target: listing,
          profile: student.copyWith(rol: KullaniciRolu.yonetici),
        ),
        RoleAccess.allowed,
      );
    });
  }
}

