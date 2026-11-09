import 'package:fixcampus_mobile/models/models.dart';
import 'package:fixcampus_mobile/providers/auth_provider.dart';
import 'package:fixcampus_mobile/providers/listings_provider.dart';
import 'package:fixcampus_mobile/routes/app_routes.dart';
import 'package:fixcampus_mobile/screens/auth/login_screen.dart';
import 'package:fixcampus_mobile/screens/listings/listing_search_screen.dart';
import 'package:fixcampus_mobile/screens/lost_found/lost_found_feed_screen.dart';
import 'package:fixcampus_mobile/screens/radar/radar_feed_screen.dart';
import 'package:fixcampus_mobile/theme/app_theme.dart';
import 'package:fixcampus_mobile/utils/listing_search_filter.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:provider/provider.dart';
import 'support/listings_fakes.dart';

class _TestAuth extends AuthProvider {
  bool loggedIn = true;
  bool loading = false;
  bool sessionError = false;
  String userId = ownerId;

  @override
  bool get isLoggedIn => loggedIn;

  @override
  bool get isLoading => loading;

  @override
  bool get hasSessionError => sessionError;

  @override
  Profil? get currentUser => loggedIn
      ? Profil(
          id: userId,
          adSoyad: 'Test Öğrenci',
          rol: KullaniciRolu.ogrenci,
          olusturulmaTarihi: DateTime.utc(2026, 11, 1),
        )
      : null;

  void notify() => notifyListeners();
}

Ilan _makeListing({
  required String id,
  required String baslik,
  required String aciklama,
  required String yer,
  required String kategoriId,
  required IlanTuru tur,
  IlanDurumu durum = IlanDurumu.aktif,
  DateTime? createdAt,
}) {
  return Ilan(
    id: id,
    baslik: baslik,
    aciklama: aciklama,
    kategoriId: kategoriId,
    sahipId: ownerId,
    tur: tur,
    durum: durum,
    yer: yer,
    bulusmaTarihi: tur == IlanTuru.sosyal
        ? DateTime.utc(2026, 12, 15, 14)
        : null,
    olusturulmaTarihi: createdAt ?? DateTime.utc(2026, 11, 1, 12),
  );
}

void main() {
  setUpAll(() async {
    await initializeDateFormatting('tr_TR', null);
  });

  group('ListingSearchFilter birim testleri', () {
    final categories = [
      const Kategori(id: 'cat-spor', ad: 'Spor', tur: IlanTuru.sosyal),
      const Kategori(id: 'cat-ders', ad: 'Ders Çalışma', tur: IlanTuru.sosyal),
      const Kategori(id: 'cat-el', ad: 'Elektronik', tur: IlanTuru.kayip),
      const Kategori(
        id: 'cat-kirtasiye',
        ad: 'Kırtasiye',
        tur: IlanTuru.buluntu,
      ),
    ];

    final listings = [
      _makeListing(
        id: '1',
        baslik: 'İstatistik Final Çalışma Grubu',
        aciklama: 'Merkez kütüphanede soru çözümü yapılacaktır.',
        yer: 'Kütüphane 2. Kat',
        kategoriId: 'cat-ders',
        tur: IlanTuru.sosyal,
        createdAt: DateTime.utc(2026, 11, 1, 10),
      ),
      _makeListing(
        id: '2',
        baslik: 'Akşam Halı Saha Maçı',
        aciklama: 'Kaleci ve defans oyuncusu aranıyor.',
        yer: 'Kampüs Spor Tesisi',
        kategoriId: 'cat-spor',
        tur: IlanTuru.sosyal,
        createdAt: DateTime.utc(2026, 11, 2, 10),
      ),
      _makeListing(
        id: '3',
        baslik: 'Siyah Kablosuz Kulaklık',
        aciklama: 'Mühendislik kantininde unutuldu.',
        yer: 'A Blok Kantin',
        kategoriId: 'cat-el',
        tur: IlanTuru.kayip,
        createdAt: DateTime.utc(2026, 11, 3, 10),
      ),
      _makeListing(
        id: '4',
        baslik: 'Mavi Hesap Makinesi',
        aciklama: 'Amfi 3 sıralarında bulundu.',
        yer: 'Mühendislik Amfi 3',
        kategoriId: 'cat-kirtasiye',
        tur: IlanTuru.buluntu,
        createdAt: DateTime.utc(2026, 11, 4, 10),
      ),
    ];

    test(
      'Türkçe karakter duyarlı metin araması başlık, açıklama ve yeri kapsar',
      () {
        final byTurkishI = ListingSearchFilter.filter(
          listings,
          query: 'istatistik kütüphane',
          kategoriler: categories,
        );
        expect(byTurkishI.map((e) => e.id), ['1']);

        final byUpperDotlessI = ListingSearchFilter.filter(
          listings,
          query: 'HALI SAHA',
          kategoriler: categories,
        );
        expect(byUpperDotlessI.map((e) => e.id), ['2']);

        final byCategoryName = ListingSearchFilter.filter(
          listings,
          query: 'elektronik',
          kategoriler: categories,
        );
        expect(byCategoryName.map((e) => e.id), ['3']);
      },
    );

    test(
      'Tür ve kategori filtreleri birlikte uygulanır ve tarihe göre sıralanır',
      () {
        final socialOnly = ListingSearchFilter.filter(
          listings,
          tur: IlanTuru.sosyal,
          kategoriler: categories,
        );
        expect(socialOnly.map((e) => e.id), ['2', '1']);

        final socialSport = ListingSearchFilter.filter(
          listings,
          tur: IlanTuru.sosyal,
          kategoriId: 'cat-spor',
          kategoriler: categories,
        );
        expect(socialSport.map((e) => e.id), ['2']);
      },
    );

    test('Sayfalama doğru aralıkları ve sayfa sınırlarını hesaplar', () {
      final page1 = ListingSearchFilter.paginate(
        listings,
        page: 1,
        pageSize: 2,
      );
      expect(page1.items.length, 2);
      expect(page1.currentPage, 1);
      expect(page1.totalPages, 2);
      expect(page1.totalItems, 4);
      expect(page1.hasPrevious, isFalse);
      expect(page1.hasNext, isTrue);
      expect(page1.startItem, 1);
      expect(page1.endItem, 2);

      final page2 = ListingSearchFilter.paginate(
        listings,
        page: 2,
        pageSize: 2,
      );
      expect(page2.items.length, 2);
      expect(page2.currentPage, 2);
      expect(page2.hasPrevious, isTrue);
      expect(page2.hasNext, isFalse);
      expect(page2.startItem, 3);
      expect(page2.endItem, 4);

      final clamped = ListingSearchFilter.paginate(
        listings,
        page: 99,
        pageSize: 2,
      );
      expect(clamped.currentPage, 2);

      final empty = ListingSearchFilter.paginate(
        const [],
        page: 1,
        pageSize: 5,
      );
      expect(empty.items, isEmpty);
      expect(empty.currentPage, 1);
      expect(empty.totalPages, 1);
      expect(empty.totalItems, 0);
      expect(empty.hasPrevious, isFalse);
      expect(empty.hasNext, isFalse);
    });
  });

  group('Arama ve Sayfalama Ekran Testleri', () {
    late _TestAuth auth;
    late FakeListingsService service;
    late ListingsProvider listingsProvider;

    setUp(() async {
      auth = _TestAuth();
      service = FakeListingsService();
      service.categories
        ..clear()
        ..addAll([
          const Kategori(id: categoryId, ad: 'Spor', tur: IlanTuru.sosyal),
          const Kategori(
            id: 'study-cat',
            ad: 'Ders Çalışma',
            tur: IlanTuru.sosyal,
          ),
          const Kategori(
            id: 'lost-category',
            ad: 'Elektronik',
            tur: IlanTuru.kayip,
          ),
          const Kategori(
            id: 'found-category',
            ad: 'Kart',
            tur: IlanTuru.buluntu,
          ),
        ]);
      service.items = [
        _makeListing(
          id: 's1',
          baslik: 'Basketbol Turnuvası',
          aciklama: 'Takım arkadaşı arıyoruz.',
          yer: 'Spor Salonu',
          kategoriId: categoryId,
          tur: IlanTuru.sosyal,
          createdAt: DateTime.utc(2026, 11, 5, 10),
        ),
        _makeListing(
          id: 's2',
          baslik: 'Algoritma Çalışma Grubu',
          aciklama: 'Vize hazırlık toplantısı.',
          yer: 'Kütüphane',
          kategoriId: 'study-cat',
          tur: IlanTuru.sosyal,
          createdAt: DateTime.utc(2026, 11, 4, 10),
        ),
        _makeListing(
          id: 's3',
          baslik: 'Sabah Koşusu',
          aciklama: 'Kampüs içi koşu etkinliği.',
          yer: 'Stadyum',
          kategoriId: categoryId,
          tur: IlanTuru.sosyal,
          createdAt: DateTime.utc(2026, 11, 3, 10),
        ),
        _makeListing(
          id: 'k1',
          baslik: 'Kayıp AirPods Kulaklık',
          aciklama: 'Yemekhane önünde düştü.',
          yer: 'Merkez Yemekhane',
          kategoriId: 'lost-category',
          tur: IlanTuru.kayip,
          createdAt: DateTime.utc(2026, 11, 2, 10),
        ),
        _makeListing(
          id: 'b1',
          baslik: 'Bulunan Öğrenci Kimlik Kartı',
          aciklama: 'Güvenliğe teslim edildi.',
          yer: 'Ana Giriş Kapısı',
          kategoriId: 'found-category',
          tur: IlanTuru.buluntu,
          createdAt: DateTime.utc(2026, 11, 1, 10),
        ),
      ];

      listingsProvider = ListingsProvider(service: service);
      await listingsProvider.load();
    });

    tearDown(() {
      auth.dispose();
      listingsProvider.dispose();
    });

    Future<void> mountScreen(WidgetTester tester, Widget child) async {
      tester.view.physicalSize = const Size(390, 844);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider<AuthProvider>.value(value: auth),
            ChangeNotifierProvider<ListingsProvider>.value(
              value: listingsProvider,
            ),
          ],
          child: MaterialApp(
            theme: AppTheme.lightTheme,
            onGenerateRoute: AppRoutes.onGenerateRoute,
            home: child,
          ),
        ),
      );
      await tester.pumpAndSettle();
    }

    testWidgets(
      'RadarFeedScreen metin arama, kategori filtreleme ve sayfalamayı çalıştırır',
      (tester) async {
        await mountScreen(tester, const RadarFeedScreen(pageSize: 2));

        // İlk sayfada 2 sosyal ilan görünür, 3. ilan 2. sayfadadır
        expect(find.text('Basketbol Turnuvası'), findsOneWidget);
        expect(service.queries.last.tur, IlanTuru.sosyal);
        expect(service.queries.last.page, 1);
        expect(service.queries.last.pageSize, 2);
        expect(service.queries.last.activeSocialOnly, isTrue);
        expect(find.text('Algoritma Çalışma Grubu'), findsOneWidget);
        expect(find.text('Sabah Koşusu'), findsNothing);
        expect(find.text('Sayfa 1 / 2'), findsOneWidget);

        // Sonraki sayfaya geç
        await tester.ensureVisible(find.text('Sonraki'));
        await tester.tap(find.text('Sonraki'));
        await tester.pumpAndSettle();

        expect(find.text('Sabah Koşusu'), findsOneWidget);
        expect(find.text('Basketbol Turnuvası'), findsNothing);
        expect(find.text('Sayfa 2 / 2'), findsOneWidget);
        expect(service.queries.last.page, 2);

        // Arama yapılınca sayfa 1'e döner ve eşleşen kayıt gösterilir
        await tester.enterText(find.byType(TextField), 'Algoritma');
        await tester.pumpAndSettle();

        expect(find.text('Algoritma Çalışma Grubu'), findsOneWidget);
        expect(find.text('1 ilan bulundu'), findsOneWidget);
        expect(service.queries.last.text, 'Algoritma');
        expect(service.queries.last.page, 1);

        // Filtreleri temizle
        await tester.tap(find.text('Filtreleri Temizle'));
        await tester.pumpAndSettle();

        expect(find.text('Basketbol Turnuvası'), findsOneWidget);
        expect(tester.takeException(), isNull);
      },
    );

    testWidgets(
      'LostFoundFeedScreen tür değişiminde uyumsuz kategoriyi sıfırlar ve arama yapar',
      (tester) async {
        await mountScreen(tester, const LostFoundFeedScreen());

        expect(find.text('Kayıp AirPods Kulaklık'), findsOneWidget);
        expect(find.text('Bulunan Öğrenci Kimlik Kartı'), findsOneWidget);
        expect(service.queries.last.lostFoundOnly, isTrue);

        // Kayıp segmentini ve Elektronik kategorisini seç
        await tester.tap(find.text('Kayıp').first);
        await tester.pumpAndSettle();
        await tester.tap(find.text('Elektronik').first);
        await tester.pumpAndSettle();

        expect(find.text('Kayıp AirPods Kulaklık'), findsOneWidget);
        expect(find.text('Bulunan Öğrenci Kimlik Kartı'), findsNothing);

        // Bulunan segmentine geçince Elektronik kategorisi sıfırlanır ve Bulunan ilan görünür
        await tester.tap(find.text('Bulunan').first);
        await tester.pumpAndSettle();

        expect(find.text('Bulunan Öğrenci Kimlik Kartı'), findsOneWidget);
        expect(find.text('Kayıp AirPods Kulaklık'), findsNothing);
        expect(tester.takeException(), isNull);
      },
    );

    testWidgets(
      'ListingSearchScreen tüm türlerde arama, sayfalama ve oturum koruması sağlar',
      (tester) async {
        await mountScreen(tester, const ListingSearchScreen(pageSize: 2));

        expect(find.text('İlan Arama'), findsOneWidget);
        expect(find.text('Sayfa 1 / 3'), findsOneWidget);

        // Konuma göre arama yap
        await tester.enterText(find.byType(TextField), 'Yemekhane');
        await tester.pumpAndSettle();

        expect(find.text('Kayıp AirPods Kulaklık'), findsOneWidget);
        expect(find.text('1 ilan bulundu'), findsOneWidget);

        // Eşleşmeyen arama durumunda boş durum ve temizleme butonu
        await tester.enterText(find.byType(TextField), 'olmayan_kelime_xyz');
        await tester.pumpAndSettle();

        expect(find.text('Eşleşen İlan Bulunamadı'), findsOneWidget);
        await tester.tap(find.text('Filtreleri Temizle').first);
        await tester.pumpAndSettle();

        expect(find.text('Sayfa 1 / 3'), findsOneWidget);

        // Oturum kaybedilirse korunan arama ekranı kapatılır
        auth.loggedIn = false;
        auth.notify();
        await tester.pumpAndSettle();

        expect(find.byType(LoginScreen), findsOneWidget);
        expect(tester.takeException(), isNull);
      },
    );
  });
}
