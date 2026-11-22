---
name: ve-ha-tang
description: Chuẩn vẽ hạ tầng / quy hoạch của người dùng - layer tim đường, mép đường, vỉa hè, bó vỉa, cống, ranh giới; quy ước ghi cao độ và lý trình (Km0+125.50); vẽ tuyến bằng polyline qua điểm tọa độ thực (VN-2000, đổi trục X/Y); bảng tọa độ bằng add_table. Dùng khi được yêu cầu vẽ đường giao thông, tuyến đường, mặt cắt ngang, vỉa hè, cống thoát nước, hố ga, ranh giới lô đất, mốc tọa độ, cao độ, lý trình, quy hoạch. Use for roads, alignments, chainage, levels, drainage networks, plot boundaries and coordinate tables.
---

# Vẽ hạ tầng / quy hoạch theo chuẩn

> Khung chuẩn mẫu, **số liệu chưa được duyệt theo chuẩn công ty** — người dùng điền chuẩn riêng. Luôn theo đúng nội dung hiện có của file này.

Quy ước hệ thống vẫn là **mm**; tọa độ thực tính bằng **mét** phải nhân 1000. Tỷ lệ in mặc định 1:500 cho mặt bằng tuyến.

## 1. Quy trình chuẩn bị (luôn làm trước khi vẽ)

1. Gọi `get_drawing_info` và `list_layers`. Xem `extents` để biết bản vẽ đang dùng tọa độ thực (số rất lớn) hay tọa độ cục bộ.
2. Tạo trong **một `batch_execute`** các layer còn thiếu theo bảng mục 2.
3. **Gốc tọa độ**: tọa độ VN-2000 cỡ hàng trăm nghìn – hàng triệu mét (×1000 thành 10⁸–10⁹ mm). Nếu bản vẽ trắng, hỏi người dùng một câu: vẽ theo tọa độ thực hay trừ một điểm gốc (ví dụ gốc = điểm đầu tuyến)? Nếu dùng gốc cục bộ, ghi một dòng `add_text` ở góc bản vẽ: "Gốc tọa độ: X = …, Y = … (VN-2000)".
4. **Đổi trục**: trong VN-2000, X là hướng Bắc, Y là hướng Đông. Trong AutoCAD: x_cad = Y × 1000, y_cad = X × 1000 (trừ gốc nếu có). Luôn nhắc lại quy ước này trong câu trả lời đầu tiên khi nhận bảng tọa độ.
5. Ghi nhớ `count_entities` với `layers: ["0"]` để lúc kiểm tra so trước / sau.
6. **Danh mục trước khi vẽ:** ghi ngắn số đối tượng dự kiến theo layer, ví dụ "C-ROAD-CL: 1 tim tuyến, C-ROAD-EDGE: 2, C-STATION: 9 vạch + 9 chữ, C-COORD: 5 mốc + 1 bảng". Lúc kiểm tra so lại đúng danh mục này.

## 2. Bảng layer chuẩn

| Layer | Nội dung | Màu | Linetype | Lineweight |
| --- | --- | --- | --- | --- |
| `C-ROAD-CL` | tim đường | 1 (đỏ) | CENTER | 0.25 |
| `C-ROAD-EDGE` | mép mặt đường | 7 | CONTINUOUS | 0.50 |
| `C-CURB` | bó vỉa | 7 | CONTINUOUS | 0.35 |
| `C-SIDEWALK` | mép vỉa hè, hatch vỉa hè | 8 | CONTINUOUS | 0.25 |
| `C-DRAIN` | cống, hố ga | 5 | DASHED | 0.35 |
| `C-BOUND` | ranh giới lô / chỉ giới | 6 | PHANTOM | 0.50 |
| `C-COORD` | mốc tọa độ, bảng tọa độ | 3 | CONTINUOUS | 0.18 |
| `C-STATION` | vạch và chữ lý trình | 2 | CONTINUOUS | 0.18 |
| `C-LEVEL` | ký hiệu và chữ cao độ | 2 | CONTINUOUS | 0.18 |
| `C-TEXT` | ghi chú chung | 7 | CONTINUOUS | 0.18 |

## 3. Tuyến đường

- Tim tuyến: `draw_polyline` qua các điểm tọa độ (đã đổi trục, ×1000), layer `C-ROAD-CL`. Đoạn cong nằm ngang: dùng `bulges` (bulge = tan(góc ở tâm / 4), dấu dương khi cong phải theo chiều đi).
- Mép đường, bó vỉa, vỉa hè: **luôn** `offset_entity` từ tim tuyến về hai phía (không tự tính tọa độ mép, đoạn cong sẽ lệch) theo mặt cắt (ví dụ đường B = 7.5 m: mép đường ±3750, bó vỉa ±3750, mép vỉa hè ±(3750 + bề rộng hè)).
- Vỉa hè: hatch `AR-CONC` scale 50 hoặc `ANSI37` scale 500, layer `C-SIDEWALK`.

## 4. Lý trình

- Ghi dạng `Km0+125.50` (km + mét, 2 chữ số thập phân). Lý trình 0 tại điểm đầu tuyến (hoặc theo người dùng).
- Vạch lý trình mỗi 50 m (cọc chẵn) và tại điểm đầu/cuối, đỉnh cong: đoạn ngắn 2000 vuông góc tim tuyến, layer `C-STATION`; chữ cao 1250 (2.5 mm × 500), xoay song song vạch.
- Tính vị trí: dùng `get_entity` của tim tuyến (vertices) để tính chiều dài cộng dồn; với đoạn thẳng nội suy tuyến tính. Trung điểm vạch lý trình phải nằm **đúng trên** tim tuyến.

## 5. Cao độ

- Ghi `+12.350` (mét, 3 chữ số thập phân, có dấu + / −). Ký hiệu: tam giác đều cạnh 750 đỉnh chạm điểm + đường nằm ngang, chữ cao 1250, layer `C-LEVEL`. Có thể `define_block` `C-CAODO` với attribute `CAO_DO`.
- Cao độ thiết kế và tự nhiên ghi hai dòng: thiết kế trên, tự nhiên dưới (trong ngoặc).

## 6. Bảng tọa độ

`add_table` layer `C-COORD`, title "BẢNG TỌA ĐỘ MỐC", cột: `Điểm | X (m) | Y (m)` (giá trị VN-2000 gốc, 3 chữ số thập phân, **không** đổi trục trong bảng), `col_widths` [3000, 6000, 6000] cho 1:500, text_height 1250. Mỗi mốc trên bản vẽ: `draw_circle` R500 + `add_text` tên mốc cao 1250, layer `C-COORD`.

Ví dụ (gốc cục bộ X0 = 1 234 000, Y0 = 567 000; điểm M1 X = 1 234 012.500, Y = 567 045.250 → x_cad = 45 250, y_cad = 12 500):

```json
{"calls": [
  {"tool": "draw_circle", "params": {"layer": "C-COORD", "center": [45250, 12500], "radius": 500}},
  {"tool": "add_text", "params": {"layer": "C-COORD", "position": [46000, 13000], "text": "M1", "height": 1250}},
  {"tool": "add_table", "params": {"layer": "C-COORD", "position": [60000, 30000], "title": "BẢNG TỌA ĐỘ MỐC", "col_widths": [3000, 6000, 6000], "text_height": 1250,
    "data": [["Điểm", "X (m)", "Y (m)"], ["M1", "1234012.500", "567045.250"]]}}
]}
```

## 7. Kiểm tra trước khi báo xong (bắt buộc, mỗi lượt có vẽ hoặc sửa)

Ngưỡng khe hở: 10 mm

Kiểm bằng **số đo của công cụ**, không chỉ nhìn ảnh. Ghi lại mục nào ✓ / ✗:

1. **Layer:** `count_entities` với `layers: ["0"]` không tăng so với lúc chuẩn bị. Mọi layer trong `by_layer` của vùng vừa vẽ có trong bảng mục 2.
2. **Tọa độ:** `get_entity` vài mốc (đầu, cuối, một điểm giữa): tâm vòng mốc đúng tọa độ đã đổi trục, ×1000, trừ gốc. `measure_distance` giữa hai mốc = khoảng cách tính từ tọa độ gốc (sai lệch < 1 mm). Giá trị trong bảng tọa độ là giá trị VN-2000 gốc, không đổi trục.
3. **Điểm khớp:** tim tuyến là **một** polyline liền (không đứt đoạn); các đoạn mép đường, bó vỉa nối nhau không hở; cống nối đúng tâm hố ga. Cách nhau từ 0 tới ngưỡng khe hở là **lỗi hở**: nối bằng `join_entities` hoặc `edit_polyline`.
4. **Hình học:** `measure_distance` từ tim tới mép đường = nửa bề rộng mặt cắt; vạch lý trình vuông góc tim tuyến, trung điểm nằm trên tim; chiều dài tim tuyến (`list_entities` trả `length`) khớp lý trình cuối.
5. **Chữ:** chiều cao chữ đúng tỷ lệ (1250 cho 1:500); định dạng `Km0+125.50` và `+12.350` đúng số chữ số; `get_bounding_box` chữ lý trình / cao độ không chồng lên nhau.
6. **Thiếu / thừa:** `count_entities` theo layer so với danh mục; số mốc trên bản vẽ = số dòng trong bảng tọa độ; không có đối tượng nằm xa tuyến (dấu hiệu quên trừ gốc hoặc quên đổi trục).
7. **Nhìn lại:** `capture_view` với các handle vừa vẽ.

Gặp lỗi: sửa ngay rồi kiểm lại mục đó. Tối đa **2 vòng** sửa; vẫn còn lỗi thì không nói "xong", mà liệt kê từng lỗi còn lại (handle, lỗi gì, vị trí) để người dùng quyết.

## 8. Lỗi hay gặp (tránh từ đầu)

- Quên đổi trục X/Y hoặc quên ×1000 → tuyến xoay 90° hoặc nhỏ 1000 lần; kiểm bằng khoảng cách hai mốc (mục 7.2).
- Quên trừ gốc cục bộ ở một vài điểm → điểm đó văng rất xa; kiểm bằng đối tượng nằm xa tuyến.
- Tự tính tọa độ mép đường ở đoạn cong → mép không song song tim; luôn dùng `offset_entity`.
- Vẽ tim tuyến thành nhiều đoạn rời → lý trình cộng dồn sai; vẽ một polyline, hoặc `join_entities`.
- Bulge sai dấu → đường cong ngược chiều; so `capture_view` với hướng rẽ của tuyến.
- Ghi lý trình / cao độ sai số chữ số thập phân.

## 9. Báo cáo cuối lượt

> Đã vẽ: tuyến dài 412.35 m, 9 cọc lý trình, 5 mốc + bảng tọa độ (C-ROAD-CL 1, C-ROAD-EDGE 2, C-STATION 18, C-COORD 11) — đúng danh mục.
> Quy ước: gốc X0 = …, Y0 = …, đã đổi trục X/Y.
> Tự kiểm: layer ✓ · tọa độ ✓ · điểm khớp ✓ · hình học ✓ · chữ ✓ · thiếu/thừa ✓.
> Gõ U một lần để hoàn tác cả lượt.

Có lỗi chưa sửa được thì thay ✓ bằng ✗ và ghi rõ lỗi ngay bên dưới.
