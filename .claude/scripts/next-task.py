#!/usr/bin/env python3
"""Loop sürücüsü için sıradaki uygun task'ı seçer.

tasks.json'dan: status==todo + priority in (critical,high,medium) +
tüm dependsOn'ları done olan task'lardan en yüksek öncelikliyi döner.

Kullanım:
  python3 .claude/scripts/next-task.py            # sıradaki task'ın id'sini bas (yoksa NONE)
  python3 .claude/scripts/next-task.py --json     # tam task objesini bas
  python3 .claude/scripts/next-task.py --list      # tüm uygun task'ları sırayla listele
  python3 .claude/scripts/next-task.py --done T028 # T028'i done işaretle (loop, push sonrası çağırır)
"""
import json
import sys

TASKS = "docs/tasks/tasks.json"
PRIO = {"critical": 0, "high": 1, "medium": 2, "low": 3}


def load():
    with open(TASKS) as f:
        return json.load(f)


def save(tasks):
    with open(TASKS, "w") as f:
        json.dump(tasks, f, ensure_ascii=False, indent=2)


def eligible(tasks):
    done = {t["id"] for t in tasks if t["status"] == "done"}
    out = []
    for t in tasks:
        if t["status"] != "todo":
            continue
        if PRIO.get(t["priority"], 9) > 2:
            continue
        deps = t.get("dependsOn", [])
        if any(d not in done for d in deps):
            continue  # bağımlılık tamamlanmamış — atla
        out.append(t)
    out.sort(key=lambda t: (PRIO[t["priority"]], t["id"]))
    return out


def main():
    args = sys.argv[1:]
    tasks = load()

    if args and args[0] == "--done":
        tid = args[1]
        hit = next((t for t in tasks if t["id"] == tid), None)
        if not hit:
            print(f"BULUNAMADI: {tid}", file=sys.stderr)
            sys.exit(1)
        hit["status"] = "done"
        save(tasks)
        print(f"DONE: {tid}")
        return

    elig = eligible(tasks)
    if args and args[0] == "--list":
        for t in elig:
            blk = "" if not t.get("dependsOn") else f" deps={t['dependsOn']}"
            print(f"{t['id']} [{t['priority']}] {t['task'][:60]}{blk}")
        print(f"# toplam uygun: {len(elig)}")
        return

    if not elig:
        print("NONE")
        return

    if args and args[0] == "--json":
        print(json.dumps(elig[0], ensure_ascii=False))
    else:
        print(elig[0]["id"])


if __name__ == "__main__":
    main()
