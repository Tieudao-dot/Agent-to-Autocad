---
name: ve-mep
description: Chuẩn vẽ điện nước (MEP) của người dùng - layer hệ điện E-* (chiếu sáng, ổ cắm, máng cáp) và cấp thoát nước P-* (cấp nước, thoát nước, thông hơi), nguyên tắc đi tuyến song song tường với offset chuẩn, ký hiệu thiết bị dạng block, ghi chú đường kính ống / tiết diện dây bằng multileader. Dùng khi được yêu cầu vẽ hoặc sửa bản vẽ điện, chiếu sáng, ổ cắm, công tắc, tủ điện, máng cáp, cấp nước, thoát nước, ống, thiết bị vệ sinh, MEP. Use for electrical, lighting, power, cable tray, plumbing, water supply and drainage drawings.
---

# Vẽ điện nước (MEP) theo chuẩn

> Khung chuẩn mẫu, **số liệu chưa được duyệt theo chuẩn công ty** — người dùng điền chuẩn riêng vào các bảng. Luôn theo đúng nội dung hiện có của file này.

Quy ước: mm, WCS, tỷ lệ in mặc định 1:100. Bản vẽ MEP thường vẽ trên nền kiến trúc có sẵn — **không sửa đối tượng kiến trúc** (layer `A-*`).

## 1. Quy trình chuẩn bị (luôn làm trước khi vẽ)

1. Gọi `get_drawing_info`, `list_layers`, `list_block_defs` để biết nền kiến trúc, layer và block ký hiệu đã có.
2. Tạo trong **một `batch_execute`** các layer còn thiếu theo bảng mục 2.
3. Nếu thiếu block ký hiệu cần dùng (mục 4), vẽ và `define_block` một lần rồi mới chèn.
4. Đọc vị trí tường cần bám tuyến bằng `list_entities` (`layers: ["A-WALL"]`) trước khi tính tọa độ tuyến.
5. Ghi nhớ `count_entities` với `layers: ["0"]` để lúc kiểm tra so trước / sau.
6. **Danh mục trước khi vẽ:** ghi ngắn số thiết bị và tuyến dự kiến theo layer, ví dụ "E-LIGHT: 4 đèn + 1 tuyến dây, E-SWITCH: 1, E-TEXT: 1 mleader". Lúc kiểm tra so lại đúng danh mục này.

## 2. Bảng layer chuẩn

| Layer | Nội dung | Màu | Linetype | Lineweight |
| --- | --- | --- | --- | --- |
| `E-LIGHT` | đèn, dây chiếu sáng | 2 (vàng) | CONTINUOUS | 0.25 |
| `E-POWER` | ổ cắm, dây động lực | 1 (đỏ) | CONTINUOUS | 0.25 |
| `E-SWITCH` | công tắc | 2 | CONTINUOUS | 0.25 |
| `E-PANEL` | tủ / bảng điện | 6 | CONTINUOUS | 0.35 |
| `E-TRAY` | máng / thang cáp | 4 | CONTINUOUS | 0.35 |
| `E-TEXT` | ghi chú điện | 7 | CONTINUOUS | 0.18 |
| `P-SUPL` | ống cấp nước | 5 (xanh dương) | CONTINUOUS | 0.35 |
| `P-HOT` | ống nước nóng | 1 | DASHED | 0.35 |
| `P-DRAIN` | ống thoát nước | 3 (xanh lá) | DASHED | 0.35 |
| `P-VENT` | ống thông hơi | 8 | HIDDEN | 0.25 |
| `P-FIXT` | thiết bị vệ sinh, van, phễu thu | 7 | CONTINUOUS | 0.25 |
| `P-TEXT` | ghi chú cấp thoát nước | 7 | CONTINUOUS | 0.18 |

## 3. Nguyên tắc đi tuyến

- Tuyến ống / dây đi **song song tường**, cách mặt trong tường: dây điện âm tường 50, ống cấp nước 100, ống thoát 150, máng cáp 300 (tính tới tim tuyến). Ưu tiên `offset_entity` từ nét tường trong (đúng khoảng cách, luôn song song), rồi `trim_entities` / `extend_entities` hai đầu tới tuyến hoặc thiết bị cần nối. Chỉ tự tính tọa độ khi không có nét tường để bám.
- Rẽ góc 90°; tránh cắt chéo phòng. Hai tuyến song song cách nhau tối thiểu 100.
- Ống thoát nước có độ dốc: ghi "i = 2%" và mũi tên chiều dốc cạnh tuyến.
- Đường dây từ công tắc tới đèn: polyline, có thể bo cung nhẹ (bulge 0.2) cho dễ đọc.
- **Đầu tuyến phải chạm thiết bị:** đỉnh đầu / cuối của dây, ống lấy đúng **điểm chèn** của block thiết bị (tọa độ trong `positions` đã dùng, hoặc đọc lại bằng `get_entity`), không ước lượng. Ống nhánh nối vào ống chính phải có đỉnh nằm đúng trên ống chính.

## 4. Ký hiệu thiết bị (block)

| Block | Hình vẽ (gốc block tại tâm) | Layer chèn |
| --- | --- | --- |
| `E-DEN` | đèn: vòng tròn R150 + hai đường chéo dài 300 | `E-LIGHT` |
| `E-DEN-ONG` | đèn tuýp: hình chữ nhật 1200x100 | `E-LIGHT` |
| `E-OCAM` | ổ cắm: nửa vòng tròn R120 + đoạn thẳng 240 | `E-POWER` |
| `E-CTAC` | công tắc: vòng tròn R60 (tô SOLID) + đoạn chéo 200 | `E-SWITCH` |
| `E-TU` | tủ điện: chữ nhật 600x200, hatch ANSI31 scale 5 | `E-PANEL` |
| `P-VAN` | van: hai tam giác đối đỉnh 200x150 | `P-FIXT` |
| `P-PTSAN` | phễu thu sàn: vòng tròn R100 + chữ thập | `P-FIXT` |

Mỗi block có attribute `KY_HIEU` (cao 200, invisible = false) nếu cần đánh số (ví dụ "Đ1", "OC2"). Vẽ hình bằng `batch_execute`, rồi `define_block` với `base_point` ở tâm; chèn hàng loạt bằng `insert_block` với `positions`.

## 5. Ghi chú

- Đường kính ống: `add_mleader` chữ "D110", "D90 i=2%", "PPR D25", cao 250 (1:100), layer `P-TEXT`, mũi tên chạm tuyến ống.
- Dây điện: `add_mleader` "CV 2x2.5mm² + E2.5 - PVC D20", layer `E-TEXT`.
- Tên mạch cạnh đèn/ổ cắm: `add_text` cao 200, ví dụ "L1", "P2".
- Bảng thiết bị: `add_table` cột [STT, Ký hiệu, Tên thiết bị, Đơn vị, Số lượng]; đếm số lượng bằng `count_entities` với `types: ["INSERT"]` và lọc layer.

## 6. Ví dụ ngắn: 4 đèn trong phòng 4x6 m (trục [0,0]–[4000,6000])

Sau quy trình chuẩn bị và có block `E-DEN`:

```json
{"tool": "insert_block", "params": {"name": "E-DEN", "layer": "E-LIGHT", "positions": [[1000,1500],[3000,1500],[1000,4500],[3000,4500]]}}
```

Rồi nối dây bằng một `batch_execute` các `draw_polyline` trên `E-LIGHT`, mỗi tuyến đi qua đúng các điểm chèn [1000,1500] → [3000,1500] → [3000,4500] → [1000,4500]; ghi `add_mleader` loại dây. Sau đó làm đủ mục 7 rồi báo cáo theo mục 9.

## 7. Kiểm tra trước khi báo xong (bắt buộc, mỗi lượt có vẽ hoặc sửa)

Ngưỡng khe hở: 10 mm

Kiểm bằng **số đo của công cụ**, không chỉ nhìn ảnh. Ghi lại mục nào ✓ / ✗:

1. **Layer:** `count_entities` với `layers: ["0"]` không tăng so với lúc chuẩn bị. Mọi layer trong `by_layer` của vùng vừa vẽ có trong bảng mục 2. Không có đối tượng mới nào trên layer `A-*` (không sửa nền kiến trúc).
2. **Thuộc tính:** `list_entities` các handle vừa vẽ: linetype và màu theo layer (ống thoát `DASHED`, thông hơi `HIDDEN`), không gán riêng.
3. **Điểm khớp:** `get_entity` các tuyến: đỉnh đầu / cuối trùng đúng điểm chèn thiết bị hoặc nằm đúng trên tuyến chính. Cách nhau từ 0 tới ngưỡng khe hở là **lỗi hở**: sửa bằng `edit_polyline` với tọa độ đọc được.
4. **Hình học:** khoảng cách tuyến tới mặt trong tường đúng mục 3 (`measure_distance`); hai tuyến song song cách nhau ≥ 100; không có tuyến trùng nhau hoặc dài 0; ống thoát có ghi độ dốc.
5. **Chữ và ghi chú:** chiều cao chữ, mleader đúng mục 5 theo tỷ lệ; mũi tên mleader chạm tuyến; `get_bounding_box` các chữ gần nhau không chồng lên nhau và không đè ký hiệu thiết bị.
6. **Thiếu / thừa:** `count_entities` với `types: ["INSERT"]` theo layer, so với danh mục; số trong bảng thiết bị (nếu có) khớp số đếm.
7. **Nhìn lại:** `capture_view` với các handle vừa vẽ.

Gặp lỗi: sửa ngay rồi kiểm lại mục đó. Tối đa **2 vòng** sửa; vẫn còn lỗi thì không nói "xong", mà liệt kê từng lỗi còn lại (handle, lỗi gì, vị trí) để người dùng quyết.

## 8. Lỗi hay gặp (tránh từ đầu)

- Dây / ống dừng cách thiết bị vài mm vì tọa độ ước lượng → lấy đúng điểm chèn block.
- Tuyến tự tính tọa độ không song song tường → `offset_entity` từ nét tường.
- Chèn block thiết bị lên layer `0` hoặc layer của block → luôn ghi `layer` khi `insert_block`.
- Vẽ đè hoặc sửa nhầm đối tượng kiến trúc `A-*` → chỉ đọc nền kiến trúc.
- Đếm thiết bị bằng mắt rồi ghi vào bảng → đếm bằng `count_entities`.
- Chạy lại cả batch khi một lệnh lỗi → thiết bị bị chèn hai lần; chỉ chạy lại các lệnh lỗi.

## 9. Báo cáo cuối lượt

> Đã vẽ: 4 đèn, 1 tuyến dây, 1 ghi chú (E-LIGHT 5, E-TEXT 1) — đúng danh mục.
> Tự kiểm: layer ✓ · điểm khớp ✓ · hình học ✓ · chữ ✓ · thiếu/thừa ✓.
> Gõ U một lần để hoàn tác cả lượt.

Có lỗi chưa sửa được thì thay ✓ bằng ✗ và ghi rõ lỗi ngay bên dưới.
