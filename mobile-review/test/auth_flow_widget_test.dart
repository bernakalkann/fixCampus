import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/providers/auth_provider.dart';
import 'package:fixcampus_mobile/providers/listings_provider.dart';
import 'package:fixcampus_mobile/screens/auth/login_screen.dart';
import 'package:fixcampus_mobile/screens/auth/register_screen.dart';
import 'package:fixcampus_mobile/screens/profile/profile_screen.dart';
import 'package:fixcampus_mobile/widgets/auth_guard.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:fixcampus_mobile/services/local_profile_storage.dart';
import 'auth_provider_test.dart' show FakeAuthService;

void main() {
  setUpAll(() => initializeDateFormatting('tr_TR', null));
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

  Future<void> mount(WidgetTester tester, Widget screen) async {
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(MultiProvider(
        providers: [
          ChangeNotifierProvider.value(value: auth),
          ChangeNotifierProvider(create: (_) => ListingsProvider()),
        ],
        child: MaterialApp(home: screen, routes: {
          '/main': (_) => const Scaffold(body: Text('Oturum açıldı')),
          '/login': (_) => const LoginScreen(),
        })));
    await tester.pumpAndSettle();
  }

  testWidgets('Girişte boş form yönlendirme yapmaz', (tester) async {
    await mount(tester, const LoginScreen());
    await tester.tap(find.widgetWithText(ElevatedButton, 'Giriş Yap'));
    await tester.pumpAndSettle();
    expect(auth.isLoggedIn, false);
    expect(find.textContaining('Geçerli bir e-posta'), findsOneWidget);
  });
  testWidgets('Doğru bilgiler SDK servisini çağırır ve ana sayfaya geçer',
      (tester) async {
    await mount(tester, const LoginScreen());
    await tester.enterText(find.byType(TextField).at(0), 'berna@example.com');
    await tester.enterText(find.byType(TextField).at(1), 'password123');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Giriş Yap'));
    await tester.pumpAndSettle();
    expect(auth.isLoggedIn, true);
    expect(find.text('Oturum açıldı'), findsOneWidget);
  });
  testWidgets('Servis hatasında giriş ekranında mesaj gösterilir',
      (tester) async {
    service.fail = true;
    await mount(tester, const LoginScreen());
    await tester.enterText(find.byType(TextField).at(0), 'berna@example.com');
    await tester.enterText(find.byType(TextField).at(1), 'password123');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Giriş Yap'));
    await tester.pumpAndSettle();
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(find.textContaining('İşlem tamamlanamadı'), findsOneWidget);
    expect(auth.isLoading, false);
  });
  testWidgets('Kayıt ekranı geçerli ad ve parola ile profil oluşturur',
      (tester) async {
    await mount(tester, const RegisterScreen());
    await tester.enterText(find.byType(TextField).at(0), 'Berna Kalkan');
    await tester.enterText(find.byType(TextField).at(1), 'berna@example.com');
    await tester.enterText(find.byType(TextField).at(2), 'password123');
    await tester.tap(find.widgetWithText(ElevatedButton, 'Kayıt Ol'));
    await tester.pumpAndSettle();
    expect(auth.currentUser?.adSoyad, 'Berna Kalkan');
    expect(auth.isAdmin, false);
    expect(find.text('Oturum açıldı'), findsOneWidget);
  });
  testWidgets('Profil düzenleme kaydedilen adı ekrana yansıtır',
      (tester) async {
    service.hasSession = true;
    await auth.initialize();
    await mount(tester, const ProfileScreen());
    await tester.tap(find.text('Profili düzenle'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'Berna Kalkan');
    await tester.tap(find.text('Kaydet'));
    await tester.pump(const Duration(milliseconds: 500));
    await tester.pumpAndSettle();
    expect(auth.currentUser?.adSoyad, 'Berna Kalkan');
    expect(find.text('Berna Kalkan'), findsOneWidget);
  });
  testWidgets('Profil çıkışı oturumu temizler ve giriş ekranına döner',
      (tester) async {
    service.hasSession = true;
    await auth.initialize();
    await mount(tester, const ProfileScreen());
    await tester.tap(find.text('Çıkış yap'));
    await tester.pumpAndSettle();
    expect(auth.isLoggedIn, false);
    expect(auth.currentUser, null);
    expect(find.byType(LoginScreen), findsOneWidget);
  });
  testWidgets('Girişsiz korumalı ekran giriş sayfasını gösterir',
      (tester) async {
    await mount(tester, const AuthGuard(child: Text('Özel ekran')));
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(find.text('Özel ekran'), findsNothing);
  });
  testWidgets('Öğrenci yönetici ekranını açamaz', (tester) async {
    service.hasSession = true;
    await auth.initialize();
    await mount(tester,
        const AuthGuard(adminOnly: true, child: Text('Yönetici ekranı')));
    expect(find.textContaining('yönetici yetkisi gerekir'), findsOneWidget);
    expect(find.text('Yönetici ekranı'), findsNothing);
  });
  testWidgets('API yönetici rolü döndürdüğünde yönetici ekranı açılır',
      (tester) async {
    service.hasSession = true;
    service.role = KullaniciRolu.yonetici;
    await auth.initialize();
    await mount(tester,
        const AuthGuard(adminOnly: true, child: Text('Yönetici ekranı')));
    expect(find.text('Yönetici ekranı'), findsOneWidget);
  });
  testWidgets('Oturum kaybında açık korumalı ekran kapanır', (tester) async {
    service.hasSession = true;
    await auth.initialize();
    await mount(tester, const AuthGuard(child: Text('Özel ekran')));
    service.hasSession = false;
    service.events.add(null);
    await tester.pumpAndSettle();
    expect(find.byType(LoginScreen), findsOneWidget);
    expect(find.text('Özel ekran'), findsNothing);
  });
}

