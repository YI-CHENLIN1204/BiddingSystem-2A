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
]
SHEETS = ['結構體明細', '基礎及地下工程', '樓層材料明細']


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
        return f'<c r="{ref}"><v>{value}</v></c>'
    return f'<c r="{ref}" t="inlineStr"><is><t>{escape(str(value))}</t></is></c>'


def worksheet_xml(rows):
    result = []
    for n, values in enumerate(rows, 1):
        cells = ''.join(cell_xml(f'{excel_col(i)}{n}', v) for i, v in enumerate(values, 1))
        result.append(f'<row r="{n}">{cells}</row>')
    return f'<?xml version="1.0" encoding="utf-8"?><worksheet xmlns="{BASE}"><sheetData>{"".join(result)}</sheetData></worksheet>'


def make_xlsx(sheets):
    if len(set(sheets)) != len(sheets):
        raise ValueError('duplicate sheet names')
    strings = [f'<sheet name="{escape(name)}" sheetId="{i}" r:id="rId{i}"/>' for i, name in enumerate(sheets, 1)]
    sheet_rels = [f'<Relationship Id="rId{i}" Type="{REL}/worksheet" Target="worksheets/sheet{i}.xml"/>' for i in range(1, len(sheets)+1)]
    overrides = ''.join(f'<Override PartName="/xl/worksheets/sheet{i}.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>' for i in range(1, len(sheets)+1))
    parts = {
        '[Content_Types].xml': ('<?xml version="1.0" encoding="utf-8"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>' + overrides + '</Types>'),
        '_rels/.rels': f'<?xml version="1.0" encoding="utf-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="{REL}/officeDocument" Target="xl/workbook.xml"/></Relationships>',
        'xl/workbook.xml': f'<?xml version="1.0" encoding="utf-8"?><workbook xmlns="{BASE}" xmlns:r="{REL}"><sheets>{"".join(strings)}</sheets></workbook>',
        'xl/_rels/workbook.xml.rels': f'<?xml version="1.0" encoding="utf-8"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">{"".join(sheet_rels)}</Relationships>',
    }
    for i, rows in enumerate(sheets.values(), 1):
        parts[f'xl/worksheets/sheet{i}.xml'] = worksheet_xml(rows)
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


def generate(source, xlsx_path, truth_path, project_id=None, seed=4102):
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
            name = next((row[h] for h in LAYOUTS[sheet_i] if h in ('項目名稱', '工項名稱', '品名')), '')
            sheet_row = len(sheets[sheet_name])+1
            sheets[sheet_name].append(values)
            truth.append({'sheet': sheet_name, 'excel_row': sheet_row, 'source_synthetic_transaction_id': transaction['synthetic_transaction_id'],
                          'material_code': code, 'rendered_item_name': name, 'scenario': scenario,
                          'expected_quantity': str(part_qty) if scenario != 'missing_quantity' else None,
                          'expected_unit_price': str(unit_price) if scenario != 'missing_unit_price' else None,
                          'source_data_origin': 'SYNTHETIC', 'split': chosen['split']})
    for name in SHEETS:
        sheets[name].extend([[], ['備註', '以下留白'], ['合計', '測試彙總列，解析時應略過']])
    blob = make_xlsx(sheets)
    truth_data = {'data_origin': 'SYNTHETIC', 'source_project_id': target, 'source_split': chosen['split'],
                  'seed': seed, 'worksheets': list(sheets), 'xlsx_sha256': hashlib.sha256(blob).hexdigest(),
                  'project': {k: chosen[k] for k in ('project_type','structure_type','city','total_floor_area','floor_count','basement_floor_count','start_date')},
                  'cases': truth,
                  'warnings': ['Synthetic aliases are illustrative; not verified against real enterprise templates.',
                               'Missing quantities/prices are intentional malformed input, not suitable for history.',
                               'The missing_quantity line contributes to source total but is intentionally unreadable.',
                               'Do not import to production or compute real-world accuracy.']}
    xlsx_path.parent.mkdir(parents=True, exist_ok=True)
    truth_path.parent.mkdir(parents=True, exist_ok=True)
    xlsx_path.write_bytes(blob)
    truth_path.write_text(json.dumps(truth_data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    return {'project_id': target, 'sheets': list(sheets), 'test_cases': len(truth), 'xlsx_sha256': truth_data['xlsx_sha256']}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--truth', type=Path, required=True)
    parser.add_argument('--project-id')
    parser.add_argument('--seed', type=int, default=4102)
    args = parser.parse_args(argv)
    try:
        print(json.dumps(generate(args.source, args.output, args.truth, args.project_id, args.seed), ensure_ascii=False, indent=2))
    except (ValueError, KeyError, OSError, json.JSONDecodeError) as error:
        parser.error(str(error))


if __name__ == '__main__':
    main()