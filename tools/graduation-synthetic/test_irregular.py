import json
import tempfile
import unittest
from pathlib import Path
from xml.etree import ElementTree as ET
from zipfile import ZipFile
from generate_irregular_excel import generate, BASE
from generate import generate as make_data, write_csv, PROJECT_FIELDS, HISTORY_FIELDS
import hashlib

class IrregularTests(unittest.TestCase):
    def setUp(self):
        self.tmp=tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root=Path(self.tmp.name)
        self.source=self.root/'source';self.source.mkdir()
        p,t=make_data(30,12)
        for fn,fields,rows in [('projects.csv',PROJECT_FIELDS,p),('historical_transactions.csv',HISTORY_FIELDS,t)]:
            write_csv(self.source/fn,fields,rows)
        hashes={f:hashlib.sha256((self.source/f).read_bytes()).hexdigest() for f in ('projects.csv','historical_transactions.csv')}
        (self.source/'manifest.json').write_text(json.dumps({'data_origin':'SYNTHETIC','files_sha256':hashes,'project_count':len(p),'transaction_count':len(t)}),encoding='utf-8')
    def test_deterministic_multisheet_and_defect_truth(self):
        a,b=self.root/'a.xlsx',self.root/'b.xlsx'
        ta,tb=self.root/'a.json',self.root/'b.json'
        generate(self.source,a,ta)
        generate(self.source,b,tb)
        self.assertEqual(a.read_bytes(),b.read_bytes())
        self.assertEqual(json.loads(ta.read_text(encoding='utf-8'))['cases'],json.loads(tb.read_text(encoding='utf-8'))['cases'])
        truth=json.loads(ta.read_text(encoding='utf-8'))
        self.assertEqual(len(truth['worksheets']),7)
        self.assertEqual({x['scenario'] for x in truth['cases']},{'complete','missing_unit_price','missing_quantity','missing_unit','unmapped_extra'})
        self.assertEqual(len(truth['cases']),360)
        self.assertEqual(truth['requested_detail_rows'], 360)
        self.assertEqual(truth['actual_detail_rows'], 360)
        self.assertEqual(sum(truth['detail_rows_per_sheet'].values()), 360)
        self.assertEqual(len(truth['detail_rows_per_sheet']), 6)
        self.assertGreater(len({x['rendered_item_name'] for x in truth['cases']}), 250)
        self.assertGreater(sum(x['scenario']=='complete' for x in truth['cases']), 200)
        for case in truth['cases']:
            if case['scenario']=='missing_quantity':
                self.assertIsNone(case['expected_quantity'])
            if case['scenario']=='missing_unit_price':
                self.assertIsNone(case['expected_unit_price'])
        self.assertEqual(truth['xlsx_sha256'],hashlib.sha256(a.read_bytes()).hexdigest())
        self.assertTrue(all(x['source_data_origin']=='SYNTHETIC' for x in truth['cases']))
        self.assertTrue(all('示意' not in x['rendered_item_name'] for x in truth['cases']))
        with ZipFile(a) as z:
            self.assertIsNone(z.testzip())
            self.assertEqual(len([n for n in z.namelist() if n.startswith('xl/worksheets/sheet')]),7)
            self.assertIn('xl/styles.xml', z.namelist())
            styles=ET.fromstring(z.read('xl/styles.xml'))
            self.assertTrue(styles.findall(f'.//{{{BASE}}}alignment[@wrapText="1"]'))
            for n in range(1,8):
                sheet=ET.fromstring(z.read(f'xl/worksheets/sheet{n}.xml'))
                self.assertGreater(len(sheet.findall(f'.//{{{BASE}}}row')),3)
                widths=sheet.findall(f'.//{{{BASE}}}cols/{{{BASE}}}col')
                self.assertGreater(len(widths), 0)
                self.assertTrue(any(float(col.attrib['width']) >= 28 for col in widths))
                self.assertTrue(all(row.get('customHeight') == '1'
                    for row in sheet.findall(f'.//{{{BASE}}}row')))
    def test_no_overwrite_and_tamper(self):
        out=self.root/'out.xlsx';truth=self.root/'truth.json'
        out.write_bytes(b'protect')
        with self.assertRaisesRegex(ValueError,'overwrite'): generate(self.source,out,truth)
        self.assertEqual(out.read_bytes(),b'protect')
        out.unlink()
        (self.source/'historical_transactions.csv').write_bytes((self.source/'historical_transactions.csv').read_bytes()+b'\n')
        with self.assertRaisesRegex(ValueError,'SHA256'):generate(self.source,out,truth)
if __name__=='__main__':unittest.main()