# Kullanıcı Geri Bildirimi — 2026-06-10 (8085 dev testi)

Kullanıcı dev ortamında (8085) gezerken topladığı bulgular. PA bunları `tasks.json`'a
formalize edecek. **#1 öncelik kullanıcı tarafından: TOKEN YÖNETİMİ.**

## 🔴 Bug'lar (dev 8085'te görüldü)
1. `/sales` — runtime hata (kullanıcı ekran çıktısı yapıştırdı; tam metin alınacak / sayfa açılışı incelenecek).
2. `/branch-offices/1` — runtime hata (yapıştırılan çıktı; incelenecek).
3. **Depo ekleme yok** — branch-offices'te "depo ekle" butonu/sayfası görünmüyor.
4. **nav-active bug (pazaryeri menüsü):** Senkronizasyon + Ürün Eşleştirme alt sayfaları soldaki menüde hep "Senkronizasyon"u aktif gösteriyor.
5. **nav-active bug (storefront):** Aynı menü-asılı-kalma sorunu storefront yolunda da var (sadece ilk sayfada aktif kalıyor).
6. **Müşteri ekle yok** — Müşteriler sayfasında "müşteri ekle" görünmüyor.
7. **Vergi ayarları varsayılan:** Varsayılan değiştirme çalışmıyor — seçtikten sonra "varsayılan" etiketi kayboluyor.
8. **Ödeme yöntemleri sürükle-bırak çalışmıyor** (SortableJS bağlanmamış olabilir).
9. **`badge bg-XXXX` eksik kullanımları:** Solid badge'ler `bg-X-lt` veya `bg-X text-X-fg` olmalı (Tabler strict). Tüm `badge bg-*` taranıp düzeltilecek.
10. **Kategori `["Brand"]="Brands"` artefaktı** — ✅ ÇÖZÜLDÜ (`e6932fe6`).

## 🟡 UX / İyileştirme
11. **Türkçe karakter:** Önceki sayfalarda Türkçe karakter kullanılmamış. Bundan sonra oluşturulan/düzenlenen tüm sayfalarda tam Türkçe karakter (tam izin). → Designer/SWE agent kuralı yapılacak.
12. **Tablo satırları tıklanabilir:** En sağ kolonda düzenle/sil/detay yerine satırın kendisi tıklanabilir olsun; aksiyonlar detay içine alınsın.
13. **Smoke/E2E:** Smoke testimiz yok. En azından ana sayfaları (sales, branch-offices, customers, sync, settings...) "açılıyor mu" E2E smoke testine koy.
14. **Dev observability:** Sync sayfaları arası hızlı gezinince yüklemede takılma var. Dev'de derin loglama aç + gecikmeleri TL'nin izleyebileceği yere (Grafana/Loki zaten var) akıt → latency tespiti.
15. **Sidebar:** En alta "Sohbetler" + yeni "Yardım" bölümü taşı. Yardım formundan girilenler (hata raporu/yardım) Admin panele düşsün.
16. **TablerUI skill:** Tüm Tabler sayfaları gezilip skill oluşturulacak. (Doküman/sayfaya ulaşılamazsa kullanıcı ayrı session'da kuracak — TL haber verecek.)

## 🟢 Büyük özellikler
17. **Canlı kullanıcı takibi (admin):** Kullanıcı detay sayfasından — bağlantı durumu, ne süredir aktif/pasif, kaç ürün satmış/eklemiş/silmiş vb. canlı izleme.
18. **Şifre sıfırlama (admin):** Admin kullanıcının şifresini sistemden sıfırlayabilsin.
19. **Kullanıcı tepkileri (güvenlik):** Şifre sıfırlanan/pasifleştirilen/silinen kullanıcı OTOMATİK logout olsun — pasif/silinmiş hesap kullanılamaz (aktif session geçersiz kılınmalı).

## ⚙️ Altyapı / Süreç
20. **TOKEN YÖNETİMİ (EN ÖNEMLİ):** Takım token'ı çok hızlı tükeniyor. Kök neden hipotezi: kalıcı teammate'lere arka arkaya iş verilince context şişiyor (her mesajda tüm geçmiş yeniden işleniyor). Çözüm birlikte kararlaştırılacak — efemeral görev-başı ajan modeli öneriliyor.
21. **Graphify (develop):** Hook zaten aktif (commit'lerde `[graphify hook] launching background rebuild` görülüyor). Develop'ta çalışıyor — doğrulanacak.

## Açık (kullanıcıdan istenecek pasted-error metinleri)
- `/sales` Pasted text #1 (+8 satır)
- `/branch-offices/1` Pasted text #2 (+13 satır)
