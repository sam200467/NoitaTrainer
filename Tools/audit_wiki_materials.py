# -*- coding: utf-8 -*-
"""Audit the bundled material catalog against the current zh Noita Wiki material table."""
import html
import json
import re
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent
PROJECT = ROOT / "NoitaTrainer" if (ROOT / "NoitaTrainer").is_dir() else ROOT.parent
CATALOG = PROJECT / "Assets" / "catalog.json"
API = "https://noita.wiki.gg/zh/api.php"

SECTIONS = [
    ("火焰", "spark"),
    ("砂", "powder"),
    ("液体", "liquid"),
    ("气体", "gas"),
    ("固体", "solid"),
    ("Box2D", "box2d"),
]


def clean_markup(value: str) -> str:
    value = re.sub(r"<[^>]+>", "", value)
    return html.unescape(value).strip()


def fetch_wiki_html() -> str:
    query = urllib.parse.urlencode({
        "action": "parse",
        "page": "材料",
        "prop": "text",
        "format": "json",
        "formatversion": "2",
        "origin": "*",
    })
    request = urllib.request.Request(
        f"{API}?{query}",
        headers={"User-Agent": "NoitaTrainerCatalogAudit/1.0"},
    )
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)["parse"]["text"]


def parse_wiki_materials(page: str) -> list[dict]:
    positions = []
    for label, category in SECTIONS:
        match = re.search(rf'<span class="mw-headline" id="{re.escape(label)}">', page)
        if not match:
            raise RuntimeError(f"Wiki section not found: {label}")
        positions.append((match.start(), label, category))

    result = []
    for index, (start, _, category) in enumerate(positions):
        end = positions[index + 1][0] if index + 1 < len(positions) else page.find("<h2", start + 1)
        section = page[start:end]
        chunks = section.split('<div class="MaterialQueryItem">')[1:]
        for chunk in chunks:
            code_match = re.search(r'<code id="MaterialQueryCodeItem">([^<]+)</code>', chunk)
            bold_match = re.search(r'<b>(.*?)</b>', chunk, re.S)
            english_match = re.search(
                r'<div style="flex-basis:\s*203px;">\s*(?:<a[^>]*>)?([^<\r\n]+)',
                chunk,
                re.S,
            )
            if not code_match or not bold_match:
                continue
            result.append({
                "id": clean_markup(code_match.group(1)),
                "zh": clean_markup(bold_match.group(1)),
                "en": clean_markup(english_match.group(1)) if english_match else "",
                "category": category,
            })
    return result


def main() -> None:
    catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
    current = {item["id"]: item for item in catalog["materials"]}
    wiki_list = parse_wiki_materials(fetch_wiki_html())
    wiki = {item["id"]: item for item in wiki_list}

    print("Wiki rows:", len(wiki_list), "unique:", len(wiki))
    for _, category in SECTIONS:
        print(category, sum(item["category"] == category for item in wiki.values()))
    print("Current rows:", len(current))
    print("Missing from current:", sorted(set(wiki) - set(current)))
    print("Not on Wiki:", sorted(set(current) - set(wiki)))

    category_changes = []
    name_changes = []
    missing_names = []
    for material_id in sorted(set(current) & set(wiki)):
        before = current[material_id]
        after = wiki[material_id]
        if before.get("category") != after["category"]:
            category_changes.append((material_id, before.get("category"), after["category"]))
        if after["zh"].startswith("mat_") or "此材料无名" in after["zh"]:
            missing_names.append((material_id, after["zh"], after["en"]))
        elif before.get("zh") != after["zh"]:
            name_changes.append((material_id, before.get("zh"), after["zh"]))

    print("Category changes:", len(category_changes))
    print("Name changes:", len(name_changes))
    print("Wiki missing Chinese names:", len(missing_names))
    print("\nCategory change sample:")
    for row in category_changes[:30]:
        print("  ", row)
    print("\nName changes:")
    for row in name_changes:
        print("  ", row)
    print("\nWiki missing names:")
    for row in missing_names:
        print("  ", row)

    (ROOT / "wiki_materials_current.json").write_text(
        json.dumps(wiki_list, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
