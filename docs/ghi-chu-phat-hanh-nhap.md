# Ghi chú phát hành — BẢN NHÁP cho ClaudeCAD `<PHIÊN BẢN>`

*Soạn 15/11/2026 trên `claude/release-0.9.5`. Chưa nâng phiên bản, chưa phát hành. Khi chủ dự án chốt tên, thay
`<PHIÊN BẢN>` ở cả file, chọn một phương án cho AutoCAD 2027 (mục 3), rồi chép từng phần vào đúng chỗ (mục
"Dùng ở đâu" cuối file).*

---

## A. Điểm mới so với v0.9.3 (viết cho người dùng)

### 1. Kích hoạt và bảo mật

- **Mỗi máy cần được kích hoạt.** Bộ cài hiện **mã máy** (`CC-…`) và tự gửi yêu cầu cho chủ plugin. Có hai cách:
  chờ chủ plugin duyệt (cài tiếp không cần key, được duyệt là mở AutoCAD dùng được), hoặc nhập key nhận qua Zalo.
- **Key gắn với máy**, dùng vĩnh viễn trên máy đó; cài đè hay cập nhật không hỏi lại. Chép thư mục plugin sang máy
  khác thì máy đó vẫn phải kích hoạt.
- **Chủ plugin duyệt / khóa từng máy** trong bảng thiết bị. Máy bị khóa hiện thẻ 🔒 trong khung chat; được mở lại
  thì bấm **Kiểm tra lại** là dùng tiếp, không cần key mới.
- Plugin kiểm tra trạng thái mỗi lần mở AutoCAD và khoảng 6 giờ một lần. **Mất mạng vẫn dùng được tối đa 7 ngày.**

### 2. Giao diện kiểu AutoCAD

- Khung chat lấy **màu và font từ theme của AutoCAD** (giống Properties, Layer); đổi Color theme là khung chat đổi theo.
- Thanh công cụ và hàng nút dưới ô nhập thành **icon có tooltip**; ⚙ gọn lại thành các nhóm thu gọn.
- **Gửi thêm ý khi Claude đang làm:** gõ rồi Enter, tin tự gửi khi lượt xong. **Esc** trong ô nhập để dừng lượt.
- **Thu gọn khung chat (nút ⌄):** chỉ còn ô nhập; lượt xong hiện một dòng báo, bấm để mở lại nguyên đoạn chat.
- **Icon ClaudeCAD trên thanh trạng thái AutoCAD:** chấm xanh khi Claude đang làm, chấm vàng khi cần cậu xem.
- **Thẻ "Bắt đầu" 3 bước** (Kích hoạt → Đăng nhập Claude → Thử một yêu cầu mẫu).
- **Thấy Claude đang làm bước nào:** danh sách bước ✓ / ● ngay trên câu trả lời; với kế hoạch đã duyệt thì hiện
  "Bước 3/7: …".
- Thẻ xác nhận trước khi xóa / sửa nhiều đối tượng in to số lượng, có nút **Xem trên bản vẽ**.

### 3. AutoCAD 2026 / 2027

Vẫn **một bộ cài chung**. AutoCAD 2026 dùng chung bản plugin với 2025.

> **Chọn MỘT trong hai phương án cho AutoCAD 2027, xóa phương án còn lại:**
>
> **Phương án "hỗ trợ":**
> - Hỗ trợ **AutoCAD 2022 – 2027** (bản đầy đủ, không phải LT). AutoCAD 2027 có bản plugin riêng (.NET 10).
>
> **Phương án "thử nghiệm":**
> - Hỗ trợ **AutoCAD 2022 – 2026** (bản đầy đủ, không phải LT).
> - **AutoCAD 2027: thử nghiệm.** Bộ cài có sẵn bản plugin cho 2027 nhưng chưa được thử trên AutoCAD 2027 thật;
>   gặp lỗi thì dùng ⚙ ▸ Gửi báo cáo lỗi.

### 4. Sửa lỗi và ổn định

- **`install.ps1` (cài không dùng Setup.exe) của v0.9.3 bị lỗi cú pháp, không chạy được — đã sửa.**
- Khoảng 30 bản sửa sau đợt rà soát toàn dự án, đáng chú ý:
  - Xóa / sửa hàng loạt luôn hỏi xác nhận trước.
  - Tự gửi lại khi hết hạn mức không gửi nhầm sang bản vẽ khác.
  - Lệnh nặng không bị vẽ trùng; cuộc chat dài mở nhanh hơn.
  - Lỗi của plugin không làm AutoCAD đóng đột ngột; khởi động lại trợ lý không để sót tiến trình.
  - Bộ cài không treo ở trang cuối; báo cáo lỗi không kèm nội dung chat.
- **Skill theo từng đoạn chat:** thêm nhiều file skill một lúc, danh sách chia *Đang dùng* / *Thư viện*.
- **Tự kiểm tra mở rộng** (`CLAUDECHATSELFTEST`): kiểm thêm bản plugin có đúng đời AutoCAD không, theme, icon thanh
  trạng thái, thu gọn / mở lại, hàng chờ gửi.

### 5. Lưu ý khi nâng cấp từ v0.9.3

- **Phải kích hoạt máy sau khi cập nhật.** Bản 0.9.3 chưa khóa theo máy, nên lần đầu chạy bản mới máy sẽ hỏi kích
  hoạt: chờ chủ plugin duyệt hoặc nhập key (liên hệ Zalo chủ plugin, số hiện ngay trong bộ cài và thẻ 🔒).
- Chưa kích hoạt thì khung chat khóa, **AutoCAD và bản vẽ không bị ảnh hưởng**.
- Lịch sử chat, macro, mẫu lệnh, skill và đăng nhập Claude được giữ nguyên.
- Cần mạng ở lần kích hoạt đầu; sau đó mất mạng vẫn dùng được tối đa 7 ngày.

---

## B. Dòng cho bảng "Lịch sử phiên bản" trong `bo-cai/README.md`

Thay dòng `0.9.5 *(bản thử, chưa phát hành; …)*` hiện có bằng dòng dưới (giữ nội dung chi tiết cũ nếu muốn, hoặc
dùng bản gọn này). Đồng thời xóa ghi chú "Bộ cài thử 0.9.5: …" ngay dưới tiêu đề mục.

```markdown
| **<PHIÊN BẢN>** | **Kích hoạt theo máy + quản lý thiết bị:** bộ cài hiện mã máy, chủ plugin duyệt / khóa từng máy trong bảng thiết bị (key qua Zalo vẫn dùng được), mất mạng dùng tối đa 7 ngày. **Giao diện kiểu AutoCAD:** màu + font theo theme AutoCAD, nút icon có tooltip, gửi thêm ý khi Claude đang làm, Esc để dừng, thu gọn khung chat, icon trên thanh trạng thái, thẻ "Bắt đầu" 3 bước, tiến độ từng bước. **AutoCAD 2026 / 2027:** <CHỌN: "hỗ trợ 2022–2027" | "hỗ trợ 2022–2026, 2027 thử nghiệm">. **Sửa lỗi:** `install.ps1` của 0.9.3, ~30 bản sửa sau rà soát, skill theo từng đoạn chat, tự kiểm tra mở rộng. **Nâng cấp từ 0.9.3 phải kích hoạt máy.** |
```

---

## C. Mô tả bản phát hành trên GitHub Releases

CI tự tạo phần mô tả ngắn (link hướng dẫn cài + kích thước và SHA-256 của `ClaudeCAD-Setup.exe`). Phần dưới **dán
thêm lên trên** phần đó sau khi bản phát hành được tạo (sửa mô tả trên trang Releases). Không cần đổi CI.

```markdown
## ClaudeCAD <PHIÊN BẢN>

**Quan trọng khi nâng cấp từ 0.9.3:** bản này khóa theo máy. Sau khi cập nhật, máy sẽ hỏi kích hoạt — chờ chủ plugin duyệt hoặc nhập key (liên hệ Zalo chủ plugin, số hiện trong bộ cài). Lịch sử chat, macro, skill được giữ nguyên.

### Điểm mới
- **Kích hoạt theo máy:** bộ cài hiện mã máy; chủ plugin duyệt / khóa từng máy; mất mạng dùng tối đa 7 ngày.
- **Giao diện kiểu AutoCAD:** màu + font theo theme AutoCAD, nút icon có tooltip, gửi thêm ý khi Claude đang làm, Esc để dừng, thu gọn khung chat, icon trên thanh trạng thái, tiến độ từng bước.
- **AutoCAD 2026 / 2027:** <CHỌN: "một bộ cài cho AutoCAD 2022–2027" | "một bộ cài cho AutoCAD 2022–2026; AutoCAD 2027 ở mức thử nghiệm">.
- **Sửa lỗi:** `install.ps1` của 0.9.3 không chạy được; ~30 bản sửa về xác nhận trước khi xóa, tự gửi lại, độ ổn định.

### Cài đặt
Tải `ClaudeCAD-Setup.exe` bên dưới, chạy và làm theo hướng dẫn: https://github.com/Tieudao-dot/Agent-to-Autocad/tree/main/bo-cai
Yêu cầu: Windows, AutoCAD bản đầy đủ (không phải LT), tài khoản Claude Pro / Max hoặc API key Anthropic.
```

---

## Dùng ở đâu khi phát hành

| Phần | Chép vào | Lúc nào |
| --- | --- | --- |
| A | Tin báo cho người dùng (Zalo / email), và làm nguồn cho hướng dẫn người dùng sau phát hành | Khi phát hành |
| B | `bo-cai/README.md`, bảng "Lịch sử phiên bản" | Cùng commit nâng phiên bản |
| C | Mô tả bản phát hành trên GitHub Releases | Sau khi CI tạo bản phát hành |

## Việc còn lại ở bước phát hành (ghi cho thread gộp nhánh & phát hành)

1. Hỏi chủ dự án tên phiên bản, rồi phương án cho AutoCAD 2027 (mỗi lần một câu).
2. Nâng phiên bản ở mọi nơi (có test kiểm phiên bản thống nhất); thay `<PHIÊN BẢN>`; chép phần B.
3. Gộp `claude/wizardly-heisenberg-ugup5c` vào `claude/release-0.9.5` để `main` có tài liệu mới (nhánh đó chỉ hơn
   phần tài liệu: tổng quan gộp một file, `CLAUDE.md`, xóa `docs/plugin-tong-quan.md` và
   `docs/ke-hoach-gop-nhanh.md`; thêm hướng dẫn người dùng `docs/huong-dan-su-dung.md`). Sau khi gộp, cho phần A
   và phần C trỏ tới file hướng dẫn đó.
   Sửa dòng 20 của `bo-cai/README.md` (đang ghi AutoCAD 2026 / 2027 "đang làm, chưa phát hành") cho khớp phương án
   2027 đã chốt.
4. CI `release: "false"` xanh trên commit cuối.
5. Chỉ khi chủ dự án đồng ý rõ: tạo PR / merge `main` (CI tự phát hành), rồi dán phần C lên Releases.
6. Xóa file nháp này sau khi phát hành.
