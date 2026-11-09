import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/providers/auth_provider.dart';
import 'package:fixcampus_mobile/providers/listings_provider.dart';
import 'package:fixcampus_mobile/routes/app_routes.dart';
import 'package:fixcampus_mobile/screens/admin/admin_panel_screen.dart';
import 'package:fixcampus_mobile/screens/auth/login_screen.dart';
import 'package:fixcampus_mobile/services/auth_service.dart';
import 'package:fixcampus_mobile/services/local_profile_storage.dart';
import 'package:fixcampus_mobile/widgets/role_session_guard.dart';
import 'package:shared_preferences/shared_preferences.dart';

class RoleTestService implements AuthService {
  @override
  bool hasSession = false;
  @override
  String? get email => hasSession ? 'role-test@example.invalid' : null;
  final events = StreamController<void>.broadcast(sync: true);
  KullaniciRolu role = KullaniciRolu.ogrenci;
  bool failProfile = false;
  Completer<void>? pendingProfile;
  int profileReads = 0;

  @override
  Stream<void> get changes => events.stream;

  @override
  Future<Profil> loadProfile() async {
    profileReads++;
    await pendingProfile?.future;
    if (failProfile) throw StateError('Profile API unavailable');
    return Profil(
      id: '00000000-0000-0000-0000-000000000001',
      adSoyad: 'Rol Testi',
      rol: role,
      olusturulmaTarihi: DateTime.utc(2026, 10, 26),
    );
  }

  @override
  Future<Profil> saveProfile(
    String name, {
    String? bolum,
    String? sinif,
  }) =>
      loadProfile();
  @override
  Future<void> signIn(String email, String password) async {
    hasSession = true;
  }

  @override
  Future<void> signUp(String name, String email, String password) async {
    hasSession = true;
  }

  @override
  Future<void> signOut() async {
    hasSession = false;
    events.add(null);
  }
}

void main() {
  setUpAll(() => initializeDateFormatting('tr_TR', null));
  late RoleTestService service;
  late AuthProvider auth;

  setUp(() {
    SharedPreferences.setMockInitialValues({});
    LocalProfileStorage.resetMemoryForTest();
    service = RoleTestService();
    auth = AuthProvider(service: service);
  });
  tearDown(() async {
    auth.dispose();
    await service.events.close();
  });

  Future<void> mount(WidgetTester tester, {bool adminOnly = false}) async {
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      ChangeNotifierProvider.value(
        value: auth,
        child: MaterialApp(
          home: RoleSessionGuard(
            adminOnly: adminOnly,
            child: const Scaffold(body: Text('Korunan içerik')),
          ),
        ),
      ),
    );
    await tester.pump();
  }

  Future<void> mountRoute(WidgetTester tester, String route) async {
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider.value(value: auth),
          ChangeNotifierProvider(create: (_) => ListingsProvider()),
        ],
        child: MaterialApp(
          onGenerateRoute: AppRoutes.onGenerateRoute,
          home: Builder(
            builder: (context) {
              return Scaffold(
                body: TextButton(
                  onPressed: () => Navigator.pushNamed(context, route),
                  child: const Text('Rotayı aç'),
                ),
              );
            },
          ),
        ),
      ),
    );
    await tester.tap(find.text('Rotayı aç'));
    await tester.pumpAndSettle();
  }

  testWidgets('Oturumsuz kullanıcı korunan içeriği göremez', (tester) async {
    await mount(tester);
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(find.text('Korunan içerik'), findsNothing);
  });

  testWidgets('SDK oturumu tek başına profil doğrulaması yerine geçmez', (
    tester,
  ) async {
    service.hasSession = true;
    await mount(tester);
    expect(find.text('Profil doğrulanamadı'), findsOneWidget);
    expect(find.text('Korunan içerik'), findsNothing);
  });

  testWidgets('Doğrulanmış öğrenci genel korunan ekranı açar', (tester) async {
    service.hasSession = true;
    await auth.initialize();
    await mount(tester);
    expect(find.text('Korunan içerik'), findsOneWidget);
  });

  testWidgets('Öğrenci yönetici ekranına doğrudan erişemez', (tester) async {
    service.hasSession = true;
    await auth.initialize();
    await mount(tester, adminOnly: true);
    expect(find.textContaining('yönetici yetkisi gerekir'), findsOneWidget);
    expect(find.text('Korunan içerik'), findsNothing);
  });

  testWidgets('API yönetici profili döndürürse yönetici ekranı açılır', (
    tester,
  ) async {
    service.hasSession = true;
    service.role = KullaniciRolu.yonetici;
    await auth.initialize();
    await mount(tester, adminOnly: true);
    expect(find.text('Korunan içerik'), findsOneWidget);
  });

  testWidgets('Rol yeniden okunurken eski yönetici profiline güvenilmez', (
    tester,
  ) async {
    service.hasSession = true;
    service.role = KullaniciRolu.yonetici;
    await auth.initialize();
    await mount(tester, adminOnly: true);
    service.pendingProfile = Completer<void>();
    final refresh = auth.initialize();
    await tester.pump();
    expect(find.text('Oturum ve rol doğrulanıyor.'), findsOneWidget);
    expect(find.text('Korunan içerik'), findsNothing);
    service.role = KullaniciRolu.ogrenci;
    service.pendingProfile!.complete();
    await refresh;
    await tester.pumpAndSettle();
    expect(find.textContaining('yönetici yetkisi gerekir'), findsOneWidget);
  });

  testWidgets('Profil okuma hatasında eski yönetici yetkisi erişim açmaz', (
    tester,
  ) async {
    service.hasSession = true;
    service.role = KullaniciRolu.yonetici;
    await auth.initialize();
    await mount(tester, adminOnly: true);
    service.failProfile = true;
    await auth.initialize();
    await tester.pumpAndSettle();
    expect(find.text('Profil doğrulanamadı'), findsOneWidget);
    expect(find.text('Korunan içerik'), findsNothing);
    service.failProfile = false;
    service.role = KullaniciRolu.ogrenci;
    await tester.tap(find.text('Tekrar dene'));
    await tester.pumpAndSettle();
    expect(find.textContaining('yönetici yetkisi gerekir'), findsOneWidget);
    expect(find.text('Korunan içerik'), findsNothing);
  });

  testWidgets('Profil hatasında kullanıcı çıkış yapabilir', (tester) async {
    service.hasSession = true;
    await mount(tester);
    await tester.tap(find.text('Çıkış yap'));
    await tester.pumpAndSettle();
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(auth.isLoggedIn, false);
  });

  testWidgets('Oturum kapanınca açık yönetici içeriği kaldırılır', (
    tester,
  ) async {
    service.hasSession = true;
    service.role = KullaniciRolu.yonetici;
    await auth.initialize();
    await mount(tester, adminOnly: true);
    service.hasSession = false;
    service.events.add(null);
    await tester.pumpAndSettle();
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(find.text('Korunan içerik'), findsNothing);
  });

  testWidgets('Uygulamaya dönüşte rol API üzerinden yeniden okunur', (
    tester,
  ) async {
    service.hasSession = true;
    service.role = KullaniciRolu.yonetici;
    await auth.initialize();
    await mount(tester, adminOnly: true);
    final reads = service.profileReads;
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.inactive);
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.paused);
    service.role = KullaniciRolu.ogrenci;
    tester.binding.handleAppLifecycleStateChanged(AppLifecycleState.resumed);
    await tester.pumpAndSettle();
    expect(service.profileReads, reads + 1);
    expect(find.textContaining('yönetici yetkisi gerekir'), findsOneWidget);
    expect(find.text('Korunan içerik'), findsNothing);
  });

  testWidgets('Gerçek yönetici rotasında API hatası erişimi kapatır', (
    tester,
  ) async {
    service.hasSession = true;
    service.role = KullaniciRolu.yonetici;
    await auth.initialize();
    await mountRoute(tester, AppRoutes.adminPanel);
    expect(find.byType(AdminPanelScreen), findsOneWidget);
    service.failProfile = true;
    await auth.initialize();
    await tester.pumpAndSettle();
    expect(find.text('Profil doğrulanamadı'), findsOneWidget);
    expect(find.byType(AdminPanelScreen), findsNothing);
  });

  testWidgets('Rol kontrolü bitince kullanıcının Profil sekmesi korunur', (
    tester,
  ) async {
    service.hasSession = true;
    await auth.initialize();
    await mountRoute(tester, AppRoutes.main);
    await tester.tap(find.text('Profil').last);
    await tester.pumpAndSettle();
    expect(find.text('Profili düzenle'), findsOneWidget);
    service.pendingProfile = Completer<void>();
    final refresh = auth.initialize();
    await tester.pump();
    expect(find.text('Oturum ve rol doğrulanıyor.'), findsOneWidget);
    expect(find.text('Profili düzenle'), findsNothing);
    service.pendingProfile!.complete();
    await refresh;
    await tester.pumpAndSettle();
    expect(find.text('Profili düzenle'), findsOneWidget);
  });
  testWidgets('Profil form doğrulama hatası geçerli oturumu kapatmaz', (
    tester,
  ) async {
    service.hasSession = true;
    await auth.initialize();
    await mount(tester);
    expect(await auth.updateProfile(' '), false);
    await tester.pumpAndSettle();
    expect(find.text('Korunan içerik'), findsOneWidget);
    expect(find.text('Profil doğrulanamadı'), findsNothing);
  });
}

