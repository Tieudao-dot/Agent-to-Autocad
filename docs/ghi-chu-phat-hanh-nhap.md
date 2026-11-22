# Ghi chú phát hành ClaudeCAD 0.9.7

*Soạn 22/11/2026. Bản sửa nhỏ sau 0.9.6, phát hành theo yêu cầu của chủ dự án; phần bộ cài chưa được thử trên Windows
thật (mục I-6 trong [`test-log.md`](test-log.md)). Điểm mới của 0.9.6: xem [`ban-0.9.6.md`](ban-0.9.6.md).*

## Điểm mới so với v0.9.6

- **Cài đè khi AutoCAD đang mở:** chạy `ClaudeCAD-Setup.exe` lên máy đã cài mà AutoCAD còn mở, bộ cài hiện hộp tiếng
  Việt "AutoCAD đang mở… đóng AutoCAD rồi bấm Retry" thay cho hộp lỗi tiếng Anh *"Setup was unable to automatically
  close all applications"*. Đóng AutoCAD rồi bấm Retry là cài tiếp.
- Bộ cài tự dừng tiến trình trợ lý của ClaudeCAD còn chạy trước khi chép file. Node.js khác trên máy không bị đụng tới.
- Plugin và trợ lý không đổi so với 0.9.6.

## Lưu ý khi nâng cấp

- Tự cập nhật trong khung chat (`CLAUDECHATUPDATE`) như thường; không phải kích hoạt lại.
- Bộ cài 0.9.6 trở về trước vẫn hiện hộp tiếng Anh nếu AutoCAD đang mở: đóng AutoCAD rồi bấm **Try again**.

## Mô tả cho GitHub Releases (dán thêm dưới phần CI tự sinh, nếu muốn)

```markdown
### Điểm mới
- Cài đè khi AutoCAD đang mở: bộ cài nhắc bằng tiếng Việt "đóng AutoCAD rồi bấm Retry" và tự dừng tiến trình trợ lý còn sót, thay cho hộp lỗi tiếng Anh.

Plugin không đổi so với 0.9.6. Nâng cấp không phải kích hoạt lại.
```
