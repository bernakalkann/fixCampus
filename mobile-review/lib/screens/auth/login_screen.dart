import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/constants/app_constants.dart';
import '../../providers/auth_provider.dart';
import '../../routes/app_routes.dart';
import '../../widgets/app_button.dart';

/// Giriş Ekranı (Öğrenci / Yönetici Seçimi, Form Validasyonu ve Hata Yönetimi)
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();

  bool _isStudent = true; // true: Öğrenci, false: Yönetici
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  void _onRoleChanged(bool isStudentSelected) {
    if (_isStudent == isStudentSelected) return;
    setState(() {
      _isStudent = isStudentSelected;
    });
  }

  Future<void> _handleLogin() async {
    final emailText = _emailController.text.trim();
    final passText = _passwordController.text;

    if (!RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(emailText) ||
        passText.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Geçerli bir e-posta adresi ve parola girin.'),
        ),
      );
      return;
    }

    final auth = context.read<AuthProvider>();
    final success = await auth.login(
      email: emailText,
      password: passText,
      asAdmin: !_isStudent,
    );

    if (!mounted) return;

    if (success && auth.isLoggedIn) {
      Navigator.pushNamedAndRemoveUntil(context, AppRoutes.main, (_) => false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return Scaffold(
      backgroundColor: Colors.white,
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 40),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 20),
              const Text(
                AppConstants.appName,
                style: TextStyle(
                  fontSize: 24,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF18181B),
                  letterSpacing: -0.5,
                ),
              ),
              const SizedBox(height: 6),
              const Text(
                'Hesap türünüzü seçip giriş yapın',
                style: TextStyle(
                  fontSize: 13,
                  color: Color(0xFF71717A),
                ),
              ),
              const SizedBox(height: 24),

              // Üst Kısım: Öğrenci / Yönetici Seçim Sekmesi
              Container(
                decoration: BoxDecoration(
                  color: const Color(0xFFF4F4F5),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: const Color(0xFFE4E4E7)),
                ),
                padding: const EdgeInsets.all(4),
                child: Row(
                  children: [
                    Expanded(
                      child: GestureDetector(
                        onTap: () => _onRoleChanged(true),
                        child: AnimatedContainer(
                          duration: const Duration(milliseconds: 180),
                          padding: const EdgeInsets.symmetric(vertical: 10),
                          decoration: BoxDecoration(
                            color: _isStudent ? Colors.white : Colors.transparent,
                            borderRadius: BorderRadius.circular(8),
                            boxShadow: _isStudent
                                ? const [
                                    BoxShadow(
                                      color: Color(0x10000000),
                                      blurRadius: 4,
                                      offset: Offset(0, 2),
                                    ),
                                  ]
                                : null,
                          ),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(
                                Icons.school_outlined,
                                size: 18,
                                color: _isStudent
                                    ? const Color(0xFF18181B)
                                    : const Color(0xFF71717A),
                              ),
                              const SizedBox(width: 8),
                              Text(
                                'Öğrenci',
                                style: TextStyle(
                                  fontSize: 14,
                                  fontWeight: _isStudent
                                      ? FontWeight.w700
                                      : FontWeight.w500,
                                  color: _isStudent
                                      ? const Color(0xFF18181B)
                                      : const Color(0xFF71717A),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                    Expanded(
                      child: GestureDetector(
                        onTap: () => _onRoleChanged(false),
                        child: AnimatedContainer(
                          duration: const Duration(milliseconds: 180),
                          padding: const EdgeInsets.symmetric(vertical: 10),
                          decoration: BoxDecoration(
                            color: !_isStudent
                                ? const Color(0xFF18181B)
                                : Colors.transparent,
                            borderRadius: BorderRadius.circular(8),
                            boxShadow: !_isStudent
                                ? const [
                                    BoxShadow(
                                      color: Color(0x20000000),
                                      blurRadius: 4,
                                      offset: Offset(0, 2),
                                    ),
                                  ]
                                : null,
                          ),
                          child: Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(
                                Icons.shield_outlined,
                                size: 18,
                                color: !_isStudent
                                    ? Colors.white
                                    : const Color(0xFF71717A),
                              ),
                              const SizedBox(width: 8),
                              Text(
                                'Yönetici',
                                style: TextStyle(
                                  fontSize: 14,
                                  fontWeight: !_isStudent
                                      ? FontWeight.w700
                                      : FontWeight.w500,
                                  color: !_isStudent
                                      ? Colors.white
                                      : const Color(0xFF71717A),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 24),

              // Hata Bildirimi
              if (auth.message != null) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: const Color(0xFFFEF2F2),
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: const Color(0xFFFCA5A5)),
                  ),
                  child: Row(
                    children: [
                      const Icon(Icons.error_outline,
                          color: Color(0xFFDC2626), size: 20),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text(
                          auth.message!,
                          style: const TextStyle(
                              fontSize: 13, color: Color(0xFFB91C1C)),
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
              ],

              // E-posta Alanı
              Text(
                _isStudent ? 'Öğrenci E-Postası' : 'Yönetici E-Postası',
                style: const TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF27272A),
                ),
              ),
              const SizedBox(height: 6),
              TextField(
                controller: _emailController,
                keyboardType: TextInputType.emailAddress,
                decoration: InputDecoration(
                  hintText: _isStudent
                      ? 'ornek@ogr.edu.tr'
                      : 'admin@kampus.edu.tr',
                  prefixIcon: Icon(
                    _isStudent ? Icons.school_outlined : Icons.shield_outlined,
                    size: 20,
                    color: const Color(0xFF71717A),
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Parola Alanı
              const Text(
                'Parola',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF27272A),
                ),
              ),
              const SizedBox(height: 6),
              TextField(
                controller: _passwordController,
                obscureText: _obscurePassword,
                decoration: InputDecoration(
                  hintText: '••••••••',
                  prefixIcon: const Icon(Icons.lock_outline,
                      size: 20, color: Color(0xFF71717A)),
                  suffixIcon: IconButton(
                    icon: Icon(
                      _obscurePassword
                          ? Icons.visibility_off_outlined
                          : Icons.visibility_outlined,
                      size: 20,
                      color: const Color(0xFF71717A),
                    ),
                    onPressed: () {
                      setState(() {
                        _obscurePassword = !_obscurePassword;
                      });
                    },
                  ),
                ),
              ),
              const SizedBox(height: 24),

              // Giriş Butonu
              AppButton(
                label: 'Giriş Yap',
                onPressed: _handleLogin,
                isLoading: auth.isLoading,
              ),
              const SizedBox(height: 16),

              // Kayıt Ol Yönlendirmesi (Öğrenci modunda)
              if (_isStudent)
                Center(
                  child: Wrap(
                    alignment: WrapAlignment.center,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      const Text(
                        'Hesabınız yok mu?',
                        style: TextStyle(color: Color(0xFF71717A), fontSize: 13),
                      ),
                      TextButton(
                        onPressed: () {
                          auth.clearError();
                          Navigator.pushNamed(context, AppRoutes.register);
                        },
                        child: const Text(
                          'Kayıt Ol',
                          style: TextStyle(
                            fontWeight: FontWeight.w700,
                            color: Colors.black,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
