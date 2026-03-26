# SP-4: Sepet & Odeme — Tasarim Dokumani

## Ozet

Storefront sepet ve odeme sistemi: session-based sepet (giris yapilmissa DB-backed), checkout akisi, iyzico 3D Secure odeme, siparis olusturma (MarketPlaceId=10), atomik stok dusme.

## Kapsam

**Dahil:** Cart + CartItem entity, sepet islemleri (ekle/sil/guncelle), checkout akisi, iyzico Checkout Form entegrasyonu (3D Secure), siparis olusturma (Order + OrderItem, MarketPlaceId=10), stok dusme (DecreaseStockAtomicAsync), siparis basarili sayfasi, header sepet badge, kupon kodu (mevcut DiscountVoucher), teslimat adresi

**Haric:** PayTR (Faz 2), havale/EFT (Faz 2), kapida odeme (Faz 2), fatura PDF (SP-7), kargo API (SP-7), email bildirimi (SP-5)

## Bagimliliklari

- SP-3 (Auth) tamamlanmis olmali — checkout icin giris gerekli
- Mevcut DecreaseStockAtomicAsync — stok icin
- Mevcut Order/OrderItem entity — MarketPlaceId=10 ile kullanilir

---

## 1. Entity'ler

### Cart : BaseEntity
Id (Guid), TenantId (int), CustomerId (int? FK->Customer — giris yapilmissa), SessionId (string? — misafir), CouponCode (string?), ExpiresAt (DateTimeOffset — 7 gun), ICollection<CartItem> Items

### CartItem : BaseEntity
Id (int), CartId (Guid FK->Cart), ProductVariantId (Guid FK->ProductVariant), Quantity (int), UnitPrice (decimal — ekleme anindaki fiyat), AddedAt (DateTimeOffset)

### Order entity'sine eklenen alanlar:
- CustomerId (int? FK->Customer) — B2C siparisler icin
- PaymentMethod (string? — "CreditCard", "CashOnDelivery")
- PaymentTransactionId (string? — iyzico transaction id)
- PaymentStatus (PaymentStatus enum: Pending, Paid, Failed, Refunded)
- OrderStatus (OrderStatus enum: Received, Preparing, Shipped, Delivered, Cancelled)
- OrderNote (string?)
- SubTotal (decimal), ShippingCost (decimal), DiscountAmount (decimal)

### StorefrontPaymentConfig : BaseEntity (spec'ten)
TenantId (int), PaymentProvider (string — "Iyzico"), MerchantId (string?), ApiKey (string — encrypted), SecretKey (string — encrypted), IsLive (bool), InstallmentEnabled (bool), MaxInstallmentCount (int), MinInstallmentAmount (decimal?), IsActive (bool)

### PaymentStatus enum
Pending, Paid, Failed, Refunded

### OrderStatus enum
Received, Preparing, Shipped, Delivered, Cancelled, Returned

## 2. Business Layer

### ICartManager
AddToCartAsync(int tenantId, Guid sessionCartId, Guid productVariantId, int quantity) -> IDataResult<Cart>
GetCartAsync(int tenantId, Guid cartId) -> IDataResult<Cart>
GetOrCreateCartAsync(int tenantId, int? customerId, string? sessionId) -> IDataResult<Cart>
UpdateQuantityAsync(Guid cartId, Guid productVariantId, int quantity) -> IResult
RemoveItemAsync(Guid cartId, Guid productVariantId) -> IResult
ClearCartAsync(Guid cartId) -> IResult
ApplyCouponAsync(Guid cartId, string couponCode) -> IResult
MergeCartsAsync(Guid sessionCartId, int customerId) -> IResult (misafir -> uye birlesme)

### ICheckoutManager
ValidateCheckoutAsync(Guid cartId, CheckoutRequestDto dto) -> IDataResult<CheckoutSummaryDto>
CreateOrderFromCartAsync(Guid cartId, CheckoutRequestDto dto) -> IDataResult<Order>

### IPaymentGatewayService (adapter pattern)
InitiatePaymentAsync(PaymentRequest request) -> IDataResult<PaymentInitResult>
HandleCallbackAsync(string callbackData) -> IDataResult<PaymentResult>

### IIyzicoPaymentService : IPaymentGatewayService
iyzico Checkout Form API implementasyonu

## 3. DTOs

CheckoutRequestDto: CartId, ShippingAddressId (int), BillingAddressId (int?), PaymentMethod (string), OrderNote (string?), UseSameAddressForBilling (bool)

CheckoutSummaryDto: Items (list), SubTotal, ShippingCost, DiscountAmount, TaxAmount, GrandTotal, ShippingAddress, EstimatedDeliveryDays

PaymentRequest: OrderId (Guid), Amount (decimal), Currency ("TRY"), CustomerEmail, CustomerName, CustomerPhone, Items (list), CallbackUrl, BillingAddress, ShippingAddress

PaymentInitResult: PaymentPageUrl (string — iyzico checkout form URL), Token (string)

PaymentResult: OrderId (Guid), Success (bool), TransactionId (string?), ErrorMessage (string?)

CartSummaryDto: ItemCount (int), Total (decimal) — header badge icin

AddToCartRequestDto: ProductVariantId (Guid), Quantity (int)

## 4. iyzico Entegrasyonu

NuGet: Iyzipay (official iyzico .NET SDK)

Checkout Form API (en basit entegrasyon):
1. Cart -> CheckoutSummaryDto hesapla
2. Iyzipay.Request.CreateCheckoutFormInitializeRequest olustur
3. iyzico Checkout Form URL'i al
4. Kullaniciyi iyzico sayfasina yonlendir
5. Odeme sonrasi callback URL'e doner
6. HandleCallback ile sonucu dogrula
7. Basariliysa: Order olustur + stok dus + redirect /odeme/basarili
8. Basarisizsa: hata sayfasi goster

Config: StorefrontPaymentConfig'ten ApiKey + SecretKey (tenant bazli)

## 5. Controllers & Routes

### CartController
POST /sepet/ekle — AddToCart (AJAX, JSON response)
POST /sepet/guncelle — UpdateQuantity (AJAX)
POST /sepet/sil — RemoveItem (AJAX)
GET /sepet — Cart page (full page)
GET /api/sepet/ozet — CartSummary (JSON, header badge icin)

### CheckoutController [Authorize]
GET /odeme — Checkout page (adres secimi, odeme yontemi, siparis ozeti)
POST /odeme/onayla — ValidateCheckout + InitiatePayment -> redirect iyzico
GET /odeme/callback — iyzico callback handler
GET /odeme/basarili — Siparis basarili sayfasi
GET /odeme/basarisiz — Odeme basarisiz sayfasi

## 6. Views

Cart/Index.cshtml — sepet sayfasi (urun listesi, miktar, toplam, kupon, checkout butonu)
Checkout/Index.cshtml — adres, odeme, siparis ozeti
Checkout/Success.cshtml — siparis onay
Checkout/Failed.cshtml — hata

Header guncelleme: sepet badge (item count)

## 7. Sepet Akisi

```
Urun detay -> "Sepete Ekle" (AJAX POST) -> CartManager.AddToCartAsync
  -> Stok kontrolu (yeterli mi?)
  -> CartItem olustur (UnitPrice = su anki SalePrice)
  -> Header badge guncelle (AJAX GET /api/sepet/ozet)

Sepet sayfasi -> miktar guncelle / sil (AJAX)
  -> "Odemeye Gec" -> /odeme (Authorize gerekli)
```

## 8. Checkout Akisi

```
GET /odeme -> CheckoutController.Index
  -> Cart'i getir + validate (bos mu? stoklar yeterli mi?)
  -> Adres secimi goster (SP-3'ten StorefrontCustomerAuth -> Customer -> Addresses)
  -> Siparis ozeti hesapla (SubTotal + Shipping + Discount + Tax = GrandTotal)

POST /odeme/onayla -> CheckoutController.Confirm
  1. ValidateCheckoutAsync (son stok kontrolu)
  2. CreateOrderFromCartAsync (Order + OrderItems, PaymentStatus=Pending)
  3. DecreaseStockAtomicAsync (her OrderItem icin)
  4. InitiatePaymentAsync (iyzico Checkout Form)
  5. Redirect -> iyzico odeme sayfasi

GET /odeme/callback?token=... -> CheckoutController.Callback
  1. HandleCallbackAsync (iyzico sonucu dogrula)
  2. Basarili: Order.PaymentStatus = Paid, Cart temizle -> /odeme/basarili
  3. Basarisiz: Order.PaymentStatus = Failed, stok geri ekle -> /odeme/basarisiz
```

## 9. Stok Yonetimi

- DecreaseStockAtomicAsync mevcut — dogrudan kullanilir
- StockMovementType.Sale + ReferenceType="StorefrontOrder" + ReferenceId=Order.Id
- Odeme basarisiz olursa: stok geri eklenir (IncreaseStockAsync)
- Race condition yok: atomic SQL update

## 10. Guvenlik

- Checkout [Authorize] — giris zorunlu
- CSRF korumalari (AntiForgeryToken)
- iyzico API key encrypted (AES-256 at rest — StorefrontPaymentConfig)
- Kart bilgisi sunucuya gelmez (iyzico Checkout Form — PCI DSS uyum)
- Cart ID'leri GUID (tahmin edilemez)

## 11. Tests

Unit: CartManagerTests (add/remove/update/clear/merge), CheckoutManagerTests (validate, create order), IyzicoPaymentServiceTests (mock HTTP)
Integration: full checkout akisi (cart -> order -> stock decrease)
