"""HTTP integration checks. Application calculations run exclusively in C#."""
import json
import os
from pathlib import Path
from urllib.request import Request, urlopen
from urllib.error import HTTPError
from uuid import uuid4

ROOT = Path(__file__).resolve().parents[1]
BASE = os.environ.get('DATAGUARD_API', 'http://127.0.0.1:5080')

def request(path, payload=None):
    data = json.dumps(payload).encode() if payload is not None else None
    req = Request(BASE+path, data=data, headers={'Content-Type': 'application/json'})
    with urlopen(req, timeout=30) as response:
        return json.load(response)

def upload(role, name, content):
    boundary = uuid4().hex
    body = (f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{name}"\r\n'
            'Content-Type: text/csv\r\n\r\n').encode() + content + f'\r\n--{boundary}--\r\n'.encode()
    req = Request(BASE+'/api/datasets/'+role, data=body,
                  headers={'Content-Type': 'multipart/form-data; boundary='+boundary})
    with urlopen(req, timeout=30) as response:
        return json.load(response)

def rejected(name, content):
    try:
        upload('current', name, content)
    except HTTPError as e:
        assert e.code == 400, (name, e.code)
    else:
        raise AssertionError('Invalid CSV accepted: '+name)

b = upload('baseline', 'customer_baseline.csv', (ROOT/'sample-data/customer_baseline.csv').read_bytes())
c = upload('current', 'customer_current.csv', (ROOT/'sample-data/customer_current.csv').read_bytes())
assert b['profile']['rowCount'] == 1005 and c['profile']['rowCount'] == 972
assert b['profile']['duplicateRowCount'] == 5 and c['profile']['duplicateRowCount'] == 12
assert request('/api/datasets/'+b['id']+'/profile') == b
r = request('/api/comparisons', {'baselineDatasetId': b['id'], 'currentDatasetId': c['id']})
f = {x['featureName']: x for x in r['features']}
assert f['annual_income']['missingChange'] > 0
assert f['annual_income']['drift']['status'] == 'Significant'
assert f['age']['drift']['status'] == 'Stable'
assert f['customer_id']['drift'] is None
assert f['account_balance']['currentStatistics']['outlierPercentage'] > f['account_balance']['baselineStatistics']['outlierPercentage']
assert any(x['columnName'] == 'customer_segment' and x['changeType'] == 'Added' for x in r['schemaChanges'])
assert request('/api/comparisons/'+r['id']) == r
assert request('/api/comparisons/'+r['id']+'/features') == r['features']
for feature in r['features']:
    if feature['drift']:
        assert abs(sum(x['baselinePercentage'] for x in feature['drift']['bins'])-100) < 1e-8
        assert abs(sum(x['currentPercentage'] for x in feature['drift']['bins'])-100) < 1e-8
s = upload('current', 'schema_change_current.csv', (ROOT/'sample-data/schema_change_current.csv').read_bytes())
schema = request('/api/comparisons', {'baselineDatasetId': b['id'], 'currentDatasetId': s['id']})['schemaChanges']
assert any(x['columnName'] == 'age' and x['changeType'] == 'Removed' for x in schema)
assert any(x['columnName'] == 'annual_income' and x['changeType'] == 'Type changed' for x in schema)
for name, content in [('empty.csv', b''), ('header.csv', b'a,b\n'), ('duplicate.csv', b'a,a\n1,2\n'),
                      ('width.csv', b'a,b\n1,2,3\n'), ('quote.csv', b'a,b\n"unclosed,2\n'),
                      ('headerless.csv', b',b\n1,2\n'), ('wrong.txt', b'a\n1\n')]:
    rejected(name, content)
quoted = upload('current', 'quoted.csv', b'number,label\n1,"a,b"\n2,"a""b"\n3,"multi\nline"\n')
assert quoted['profile']['rowCount'] == 3
assert quoted['profile']['features'][1]['categories']['multi\nline'] == 1
constant = upload('baseline', 'constant.csv', b'value\n5\n5\n5\n')
lower = upload('current', 'lower.csv', b'value\n4\n4\n4\n')
assert request('/api/comparisons', {'baselineDatasetId': constant['id'], 'currentDatasetId': lower['id']})['features'][0]['drift']['status'] == 'Significant'
try:
    request('/api/comparisons', {'baselineDatasetId': c['id'], 'currentDatasetId': b['id']})
except HTTPError as e:
    assert e.code == 400
else:
    raise AssertionError('Invalid roles accepted')
print('PASS: upload/profile, CSV validation, duplicates, missingness, outliers, PSI, schema, persisted retrieval and role validation')
print('Calculated sample results:')
for name in ['age', 'annual_income', 'account_balance', 'monthly_transactions']:
    x = f[name]
    print(f"  {name}: PSI={x['drift']['psi']:.6f}, {x['drift']['status']}, missing change={x['missingChange']:+.4f} pp")
print('Saved comparison:', r['id'])
