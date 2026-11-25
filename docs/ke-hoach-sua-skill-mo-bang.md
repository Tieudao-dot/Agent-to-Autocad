# Kế hoạch: sửa skill — chọn cách mở (ứng dụng .md / Word) + quy tắc lưu mới

> Trạng thái: **đã chốt (25/11), chờ chủ dự án duyệt plan**. Chưa code. Nhánh `claude/skill-docx-gdocs`. Yêu cầu từ
> thread QnA, chuyển 25/11.

## 0. Quyết định đã chốt (25/11/2026)

| # | Điểm | Chốt |
| --- | --- | --- |
| 1 | Google Docs: xem trước | Xem trước trong plugin (không dùng link công khai) — rồi bị thay bởi #2 |
| 2 | Sửa skill trên Google Docs | **Không làm.** Ẩn mọi tính năng sửa skill bằng Google Docs phía khách: không đăng nhập Google trong plugin, không có OAuth client. Mục 3.3 giữ lại chỉ để tham khảo |
| 2b | Link Google Docs trong trang Skill của Sheet (máy chủ 0.9.8) | **Giữ nguyên**, chỉ chủ plugin dùng; máy khách không tự tải link |
| 3 | Skill Admin trên Google Docs của khách | Không còn áp dụng (theo #2) |
| 4 | Phần đầu name / description khi mở bằng Word | **Ẩn khỏi Word, plugin tự giữ**; Tên / Mô tả chỉ đổi trong popup Tạo skill mới |

## 1. Yêu cầu

- Bấm **Sửa** trên một skill → khách **chọn cách mở**:
  - ứng dụng mở được `.md` (app mặc định, hoặc hộp "Mở bằng…" của Windows);
  - **Word**;
  - ~~Google Docs~~ — không làm (chốt #2).
- **Lưu sau khi sửa** (với mọi cách mở):
  - Skill **You** → ghi đè vào chính skill đó.
  - Skill **Default** / **Admin** → hiện popup **"Tạo skill mới từ skill gốc"**, có ô **Tên** và ô **Mô tả** để khách
    sửa. Bản gốc giữ nguyên.
- ~~Google Docs: "mở rồi khi khách sửa mới yêu cầu đăng nhập".~~ Không làm (chốt #2).

## 2. Thay đổi so với hiện tại

Hiện tại (từ v0.9.6, `d76641b`): bấm Sửa trên skill Default / Admin thì plugin **chép ngay** sang `<tên>-sua` rồi mở.

Bản mới:

- Plugin mở một **bản làm việc tạm** (ngoài thư viện).
- Khi khách **lưu thật** (nội dung khác bản gốc), popup hiện ra, gợi ý sẵn:
  - Tên `<tên>-sua`;
  - Mô tả của bản gốc.
- Khách sửa Tên / Mô tả rồi bấm **Tạo skill**:
  - Plugin kiểm Tên: đúng quy tắc (chữ thường không dấu, số, gạch nối) và không trùng.
  - Plugin kiểm Mô tả: 20–1024 ký tự.
  - Skill You mới được tạo, đoạn chat hiện tại chuyển sang skill mới (như hiện nay).
- Bấm **Để sau**: bản làm việc vẫn giữ, lần lưu sau popup hiện lại. Không tạo gì trong thư viện.
- Khách mở rồi đóng mà không sửa gì: không có gì xảy ra. Không còn bản chép thừa phải dọn.
- Khách sửa thẳng file gốc ngoài plugin: vẫn tách như chốt 22/11 (`<tên>-sua`, khôi phục bản gốc), không đổi.

## 3. Ba cách mở

### 3.1 Ứng dụng mở .md

- Menu dưới nút Sửa:
  - **Mở bằng ứng dụng mặc định**;
  - **Chọn ứng dụng khác…** (hộp "Mở bằng…" của Windows, `OpenAs_RunDLL`).
- Plugin nhớ lựa chọn gần nhất trong `settings.json`, nên bấm Sửa lần sau mở luôn. Bấm ▾ để chọn lại.
- Skill You: mở thẳng `SKILL.md` như hiện nay. Default / Admin: mở bản làm việc tạm
  (`%LocalAppData%\ClaudeCAD\skill-edit\<tên>\SKILL.md`). Watcher thấy file được lưu thì hiện popup theo mục 2.

### 3.2 Word

- Plugin chuyển `SKILL.md` → `.docx` (bộ ghi `MarkdownToDocx` mới, thuần .NET, chiều ngược của `DocxToMarkdown`):
  tiêu đề, danh sách, bảng, đậm / nghiêng.
- Mở file bằng Word, file nằm ở thư mục làm việc tạm.
- **Phần đầu (name / description) không đưa vào file Word** (chốt #4). Plugin tự giữ phần này, vì Word hay tự đổi
  `---` thành đường kẻ. Tên / Mô tả chỉ sửa trong popup.
- Khách lưu trong Word → plugin chuyển ngược về Markdown. Bảng có cột Layer tự bọc `` ` ``. Sau đó chạy lại kiểm
  frontmatter + **Bảng layer chuẩn** (`Layer | Nội dung | Màu | Linetype | Lineweight`).
  - Bảng bị làm lệch (thiếu cột, màu sai…) → khung chat báo **lỗi rõ theo dòng**, và vẫn hỏi có lưu không.
  - Phần Word thêm vào mà skill không giữ được (ảnh, ô gộp…) → báo như khi + Add skill.
- Skill You → ghi đè. Default / Admin → popup mục 2.
- Máy không có Word: mục Word bị mờ, tooltip "Máy này chưa cài Microsoft Word".

### 3.3 Google Docs — KHÔNG LÀM (chốt #2, chỉ giữ để tham khảo nếu sau này mở lại)

**Về "mở xem trước, chỉ đòi đăng nhập khi sửa":**

- Muốn mở trên Google Docs mà chưa đăng nhập thì file phải nằm trên Drive ở chế độ *ai có link cũng xem được*. Khi đó
  nội dung skill **lộ qua link**, kể cả chuẩn riêng của khách hay skill Admin của cậu.
- Chốt #1 (trước khi bỏ):
  - **Xem trước ngay trong plugin**: Markdown hiện thành văn bản đẹp trong khung chat, không cần đăng nhập.
  - Chỉ khi bấm **"Sửa trên Google Docs"** thì mới đăng nhập Google và tải lên **Drive riêng của khách**. Doc để
    chế độ **Hạn chế**, không công khai.

**Luồng:**

1. Đăng nhập Google **một lần** theo kiểu ứng dụng máy tính: trình duyệt mở trang cho phép, rồi trả về plugin qua
   `localhost`.
   - Quyền tối thiểu là **`drive.file`**: plugin chỉ thấy file do chính nó tạo, không đọc được Drive khác của khách.
   - Token lưu mã hóa theo user Windows (DPAPI), ở `%LocalAppData%\ClaudeCAD\google-token.bin`.
2. Plugin tải Markdown lên thành Google Docs: Drive API upload, chuyển `text/markdown` → Google Docs. Thư mục
   `ClaudeCAD skill` trên Drive của khách. Sau đó trình duyệt mở doc.
3. Khách sửa trên Google Docs xong, quay lại plugin bấm **"Lấy bản sửa về"**. Thẻ này hiện trong khung chat suốt lúc
   đang sửa. Plugin cũng tự hỏi giờ sửa của doc mỗi 1 phút khi AutoCAD mở.
   - Plugin xuất Markdown → kiểm như mục 3.2 → You: ghi đè / Default · Admin: popup mục 2.
4. Doc giữ trên Drive khách, nên lần sau bấm Sửa trên Google Docs sẽ mở lại đúng doc đó. Nút "Bỏ liên kết" xóa liên
   kết (doc vẫn ở Drive khách).

**Việc cần ở phía Google (nếu sau này làm):**

- Tạo một Google Cloud project và một **OAuth client kiểu "Desktop app"**.
- Bật **Google Drive API**.
- Màn hình xin quyền chỉ cần `drive.file`. Đây là quyền "không nhạy cảm", nên Google **không bắt** kiểm duyệt ứng
  dụng. Khi còn ở chế độ "Testing", tối đa 100 người dùng thử, phải thêm email của họ; chuyển sang "In production"
  thì ai cũng đăng nhập được.
- Client ID được nhúng vào plugin. Client secret của ứng dụng máy tính theo Google không phải bí mật thật, nhưng vẫn
  để ngoài repo, nhúng lúc build như `activation.json`.

**Skill Admin trên Google Docs của khách:**

- Bản Admin là chuẩn của cậu. Tải lên Drive khách nghĩa là chuẩn đó nằm trong Drive của họ, chia sẻ được cho người
  khác.
- Dù vậy, khách vốn đã có file `SKILL.md` trên máy, nên mức lộ thêm là "dễ chia sẻ hơn", không phải "lộ cái chưa
  từng thấy".

## 4. Code (ước tính theo phần)

| Phần | File | Công |
| --- | --- | --- |
| Menu Sửa + bản làm việc tạm + popup Tạo skill mới (thay `PrepareEdit` / `-sua` tự động) | `ChatControl`, `ChatController`, `SkillStore`, `UI/SkillInfoDialog` (dùng lại) | ~1 ngày |
| Ứng dụng .md: mặc định / Mở bằng… / nhớ lựa chọn | `ChatController`, `PaletteSettings` | ~0,25 ngày |
| Word: `MarkdownToDocx` + theo dõi file lưu + chuyển ngược + báo lỗi bảng | `Core/MarkdownToDocx.cs` (mới), `ChatController` | ~1–1,5 ngày |
| Test lõi + UI test + tài liệu | | ~0,5–1 ngày |
| CI 1–2 lượt | | chờ |

Tổng khoảng **3 ngày công**. Không cần làm gì trên Google, và không có phần mới cần thread bảo mật rà (không đăng
nhập, không mạng).

## 5. Test

- **Lõi:**
  - `MarkdownToDocx` ↔ `DocxToMarkdown` đi khứ hồi giữ nguyên tiêu đề / danh sách / bảng layer.
  - Bản làm việc: lưu → đúng quy tắc You / Default / Admin. Tên mới trùng / sai quy tắc bị chặn.
  - Bảng layer lệch → báo đúng dòng.
- **UI test (CI Windows):** menu Sửa, popup Tạo skill mới.
- **Thử thật:** sửa bằng Notepad / VS Code / Word trên AutoCAD 2026.
