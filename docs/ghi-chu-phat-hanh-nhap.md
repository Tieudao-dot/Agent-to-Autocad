# Ghi chú phát hành ClaudeCAD 0.9.6 — BẢN NHÁP

*Soạn 22/11/2026 trên nhánh gộp. Bản 0.9.6 mới là bộ cài thử, chưa phát hành. Nội dung đầy đủ từng nhóm:
[`ban-0.9.6.md`](ban-0.9.6.md) (nếu có trên nhánh), kế hoạch đẩy skill: [`ke-hoach-day-skill.md`](ke-hoach-day-skill.md).*

---

## A. Điểm mới so với v0.9.5 (viết cho người dùng)

### 1. Chủ plugin gửi skill tới từng máy

- Chủ plugin chọn skill cho từng máy; plugin tự nhận khi mở AutoCAD và khoảng 6 giờ một lần, không phải chép file tay.
- Danh sách Skill có nhãn: **Admin** (chủ plugin gửi), **Default** (skill mẫu có sẵn), **You** (skill bạn tự thêm
  hoặc đã sửa).
- Skill mới được gửi tới **không tự bật**: bấm **+** trong danh sách Skill để dùng cho đoạn chat.
- Trùng tên với skill bạn đang có thì giữ cả hai; bản của chủ plugin có đuôi `-admin`.

### 2. Claude tự kiểm bản vẽ trước khi báo xong

- Vẽ / sửa xong, Claude đo lại bản vẽ thật và sửa lỗi (tối đa 2 vòng) trước khi báo xong; còn lỗi thì liệt kê rõ.
  Thẻ **Tự kiểm** hiện trong khung chat.
- Claude bắt điểm theo đối tượng thật (đầu mút, trung điểm, tâm, giao điểm…) thay vì tự tính tọa độ, nên cửa, tường,
  kích thước khớp nhau hơn.
- Khi đang dùng skill có bảng chuẩn: không vẽ vào layer 0 hay layer ngoài chuẩn (trừ khi bạn nêu tên layer), tự tạo
  layer chuẩn còn thiếu, chiều cao chữ lấy theo bảng × tỷ lệ in.

### 3. Skill mẫu mới, tự cập nhật

- 3 skill mẫu (`ve-kien-truc`, `ve-mep`, `ve-ha-tang`) viết lại: chuẩn bị trước khi vẽ, dựng hình bằng offset / trim,
  tự kiểm, lỗi hay gặp. Số liệu vẫn là số mẫu.
- Máy đã cài nhận skill mẫu mới ở lần mở AutoCAD đầu tiên sau khi cập nhật, **nếu bạn chưa sửa** skill đó. Skill bạn
  đã sửa hoặc tự thêm được giữ nguyên.

### 4. Lưu ý khi nâng cấp từ v0.9.5

- Cập nhật như thường (tự cập nhật hoặc chạy đè `ClaudeCAD-Setup.exe`); **không phải kích hoạt lại**.
- Lịch sử chat, macro, mẫu lệnh, skill của bạn được giữ nguyên.
- Mỗi lượt Claude dùng thêm một ít hạn mức do danh sách lệnh dài hơn (khoảng 9 %).

---

## B. Dòng "Lịch sử phiên bản" trong `bo-cai/README.md`

Đã có sẵn dòng `0.9.6 (bản thử, chưa phát hành)`. Khi phát hành: đổi nhãn thành `**0.9.6**` và bỏ chữ "bản thử".

---

## C. Mô tả bản phát hành trên GitHub Releases

CI tự sinh phần đầu (link tải trực tiếp, hướng dẫn cài, kích thước, SHA-256). Dán thêm phần dưới nếu muốn:

```markdown
### Điểm mới
- **Chủ plugin gửi skill tới từng máy**; danh sách Skill có nhãn Admin / Default / You.
- **Claude tự kiểm bản vẽ trước khi báo xong**: bắt điểm theo đối tượng, kiểm layer / chữ / kích thước, tự sửa lỗi.
- **3 skill mẫu viết lại** và tự cập nhật trên máy đã cài nếu bạn chưa sửa.

Nâng cấp từ 0.9.5 không phải kích hoạt lại; skill, macro, lịch sử chat được giữ nguyên.
```

---

## Thử thật trước khi phát hành (bộ cài thử)

1. Đẩy skill theo máy: các bước thử ở [`ke-hoach-day-skill.md`](ke-hoach-day-skill.md) mục 6.
2. Tự kiểm: [`checklist-thu-that.md`](checklist-thu-that.md) phần I, bước I10–I13.
3. Nhãn Admin / Default / You trong danh sách Skill.
4. Skill mẫu tự cập nhật trên máy đang cài 0.9.5: `test-log.md` mục S-6, S-7.

## Việc còn lại ở bước phát hành (thread gộp nhánh & phát hành)

1. Chủ dự án thử xong và đồng ý rõ → hỏi lại trước khi tạo PR / merge `main`.
2. Đổi nhãn dòng lịch sử (phần B), CI `release: "false"` xanh trên commit cuối.
3. Merge `main` (CI tự phát hành), gộp thêm tài liệu của luồng chính và `claude/luu-tru-bo-cai` nếu có commit mới.
4. Gửi thread lưu trữ bộ cài: link tải trực tiếp + kích thước + SHA-256.
