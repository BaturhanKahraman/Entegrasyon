export const meta = {
  name: 'feature-pipeline',
  description: 'Tek backlog task\'ını rol-sınırlı agent zincirinden geçirir: PA inceleme → (Designer) → (DB) → SWE TDD → eş-review → QA DoD. Çıktı = TL push-gate için yapılandırılmış teslim raporu.',
  whenToUse: 'Loop sürücüsü ya da elle: bir tasks.json task id\'sini args ile ver. mode:dry plan-only (ucuz kanıt), mode:full implement+test.',
  phases: [
    { title: 'PA İnceleme' },
    { title: 'Tasarım' },
    { title: 'DB' },
    { title: 'SWE Implementasyon' },
    { title: 'Eş-Review' },
    { title: 'QA Kapısı' },
  ],
}

// ── args (string olarak gelir → parse) ──
const cfg = args ? (typeof args === 'string' ? JSON.parse(args) : args) : {}
const TASK_ID = cfg.taskId
const MODE = cfg.mode === 'full' ? 'full' : 'dry'  // varsayılan dry (güvenli)
const MODEL = cfg.modelOverride || undefined        // ör. 'sonnet' maliyet için; yoksa agent.md modeli
if (!TASK_ID) {
  throw new Error('feature-pipeline: args.taskId zorunlu (ör. {"taskId":"T028","mode":"dry"}). Ana oturum next-task.py ile seçip verir.')
}

const HEADLESS = 'WORKFLOW MODU (headless): TL onayı = bu görev tanımı; soru sorma, git/push yapma, başka agent çağırma, integration/E2E koşma. Belirsizlikte makul varsay + VARSAYIM: ile raporla. Eksik altyapı/devir için DEVİR: yaz. Çıktın yapılandırılmış veri teslimidir.'
const mopt = (o) => (MODEL ? { ...o, model: MODEL } : o)

// ── şemalar ──
const PA_SCHEMA = {
  type: 'object',
  properties: {
    status: { type: 'string', enum: ['ready', 'stale', 'already_done', 'blocked'], description: 'ready=işlenebilir; stale=tanım kodla uyuşmuyor güncellensin; already_done=zaten yapılmış; blocked=ön koşul eksik' },
    reason: { type: 'string', description: 'status gerekçesi, kod kanıtıyla (dosya:satır)' },
    summary: { type: 'string', description: '1-2 cümle: ne yapılacak' },
    acceptanceCriteria: { type: 'array', items: { type: 'string' }, description: 'kod-teyitli, güncel kabul kriterleri' },
    needsDesign: { type: 'boolean', description: 'UI/Razor view/Tabler tasarımı gerekir mi' },
    needsMigration: { type: 'boolean', description: 'entity/DbContext/migration gerekir mi' },
    needsBackend: { type: 'boolean', description: 'controller/manager/servis kodu gerekir mi' },
    isReadOnly: { type: 'boolean', description: 'salt-okuma mı (mutasyon yok)' },
    blockers: { type: 'array', items: { type: 'string' } },
    assumptions: { type: 'array', items: { type: 'string' } },
  },
  required: ['status', 'reason', 'summary', 'acceptanceCriteria', 'needsDesign', 'needsMigration', 'needsBackend', 'isReadOnly'],
}

const PLAN_SCHEMA = {
  type: 'object',
  properties: {
    filesToCreate: { type: 'array', items: { type: 'string' } },
    filesToModify: { type: 'array', items: { type: 'string' } },
    testsPlanned: { type: 'array', items: { type: 'string' }, description: 'yazılacak test sınıf/metod adları (RED-first)' },
    designContract: { type: 'string', description: 'designer aşamasından beklenen sözleşme (UI varsa) — yoksa boş' },
    dbContract: { type: 'string', description: 'DB aşamasından beklenen şema/migration (varsa) — yoksa boş' },
    complexity: { type: 'string', enum: ['S', 'M', 'L'] },
    openQuestions: { type: 'array', items: { type: 'string' } },
    risks: { type: 'array', items: { type: 'string' } },
  },
  required: ['filesToCreate', 'filesToModify', 'testsPlanned', 'complexity'],
}

const DESIGN_SCHEMA = {
  type: 'object',
  properties: {
    viewFiles: { type: 'array', items: { type: 'string' }, description: 'yazılan/güncellenen .cshtml + partial yolları (full mod)' },
    contract: { type: 'string', description: 'SWE\'ye sözleşme: gereken ViewModel alanları + controller action imzaları + ViewBag/section anahtarları' },
    tablerNotes: { type: 'string', description: 'kullanılan Tabler bileşenleri + doğrulanan class kombinasyonları' },
    deferrals: { type: 'array', items: { type: 'string' }, description: 'DEVİR: satırları (altyapı SWE\'ye)' },
    assumptions: { type: 'array', items: { type: 'string' } },
  },
  required: ['contract'],
}

const DB_SCHEMA = {
  type: 'object',
  properties: {
    entityChanges: { type: 'array', items: { type: 'string' } },
    migrationName: { type: 'string' },
    schemaSummary: { type: 'string' },
    indexes: { type: 'array', items: { type: 'string' } },
    handoffNotes: { type: 'string', description: 'SWE\'nin tüketeceği yeni alan/DbSet/repo imzaları + DEVİR/VARSAYIM' },
    assumptions: { type: 'array', items: { type: 'string' } },
  },
  required: ['schemaSummary', 'handoffNotes'],
}

const SWE_SCHEMA = {
  type: 'object',
  properties: {
    filesChanged: { type: 'array', items: { type: 'string' } },
    testsAdded: { type: 'array', items: { type: 'string' } },
    buildResult: { type: 'string', enum: ['pass', 'fail'] },
    unitTestResult: { type: 'string', description: 'ör. "1752 passed, 0 failed" ya da fail özeti' },
    redFirstProof: { type: 'string', description: 'RED-first kanıtı: test fix öncesi başarısızdı mı, nasıl doğrulandı' },
    summary: { type: 'string' },
    deferrals: { type: 'array', items: { type: 'string' }, description: 'DEVİR: satırları' },
    assumptions: { type: 'array', items: { type: 'string' } },
  },
  required: ['filesChanged', 'buildResult', 'unitTestResult', 'summary'],
}

const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    verdict: { type: 'string', enum: ['approve', 'changes_requested'] },
    blockingCount: { type: 'number' },
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          severity: { type: 'string', enum: ['blocker', 'major', 'minor', 'nit'] },
          location: { type: 'string', description: 'dosya:satır' },
          problem: { type: 'string' },
          fix: { type: 'string' },
        },
        required: ['severity', 'location', 'problem', 'fix'],
      },
    },
  },
  required: ['verdict', 'blockingCount', 'findings'],
}

const QA_SCHEMA = {
  type: 'object',
  properties: {
    verdict: { type: 'string', enum: ['pass', 'fail'] },
    buildResult: { type: 'string', enum: ['pass', 'fail'] },
    unitTestResult: { type: 'string' },
    acceptanceCovered: { type: 'array', items: { type: 'string' }, description: 'karşılanan kabul kriterleri' },
    blockers: { type: 'array', items: { type: 'string' }, description: 'pass\'ı engelleyen eksikler (boşsa pass)' },
    ciDeferred: { type: 'array', items: { type: 'string' }, description: 'CI-DEVİR: headless\'ta kanıtlanamayan (integration/E2E/görsel) maddeler' },
    notes: { type: 'string' },
  },
  required: ['verdict', 'buildResult', 'unitTestResult', 'blockers', 'ciDeferred'],
}

// ─────────────────────── PA İNCELEME (her modda) ───────────────────────
phase('PA İnceleme')
const pa = await agent(
  `${HEADLESS}

ROL: Product Analyst (kod-önce doğrulama).
GÖREV: docs/tasks/tasks.json içinde id == "${TASK_ID}" olan task'ı bul ve OKU (python3/jq ile id'ye göre çek). Sonra task'ın description'ında geçen kod referanslarını (dosya:satır, servis/metod adları) ve varsa "Kaynak: docs/superpowers/specs/..." spec'ini KODDAN teyit et (Read/Grep).

Belirle:
- status: 'ready' (işlenebilir, tanım kodla uyumlu) | 'stale' (tanım kodla çelişiyor, önce güncellenmeli) | 'already_done' (kodda zaten var) | 'blocked' (ön koşul kodda yok).
- Güncel, KOD-TEYİTLİ kabul kriterleri (acceptanceCriteria) — manual_test_steps'i de dikkate al.
- needsDesign / needsMigration / needsBackend / isReadOnly bayrakları (kodun mevcut durumuna göre).
- blockers + assumptions.
Kanıtını reason'da dosya:satır ile ver. Kod yazma, dosya düzenleme YOK — sadece analiz.`,
  mopt({ label: `PA:${TASK_ID}`, phase: 'PA İnceleme', agentType: 'pa-entegrasyon', schema: PA_SCHEMA })
)

if (!pa) {
  return { taskId: TASK_ID, mode: MODE, outcome: 'error', detail: 'PA aşaması sonuç döndürmedi (subagent öldü).' }
}
log(`PA verdict: ${pa.status} — ${pa.summary}`)

if (pa.status !== 'ready') {
  // erken dur — downstream'i boşa harcama. Ana oturum bu verdict'e göre task'ı güncellesin/atlasın.
  return {
    taskId: TASK_ID, mode: MODE, outcome: 'pa_stop', paStatus: pa.status,
    reason: pa.reason, summary: pa.summary, blockers: pa.blockers || [],
    acceptanceCriteria: pa.acceptanceCriteria, assumptions: pa.assumptions || [],
    nextAction: pa.status === 'already_done' ? 'tasks.json → status done işaretle'
      : pa.status === 'stale' ? 'task tanımını koda göre güncelle (ana oturum/PA), sonra tekrar çalıştır'
      : 'bağımlılığı/ön koşulu çöz, sonra tekrar çalıştır',
  }
}

// ─────────────────────── DRY MOD: plan-only, ucuz kanıt ───────────────────────
if (MODE === 'dry') {
  phase('SWE Implementasyon')
  const plan = await agent(
    `${HEADLESS}

ROL: Software Engineer (planlama — KOD YAZMA, sadece plan).
GÖREV: "${TASK_ID}" task'ı için implementasyon planı çıkar. PA analizi:
- Özet: ${pa.summary}
- Kabul kriterleri: ${JSON.stringify(pa.acceptanceCriteria)}
- needsDesign=${pa.needsDesign}, needsMigration=${pa.needsMigration}, needsBackend=${pa.needsBackend}, isReadOnly=${pa.isReadOnly}

İlgili mevcut kodu OKU (controller/manager/view), katmanlı mimariye (Manager/Interface + FluentValidation + LogicRunner pipeline + Mapperly + çift loglama) uygun planı ver: oluşturulacak/değişecek dosyalar, RED-first test listesi, designer/DB sözleşme beklentisi, karmaşıklık (S/M/L), açık sorular, riskler. Dosya YAZMA.`,
    mopt({ label: `PLAN:${TASK_ID}`, phase: 'SWE Implementasyon', agentType: 'swe-entegrasyon', schema: PLAN_SCHEMA })
  )
  return {
    taskId: TASK_ID, mode: 'dry', outcome: 'plan_ready',
    pa: { status: pa.status, summary: pa.summary, acceptanceCriteria: pa.acceptanceCriteria, needsDesign: pa.needsDesign, needsMigration: pa.needsMigration, needsBackend: pa.needsBackend, isReadOnly: pa.isReadOnly, assumptions: pa.assumptions || [] },
    plan: plan || { error: 'plan agent null' },
    nextAction: 'Plan onaylanırsa aynı task\'ı mode:full ile çalıştır.',
  }
}

// ─────────────────────── FULL MOD: implement + test ───────────────────────
// Tasarım (koşullu)
let design = null
if (pa.needsDesign) {
  phase('Tasarım')
  design = await agent(
    `${HEADLESS}

ROL: UI/UX Designer (Razor + Tabler + HTMX). C#/iş mantığı/migration YAZMA — altyapı gerekirse DEVİR: swe + veri sözleşmesi yaz.
GÖREV: "${TASK_ID}" — ${pa.summary}
Kabul kriterleri: ${JSON.stringify(pa.acceptanceCriteria)}
Features/{Feature}/Views altına .cshtml + partial yaz. Tabler bileşeni kullanmadan ÖNCE class kombinasyonunu doğrula (tabler-ui referansı). Referans tasarım dili: Features/Reports/Views/* + Products. Türkçe diakritikler doğru. SWE'nin sağlaması gereken ViewModel alanları + controller action imzaları + ViewBag anahtarlarını "contract" alanında net yaz.`,
    mopt({ label: `DESIGN:${TASK_ID}`, phase: 'Tasarım', agentType: 'designer', schema: DESIGN_SCHEMA })
  )
  if (design) log(`Designer: ${(design.viewFiles || []).length} view, contract hazır`)
}

// DB aşaması (yeniden kullanılabilir — hem önden hem geç-keşif kurtarması için)
async function runDb(extraCtx) {
  phase('DB')
  const r = await agent(
    `${HEADLESS}

ROL: Database Master (EF Core + PostgreSQL). Entity/DbContext değişikliği + IEntityTypeConfiguration + migration strict-rule (add → gözden geçir → has-pending-model-changes ile snapshot doğrula). Dev DB erişilemezse migration dosyası + temiz snapshot yeterli (update'i DEVİR: ile bırak).
GÖREV: "${TASK_ID}" — ${pa.summary}
Kabul kriterleri: ${JSON.stringify(pa.acceptanceCriteria)}${extraCtx || ''}
Multi-tenant izolasyon + index/hot-path'i göz önünde bulundur. SWE'nin tüketeceği yeni alan/DbSet/servis imzalarını "handoffNotes"ta yaz. EF mutasyon-persist kuralı (no-tracking footgun) için ilgili mutasyon yollarını işaretle.`,
    mopt({ label: `DB:${TASK_ID}`, phase: 'DB', agentType: 'db-entegrasyon', schema: DB_SCHEMA })
  )
  if (r) log(`DB: migration ${r.migrationName || '(yok)'} — ${r.schemaSummary}`)
  return r
}

// DB (koşullu — PA bayrağına göre önden)
let db = pa.needsMigration ? await runDb() : null

// SWE implementasyon (TDD)
phase('SWE Implementasyon')
const designCtx = design ? `\nDESIGNER SÖZLEŞMESİ:\n${design.contract}\nView dosyaları: ${JSON.stringify(design.viewFiles || [])}\nDesigner DEVİR: ${JSON.stringify(design.deferrals || [])}` : ''
const dbCtx = db ? `\nDB HANDOFF:\n${db.handoffNotes}\nMigration: ${db.migrationName || '(yok)'}` : ''
const swe = await agent(
  `${HEADLESS}

ROL: Software Engineer-A (TDD-First, RED→GREEN). Katmanlı mimari: Manager/Interface + FluentValidation + LogicRunner 3-adım pipeline (Validation→BusinessRules→Execution) + Mapperly + çift loglama (IApplicationLogManager TR + ILogger). EF mutasyonda .AsTracking()/Update şart (no-tracking footgun). Entity/migration GEREKİYORSA kendin yapma → DEVİR: db-entegrasyon (zaten yapıldıysa handoff'u tüket).
GÖREV: "${TASK_ID}" — ${pa.summary}
Kabul kriterleri: ${JSON.stringify(pa.acceptanceCriteria)}${designCtx}${dbCtx}

Sıra: (1) RED testi yaz, başarısız olduğunu doğrula. (2) Minimum kodu yaz (GREEN). (3) dotnet build Entegrasyon.sln + dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj koş. buildResult + unitTestResult + redFirstProof döndür. Integration/E2E KOŞMA (CI'da).`,
  mopt({ label: `SWE-A:${TASK_ID}`, phase: 'SWE Implementasyon', agentType: 'swe-entegrasyon', schema: SWE_SCHEMA })
)
if (!swe) {
  return { taskId: TASK_ID, mode: 'full', outcome: 'error', detail: 'SWE aşaması sonuç döndürmedi.', pa, design, db }
}
log(`SWE-A: build=${swe.buildResult}, test=${swe.unitTestResult}, ${swe.filesChanged.length} dosya`)

// Geç-keşif kurtarması: PA needsMigration=false dedi ama SWE migration'a ihtiyaç duyduysa
// (DEVİR: db-entegrasyon / migration), DB aşamasını şimdi çalıştır + SWE'yi handoff ile tekrarla.
if (!db) {
  const wantsDb = (swe.deferrals || []).some((d) => /db-entegrasyon|migration|entity|dbcontext|şema|schema/i.test(d))
  if (wantsDb) {
    log('SWE migration devretti — DB aşaması geç çalıştırılıyor, sonra SWE tekrar.')
    db = await runDb(`\nSWE şu DEVİR'i yaptı, bunu karşıla: ${JSON.stringify(swe.deferrals)}`)
    if (db) {
      phase('SWE Implementasyon')
      const swe2 = await agent(
        `${HEADLESS}

ROL: Software Engineer-A (DB handoff sonrası implementasyon tamamlama). DB Master migration'ı hazırladı:
${db.handoffNotes}
Migration: ${db.migrationName || '(yok)'}
"${TASK_ID}" — ${pa.summary}: backend + testleri bu yeni şemayı tüketerek TAMAMLA (TDD RED→GREEN). dotnet build + unit test koş.`,
        mopt({ label: `SWE-A2:${TASK_ID}`, phase: 'SWE Implementasyon', agentType: 'swe-entegrasyon', schema: SWE_SCHEMA })
      )
      if (swe2) {
        swe.filesChanged = [...new Set([...(swe.filesChanged || []), ...(swe2.filesChanged || [])])]
        swe.buildResult = swe2.buildResult
        swe.unitTestResult = swe2.unitTestResult
        swe.summary = swe.summary + ' | DB handoff sonrası: ' + swe2.summary
        swe.deferrals = swe2.deferrals || []
        log(`SWE-A2 (DB sonrası): build=${swe2.buildResult}, test=${swe2.unitTestResult}`)
      }
    }
  }
}

// Eş-review (SWE-B)
phase('Eş-Review')
const review = await agent(
  `${HEADLESS}

ROL: Software Engineer-B (eş-review). SWE-A'nın "${TASK_ID}" için yaptığı değişikliği git diff ile incele (git diff + git status; commit YOK). Mimari kurallara uyum: 3-adım pipeline, no-tracking persist, çift loglama, Mapperly, multi-tenant, FluentValidation. Güvenlik (kullanıcı girdisi/yetki/mutasyon) ve doğruluk hatalarına odaklan; format nit'lerini atla.
SWE-A özeti: ${swe.summary}
Değişen dosyalar: ${JSON.stringify(swe.filesChanged)}
Her bulgu: severity + dosya:satır + problem + fix. blocker varsa changes_requested.`,
  mopt({ label: `SWE-B:${TASK_ID}`, phase: 'Eş-Review', agentType: 'swe-entegrasyon', schema: REVIEW_SCHEMA })
)
log(`Review: ${review ? review.verdict + ' (' + review.blockingCount + ' blocker)' : 'null'}`)

// Blocker varsa bir tur fix
let sweFix = null
if (review && review.verdict === 'changes_requested' && review.blockingCount > 0) {
  const blockers = review.findings.filter((f) => f.severity === 'blocker' || f.severity === 'major')
  sweFix = await agent(
    `${HEADLESS}

ROL: Software Engineer-A (review fix turu). Eş-review şu blocker/major bulguları verdi — DÜZELT, sonra build + unit test tekrar koş:
${JSON.stringify(blockers, null, 2)}
Sadece bu bulguları gider, kapsam genişletme. buildResult + unitTestResult döndür.`,
    mopt({ label: `SWE-FIX:${TASK_ID}`, phase: 'Eş-Review', agentType: 'swe-entegrasyon', schema: SWE_SCHEMA })
  )
  if (sweFix) log(`SWE fix: build=${sweFix.buildResult}, test=${sweFix.unitTestResult}`)
}

// QA kapısı (DoD)
phase('QA Kapısı')
const qa = await agent(
  `${HEADLESS}

ROL: QA / TDD bekçisi (Definition of Done kapısı). "${TASK_ID}" değişikliğini incele (git diff). Kabul kriterleri + manual_test_steps karşılanıyor mu? RED-first izlendi mi? dotnet build + dotnet test Test/Entegrasyon.Test/Entegrasyon.UnitTest.csproj KOŞ ve sonucu raporla. Integration/E2E/görsel doğrulamayı KOŞMA — bunları ciDeferred (CI-DEVİR) olarak işaretle, bu yüzden fail VERME. Yalnız headless'ta kanıtlanabilir eksikler (build fail, unit fail, kabul kriteri açıkça karşılanmamış) blocker'dır.
Kabul kriterleri: ${JSON.stringify(pa.acceptanceCriteria)}
SWE özeti: ${swe.summary}${sweFix ? '\nFix sonrası: ' + sweFix.summary : ''}
Review verdict: ${review ? review.verdict : 'n/a'}`,
  mopt({ label: `QA:${TASK_ID}`, phase: 'QA Kapısı', agentType: 'qa-entegrasyon', schema: QA_SCHEMA })
)
log(`QA: ${qa ? qa.verdict : 'null'}`)

// ── Teslim raporu (ana oturum push-gate'i bunu okur) ──
const finalBuild = (sweFix && sweFix.buildResult) || swe.buildResult
const readyToPush = !!qa && qa.verdict === 'pass' && qa.buildResult === 'pass' && finalBuild === 'pass'
return {
  taskId: TASK_ID,
  mode: 'full',
  outcome: readyToPush ? 'ready_to_push' : 'needs_attention',
  readyToPush,
  summary: pa.summary,
  acceptanceCriteria: pa.acceptanceCriteria,
  filesChanged: [...new Set([...(swe.filesChanged || []), ...(sweFix ? sweFix.filesChanged : []), ...(design ? design.viewFiles || [] : [])])],
  migration: db ? { name: db.migrationName, schema: db.schemaSummary } : null,
  build: finalBuild,
  unitTests: (sweFix && sweFix.unitTestResult) || swe.unitTestResult,
  review: review ? { verdict: review.verdict, blockingCount: review.blockingCount, findings: review.findings } : null,
  qa: qa ? { verdict: qa.verdict, blockers: qa.blockers, ciDeferred: qa.ciDeferred, acceptanceCovered: qa.acceptanceCovered, notes: qa.notes } : null,
  deferrals: [...(swe.deferrals || []), ...(design ? design.deferrals || [] : [])],
  assumptions: [...(pa.assumptions || []), ...(swe.assumptions || []), ...(design ? design.assumptions || [] : []), ...(db ? db.assumptions || [] : [])],
  nextAction: readyToPush
    ? 'Ana oturum: diff incele → commit → git push gitea+origin develop → dev deploy → (görsel QA 8085) → next-task.py --done ' + TASK_ID
    : 'QA fail/blocker — ana oturum kararı: aynı task mode:full tekrar (fix odaklı) ya da elle müdahale. Blocker listesine bak.',
}
