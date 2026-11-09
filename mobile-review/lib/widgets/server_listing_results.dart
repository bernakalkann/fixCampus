import 'dart:convert';

import 'package:flutter/material.dart';
import '../providers/listings_provider.dart';
import '../services/api_exception.dart';
import '../services/listings_service.dart';
import '../utils/listing_search_filter.dart';

/// Her ekran kendi sorgusunu tutar; eski filtre/hesap yanıtı yeni sonucu ezemez.
class ServerListingResults extends StatefulWidget {
  const ServerListingResults({
    super.key,
    required this.provider,
    required this.query,
    required this.builder,
    this.onPageCorrected,
  });

  final ListingsProvider provider;
  final ListingQuery query;
  final ValueChanged<int>? onPageCorrected;
  final Widget Function(
    ListingPaginationResult page,
    bool loading,
    String? error,
    Future<void> Function() refresh,
  )
  builder;

  @override
  State<ServerListingResults> createState() => _ServerListingResultsState();
}

class _ServerListingResultsState extends State<ServerListingResults> {
  ListingPage? _page;
  String? _error;
  bool _loading = true;
  int _request = 0;
  int? _pageOverride;
  late String _signature;

  String get _currentSignature => jsonEncode([
    widget.query.copyWith(page: _pageOverride).toQueryParameters(),
    widget.provider.dataRevision,
    widget.provider.sessionRevision,
  ]);

  @override
  void initState() {
    super.initState();
    _signature = _currentSignature;
    Future.microtask(_fetch);
  }

  @override
  void didUpdateWidget(covariant ServerListingResults oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (jsonEncode(oldWidget.query.toQueryParameters()) !=
        jsonEncode(widget.query.toQueryParameters())) {
      _pageOverride = null;
    }
    final next = _currentSignature;
    if (next != _signature || widget.provider != oldWidget.provider) {
      _signature = next;
      ++_request;
      _page = null;
      _error = null;
      _loading = true;
      Future.microtask(_fetch);
    }
  }

  Future<void> _fetch() async {
    if (!mounted) return;
    final version = ++_request;
    final provider = widget.provider;
    var query = widget.query.copyWith(page: _pageOverride);
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      var page = await provider.searchListings(query);
      if (!mounted || version != _request) return;
      // Silme sırasında son sayfa kaybolabilir. Hedef her turda küçülür;
      // boş veri kümesinde veya geçerli bir sayfada yeniden sorgulama yapılmaz.
      while (page.items.isEmpty &&
          page.totalCount > 0 &&
          page.totalPages >= 1 &&
          query.page > page.totalPages) {
        query = query.copyWith(page: page.totalPages);
        page = await provider.searchListings(query);
        if (!mounted || version != _request) return;
      }
      if (query.page != widget.query.page) {
        _pageOverride = query.page;
        _signature = _currentSignature;
      }
      setState(() {
        _page = page;
      });
      if (query.page != widget.query.page) {
        widget.onPageCorrected?.call(query.page);
      }
    } catch (error) {
      if (!mounted || version != _request) return;
      setState(() {
        _page = null;
        _error = error is ApiException
            ? error.message
            : 'İlanlar yüklenemedi. Tekrar deneyin.';
      });
    } finally {
      if (mounted && version == _request) {
        setState(() {
          _loading = false;
        });
      }
    }
  }

  Future<void> _refresh() async {
    await widget.provider.load();
    if (mounted) await _fetch();
  }

  @override
  Widget build(BuildContext context) {
    final page = _page;
    return widget.builder(
      ListingPaginationResult(
        items: page?.items ?? const [],
        currentPage: page?.page ?? 1,
        totalPages: page?.totalPages ?? 1,
        totalItems: page?.totalCount ?? 0,
        pageSize: page?.pageSize ?? widget.query.pageSize,
      ),
      _loading,
      _error,
      _refresh,
    );
  }
}

class ListingQueryStatus extends StatelessWidget {
  const ListingQueryStatus({
    super.key,
    required this.loading,
    required this.error,
    required this.onRetry,
  });
  final bool loading;
  final String? error;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    if (loading) return const LinearProgressIndicator();
    if (error == null) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        children: [
          Text(error!, textAlign: TextAlign.center),
          TextButton(onPressed: onRetry, child: const Text('Tekrar dene')),
        ],
      ),
    );
  }
}
