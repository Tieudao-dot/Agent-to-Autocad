# Nghiên cứu Skill: ràng buộc AI vẽ chuẩn, tự kiểm và sửa lỗi trước khi báo xong

Thread "ClaudeCAD — nghiên cứu Skill", 22/11/2026, dựa trên bản v0.9.5 (`main`). Tài liệu này chỉ đề xuất:
chưa sửa code, chưa build. Phần cần code chuyển cho thread phù hợp sau khi chủ dự án duyệt.

## 1. Vấn đề chủ dự án nêu

Khi dùng thật, AI vẽ xong phần cơ bản là báo xong, không tự kiểm lại. Các lỗi gặp phải (chủ dự án chọn đủ 5 nhóm):

1. Vẽ không bắt đúng điểm, các điểm không khớp (hở góc, đầu nét không chạm).
2. Sai layer / thuộc tính (layer `0`, sai tên, màu, nét, lineweight).
3. Chữ / dim sai (chiều cao theo tỷ lệ, chữ đè nhau, dim lệch, thiếu phía).
4. Hình học sai (lỗ cửa sai bề rộng, cánh / cung cửa lệch, đường trùng).
5. Thiếu / thừa đối tượng, đối tượng lạc xa, sai vị trí so với yêu cầu.

Mục tiêu: **ràng buộc AI vẽ đúng ngay từ đầu, và bắt buộc tự kiểm, tự sửa trước khi báo xong**.

## 2. Hiện trạng

### 2.1. Ba skill mẫu

- `ve-kien-truc` (105 dòng), `ve-mep` (72 dòng), `ve-ha-tang` (68 dòng). Tạo ngày 05/11/2026 (commit `3c71e0e`,
  "Phase 1e"), đến nay chưa sửa lần nào.
- Căn cứ: `docs/spec.md` mục 8 yêu cầu "khung để người dùng điền chuẩn riêng, kèm ví dụ hoàn chỉnh". Số liệu (màu,
  lineweight, chữ 3.5 / 2.5 mm, dim style, vòng trục Ø800…) do AI điền theo thông lệ chung. Skill không trích TCVN
  hay chuẩn công ty nào, và chưa có người làm nghề duyệt. Tên layer kiểu `A-WALL`, `E-LIGHT` giống lối đặt tên
  AIA / NCS của Mỹ (suy ra từ tên, file không ghi).
- `ve-kien-truc` có mục "8. Kiểm tra trước khi báo xong", nhưng chỉ là 3 câu nhắc ("đúng layer", "đúng chiều
  cao", "lỗ cửa đúng bề rộng") và **không nói kiểm bằng công cụ nào**.

### 2.2. Trợ lý và công cụ

- System prompt (`agent/src/agent/systemPrompt.ts`): chỉ dặn "sau việc vẽ lớn thì *nhìn một lần* bằng
  `capture_view`", và "không đọc tọa độ từ ảnh". Không có quy tắc bắt buộc kiểm bằng số đo.
- Phiên agent (`agent/src/agent/session.ts`): **không có hook nào**. Không có gì ngăn Claude kết thúc lượt khi
  chưa kiểm.
- **Không có bắt điểm (object snap).** Mọi tham số điểm là `[x, y]` số thuần (`agent/src/tools/common.ts`,
  `point`). Claude tự cộng trừ tọa độ trong đầu → nguyên nhân chính của lỗi nhóm 1 và 4.
- Công cụ đọc đã có, dùng được cho kiểm tra: `list_entities`, `get_entity`, `count_entities`, `find_text`,
  `get_bounding_box`, `measure_distance`, `measure_area`, `list_layers`, `list_styles`, `capture_view`.
- Công cụ sửa giúp tránh tự tính tọa độ: `offset_entity`, `trim_entities`, `extend_entities`, `join_entities`,
  `fillet_entities`. Skill hiện không hướng Claude dùng chúng (ví dụ mẫu tự tính cả hai nét tường).
- `batch_execute` trả về `handles` của mọi đối tượng tạo / sửa: có sẵn danh sách "đối tượng vừa vẽ" để kiểm.

## 3. Đề xuất: 4 lớp ràng buộc

Lớp A và B **phòng lỗi lúc vẽ**, lớp C **bắt lỗi sau khi vẽ**, lớp D **bắt buộc** chạy C và sửa xong mới được
báo. Mỗi lớp làm riêng được, nhưng hiệu quả nhất khi có đủ.

### Lớp A. Bắt điểm theo đối tượng (giải quyết nhóm 1, 4)

Cho mọi tham số điểm nhận thêm dạng **tham chiếu**, plugin tự tính như OSNAP:

```json
{"tool": "draw_line", "params": {"layer": "A-DOOR",
  "start": {"snap": "end", "of": "2F1A", "near": [3890, 2550]},
  "end":   {"snap": "per", "of": "2F1C", "from": "start"}}}
```

- Kiểu bắt: `end` (điểm cuối), `mid`, `cen` (tâm), `int` (giao, `of: [h1, h2]`), `per` (chân vuông góc),
  `nea` (điểm gần nhất trên đối tượng), `ins` (điểm chèn block / chữ). Có thể cộng thêm `offset: [dx, dy]`.
- `near` chọn điểm nào khi đối tượng có nhiều điểm cùng loại (hai đầu đường).
- Tham chiếu tới đối tượng **vừa tạo trong cùng batch**: `"of": "@3"` = kết quả lệnh thứ 3 của batch.
- Tool mới `get_snap_points(handles)`: trả các điểm đặc trưng (end, mid, cen, giao giữa các đối tượng đã cho), để
  Claude lấy số chính xác khi cần.
- Đổi skill: vẽ nét tường ngoài một lần (polyline kín), nét trong bằng `offset_entity`. Mở lỗ cửa bằng
  `trim_entities` hoặc `edit_polyline`, không tự tính hai polyline. Cánh và cung cửa bắt `end` vào mép lỗ.

Nơi làm: schema `point` trong `agent/src/tools/common.ts` (thêm dạng tham chiếu) + bộ giải điểm phía plugin
(`plugin/ClaudeCAD.Plugin/Drawing/`, dùng `GetClosestPointTo`, `IntersectWith`, `GetPointAtParameter`…).

### Lớp B. Công cụ vẽ tự áp chuẩn (giải quyết nhóm 2, 3)

- Từ chối vẽ lên layer `0` và layer không có trong chuẩn của skill đang dùng (lỗi `LAYER_NOT_IN_STANDARD`, gợi ý
  layer đúng). Layer chuẩn còn thiếu thì tự tạo đúng màu / nét / lineweight.
- Chiều cao chữ: nếu Claude không ghi, công cụ tự tính theo loại chữ × tỷ lệ trên thanh ngữ cảnh.
- Cần chuẩn ở **dạng máy đọc được**. Hai cách (đã chốt cách 2, xem mục 6):
  1. File `standard.json` cạnh `SKILL.md`: layer (tên, màu, nét, lineweight), bảng chiều cao chữ trên giấy, dim
     style, sai số cho phép. Skill vẫn giữ bảng chữ cho người đọc.
  2. Plugin đọc thẳng bảng Markdown "Bảng layer chuẩn" trong `SKILL.md`. Không thêm file, nhưng dễ vỡ khi người
     dùng sửa bảng sai định dạng.

### Lớp C. Công cụ tự kiểm `verify_drawing` (giải quyết cả 5 nhóm)

Tool mới, chạy phía plugin, **đo bằng máy**, không nhờ AI nhìn. Đầu vào: `handles` vừa vẽ / sửa (mặc định: mọi
handle của lượt này), tùy chọn `expect` là danh mục dự kiến (xem dưới). Đầu ra: danh sách lỗi, mỗi lỗi có mã,
handle, mô tả, cách sửa gợi ý, và có `auto_fix` hay không.

| Mã lỗi | Dò gì | Nhóm |
| --- | --- | --- |
| `GAP` | Hai đầu nét cách nhau trong khoảng (0, ngưỡng], mặc định 10 mm (đã chốt), tự nối: gần như chạm mà không chạm | 1 |
| `OVERSHOOT` | Nét vượt quá giao điểm một đoạn ngắn (< ngưỡng) | 1, 4 |
| `OPEN_POLY` | Polyline có điểm đầu ≈ điểm cuối nhưng không `closed` | 1, 4 |
| `LAYER_0` / `LAYER_UNKNOWN` | Đối tượng trên layer `0` hoặc ngoài bảng chuẩn | 2 |
| `PROP_OVERRIDE` | Màu / nét / lineweight gán riêng khác ByLayer khi chuẩn không cho | 2 |
| `TEXT_HEIGHT` | Chiều cao chữ không khớp bảng chuẩn × tỷ lệ | 3 |
| `TEXT_OVERLAP` | Hộp bao chữ / dim chồng lên nhau hoặc đè lên ký hiệu | 3 |
| `DIM_STYLE` | Dim không dùng dim style chuẩn của tỷ lệ | 3 |
| `ZERO_LEN` / `DUPLICATE` | Đường dài 0, hai đối tượng trùng hoàn toàn | 4 |
| `ARC_MISMATCH` | Đầu cung không trùng đầu một đối tượng khác (cung cửa không chạm mép lỗ) | 4 |
| `STRAY` | Đối tượng nằm xa cụm chính (> 3 lần kích thước cụm) | 5 |
| `COUNT` | Số đối tượng theo layer khác danh mục `expect` | 5 |

- **Danh mục dự kiến** (`expect`): trước khi vẽ, Claude ghi đếm theo layer, ví dụ
  `{"A-WALL": 4, "A-DOOR": 2, "A-DIM": 7, "A-TEXT": 2}`. `verify_drawing` so với thực tế để bắt thiếu / thừa.
- `auto_fix: true` cho lỗi an toàn: nối `GAP` (dời điểm cuối về điểm kia), đóng `OPEN_POLY`, xóa `DUPLICATE` /
  `ZERO_LEN`, đổi layer `0` sang layer gợi ý. Mỗi lần sửa nằm trong cùng undo group của lượt.
- Ảnh `capture_view` vẫn dùng **sau** `verify_drawing` cho các lỗi máy không dò được (bố cục, ý đồ).

### Lớp D. Bắt buộc kiểm trước khi báo xong (hook)

- Agent SDK có hook chạy khi Claude định kết thúc lượt (`Stop`) và sau mỗi lần gọi tool (`PostToolUse`). Cú pháp
  cụ thể của bản `@anthropic-ai/claude-agent-sdk` đang dùng cần tra lại khi làm (spec dặn dùng skill `claude-api`).
- Cơ chế:
  1. `PostToolUse` ghi lại: lượt này có gọi tool ghi (vẽ / sửa / xóa) không, đã gọi `verify_drawing` sau lần ghi
     cuối chưa, kết quả còn lỗi không.
  2. `Stop`: nếu có ghi mà chưa verify, hoặc verify còn lỗi chưa xử lý → **chặn kết thúc**, gửi lại cho Claude:
     "Chạy verify_drawing cho các đối tượng vừa vẽ và sửa lỗi trước khi báo xong."
  3. Đã chốt **bắt buộc sửa hết**: mọi mã lỗi (kể cả chữ đè, thiếu / thừa đối tượng) đều chặn kết thúc. Giới hạn
     tối đa 2 vòng sửa. Vòng thứ 3 vẫn còn lỗi thì cho kết thúc, nhưng câu trả lời phải liệt kê lỗi còn lại (system
     prompt bắt buộc), tránh vòng lặp vô tận và tốn token.
  4. Bỏ qua khi lượt chỉ đọc, ở chế độ Chỉ xem, hoặc khi người dùng bấm Dừng.
- Khung chat: thẻ nhỏ cuối lượt "Tự kiểm: 12 ✓ · 1 ✗ (khe hở 3 mm ở góc trục A-1)", bấm vào thì chọn đối tượng
  lỗi trong bản vẽ.

### Lớp E. Viết lại skill (làm được ngay, không cần code)

Áp cho cả 3 skill mẫu và hướng dẫn viết skill trong `skills/README.md`:

- Mục **"Kiểm tra trước khi báo xong"** ghi lệnh cụ thể. Trước khi có lớp C: `count_entities` layer `0` = 0,
  `find_text` soát chiều cao, `measure_distance` đo lỗ cửa, `get_snap_points` / `get_entity` so đầu nét, cuối cùng
  `capture_view`. Có lớp C rồi: một dòng `verify_drawing` + cách xử lý từng mã lỗi.
- Mục **"Danh mục trước khi vẽ"**: bắt Claude nêu số đối tượng dự kiến theo layer.
- **Ví dụ mẫu** viết lại theo kiểu bắt điểm và `offset_entity`, không tính tay hai nét tường.
- Mục **"Lỗi hay gặp"**: liệt kê cụ thể, ví dụ cung cửa phải bắt `end` vào mép lỗ, không vẽ hai nét trùng ở góc.
- Báo cáo cuối lượt theo mẫu cố định: số đối tượng, layer, "Tự kiểm: ✓/✗".
- Thay số liệu mẫu bằng chuẩn thật của công ty / TCVN khi chủ dự án cung cấp (việc riêng, xem mục 6).

## 4. Công sức ước tính

| Phần | Ở đâu | Ước tính | Thread gợi ý |
| --- | --- | --- | --- |
| E. Viết lại 3 skill + README (bản chưa có tool mới) | `skills/` | 0,5–1 ngày + thử thật | Thread này (đã chốt, đang làm) |
| A. Tham chiếu điểm + `get_snap_points` | `agent/src/tools`, `plugin/.../Drawing` | 2–3 ngày, kèm test | Thread "Tự kiểm trước khi báo xong" (`session_01D56HmJy4Urvum4vU5EmdwS`, nhánh `claude/tu-kiem-tra`) |
| C. `verify_drawing` (12 mã lỗi, auto_fix) | plugin `Drawing/` + tool phía agent | 3–4 ngày, kèm test | như trên |
| B. Áp chuẩn lúc vẽ (đọc bảng trong `SKILL.md`) | plugin + `SkillStore.cs` | 1,5–2 ngày | như trên |
| D. Hook Stop / PostToolUse + quy tắc prompt | `agent/src/agent/session.ts`, `systemPrompt.ts` | 0,5–1 ngày | như trên |
| Thẻ "Tự kiểm" trong khung chat | `plugin/.../UI` | 0,5 ngày | Giao diện & UX |
| Checklist thử thật cho các lỗi trên | `docs/checklist-thu-that.md` | 0,5 ngày | Kiểm thử |

Tổng khoảng 8–12 ngày công. Thứ tự đề xuất theo hiệu quả giảm lỗi vặt: **E** (rẻ, làm ngay) → **A** + dò `GAP`
→ **C** + **D** → **B** → thẻ UI.

## 5. Rủi ro

- **Tốn token và thời gian hơn:** mỗi lượt vẽ thêm 1–3 lần gọi kiểm và có thể thêm vòng sửa. Giảm bằng cách chỉ
  kiểm handle của lượt, trả kết quả gọn, giới hạn 2 vòng.
- **Báo lỗi sai:** ví dụ khe hở cố ý (lỗ cửa), chữ đè cố ý. Cần ngưỡng hợp lý, loại trừ theo layer (lỗ cửa không
  tính `GAP`), và cho skill tắt từng mã lỗi.
- **Hook chặn sai làm kẹt lượt:** phải có giới hạn vòng và đường thoát khi người dùng bấm Dừng.
- **Đổi schema điểm:** phải giữ dạng `[x, y]` cũ chạy bình thường. Macro và mẫu lệnh đã lưu không được hỏng.
- **Chuẩn dạng dữ liệu** (lớp B) chỉ đúng khi số liệu chuẩn đúng. Skill mẫu hiện chưa ai duyệt, xem mục 2.1.
- **API hook của SDK** thay đổi nhanh. Phải tra bản đang dùng (`^0.3.318`) trước khi làm.
- Hiệu năng `verify_drawing` trên bản vẽ lớn: chỉ xét handle của lượt và vùng lân cận, không quét cả bản vẽ.

## 6. Quyết định của chủ dự án (22/11/2026)

| # | Câu hỏi | Đã chốt |
| --- | --- | --- |
| 1 | Thứ tự làm | Theo đề xuất: E (viết lại skill) → A (bắt điểm) → C + D (`verify_drawing` + hook) → B (tự áp chuẩn) → thẻ UI |
| 2 | Ai làm phần code (A, B, C, D) | Chủ dự án chọn thread **"Tự kiểm trước khi báo xong"** (`session_01D56HmJy4Urvum4vU5EmdwS`, nhánh `claude/tu-kiem-tra`), đã có sẵn `verify_drawing` 10 mã + hook Stop + thẻ Tự kiểm |
| 3 | Mức chặt khi kiểm ra lỗi | **Bắt buộc sửa hết** mọi mã lỗi, tối đa 2 vòng; còn lỗi thì liệt kê cho người dùng |
| 4 | Chuẩn dạng máy đọc | **Đọc bảng trong `SKILL.md`** (không thêm `standard.json`); plugin báo ngay khi thêm skill có bảng sai định dạng |
| 5 | Khe hở | Ngưỡng **10 mm** trong model, **tự nối** (`auto_fix`), skill sửa được ngưỡng; gõ U hoàn tác được |
| 6 | Số liệu chuẩn | Chủ dự án **gửi chuẩn công ty**; khi có thì thay số liệu mẫu trong 3 skill |
| 7 | Viết lại 3 skill mẫu | Thread này **làm luôn phần cấu trúc** (lớp E), giữ số liệu cũ đến khi có chuẩn công ty |

Còn chờ: tài liệu chuẩn công ty (câu 6).

### Ghi chú cho thread làm phần code

- Bảng layer trong `SKILL.md` phải giữ định dạng cố định để máy đọc: tiêu đề mục chứa "Bảng layer chuẩn", cột
  `Layer | Nội dung | Màu | Linetype | Lineweight`, tên layer trong dấu `` ` ``. Bảng chiều cao chữ: mục chứa
  "Chữ và kích thước theo tỷ lệ in", cột "Trên giấy" tính bằng mm. 3 skill mẫu đã viết theo định dạng này.
- Ngưỡng khe hở có thể ghi đè trong skill bằng dòng `Ngưỡng khe hở: <số> mm` ở mục "Kiểm tra trước khi báo xong".
- Khi `verify_drawing` và bắt điểm có rồi, cập nhật lại mục kiểm tra của 3 skill (thay chuỗi lệnh đo thủ công bằng
  một lệnh `verify_drawing`) và test `agent/test/skills.test.ts` (chỉ cho nhắc tool có thật).

## 7. Lớp E đã làm (22/11/2026)

- 3 skill mẫu viết lại phần cấu trúc, **giữ nguyên số liệu cũ**: thêm "Danh mục trước khi vẽ", ghi nhớ số đối
  tượng layer `0`, cách dựng hình bằng `offset_entity` / `trim_entities` / `extend_entities`, mục "Kiểm tra trước
  khi báo xong" bằng lệnh đo có sẵn (dòng `Ngưỡng khe hở: 10 mm`, tối đa 2 vòng sửa), "Lỗi hay gặp", "Báo cáo
  cuối lượt" có dòng "Tự kiểm". Ví dụ kiến trúc viết lại theo 4 bước: polyline kín → offset → trim lỗ cửa → cửa, dim,
  chữ. Đầu mỗi skill ghi rõ "số liệu chưa được duyệt theo chuẩn công ty".
- `skills/README.md` có mục "Viết skill để Claude vẽ đúng và tự kiểm lại"; `docs/huong-dan-su-dung.md` cập nhật theo.
- Đã soát bằng script làm đúng các kiểm tra của `agent/test/skills.test.ts` (frontmatter, mục chuẩn bị, chỉ nhắc
  tool có thật, JSON hợp lệ): cả 3 skill đạt. Chưa chạy `npm test` và chưa thử trên AutoCAD thật.
- **Lưu ý phát hành:** bộ cài chép skill với cờ `onlyifdoesntexist` (`installer/*.iss`), nên máy đã cài sẽ **không
  nhận** bản skill mẫu mới khi cập nhật, chỉ máy cài mới mới có. Thread gộp nhánh & phát hành đã xử lý theo cách chủ dự án
  chọn, "đè nếu chưa sửa" (`plugin/ClaudeCAD.Plugin/Core/SampleSkills.cs`, nhánh `claude/release-0.9.5` @ `5838cf1`):
  file mẫu chưa bị người dùng sửa sẽ được thay bằng bản mới khi khởi động.
- `bo-cai/README.md` chưa sửa: thay đổi chưa vào phiên bản nào, dòng lịch sử phiên bản do thread phát hành ghi khi
  gộp.
- Khi có chuẩn công ty: thay số trong các bảng mục 2, 4 (và mục ký hiệu) của từng skill, giữ nguyên định dạng bảng.

## 8. Nhãn skill Admin / Default / You: khách sửa skill (22/11/2026)

Nhãn có từ tính năng đẩy skill theo máy (`claude/day-skill-theo-may`, đã gộp vào `claude/release-0.9.5`, chưa vào
`main`). Chủ dự án chốt cách xử lý khi khách sửa skill:

1. Áp dụng cho **cả Default và Admin**: sửa thì sinh **một skill mới nhãn You**, bản gốc giữ nguyên và vẫn nhận cập
   nhật / Ẩn / Xóa. Thay quy tắc cũ "sửa Admin thì thành You".
2. Cách tạo: **chép trước rồi mở**. Bấm Sửa thì plugin chép sang `<tên>-sua` (nhãn You) rồi mở bản chép, bản gốc
   không bị ghi.
3. Đoạn chat hiện tại đang dùng bản gốc thì **tự đổi sang bản You**; đoạn chat khác giữ nguyên.
4. Bản khách sửa **không báo về chủ plugin**. Hiện đã đúng, giữ nguyên.

Đã chuyển cho thread "đẩy skill theo máy" (`session_01D56HmERgjnrwP5MTbkGKro`) làm.
