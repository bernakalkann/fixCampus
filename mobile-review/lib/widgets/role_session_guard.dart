import 'dart:async';

import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../providers/auth_provider.dart';
import '../screens/auth/login_screen.dart';
import '../security/role_session_policy.dart';

/// API üzerinden doğrulanan oturum ve rol durumunu ekranlara uygular.
class RoleSessionGuard extends StatefulWidget {
  const RoleSessionGuard({
    super.key,
    required this.child,
    this.adminOnly = false,
  });

  final Widget child;
  final bool adminOnly;

  @override
  State<RoleSessionGuard> createState() => _RoleSessionGuardState();
}

class _RoleSessionGuardState extends State<RoleSessionGuard>
    with WidgetsBindingObserver {
  bool _hadVerifiedContent = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state != AppLifecycleState.resumed || !mounted) return;
    final auth = context.read<AuthProvider>();
    if (auth.isLoggedIn && !auth.isLoading) {
      // Arka planda değişmiş olabilecek rolü API'den yeniden oku.
      unawaited(auth.initialize());
    }
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();
    final access = const RoleSessionPolicy().evaluate(
      hasSession: auth.isLoggedIn,
      isLoading: auth.isLoading,
      profile: auth.currentUser,
      hasError: auth.hasSessionError,
      adminOnly: widget.adminOnly,
    );

    final keepContent =
        access == RoleAccess.allowed ||
        (access == RoleAccess.checking && _hadVerifiedContent);
    if (access != RoleAccess.checking) {
      _hadVerifiedContent = access == RoleAccess.allowed;
    }

    return Stack(
      fit: StackFit.expand,
      children: [
        if (keepContent)
          Offstage(
            offstage: access != RoleAccess.allowed,
            child: TickerMode(
              enabled: access == RoleAccess.allowed,
              child: widget.child,
            ),
          ),
        if (access != RoleAccess.allowed) _accessView(access, auth),
      ],
    );
  }

  Widget _accessView(RoleAccess access, AuthProvider auth) {
    switch (access) {
      case RoleAccess.signInRequired:
        return const LoginScreen();
      case RoleAccess.checking:
        return const Scaffold(
          body: SafeArea(
            child: Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  CircularProgressIndicator(),
                  SizedBox(height: 16),
                  Text('Oturum ve rol doğrulanıyor.'),
                ],
              ),
            ),
          ),
        );
      case RoleAccess.profileRequired:
        return Scaffold(
          appBar: AppBar(title: const Text('Profil doğrulanamadı')),
          body: SafeArea(
            child: Center(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      auth.message ??
                          'Profiliniz okunmadan bu ekrana erişilemez.',
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 16),
                    ElevatedButton(
                      onPressed: () => unawaited(auth.initialize()),
                      child: const Text('Tekrar dene'),
                    ),
                    TextButton(
                      onPressed: () => unawaited(auth.logout()),
                      child: const Text('Çıkış yap'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        );
      case RoleAccess.forbidden:
        return Scaffold(
          appBar: AppBar(title: const Text('Erişim yetkisi yok')),
          body: const SafeArea(
            child: Center(
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Text(
                  'Bu ekran için yönetici yetkisi gerekir.',
                  textAlign: TextAlign.center,
                ),
              ),
            ),
          ),
        );
      case RoleAccess.allowed:
        return const SizedBox.shrink();
    }
  }
}
