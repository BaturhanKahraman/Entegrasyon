"""
Trendyol API'den tüm yaprak kategorilerin attribute + value bilgilerini çeker.
Sonucu docs/trendyol/attributes-snapshot.json olarak kaydeder.
Ayrıca markaları da docs/trendyol/brands-snapshot.json olarak kaydeder.
"""
import json
import asyncio
import aiohttp
import time
import os

BASE_URL = "https://apigw.trendyol.com/integration/product"
HEADERS = {"User-Agent": "Entegrasyon/1.0"}
DOCS_DIR = os.path.join(os.path.dirname(__file__), "..", "docs", "trendyol")
CATEGORIES_FILE = os.path.join(DOCS_DIR, "categories-snapshot.json")
ATTRIBUTES_OUT = os.path.join(DOCS_DIR, "attributes-snapshot.json")
BRANDS_OUT = os.path.join(DOCS_DIR, "brands-snapshot.json")
CONCURRENT = 10  # parallel requests


def collect_leaves(categories, leaves=None):
    if leaves is None:
        leaves = []
    for cat in categories:
        subs = cat.get("subCategories") or []
        if not subs:
            leaves.append(cat["id"])
        else:
            collect_leaves(subs, leaves)
    return leaves


async def fetch_attributes(session, sem, cat_id):
    url = f"{BASE_URL}/categories/{cat_id}/attributes"
    async with sem:
        try:
            async with session.get(url) as resp:
                if resp.status != 200:
                    return cat_id, None
                data = await resp.json()
                return cat_id, data
        except Exception:
            return cat_id, None


async def fetch_values(session, sem, cat_id, attr_id):
    url = f"{BASE_URL}/categories/{cat_id}/attributes/{attr_id}/values?size=1000"
    async with sem:
        try:
            async with session.get(url) as resp:
                if resp.status != 200:
                    return attr_id, []
                data = await resp.json()
                return attr_id, data.get("content", [])
        except Exception:
            return attr_id, []


async def fetch_all_attributes():
    with open(CATEGORIES_FILE) as f:
        snapshot = json.load(f)

    leaves = collect_leaves(snapshot["categories"])
    print(f"Toplam yaprak kategori: {len(leaves)}")

    sem = asyncio.Semaphore(CONCURRENT)
    result = {}
    start = time.time()

    async with aiohttp.ClientSession(headers=HEADERS) as session:
        # 1. Tüm yaprak kategorilerin attribute'larını çek
        tasks = [fetch_attributes(session, sem, cid) for cid in leaves]
        done = 0
        for coro in asyncio.as_completed(tasks):
            cat_id, data = await coro
            done += 1
            if done % 200 == 0:
                elapsed = time.time() - start
                print(f"  Attribute: {done}/{len(leaves)} ({elapsed:.0f}s)")

            if data and data.get("categoryAttributes"):
                cat_attrs = []
                for ca in data["categoryAttributes"]:
                    attr = ca.get("attribute", {})
                    cat_attrs.append({
                        "attributeId": attr.get("id"),
                        "attributeName": attr.get("name", ""),
                        "required": ca.get("required", False),
                        "varianter": ca.get("varianter", False),
                        "slicer": ca.get("slicer", False),
                        "allowCustom": ca.get("allowCustom", False),
                        "values": []  # placeholder — aşağıda doldurulacak
                    })
                result[str(cat_id)] = cat_attrs

        print(f"Attribute'lar çekildi: {len(result)} kategoride. ({time.time()-start:.0f}s)")

        # 2. Unique attribute ID'lerini topla ve value'larını çek
        unique_attrs = {}  # attr_id → first_cat_id (value çekmek için)
        for cat_id_str, attrs in result.items():
            for a in attrs:
                aid = a["attributeId"]
                if aid and aid not in unique_attrs:
                    unique_attrs[aid] = int(cat_id_str)

        print(f"Toplam unique attribute: {len(unique_attrs)}, value'ları çekiliyor...")

        value_map = {}  # attr_id → [values]
        tasks2 = [fetch_values(session, sem, cat_id, attr_id) for attr_id, cat_id in unique_attrs.items()]
        done2 = 0
        for coro in asyncio.as_completed(tasks2):
            attr_id, values = await coro
            done2 += 1
            if done2 % 200 == 0:
                elapsed = time.time() - start
                print(f"  Values: {done2}/{len(unique_attrs)} ({elapsed:.0f}s)")
            if values:
                value_map[attr_id] = [{"id": v["attributeValueId"], "name": v["attributeValue"]} for v in values]

        # 3. Value'ları attribute'lara yerleştir
        for cat_id_str, attrs in result.items():
            for a in attrs:
                aid = a["attributeId"]
                if aid in value_map:
                    a["values"] = value_map[aid]

    elapsed = time.time() - start
    total_values = sum(len(v) for v in value_map.values())
    print(f"Tamamlandı: {len(result)} kategori, {len(unique_attrs)} attribute, {total_values} value ({elapsed:.0f}s)")

    with open(ATTRIBUTES_OUT, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=2)
    print(f"Kaydedildi: {ATTRIBUTES_OUT}")


async def fetch_all_brands():
    print("\nMarkalar çekiliyor...")
    all_brands = []
    page = 0
    page_size = 500

    async with aiohttp.ClientSession(headers=HEADERS) as session:
        while True:
            url = f"{BASE_URL}/brands?page={page}&size={page_size}"
            async with session.get(url) as resp:
                if resp.status != 200:
                    break
                data = await resp.json()
                brands = data.get("brands", [])
                if not brands:
                    break
                all_brands.extend(brands)
                print(f"  Sayfa {page}: {len(brands)} marka (toplam: {len(all_brands)})")
                if len(brands) < page_size:
                    break
                page += 1

    print(f"Toplam marka: {len(all_brands)}")

    with open(BRANDS_OUT, "w", encoding="utf-8") as f:
        json.dump({"brands": all_brands}, f, ensure_ascii=False)
    print(f"Kaydedildi: {BRANDS_OUT}")


async def main():
    await fetch_all_attributes()
    await fetch_all_brands()


if __name__ == "__main__":
    asyncio.run(main())
