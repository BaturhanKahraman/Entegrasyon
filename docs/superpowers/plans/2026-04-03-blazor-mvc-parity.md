# Blazor→MVC Feature Parity Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Blazor'da olup MVC'de eksik/stub olan tum ozellikleri MVC'ye tasimak.

**Architecture:** Mevcut MVC controller + HTMX + Tabler UI pattern'i kullanilacak. Blazor'daki dialog'lar MVC'de modal veya partial view olarak implemente edilecek. Business logic zaten business layer'da — sadece UI tasiniyor.

**Tech Stack:** ASP.NET Core MVC, HTMX, Tabler UI, Vanilla JS

---

## Faz 1 — Gunluk Kullanim (Kritik)

### Task 1: Products — Excel Export
Blazor'da Products.razor'da "Excel'e Aktar" butonu vardi.
MVC ProductController'a Export action ekle, ClosedXML ile Excel uret.

### Task 2: Products — Copy/Duplicate
ProductDetail'da "Kopyala" butonu. Urun bilgilerini kopyalayip yeni urun olusturur.
MVC ProductController'a Duplicate action ekle.

### Task 3: Products — Discount Dialog
ProductDetail'da indirim uygulama dialog'u. Yuzde giris, canli onizleme, marketplace fiyat karsilastirma.
MVC'de modal form + HTMX partial olarak implement et.

### Task 4: Products — Barcode Print
ProductDetail'da barkod yazdirma. PrintAgent ile entegrasyon.
MVC'de PrintBarcode action + modal dialog.

### Task 5: Orders — Eksik Detay Ozellikleri
OrderDetail'da: cargo tracking link (tiklanabilir), "Tedarik Edilemez" butonu, detayli status cards.

### Task 6: Invoicing — Dynamic Line Items + Bulk
CreateInvoice'da: integrator secimi, dynamic satir ekleme/silme.
BulkInvoice: toplu fatura olusturma.

### Task 7: Dashboard — Profit Chart + Quick Actions
DashboardProfitChart (30 gunluk trend) ve QuickActions paneli.

## Faz 2 — Marketplace (Is Kritik)

### Task 8: TrendyolProductSend — Tam Workflow
Stub'i tam implemente et: preflight checks, override form, pricing panel, payload preview, send action.

### Task 9: ProductSync Detail — Marketplace Panels
Marketplace-specific detail panelleri (Trendyol, Hepsiburada, N11) + ProductActivityTimeline.

### Task 10: BulkCategoryMatch — Auto-Match
Auto-match suggestions, bulk mapping, progress dialog.

### Task 11: MatchedEntityImport — Tam UI
Search, filter, import, conflict resolution dialog.

### Task 12: Template System
Mapping template save/load/apply — CategorySync ve AttributeSync icin.

## Faz 3 — Operasyonel

### Task 13: BulkOperations — Gelismis Import/Export
Column mapping wizard, validation preview, export column selector.

### Task 14: BranchOffices — Stock Transfer
Subeler arasi stok transfer dialog'u.

### Task 15: Attributes — Editable Detail Panel
Ozellik detay panelinde duzenleme, deger yonetimi.

### Task 16: POS — Payment + Session Dialogs
Odeme yontemi secim dialog'u, session kapatma dialog'u, mixed payment.

### Task 17: Sales — POS Entry Interface
Barkod tarama, sepet, odeme — Sales sayfasinda satis girisi.

### Task 18: Categories Import — Eksikler
Temu marketplace, secim paneli, interaktif tree UI.

## Faz 4 — Storefront (13 sayfa)

### Task 19-31: Storefront Sayfalari
Her stub sayfayi implement et:
19. StorefrontBanners
20. StorefrontPayment  
21. StorefrontLegal
22. StorefrontSellers
23. StorefrontReviews
24. StorefrontReturns
25. StorefrontPayouts
26. StorefrontMessages
27. StorefrontNewsletter
28. StorefrontCampaigns
29. StorefrontSizeGuides
30. StorefrontEmailCampaigns
31. StorefrontSettings (tamamla)

## Faz 5 — Nice-to-Have

### Task 32: Label Designer
Visual label designer (canvas, toolbox, properties) — karmasik, ayri sprint.

### Task 33: Desktop App Settings
Download linkleri, kurulum talimatlari.

### Task 34: AttributeSync — Value Matching
Deger eslestirme paneli.

### Task 35: BrandMapping — Interactive Detail
Interaktif detay paneli.

### Task 36: CommissionRates — Calculator
Komisyon hesaplama paneli.
