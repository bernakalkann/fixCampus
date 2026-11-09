import 'package:flutter/material.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import 'core/network/api_client.dart';
import 'core/network/web_api_auth_client.dart';
import 'services/auth_service.dart';
import 'services/listings_service.dart';
import 'package:provider/provider.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'core/constants/app_constants.dart';
import 'providers/auth_provider.dart';
import 'providers/listings_provider.dart';
import 'routes/app_routes.dart';
import 'theme/app_theme.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initializeDateFormatting('tr_TR', null);
  const url = AppConstants.defaultSupabaseUrl;
  const key = AppConstants.defaultSupabaseAnonKey;
  AuthService? service;
  ApiClient? api;
  if (url.isNotEmpty && key.isNotEmpty) {
    final authApi = WebApiAuthClient(
      apiBaseUrl: AppConstants.defaultApiBaseUrl,
      supabaseUrl: url,
    );
    try {
      await Supabase.initialize(
        url: url,
        publishableKey: key,
        httpClient: authApi,
      );
    } catch (_) {
      // Supabase hot-restart durumunda tekrar başlatma hatasını yoksay
    }
    final client = Supabase.instance.client;
    api = ApiClient(
      baseUrl: AppConstants.defaultApiBaseUrl,
      tokenProvider: () => client.auth.currentSession?.accessToken,
    );
    service = ApiAuthService(client, api, authApi);
  }
  runApp(
    FixCampusApp(
      authService: service,
      listingsService: api == null ? null : ApiListingsService(api),
    ),
  );
}

/// fixCampus Mobil Uygulama Kök Bileşeni (Android Telefon Çerçevesi Destekli)
class FixCampusApp extends StatelessWidget {
  const FixCampusApp({super.key, this.authService, this.listingsService});
  final AuthService? authService;
  final ListingsService? listingsService;

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(
          create: (_) => AuthProvider(service: authService),
        ),
        ChangeNotifierProxyProvider<AuthProvider, ListingsProvider>(
          create: (_) => ListingsProvider(service: listingsService),
          update: (_, auth, listings) => listings!
            ..setSession(
              auth.isLoggedIn && !auth.hasSessionError
                  ? auth.currentUser?.id
                  : null,
            ),
        ),
      ],
      child: MaterialApp(
        title: AppConstants.appName,
        debugShowCheckedModeBanner: false,
        theme: AppTheme.lightTheme,
        darkTheme: AppTheme.darkTheme,
        themeMode: ThemeMode.light,
        initialRoute: AppRoutes.splash,
        onGenerateRoute: AppRoutes.onGenerateRoute,
        builder: (context, child) {
          final screenSize = MediaQuery.of(context).size;
          // Masaüstü ekranında tam boy yayılmak yerine Android telefon görünümünde göster
          if (screenSize.width > 500) {
            return Scaffold(
              backgroundColor: const Color(0xFF09090B), // Koyu masaüstü zemini
              body: Center(
                child: Container(
                  width: 412,
                  height: (screenSize.height - 40).clamp(0.0, 870.0),
                  margin: const EdgeInsets.symmetric(vertical: 20),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(32),
                    border: Border.all(
                      color: const Color(0xFF27272A),
                      width: 8,
                    ),
                    boxShadow: const [
                      BoxShadow(
                        color: Colors.black54,
                        blurRadius: 36,
                        offset: Offset(0, 16),
                      ),
                    ],
                  ),
                  child: ClipRRect(
                    borderRadius: BorderRadius.circular(24),
                    child: child ?? const SizedBox.shrink(),
                  ),
                ),
              ),
            );
          }
          return child ?? const SizedBox.shrink();
        },
      ),
    );
  }
}
