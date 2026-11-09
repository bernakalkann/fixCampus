import 'package:flutter_test/flutter_test.dart';
import 'package:fixcampus_mobile/main.dart';

void main() {
  testWidgets('FixCampusApp smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const FixCampusApp());
    await tester.pumpAndSettle();
    expect(find.byType(FixCampusApp), findsOneWidget);
  });
}

