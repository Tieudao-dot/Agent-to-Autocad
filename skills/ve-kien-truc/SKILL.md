---
name: ve-kien-truc
description: Chuẩn vẽ kiến trúc của người dùng (bảng layer A-*, tường 220/110, quy trình mặt bằng trục → cột → tường → cửa → kích thước → chữ, chiều cao chữ và dim style theo tỷ lệ in, ký hiệu cửa đi/cửa sổ). Dùng skill này mỗi khi được yêu cầu vẽ hoặc sửa bản vẽ kiến trúc - mặt bằng, phòng, căn hộ, tường, cột, lưới trục, cửa đi, cửa sổ, cầu thang, ghi kích thước kiến trúc, ghi tên phòng/diện tích. Use for any architectural floor-plan task (walls, doors, windows, columns, grid axes, room labels, architectural dimensions).
---

# Vẽ kiến trúc theo chuẩn

> Đây là khung chuẩn mẫu, **số liệu chưa được duyệt theo chuẩn công ty**. Người dùng có thể sửa các bảng bên dưới cho đúng chuẩn công ty — luôn làm theo nội dung hiện có của file này, không dùng giá trị khác khi file đã quy định.

Quy ước chung: đơn vị mm, WCS. Tỷ lệ in mặc định **1:100** (nếu người dùng nói tỷ lệ khác thì nhân lại theo bảng mục 4). Kích thước phòng người dùng nói ("phòng 4x6m") hiểu là **kích thước theo trục tường** trừ khi họ nói "thông thủy".

## 1. Quy trình chuẩn bị (luôn làm trước khi vẽ)

1. Gọi `get_drawing_info` và `list_layers` (thêm `list_styles` nếu sẽ ghi chữ/kích thước) để biết bản vẽ đang có gì.
2. So với bảng layer ở mục 2: tạo **trong một `batch_execute`** mọi layer còn thiếu bằng `create_layer` đúng tên, màu, linetype, lineweight. Không đổi thuộc tính của layer đã có trừ khi người dùng yêu cầu.
3. Nếu chưa có: tạo text style `KT-ARIAL` (`create_text_style` font "Arial", width_factor 0.85) và dim style `KT-100` theo mục 4 (`create_dim_style`), rồi `set_current_styles`.
4. Xác định gốc vẽ: bản vẽ trắng → góc dưới-trái trục đầu tiên tại [0, 0]; bản vẽ đã có đối tượng → đặt bên phải extents hiện có, cách 5000, trừ khi người dùng chỉ vị trí.
5. **Danh mục trước khi vẽ:** ghi ngắn trong câu trả lời số đối tượng dự kiến theo layer, ví dụ "A-WALL: 2 nét tường + 2 mép lỗ, A-DOOR: 1 cánh + 1 cung, A-DIM: 7, A-TEXT: 2". Lúc kiểm tra truyền đúng danh mục này làm `expect` của `verify_drawing`.
6. Sau đó mới vẽ, mỗi nhóm đối tượng một `batch_execute`. Cần handle của bước trước (để `offset_entity`, `trim_entities`) thì lấy từ kết quả `handles` của batch trước, không đoán.

## 2. Bảng layer chuẩn

| Layer | Nội dung | Màu (ACI) | Linetype | Lineweight (mm) |
| --- | --- | --- | --- | --- |
| `A-AXIS` | đường trục | 1 (đỏ) | CENTER | 0.18 |
| `A-AXIS-TAG` | vòng tròn + chữ ký hiệu trục | 1 | CONTINUOUS | 0.25 |
| `A-COL` | cột | 7 | CONTINUOUS | 0.50 |
| `A-WALL` | tường (nét cắt) | 7 | CONTINUOUS | 0.50 |
| `A-WALL-PAT` | hatch tường | 8 | CONTINUOUS | 0.09 |
| `A-DOOR` | cửa đi (cánh + cung mở) | 3 (xanh lá) | CONTINUOUS | 0.25 |
| `A-WIND` | cửa sổ | 4 (cyan) | CONTINUOUS | 0.25 |
| `A-STAIR` | cầu thang | 6 | CONTINUOUS | 0.25 |
| `A-FURN` | nội thất, thiết bị | 8 | CONTINUOUS | 0.18 |
| `A-DIM` | kích thước | 2 (vàng) | CONTINUOUS | 0.18 |
| `A-TEXT` | tên phòng, ghi chú | 7 | CONTINUOUS | 0.25 |
| `A-HIDDEN` | nét khuất | 8 | HIDDEN | 0.18 |

## 3. Tường, cột, trục

- **Tường 220** (tường bao, tường gạch đặc): hai nét cách nhau 220, đối xứng qua trục tường (mỗi bên 110). **Tường 110** (tường ngăn): hai nét cách nhau 110, đối xứng qua trục.
- **Không tự tính tọa độ cả hai nét tường.** Vẽ **một** nét bằng `draw_polyline` kín (`closed: true`, layer `A-WALL`), nét còn lại tạo bằng `offset_entity` đúng bề dày tường (220 hoặc 110) về phía trong. Hai nét luôn song song và khép kín, không hở góc.
- Lỗ cửa: vẽ hai **mép lỗ** bằng `draw_line` ngắn nối thẳng từ nét này sang nét kia (đúng bằng bề dày tường, không vượt ra ngoài), rồi `trim_entities` hai nét tường với `cutting_handles` là hai mép lỗ, `pick_points` nằm trên đoạn tường giữa hai mép. Không vẽ tường thành nhiều polyline rời rồi tự chừa lỗ.
- Tường giao nhau (chữ T, chữ L giữa các phòng): dùng `trim_entities` / `extend_entities` để nét gặp nhau đúng giao điểm, không để nét chồng hay hụt.
- Hatch tường (khi người dùng muốn tô): `draw_hatch` pattern `ANSI31`, scale 15, layer `A-WALL-PAT`, biên là polyline kín của phần tường.
- Cột BTCT: `draw_rectangle` tâm tại giao trục, kích thước mặc định 220x220 (hoặc theo người dùng), layer `A-COL`, hatch `SOLID` màu 8.
- Trục: `draw_line` (không dùng xline để in gọn) vượt ra ngoài công trình 1500 mỗi đầu, layer `A-AXIS`. Ký hiệu trục: vòng tròn đường kính 800 (`draw_circle` radius 400, layer `A-AXIS-TAG`) ở đầu trục, chữ trong vòng cao 350, justify `middle_center`. Trục đứng đánh số 1, 2, 3… từ trái sang; trục ngang đánh chữ A, B, C… từ dưới lên.
- Kích thước trục: một chuỗi `add_dim_linear` + `add_dim_continue` giữa các trục, cộng một dim tổng (`add_dim_baseline` hoặc dim riêng) ở ngoài, layer `A-DIM`.

## 4. Chữ và kích thước theo tỷ lệ in

Chiều cao **trên giấy** cố định; chiều cao **trong model** = chiều cao giấy × mẫu số tỷ lệ.

| Loại | Trên giấy | 1:50 | 1:100 | 1:200 |
| --- | --- | --- | --- | --- |
| Tên phòng | 3.5 mm | 175 | 350 | 700 |
| Diện tích, ghi chú | 2.5 mm | 125 | 250 | 500 |
| Ký hiệu trục (chữ trong vòng) | 3.5 mm | 175 | 350 | 700 |
| Chữ kích thước | 2.5 mm | dim style scale 50 | scale 100 | scale 200 |

Dim style `KT-100` (thay số 100 theo tỷ lệ): `create_dim_style` với `{"name": "KT-100", "scale": 100, "text_height": 2.5, "arrow": "archtick", "arrow_size": 1.2, "extension_offset": 2, "extension_beyond": 1.5, "dim_line_extension": 1, "text_gap": 0.8, "decimals": 0, "text_style": "KT-ARIAL"}`.

- Dim cách mép tường ngoài 800 (đường dim thứ nhất – chi tiết), dim tổng cách thêm 600.
- Ghi đủ **bốn phía** với phòng/căn đơn lẻ; phía có cửa ghi chuỗi chi tiết (mép tường – mép lỗ cửa – mép lỗ cửa – mép tường).

## 5. Ký hiệu cửa

**Cửa đi một cánh** (rộng W, mặc định 900): lỗ cửa rộng W trong tường; cánh cửa là một đoạn thẳng dài W vẽ ở vị trí **mở 90°**, vuông góc với tường, gốc tại bản lề (mép trong của một bên lỗ); cung mở là `draw_arc` tâm tại bản lề, bán kính W, từ đầu cánh tới mép lỗ bên kia (cung 90°). Layer `A-DOOR`. Cửa mở vào trong phòng, bản lề đặt sát góc phòng gần hơn nếu người dùng không nói.

**Cửa sổ** (rộng W): lỗ trong tường như cửa đi; vẽ 3 nét song song trong bề dày tường (hai nét mép tường + một nét giữa), hoặc 4 nét chia đều với tường 220. Layer `A-WIND`.

Khi cùng một loại cửa dùng nhiều lần: vẽ một lần, `define_block` tên `CUA-DI-<W>` / `CUA-SO-<W>` với base point tại bản lề, rồi `insert_block` (xoay theo hướng tường).

## 6. Chữ trong phòng

- Tên phòng (IN HOA, ví dụ "PHÒNG NGỦ") ở tâm phòng, `add_text` justify `middle_center`, cao theo bảng mục 4, layer `A-TEXT`, style `KT-ARIAL`.
- Dòng diện tích ngay dưới, cách 1.5 × chiều cao chữ: `S = 21.85 m²` — diện tích **thông thủy** (đo bằng `measure_area` của polyline nét tường trong, hoặc tính (dài − dày tường) × (rộng − dày tường)), 2 chữ số thập phân.

## 7. Ví dụ hoàn chỉnh: phòng 4x6 m, tường 220, cửa đi 900 ở cạnh dài, ghi kích thước (tỷ lệ 1:100)

Trục tường là hình chữ nhật [0,0]–[4000,6000]; cạnh dài là cạnh đứng; đặt cửa ở cạnh phải (x = 4000), giữa cạnh: lỗ cửa y = 2550…3450. Sau quy trình chuẩn bị (layer, `KT-ARIAL`, `KT-100`, danh mục):

**Bước 1:** nét tường ngoài (kín) và hai mép lỗ cửa. Mép lỗ đi đúng từ nét trong (x = 3890) tới nét ngoài (x = 4110).

```json
{"calls": [
  {"tool": "draw_polyline", "params": {"layer": "A-WALL", "closed": true, "vertices": [[-110,-110],[4110,-110],[4110,6110],[-110,6110]]}},
  {"tool": "draw_line", "params": {"layer": "A-WALL", "start": [3890,2550], "end": [4110,2550]}},
  {"tool": "draw_line", "params": {"layer": "A-WALL", "start": [3890,3450], "end": [4110,3450]}}
]}
```

**Bước 2:** nét tường trong bằng offset 220 về phía trong phòng (H1 = handle polyline ở bước 1).

```json
{"tool": "offset_entity", "params": {"handle": "H1", "distance": 220, "side_point": [2000,3000]}}
```

**Bước 3:** cắt lỗ cửa trên cả hai nét (H2 = nét trong vừa offset, M1 / M2 = hai mép lỗ). Điểm chọn nằm trên đoạn tường giữa hai mép.

```json
{"tool": "trim_entities", "params": {"handles": ["H1","H2"], "cutting_handles": ["M1","M2"], "pick_points": [[4110,3000],[3890,3000]]}}
```

**Bước 4:** cửa, kích thước, chữ. Bản lề là đầu trong của mép lỗ M1 ([3890,2550]); cánh cửa và cung **bắt điểm** vào
đúng đầu đó (`{"snap": "end", "of": "M1", "near": ...}`), không gõ lại tọa độ.

```json
{"calls": [
  {"tool": "draw_line", "params": {"layer": "A-DOOR", "start": {"snap": "end", "of": "M1", "near": [3890,2550]}, "end": {"snap": "end", "of": "M1", "near": [3890,2550], "offset": [-900,0]}}},
  {"tool": "draw_arc",  "params": {"layer": "A-DOOR", "center": {"snap": "end", "of": "M1", "near": [3890,2550]}, "radius": 900, "start_angle": 90, "end_angle": 180}},
  {"tool": "add_dim_linear", "params": {"layer": "A-DIM", "p1": [-110,-110], "p2": [4110,-110], "dim_line_point": [2000,-910]}},
  {"tool": "add_dim_linear", "params": {"layer": "A-DIM", "p1": [-110,6110], "p2": [4110,6110], "dim_line_point": [2000,6910]}},
  {"tool": "add_dim_linear", "params": {"layer": "A-DIM", "p1": [-110,-110], "p2": [-110,6110], "dim_line_point": [-910,3000]}},
  {"tool": "add_dim_linear", "params": {"layer": "A-DIM", "p1": [4110,-110], "p2": [4110,2550], "dim_line_point": [4910,1200]}},
  {"tool": "add_dim_linear", "params": {"layer": "A-DIM", "p1": [4110,2550], "p2": [4110,3450], "dim_line_point": [4910,3000]}},
  {"tool": "add_dim_linear", "params": {"layer": "A-DIM", "p1": [4110,3450], "p2": [4110,6110], "dim_line_point": [4910,4800]}},
  {"tool": "add_dim_linear", "params": {"layer": "A-DIM", "p1": [4110,-110], "p2": [4110,6110], "dim_line_point": [5510,3000]}},
  {"tool": "add_text", "params": {"layer": "A-TEXT", "style": "KT-ARIAL", "position": [2000,3100], "text": "PHÒNG", "height": 350, "justify": "middle_center"}},
  {"tool": "add_text", "params": {"layer": "A-TEXT", "style": "KT-ARIAL", "position": [2000,2550], "text": "S = 21.85 m²", "height": 250, "justify": "middle_center"}}
]}
```

Diện tích lấy từ `measure_area` của nét tường trong **trước** bước 3 (khi còn kín), hoặc tính (4000 − 220) × (6000 − 220) = 21.85 m². Sau đó làm đủ mục 8 rồi mới `zoom_extents` và báo cáo theo mục 10.

## 8. Kiểm tra trước khi báo xong (bắt buộc, mỗi lượt có vẽ hoặc sửa)

Ngưỡng khe hở: 10 mm

Kiểm bằng **số đo của công cụ**, không chỉ nhìn ảnh. Plugin bắt buộc bước này: lượt có vẽ / sửa mà chưa
`verify_drawing` thì chưa được báo xong.

1. **`verify_drawing`** (không cần `handles`), truyền danh mục ở mục 1 làm `expect`, ví dụ
   `{"expect": {"A-WALL": 4, "A-DOOR": 2, "A-DIM": 7, "A-TEXT": 2}}`. Công cụ đo đối tượng của lượt, lỗi an toàn tự sửa (nối khe hở ≤ ngưỡng, đóng polyline,
   xóa bản trùng / dài 0). Ngưỡng khe hở lấy từ dòng trên.
2. **Sửa mọi lỗi còn lại** rồi gọi lại `verify_drawing`:

| Mã | Sửa thế nào |
| --- | --- |
| `GAP` / `OVERSHOOT` | đầu nét hở hoặc thừa: `extend_entities` / `trim_entities`, hoặc vẽ lại đầu nét bằng bắt điểm `{"snap": "end", "of": ...}` |
| `OPEN_POLY` | `edit_polyline` với `closed: true` |
| `ARC_MISMATCH` | vẽ lại cung với tâm / đầu cung bắt `end` vào đúng đối tượng |
| `LAYER_0` / `LAYER_UNKNOWN` | `set_entity_props` sang layer đúng của bảng mục 2 |
| `PROP_OVERRIDE` | `set_entity_props` với `color` / `linetype` / `lineweight` = `"bylayer"` |
| `TEXT_HEIGHT` | `edit_text` về chiều cao theo bảng chữ × tỷ lệ in |
| `TEXT_OVERLAP` | `move_entities` chữ / dim cho khỏi chồng |
| `STRAY` / `COUNT` | xem lại tọa độ / danh mục: vẽ thêm phần thiếu, xóa phần thừa |
| `DIM_STYLE` | dùng dim style `KT-<tỷ lệ>` của mục 4 (`create_dim_style` rồi `set_current_styles`), vẽ lại dim |

3. **Kiểm thêm phần máy không đo được:**
   - `measure_distance` giữa hai mép lỗ = bề rộng cửa; giữa hai nét tường = 220 / 110.
   - Đủ dim bốn phía theo mục 4; cửa nằm đúng tường người dùng nói.
4. **Nhìn lại:** `capture_view` với các handle vừa vẽ.

Tối đa **2 vòng** sửa; vẫn còn lỗi thì không nói "xong", mà liệt kê từng lỗi còn lại (handle, lỗi gì, vị trí) để
người dùng quyết.

## 9. Lỗi hay gặp (tránh từ đầu)

- Tự tính tọa độ hai nét tường rồi lệch 1 nét ở góc → dùng `offset_entity` (mục 3).
- Cung cửa vẽ theo tâm tính nhẩm, đầu cung không chạm mép lỗ → tâm cung **bắt `end`** vào điểm bản lề, bán kính **bằng** bề rộng cửa.
- Mép lỗ vượt ra ngoài nét tường, hoặc ngắn hơn bề dày tường → mép lỗ đi đúng từ nét trong tới nét ngoài.
- Vẽ lên layer `0` vì quên tham số `layer` → mọi lệnh vẽ đều ghi `layer` theo bảng mục 2.
- Chữ cao theo tỷ lệ 1:100 trong khi người dùng chọn 1:50 → đọc tỷ lệ từ ngữ cảnh mỗi lần, tra bảng mục 4.
- Vẽ cùng một nét hai lần khi chạy lại batch lỗi → batch lỗi chỉ chạy lại **các lệnh lỗi**, không chạy lại cả batch.
- Dim tổng và dim chi tiết cùng một khoảng cách, chồng lên nhau → dim chi tiết cách tường 800, dim tổng cách thêm 600.

## 10. Báo cáo cuối lượt

Một đoạn ngắn theo mẫu:

> Đã vẽ: 15 đối tượng (A-WALL 4, A-DOOR 2, A-DIM 7, A-TEXT 2) — đúng danh mục.
> Tự kiểm: layer ✓ · điểm khớp ✓ · hình học ✓ · chữ/dim ✓ · thiếu/thừa ✓.
> Gõ U một lần để hoàn tác cả lượt.

Có lỗi chưa sửa được thì thay ✓ bằng ✗ và ghi rõ lỗi ngay bên dưới.
