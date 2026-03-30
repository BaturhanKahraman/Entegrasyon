# Import Column Mapping Design

**Tarih:** 2026-03-30
**Durum:** Onaylandi

## Amac

Kullanicilarin farkli ERP/sistemlerden gelen Excel/CSV dosyalarindaki kolon adlarini sistem field'larina eslestirebilmesi. Otomatik oneri (fuzzy matching), veri onizleme, ve yeniden kullanilabilir profiller.

## Tasarim

### Wizard Akisi
1. Import tipi sec (Product/Price/Stock)
2. Dosya yukle
3. **Kolon Eslestirme** (yeni adim): header oku, 5 satir onizle, dropdown ile esle, auto-suggest
4. Validate → Execute

### Entity: ImportColumnProfile
Id, Name, ImportType, MappingsJson (JSON dictionary)

### Fuzzy Matching
Exact → Normalized (Turkish) → Contains → Alias tablosu

### Parser
ExcelParser/CsvParser'a mapping overload — kolon adina gore parse. Mevcut metodlar korunur.

### Profil
Kaydet/yukle/sil. Tenant bazinda izole.
