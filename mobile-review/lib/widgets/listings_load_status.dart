import 'package:flutter/material.dart';
import '../providers/listings_provider.dart';

/// Başarısız isteği boş bir ilan listesi gibi göstermeyen ortak durum alanı.
class ListingsLoadStatus extends StatelessWidget {
  const ListingsLoadStatus({super.key, required this.provider});
  final ListingsProvider provider;

  @override
  Widget build(BuildContext context) {
    if (provider.isLoading) return const LinearProgressIndicator();
    if (provider.error == null) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          Text(provider.error!, textAlign: TextAlign.center),
          TextButton(
            onPressed: provider.load,
            child: const Text('Tekrar dene'),
          ),
        ],
      ),
    );
  }
}
