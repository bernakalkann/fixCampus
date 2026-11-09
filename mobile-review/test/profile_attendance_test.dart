import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:fixcampus_mobile/providers/auth_provider.dart';
import 'auth_provider_test.dart' show FakeAuthService;
import 'support/listings_fakes.dart';
import 'package:fixcampus_mobile/providers/listings_provider.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:fixcampus_mobile/screens/profile/profile_screen.dart';
import 'package:fixcampus_mobile/widgets/listing_card.dart';

void main() {
  setUpAll(() async {
    await initializeDateFormatting('tr_TR', null);
  });

  testWidgets(
    'Profil ekranında ListingCard katılımcı sayısını ve durumunu Provider dan alır',
    (WidgetTester tester) async {
      SharedPreferences.setMockInitialValues({});
      final authProvider = AuthProvider(
        service: FakeAuthService()..hasSession = true,
      );
      await authProvider.initialize();
      final listingsProvider = ListingsProvider(service: FakeListingsService());
      await listingsProvider.load();

      await tester.pumpWidget(
        MultiProvider(
          providers: [
            ChangeNotifierProvider.value(value: authProvider),
            ChangeNotifierProvider.value(value: listingsProvider),
          ],
          child: const MaterialApp(home: ProfileScreen()),
        ),
      );

      await tester.pumpAndSettle();

      // Profilde listelenen ListingCard'ları bul
      final listingCardFinder = find.byType(ListingCard);
      expect(listingCardFinder, findsWidgets);

      // İlk kartı incele
      final ListingCard firstCard = tester.widget(listingCardFinder.first);
      final expectedCount = listingsProvider.getAttendanceCount(
        firstCard.ilan.id,
      );
      final expectedAttending = listingsProvider.isUserAttending(
        firstCard.ilan.id,
        authProvider.currentUser?.id ?? '',
      );

      // Kartın parametreleri sıfır değil, provider'daki gerçek değerler olmalı
      expect(firstCard.attendanceCount, expectedCount);
      expect(firstCard.isAttending, expectedAttending);

      // Kart üzerinde katılımcı sayısı yazısının doğru render edildiğini doğrula
      expect(find.text('$expectedCount katılımcı'), findsWidgets);
    },
  );
}

