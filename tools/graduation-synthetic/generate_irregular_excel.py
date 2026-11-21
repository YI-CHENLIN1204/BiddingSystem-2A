#!/usr/bin/env python3
"""Create synthetic irregular multi-sheet BOQ for 2A parser robustness tests.

Offline; no real construction data and no database access.
"""
import argparse
import csv
import hashlib
import io
import json
import random
from decimal import Decimal
from pathlib import Path
from xml.sax.saxutils import escape
from zipfile import ZipFile, ZipInfo, ZIP_DEFLATED

BASE = 'http://schemas.openxmlformats.org/spreadsheetml/2006/main'
REL = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships'
ZIP_TIME = (2020, 1, 1, 0, 0, 0)

ALIASES = {
    'SYN-REBAR': [
        'SD420W D19 竹節鋼筋加工及組立',
        '結構體鋼筋續接、加工綁紮（含耗損）',
        '基礎版主筋 D16～D22 綁紮施工',
        '鋼筋工程－柱梁牆筋材料及工資',
    ],
    'SYN-CONCRETE': [
        '預拌混凝土 280kgf/cm²（含泵送）',
        '基礎底版混凝土澆置 f′c=280',
        '柱梁版結構混凝土／預拌料',
        '混凝土工程－樓板灌漿及搗實',
    ],
    'SYN-STEEL': [
        'H型鋼梁柱構件製作及吊裝',
        '鋼構件加工組立（含接合板）',
        '結構鋼骨工程 H-300×150 配件',
        '鋼構主體安裝／現場接合',
    ],
}
LAYOUTS = [
    ['項次', '項目名稱', '規格', '單位', '數量', '單價', '金額', '備註'],
    ['序號', '工項名稱', '材料規格', '單位', '工程數量', '單價', '複價'],
    ['項次', '品名', '單位', '規格', '單價', '數量', '總價'],
    ['編號', '項目及說明', '規格', '工程數量', '單位', '單價', '複價'],
    ['項次', '名稱與說明', '規格', '單位', '數量', '單價', '金額', '備註'],
    ['序號', '項目名稱', '規格', '單位', '單價', '工程數量', '總價'],
]
SHEETS = ['結構體明細', '基礎及地下工程', '樓層材料明細', '防水門窗及外裝', '機電消防及弱電', '室內裝修與雜項']

EXTRA_ITEMS = [
    ('臨時圍籬組立及拆除（含安全設施）', 'm', '假設工程'),
    ('基坑土方開挖與運棄（含機具）', 'm3', '土方工程'),
    ('地下室擋土支撐構件組立', '式', '基礎工程'),
    ('地坪級配料回填夯實', 'm3', '土方工程'),
    ('普通模板組立及拆除（柱梁）', 'm2', '結構體工程'),
    ('樓版支撐及鋁模租賃', 'm2', '結構體工程'),
    ('外牆防水底塗與 PU 彈性塗膜', 'm2', '防水工程'),
    ('地下室連續壁止水處理', 'm', '防水工程'),
    ('AL 鋁窗框含玻璃安裝', '樘', '門窗工程'),
    ('不鏽鋼欄杆製作及固定', 'm', '金屬工程'),
    ('電氣配管 EMT 25mm 及管件', 'm', '水電工程'),
    ('弱電線槽與機櫃設備安裝', '組', '弱電工程'),
    ('給排水管路 PVC 4吋 配設', 'm', '水電工程'),
    ('室內 60×60 磁磚鋪貼含打底', 'm2', '裝修工程'),
    ('防火門框扇製作含五金', '樘', '門窗工程'),
    ('屋頂排水與泛水收邊', 'm', '屋頂工程'),
    ('施工安全監測沉陷觀測點', '點', '安全觀測工程'),
    ('工區臨時供水配管系統', '式', '假設工程'),
    ('連續壁導溝開挖及回填', 'm', '基礎工程'),
    ('基礎筏式底版止水帶施工', 'm', '防水工程'),
    ('接地銅排及接地極施作', '組', '電氣工程'),
    ('PVC-U 排水管及管件安裝', 'm', '給排水工程'),
    ('樓梯間油漆批土及面漆', 'm2', '裝修工程'),
    ('屋頂防水保護層及隔熱材', 'm2', '防水工程'),
    ('外牆石材乾掛骨架系統', 'm2', '外牆工程'),
    ('輕隔間 C 型骨架與雙面封板', 'm2', '裝修工程'),
    ('公共區域天花板輕鋼架', 'm2', '裝修工程'),
    ('機房消防灑水配管與測試', 'm', '消防工程'),
    ('照明迴路配管配線施工', 'm', '電氣工程'),
    ('地下停車場標線及標誌', 'm2', '雜項工程'),
    ('戶外人行步道透水鋪面', 'm2', '景觀工程'),
    ('混凝土表面修補及粉光', 'm2', '泥作工程'),
    ('防火區劃貫穿部填塞', '處', '消防工程'),
    ('空調冷媒銅管保溫及安裝', 'm', '空調工程'),
    ('電梯門框收邊及修飾', '處', '電梯工程'),
    ('衛浴設備給水配管及測試', '組', '給排水工程'),
    ('陽台滴水線及收邊施作', 'm', '外牆工程'),
    ('地下室地坪耐磨塗裝', 'm2', '裝修工程'),
    ('弱電資訊插座與面板配設', '只', '弱電工程'),
    ('工區臨時電力分盤設置', '組', '假設工程'),
    ('屋頂設備基座澆置及固定', '座', '屋頂工程'),
]


def excel_col(n):
    result = ''
    while n:
        n, rem = divmod(n - 1, 26)
        result = chr(65 + rem) + result
    return result


def cell_xml(ref, value):
    if value is None or value == '':
        return ''
    if isinstance(value, (int, Decimal, float)):
        return f'<c r="{ref}" s="1"><v>{value}</v></c>'
    return f'<c r="{ref}" s="1" t="inlineStr"><is><t>{escape(str(value))}</t></is></c>'


def worksheet_xml(rows, headers=None):
    result = []
    for n, values in enumerate(rows, 1):
        cells = ''.join(cell_xml(f'{excel_col(i)}{n}', v) for i, v in enumerate(values, 1))
        # Comfortable row height for long construction item names and specs.
        row_height = 42 if n > 4 else 30
        result.append(f'<row r="{n}" ht="{row_height}" customHeight="1">{cells}</row>')
    max_cols = max((len(row) for row in rows), default=1)
    names = headers or []
    widths = []
    for index in range(max_cols):
        label = names[index] if index < len(names) else ''
        # The summary sheet has no typed column headers; preserve enough space
        # for the project title and description rather than defaulting to 17.
        width = 38 if not headers else 68 if label in ('項目名稱', '工項名稱', '品名', '名稱與說明', '項目及說明') else 36 if label in ('規格', '材料規格') else 28 if label == '備註' else 21 if label in ('金額', '總價', '複價', '單價') else 17
        widths.append(f'<col min="{index+1}" max="{index+1}" width="{width}" customWidth="1"/>')
    return f'<?xml version="1.0" encoding="utf-8"?><worksheet xmlns="{BASE}"><cols>{"".join(widths)}</cols><sheetData>{"".join(result)}</sheetData></worksheet>'


def make_xlsx(sheets):
    if len(set(sheets)) != len(sheets):
        raise ValueError('duplicate sheet names')
    strings = [f'<sheet name="{escape(name)}" sheetId="{i}" r:id="rId{i}"/>' for i, name in enumerate(sheets, 1)]
    sheet_rels = [f'<Relationship Id="rId{i}" Type="{REL}/worksheet" Target="worksheets/sheet{i}.xml"/>' for i in range(1, len(sheets)+1)]
    sheet_rels.append(f'<Relationship Id="rId{len(sheets)+1}" Type="{REL}/styles" Target="styles.xml"/>')
    overrides = ''.join(f'<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>' for i in range(1, len(sheets)+1))
    parts = {
        '[Content_Types].xml': ('<?xml version="1.0" encoding="utf-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>' + overrides + '<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>'),
        '_rels/.rels': f'<?xml version="1.0" encoding="utf-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="{REL}/officeDocument" Target="xl/workbook.xml"/></Relationships>',
        'xl/workbook.xml': f'<?xml version="1.0" encoding="utf-8"?><workbook xmlns="{BASE}" xmlns:r="{REL}"><sheets>{"".join(strings)}</sheets></workbook>',
        'xl/_rels/workbook.xml.rels': f'<?xml version="1.0" encoding="utf-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">{"".join(sheet_rels)}</Relationships>',
    }
    parts['xl/styles.xml'] = (
        f'<?xml version="1.0" encoding="utf-8"?><styleSheet xmlns="{BASE}">'
        '<fonts count="1"><font><sz val="11"/><name val="Microsoft JhengHei"/></font></fonts>'
        '<fills count="2"><fill><patternFill patternType="none"/></fill>'
        '<fill><patternFill patternType="gray125"/></fill></fills>'
        '<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>'
        '<cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>'
        '<cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>'
        '<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1">'
        '<alignment vertical="center" wrapText="1"/></xf></cellXfs></styleSheet>'
    )
    for i, rows in enumerate(sheets.values(), 1):
        # Sheet one is the summary; detail header rows vary among sheets.
        header = None if i == 1 else LAYOUTS[(i - 2) % len(LAYOUTS)]
        parts[f'xl/worksheets/sheet{i}.xml'] = worksheet_xml(rows, header)
    output = io.BytesIO()
    with ZipFile(output, 'w') as workbook:
        for filename, payload in parts.items():
            info = ZipInfo(filename, ZIP_TIME)
            info.compress_type = ZIP_DEFLATED
            workbook.writestr(info, payload.encode('utf-8'))
    return output.getvalue()


def load_checked_source(source):
    manifest = json.loads((source / 'manifest.json').read_text(encoding='utf-8'))
    if manifest.get('data_origin') != 'SYNTHETIC':
        raise ValueError('only SYNTHETIC input allowed')
    for filename in ('projects.csv', 'historical_transactions.csv'):
        digest = hashlib.sha256((source / filename).read_bytes()).hexdigest()
        if digest != manifest.get('files_sha256', {}).get(filename):
            raise ValueError(f'SHA256 mismatch: {filename}')
    with (source / 'projects.csv').open(encoding='utf-8-sig', newline='') as f:
        projects = list(csv.DictReader(f))
    with (source / 'historical_transactions.csv').open(encoding='utf-8-sig', newline='') as f:
        txs = list(csv.DictReader(f))
    if len(projects) != manifest['project_count'] or len(txs) != manifest['transaction_count']:
        raise ValueError('row count mismatch')
    return projects, txs


def generate(source, xlsx_path, truth_path, project_id=None, seed=4102, items=360):
    if not 60 <= items <= 2000:
        raise ValueError('items must be between 60 and 2000')
    if xlsx_path.exists() or truth_path.exists():
        raise ValueError('refusing to overwrite existing output')
    if xlsx_path.resolve() == truth_path.resolve():
        raise ValueError('output paths must differ')
    projects, txs = load_checked_source(source)
    if len({p['synthetic_project_id'] for p in projects}) != len(projects):
        raise ValueError('duplicate source project ids')
    chosen = next((p for p in projects if p['synthetic_project_id'] == project_id), None) if project_id else next((p for p in projects if p['split'] == 'test'), None)
    if not chosen or chosen['data_origin'] != 'SYNTHETIC':
        raise ValueError('synthetic project not found')
    target = chosen['synthetic_project_id']
    tx = [t for t in txs if t['synthetic_project_id'] == target]
    if not tx or any(t['data_origin'] != 'SYNTHETIC' or t['split'] != chosen['split'] for t in tx):
        raise ValueError('orphan or incompatible synthetic transaction')
    rng = random.Random(f'{seed}:{target}')
    sheets = {'工程總表': [
        ['合成建案測試標單（示意）'], ['工程名稱', chosen['project_name']],
        ['說明', '此工作表為摘要，詳細工程項目分散於其他工作表'],
        ['工項分類', '金額'], ['結構體工程', '僅示意'],
    ]}
    for n, name in enumerate(SHEETS):
        rows = [[f'測試工程清單 / {name}'], [], ['版次', f'模擬版-{n+1}'], []]
        if n == 1:
            rows.extend([['施工註記', '數量以後續確認為準'], []])
        rows.extend([LAYOUTS[n], ['壹', '結構體工程']])
        sheets[name] = rows
    truth = []
    # Same material is split across sheets: this is NOT a duplicate transaction.
    for transaction in tx:
        code = transaction['material_code']
        if code not in ALIASES:
            raise ValueError(f'unknown material code: {code}')
        quantity = Decimal(transaction['actual_quantity'])
        unit_price = Decimal(transaction['unit_price'])
        if quantity <= 0 or unit_price <= 0:
            raise ValueError('source requires positive quantity and price')
        parts = [(quantity * Decimal('0.43')).quantize(Decimal('0.001')),
                 (quantity * Decimal('0.37')).quantize(Decimal('0.001'))]
        parts.append(quantity - sum(parts))
        scenarios = [('complete', 0), ('missing_unit_price', 1), ('missing_quantity', 2)]
        for part_i, (scenario, sheet_i) in enumerate(scenarios):
            part_qty = parts[part_i]
            row = {'項次': str(len(truth)+1), '序號': str(len(truth)+1),
                   '項目名稱': ALIASES[code][(part_i + rng.randrange(4)) % 4],
                   '工項名稱': ALIASES[code][(part_i + rng.randrange(4)) % 4],
                   '品名': ALIASES[code][(part_i + rng.randrange(4)) % 4],
                   '項目及說明': ALIASES[code][(part_i + rng.randrange(4)) % 4],
                   '名稱與說明': ALIASES[code][(part_i + rng.randrange(4)) % 4],
                   '編號': str(len(truth)+1),
                   '材料規格': f'{code} / 合成規格', '規格': f'{code} / 合成規格',
                   '單位': transaction['unit'], '數量': part_qty,
                   '工程數量': part_qty, '單價': unit_price,
                   '金額': (part_qty * unit_price).quantize(Decimal('0.01')),
                   '複價': (part_qty * unit_price).quantize(Decimal('0.01')),
                   '總價': (part_qty * unit_price).quantize(Decimal('0.01')),
                   '備註': '合成測試資料'}
            if scenario == 'missing_unit_price':
                row['單價'] = None
                row['金額'] = None
                row['複價'] = None
                row['總價'] = None
            elif scenario == 'missing_quantity':
                row['數量'] = None
                row['工程數量'] = None
                row['金額'] = None
                row['複價'] = None
                row['總價'] = None
            sheet_name = SHEETS[sheet_i]
            values = [row.get(h, '') for h in LAYOUTS[sheet_i]]
            # Ensure only one name alias per physical row, including mixed layouts.
            name = next((row[h] for h in LAYOUTS[sheet_i] if h in ('項目名稱', '工項名稱', '品名', '項目及說明', '名稱與說明')), '')
            sheet_row = len(sheets[sheet_name])+1
            sheets[sheet_name].append(values)
            truth.append({'sheet': sheet_name, 'excel_row': sheet_row, 'source_synthetic_transaction_id': transaction['synthetic_transaction_id'],
                          'material_code': code, 'rendered_item_name': name, 'scenario': scenario,
                          'expected_quantity': str(part_qty) if scenario != 'missing_quantity' else None,
                          'expected_unit_price': str(unit_price) if scenario != 'missing_unit_price' else None,
                          'source_data_origin': 'SYNTHETIC', 'split': chosen['split']})
    # Project-dependent scope: no underground-floor items for buildings
    # without a basement; avoid floors which exceed actual building height.
    basement_count = int(chosen['basement_floor_count'])
    floor_count = int(chosen['floor_count'])
    if basement_count < 0 or floor_count < 1:
        raise ValueError('invalid synthetic project floors')
    underground_keywords = ('地下', '連續壁', '擋土', '基坑')
    eligible_items = [
        item for item in EXTRA_ITEMS
        if basement_count > 0 or not any(k in item[0] for k in underground_keywords)
    ]
    above_ground_levels = [f'{f}F' for f in range(1, floor_count + 1)]
    floors = ([f'B{f}' for f in range(1, basement_count + 1)] +
              above_ground_levels + ['RF'])
    zones = (['A棟', 'B棟', '公共區域', '屋頂層'] +
             (['地下停車場', '地下機房'] if basement_count else ['戶外地坪', '一樓大廳']))
    # Extra non-core construction work items exercise unmapped-item workflows.
    # These rows are synthetic test fixtures, NOT added to historical_transactions.csv.
    extra_count = items - len(truth)
    if extra_count < 0:
        raise ValueError('requested row count below core test rows')
    for extra_idx in range(extra_count):
        label, unit, category = eligible_items[extra_idx % len(eligible_items)]
        # Group by work type to mimic disciplinary worksheets, with structural
        # details split across two structural sheets for cross-sheet testing.
        if category in ('結構體工程', '泥作工程'):
            sheet_idx = (0 if extra_idx % 2 == 0 else 2)
        elif category in ('假設工程', '土方工程', '基礎工程', '安全觀測工程'):
            sheet_idx = 1
        elif category in ('防水工程', '門窗工程', '金屬工程', '外牆工程', '屋頂工程'):
            sheet_idx = 3
        elif category in ('水電工程', '弱電工程', '電氣工程', '給排水工程', '消防工程', '空調工程'):
            sheet_idx = 4
        else:
            sheet_idx = 5
        sheet_name = SHEETS[sheet_idx]
        quantity = Decimal(rng.randint(1, 500)) + Decimal(str(rng.randrange(10))) / Decimal('10')
        unit_price = Decimal(rng.randint(5, 900))
        marker = extra_idx % 20
        scenario = ('missing_unit' if marker == 0 else
                    'missing_unit_price' if marker in (1, 2) else
                    'missing_quantity' if marker == 3 else
                    'unmapped_extra' if marker in (4, 5) else 'complete')
        zone = zones[(extra_idx // len(eligible_items)) % len(zones)]
        level = floors[(extra_idx // 6) % len(floors)]
        spec = ('標準型', '加強型', '耐候型', '防火型', '依圖說', '含施工測試')[(extra_idx // 11) % 6]
        item_label = f'{label}－{zone}/{level}（{spec}）'
        row = {'項次': f'E{extra_idx+1:03d}', '序號': f'E{extra_idx+1:03d}', '編號': f'E{extra_idx+1:03d}',
               '項目名稱': item_label, '工項名稱': item_label, '品名': item_label,
               '項目及說明': item_label, '名稱與說明': item_label,
               '材料規格': category, '規格': category, '單位': unit,
               '數量': quantity, '工程數量': quantity, '單價': unit_price,
               '金額': quantity * unit_price, '複價': quantity * unit_price, '總價': quantity * unit_price,
               '備註': '合成測試'}
        if scenario == 'missing_unit':
            row['單位'] = None
        elif scenario == 'missing_quantity':
            row['數量'] = None
            row['工程數量'] = None
            for amount_key in ('金額', '複價', '總價'):
                row[amount_key] = None
        elif scenario == 'missing_unit_price':
            row['單價'] = None
            for amount_key in ('金額', '複價', '總價'):
                row[amount_key] = None
        sheets[sheet_name].append([row.get(h, '') for h in LAYOUTS[sheet_idx]])
        truth.append({'sheet': sheet_name, 'excel_row': len(sheets[sheet_name]),
                      'source_synthetic_transaction_id': None, 'material_code': None,
                      'rendered_item_name': item_label, 'scenario': scenario,
                      'expected_quantity': str(quantity) if scenario != 'missing_quantity' else None,
                      'expected_unit_price': str(unit_price) if scenario != 'missing_unit_price' else None,
                      'source_data_origin': 'SYNTHETIC', 'split': chosen['split'],
                      'expected_mapping': 'unmapped_by_default'})
    for name in SHEETS:
        sheets[name].extend([[], ['備註', '以下留白'], ['合計', '測試彙總列，解析時應略過']])
    blob = make_xlsx(sheets)
    truth_data = {'data_origin': 'SYNTHETIC', 'source_project_id': target, 'source_split': chosen['split'],
                  'seed': seed, 'worksheets': list(sheets), 'xlsx_sha256': hashlib.sha256(blob).hexdigest(),
                  'project': {k: chosen[k] for k in ('project_type','structure_type','city','total_floor_area','floor_count','basement_floor_count','start_date')},
                  'composition_policy': 'Floor counts and basement-dependent work items enforced; other BOQ quantity assumptions remain illustrative.',
                  'cases': truth,
                  'requested_detail_rows': items,
                  'actual_detail_rows': len(truth),
                  'detail_rows_per_sheet': {name: sum(1 for case in truth if case['sheet'] == name) for name in SHEETS},
                  'warnings': ['Synthetic aliases are illustrative; not verified against real enterprise templates.',
                               'Missing quantities/prices are intentional malformed input, not suitable for history.',
                               'The missing_quantity line contributes to source total but is intentionally unreadable.',
                               'Do not import to production or compute real-world accuracy.']}
    xlsx_path.parent.mkdir(parents=True, exist_ok=True)
    truth_path.parent.mkdir(parents=True, exist_ok=True)
    xlsx_path.write_bytes(blob)
    truth_path.write_text(json.dumps(truth_data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    return {'project_id': target, 'sheets': list(sheets), 'test_cases': len(truth),
            'detail_rows_per_sheet': truth_data['detail_rows_per_sheet'],
            'xlsx_sha256': truth_data['xlsx_sha256']}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--truth', type=Path, required=True)
    parser.add_argument('--project-id')
    parser.add_argument('--seed', type=int, default=4102)
    parser.add_argument('--items', type=int, default=360, help='Total detail rows (60-2000); default 360')
    args = parser.parse_args(argv)
    try:
        print(json.dumps(generate(args.source, args.output, args.truth, args.project_id, args.seed, args.items), ensure_ascii=False, indent=2))
    except (ValueError, KeyError, OSError, json.JSONDecodeError) as error:
        parser.error(str(error))


if __name__ == '__main__':
    main()