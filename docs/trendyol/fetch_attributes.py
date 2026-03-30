#!/usr/bin/env python3
"""Fetch Trendyol category attributes for all leaf categories."""
import json
import time
import urllib.request
import urllib.error
import sys
import os

CATS_FILE = os.path.join(os.path.dirname(__file__), "categories-snapshot.json")
ATTRS_FILE = os.path.join(os.path.dirname(__file__), "attributes-snapshot.json")
API_URL = "https://apigw.trendyol.com/integration/product/product-categories/{}/attributes"
SLEEP_SEC = 1.5
SAVE_EVERY = 100

def find_leaves(cats):
    leaves = []
    for c in cats:
        subs = c.get("subCategories", [])
        if not subs:
            leaves.append(c["id"])
        else:
            leaves.extend(find_leaves(subs))
    return leaves

def main():
    # Load category tree
    with open(CATS_FILE, "r", encoding="utf-8") as f:
        cat_data = json.load(f)
    all_leaves = find_leaves(cat_data["categories"])
    print(f"Total leaf categories: {len(all_leaves)}")

    # Load existing attributes
    if os.path.exists(ATTRS_FILE):
        with open(ATTRS_FILE, "r", encoding="utf-8") as f:
            attrs = json.load(f)
    else:
        attrs = {}

    # Keys are strings in JSON
    already_fetched = set(attrs.keys())
    remaining = [cid for cid in all_leaves if str(cid) not in already_fetched]
    print(f"Already fetched: {len(already_fetched)}")
    print(f"Remaining: {len(remaining)}")

    if not remaining:
        print("Nothing to fetch. Done.")
        return

    fetched_count = 0
    error_count = 0
    start_time = time.time()

    for i, cid in enumerate(remaining):
        url = API_URL.format(cid)
        try:
            req = urllib.request.Request(url)
            req.add_header("User-Agent", "Mozilla/5.0")
            with urllib.request.urlopen(req, timeout=30) as resp:
                data = json.loads(resp.read().decode("utf-8"))
            attrs[str(cid)] = data
            fetched_count += 1
        except (urllib.error.HTTPError, urllib.error.URLError, Exception) as e:
            error_count += 1
            print(f"  ERROR category {cid}: {e}", file=sys.stderr)

        # Progress & save
        done = i + 1
        if done % SAVE_EVERY == 0 or done == len(remaining):
            elapsed = time.time() - start_time
            rate = done / elapsed if elapsed > 0 else 0
            eta_min = (len(remaining) - done) / rate / 60 if rate > 0 else 0
            print(f"[{done}/{len(remaining)}] fetched={fetched_count} errors={error_count} "
                  f"elapsed={elapsed/60:.1f}min ETA={eta_min:.1f}min total_keys={len(attrs)}")
            # Save
            with open(ATTRS_FILE, "w", encoding="utf-8") as f:
                json.dump(attrs, f, ensure_ascii=False)
            print(f"  Saved {len(attrs)} categories to {ATTRS_FILE}")

        time.sleep(SLEEP_SEC)

    print(f"\nDone! Total in snapshot: {len(attrs)}, errors: {error_count}")

if __name__ == "__main__":
    main()
