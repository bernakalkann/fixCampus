import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:supabase/supabase.dart';
import 'package:fixcampus_mobile/core/network/api_client.dart';
import 'package:fixcampus_mobile/core/network/web_api_auth_client.dart';
import 'package:fixcampus_mobile/services/auth_service.dart';

Map<String, dynamic> sessionBody({
  String name = 'Berna',
  String refresh = 'refresh-1',
  int expiresIn = 3600,
}) {
  final now = DateTime.now().millisecondsSinceEpoch ~/ 1000;
  String encode(Map<String, dynamic> data) =>
      base64Url.encode(utf8.encode(jsonEncode(data))).replaceAll('=', '');
  final token =
      '${encode({'alg': 'HS256', 'typ': 'JWT'})}.${encode({'sub': '00000000-0000-4000-8000-000000000001', 'exp': now + expiresIn, 'iat': now})}.test-signature';
  return {
    'access_token': token,
    'refresh_token': refresh,
    'token_type': 'bearer',
    'expires_in': expiresIn,
    'user': {
      'id': '00000000-0000-4000-8000-000000000001',
      'aud': 'authenticated',
      'email': 'berna@example.com',
      'app_metadata': {},
      'user_metadata': {'ad_soyad': name},
      'created_at': '2026-10-26T08:00:00Z',
    },
  };
}

void main() {
  test(
    'Kayıt/giriş/SDK yenilemesi/çıkışı kendi API DTO ve uçlarını kullanır',
    () async {
      final requests = <http.Request>[];
      final transport = WebApiAuthClient(
        apiBaseUrl: 'https://our-api.test',
        supabaseUrl: 'https://project.supabase.co',
        inner: MockClient((request) async {
          requests.add(request);
          expect(request.url.origin, 'https://our-api.test');
          expect(request.headers.containsKey('apikey'), false);
          if (request.url.path.endsWith('/cikis')) {
            return http.Response('', 204);
          }
          return http.Response(
            jsonEncode(
              sessionBody(
                refresh: request.url.path.endsWith('/yenile')
                    ? 'refresh-2'
                    : 'refresh-1',
              ),
            ),
            200,
            headers: {'content-type': 'application/json'},
          );
        }),
      );
      final client = SupabaseClient(
        'https://project.supabase.co',
        'public-key',
        httpClient: transport,
        authOptions: const AuthClientOptions(autoRefreshToken: false),
      );
      final auth = ApiAuthService(client, ApiClient(), transport);
      await auth.signUp('Berna', 'berna@example.com', 'password123');
      expect(jsonDecode(requests.last.body), {
        'adSoyad': 'Berna',
        'email': 'berna@example.com',
        'password': 'password123',
      });
      await auth.signIn('berna@example.com', 'password123');
      expect(jsonDecode(requests.last.body), {
        'email': 'berna@example.com',
        'password': 'password123',
      });
      await client.auth.refreshSession();
      expect(jsonDecode(requests.last.body), {'refreshToken': 'refresh-1'});
      expect(client.auth.currentSession?.refreshToken, 'refresh-2');
      final signedToken = client.auth.currentSession!.accessToken;
      await auth.signOut();
      expect(requests.last.headers['Authorization'], 'Bearer $signedToken');
      expect(client.auth.currentSession == null, true);
      expect(requests.map((r) => r.url.path).toList(), [
        '/api/auth/kayit',
        '/api/auth/giris',
        '/api/auth/yenile',
        '/api/auth/cikis',
      ]);
      await client.dispose();
    },
  );

  test('API geçersiz parola kodu korunur; sahte oturum açılmaz', () async {
    final transport = WebApiAuthClient(
      apiBaseUrl: 'https://our-api.test',
      supabaseUrl: 'https://project.supabase.co',
      inner: MockClient(
        (_) async => http.Response(
          jsonEncode({
            'code': 'invalid_credentials',
            'message': 'Hatalı giriş',
          }),
          400,
          headers: {'content-type': 'application/json; charset=utf-8'},
        ),
      ),
    );
    final client = SupabaseClient(
      'https://project.supabase.co',
      'public-key',
      httpClient: transport,
      authOptions: const AuthClientOptions(autoRefreshToken: false),
    );
    final auth = ApiAuthService(client, ApiClient(), transport);
    await expectLater(
      auth.signIn('b@example.com', 'wrong'),
      throwsA(
        isA<AuthException>().having(
          (e) => e.code,
          'code',
          'invalid_credentials',
        ),
      ),
    );
    expect(auth.hasSession, false);
    await client.dispose();
  });

  test('E-posta onayı gerektiren kayıt sahte oturum üretmez', () async {
    final transport = WebApiAuthClient(
      apiBaseUrl: 'https://our-api.test',
      supabaseUrl: 'https://project.supabase.co',
      inner: MockClient(
        (_) async =>
            http.Response(jsonEncode({'user': sessionBody()['user']}), 200),
      ),
    );
    final client = SupabaseClient(
      'https://project.supabase.co',
      'public-key',
      httpClient: transport,
      authOptions: const AuthClientOptions(autoRefreshToken: false),
    );
    await ApiAuthService(
      client,
      ApiClient(),
      transport,
    ).signUp('Berna', 'b@example.com', 'password123');
    expect(client.auth.currentSession == null, true);
    await client.dispose();
  });

  test(
    'Tanımlanmamış SDK Auth çağrısı doğrudan Supabase ağına çıkamaz',
    () async {
      var sent = 0;
      final transport = WebApiAuthClient(
        apiBaseUrl: 'https://our-api.test',
        supabaseUrl: 'https://project.supabase.co',
        inner: MockClient((_) async {
          sent++;
          return http.Response('{}', 200);
        }),
      );
      await expectLater(
        transport.get(Uri.parse('https://project.supabase.co/auth/v1/user')),
        throwsA(isA<UnsupportedError>()),
      );
      expect(sent, 0);
      transport.close();
    },
  );

  test(
    'Storage gibi Auth dışı SDK servislerinin adresi değiştirilmez',
    () async {
      final uri = Uri.parse(
        'https://project.supabase.co/storage/v1/object/public/image.png',
      );
      final transport = WebApiAuthClient(
        apiBaseUrl: 'https://our-api.test',
        supabaseUrl: 'https://project.supabase.co',
        inner: MockClient((r) async {
          expect(r.url, uri);
          return http.Response('ok', 200);
        }),
      );
      expect((await transport.get(uri)).body, 'ok');
      transport.close();
    },
  );

  test('SDK refresh reddedilince yerel oturum temizlenir', () async {
    final transport = WebApiAuthClient(
      apiBaseUrl: 'https://our-api.test',
      supabaseUrl: 'https://project.supabase.co',
      inner: MockClient(
        (r) async => r.url.path.endsWith('/yenile')
            ? http.Response(
                jsonEncode({
                  'code': 'refresh_token_not_found',
                  'message': 'Geçersiz oturum',
                }),
                400,
              )
            : http.Response('', 204),
      ),
    );
    final client = SupabaseClient(
      'https://project.supabase.co',
      'public-key',
      httpClient: transport,
      authOptions: const AuthClientOptions(autoRefreshToken: false),
    );
    await client.auth.recoverSession(jsonEncode(sessionBody()));
    await expectLater(
      client.auth.refreshSession(),
      throwsA(isA<AuthException>()),
    );
    expect(client.auth.currentSession == null, true);
    await client.dispose();
  });
  test(
    'SDK otomatik yenileme zamanlayıcısı da Web API yenile ucunu kullanır',
    () async {
      final paths = <String>[];
      final transport = WebApiAuthClient(
        apiBaseUrl: 'https://our-api.test',
        supabaseUrl: 'https://project.supabase.co',
        inner: MockClient((r) async {
          paths.add(r.url.path);
          expect(r.url.origin, 'https://our-api.test');
          return http.Response(
            jsonEncode(sessionBody(refresh: 'refresh-2')),
            200,
            headers: {'content-type': 'application/json'},
          );
        }),
      );
      final client = SupabaseClient(
        'https://project.supabase.co',
        'public-key',
        httpClient: transport,
        authOptions: const AuthClientOptions(autoRefreshToken: true),
      );
      await client.auth.recoverSession(jsonEncode(sessionBody(expiresIn: 35)));
      final renewed = client.auth.onAuthStateChange.firstWhere(
        (state) => state.session?.refreshToken == 'refresh-2',
      );
      client.auth.startAutoRefresh();
      await renewed.timeout(const Duration(seconds: 5));
      client.auth.stopAutoRefresh();
      expect(paths, ['/api/auth/yenile']);
      await client.dispose();
    },
  );
}

