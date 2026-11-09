import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import '../../routes/app_routes.dart';
import '../../widgets/app_button.dart';

/// Kayıt Formu (Ad Soyad, E-posta, Parola, Bölüm ve Sınıf Alanları)
class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  final _bolumController = TextEditingController(text: 'Bilgisayar Mühendisliği');

  String _secilenSinif = '1. Sınıf';

  static const List<String> _sinifSecenekleri = [
    'Hazırlık',
    '1. Sınıf',
    '2. Sınıf',
    '3. Sınıf',
    '4. Sınıf',
    'Yüksek Lisans / Doktora',
  ];

  bool _obscurePassword = true;
  bool _obscureConfirmPassword = true;

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    _bolumController.dispose();
    super.dispose();
  }

  Future<void> _handleRegister() async {
    final emailText = _emailController.text.trim();
    final nameText = _nameController.text.trim();
    final passText = _passwordController.text;
    final confirmPass = _confirmPasswordController.text;

    if (!RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$').hasMatch(emailText) ||
        passText.length < 8 ||
        nameText.length < 2 ||
        nameText.length > 100) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Geçerli e-posta, ad soyad ve parola girin. Kayıt parolası en az 8 karakter olmalıdır.',
          ),
        ),
      );
      return;
    }

    if (confirmPass.isNotEmpty && confirmPass != passText) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Parolalar birbiriyle eşleşmiyor.'),
        ),
      );
      return;
    }

    final auth = context.read<AuthProvider>();
    final success = await auth.register(
      adSoyad: nameText,
      email: emailText,
      password: passText,
      bolum: _bolumController.text.trim().isNotEmpty
          ? _bolumController.text.trim()
          : 'Bilgisayar Mühendisliği',
      sinif: _secilenSinif,
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
      appBar: AppBar(
        title: const Text('Hesap Oluştur'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'Yeni Hesap',
                style: TextStyle(
                  fontSize: 22,
                  fontWeight: FontWeight.w800,
                  color: Color(0xFF18181B),
                  letterSpacing: -0.4,
                ),
              ),
              const SizedBox(height: 4),
              const Text(
                'Kampüs hesabınızı oluşturup aramıza katılın',
                style: TextStyle(
                  fontSize: 13,
                  color: Color(0xFF71717A),
                ),
              ),
              const SizedBox(height: 24),

              // Hata Çubuğu
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

              // 1. TextField: Ad Soyad
              const Text(
                'Ad Soyad',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF27272A),
                ),
              ),
              const SizedBox(height: 6),
              TextField(
                controller: _nameController,
                textCapitalization: TextCapitalization.words,
                decoration: const InputDecoration(
                  hintText: 'Ad Soyad',
                  prefixIcon: Icon(Icons.person_outline,
                      size: 20, color: Color(0xFF71717A)),
                ),
              ),
              const SizedBox(height: 16),

              // 2. TextField: Kampüs E-Postası
              const Text(
                'Kampüs E-Postası',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF27272A),
                ),
              ),
              const SizedBox(height: 6),
              TextField(
                controller: _emailController,
                keyboardType: TextInputType.emailAddress,
                decoration: const InputDecoration(
                  hintText: 'ornek@ogr.edu.tr',
                  prefixIcon: Icon(Icons.school_outlined,
                      size: 20, color: Color(0xFF71717A)),
                ),
              ),
              const SizedBox(height: 16),

              // 3. TextField: Parola
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
                  hintText: 'En az 8 karakter',
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
              const SizedBox(height: 16),

              // 4. TextField: Parola Tekrarı
              const Text(
                'Parola Tekrarı',
                style: TextStyle(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF27272A),
                ),
              ),
              const SizedBox(height: 6),
              TextField(
                controller: _confirmPasswordController,
                obscureText: _obscureConfirmPassword,
                decoration: InputDecoration(
                  hintText: 'Parolayı tekrar giriniz',
                  prefixIcon: const Icon(Icons.lock_reset_outlined,
                      size: 20, color: Color(0xFF71717A)),
                  suffixIcon: IconButton(
                    icon: Icon(
                      _obscureConfirmPassword
                          ? Icons.visibility_off_outlined
                          : Icons.visibility_outlined,
                      size: 20,
                      color: const Color(0xFF71717A),
                    ),
                    onPressed: () {
                      setState(() {
                        _obscureConfirmPassword = !_obscureConfirmPassword;
                      });
                    },
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Bölüm ve Sınıf Alanları
              Row(
                children: [
                  Expanded(
                    flex: 3,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Bölüm',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: Color(0xFF27272A),
                          ),
                        ),
                        const SizedBox(height: 6),
                        TextField(
                          controller: _bolumController,
                          decoration: const InputDecoration(
                            hintText: 'Bölümünüz',
                            prefixIcon: Icon(Icons.account_balance_outlined,
                                size: 18, color: Color(0xFF71717A)),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    flex: 2,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Text(
                          'Sınıf',
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: Color(0xFF27272A),
                          ),
                        ),
                        const SizedBox(height: 6),
                        DropdownButtonFormField<String>(
                          isExpanded: true,
                          initialValue: _secilenSinif,
                          decoration: const InputDecoration(
                            contentPadding: EdgeInsets.symmetric(
                                horizontal: 10, vertical: 15),
                          ),
                          items: _sinifSecenekleri
                              .map(
                                (s) => DropdownMenuItem(
                                  value: s,
                                  child: Text(s,
                                      style: const TextStyle(fontSize: 13)),
                                ),
                              )
                              .toList(),
                          onChanged: (val) {
                            if (val != null) {
                              setState(() {
                                _secilenSinif = val;
                              });
                            }
                          },
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 28),

              // Kayıt Butonu
              AppButton(
                label: 'Kayıt Ol',
                onPressed: _handleRegister,
                isLoading: auth.isLoading,
              ),
              const SizedBox(height: 16),

              Center(
                child: Wrap(
                  alignment: WrapAlignment.center,
                  crossAxisAlignment: WrapCrossAlignment.center,
                  children: [
                    const Text(
                      'Zaten hesabınız var mı?',
                      style: TextStyle(color: Color(0xFF71717A), fontSize: 13),
                    ),
                    TextButton(
                      onPressed: () {
                        auth.clearError();
                        Navigator.pop(context);
                      },
                      child: const Text(
                        'Giriş Yap',
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
