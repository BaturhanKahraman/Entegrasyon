---
name: postgres-performance
description: Entegrasyon için PostgreSQL performans ve şema mühendisliği — index stratejisi (FK, composite, partial IsDeleted, CreatedAt), EXPLAIN ANALYZE okuma, N+1 önleme, timestamptz/numeric tipler, MVCC/VACUUM/bloat, connection pooling (multi-tenant DB-per-tenant), pg_stat_statements, GIN/trigram text search, partitioning, EF Core→SQL verimliliği (no-tracking, compiled query, AsSplitQuery, ExecuteUpdate). DB tasarımı, index kararı, yavaş sorgu teşhisi, performans denetimi yaparken kullan. "Yavaşlığa tahammül yok."
---

# PostgreSQL Performance & Schema (Entegrasyon)

Veritabanı bu işin kalbi. Her karar **"kaç tenant × kaç satırda çalışır?"** sorusundan geçer. Tahmin değil, **ölç** (EXPLAIN ANALYZE / pg_stat_statements). Postgres MCP Pro varsa (`postgres-mcp`) canlı plan + index advisor + HypoPG simülasyonu için onu kullan.

## 1. Index stratejisi
- **FK kolonları:** Her foreign key'e index (Postgres FK'ye otomatik index AÇMAZ — join + cascade için şart).
- **Composite index sırası:** En seçici / eşitlikle filtrelenen kolon önde, range sonda. `WHERE tenant_id = ? AND created_at >= ?` → `(tenant_id, created_at)`.
- **Partial index (soft-delete):** `CREATE INDEX ... WHERE is_deleted = false` — projedeki tüm sorgular silinmemişleri filtreler; partial index hem küçük hem hızlı.
- **CreatedAt:** Tarih-aralığı filtreleri (ör. export `dateFrom/dateTo`) için `created_at`'e index; çok satırda seq scan'i önler.
- **Covering / INCLUDE:** Sık okunan ek kolonları `INCLUDE (...)` ile indexe koy → index-only scan.
- **Fazlalıktan kaçın:** Duplicate/unused index yazmayı yavaşlatır + bloat. `pg_stat_user_indexes.idx_scan = 0` olanları ayıkla.

## 2. EXPLAIN ANALYZE okuma
- `EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT) <sorgu>`. Bak: **Seq Scan** (büyük tabloda kötü) vs **Index Scan/Only**; **rows estimated vs actual** (sapma = istatistik/uygun-index yok); **Buffers** (shared hit vs read = cache); **nested loop** yüksek satırda patlar.
- EF'in ürettiği SQL'i gör: `query.ToQueryString()` veya `LogTo`/`EnableSensitiveDataLogging` (dev). Plan körlemesine yapılmaz.

## 3. N+1 ve verimli sorgu (EF Core)
- **Projection:** Mümkünse entity yerine DTO'ya `Select` (Mapperly) — sadece gereken kolonlar, navigation lazılığı yok.
- **Include patlaması:** Birden çok koleksiyon `Include` → kartezyen join; `AsSplitQuery()` ile böl.
- **Default no-tracking** (proje zaten böyle) okuma için; yazma gerekiyorsa bilinçli tracking.
- **Toplu işlem:** `ExecuteUpdateAsync`/`ExecuteDeleteAsync` (tek SQL) — ama soft-delete semantiğini koru (gerçek delete yerine `IsDeleted=true`).
- **Hot-path compiled query:** Çok çağrılan sorgularda `EF.CompileAsyncQuery` ile plan/derleme maliyetini düşür.
- Client-eval'e düşme: translate edilemeyen ifade (özel C# metodu) tüm satırı belleğe çeker.

## 4. Tipler (Postgres)
- **Zaman:** `timestamptz` (UTC) — proje `SaveChangesAsync`'te UTC'ye çeviriyor, kolon `timestamptz` olmalı.
- **Para:** `numeric(18,2)` — asla `float/double`.
- **Metin:** `text` (varchar(n) ile fark yok, gereksiz limit koyma); aranabilir alanlarda trigram/GIN.
- **Yapısal:** `jsonb` (json değil) — index'lenebilir.

## 5. MVCC / VACUUM / bloat
- Update/delete ölü tuple bırakır → autovacuum temizler. Yoğun update'li tabloda bloat'a dikkat; `pg_stat_user_tables.n_dead_tup` izle.
- **HOT update:** Index'lenmeyen kolon güncellemesi daha ucuz; gereksiz kolonu index'leme.
- Büyük silmelerde autovacuum ayarı / manuel `VACUUM (ANALYZE)`.

## 6. Connection pooling (multi-tenant DB-per-tenant — KRİTİK)
- DB-per-tenant'ta her tenant ayrı DB → **ayrı pool**. N tenant × pool size patlayabilir (Postgres `max_connections` ~100 default). Npgsql pool size'ı tenant sayısına göre planla; gerekiyorsa **PgBouncer** (transaction pooling).
- Npgsql **multiplexing** yüksek eşzamanlılıkta connection verimliliğini artırır.

## 7. Teşhis araçları
- `pg_stat_statements` — en pahalı/sık sorgular (total_exec_time, calls, mean). Hot-path'i bununla bul.
- Postgres MCP Pro: `analyze_workload` / index advisor + HypoPG ile **gerçekte index oluşturmadan** kazancı simüle et.
- `pg_stat_user_indexes` (unused index), `pg_stat_user_tables` (seq_scan oranı, dead tuples).

## 8. Text search (ürün arama)
- LIKE '%x%' index kullanamaz → **trigram** (`pg_trgm` + GIN index) veya `tsvector`/GIN full-text. Storefront ürün aramada bu fark gece-gündüz.

## 9. Partitioning (büyüyünce)
- Log/sipariş/aktivite gibi zaman-temelli büyüyen tablolar → `RANGE (created_at)` partition; eski partition'ı ucuz arşivle/sil.

## Çıktı disiplini
Bir performans bulgusu raporlarken: **yer** (tablo/sorgu/dosya) · **kanıt** (EXPLAIN/pg_stat satırı) · **fix** (index DDL / EF değişikliği) · **ölçülebilir kazanım** (tahmini plan farkı). Şema değişikliği → migration (strict-rule) + TL onayı. EF Core API'sini `microsoft-docs`/MS Docs MCP ile doğrula.
