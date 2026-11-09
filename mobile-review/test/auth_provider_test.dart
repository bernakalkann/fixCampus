import 'dart:async';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/providers/auth_provider.dart';
import 'package:fixcampus_mobile/services/auth_service.dart';
import 'package:fixcampus_mobile/services/local_profile_storage.dart';

class FakeAuthService implements AuthService {
  @override
  bool hasSession = false;
  bool fail = false;
  bool profileFails = false;
  bool signOutFails = false;
  Completer<void>? pendingSignIn;
  bool confirmEmail = false;
  String name = 'Berke Mahmut Acar';
  String userId = '00000000-0000-0000-0000-000000000001';
  KullaniciRolu role = KullaniciRolu.ogrenci;
  final events = StreamController<void>.broadcast(sync: true);
  @override
  String? get email => hasSession ? 'test@example.com' : null;
  @override
  Stream<void> get changes => events.stream;
  @override
  Future<void> signIn(String email, String password) async {
    if (fail) throw StateError('offline');
    if (pendingSignIn != null) await pendingSignIn!.future;
    hasSession = true;
    events.add(null);
  }

  @override
  Future<void> signUp(String name, String email, String password) async {
    hasSession = !confirmEmail;
  }

  @override
  Future<void> signOut() async {
    if (signOutFails) throw StateError('offline');
    hasSession = false;
    events.add(null);
  }

  String? lastSavedName;
  String? lastSavedBolum;
  String? lastSavedSinif;
  String? serverBolum;
  String? serverSinif;
  final Map<String, String> _userBolumler = {};
  final Map<String, String> _userSiniflar = {};

  @override
  Future<Profil> loadProfile() async {
    if (profileFails) throw StateError('API offline');
    if (fail) throw StateError('offline');
    return Profil(
        id: userId,
        adSoyad: name,
        rol: role,
        bolum: _userBolumler.containsKey(userId)
            ? _userBolumler[userId]
            : serverBolum,
        sinif: _userSiniflar.containsKey(userId)
            ? _userSiniflar[userId]
            : serverSinif,
        olusturulmaTarihi: DateTime.utc(2026, 10, 21));
  }

  @override
  Future<Profil> saveProfile(
    String name, {
    String? bolum,
    String? sinif,
  }) async {
    if (fail) throw StateError('offline');
    this.name = name;
    lastSavedName = name;
    lastSavedBolum = bolum;
    lastSavedSinif = sinif;
    if (bolum != null) {
      _userBolumler[userId] = bolum;
    }
    if (sinif != null) {
      _userSiniflar[userId] = sinif;
    }
    return loadProfile();
  }
}

void main() {
  late FakeAuthService service;
  late AuthProvider auth;
  setUp(() {
    SharedPreferences.setMockInitialValues({});
    LocalProfileStorage.resetMemoryForTest();
    service = FakeAuthService();
    auth = AuthProvider(service: service);
  });
  tearDown(() async {
    auth.dispose();
    await service.events.close();
  });

  test('Başlangıçta demo kullanıcı veya yetki verilmez', () {
    expect(auth.isLoggedIn, false);
    expect(auth.currentUser, null);
    expect(auth.isAdmin, false);
  });
  test('Kayıt profil adını kaydeder; çıkış profili temizler', () async {
    expect(
        await auth.register(
            adSoyad: ' Berna Kalkan ',
            email: 'b@example.com',
            password: 'password123'),
        true);
    expect(auth.currentUser?.adSoyad, 'Berna Kalkan');
    expect(auth.isAdmin, false);
    expect(await auth.logout(), true);
    expect(auth.isLoggedIn, false);
    expect(auth.currentUser, null);
  });
  test('Kayıt onay gerektiriyorsa giriş yapılmış sayılmaz', () async {
    service.confirmEmail = true;
    expect(
        await auth.register(
            adSoyad: 'Berna', email: 'b@example.com', password: 'password123'),
        true);
    expect(auth.isLoggedIn, false);
    expect(auth.message, contains('onaylayıp'));
  });
  test('SDK oturumu geri yüklenir; ad düzenlemesi kalıcı servise iletilir',
      () async {
    service.hasSession = true;
    await auth.initialize();
    expect(auth.currentUser, isNotNull);
    expect(await auth.updateProfile(' Berna Kalkan '), true);
    expect(service.name, 'Berna Kalkan');
    expect(await auth.updateProfile(' '), false);
    expect(service.name, 'Berna Kalkan');
  });
  test('Başarısız girişte loading kapanır ve kullanıcı oluşmaz', () async {
    service.fail = true;
    expect(await auth.login(email: 'b@example.com', password: 'wrong'), false);
    expect(auth.isLoading, false);
    expect(auth.currentUser, null);
    expect(auth.message, isNotNull);
  });
  test('Oturum sona erince profil ve yönetici yetkisi temizlenir', () async {
    service.hasSession = true;
    await auth.initialize();
    service.hasSession = false;
    service.events.add(null);
    expect(auth.currentUser, null);
    expect(auth.isLoggedIn, false);
    expect(auth.isAdmin, false);
  });
  test('Eksik yapılandırma sahte giriş sağlamaz', () async {
    final unconfigured = AuthProvider();
    expect(await unconfigured.login(email: 'a@b.com', password: 'password123'),
        false);
    expect(unconfigured.isLoggedIn, false);
    unconfigured.dispose();
  });
  test('Aynı anda iki giriş isteği gönderilmez', () async {
    service.pendingSignIn = Completer<void>();
    final first = auth.login(email: 'b@example.com', password: 'password123');
    expect(auth.isLoading, true);
    expect(await auth.login(email: 'b@example.com', password: 'password123'),
        false);
    service.pendingSignIn!.complete();
    expect(await first, true);
    expect(auth.isLoading, false);
  });
  test('SDK girişi sonrası profil API hatası tekrar denenebilir', () async {
    service.profileFails = true;
    expect(await auth.login(email: 'b@example.com', password: 'password123'),
        false);
    expect(auth.isLoggedIn, true);
    expect(auth.currentUser, null);
    expect(auth.isAdmin, false);
    service.profileFails = false;
    expect(await auth.initialize(), true);
    expect(auth.currentUser, isNotNull);
    expect(auth.message, null);
  });
  test(
      'Başarısız çıkışta kullanıcıya hata gösterilir ve oturum yanlışlıkla temizlenmez',
      () async {
    service.hasSession = true;
    await auth.initialize();
    service.signOutFails = true;
    expect(await auth.logout(), false);
    expect(auth.isLoggedIn, true);
    expect(auth.currentUser, isNotNull);
    expect(auth.message, isNotNull);
    expect(auth.isLoading, false);
  });
  test('SDK değişiklik bildirimi API rolünü yeniden okur', () async {
    service.hasSession = true;
    await auth.initialize();
    service.role = KullaniciRolu.yonetici;
    service.events.add(null);
    await Future<void>.delayed(Duration.zero);
    expect(auth.isAdmin, true);
    service.role = KullaniciRolu.ogrenci;
    service.events.add(null);
    await Future<void>.delayed(Duration.zero);
    expect(auth.isAdmin, false);
  });
  test('SDK yenileme hatası uygulamayı düşürmeden kullanıcıya bildirilir',
      () async {
    service.hasSession = true;
    await auth.initialize();
    service.events.addError(StateError('offline'));
    expect(auth.message, contains('Oturum yenilenemedi'));
    expect(auth.isLoading, false);
  });

  test('Uygulama yeniden başladığında seçilen bölüm ve sınıf yerel depodan korunur',
      () async {
    service.hasSession = true;
    await auth.initialize();
    expect(
      await auth.updateProfile(
        'Berke Mahmut Acar',
        bolum: 'Yazılım Mühendisliği',
        sinif: '3. Sınıf',
      ),
      true,
    );
    expect(auth.currentUser?.bolum, 'Yazılım Mühendisliği');
    expect(auth.currentUser?.sinif, '3. Sınıf');

    // Yeni uygulama açılışı simülasyonu
    final restartedAuth = AuthProvider(service: service);
    await restartedAuth.initialize();
    expect(restartedAuth.currentUser?.bolum, 'Yazılım Mühendisliği');
    expect(restartedAuth.currentUser?.sinif, '3. Sınıf');
    restartedAuth.dispose();
  });

  test('Çıkış yapılıp başka hesapla girildiğinde önceki hesabın bölüm ve sınıfı taşınmaz',
      () async {
    service.hasSession = true;
    service.userId = '00000000-0000-0000-0000-000000000001';
    await auth.initialize();
    await auth.updateProfile(
      'Kullanıcı 1',
      bolum: 'Hukuk',
      sinif: '2. Sınıf',
    );
    expect(auth.currentUser?.bolum, 'Hukuk');
    expect(auth.currentUser?.sinif, '2. Sınıf');

    // Çıkış yap
    expect(await auth.logout(), true);
    expect(auth.currentUser, null);

    // Farklı kullanıcı girişi
    service.userId = '00000000-0000-0000-0000-000000000002';
    service.name = 'Kullanıcı 2';
    expect(
      await auth.login(email: 'kullanici2@example.com', password: 'password123'),
      true,
    );

    // 2. kullanıcı 1. kullanıcının bilgilerini görmemeli, kendi varsayılanını almalı
    expect(auth.currentUser?.bolum, 'Bilgisayar Mühendisliği');
    expect(auth.currentUser?.sinif, '4. Sınıf');

    // 2. kullanıcı kendi bilgilerini günceller
    await auth.updateProfile(
      'Kullanıcı 2',
      bolum: 'Tıp',
      sinif: '1. Sınıf',
    );
    expect(auth.currentUser?.bolum, 'Tıp');
    expect(auth.currentUser?.sinif, '1. Sınıf');

    // 2. kullanıcı çıkış yapıp tekrar 1. kullanıcı girerse
    await auth.logout();
    service.userId = '00000000-0000-0000-0000-000000000001';
    service.name = 'Kullanıcı 1';
    await auth.login(email: 'kullanici1@example.com', password: 'password123');

    // 1. kullanıcının kendi bilgileri korunmuş olarak geri gelmeli
    expect(auth.currentUser?.bolum, 'Hukuk');
    expect(auth.currentUser?.sinif, '2. Sınıf');
  });

  test(
      'Profil kaydedilirken PUT isteğinde adSoyad yanında bolum ve sinif de sunucuya iletilir',
      () async {
    service.hasSession = true;
    await auth.initialize();

    final result = await auth.updateProfile(
      'Berke Mahmut Acar',
      bolum: 'Endüstri Mühendisliği',
      sinif: '3. Sınıf',
    );
    expect(result, true);
    expect(service.lastSavedName, 'Berke Mahmut Acar');
    expect(service.lastSavedBolum, 'Endüstri Mühendisliği');
    expect(service.lastSavedSinif, '3. Sınıf');
    expect(auth.currentUser?.bolum, 'Endüstri Mühendisliği');
    expect(auth.currentUser?.sinif, '3. Sınıf');
  });

  test(
      'GET loadProfile’tan gelen sunucu değerleri yerel depodaki değerlerden önceliklidir',
      () async {
    service.hasSession = true;
    service.userId = '00000000-0000-0000-0000-000000000001';

    // Önce yerel depoda farklı değerler olsun (örneğin offline kaydedilmiş eski değer)
    const storage = LocalProfileStorage();
    await storage.saveAcademicInfo(
      service.userId,
      bolum: 'Eski Yerel Bölüm',
      sinif: '1. Sınıf',
    );

    // Sunucudan (GET /api/profil) gelen değerler ayarlanır
    service.serverBolum = 'Yazılım Mühendisliği';
    service.serverSinif = '4. Sınıf';

    await auth.initialize();

    // Sunucu değerleri öncelikli olmalı
    expect(auth.currentUser?.bolum, 'Yazılım Mühendisliği');
    expect(auth.currentUser?.sinif, '4. Sınıf');

    // Sunucudan gelen öncelikli değerler yerel depoyu da güncellemiş olmalı
    expect(await storage.getBolum(service.userId), 'Yazılım Mühendisliği');
    expect(await storage.getSinif(service.userId), '4. Sınıf');
  });

  test(
      'Sunucudan bölüm/sınıf gelmediğinde yerel depodaki değerler korunarak kullanılır',
      () async {
    service.hasSession = true;
    service.userId = '00000000-0000-0000-0000-000000000001';

    // Yerel depoda kayıtlı değer
    const storage = LocalProfileStorage();
    await storage.saveAcademicInfo(
      service.userId,
      bolum: 'Makine Mühendisliği',
      sinif: '2. Sınıf',
    );

    // Sunucudan null değer geliyor
    service.serverBolum = null;
    service.serverSinif = null;

    await auth.initialize();

    expect(auth.currentUser?.bolum, 'Makine Mühendisliği');
    expect(auth.currentUser?.sinif, '2. Sınıf');
  });
}



