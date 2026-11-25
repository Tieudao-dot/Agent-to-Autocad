# Kế hoạch: skill nhận file .docx và nội dung Google Docs

> Trạng thái: **bản nháp, chờ chủ dự án duyệt**. Chưa code. Nhánh `claude/skill-docx-gdocs`, tách từ `main` @6e90240
> (v0.9.7). Yêu cầu từ thread QnA, chuyển qua luồng chính. Các chỗ **[CHỐT]** đang chờ chủ dự án chọn; phương án đề
> xuất ghi trước.

## 1. Mục tiêu

1. **.docx:** nút **+ Add skill** nhận thêm file Word. Plugin chuyển file sang Markdown, lưu thành `SKILL.md`, rồi
   kiểm như file `.md` hiện nay (frontmatter, bảng layer chuẩn).
2. **Google Docs:** chủ plugin soạn skill trên Google Docs. Máy chủ Apps Script lấy nội dung, ký, rồi đẩy tới các
   máy được gán qua kênh đẩy skill đã có. Máy khách **không tự tải link**, vì ai sửa được doc là sửa được hành vi
   Claude trên máy khách.

## 2. Phần .docx (làm trước)

### Luồng

```
+ Add skill → chọn *.md hoặc *.docx (chọn được nhiều)
  .docx → DocxToMarkdown (Core, không cần Word, không cần thư viện ngoài)
        → có frontmatter ở đầu file? (các dòng --- / name: / description: / ---)
            có  → dùng luôn
            không → hộp thoại: Tên skill (gợi ý từ tên file) + Mô tả khi nào dùng   [CHỐT 1]
        → SkillStore.ImportText(...) → cùng bước kiểm như .md (frontmatter, bảng layer, trùng tên)
        → ghi chú trong chat: "✓ Đã thêm … từ Word" + danh sách phần bị bỏ
```

### Chuyển đổi

- **Cách đọc:** `.docx` là file zip. Plugin đọc `word/document.xml`, `styles.xml` và `numbering.xml` bằng
  `ZipArchive` + `XDocument`, có sẵn trong .NET 4.8 / 8 / 10. Không thêm gói NuGet.
- **Giữ được:**
  - Tiêu đề (Heading 1–6 / Title) → `#`…`######`. Nhận cả tên style tiếng Việt, tra qua `styles.xml`.
  - Đậm / nghiêng.
  - Danh sách chấm và danh sách số, có thụt cấp.
  - Bảng → bảng Markdown. Hàng đầu làm tiêu đề, nên bảng layer `Layer | Nội dung | Màu | Linetype | Lineweight`
    soạn trong Word vẫn qua được bước kiểm bảng chuẩn.
  - Chữ của link. Ngắt dòng.
  - Chỉnh sửa theo dõi (tracked changes): lấy bản đã chấp nhận, tức bỏ chữ bị xóa và giữ chữ được thêm.
- **Bỏ và báo rõ:**
  - Ảnh / hình vẽ: số lượng bị bỏ **[CHỐT 2]**.
  - Ô gộp ngang / dọc: ô được tách ra và để trống, kèm cảnh báo.
  - Hộp chữ, chú thích cuối trang, ghi chú lề, công thức toán, đầu trang / chân trang.
- **Giới hạn:** file tối đa 10 MB; kết quả tối đa 45.000 ký tự, giống giới hạn của skill đẩy. `.doc` (Word cũ) không
  hỗ trợ: báo "Lưu lại thành .docx".
- **Theo dõi file:** `FileSystemWatcher` vẫn chỉ theo dõi `*.md` trong thư viện, vì thư viện chỉ chứa `SKILL.md` đã
  chuyển. File Word gốc không được chép vào thư viện. Đường dẫn file gốc ghi ở cột Nguồn (`source`). Muốn cập nhật thì
  chọn lại file Word bằng + Add skill; plugin sẽ hỏi có thay không, như hiện nay.

### Code

| File | Thay đổi |
| --- | --- |
| `Core/DocxToMarkdown.cs` *(mới)* | `Convert(Stream) → (markdown, List<string> dropped)`. Thuần .NET, có test. |
| `Core/SkillStore.cs` | `ImportText(name, md, source, overwrite)`: dùng chung phần kiểm của `Import`. |
| `ChatController.cs` | Bộ lọc file `*.md;*.docx`; nhánh .docx; hộp thoại tên + mô tả; ghi chú phần bị bỏ. |
| `UI/` | Hộp thoại nhỏ "Thêm skill từ Word": Tên, Mô tả, nút Thêm / Hủy. |

## 3. Phần Google Docs (qua kênh đẩy skill có chữ ký)

### Luồng

```
Chủ plugin: trang "Skill" trong Sheet, cột Nội dung dán LINK Google Docs thay vì nội dung   [CHỐT 3]
  (doc KHÔNG cần chia sẻ công khai: Apps Script chạy bằng tài khoản chủ plugin nên đọc được doc riêng)
Apps Script, mỗi lần tính gói cho một máy:
  link docs.google.com/document/d/<id> → Drive API export text/markdown → nội dung skill
  phiên bản = giờ sửa cuối của doc (tự đổi khi chủ plugin sửa doc)   [CHỐT 4]
  → cùng bước kiểm (tên, frontmatter, 45.000 ký tự) → ký vào gói CCP1 như hiện nay
Máy khách: không đổi gì. Vẫn nhận gói ký số như bây giờ.
```

- **Nhanh và nhẹ:**
  - Dùng lại giới hạn 1 lần/phút và nhớ 10 phút cho mỗi máy.
  - Nội dung doc được cache theo `(id, giờ sửa)`, nên doc không đổi thì không tải lại.
  - Doc lớn hơn ô cache (100 KB) thì tải lại khi cần.
- **Lỗi:** lỗi được ghi ở cột Ghi chú của trang Skill, ví dụ "không mở được doc (sai link / chưa có quyền)", "doc
  thiếu phần đầu name / description", "dài quá 45.000 ký tự".
- **Quyền Google mới:** script cần thêm quyền đọc Drive (`drive.readonly`) và gọi URL ngoài (`UrlFetchApp`). Chạy
  `kiemTra` một lần thì Google sẽ hỏi lại quyền.
- **Code:** `Code.gs.template`, các hàm `skillLibrary` / `docsContent` mới, cùng test VM trong
  `agent/test/activation.test.ts` (giả lập Drive / UrlFetch).
- **Link trong plugin** (dán link, nút "Cập nhật từ link") **[CHỐT 3]**: đề xuất **không làm** ở bản này. Plugin
  không phân biệt được máy của chủ plugin với máy khách. Cho mọi máy tự tải link nghĩa là ai sửa được doc là đổi được
  hành vi Claude. Chủ plugin đã có đường an toàn qua Sheet.

## 4. Test

- **Lõi C#:**
  - `DocxToMarkdown`: file .docx mẫu dựng ngay trong test bằng `ZipArchive`, có tiêu đề, danh sách nhiều cấp, bảng
    layer, ô gộp, ảnh, tracked changes, style tiếng Việt.
  - File hỏng / không phải zip / quá lớn.
  - `ImportText`: trùng tên, frontmatter có sẵn.
- **Trợ lý / Apps Script (VM):** link doc → nội dung; cache theo giờ sửa; doc lỗi → Ghi chú; nội dung dán thẳng vẫn
  chạy như cũ.
- **CI Windows:** 1–2 lượt với `release: false`, có UI test hộp thoại.
- **Rà bảo mật:** gửi phần Apps Script (quyền Drive, tải doc) cho thread bảo mật trước khi chủ plugin triển khai.

## 5. Tài liệu

`skills/README.md`, `docs/palette-features.md`, `activation/README.md` (cột link Google Docs, quyền mới),
`bo-cai/README.md` (dòng bản kế tiếp).

## 6. Việc chủ dự án làm tay trên Google (sau khi rà bảo mật)

1. Dán `Code.gs` mới, **không đụng `BiMat.gs`**.
2. Chạy `kiemTra` và cho phép thêm quyền đọc Drive.
3. Quản lý bản triển khai → **Phiên bản mới**.
4. Ở trang Skill, dán link doc vào cột Nội dung.

## 7. Ước tính

| Phần | Công |
| --- | --- |
| .docx: bộ chuyển đổi + hộp thoại + import + test | ~1–1,5 ngày |
| Google Docs qua Apps Script + test VM | ~0,5–1 ngày |
| Tài liệu | ~0,25 ngày |
| CI Windows 1–2 lượt (~15 phút/lượt), rà bảo mật | chờ |

Tổng khoảng **2–2,5 ngày công**. Tốn nhất là bộ chuyển `.docx`, vì phải tự đọc XML của Word cho bảng / danh sách, và
các lượt CI.
