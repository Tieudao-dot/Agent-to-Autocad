# Ghi chú phát hành ClaudeCAD 0.9.8 — BẢN NHÁP

*Soạn 25/11/2026 trên nhánh gộp. Bản 0.9.8 mới là bộ cài thử, chưa phát hành. Kế hoạch và chi tiết:
[`ke-hoach-skill-docx-gdocs.md`](ke-hoach-skill-docx-gdocs.md).*

---

## A. Điểm mới so với v0.9.7 (viết cho người dùng)

### 1. Thêm skill từ file Word

- **+ Add skill** nhận file `.docx`: plugin tự chuyển sang `SKILL.md`, giữ tiêu đề, danh sách, bảng, chữ đậm / nghiêng.
- Bảng có cột **Layer** tự thành *Bảng layer chuẩn*, nên Claude áp chuẩn và tự kiểm theo bảng đó.
- File chưa có tên / mô tả ở đầu thì plugin hỏi **Tên + Mô tả** trong một hộp thoại.
- Phần Word không chuyển được (ảnh, hộp chữ, ô gộp, chú thích) được báo rõ là đã bỏ.

### 2. Chủ plugin soạn skill trên Google Docs

- Chủ plugin dán link Google Docs vào trang **Skill** của bảng thiết bị rồi gán cho máy; máy nhận skill nhãn **Admin**
  như skill gửi thường.
- Sửa doc là máy nhận bản mới ở lần kiểm tra kế tiếp (khi mở AutoCAD và khoảng 6 giờ một lần).
- Máy của người dùng không tự tải link nào: nội dung do máy chủ của chủ plugin đọc, ký số rồi gửi xuống.

### 3. Lưu ý khi nâng cấp từ v0.9.7

- Cập nhật như thường; không phải kích hoạt lại. Skill, macro, lịch sử chat được giữ nguyên.
- Riêng chủ plugin: tính năng Google Docs cần máy chủ đã cập nhật `Code.gs` và cấp quyền xem Drive (đã làm 25/11).

---

## B. Dòng "Lịch sử phiên bản" trong `bo-cai/README.md`

Đã có dòng `0.9.8 (bản thử, chưa phát hành)`. Khi phát hành: đổi nhãn thành `**0.9.8**`.

---

## C. Mô tả bản phát hành trên GitHub Releases (dán thêm dưới phần CI tự sinh, nếu muốn)

```markdown
### Điểm mới
- **Thêm skill từ file Word (.docx):** tự chuyển sang SKILL.md, bảng có cột Layer thành bảng layer chuẩn, hỏi Tên + Mô tả khi thiếu, báo phần bị bỏ.
- **Skill soạn trên Google Docs** (chủ plugin): dán link vào trang Skill, gán cho máy; sửa doc là máy nhận bản mới.

Nâng cấp từ 0.9.7 không phải kích hoạt lại.
```

---

## Thử thật trước khi phát hành (bộ cài thử)

1. **+ Add skill** một file `.docx` có bảng layer → skill xuất hiện, bảng layer đọc được (không có cảnh báo bảng chuẩn).
2. Một `.docx` thiếu tên / mô tả → hiện hộp thoại Tên + Mô tả.
3. Dán một link Google Docs vào trang **Skill** của Sheet, gán cho máy → máy nhận skill nhãn **Admin**.
4. Sửa doc → máy nhận bản mới ở lần kiểm tra kế tiếp.
5. Cài đè bằng bộ cài này khi AutoCAD đang mở → hộp tiếng Việt "đóng AutoCAD rồi bấm Retry" (`test-log.md` I-6, phần sửa
   của 0.9.7 chưa được xác nhận trên máy thật).

## Việc còn lại ở bước phát hành (thread gộp nhánh & phát hành)

1. Chủ dự án thử xong và đồng ý rõ → hỏi lại trước khi tạo PR / merge `main`.
2. Đổi nhãn dòng lịch sử (phần B); gộp tài liệu mới của luồng chính và `claude/luu-tru-bo-cai` nếu có.
3. Merge `main` (CI tự phát hành); gửi thread lưu trữ bộ cài link tải trực tiếp + kích thước + SHA-256.
