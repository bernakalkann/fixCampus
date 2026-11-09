import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/constants/app_constants.dart';
import '../../providers/auth_provider.dart';
import '../../routes/app_routes.dart';

/// Açılış ve Oturum Yönlendirme Ekranı
class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen> {
  @override
  void initState() {
    super.initState();
    _navigateToNext();
  }

  Future<void> _navigateToNext() async {
    await Future<void>.delayed(Duration.zero);
    if (!mounted) return;
    final auth = context.read<AuthProvider>();
    await auth.initialize();
    if (!mounted) return;
    Navigator.pushReplacementNamed(
      context,
      auth.isLoggedIn ? AppRoutes.main : AppRoutes.login,
    );
  }

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      backgroundColor: Colors.white,
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              AppConstants.appName,
              style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.w800,
                color: Color(0xFF18181B),
                letterSpacing: -0.8,
              ),
            ),
            SizedBox(height: 6),
            Text(
              'Kampüs Sosyal Buluşma ve Yardımlaşma',
              style: TextStyle(
                fontSize: 13,
                color: Color(0xFF71717A),
                fontWeight: FontWeight.w500,
              ),
            ),
            SizedBox(height: 36),
            SizedBox(
              width: 20,
              height: 20,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                valueColor: AlwaysStoppedAnimation<Color>(Color(0xFF18181B)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
