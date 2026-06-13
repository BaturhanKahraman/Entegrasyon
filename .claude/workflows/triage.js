export const meta = {
  name: 'triage',
  description: 'Backlog hijyeni: verilen task id\'lerini PA-only paralel sınıflandırır (ready/stale/already_done/blocked) — kodla teyit. Hangileri gerçekten işlenebilir görmek + bayat done\'ları yakalamak için.',
  whenToUse: 'args.taskIds = ["T027",...]. Loop öncesi backlog temizliği ya da full-mode için ready task bulmak.',
  phases: [{ title: 'Triyaj' }],
}

const cfg = args ? (typeof args === 'string' ? JSON.parse(args) : args) : {}
const IDS = cfg.taskIds || []
if (!IDS.length) throw new Error('triage: args.taskIds zorunlu (ör. {"taskIds":["T027","T028"]})')
const MODEL = cfg.modelOverride || undefined
const mopt = (o) => (MODEL ? { ...o, model: MODEL } : o)

const HEADLESS = 'WORKFLOW MODU (headless): TL onayı = bu görev; soru sorma, git/dosya değiştirme YOK, sadece OKU+analiz et. Belirsizlikte makul varsay.'

const TRIAGE_SCHEMA = {
  type: 'object',
  properties: {
    taskId: { type: 'string' },
    status: { type: 'string', enum: ['ready', 'stale', 'already_done', 'blocked'] },
    reason: { type: 'string', description: 'kod kanıtı (dosya:satır)' },
    blockers: { type: 'array', items: { type: 'string' } },
    needsDesign: { type: 'boolean' },
    needsMigration: { type: 'boolean' },
    complexity: { type: 'string', enum: ['S', 'M', 'L'] },
  },
  required: ['taskId', 'status', 'reason', 'complexity'],
}

phase('Triyaj')
const results = await parallel(
  IDS.map((id) => () =>
    agent(
      `${HEADLESS}

ROL: Product Analyst (kod-önce triyaj). docs/tasks/tasks.json içinde id == "${id}" task'ını bul+oku. Description'daki kod referanslarını (dosya:satır, servis/metod) ve varsa spec'i KODDAN teyit et (Read/Grep).
Sınıfla:
- 'already_done': kodda zaten tam yapılmış (kanıt: dosya:satır).
- 'stale': tanım kodla çelişiyor / kısmen yapılmış, önce güncellenmeli.
- 'blocked': ön koşul yok (API kontratı belirsiz, başka task/servis/mock eksik) — SWE kontratsız ilerleyemez.
- 'ready': işlenebilir, tanım kodla uyumlu, bağımlılık yok.
needsDesign/needsMigration/complexity(S/M/L) bayraklarını da ver. Kod YAZMA.`,
      mopt({ label: `triage:${id}`, phase: 'Triyaj', agentType: 'pa-entegrasyon', schema: TRIAGE_SCHEMA })
    )
  )
)

const clean = results.filter(Boolean)
const byStatus = (s) => clean.filter((r) => r.status === s).map((r) => r.taskId)
return {
  triaged: clean.length,
  ready: clean.filter((r) => r.status === 'ready').map((r) => ({ id: r.taskId, complexity: r.complexity, needsDesign: r.needsDesign, needsMigration: r.needsMigration, reason: r.reason })),
  alreadyDone: byStatus('already_done'),
  stale: clean.filter((r) => r.status === 'stale').map((r) => ({ id: r.taskId, reason: r.reason })),
  blocked: clean.filter((r) => r.status === 'blocked').map((r) => ({ id: r.taskId, blockers: r.blockers || [], reason: r.reason })),
  all: clean.map((r) => ({ id: r.taskId, status: r.status, complexity: r.complexity })),
}
