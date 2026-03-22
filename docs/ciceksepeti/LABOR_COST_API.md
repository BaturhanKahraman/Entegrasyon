# Çiçeksepeti Labor Cost (İşçilik Bedeli) API

## İşçilik Bedeli Gönderimi

**Endpoint:** `PUT /api/v1/Order/UpdateLaborCost`

Belirli mücevher kategorilerindeki siparişler için KDV dahil işçilik bedeli gönderir.

**Request Body:**
```json
{
  "items": [
    {
      "orderProductId": 111,
      "laborCost": 150.00
    }
  ]
}
```

### Alanlar

| Alan | Tip | Zorunlu | Açıklama |
|------|-----|---------|----------|
| `orderProductId` | int | Evet | Sipariş ürün ID |
| `laborCost` | double | Evet | İşçilik bedeli (KDV dahil) |

### İş Kuralları

1. İşçilik bedeli **kargoya verilmeden önce** gönderilmelidir
2. `laborCost` > 0 olmalıdır
3. `laborCost` < ürün fiyatı olmalıdır
4. Sadece belirli **mücevher kategorileri** için geçerlidir

### Geçerli Kategori ID'leri

Aşağıdaki kategori ID'leri işçilik bedeli gönderimi için geçerlidir:

```
16209, 14550, 14551, 14552, 14553, 14554, 14556, 14557, 14558, 14559,
14560, 14561, 14562, 14563, 14564, 14565, 14566, 14567, 14568, 14569,
14570, 14571, 14572, 14573, 14574, 14575, 14576, 14577, 14578, 14579,
14580, 14581, 14582, 14583, 14584
```

> **NOT:** Bu kategori listesi Çiçeksepeti tarafından güncellenebilir. Entegrasyonda bu liste konfigürasyonda tutulmalı, hardcode edilmemelidir.
