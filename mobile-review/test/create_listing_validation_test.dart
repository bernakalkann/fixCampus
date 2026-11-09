import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/providers/auth_provider.dart';
import 'package:fixcampus_mobile/providers/listings_provider.dart';
import 'package:fixcampus_mobile/screens/listings/create_listing_screen.dart';
import 'package:fixcampus_mobile/screens/listings/listing_detail_screen.dart';
import 'package:fixcampus_mobile/theme/app_theme.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'support/listings_fakes.dart';

class _Auth extends AuthProvider {
  bool session = true;
  bool loading = false;
  bool failed = false;
  String userId = ownerId;
  @override
  bool get isLoggedIn => session;
  @override
  bool get isLoading => loading;
  @override
  bool get hasSessionError => failed;
  @override
  Profil get currentUser => Profil(
    id: userId,
    adSoyad: 'Öğrenci',
    rol: KullaniciRolu.ogrenci,
    olusturulmaTarihi: DateTime.utc(2026),
  );
  void refresh() => notifyListeners();
}

void main() {
  late _Auth auth;
  late FakeListingsService service;
  late ListingsProvider listings;
  setUp(() async {
    auth = _Auth();
    service = FakeListingsService();
    listings = ListingsProvider(service: service);
    await listings.load();
  });
  tearDown(() {
    auth.dispose();
    listings.dispose();
  });

  Future<void> open(WidgetTester tester, {Widget? screen}) async {
    tester.view.physicalSize = const Size(390, 844);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider<AuthProvider>.value(value: auth),
          ChangeNotifierProvider.value(value: listings),
        ],
        child: MaterialApp(
          theme: AppTheme.lightTheme,
          home: screen ?? const CreateListingScreen(),
        ),
      ),
    );
    await tester.pumpAndSettle();
  }

  Future<void> submit(WidgetTester tester, {bool edit = false}) async {
    final button = find.text(edit ? 'Değişiklikleri Kaydet' : 'İlanı Yayınla');
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pumpAndSettle();
  }

  Future<void> selectType(WidgetTester tester, String label) async {
    final dropdown = find.byType(DropdownButtonFormField<IlanTuru>);
    await tester.ensureVisible(dropdown);
    await tester.tap(dropdown);
    await tester.pumpAndSettle();
    await tester.tap(find.text(label).last);
    await tester.pumpAndSettle();
  }

  Future<void> fillLost(WidgetTester tester) async {
    await selectType(tester, 'Kayıp');
    await tester.tap(find.byType(DropdownButtonFormField<String>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Elektronik').last);
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextFormField).at(0), 'Kayıp defter');
    await tester.enterText(find.byType(TextFormField).at(1), 'Kütüphane');
    await tester.enterText(find.byType(TextFormField).at(2), 'Mavi defter.');
    FocusManager.instance.primaryFocus?.unfocus();
  }

  testWidgets('Boş ilan gerçek servise gönderilmez; dar ekranda taşma olmaz', (
    tester,
  ) async {
    await open(tester);
    await submit(tester);
    expect(find.text('Bu alan zorunludur.'), findsNWidgets(3));
    expect(find.text('Kategori seçin.'), findsOneWidget);
    expect(service.creates, 0);
    expect(tester.takeException(), isNull);
  });
  testWidgets('Tür değişince eski kategoriyle gönderim engellenir', (
    tester,
  ) async {
    await open(tester);
    await fillLost(tester);
    await selectType(tester, 'Buluntu');
    await submit(tester);
    expect(find.text('Kategori seçin.'), findsOneWidget);
    expect(service.creates, 0);
    expect(tester.takeException(), isNull);
  });
  testWidgets('Geçerli ilan API servisine gönderilir', (tester) async {
    await open(tester);
    await fillLost(tester);
    await submit(tester);
    expect(service.creates, 1);
    expect(service.savedRequest!.kategoriId, 'lost-category');
    expect(find.text('İlan yayınlandı.'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });
  testWidgets('Oturum kaybı açık formdan gönderimi kapatır', (tester) async {
    await open(tester);
    await fillLost(tester);
    auth.session = false;
    auth.refresh();
    await tester.pumpAndSettle();
    expect(find.text('İlanı Yayınla'), findsNothing);
    expect(
      find.text('İlan işlemi için oturumunuzu doğrulayın.'),
      findsOneWidget,
    );
    expect(service.creates, 0);
  });
  testWidgets('Form açıkken profil doğrulama hatası gönderimi kapatır', (
    tester,
  ) async {
    await open(tester);
    await fillLost(tester);
    auth.failed = true;
    auth.refresh();
    await tester.pumpAndSettle();
    expect(find.text('İlanı Yayınla'), findsNothing);
    expect(service.creates, 0);
  });
  testWidgets('Başkasının ilanıyla doğrudan düzenleme ekranı açılmaz', (
    tester,
  ) async {
    auth.userId = 'other';
    await open(tester, screen: CreateListingScreen(ilan: service.items.single));
    expect(find.text('İlanı yalnızca sahibi değiştirebilir.'), findsOneWidget);
    expect(find.byType(TextFormField), findsNothing);
    expect(service.savedRequest, isNull);
  });
  testWidgets('Hesap değişince açık düzenleme formu kapanır', (tester) async {
    await open(tester, screen: CreateListingScreen(ilan: service.items.single));
    expect(find.text('Değişiklikleri Kaydet'), findsOneWidget);
    auth.userId = 'other';
    auth.refresh();
    await tester.pumpAndSettle();
    expect(find.byType(TextFormField), findsNothing);
    expect(service.savedRequest, isNull);
  });
  testWidgets('Profil doğrulanırken eski sahip için işlem düğmeleri gizlenir', (
    tester,
  ) async {
    await open(tester, screen: ListingDetailScreen(ilan: service.items.single));
    expect(find.text('İlanı Sil'), findsOneWidget);
    auth.loading = true;
    auth.refresh();
    await tester.pumpAndSettle();
    expect(find.text('İlanı Sil'), findsNothing);
    expect(find.text('İlanı Düzenle'), findsNothing);
  });
  for (final changeAccount in [false, true]) {
    testWidgets(
      'Silme onayı açıkken ${changeAccount ? 'hesap değişimi' : 'profil hatası'} isteği engeller',
      (tester) async {
        await open(
          tester,
          screen: ListingDetailScreen(ilan: service.items.single),
        );
        await tester.ensureVisible(find.text('İlanı Sil'));
        await tester.tap(find.text('İlanı Sil'));
        await tester.pumpAndSettle();
        if (changeAccount) {
          auth.userId = 'other';
        } else {
          auth.failed = true;
        }
        auth.refresh();
        await tester.pumpAndSettle();
        await tester.tap(find.text('Sil'));
        await tester.pumpAndSettle();
        expect(service.deletes, 0);
        expect(service.items, isNotEmpty);
        expect(
          find.text(
            changeAccount
                ? 'İlanı yalnızca sahibi değiştirebilir.'
                : 'İlan işlemi için oturumunuzu doğrulayın.',
          ),
          findsOneWidget,
        );
      },
    );
  }
}

