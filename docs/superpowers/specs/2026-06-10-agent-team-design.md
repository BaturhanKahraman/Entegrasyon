# Entegrasyon Geliştirme Takımı — Tasarım (Faz 1)

**Tarih:** 2026-06-10
**Amaç:** Projeyi sahibi başında olmasa bile yürütebilecek, kalıcı, projeye özel bir Claude agent takımı kurmak. Team Leader = ana oturum; PM, 2×SWE, DB Master, QA teammate'ler.

---

## 1. Vizyon & Misyon (her agent'a gömülecek)

Tek bir yerden (`Entegrasyon.MVC`) bir esnafın ürününü girip **tüm pazaryerlerine + kendi e-ticaret sitesine + fiziksel mağaza satışına** dağıtabildiği, sonra hepsini tek panelden kontrol edebildiği merkezi çok-pazaryeri entegrasyon platformu (SaaS, multi-tenant).

- **İlk müşteri:** zekidsbebe.com (fiziksel mağaza + e-ticaret). Nebim + Ticimax dağınıklığı ve kötü destekten bıkmışlar. İstek: tek yerden ürün ekle → tüm mağazalara gönder → tek panelden kontrol.
- **Parçalar:** `Entegrasyon.MVC` (esnaf paneli) · `Storefront` (müşteri e-ticaret sitesi; 2. müşteride iskelet + dizayn) · `Admin` (müşteri yönetimi katı) · `Agent` (yazıcı/offline).
- **Kuzey yıldızı:** Üretilen **her task gerçek hayatta işe yaramalı.** Esnaf 10 ayrı yere girip çile çekmesin.

Detaylı domain referansları: `docs/rakip-analizi.md`, `docs/production-audit.md`, `docs/tasks/`.

---

## 2. Mimari

```
                    KULLANICI (Baturhan)
                          │
                   ┌──────▼───────┐
                   │ TEAM LEADER  │  = ana oturum (Claude ana hattı)
                   └──┬───┬───┬───┘
            ┌─────────┘   │   └──────────┐
        ┌───▼───┐    ┌────▼────┐    ┌────▼────┐
        │  PM   │    │DB Master│    │   QA    │
        └───────┘    └─────────┘    └────┬────┘
                 ┌──────────┬────────────┘
            ┌────▼────┐ ┌───▼─────┐
            │ SWE-A   │◄┤ SWE-B   │  ← birbirini code-review eder
            └─────────┘ └─────────┘
```

- Takımı **her zaman ana oturum kurar** (`TeamCreate` + `Agent(name=…, team_name=…)`), teammate'ler tmux panelinde canlı yaşar (`teammateMode: tmux` zaten ayarlı).
- Team Leader = ana oturum (kullanıcıyla konuşan Claude). İşi böler, dağıtır, sonuçları toplar, raporlar.
- Teammate'ler birbirine `SendMessage` ile konuşur. Alt-teammate doğurmayı TL yapar.

**Team Leader'ın asıl görevi — takımı idame + çıktı kalitesi (sadece dağıtıcı değil):**
1. **Ekstra QC / çıktı güzelleştirme:** Her teammate çıktısını son süzgeçten geçir (tutarlılık, desen uyumu, vizyon, gerçek-işe-yararlık). QA'nın üstünde son kalite kapısı.
2. **Agent geliştirme:** Zayıf/yanlış davranan agent'ın `.claude/agents/<rol>.md` tanımını düzenle, güçlendir; tekrarlayan hataları kalıcı kurala çevir.
3. **Yeni rol:** İhtiyaç görürse yeni rol tasarlar, `.claude/agents/`'a ekler, takıma katar (`skill-creator` ile gerekli skill'i de üretir).
4. **Takım sağlığı:** Darboğaz/izolasyon/iş-rol eşleşmesini sürekli gözetir ve ayarlar. Yapısal değişiklikleri commit'ler.

---

## 3. Roller

Her rol `.claude/agents/<isim>.md` (frontmatter + projeye özel system prompt). Repoya commit'li = kalıcı.

| Rol | Dosya | Model | Araçlar | Skill'ler |
|---|---|---|---|---|
| **Product Manager** | `pm-entegrasyon` | sonnet | Read, Grep, Glob, Write(*spec/backlog*), WebSearch, WebFetch | `entegrasyon-pm`, `brainstorming`, `writing-plans`, `deep-research` |
| **Software Engineer** (×2) | `swe-entegrasyon` | opus | Tümü | `aspnet-mvc-htmx`, `test-driven-development`, `simplify`, `code-simplifier`, `systematic-debugging`, `requesting/receiving-code-review` |
| **Database Master** | `db-entegrasyon` | opus | Read, Edit, Write, Bash, Grep, Glob | `entegrasyon-db`, `microsoft-docs`, `aspnet-mvc-htmx` |
| **QA / Test Engineer** | `qa-entegrasyon` | sonnet | Read, Edit, Write, Bash, Grep, Glob + Playwright/Chrome DevTools MCP | `test-driven-development`, `code-review`, `verify`, `verification-before-completion`, `fallow`(JS) |

> İki SWE aynı `swe-entegrasyon` tanımını paylaşır, farklı isimlerle doğar (ör. `SWE-Ahmet`, `SWE-Mehmet`); karşılıklı review yapar.
> Model katmanlaması maliyet içindir (Faz 2 usage hedefi); frontmatter'da sonradan değiştirilebilir.

---

## 4. İş Akışı (TL pipeline)

1. **Kullanıcı → TL:** istek (veya Faz 2'de backlog'dan otomatik).
2. **TL → PM:** spec + kabul kriteri + task üret. **PM proaktiftir** — iş verilmesini beklemeden fonksiyonel eksikleri (production-audit, rakip-analizi, backlog açıkları) VE teknik ihtiyaçları (test kapsamı, tech-debt, performans/N+1, güvenlik, refactor, eksik migration/index) tarayıp her tur 1-3 yüksek-değerli task önerir. PM çıktısını `docs/tasks/tasks.json` şemasına yazar (mevcut format: `task, description, status, priority, plan, manual_test_steps[], addOrUpdateUnitTests, addOrUpdateIntegrationTests`). Büyük işler için `docs/superpowers/specs/`'e tasarım dokümanı.
3. **TL onay kapısı:** Saçma/gereksiz/kapsam dışı istek geçmez. Mantık sağlam mı, vizyona hizmetli mi, gerçek işe yarıyor mu?
4. **DB'ye dokunan iş → DB Master önce:** entity/DbContext değişikliği + migration (strict-rule) + `has-pending-model-changes` temiz.
5. **TL → SWE-A/B:** TDD-First implementasyon. QA RED-first'i zorlar.
6. **SWE-B ↔ SWE-A:** karşılıklı code-review.
7. **QA:** Definition of Done kapısı (§6). Tam suite yeşil.
8. **TL:** entegre eder, kullanıcıya raporlar.

**İzolasyon:** Varsayılan tek workspace. Bir görev iki SWE'yi aynı dosyalara sokacaksa, o görev için `Agent(isolation: "worktree")` → TL birleştirir.

---

## 5. Backlog

Tek kaynak: **`docs/tasks/tasks.json`** (mevcut şema korunur). PM buraya ekler/günceller; TL onaylar; Faz 2 loop'u buradan `status != done/wont_do` + `priority` sırasıyla iş çeker.

---

## 6. Definition of Done (QA + TL zorlar)

Bir task ancak şunların **tamamı** sağlanınca "done":
- [ ] `dotnet build Entegrasyon.sln` yeşil
- [ ] Unit testler yeşil (`Entegrasyon.UnitTest`)
- [ ] Integration testler yeşil (`Entegrasyon.IntegrationTest`)
- [ ] E2E testler yeşil (gerekiyorsa, `Entegrasyon.E2E`)
- [ ] Entity/DbContext değiştiyse migration eklenmiş + uygulanmış + `has-pending-model-changes` temiz
- [ ] Diğer SWE code-review etti, bulgular kapandı
- [ ] `manual_test_steps` yazıldı (gerçek hayatta işe yaradığı doğrulanabilir)

---

## 7. Kırmızı Çizgiler (Guardrails — özellikle Faz 2 otonom için)

Agent'lar **TL onayı olmadan ASLA**:
- `main`/prod branch'e push veya prod deploy yapmaz
- Gerçek/prod DB'de destructive işlem (drop, truncate, toplu delete) yapmaz
- Mevcut migration dosyasını silmez/elle düzenlemez (yeni migration ekler)
- Geri dönülmez/dışa-bakan aksiyon (dış servise veri gönderme, gerçek pazaryeri API'sine canlı yazma) almaz — önce TL'ye sorar
- Secret/token'ı log'a veya commit'e yazmaz
- `data/`, `.env`, prod compose dosyalarını değiştirmez

Bu liste `.claude/agents/_guardrails.md` (veya her agent prompt'unun ortak bölümü) olarak gömülür.

---

## 8. Teslim Edilecekler (Faz 1)

1. `.claude/agents/pm-entegrasyon.md`
2. `.claude/agents/swe-entegrasyon.md`
3. `.claude/agents/db-entegrasyon.md`
4. `.claude/agents/qa-entegrasyon.md`
5. Yeni skill: `entegrasyon-db` (migration strict-rule + multi-tenant + entity config)
6. Yeni skill: `entegrasyon-pm` (vizyon + domain + spec/backlog şablonu)
   (Temiz/dar C# kod için ayrı skill yazılmadı — mevcut `simplify` + `code-simplifier` kullanılır.)
8. `.claude/commands/takim-baslat.md` → `/takim-baslat`
9. Bu spec dokümanı + commit
10. **Kanıt koşusu:** Takımı bir gerçek backlog task'ında uçtan uca çalıştır, makineyi doğrula.

---

## 9. Faz 2 (ertelendi — ayrı tasarlanacak)

Sahibi başında değilken **usage-farkında otonom loop**: PM backlog'dan iş çeker, takım geliştirir, TL koordine eder. `loop`/`ScheduleWakeup` + usage-gate ile.

### Kullanıcının çalışma pencereleri (KORUNACAK)
- Sabah **08:45** ve öğleden sonra **16:45** — Baturhan'ın iş için çalıştığı saatler. Bu pencerelere yeterli kapasite kalmalı.

### Reset-farkında mod seçimi (çekirdek mantık)
İki limit dikkate alınır: **5 saatlik (oturum)** ve **haftalık** reset.

- **Reset "harcamayı sıfırlayacak" konumdaysa → en güçlü modda çalış (full Opus).** Çünkü kalan kullanım nasılsa sıfırlanacak, biriktirmenin anlamı yok. İki durum:
  1. **Haftalık reset'e çarpıyorsa:** çalışma haftalık reset anına denk geliyorsa → sıfırlanacak → en güçlü mod.
  2. **5 saatlik limit 08:45'te veya öncesinde sıfırlanıyorsa:** kullanıcı penceresi taze başlayacağı için loop o reset'e kadar serbestçe (güçlü mod) harcayabilir.
- **Reset yakın değilse → tutumlu mod:** kullanıcının 08:45/16:45 pencerelerine kapasite bırakacak şekilde kıs (daha ucuz model, daha az paralel doğurma, ara ver).
- **Döngü yeniden başlatma:** Kullanıcı öğleden sonra işini bitirince (~**17:00 / "5 gibi"**) loop tekrar başlayabilir.

### Açık sorular (Faz 2 inşa edilirken netleştirilecek)
- 5 saatlik pencerenin ve haftalık limitin **gerçek reset saatleri** (rolling olduğundan ilk-kullanıma bağlı) — gözlemleyip kalibre et.
- "Yeterli kapasite" eşiği token cinsinden ne olmalı (kullanıcının tipik 5 saatlik tüketimi).
- Usage'ı programatik okuma yolu (Claude Code usage komutu / telemetri).

**Önkoşul:** Faz 1 kanıt koşusu başarılı olmadan Faz 2'ye geçilmez.
