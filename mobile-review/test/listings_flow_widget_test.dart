import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/providers/auth_provider.dart';
import 'package:fixcampus_mobile/providers/listings_provider.dart';
import 'package:fixcampus_mobile/screens/listings/create_listing_screen.dart';
import 'package:fixcampus_mobile/screens/listings/listing_detail_screen.dart';
import 'package:fixcampus_mobile/services/api_exception.dart';
import 'package:fixcampus_mobile/services/listings_service.dart';
import 'package:fixcampus_mobile/services/local_profile_storage.dart';
import 'auth_provider_test.dart' show FakeAuthService;
import 'support/listings_fakes.dart';

void main() {
  late FakeAuthService authService;
  late AuthProvider auth;
  late FakeListingsService service;
  late ListingsProvider listings;

  setUp(() async {
    SharedPreferences.setMockInitialValues({});
    LocalProfileStorage.resetMemoryForTest();
    authService = FakeAuthService()..hasSession = true;
    auth = AuthProvider(service: authService);
    await auth.initialize();
    service = FakeListingsService();
    listings = ListingsProvider(service: service);
    await listings.load();
  });
  tearDown(() async {
    auth.dispose();
    listings.dispose();
    await authService.events.close();
  });

  Future<void> mount(WidgetTester tester, Widget screen) async {
    await tester.binding.setSurfaceSize(const Size(390, 844));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider.value(value: auth),
          ChangeNotifierProvider.value(value: listings),
        ],
        child: MaterialApp(
          home: Scaffold(
            body: Builder(
              builder: (context) => TextButton(
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute<void>(builder: (_) => screen),
                ),
                child: const Text('Ekranı aç'),
              ),
            ),
          ),
        ),
      ),
    );
    await tester.tap(find.text('Ekranı aç'));
    await tester.pumpAndSettle();
  }

  Future<void> fillFields(WidgetTester tester) async {
    await tester.enterText(find.byType(TextFormField).at(0), 'Kayıp defter');
    await tester.enterText(find.byType(TextFormField).at(1), 'Kütüphane');
    await tester.enterText(
      find.byType(TextFormField).at(2),
      'Mavi kapaklı defter.',
    );
    FocusManager.instance.primaryFocus?.unfocus();
  }

  Future<void> selectCategory(WidgetTester tester, String label) async {
    await tester.ensureVisible(find.byType(DropdownButtonFormField<String>));
    await tester.tap(find.byType(DropdownButtonFormField<String>));
    await tester.pumpAndSettle();
    await tester.tap(find.text(label).last);
    await tester.pumpAndSettle();
  }

  Future<void> fillLost(WidgetTester tester) async {
    await tester.tap(find.byType(DropdownButtonFormField<IlanTuru>));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Kayıp').last);
    await tester.pumpAndSettle();
    await selectCategory(tester, 'Elektronik');
    await fillFields(tester);
  }

  Future<void> publish(WidgetTester tester) async {
    final button = find.widgetWithText(ElevatedButton, 'İlanı Yayınla');
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pumpAndSettle();
  }

  testWidgets('Boş ilan formu servis çağrısı yapmaz', (tester) async {
    await mount(tester, const CreateListingScreen());
    await publish(tester);
    expect(service.creates, 0);
    expect(find.text('Bu alan zorunludur.'), findsWidgets);
  });

  testWidgets('Kayıp formu gerçek kategori kimliği ve null tarih gönderir', (
    tester,
  ) async {
    await mount(tester, const CreateListingScreen());
    await fillLost(tester);
    await publish(tester);
    expect(service.creates, 1);
    expect(service.savedRequest!.kategoriId, 'lost-category');
    expect(service.savedRequest!.tur, IlanTuru.kayip);
    expect(service.savedRequest!.bulusmaTarihi, isNull);
    expect(service.savedRequest!.yer, 'Kütüphane');
    expect(listings.ilanlar.first.baslik, 'Kayıp defter');
    expect(find.text('İlan yayınlandı.'), findsOneWidget);
    final type = tester.widget<DropdownButtonFormField<IlanTuru>>(
      find.byType(DropdownButtonFormField<IlanTuru>),
    );
    expect(type.initialValue, IlanTuru.sosyal);
  });

  testWidgets('Sosyal ilanda gelecek zaman seçilmeden istek gönderilmez', (
    tester,
  ) async {
    await mount(tester, const CreateListingScreen());
    await selectCategory(tester, 'Spor');
    await fillFields(tester);
    await publish(tester);
    expect(service.creates, 0);
    expect(find.textContaining('gelecekte bir buluşma zamanı'), findsOneWidget);
  });

  testWidgets('Sunucu hatası formu ve girilmiş bilgileri korur', (
    tester,
  ) async {
    await mount(tester, const CreateListingScreen());
    await fillLost(tester);
    service.error = const ListingApiException(
      ApiFailure.http,
      'Kategori bulunamadı.',
      statusCode: 400,
    );
    await publish(tester);
    expect(find.text('Kategori bulunamadı.'), findsOneWidget);
    expect(find.text('Kayıp defter'), findsOneWidget);
    expect(find.byType(CreateListingScreen), findsOneWidget);
    expect(listings.ilanlar.length, 1);
  });

  testWidgets('Düzenleme sunucu yanıtını listeye yansıtır ve geri döner', (
    tester,
  ) async {
    await mount(tester, CreateListingScreen(ilan: service.items.single));
    await tester.enterText(
      find.byType(TextFormField).first,
      'Güncellenen ilan',
    );
    FocusManager.instance.primaryFocus?.unfocus();
    final button = find.widgetWithText(ElevatedButton, 'Değişiklikleri Kaydet');
    await tester.ensureVisible(button);
    await tester.tap(button);
    await tester.pumpAndSettle();
    expect(service.savedRequest!.baslik, 'Güncellenen ilan');
    expect(listings.ilanlar.single.baslik, 'Güncellenen ilan');
    expect(find.text('Ekranı aç'), findsOneWidget);
  });

  testWidgets('Detay API den okunur; sahibine düzenleme ve silme gösterilir', (
    tester,
  ) async {
    await mount(tester, ListingDetailScreen(ilan: service.items.single));
    expect(service.detailsRead, 1);
    expect(find.text('İlanı Düzenle'), findsOneWidget);
    expect(find.text('İlanı Sil'), findsOneWidget);
    expect(find.text('Yer: Kampüs'), findsOneWidget);
  });

  testWidgets('İkinci hesap başkasının ilanında sahip düğmelerini görmez', (
    tester,
  ) async {
    authService.userId = '00000000-0000-0000-0000-000000000002';
    await auth.initialize();
    await mount(tester, ListingDetailScreen(ilan: service.items.single));
    expect(find.text('İlanı Düzenle'), findsNothing);
    expect(find.text('İlanı Sil'), findsNothing);
  });

  testWidgets(
    '404 detay hatasında eski içerik ve sahip düğmeleri gösterilmez',
    (tester) async {
      final stale = service.items.single;
      service.error = const ListingApiException(
        ApiFailure.http,
        'İlan bulunamadı.',
        statusCode: 404,
      );
      await mount(tester, ListingDetailScreen(ilan: stale));
      expect(find.text('İlan bulunamadı.'), findsOneWidget);
      expect(find.text('API ilanı'), findsNothing);
      expect(find.text('İlanı Sil'), findsNothing);
      expect(listings.ilanlar, isEmpty);
    },
  );

  testWidgets('Silme iptal edilince kayıt kalır; onaydan sonra kaldırılır', (
    tester,
  ) async {
    await mount(tester, ListingDetailScreen(ilan: service.items.single));
    await tester.tap(find.text('İlanı Sil'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Vazgeç'));
    await tester.pumpAndSettle();
    expect(service.deletes, 0);
    await tester.tap(find.text('İlanı Sil'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Sil'));
    await tester.pumpAndSettle();
    expect(service.deletes, 1);
    expect(listings.ilanlar, isEmpty);
    expect(find.text('Ekranı aç'), findsOneWidget);
  });

  testWidgets('403 silme hatası kayıt ve detay ekranını korur', (tester) async {
    await mount(tester, ListingDetailScreen(ilan: service.items.single));
    service.error = const ListingApiException(
      ApiFailure.http,
      'Bu işlem için yetkiniz yok.',
      statusCode: 403,
    );
    await tester.tap(find.text('İlanı Sil'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Sil'));
    await tester.pumpAndSettle();
    expect(find.text('Bu işlem için yetkiniz yok.'), findsOneWidget);
    expect(find.byType(ListingDetailScreen), findsOneWidget);
    expect(listings.ilanlar.length, 1);
  });
}

