# Kế hoạch: tự kiểm trên bản vẽ trước khi báo xong

Thread "ClaudeCAD — tự kiểm trước khi báo xong", nhánh `claude/tu-kiem-tra`, 22/11/2026. **Đã code xong đợt đầu**
(mục 8). Dựa trên
`docs/nghien-cuu-skill.md` (lớp C, D và thẻ "Tự kiểm"). Chủ dự án đã trả lời các câu ở mục 7; chờ duyệt kế hoạch rồi mới code.

Phạm vi thread: lớp **kiểm** (`verify_drawing`), **hook** bắt buộc kiểm, **thẻ Tự kiểm**. Không thuộc thread này:
bắt điểm `get_snap_points` (lớp A), áp chuẩn lúc vẽ / `standard.json` (lớp B), viết lại skill (lớp E).

## 1. Luồng một lượt

```
Người dùng gửi việc
  → Claude vẽ / sửa (các tool ghi; trợ lý ghi lại handle tạo / sửa của lượt — đã có sẵn trong session.ts)
  → Claude định kết thúc lượt
  → hook Stop:
       lượt không ghi gì, Chỉ xem, hoặc người dùng bấm Dừng       → cho kết thúc, không thẻ
       có ghi mà chưa gọi verify_drawing sau lần ghi cuối         → chặn, nhắc "chạy verify_drawing"
       verify còn lỗi, chưa quá số vòng                           → chặn, gửi danh sách lỗi + cách sửa
       verify sạch, hoặc đã hết số vòng                           → cho kết thúc
  → Claude sửa (auto_fix hoặc tool sửa thường) → verify lại → …
  → thẻ "Tự kiểm" cuối lượt: "Tự kiểm: 24 đối tượng · 0 lỗi ✓" hoặc
    "Tự kiểm: 24 đối tượng · 2 lỗi ✗ — khe hở 3 mm (A-WALL)…", nút "Chọn đối tượng lỗi"
  → câu trả lời của Claude: còn lỗi thì phải liệt kê (system prompt bắt buộc)
```

Chi tiết:

- **Mặc định kiểm gì:** handle tạo / sửa của lượt (`createdThisTurn` ∪ `touchedThisTurn` trong `session.ts`).
  Claude không cần truyền `handles`; trợ lý tự điền. Lân cận: đối tượng khác trong hộp bao của nhóm đó, nới thêm
  ngưỡng, để bắt khe hở với nét có sẵn. Không quét cả bản vẽ.
- **Đếm vòng:** mỗi lần hook chặn là một vòng. Hết vòng thì cho kết thúc; thẻ ghi "còn N lỗi".
- **Bấm Dừng:** hook không chặn (`cancelRequested`), không thẻ.
- **Macro / mẫu lệnh chạy lại:** không qua Claude nên không có hook; ngoài phạm vi đợt đầu.

## 2. Lỗi kiểm ở đợt đầu (10 / 12 mã)

Chỉ những mã **không cần chuẩn dạng dữ liệu** (lớp B). Bốn mã cần chuẩn (`LAYER_UNKNOWN`, `PROP_OVERRIDE`,
`TEXT_HEIGHT`, `DIM_STYLE`) làm sau khi có `standard.json`.

| Mã | Dò gì | `auto_fix` |
| --- | --- | --- |
| `GAP` | Hai đầu nét cách nhau trong (dung sai, ngưỡng]: gần chạm mà không chạm | Có: dời đầu nét của lượt này về đầu kia |
| `OVERSHOOT` | Đầu nét vượt qua nét khác một đoạn trong (dung sai, ngưỡng] | Không (cần trim, Claude tự làm) |
| `OPEN_POLY` | Polyline đầu ≈ cuối (≤ ngưỡng) mà không `closed` | Có: bỏ đỉnh cuối trùng, đặt `closed` |
| `ZERO_LEN` | Line / polyline / cung dài ≤ dung sai | Có: xóa |
| `DUPLICATE` | Hai đối tượng cùng loại, cùng layer, cùng hình (trong dung sai) | Có: xóa bản của lượt này |
| `ARC_MISMATCH` | Đầu cung không trùng đầu / thân nét nào (cung cửa không chạm mép lỗ) | Không |
| `LAYER_0` | Đối tượng trên layer `0` hoặc `Defpoints` | Không đợt đầu (chưa có layer gợi ý; có lớp B thì có) |
| `TEXT_OVERLAP` | Hộp bao chữ / dim chồng nhau | Không |
| `STRAY` | Đối tượng của lượt nằm xa cụm chính > 3 lần kích thước cụm | Không |
| `COUNT` | Số đối tượng theo layer khác `expect` Claude khai trước khi vẽ (chỉ khi có `expect`) | Không |

- **Dung sai trùng điểm:** 0,01 mm (cố định). **Ngưỡng khe hở:** 10 mm (cậu chọn 22/11).
- **Loại trừ báo nhầm:** ngưỡng 10 mm nhỏ hơn mọi lỗ cửa nên không cần loại trừ theo layer. Mã nào cố ý thì Claude
  gọi lại với `ignore: ["MÃ"]` và nói lý do; skill muốn tắt mã nào thì ghi câu dặn đó trong `SKILL.md`.
- **Kết quả trả về gọn:** `{checked, issues:[{code, handles, message, fix, auto_fixable}], fixed:[…]}`, tối đa
  30 lỗi, phần thừa gộp "và N lỗi cùng loại".

## 3. File sẽ sửa / thêm

| File | Việc |
| --- | --- |
| `plugin/ClaudeCAD.Plugin/Core/VerifyRules.cs` (mới) | Luật kiểm thuần hình học (không phụ thuộc AutoCAD): đầu vào là bản ghi đơn giản (loại, layer, đầu nét, đỉnh, cung, hộp bao chữ), đầu ra là danh sách lỗi. Test được trên Linux. |
| `plugin/ClaudeCAD.Plugin/Drawing/VerifyTools.cs` (mới) | Tool `verify_drawing`: đọc entity theo handle + lân cận → bản ghi → `VerifyRules`; `auto_fix` trong cùng undo group của lượt. |
| `plugin/ClaudeCAD.Plugin/Drawing/ToolDispatcher.cs` | Đăng ký tool; `verify_drawing` là đọc, thành ghi khi `auto_fix: true` (giống cách `batch_execute` xét `BatchWrites`); Chỉ xem thì từ chối `auto_fix`. |
| `agent/src/tools/verify.ts` (mới) + `index.ts`, `content.ts` | Schema tool cho Claude, tóm tắt kết quả cho danh sách bước. Không cần truyền `handles`: plugin tự biết đối tượng của lượt. |
| `agent/src/agent/session.ts` | `hooks.Stop` trong `buildOptions`; theo dõi "ghi sau lần verify cuối", số vòng, kết quả verify cuối (mở rộng `noteResult`). |
| `agent/src/agent/selfCheck.ts` (mới) | Logic quyết định của hook (hàm thuần, dễ test): chặn / cho qua, nội dung nhắc Claude. |
| `agent/src/agent/systemPrompt.ts` | Quy tắc: khai `expect` trước khi vẽ, kiểm bằng `verify_drawing` rồi mới `capture_view`, còn lỗi phải liệt kê. |
| `plugin/ClaudeCAD.Plugin/ChatController.cs` | `TurnChanges()` (đối tượng lượt đã thêm / sửa, từ bảng Thay đổi), giữ kết quả kiểm cuối của lượt, gắn vào câu trả lời khi lượt xong. Không cần tin IPC mới. |
| `plugin/ClaudeCAD.Plugin/Core/ChatStore.cs` | Lưu thẻ Tự kiểm vào lịch sử chat (`self_check`) và file Xuất. |
| `plugin/ClaudeCAD.Plugin/UI/ChatControl.cs` | `AddSelfCheckCard` (✓ xanh / ✗ đỏ, danh sách rút gọn, nút "Chọn đối tượng lỗi" dùng lại cơ chế chọn handle của thẻ xác nhận). |
| `docs/checklist-thu-that.md`, `docs/tong-quan-du-an.md`, `bo-cai/README.md`, `docs/spec.md` | Tài liệu đi kèm. |

Thẻ là phần giao diện: tớ code bản tối thiểu theo kiểu thẻ hiện có. Nếu cậu muốn thread Giao diện & UX làm thẻ,
tớ chỉ làm tin `self_check` và chuyển phần thẻ sang đó (mục 7).

## 4. Test tự động

- **Lõi C#** (`plugin/ClaudeCAD.Core.Tests/VerifyRulesTests.cs`, chạy Linux): mỗi mã ít nhất 1 ca bắt đúng + 1 ca
  không báo nhầm (khe hở 0,005 mm = chạm; khe hở 60 mm > ngưỡng = cố ý; lỗ cửa trên layer `A-DOOR`; polyline đã
  đóng; hai đường song song không trùng); `auto_fix` cho ra hình đúng; giới hạn 30 lỗi.
- **Trợ lý** (`agent/test/selfCheck.test.ts`, vitest): hook cho qua khi lượt chỉ đọc / Chỉ xem / Dừng; chặn khi ghi
  mà chưa verify; chặn khi ghi sau verify; dừng chặn sau đủ số vòng; `handles` tự điền; sự kiện `self_check` phát
  đúng; `buildOptions` có `hooks.Stop`.
- **UI WPF** (CI Windows, `release: "false"`): thẻ hiện ✓ / ✗, nút chọn đối tượng gọi đúng handle.
- Build 3 target plugin theo `CLAUDE.md`.

## 5. Mục thêm vào `docs/checklist-thu-that.md`

Phần mới **"I. Tự kiểm trước khi báo xong (~4 phút)"** (hỏng thì ghi lại, không chặn phát hành):

1. ★ "Vẽ phòng 4×3 m tường 200" → cuối lượt có thẻ "Tự kiểm … ✓"; câu trả lời không báo xong trước thẻ.
2. ★ Tự vẽ tay 2 line hở góc 5 mm, rồi nhờ "nối dài đường này tới đường kia" → Claude gọi `verify_drawing`, thẻ ✓
   (khe đã nối).
3. ★ "Vẽ 3 line trên layer 0" → thẻ ✗ `LAYER_0`; bấm "Chọn đối tượng lỗi" → 3 line được chọn trong bản vẽ.
4. ○ Hỏi "bản vẽ có bao nhiêu layer?" (chỉ đọc) → không có thẻ, lượt không bị kéo dài.
5. ○ Bật 🔒 Chỉ xem rồi nhờ vẽ → không có vòng kiểm, không thẻ.
6. ○ Bấm Dừng giữa lượt vẽ → lượt dừng ngay, không bị hook kéo lại.
7. ○ Vẽ phòng có cửa → không báo `GAP` ở lỗ cửa.
8. ○ Gõ U sau lượt có `auto_fix` → hoàn tác cả phần vẽ lẫn phần tự sửa bằng một lần U.

## 6. Ước tính công sức và chi phí

| Phần | Công |
| --- | --- |
| `VerifyRules` (10 mã) + test lõi | 1,5–2 ngày |
| `VerifyTools` (đọc entity, lân cận, `auto_fix`, undo) | 1 ngày |
| Tool phía agent + hook Stop + system prompt + test | 0,5–1 ngày |
| Tin `self_check` + thẻ + UI test | 0,5–1 ngày |
| Tài liệu, checklist, build 3 target, CI Windows | 0,5 ngày |
| **Tổng** | **4–5,5 ngày công** |

Chi phí khi làm: build / test Linux tại chỗ (rẻ); CI Windows khoảng 2–3 lượt (mỗi lượt chờ ~15–25 phút, 1 lượt
một lúc). Không cần tác vụ con.

Chi phí cho người dùng mỗi lượt vẽ: thêm 1 lần gọi `verify_drawing` (kết quả gọn, vài trăm token), lượt có lỗi
thêm 1–2 vòng sửa. Lượt chỉ đọc không tốn thêm.

## 7. Việc cần chủ dự án quyết (đã trả lời)

1. Hook **bắt AI tự sửa** (chặn kết thúc tối đa 2 vòng) hay **chỉ bắt kiểm và báo lỗi** (chặn 1 lần để chạy
   `verify_drawing`, không ép sửa).
2. Ngưỡng khe hở mặc định (`GAP`, `OVERSHOOT`, `OPEN_POLY`): đề xuất 50 mm.
3. Cho `auto_fix` các lỗi an toàn không (nối khe, đóng polyline, xóa trùng / dài 0).
4. Thẻ "Tự kiểm" do thread này làm, hay chuyển thread Giao diện & UX.

**Đã quyết (22/11/2026):**

1. Hook **bắt tự sửa, tối đa 2 vòng**; hết vòng vẫn còn lỗi thì cho kết thúc và phải liệt kê lỗi còn lại.
2. Ngưỡng khe hở **10 mm**.
3. **Cho `auto_fix`** các lỗi an toàn, chỉ trên đối tượng của lượt, cùng một lần U.
4. Thẻ "Tự kiểm" **do thread này làm**.

## 8. Đã làm (22/11/2026)

- **Plugin:**
  - `Core/VerifyRules.cs`: 10 mã lỗi, chọn cách sửa không chồng nhau, `SelfCheckSummary` cho thẻ.
  - `Drawing/VerifyTools.cs`: tool `verify_drawing`.
    - Đọc LINE, LWPOLYLINE (cả đoạn cong), ARC, CIRCLE, TEXT / MTEXT (hộp xoay theo chữ), DIMENSION (hộp chữ ước
      tính).
    - So với đối tượng lân cận trong vùng nới thêm 2 × ngưỡng, quét tối đa 3 giây trên bản vẽ lớn.
    - `auto_fix` chỉ sửa đối tượng của lượt, không bao giờ sửa đối tượng có sẵn của người dùng. Chạy trong undo group
      của lượt.
    - Chỉ xem: vẫn kiểm, không sửa. Không chạy trong `batch_execute`, không ghi vào macro.
- **Trợ lý:**
  - `agent/selfCheck.ts`: theo dõi lượt có ghi hay không và đã kiểm sau lần ghi cuối chưa.
  - Hook `Stop` trong `session.ts`: chặn tối đa 2 lần khi chưa kiểm, 2 vòng khi còn lỗi. Không chặn khi bấm Dừng
    hoặc khi `verify_drawing` báo lỗi tool.
  - System prompt: khai `expect` trước khi vẽ, kiểm trước khi báo xong, hết vòng thì liệt kê lỗi còn lại.
- **Khung chat:**
  - Thẻ "Tự kiểm" dưới câu trả lời, ✓ hoặc ✗ kèm tối đa 6 dòng lỗi. Bấm từng dòng để zoom tới.
  - Nút "Chọn đối tượng lỗi". Thẻ ghi "có sửa sau lần kiểm" nếu bản vẽ còn bị sửa sau lần kiểm cuối.
- **Khác bản kế hoạch ban đầu:**
  - Không có tin IPC `self_check`: tool chạy trong plugin nên plugin tự giữ kết quả.
  - Bỏ loại trừ theo layer cửa, vì ngưỡng 10 mm đã nhỏ hơn lỗ cửa.
  - Tắt mã lỗi bằng tham số `ignore`, không đọc dòng `verify:` trong `SKILL.md`.
- **Test:** lõi 199/199 (thêm 35), trợ lý 156/156 (thêm 20), build net48 / net8 / net10 đạt trên Linux. UI test thẻ
  (`SelfCheckCardShowsCleanAndOpenIssues`) đạt trên CI Windows, run 45002618380 (`release: false`).
- **Còn lại (ngoài đợt này):**
  - 4 mã cần chuẩn dạng dữ liệu (lớp B).
  - Hộp chữ của dim mới là ước tính.
  - Block (INSERT) và hatch chưa được kiểm hình học, chỉ được đếm (`COUNT`) và xét lạc (`STRAY`).
  - Chưa có nút tắt tự kiểm trong ⚙.
