# Agent Orkestrasyon — feature-pipeline + loop

Bu klasör backlog task'larını rol-sınırlı agent zincirinden otonom geçiren sistemi tutar.
Önceki üç deneme (tmux teammates / sıralı subagent / ham workflow) neden başarısız oldu ve
bu tasarım onları nasıl çözüyor — en altta "Neden bu tasarım" bölümünde.

## Parçalar

| Dosya | Görev |
|---|---|
| `feature-pipeline.js` | Tek task'ı PA→(Designer)→(DB)→SWE→eş-review→QA zincirinden geçiren Workflow scripti |
| `../scripts/next-task.py` | Loop için sıradaki uygun task'ı seçer / done işaretler |
| `../agents/*.md` | 6 rol tanımı — her birinde "Headless / Workflow Modu" bölümü (push/soru/başka-agent yok) |
| `docs/tasks/tasks.json` | Makine-okur backlog: `id`, `code`, `dependsOn[]`, `spec`, normalize `status`/`priority` |

## Akış (senin istediğin: PA inceler → TL görev verir → designer → SWE → QA → deploy)

```
[Loop iterasyonu — ana oturum = Team Leader + push-gate]
  1. python3 .claude/scripts/next-task.py            → sıradaki taskId (yoksa NONE → loop biter)
  2. Workflow({scriptPath:".../feature-pipeline.js", args:{taskId, mode:"full"}})
       ├─ PA İnceleme   (pa-entegrasyon)  → kod-önce teyit; stale/already_done/blocked ise ERKEN DUR
       ├─ Tasarım       (designer)        → needsDesign ise: .cshtml + SWE'ye sözleşme
       ├─ DB            (db-entegrasyon)  → needsMigration ise: entity + migration + handoff
       ├─ SWE Impl.     (swe-entegrasyon) → TDD RED→GREEN, build + unit test
       ├─ Eş-Review     (swe-entegrasyon) → SWE-B diff review; blocker varsa 1 tur fix
       └─ QA Kapısı     (qa-entegrasyon)  → DoD; integration/E2E → CI-DEVİR (fail değil)
  3. Workflow döner → outcome:
       - "ready_to_push" → ana oturum: diff incele → commit → git push gitea+origin develop
                            → dev deploy (8085) → (görsel QA) → next-task.py --done <id>
       - "pa_stop"       → PA stale/done/blocked dedi: tanımı güncelle / done işaretle / atla
       - "needs_attention" → QA fail/blocker: blocker listesine bak, elle karar
  4. Sonraki iterasyona geç (1'e dön)
```

**Push YALNIZ ana oturumda.** Workflow subagent'ları git'e dokunmaz (headless kuralı) —
çünkü `develop` push'u otomatik dev deploy tetikler; bu kapı kontrollü kalmalı.
Stage/prod her zaman elle.

## Kullanım

### Tek task — önce ucuz plan (dry), sonra tam (full)
```
# 1) Plan-only kanıt (ucuz, dosya yazmaz): PA + plan
Workflow scriptPath=.../feature-pipeline.js  args={"taskId":"T028","mode":"dry"}

# 2) Plan iyiyse tam pipeline (implement + test)
Workflow scriptPath=.../feature-pipeline.js  args={"taskId":"T028","mode":"full"}
```
`modelOverride:"sonnet"` → maliyet kısar (varsayılan: her agent kendi .md modelini kullanır — SWE/DB/designer opus, PA/QA sonnet).

### Loop (PA backlog'unu sırayla işle)
Ana oturumda `/loop` (self-paced) ile:
> Her iterasyonda: `next-task.py` ile sıradaki taskId'yi al; NONE ise dur. feature-pipeline'ı
> mode:full + o taskId ile çalıştır. ready_to_push → diff incele, build+unit doğrula, gitea+origin
> develop'a push, `next-task.py --done <id>`. pa_stop/needs_attention → raporla, o task'ı atla
> (veya stale ise tanımı düzelt), sonrakine geç.

`mode:dry` ile loop = backlog'un tamamına ucuz **triyaj** (her task ready/stale/done/blocked mı + plan)
— gece bırak, sabah hangi task'lar gerçekten hazır gör.

## Doğrulanmış davranışlar (2026-06-13)

- **args STRING gelir** — `JSON.parse(args)` şart (önceki "args ulaşmıyor" teşhisi yanlıştı; ulaşıyor ama JSON-string). feature-pipeline başında parse edilir.
- **Named workflow çözülmüyor** — `Workflow({name:"feature-pipeline"})` "not found" verir; `scriptPath` ile çağır.
- **agentType custom agent'ı çözer** — `agentType:"swe-entegrasyon"` agent .md system prompt'unu yükler; workflow schema talimatını ekler. Prompt'ta "WORKFLOW MODU" ibaresi agent'ın headless kurallarını tetikler.
- **Integration/E2E headless'ta yok** — Docker/Testcontainers harness'ta ölür (exit 144). Kanıt = build + unit; integration stage CI'da, görsel/E2E deploy sonrası.

## Neden bu tasarım (önceki 3 denemenin dersleri)

1. **tmux teammates** → token-ağır (idle burn + koordinasyon sohbeti), reaping/sessiz ölüm, peer SendMessage deadlock. → Çözüm: ephemeral workflow subagent'ları, idle burn yok, dönünce yapılandırılmış veri.
2. **sıralı subagent** → "tek agent her şeyi yaptı" (rol sınırı yok). → Çözüm: her aşama `agentType` ile spesifik role pinli + dar prompt; script sırayı zorlar (model değil).
3. **ham workflow** → "çıktı alamadım": args ulaşmadı sanıldı (aslında string'di) + agent'lar interaktif kapıda/integration-test'te takıldı. → Çözüm: args parse + agent'lara headless modu + integration→CI-DEVİR + erken-dur.
4. **Push subagent'ta değil** → develop push = oto-deploy; tek kapı (ana oturum). Senin "tam otonom" tercihinde bile push'u TL yapar, subagent değil.
