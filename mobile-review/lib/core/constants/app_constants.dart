import 'package:flutter/foundation.dart';

/// fixCampus Mobil Uygulama Sabitleri
class AppConstants {
  static const String appName = 'fixCampus';
  static const String appTagline =
      'Kampüs Sosyal Buluşma ve Kayıp-Buluntu Platformu';
  static const String appVersion = '1.0.0';

  // Supabase Bağlantı Varsayılanları (Yerel geliştirme)
  static const String defaultSupabaseUrl = String.fromEnvironment(
    'SUPABASE_URL',
    defaultValue: 'https://nfbmanrrhjuynaarozzs.supabase.co',
  );

  static const String defaultSupabaseAnonKey = String.fromEnvironment(
    'SUPABASE_ANON_KEY',
    defaultValue:
        'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.e30.fixcampus_dev_anon_key',
  );

  // API Bağlantı Varsayılanları (Yerel geliştirme)
  // Android Emülatör: http://10.0.2.2:5168
  // Fiziksel Cihaz veya Web: http://localhost:5168 veya yerel IP
  static String get defaultApiBaseUrl {
    const fromEnv = String.fromEnvironment('API_BASE_URL');
    if (fromEnv.isNotEmpty) return fromEnv;
    return kIsWeb ? 'http://localhost:5168' : 'http://10.0.2.2:5168';
  }

  // Supabase Varsayılanları (Geliştirme aşamasında konfigüre edilir)
  static const String supabaseUrlPlaceholder =
      'https://your-project.supabase.co';
  static const String supabaseAnonKeyPlaceholder = 'your-anon-key';

  // Depolama Bucket Adı
  static const String listingsBucket = 'ilan-fotograflari';
}
