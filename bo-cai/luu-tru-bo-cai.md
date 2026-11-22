# Sổ lưu trữ bộ cài ClaudeCAD

Ghi mọi bộ cài đã build: nguồn, link, hạn tải, trạng thái và cách cài đúng cho từng bản. **Mới nhất ở trên cùng.**

## Quy tắc ghi link tải (bắt buộc với mọi bản)

- **Chỉ ghi / gửi link tải trực tiếp:** bấm vào là trình duyệt tải ngay tệp `ClaudeCAD-Setup.exe`, không qua trang
  GitHub nào. Dạng: `https://github.com/Tieudao-dot/Agent-to-Autocad/releases/download/v<phiên bản>/ClaudeCAD-Setup.exe`, hoặc link luôn trỏ bản mới nhất
  `https://github.com/Tieudao-dot/Agent-to-Autocad/releases/latest/download/ClaudeCAD-Setup.exe`.
- **Không** ghi / gửi link trang GitHub (trang phát hành, trang run CI, trang artifact, trang repo).
- Bộ cài thử trên CI không có link tải trực tiếp (phải đăng nhập GitHub): chỉ ghi **nguồn** (run, artifact id) để tra
  cứu nội bộ, không đưa cho client. Bản giao client phải là bản phát hành có link tải trực tiếp.
- Link không được chứa key, mã máy, token, email, số điện thoại hay địa chỉ máy chủ kích hoạt.
- Kèm kích thước + SHA-256 để người nhận kiểm tra tệp: `Get-FileHash .\ClaudeCAD-Setup.exe` trong PowerShell.

## Ghi chú chung

- **Bộ cài thử (trên CI):** chỉ để thử nội bộ, cần đăng nhập GitHub và **tự hết hạn** (ngày hết hạn ghi ở từng
  mục). Bản nào đã lỡ giao client thì chủ dự án nên giữ một bản trên máy.
- **Bản phát hành chính thức:** tải trực tiếp, không cần đăng nhập, không hết hạn. Link luôn trỏ bản mới nhất:
  https://github.com/Tieudao-dot/Agent-to-Autocad/releases/latest/download/ClaudeCAD-Setup.exe
- Liên hệ kích hoạt / cấp key: **liên hệ chủ plugin** (sổ này không ghi key, số điện thoại, email hay địa chỉ máy chủ
  kích hoạt).

**Trạng thái:** ✅ dùng được · 🧪 thử nội bộ (chưa phát hành) · 🔁 thay thế (đã có bản mới hơn thay) · ⏸ tạm hoãn · ⛔ không dùng.

## Bảng tóm tắt

| Bộ cài | Phiên bản | Trạng thái | Nguồn | Hạn tải |
| --- | --- | --- | --- | --- |
| Bản thử 22/11/2026 | 0.9.5 trong bộ cài (nội dung mới hơn v0.9.5) | 🧪 thử nội bộ, chưa phát hành, không giao client | `claude/release-0.9.5` @ `5838cf1`, run 45007148184 | theo CI (~90 ngày) |
| **v0.9.5 — 18/11/2026** | 0.9.5 | ✅ **dùng được — bản đang dùng** | `main` @ `fc06894`, tag `v0.9.5`, run 44319064734 | không hết hạn |
| 11/11/2026 | 0.9.4 trong csproj, nội dung tích hợp 0.9.5 | 🔁 thay thế bởi v0.9.5 (cùng code), đã giao client | `claude/release-0.9.5` @ `7f1a278`, run 43000757496 | 09/02/2027 |
| 15/11/2026 | như trên + BiMat | ⏸ tạm hoãn, không giao | `76f1cce`, run 43665981360 | (chưa ghi) |
| Trước 11/11/2026 | thử 0.9.4 / 0.9.5 | ⛔ không dùng | run 42993287282 trở về trước | — |
| v0.9.3 — 09/11/2026 | 0.9.3 | 🔁 thay thế bởi v0.9.5 (vẫn tải được) | tag `v0.9.3` @ `47baddb` | không hết hạn |

---

## Bản thử 22/11/2026 — skill mẫu mới (chưa phát hành)

| Mục | Giá trị |
| --- | --- |
| Trạng thái | 🧪 **Thử nội bộ** — chưa phát hành, không giao client; chờ chủ dự án quyết nâng phiên bản / phát hành |
| Ngày build | 22/11/2026 |
| Ngày thử / giao | — / — |
| Phiên bản | Bộ cài ghi **0.9.5** (chưa nâng số) nhưng nội dung **khác** bản phát hành v0.9.5 |
| Nguồn | `claude/release-0.9.5` @ `5838cf1`. CI `release: false` run **45007148184**, artifact `ClaudeCAD-0.9.5` id **14748308368** (339 503 313 byte cả gói) |
| Link tải | **Không có link tải trực tiếp** (bản thử trên CI, cần đăng nhập GitHub). Bản cho người dùng vẫn là v0.9.5 |
| Kiểm tra tệp | Chưa có kích thước / SHA-256 của `ClaudeCAD-Setup.exe` trong gói |
| Hạn tải | Theo CI (thường 90 ngày kể từ 22/11/2026) |
| Thay bản nào | Không thay bản nào |

**Khác v0.9.5:**

- 3 skill mẫu viết lại (`ve-kien-truc`, `ve-mep`, `ve-ha-tang`): danh sách chuẩn bị trước khi vẽ, dựng hình bằng
  offset / trim, bắt buộc tự kiểm sau khi vẽ.
- Skill mẫu tự cập nhật trên máy đã cài nếu người dùng chưa sửa (bản gốc giữ ở `Contents\skills-mau`); skill người
  dùng đã sửa thì giữ nguyên.
- Mô tả trang phát hành do CI sinh mở đầu bằng link tải trực tiếp.

**Cài đặt:** giống [bản 11/11](#cài-đặt-bản-1111); tệp `ClaudeCAD-Setup.exe` lấy bằng cách giải nén artifact (đăng
nhập GitHub). Máy đã kích hoạt thì cài đè không hỏi lại key.

---

## v0.9.5 — 18/11/2026 (phát hành chính thức)

| Mục | Giá trị |
| --- | --- |
| Trạng thái | ✅ **Dùng được — bản đang dùng** |
| Ngày phát hành | 18/11/2026 |
| Ngày thử | Cùng code với bộ cài 11/11 đã thử ổn ngày 17/11 (chỉ khác số phiên bản) |
| Ngày giao | — (client đang dùng bản 11/11; link "bản mới nhất" giờ tải bản này) |
| Phiên bản | **0.9.5** |
| Nguồn | `main` @ `fc06894` (PR #5 từ `claude/release-0.9.5` @ `02ca9d6`), tag `v0.9.5`. CI main run **44319064734** |
| Link tải | https://github.com/Tieudao-dot/Agent-to-Autocad/releases/download/v0.9.5/ClaudeCAD-Setup.exe (tải trực tiếp, không cần đăng nhập) |
| Kiểm tra tệp | 141 361 207 byte · SHA-256 `88169253AB4DC8687DB6140F70617256C53993D3D008AEBCF29678B36AFF7840` |
| Tệp khác (cho IT) | `ClaudeCAD-0.9.5.zip`: https://github.com/Tieudao-dot/Agent-to-Autocad/releases/download/v0.9.5/ClaudeCAD-0.9.5.zip · `install.ps1`: https://github.com/Tieudao-dot/Agent-to-Autocad/releases/download/v0.9.5/install.ps1 |
| Hạn tải | Không hết hạn |
| AutoCAD | 2022–2027 (2027 chưa thử trên máy thật) |
| Bản thử tương đương | `ClaudeCAD-0.9.5` của run 44317706091, artifact 14434370313 — giống hệt bản phát hành về code (cần đăng nhập GitHub, hết hạn theo CI) |

**Gồm:** kích hoạt theo máy + quản lý thiết bị qua Sheet "Thiết bị" (khóa kích hoạt mới đổi 11/11), giao diện kiểu
AutoCAD, AutoCAD 2022–2027, sửa lỗi parse `install.ps1` của 0.9.3. **Không có** BiMat / "Đã gỡ lúc" (tạm hoãn, xem
bản 15/11).

**Cài đặt:** giống hệt [bản 11/11](#cài-đặt-bản-1111) (cài thường, cài ngầm cho IT, gỡ cài đặt). Khác:

- Bấm link tải là có ngay `ClaudeCAD-Setup.exe`, không cần đăng nhập GitHub, không phải giải nén. Kiểm tra tệp:
  `Get-FileHash .\ClaudeCAD-Setup.exe` so với SHA-256 ở bảng trên.
- Cài bằng PowerShell (`ClaudeCAD-0.9.5.zip` + `install.ps1`) chạy được ở bản này.
- **Nâng cấp từ 0.9.3 phải kích hoạt máy** (0.9.3 không có khóa key): chạy đè bộ cài → trang Kích hoạt → chờ duyệt
  hoặc nhập key như bản 11/11.
- Máy đã cài bộ cài 11/11 và đã kích hoạt: chạy đè không hỏi lại key.

---

## Bộ cài 11/11/2026 — tích hợp 0.9.5 (tên tệp 0.9.4)

| Mục | Giá trị |
| --- | --- |
| Trạng thái | 🔁 **Thay thế** bởi v0.9.5 (18/11, cùng code, chỉ khác số phiên bản). Máy client đã cài vẫn dùng bình thường |
| Ngày build | 11/11/2026 |
| Ngày thử | 17/11/2026 — thử ổn |
| Ngày giao | 17/11/2026 — đã giao client |
| Phiên bản | Tệp `ClaudeCAD-0.9.4` (~340 MB). Trong csproj / bộ cài là **0.9.4**; nội dung là **bản tích hợp 0.9.5**, chưa phát hành chính thức |
| Nhánh / commit | `claude/release-0.9.5` @ `7f1a278` (*Merge claude/device-management again: activation key pair rotated before release*) |
| Run CI / artifact | Run **43000757496**, artifact id **13847355266** |
| Link tải | Không có link tải trực tiếp (bản thử trên CI, cần đăng nhập GitHub). Cần cài mới thì dùng link của v0.9.5 (cùng code) |
| Hạn tải | **09/02/2027** |
| AutoCAD | 2022–2027 (một bộ cài chung). 2026 dùng bản .NET 8 chung với 2025; 2027 có bản .NET 10 riêng — **2027 chưa thử trên máy thật** |

**Gồm:**

- Khóa key theo máy + quản lý thiết bị qua Google Sheet **"ClaudeCAD – Thiết bị"** (Chờ duyệt / `Active` /
  `Disabled`). Mang **khóa kích hoạt mới đổi ngày 11/11** — các bộ cài build trước đó không dùng chung được.
- Giao diện kiểu AutoCAD (màu / font theo theme palette của AutoCAD, thanh công cụ icon, thu gọn khung chat, icon
  ClaudeCAD trên thanh trạng thái, tiến độ từng bước).
- AutoCAD 2026 / 2027, tự kiểm tra mở rộng (`CLAUDECHATSELFTEST`), sửa lỗi parse của `install.ps1` bản 0.9.3.
- Chi tiết: mục 0.9.5 trong `bo-cai/README.md` tại commit `7f1a278`.

### Cài đặt (bản 11/11)

Theo `bo-cai/README.md` và `docs/checklist-thu-that.md` (phần A) tại commit `7f1a278`.

**Chuẩn bị:** máy Windows 64-bit có AutoCAD bản đầy đủ (không phải LT) 2022–2027. Lấy tệp
`ClaudeCAD-Setup.exe` vào **ổ máy** (không chạy từ ổ mạng / USB): với v0.9.5 bấm link tải trực tiếp; với bản thử
11/11 thì giải nén tệp artifact đã lưu.

1. **Đóng AutoCAD.**
2. Chạy `ClaudeCAD-Setup.exe`. Nếu Windows hiện "Windows protected your PC" → **More info → Run anyway** (bộ cài chưa
   ký số). Không cần quyền admin, không cần cài Node.js hay Claude Code. Nếu báo "The source file is corrupted":
   tải lại vào ổ máy, hoặc tạm tắt phần mềm diệt virus khi cài.
3. **Trang Kích hoạt:** bộ cài hiện **mã máy** `CC-…` và dòng liên hệ chủ plugin; yêu cầu tự gửi cho chủ plugin
   (máy hiện thành dòng **Chờ duyệt** trong Sheet "Thiết bị"). Chọn một trong hai:
   - **(a) Chờ duyệt:** để trống ô key, bấm **Next** → **Yes**, cài tiếp không cần key.
   - **(b) Có key:** dán key do chủ plugin gửi vào ô **Key kích hoạt** rồi bấm **Next**. Key chỉ dùng được trên đúng
     máy có mã đó.
   - Máy đã kích hoạt rồi (cài đè, cập nhật) thì không hỏi lại.
4. **Trang cuối:** giữ ô **Đăng nhập Claude ngay** → **Finish** → trình duyệt mở trang cấp quyền, bấm **đồng ý**
   bằng tài khoản Claude Pro / Max (cửa sổ đen hiện lên và tự đóng). Máy đã đăng nhập sẵn thì ô này không hiện.
   Bỏ qua bước này thì lần đầu mở AutoCAD plugin tự mở trang đăng nhập.
5. **Mở AutoCAD.** Không có cảnh báo "load this plugin?"; khung chat tự mở.
6. **Nếu thấy thẻ 🔒 Kích hoạt** (đã chọn cách (a)): chủ plugin đổi ô **Trạng thái** của máy đó trong Sheet
   "Thiết bị" thành `Active` → bấm **Kiểm tra lại** trong khung chat → "Chủ plugin đã duyệt máy này", thẻ **Bắt đầu**
   hiện ra. (Hoặc dán key vào ô key của thẻ 🔒.)
7. Gõ `CC` (hoặc `CLAUDECHAT`, **Ctrl+Shift+Space**) để mở khung chat. Nên gõ `CLAUDECHATSELFTEST` một lần:
   Notepad mở báo cáo, dòng cuối "Tất cả đều đạt."

Ghi chú vận hành:

- Plugin kiểm tra giấy phép mỗi lần mở AutoCAD và ~6 giờ/lần. Chủ plugin đổi `Disabled` → máy bị khóa ở lần kiểm
  tra kế tiếp (mất mạng dùng tối đa 7 ngày); đổi lại `Active` là dùng tiếp, không cần key mới.
- Cài lại Windows / thay mainboard thì mã máy đổi → máy xin duyệt lại như máy mới.
- Cập nhật: chạy đè bộ cài mới; skill đã thêm / sửa, macro, mẫu lệnh, lịch sử chat được giữ nguyên.

### Cài ngầm cho IT (bản 11/11)

- Cài ngầm, không hỏi gì:
  `ClaudeCAD-Setup.exe /VERYSILENT /KEY=XXXX-XXXX-XXXX-XXXX` — key của **đúng máy đó** (lấy mã máy rồi liên hệ chủ
  plugin). Cài ngầm **không có** trang Kích hoạt: thiếu `/KEY=` thì plugin hỏi key / hiện thẻ 🔒 khi mở AutoCAD
  (chủ plugin duyệt máy trong Sheet là dùng được).
- Mỗi máy vẫn phải **Đăng nhập Claude** một lần (lần đầu mở AutoCAD plugin tự mở trang đăng nhập).
- Cài vào thư mục người dùng: `%AppData%\Autodesk\ApplicationPlugins\ClaudeCAD.bundle` (không cần admin, cài cho
  từng tài khoản Windows).
- Cách khác bằng PowerShell: giải nén `ClaudeCAD-<version>.zip`, đặt `install.ps1` cạnh thư mục `ClaudeCAD.bundle`,
  chạy `powershell -ExecutionPolicy Bypass -File install.ps1` (cách này cũng bị hỏi key trong AutoCAD).

### Gỡ cài đặt (bản 11/11)

- Windows **Settings → Apps → ClaudeCAD for AutoCAD → Uninstall**.
- Gỡ ngầm (IT): `"%AppData%\Autodesk\ApplicationPlugins\ClaudeCAD.bundle\unins000.exe" /VERYSILENT`
  (trình gỡ chuẩn của Inno Setup).
- Bản cài bằng PowerShell: `powershell -ExecutionPolicy Bypass -File install.ps1 -Uninstall`.
- Dữ liệu người dùng (lịch sử chat, macro, mẫu lệnh, trạng thái kích hoạt) nằm trong `%LocalAppData%\ClaudeCAD` và
  `%AppData%\ClaudeCAD`, không tự xóa — xóa tay nếu không cần nữa.
- Báo cho chủ plugin để đổi máy đó thành `Disabled` trong Sheet "Thiết bị" nếu ngưng cho dùng hẳn.

---

## Bộ cài 15/11/2026 — tạm hoãn

| Mục | Giá trị |
| --- | --- |
| Trạng thái | ⏸ **Tạm hoãn — không giao client** |
| Ngày build | 15/11/2026 |
| Ngày thử / giao | — / không giao |
| Phiên bản | Như bản 11/11 (0.9.4 trong csproj, tích hợp 0.9.5) + phần BiMat |
| Commit | `76f1cce` |
| Run CI / artifact | Run **43665981360**, artifact id **14142895023** |
| Link tải | Không có — bản thử trên CI, tạm hoãn, không giao |
| Hạn tải | chưa ghi (artifact CI thường giữ 90 ngày kể từ ngày build) |

**Khác bản 11/11:** thêm BiMat và ghi cột "Đã gỡ lúc" vào Sheet "Thiết bị" khi gỡ cài đặt. Phần này được giữ ở nhánh
`claude/device-management` (`53bb1bf`) để làm sau và đang được gỡ khỏi nhánh phát hành — **không dùng bản này**.
Các bước cài giống bản 11/11.

---

## Các bộ cài thử trước 11/11/2026

| Mục | Giá trị |
| --- | --- |
| Trạng thái | ⛔ **Không dùng** |
| Nguồn | Các run CI thử của nhánh gộp, từ run **42993287282** trở về trước |
| Lý do | Mang **khóa kích hoạt cũ** (khóa đổi ngày 11/11): key / duyệt máy từ máy chủ hiện tại không khớp |

Máy nào đã lỡ cài một bản này: cài đè bằng bộ cài 11/11 rồi kích hoạt lại.

---

## v0.9.3 — 09/11/2026 (phát hành chính thức)

| Mục | Giá trị |
| --- | --- |
| Trạng thái | 🔁 **Thay thế** bởi v0.9.5 (18/11/2026); vẫn tải được trên Releases |
| Ngày build / phát hành | 09/11/2026 |
| Phiên bản | 0.9.3 |
| Nguồn | `main`, tag `v0.9.3` @ `47baddb` |
| Link tải | https://github.com/Tieudao-dot/Agent-to-Autocad/releases/download/v0.9.3/ClaudeCAD-Setup.exe (tải trực tiếp; chỉ dùng khi cần bản cũ) |
| Kiểm tra tệp | 141 265 494 byte · SHA-256 `4B6B9AF1EB83C0251A05989D1D1007ED0BC1D13D6BF974A8F4A88179294D529B` |
| Hạn tải | Không hết hạn |
| AutoCAD | 2022–2025 |

**Khác biệt khi cài so với bản 11/11:**

- **Không có trang Kích hoạt**, không khóa key, không có Sheet "Thiết bị" / thẻ 🔒 — cài xong đăng nhập Claude là dùng.
- Cài 3 bước: chạy `ClaudeCAD-Setup.exe` → trang cuối giữ ô **Đăng nhập Claude ngay** → **Finish** → mở AutoCAD.
- Cài ngầm: `ClaudeCAD-Setup.exe /VERYSILENT` (không có `/KEY=`).
- Chỉ nhận AutoCAD 2022–2025.
- ⚠️ `install.ps1` của bản này có **lỗi parse** (cách cài / gỡ bằng PowerShell không chạy); đã sửa ở bản tích hợp.
  Dùng `ClaudeCAD-Setup.exe` và gỡ qua Windows Settings.
