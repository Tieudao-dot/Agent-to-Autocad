# Kế hoạch: đẩy skill tới từng máy theo mã máy

> Trạng thái: **bản nháp, chờ chủ dự án duyệt**. Chưa code. Nhánh `claude/day-skill-theo-may`.
> Các điểm đánh dấu **[CHỐT]** là chỗ còn chờ chủ dự án chọn. Phương án đề xuất ghi trước.

## 1. Mục tiêu

Chủ plugin gán một bộ skill cho từng máy ngay trên Google Sheet "ClaudeCAD – Thiết bị". Ở lần kiểm tra trạng thái
kế tiếp (mở AutoCAD, hoặc khoảng 6 giờ một lần, hoặc bấm "Kiểm tra lại"), máy được gán tự nhận skill vào thư viện
và hiện toast báo. Không cần gửi file, không phải cài lại, không phải tách thành nhiều phiên bản plugin.

Ngoài phạm vi bản đầu: file tham khảo đi kèm skill (`.csv`, `.txt`…). Bản đầu chỉ đẩy `SKILL.md`. Cũng không
đẩy macro hay mẫu lệnh.

## 2. Luồng dữ liệu

```
Chủ plugin sửa Sheet                         Máy khách (plugin ≥ 0.9.6)
 ├ tab "Skill": tên | phiên bản | nội dung
 └ tab "Thiết bị", cột mới "Skill"
   (danh sách tên skill gán cho máy)
                                              CheckStatusAsync()
                                                GET ?action=status&machine=…&ver=…&sk=<mã băm bộ skill đang có>
Apps Script handleStatus
  ├ status = CCS1 …            (như cũ)
  ├ license = CCL1 …           (như cũ, chỉ máy Active)
  └ máy Active và có tham số sk:
      tính mã băm bộ skill được gán
      ├ trùng sk  → không gửi gì thêm
      └ khác sk   → "skills": dòng CCP1 có ký số (kèm nội dung)
                                              ├ kiểm CCS1 như cũ
                                              ├ có "skills" → kiểm chữ ký CCP1, máy, thời điểm, giới hạn
                                              ├ ghi skill vào thư viện (đánh dấu "từ chủ plugin")
                                              ├ xử lý skill bị gỡ / xung đột tên  [CHỐT]
                                              └ toast "Chủ plugin đã gửi 2 skill mới: …"
```

- Plugin cũ (0.9.5) không gửi `sk`, nên máy chủ không trả `skills`. Kể cả khi có trả, `ParseResponse` cũng bỏ qua
  trường lạ. Bộ cài chỉ gọi `activate`, mà action này không đổi.
- Máy chủ cũ (chưa triển khai bản mới) không trả `skills`, nên plugin mới coi là "không có thay đổi".
- Chỉ máy **Active** nhận skill. Máy Chờ duyệt / Disabled / miễn kiểm tra (`offline`) không nhận. Skill đã nhận vẫn
  nằm trên máy, nhưng palette đã khóa thì cũng không dùng được.
- Tiết kiệm băng thông: nội dung chỉ được gửi khi bộ skill thay đổi. Mã băm `sk` giúp máy chủ biết máy đang có gì
  mà không phải ghi gì vào Sheet.

## 3. Định dạng có ký số

### 3.1 Giữ nguyên

`CCS1|máy|trạng thái|ngày|chữ ký` và `CCL1|…` **không đổi một byte nào**. Máy cũ không bị ảnh hưởng.

### 3.2 Dòng mới `CCP1` (skill **P**ack)

```
CCP1|<mã máy>|<thời điểm ký UTC yyyyMMddHHmmss>|<thân base64url>|<chữ ký base64url>
```

- Chữ ký: RSA-SHA256 PKCS#1 v1.5 trên mọi thứ đứng trước dấu `|` cuối cùng. Dùng **cùng cặp khóa** với
  `CCL1` / `CCS1`, nên không phải đổi khóa công khai trong plugin. Tiền tố `CCP1` khác hẳn, nên một chữ ký pack
  không thể đem dùng thay cho trạng thái hay giấy phép (`VerifyStatus` đòi đúng 5 phần và tiền tố `CCS1`).
- Thân là JSON UTF-8, mã hóa base64url (để không dính ký tự `|`):

```json
{ "v": 1, "skills": [
  { "name": "cty-a-ket-cau", "rev": "3", "sha256": "<base64url>", "md": "---\nname: cty-a-ket-cau\n…" }
] }
```

- `skills: []` nghĩa là máy không còn được gán skill nào. Đây là cách báo "gỡ hết".
- Mã băm `sk` (máy gửi, máy chủ tính lại): SHA-256 base64url của chuỗi `name:rev:sha256\n` (đã sắp theo tên)
  của bộ đang có. Máy chưa có skill nào gửi `sk=-`.

### 3.3 Plugin kiểm gì trước khi ghi

1. Chữ ký hợp lệ với khóa công khai trong `License.cs`.
2. Mã máy trùng máy này.
3. **Chống phát lại:** thời điểm ký phải mới hơn pack đã nhận gần nhất, lưu ở
   `%LocalAppData%\ClaudeCAD\skill-pack.txt`. Một pack cũ bị bắt lại không thể dùng để hạ skill về bản cũ.
4. Giới hạn: tối đa **20 skill**, mỗi `SKILL.md` tối đa **45.000 ký tự** (ô Sheet tối đa 50.000), cả pack tối đa
   **400 KB**. Vượt giới hạn thì từ chối **cả pack** và ghi log. Máy chủ cũng cắt từ phía mình và báo vào Ghi chú.
5. Từng skill: tên đúng quy tắc `^[a-z0-9][a-z0-9-]{0,63}$`, không phải tên skill mẫu, `ParseFrontmatter` không
   lỗi, `name` trong frontmatter trùng tên, `sha256` khớp nội dung.
6. Ghi vào thư mục tạm rồi mới đổi tên sang chỗ thật, để mất điện giữa chừng không để lại skill dở dang.

## 4. Thay đổi từng phần

### 4.1 Google Sheet

| Tab | Thay đổi |
| --- | --- |
| **Thiết bị** | Thêm cột J **"Skill"**: tên skill gán cho máy, cách nhau bằng dấu phẩy, ví dụ `cty-a-ket-cau, cty-a-layer`. Để trống = không gán. |
| **Skill** *(mới, `caiDat()` tự tạo)* | Cột: **Tên** · **Phiên bản** · **Nội dung SKILL.md** · **Ghi chú** (máy chủ ghi lỗi nếu có: quá dài, thiếu description…). |

Chỗ để nội dung skill **[CHỐT]**. Đề xuất để ngay trong tab Skill: không cần quyền Google mới, sửa ngay trên điện
thoại được. Phương án khác là Google Drive (mỗi skill một file `.md`): soạn dài thoải mái hơn, nhưng phải cấp thêm
quyền Drive cho script và phải quản lý thêm thư mục.

### 4.2 Apps Script (`activation/apps-script/Code.gs.template`)

- `caiDat()`: thêm tiêu đề cột J và tạo tab Skill (giữ nguyên dữ liệu đang có).
- `handleStatus(p)`: chỉ khi `status === 'active'` và có `p.sk`:
  đọc cột J → tra tab Skill → `computeDigest` SHA-256 → so với `sk` → khác thì gắn `out.skills = makeSkillPack(...)`.
  Trường `skills` đặt **sau** `status` trong JSON. Skill gán mà không tìm thấy hoặc không hợp lệ thì bỏ qua và ghi
  vào Ghi chú.
- `makeSkillPack(machine, list)` → `signLine('CCP1|' + machine + '|' + thời điểm + '|' + base64url(JSON))`.
- Cache 5 phút cho tab Skill (`CacheService`, chia khóa nếu quá 100 KB), để mỗi lần `status` không phải đọc lại cả
  tab.
- `kiemTra()`: thêm bước tự kiểm pack cho máy thử.
- Không đổi `SECRET` / `PRIVATE_KEY` / URL web app.

### 4.3 Plugin (C#)

| File | Thay đổi |
| --- | --- |
| `Core/License.cs` | `SkillPackLogic.Verify(line, machine, lastIssued)` → kết quả gồm danh sách skill hoặc lý do từ chối. Thêm `PackDigest(...)`. Không phụ thuộc AutoCAD, nên test được trên CI. |
| `Core/SkillStore.cs` | `Entry` / `SkillInfo` thêm `Managed` (`"owner"`), `Rev`, `Sha256`. Nếu không thêm, `SaveRegistry` sẽ làm rơi các trường này ở lần `Scan()` sau. Thêm `ApplyPack(pack)`: thêm / cập nhật / gỡ theo quy tắc **[CHỐT]**, trả danh sách thay đổi. `Import` (nút + Add skill) xử lý trùng tên với skill được đẩy **[CHỐT]**. |
| `Sidecar/Activation.cs` | `CheckStatusAsync`: gửi `sk`, đọc trường `skills` (thêm vào `ParseResponse`), kiểm rồi gọi `ApplyPack`, lưu `skill-pack.txt`. Lỗi pack **không** ảnh hưởng trạng thái kích hoạt. |
| `ChatController.cs` | Sau `CheckDeviceStatus`: có thay đổi thì toast + note "Chủ plugin đã gửi skill: …" và làm mới danh sách Skill. Bật sẵn "Đang dùng" hay không: **[CHỐT]**. |
| Danh sách Skill (UI) | Nhãn "từ chủ plugin" cạnh skill được đẩy. Nút **Sửa** / xóa theo quy tắc **[CHỐT]**. |
| `agent/src/agent/skills.ts` | Không đổi. Trợ lý vẫn đọc thư mục skill và `skills.json`, các trường mới bị bỏ qua. |

## 5. Các điểm cần chốt

1. Nội dung skill để ở tab Sheet hay Google Drive (mục 4.1).
2. Skill bị gỡ khỏi danh sách gán: xóa khỏi thư viện / giữ lại thành skill thường / chỉ tắt.
3. Người dùng có được sửa skill được đẩy không: khóa sửa (bản đẩy luôn thắng) / được sửa nhưng thôi nhận cập nhật.
4. Trùng tên với skill người dùng tự thêm: giữ skill của người dùng và báo / skill đẩy thắng (sao lưu bản người dùng).
5. Có tự bật "Đang dùng" cho skill mới nhận không.

## 6. Test

- **Lõi (`ClaudeCAD.Core.Tests`, chạy Linux + CI):**
  - `CCP1` ký bằng cặp khóa thử đã có trong `LicenseTests` (cần ký sẵn vài dòng mẫu bằng khóa thử, giống
    `TestLicense`): đúng, sai chữ ký, máy khác, tiền tố khác, pack cũ hơn (phát lại), vượt 20 skill / 45.000 ký tự /
    400 KB, sai `sha256`, tên lạ / trùng skill mẫu, frontmatter lỗi.
  - Chữ ký `CCS1` không dùng được làm `CCP1` và ngược lại.
  - `PackDigest` ổn định theo thứ tự.
  - `SkillStore.ApplyPack`: thêm / cập nhật / gỡ / xung đột tên / skill bị sửa tay, theo quy tắc đã chốt.
  - `skills.json` giữ được `managed` qua `Scan()`.
  - `ParseResponse` với JSON có và không có `skills`.
- **Trợ lý (`npm test`):** test `enabledSkills` với `skills.json` có trường mới.
- **Apps Script:** không có khung test tự động. Dùng `kiemTra()` mở rộng (tạo pack cho máy thử, tự kiểm cấu trúc),
  thêm một script Node nhỏ chạy logic tính mã băm và dựng thân pack bằng cùng hàm, để so khớp với C#.
- **CI Windows:** một lượt `build.yml` với `release: "false"` (build 3 target + UI test). Thêm một mục vào
  `CLAUDECHATSELFTEST`: đọc `skill-pack.txt`, kiểm skill được đẩy còn đúng `sha256`.
- **Thử thật (checklist):** gán 1 skill cho máy thử → bấm Kiểm tra lại → thấy toast + skill. Sửa nội dung + tăng
  phiên bản → nhận bản mới. Gỡ khỏi cột J → xử lý đúng quy tắc. Máy thứ hai không gán thì không nhận gì.

## 7. Việc chủ dự án làm tay trên Google (sau khi code xong và thread bảo mật rà xong)

1. Mở dự án Apps Script đang chạy (Sheet → Tiện ích mở rộng → Apps Script).
2. **Thay phần code, giữ nguyên 3 dòng `EMAIL` / `SECRET` / `PRIVATE_KEY` đang có.** Không chạy lại
   `make-apps-script.mjs` mà thiếu `--secret` cũ: nếu thiếu, script sẽ tạo SECRET mới và mọi key đã cấp sẽ hết
   dùng được. Tớ sẽ đưa sẵn đoạn code để dán, không chứa bí mật.
3. Chạy `caiDat` một lần (▶). Hàm này tạo cột J và tab Skill. Sau đó chạy `kiemTra`.
4. **Triển khai → Quản lý bản triển khai → sửa (✎) bản đang có → Phiên bản: Mới → Triển khai.** Không bấm
   "Bản triển khai mới": làm vậy sẽ ra URL mới, và mọi máy đã cài sẽ mất kết nối.
5. Dán skill vào tab Skill, ghi tên skill vào cột J của máy khách. Xong.

Thứ tự an toàn: triển khai Apps Script mới **trước** (máy cũ không bị ảnh hưởng), phát hành plugin 0.9.6 **sau**.

## 8. Ước tính công sức

| Phần | Công |
| --- | --- |
| Apps Script + script so khớp Node | ~0,5 ngày |
| Lõi C# (`SkillPackLogic`, `ApplyPack`, registry) + test lõi | ~1 ngày |
| Activation + ChatController + UI danh sách Skill + self-test | ~0,5–1 ngày |
| Tài liệu (`skills/README.md`, `activation/README.md`, `bo-cai/README.md`, `docs/`), checklist | ~0,25 ngày |
| Rà bảo mật (thread bảo mật), 1–2 lượt CI Windows | chờ |

Tổng khoảng **2,5–3 ngày công**, tính theo nhịp làm của thread. Tốn nhất là các lượt CI Windows (mỗi lượt khoảng
15–20 phút) và vòng rà bảo mật.

## 9. Rủi ro đã tính

- **Kênh đẩy bị chiếm:** mọi pack đều phải có ký số và chống phát lại, nên Sheet hay URL bị lộ cũng không giả được
  skill. Ai sửa được Sheet thì đẩy được skill. Đây là quyền của chủ Sheet, giống quyền Active / Disabled hiện có.
- **Nội dung skill là lời dặn cho Claude:** chỉ chủ plugin soạn. Plugin không chạy gì từ skill, chỉ chép
  `SKILL.md`.
- **Quota Apps Script:** mỗi lần `status` chỉ đọc thêm cache. Nội dung chỉ được gửi khi có thay đổi.
