# Bản 0.9.6: nội dung cập nhật (tổng hợp 22/11/2026)

Bản 0.9.6 chưa phát hành. Mọi phần code đã xong và có CI xanh, nằm trên 3 nhánh riêng, chưa gộp, chưa có bộ cài.
Máy chủ Google đã được cập nhật sẵn cho 0.9.6. Khách đang dùng 0.9.5 vẫn chạy bình thường.

## 1. Ba nhóm tính năng mới

### A. Đẩy skill theo máy

Nhánh `claude/day-skill-theo-may` @f92d8c6. CI Windows xanh (run 45006820369).

- **Trên Google Sheet:** có thêm 2 trang **Skill** và **Gán skill**. Ở trang Gán skill, chủ plugin chọn từng mã máy rồi chọn Thêm / Ẩn / Xóa skill.
- **Plugin tự nhận skill:** mỗi lần kiểm tra trạng thái (khi mở AutoCAD và 6 giờ một lần), plugin tự nhận gói skill của máy mình.
- **Gói skill có chữ ký số:** gói dùng chữ ký `CCP1`, cùng cặp khóa với giấy phép.
  - Chống phát lại gói cũ.
  - Giới hạn: tối đa 20 skill, 45.000 ký tự, 400 KB.
- **Nhãn trong danh sách skill:**
  - **Admin:** skill do chủ plugin đẩy.
  - **Default:** skill mẫu có sẵn.
  - **You:** skill khách tự thêm. Skill Admin mà khách sửa lại cũng thành You.
- **Trùng tên:** giữ cả hai skill, bản của chủ plugin đổi tên thành `<tên>-admin`.
- **Skill bị Ẩn:** không được nạp. Skill mới đẩy xuống không tự bật.
- **Bí mật máy chủ tách riêng:** bí mật nằm trong file `BiMat.gs`, còn `Code.gs` không chứa bí mật nên dán đè được khi nâng cấp.
- **Rà bảo mật:** thread bảo mật đã rà xong, không có lỗi chặn, 3 góp ý đã sửa.
- **Máy chủ Google:** đã triển khai ngày 22/11, `?action=ping` trả `ready:true`, link máy chủ giữ nguyên.

### B. Tự kiểm bản vẽ trước khi báo xong

Nhánh `claude/tu-kiem-tra` @1eacfe3. CI Windows xanh (run 45009114005). Lõi 236/236, trợ lý 172/172.

- **Bắt điểm theo đối tượng:** điểm có thể viết dạng `{"snap": "end|mid|cen|int|per|nea|ins", "of": đối tượng}`, có thêm tool `get_snap_points`.
  - Claude bám vào điểm thật của hình đã vẽ, không phải tự tính tọa độ.
  - Dạng `[x, y]` cũ, macro và mẫu lệnh đã lưu vẫn chạy như trước.
- **Kiểm tra bản vẽ `verify_drawing`:** có 14 mã lỗi chia 5 nhóm: điểm không khớp, sai layer / thuộc tính, chữ / dim sai, hình học sai, thiếu / thừa đối tượng.
  - Chuẩn đọc từ bảng trong SKILL.md.
  - Khe hở dưới 10 mm thì tự nối; skill có thể đổi ngưỡng này.
- **Bắt buộc sửa hết lỗi:** trước khi báo xong, Claude phải tự kiểm và sửa hết lỗi, tối đa 2 vòng. Còn lỗi thì liệt kê cho người dùng. Thẻ **Tự kiểm** hiện trong khung chat.
- **Tự áp chuẩn khi vẽ:**
  - Từ chối vẽ vào layer 0 hoặc layer ngoài bảng chuẩn, trừ khi người dùng tự nêu tên layer hoặc chọn bằng nút Layer.
  - Tự tạo layer chuẩn còn thiếu.
  - Chữ không ghi chiều cao thì lấy theo bảng chữ × tỷ lệ in.
- **Chi phí:** danh sách tool to thêm khoảng 9 % (~2,5k token mỗi lượt, có cache).

### C. Skill mẫu mới và tự cập nhật skill mẫu

- **3 skill mẫu viết lại cấu trúc** (trên `claude/nghien-cuu-skill` @6313359, đã gộp vào nhánh B):
  - danh mục chuẩn bị trước khi vẽ;
  - dựng hình bằng offset / trim;
  - tự kiểm bằng lệnh đo;
  - các lỗi hay gặp;
  - mẫu báo cáo.

  Số liệu vẫn là số mẫu, chờ chuẩn của công ty.
- **Skill mẫu tự cập nhật** (`claude/release-0.9.5` @5838cf1): máy đã cài sẽ được thay skill mẫu bản mới khi mở AutoCAD, nếu người dùng chưa sửa skill đó. Trước đây bộ cài không ghi đè skill mẫu nên máy cũ không nhận bản mới.

## 2. Chưa có trong 0.9.6

| Mục | Tình trạng |
| --- | --- |
| Ghi "Đã gỡ lúc" khi gỡ plugin | Vẫn để sau, nằm ở `claude/device-management` |
| Máy đã đổi Disabled vẫn dùng được | Chưa rõ nguyên nhân. Chờ cậu đóng hẳn rồi mở lại AutoCAD; nếu vẫn dùng được thì gửi mã máy `CC-…` cho thread bảo mật |
| Số liệu chuẩn công ty trong skill mẫu | Chờ cậu gửi tài liệu chuẩn |
| Thử key K-5 … K-16, thử AutoCAD 2027 trên máy thật | Chưa làm |
| Tách `ChatControl.cs` | Để sau |

## 3. Gộp và phát hành: việc còn lại

- **Gộp thử 3 nhánh:** tớ đã gộp thử trên máy, chưa đẩy lên.
  - A và B đụng nhau ở 3 file: `SkillStore.cs`, `ChatControl.cs`, `bo-cai/README.md`. Mỗi file có 1 chỗ cần chọn tay.
  - A và C đụng nhau ở 2 file: `bo-cai/README.md`, `ClaudeCAD.Core.Tests.csproj`.

  Đều là chỗ nhỏ, thread gộp nhánh xử lý được.
- **Các bước** (thread gộp nhánh & phát hành làm, khi cậu đồng ý):
  1. Tạo nhánh 0.9.6 từ `main`, gộp A + B + C, nâng phiên bản lên 0.9.6.
  2. Chạy CI và build **bộ cài thử** (`release: false`).
  3. Cậu thử thật:
     - đẩy skill theo `docs/ke-hoach-day-skill.md` mục 6 (6 bước);
     - tự kiểm theo checklist phần I (I10–I13);
     - nhãn và cập nhật skill mẫu.
  4. Cậu đồng ý thì merge `main`, CI tự phát hành 0.9.6 và ghi vào sổ lưu trữ bộ cài.
- **Ước tính:** gộp + 1–2 lượt CI khoảng nửa ngày làm của thread. Thời gian cậu thử thật khoảng 30–45 phút.

## 4. Cậu cần quyết

1. Gộp cả 3 nhóm A + B + C vào một bộ cài thử 0.9.6 (tớ đề xuất), hay chỉ gộp một phần.
2. Sau khi thử bộ cài: đồng ý phát hành hay sửa thêm.
