# Kế hoạch: đẩy skill tới từng máy theo mã máy

> Trạng thái: plan được duyệt 22/11/2026. **Đã code xong** trên nhánh `claude/day-skill-theo-may`; còn chờ CI Windows,
> rà bảo mật, rồi chủ dự án triển khai Apps Script. Hướng dẫn dùng: `activation/README.md` (mục "Đẩy skill tới từng
> máy") và `skills/README.md`.
>
> Khác với bản plan ban đầu: máy chủ **không** dùng CacheService cho trang Skill. Mỗi lần `status`, máy chủ chỉ đọc
> trang Skill khi máy có dòng gán, mà mỗi máy chỉ gọi khoảng 6 giờ một lần, nên cache không đáng thêm độ phức tạp.

## 0. Quyết định đã chốt (22/11/2026)

| # | Điểm | Chốt |
| --- | --- | --- |
| 1 | Nơi để nội dung skill | Tab **"Skill"** trong Google Sheet |
| 2 | Gán / gỡ | Tab **"Gán skill"**: Mã máy · Tên skill · **Hành động** (Thêm / Ẩn / Xóa). Dòng luôn giữ trên Sheet; hành động chỉ tác động thư viện skill trên máy khách. Máy không có dòng nào cho một skill thì giữ nguyên như lần nhận cuối |
| 3 | Khách sửa skill được đẩy | Được. Sửa xong thì skill đó thành **của khách**, không nhận cập nhật nữa. Danh sách skill có **nhãn**: **Admin** (chủ plugin đẩy), **Default** (skill mẫu kèm plugin), **You** (khách tự thêm hoặc đã sửa) |
| 4 | Trùng tên Admin ↔ You | **Lưu cả hai**, nhãn để phân biệt (bản Admin nằm ở thư mục `<tên>-admin`, xem 4.3) |
| 5 | Tự bật "Đang dùng" | **Không**. Skill chỉ vào Thư viện, kèm toast báo |

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
 └ tab "Gán skill": mã máy | tên skill | Thêm/Ẩn/Xóa
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
                                              ├ Thêm → ghi vào thư viện, nhãn Admin (không tự bật)
                                              ├ Ẩn   → giữ file, ẩn khỏi thư viện + chat
                                              ├ Xóa  → xóa khỏi máy
                                              │  (chỉ tác động skill Admin; không đụng You / Default)
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
  { "name": "cty-a-ket-cau", "act": "add",    "rev": "3", "sha256": "<base64url>", "md": "---\nname: cty-a-ket-cau\n…" },
  { "name": "cty-a-layer",   "act": "hide",   "rev": "1", "sha256": "<base64url>", "md": "…" },
  { "name": "cty-a-cu",      "act": "delete" }
] }
```

- `add` / `hide` có nội dung, để khi Ẩn rồi Thêm lại thì không phải tải lại. `delete` không có nội dung.
- Skill không có trong pack: máy giữ nguyên như lần nhận cuối (chốt #2).
- Mã băm `sk` (máy gửi, máy chủ tính lại): SHA-256 base64url của chuỗi `name:act:rev:sha256\n` (sắp theo tên),
  lấy từ pack đã nhận gần nhất. Máy chưa nhận pack nào gửi `sk=-`. Pack giống hệt thì máy chủ không gửi lại.

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
| **Thiết bị** | Không đổi. |
| **Skill** *(mới, `caiDat()` tự tạo)* | Cột: **Tên** · **Phiên bản** · **Nội dung SKILL.md** · **Ghi chú** (máy chủ ghi lỗi nếu có: quá dài, thiếu description, tên sai quy tắc…). Sửa nội dung thì tăng Phiên bản. |
| **Gán skill** *(mới, `caiDat()` tự tạo)* | Cột: **Mã máy** · **Tên skill** · **Hành động** (ô chọn Thêm / Ẩn / Xóa) · **Ghi chú**. Mỗi dòng là một skill cho một máy. Một máy có nhiều dòng. |

Không cần quyền Google mới, vì script đã có quyền trên Sheet này. Sửa được cả trên điện thoại.

### 4.2 Apps Script (`activation/apps-script/Code.gs.template`)

- `caiDat()`: tạo tab Skill và Gán skill, có ô chọn Hành động (giữ nguyên dữ liệu đang có).
- `handleStatus(p)`: chỉ khi `status === 'active'` và có `p.sk`:
  lọc các dòng Gán skill của máy → tra tab Skill → `computeDigest` SHA-256 → so với `sk` → khác thì gắn
  `out.skills = makeSkillPack(...)`. Trường `skills` đặt **sau** `status` trong JSON. Skill gán mà không tìm thấy
  hoặc không hợp lệ thì bỏ qua và ghi vào Ghi chú của dòng gán. Một máy có hai dòng cùng một skill thì dòng dưới thắng.
- `makeSkillPack(machine, list)` → `signLine('CCP1|' + machine + '|' + thời điểm + '|' + base64url(JSON))`.
- `kiemTra()`: thêm bước tự kiểm pack cho máy thử.
- Không đổi `SECRET` / `PRIVATE_KEY` / URL web app.

### 4.3 Plugin (C#)

| File | Thay đổi |
| --- | --- |
| `Core/License.cs` | `SkillPackLogic.Verify(line, machine, lastIssued)` → kết quả gồm danh sách skill hoặc lý do từ chối. Thêm `PackDigest(...)`. Không phụ thuộc AutoCAD, nên test được trên CI. |
| `Core/SkillStore.cs` | `Entry` / `SkillInfo` thêm `Origin` (`admin` / `default` / `you`; skill mẫu là `default`, còn lại mặc định `you`), `AdminName` (tên gốc trên Sheet), `Rev`, `Sha256`, `Hidden`. Nếu không thêm, `SaveRegistry` sẽ làm rơi các trường này ở lần `Scan()` sau. Thêm `ApplyPack(pack)` (xem quy tắc bên dưới), trả danh sách thay đổi để báo. |
| `Sidecar/Activation.cs` | `CheckStatusAsync`: gửi `sk`, đọc trường `skills` (thêm vào `ParseResponse`), kiểm rồi gọi `ApplyPack`, lưu `skill-pack.txt`. Lỗi pack **không** ảnh hưởng trạng thái kích hoạt. |
| `ChatController.cs` | Sau `CheckDeviceStatus`: có thay đổi thì toast + note ("Chủ plugin đã gửi 2 skill mới: …", "… đã ẩn / gỡ skill …") và làm mới danh sách Skill. **Không** tự bật "Đang dùng" (chốt #5). Skill bị Ẩn / Xóa thì tự rời khỏi "Đang dùng" của các chat. |
| Danh sách Skill (UI) | Nhãn nhỏ cạnh tên: **Admin** / **Default** / **You**. Skill Ẩn không hiện. Nút Sửa giữ nguyên cho mọi skill (chốt #3). |
| `Core/SkillStore.cs` → `SkillSelection` | `ForChat` bỏ skill `Hidden`. |
| `agent/src/agent/skills.ts` | `disabledSkills` coi `hidden: true` như tắt, và `prepareSkillsPlugin` (kể cả có `only`) không chép skill bị ẩn. Thêm test. |

**Quy tắc `ApplyPack`** (chỉ tác động skill có `Origin = admin`; không bao giờ đụng You / Default):

- **Thêm**:
  - Chưa có skill tên đó → ghi `skills\<tên>\`, nhãn Admin, không bật vào chat nào.
  - Đã có bản Admin → cập nhật nội dung nếu `rev` / `sha256` đổi, bỏ Ẩn nếu đang ẩn.
  - Tên đó đang là skill **You** hoặc **Default** → ghi bản Admin vào `skills\<tên>-admin\`, sửa `name:` trong
    frontmatter thành `<tên>-admin`, vì Claude cần tên skill không trùng (chốt #4). Danh sách vẫn hiện tên gốc kèm
    nhãn Admin. Nếu tên `<tên>-admin` cũng bị skill You chiếm thì bỏ qua và ghi log.
- **Ẩn**: giữ file, đặt `Hidden`. Skill không hiện trong thư viện và không được nạp.
- **Xóa**: xóa thư mục bản Admin.
- **Khách sửa bản Admin** (lúc `Scan()` thấy `sha256` của file khác giá trị đã lưu) → chuyển thành **You**, tên giữ
  nguyên, thôi nhận Thêm / Ẩn / Xóa cho bản đó (chốt #3). Hệ quả: ở lần nhận pack kế tiếp có skill đó, bản Admin mới
  sẽ vào `<tên>-admin` theo quy tắc trùng tên ở trên, nên khách có cả hai bản.
- Khách xóa tay một bản Admin → sẽ được ghi lại ở lần đổi pack kế tiếp. Mã băm `sk` lấy theo pack đã nhận, không theo
  file trên máy, nên không tải lại mỗi 6 giờ.

## 5. Các điểm đã chốt

Xem bảng ở mục 0.

## 6. Test

- **Lõi (`ClaudeCAD.Core.Tests`, chạy Linux + CI):**
  - `CCP1` ký bằng cặp khóa thử đã có trong `LicenseTests` (cần ký sẵn vài dòng mẫu bằng khóa thử, giống
    `TestLicense`): đúng, sai chữ ký, máy khác, tiền tố khác, pack cũ hơn (phát lại), vượt 20 skill / 45.000 ký tự /
    400 KB, sai `sha256`, tên lạ / trùng skill mẫu, frontmatter lỗi.
  - Chữ ký `CCS1` không dùng được làm `CCP1` và ngược lại.
  - `PackDigest` ổn định theo thứ tự.
  - `SkillStore.ApplyPack`:
    - Thêm / cập nhật / Ẩn / hiện lại / Xóa.
    - Trùng tên You hoặc Default → `-admin`.
    - Khách sửa → chuyển thành You.
    - Ẩn / Xóa không đụng skill You.
    - Skill vắng khỏi pack thì giữ nguyên.
  - `skills.json` giữ được `origin` / `hidden` / `sha256` qua `Scan()`.
  - Nhãn Default cho 3 skill mẫu.
  - `ParseResponse` với JSON có và không có `skills`.
- **Trợ lý (`npm test`):** skill có `hidden: true` không được nạp, kể cả khi chat có chọn.
- **Apps Script:** không có khung test tự động. Dùng `kiemTra()` mở rộng (tạo pack cho máy thử, tự kiểm cấu trúc),
  thêm một script Node nhỏ chạy logic tính mã băm và dựng thân pack bằng cùng hàm, để so khớp với C#.
- **CI Windows:** một lượt `build.yml` với `release: "false"` (build 3 target + UI test). Thêm một mục vào
  `CLAUDECHATSELFTEST`: đọc `skill-pack.txt`, kiểm skill được đẩy còn đúng `sha256`.
- **Thử thật (checklist):**
  1. Gán Thêm 1 skill cho máy thử → bấm Kiểm tra lại → thấy toast và skill nhãn Admin trong Thư viện.
  2. Sửa nội dung, tăng phiên bản → máy nhận bản mới.
  3. Chọn Ẩn → skill biến mất. Chọn Thêm lại → skill hiện lại.
  4. Chọn Xóa → skill bị xóa khỏi máy.
  5. Khách sửa file → nhãn chuyển thành You.
  6. Máy thứ hai không được gán thì không nhận gì.

## 7. Việc chủ dự án làm tay trên Google (sau khi code xong và thread bảo mật rà xong)

1. Mở dự án Apps Script đang chạy (Sheet → Tiện ích mở rộng → Apps Script).
2. **Thay phần code, giữ nguyên 3 dòng `EMAIL` / `SECRET` / `PRIVATE_KEY` đang có.** Code lấy từ
   `activation/apps-script/Code.gs.template`, bỏ 3 dòng `{{…}}`. Không chạy lại `make-apps-script.mjs` mà thiếu
   `--secret` cũ: nếu thiếu, script sẽ tạo SECRET mới và mọi key đã cấp sẽ hết dùng được.
3. Chạy `kiemTra` một lần (▶). Hàm này gọi `caiDat`, nên tự tạo trang Skill và Gán skill, rồi tự kiểm.
4. **Triển khai → Quản lý bản triển khai → sửa (✎) bản đang có → Phiên bản: Mới → Triển khai.** Không bấm
   "Bản triển khai mới": làm vậy sẽ ra URL mới, và mọi máy đã cài sẽ mất kết nối.
5. Dán skill vào tab Skill. Thêm dòng ở tab Gán skill: mã máy khách, tên skill, chọn Thêm. Xong.

Thứ tự an toàn: triển khai Apps Script mới **trước** (máy cũ không bị ảnh hưởng), phát hành plugin 0.9.6 **sau**.

## 8. Ước tính công sức

| Phần | Công |
| --- | --- |
| Apps Script + script so khớp Node | ~0,5 ngày |
| Lõi C# (`SkillPackLogic`, `ApplyPack` Thêm/Ẩn/Xóa, nhãn, `-admin`, registry) + test lõi | ~1–1,5 ngày |
| Activation + ChatController + UI nhãn Admin/Default/You + self-test | ~0,5–1 ngày |
| Trợ lý: bỏ qua skill ẩn + test | ~0,1 ngày |
| Tài liệu (`skills/README.md`, `activation/README.md`, `bo-cai/README.md`, `docs/`), checklist | ~0,25 ngày |
| Rà bảo mật (thread bảo mật), 1–2 lượt CI Windows | chờ |

Tổng khoảng **3–3,5 ngày công**, tính theo nhịp làm của thread. Tốn nhất là các lượt CI Windows (mỗi lượt khoảng
15–20 phút) và vòng rà bảo mật.

## 9. Rủi ro đã tính

- **Kênh đẩy bị chiếm:** mọi pack đều phải có ký số và chống phát lại, nên Sheet hay URL bị lộ cũng không giả được
  skill. Ai sửa được Sheet thì đẩy được skill. Đây là quyền của chủ Sheet, giống quyền Active / Disabled hiện có.
- **Nội dung skill là lời dặn cho Claude:** chỉ chủ plugin soạn. Plugin không chạy gì từ skill, chỉ chép
  `SKILL.md`.
- **Quota Apps Script:** mỗi lần `status` chỉ đọc thêm cache. Nội dung chỉ được gửi khi có thay đổi.
