# Tổng quan plugin ClaudeCAD 0.9.8

*Soạn ngày 25/11/2026, theo nhánh `claude/skill-docx-gdocs` @ 2164323. Viết cho chủ plugin, không cần biết lập
trình. Thay cho bản tổng quan 0.9.6.*

## 1. Tóm tắt

| | |
| --- | --- |
| Phiên bản | **0.9.8**, đang phát hành (link, mã SHA-256 ở mục 8) |
| Bản trước | **0.9.6** và **0.9.7** cùng phát hành ngày 22/11/2026 |
| Máy chủ Google | Đã cập nhật cho 0.9.8 ngày 25/11 (`Code.gs` mới, quyền hẹp trong `appsscript.json`) |
| AutoCAD | 2022–2027, một bộ cài chung. 2027 có trong danh sách nhưng chưa thử trên máy thật |

**Điểm mới so với 0.9.6:**

1. **Cài đè khi AutoCAD đang mở** (0.9.7): bộ cài nhắc bằng tiếng Việt *Retry / Cancel*, rồi tự dừng tiến trình trợ
   lý của ClaudeCAD còn sót.
2. **Thêm skill từ file Word `.docx`** (0.9.8): plugin tự chuyển sang `SKILL.md`, giữ tiêu đề, danh sách, bảng; bảng có
   cột Layer tự thành **Bảng layer chuẩn**.
3. **Skill soạn trên Google Docs** (0.9.8): cậu dán link doc vào trang **Skill** của Sheet, máy chủ đọc doc, ký số
   rồi đẩy tới máy được gán như skill Admin khác.
4. **Sửa skill chọn cách mở** (0.9.8): nút **Sửa ▾** mở bằng ứng dụng mặc định, ứng dụng khác hoặc Word. Sửa skill
   Default / Admin thì lưu xong plugin hỏi **Tên + Mô tả** để tạo skill mới, thay cho việc tự chép sang `<tên>-sua`.
5. **Xem nội dung skill ngay trong danh sách** (0.9.8): nút **⌄**, chỉ xem.

Từ 0.9.5 trở lên nâng cấp **không phải kích hoạt lại**. Từ 0.9.6, mỗi lượt tốn thêm khoảng **9 %** hạn mức (danh sách
lệnh dài hơn); 0.9.7 và 0.9.8 không đổi phần này.

## 2. Plugin làm gì

ClaudeCAD thêm một khung chat (palette) vào AutoCAD. Người dùng gõ yêu cầu bằng tiếng Việt, ví dụ *"vẽ mặt bằng phòng
4×6 m, tường 220, cửa đi 900"*. Claude đọc bản vẽ đang mở rồi vẽ, sửa, đo hoặc trả lời ngay trên bản vẽ đó.

**Cách chạy:** plugin (C# / WPF trong AutoCAD) gom ngữ cảnh → gửi cho trợ lý (tiến trình Node chạy cạnh AutoCAD,
dùng Claude Agent SDK) → Claude chọn lệnh → plugin thực hiện lệnh trong AutoCAD và trả kết quả lại.

### 2.1 Vẽ, sửa, đo

| Nhóm | Có gì |
| --- | --- |
| Lệnh cho Claude | Hơn 90 lệnh: đường, polyline, cung, tròn, elip, spline, hatch; chữ, mtext, bảng, 8 loại kích thước, mleader; block, thuộc tính, block động; layer, kiểu chữ, kiểu dim; fillet / chamfer, offset, trim / extend, di chuyển / xoay / sao chép / mirror / array / xóa; layout, viewport, in PDF; xref; zoom, lưu. Có lệnh gộp nhiều thao tác một lần (batch) để vẽ nhanh |
| Hiểu bản vẽ | Đọc layer, đối tượng, đơn vị; đo khoảng cách, diện tích; đếm; tìm chữ |
| Tự nhìn bản vẽ | Sau việc vẽ lớn, Claude có thể tự chụp vùng vừa vẽ để soát (chữ đè, ký hiệu lệch). Tắt ở ⚙ ▸ **Tự kiểm tra bằng ảnh** |
| Biết phần sửa tay | Người dùng sửa tay giữa các lượt thì khung chat báo *"✎ Bản vẽ đã được sửa ngoài khung chat…"*, Claude đọc lại các đối tượng đó trước khi làm tiếp (kể cả lần mở sau) |

### 2.2 Bắt điểm và áp chuẩn lúc vẽ (từ 0.9.6)

- **Bắt điểm theo đối tượng:** Claude đặt điểm bằng cách bám vào hình có sẵn (đầu mút, trung điểm, tâm, giao điểm,
  vuông góc, gần nhất, điểm chèn) thay vì tự tính tọa độ. Nhờ vậy cánh cửa, cung cửa, đầu tuyến khớp đúng. Có lệnh
  `get_snap_points` để xem các điểm bắt được. Tọa độ `[x, y]` kiểu cũ, macro và mẫu lệnh đã lưu vẫn chạy như trước.
- **Áp chuẩn lúc vẽ** (chỉ khi đoạn chat dùng skill có *Bảng layer chuẩn*):
  - từ chối vẽ lên layer `0` hoặc layer ngoài bảng, trừ khi người dùng tự nêu tên layer đó hoặc chọn bằng nút **Layer**;
  - layer chuẩn còn thiếu thì tự tạo đúng màu / nét / lineweight;
  - chữ không ghi chiều cao thì lấy theo bảng chữ của skill × tỷ lệ in;
  - thêm skill có bảng sai định dạng thì báo ngay, danh sách Skill ghi ⚠;
  - macro không bị áp chuẩn.

### 2.3 Nhập yêu cầu

| Cách | Chi tiết |
| --- | --- |
| Chọn đối tượng ⬚ | Quét chọn trên bản vẽ, gửi kèm tin. Có danh sách layer của vùng chọn để bỏ tick, nút **→ Cả layer** |
| Lấy điểm ✛ | Bấm điểm trên bản vẽ, tọa độ chèn vào ô nhập |
| Chọn layer ≡ | Lọc, tick layer cần sửa; Claude phân biệt sửa *thuộc tính layer* hay *đối tượng trên layer* |
| Mẫu lệnh `/` và `@` | `/` chọn mẫu lệnh (lưới trục, mặt bằng, diện tích…); `@` chèn đúng tên layer, block, kiểu chữ, kiểu dim, layout |
| Đính kèm | Ảnh phác thảo / scan (tối đa 5 ảnh, Ctrl+V hoặc kéo thả), **Chụp** vùng đang xem, dán bảng Excel |
| Ghi chú dự án ✎ | Ô *Dự án* (chung cho cả thư mục) và *Riêng bản vẽ này*, gửi kèm mọi tin; mâu thuẫn với skill thì theo ghi chú |
| Tỷ lệ in | Chọn 1:20 … 1:1000; Claude chỉnh chiều cao chữ và dim theo tỷ lệ |

### 2.4 An toàn bản vẽ

| Cơ chế | Cách hoạt động |
| --- | --- |
| Thẻ xác nhận | Trước khi xóa theo điều kiện hoặc sửa hơn 20 đối tượng, Claude dừng hỏi. Thẻ ghi to số đối tượng, nút **⌖ Xem trên bản vẽ**, rồi **Đồng ý / Hủy** |
| Kế hoạch trước việc lớn | Việc khoảng trên 50 đối tượng: thẻ **Kế hoạch** với các bước, kích thước, giả định; chọn **Làm / Sửa kế hoạch / Bỏ**. Khi làm thì hiện tiến độ *"Bước 3/7"* |
| Hoàn tác cả lượt | Mọi thay đổi của một lượt (kể cả phần tự sửa của Tự kiểm) là một bước: gõ `U` một lần |
| Phiên bản DWG | Trước khi chat sửa bản vẽ, nếu bản sao gần nhất cũ hơn 30 phút thì lưu một bản sao. Giữ 10 bản, xem trong tab chỉ xem |
| Chỉ xem | Nút **🔒 Chỉ xem**: Claude chỉ đọc, đo, trả lời |
| Chữ trong bản vẽ | Nội dung có sẵn trong bản vẽ không bao giờ được coi là lệnh cho Claude |

### 2.5 Sau mỗi câu trả lời

- **Xem kết quả (N):** zoom và tô sáng phần vừa vẽ / sửa.
- **Thay đổi: Thêm · Sửa · Xóa** theo layer; bấm để zoom.
- **Tự kiểm ✓ / ✗**, nút **Chọn đối tượng lỗi** (mục 5).
- **Thao tác (N):** từng thao tác, thời gian, lỗi.
- Nút gợi ý bước tiếp theo; **💾 Lưu macro**; **Hoàn tác lượt này**.

### 2.6 Tiện ích khác

- **Macro:** lưu một lượt vẽ ưng ý, chạy lại không cần Claude (không tốn hạn mức), chạy **📍 Tại điểm** khác.
- **Mỗi bản vẽ một cuộc chat**, nhớ qua các lần mở AutoCAD (bản vẽ phải được lưu). **Chat mới** mở đoạn mới.
- **Theo dõi và dừng:** dòng *"● Đang làm · thời gian · số thao tác"*, danh sách bước ✓ / ● / ⚠; **Esc** trong ô nhập
  hoặc nút ■ để dừng.
- **Gửi thêm ý khi Claude đang làm:** tin chờ, tự gửi khi lượt xong (có nút **Bỏ**).
- **Thu gọn khung chat ⌄:** còn ô nhập; tự mở lại khi cần cậu quyết.
- **Icon trên thanh trạng thái AutoCAD:** chấm xanh khi đang làm, chấm vàng khi cần xem, bong bóng khi lượt xong.
- **Giao diện theo theme AutoCAD:** màu, font, cỡ chữ lấy theo palette của AutoCAD.
- **Tạo skill từ cuộc chat**, sửa và gửi lại tin cuối, xuất cuộc chat ra `.md`.
- **Tài khoản AI:** Claude Pro / Max / Team hoặc API key Anthropic (lưu mã hóa). Thanh hạn mức, thẻ hết hạn mức có
  **Tự gửi lại lúc …**. Không chạy với ChatGPT hay Gemini.
- **Gửi báo cáo lỗi:** một file `.zip` trên Desktop (thông tin máy, log, tự kiểm tra), không có chat, bản vẽ, key.

### 2.7 Lệnh

| Lệnh | Việc |
| --- | --- |
| `CC`, `CLAUDECHAT`, Ctrl+Shift+Space | Mở khung chat |
| `CLAUDECHATRESTART` | Khởi động lại trợ lý |
| `CLAUDECHATUPDATE` | Tải bản mới (cài khi đóng AutoCAD) |
| `CLAUDECHATSELFTEST` | Tự kiểm tra plugin trên bản vẽ tạm, báo cáo ✓ / ✗ trong Notepad |
| `CLAUDECHATLOG` | Mở thư mục log |

## 3. Cài đặt và kích hoạt

### 3.1 Bộ cài

- Một file `ClaudeCAD-Setup.exe` (bản 0.9.6 khoảng 140 MB; kích thước 0.9.8 ở mục 8), kèm sẵn Node.js và Claude Code.
  Không cần quyền admin.
- Bộ cài chưa ký số: Windows hiện *"Windows protected your PC"* thì bấm **More info → Run anyway**.
- Hai trang cần chú ý: **Kích hoạt** và **Đăng nhập Claude ngay** (trang cuối).
- Cài ngầm nhiều máy: `ClaudeCAD-Setup.exe /VERYSILENT /KEY=<key của đúng máy đó>`.
- Cập nhật: khung chat tự kiểm tra bản mới mỗi ngày; skill, macro, mẫu lệnh, lịch sử chat được giữ.
- **Cài đè khi AutoCAD đang mở** (từ 0.9.7):
  - Bộ cài hiện hộp tiếng Việt *"AutoCAD đang mở… bấm Retry"*. Đóng AutoCAD rồi bấm **Retry** là cài tiếp; chưa đóng
    mà bấm Retry thì hộp hiện lại.
  - Bấm **Cancel** thì bộ cài dừng, nhắc *"Hãy đóng AutoCAD rồi chạy lại"*.
  - Bộ cài tự dừng tiến trình trợ lý của ClaudeCAD còn chạy, không đụng Node.js khác trên máy.
  - Bộ cài tới 0.9.6 hiện hộp tiếng Anh *"Setup was unable to automatically close all applications"*: đóng hết AutoCAD
    rồi bấm **Try again**, đừng bấm *Ignore*.

| AutoCAD | Bản build của plugin |
| --- | --- |
| 2022, 2023, 2024 | .NET Framework 4.8 |
| 2025, 2026 | .NET 8 |
| 2027 (chưa thử máy thật) | .NET 10 |

Không chạy trên AutoCAD LT. AutoCAD đời sau 2027 sẽ không tự nạp cho tới khi được khai báo.

### 3.2 Kích hoạt theo máy

| Bước | Chi tiết |
| --- | --- |
| Mã máy | Mỗi máy có mã `CC-XXXX-XXXX-XXXX`. Cài lại Windows / thay mainboard thì mã đổi, máy xin duyệt lại |
| Xin duyệt | Bộ cài hiện mã máy và dòng liên hệ Zalo của cậu; tự gửi yêu cầu: một dòng **Chờ duyệt** trong Sheet và một email cho cậu (kèm KEY dự phòng) |
| Duyệt qua Sheet (khuyên dùng) | Đổi ô **Trạng thái** thành `Active`. Lần mở AutoCAD kế tiếp máy tự dùng được, hoặc khách bấm **Kiểm tra lại**. Không cần key |
| Gửi key qua Zalo | Khách dán key vào ô **Key kích hoạt** (bộ cài hoặc thẻ 🔒 trong khung chat). Key chỉ dùng trên đúng máy đó |
| Khóa máy | Đổi thành `Disabled`: máy bị khóa ở lần kiểm tra kế tiếp, khung chat hiện thẻ **🔒 tạm khóa**. Đổi lại `Active` là dùng tiếp, không cần key mới. Đang có một ca máy `Disabled` vẫn dùng được (mục 10) |
| Kiểm tra định kỳ | Mỗi lần mở AutoCAD và khoảng **6 giờ** một lần. Câu trả lời có chữ ký số |
| Mất mạng | Dùng tối đa **7 ngày** từ lần kiểm tra thành công gần nhất, rồi tự khóa. Vặn lùi đồng hồ cũng bị phát hiện |
| Máy không có mạng | Cậu chạy hàm `taoKeyThuCong` trong Apps Script: email về có KEY và **mã cho máy không có mạng** (2 dòng, dán cả hai). Máy này không bị khóa từ xa, không bị giới hạn 7 ngày; chỉ cấp cho máy thật sự không có mạng |

Lưu ý:

- Ô Trạng thái chỉ nhận `Active` / `Disabled`; gõ sai gì cũng coi là Chờ duyệt (máy bị khóa), nên không mở nhầm quyền.
- Tên máy, user Windows do máy kia tự khai: xác nhận người xin qua Zalo trước khi duyệt.
- Cập nhật, cài đè, gỡ ra cài lại trên cùng máy không hỏi lại key.

## 4. Skill

Skill là file `.md` mô tả chuẩn vẽ: bảng layer, chiều cao chữ, quy cách, quy trình. Claude vẽ theo skill đang dùng.
Từ 0.9.8, skill có thể soạn bằng **Word** (khách) hoặc **Google Docs** (cậu, qua Sheet); plugin vẫn lưu thành `SKILL.md`.

### 4.1 Thư viện và skill theo từng đoạn chat

- **Thư viện:** mọi skill đã có trên máy.
- **Mỗi đoạn chat có bộ skill riêng.** Nút ✦ **Skill (N)** có phần *Đang dùng* (✓, nút **−**) và *Thư viện* (nút **+**).
  Mỗi skill có thêm nút **⌄** (xem) và **Sửa ▾** (mục 4.3).
- Đoạn chat mới dùng skill của đoạn chat gần nhất.
- Có thể nhờ *"tạo skill từ cuộc chat này"*: Claude soạn bản nháp để xem / sửa rồi thêm vào plugin.
- Bỏ hẳn một skill khỏi thư viện: xóa thư mục mang tên skill đó trong thư mục skill của plugin.

### 4.2 Thêm skill: file `.md` hoặc Word `.docx`

Bấm **＋ Add skill**, chọn một hoặc nhiều file (giữ Ctrl / Shift). Chọn lẫn `.md` và `.docx` cũng được. Skill vào thư
viện và dùng luôn ở đoạn chat hiện tại. Trùng tên skill đã có thì plugin hỏi có thay không.

**File Word `.docx` (mới ở 0.9.8):** plugin tự chuyển sang `SKILL.md`.

| | Chi tiết |
| --- | --- |
| Giữ được | Tiêu đề (Heading 1–6, kể cả style tiếng Việt); chữ đậm / nghiêng; danh sách chấm, danh sách số, có thụt cấp; bảng (hàng đầu là tiêu đề); chữ của link; tracked changes lấy bản đã chấp nhận |
| Bảng layer | Bảng có cột **Layer** tự thành **Bảng layer chuẩn**, nên áp chuẩn lúc vẽ và tự kiểm theo bảng vẫn chạy |
| Bị bỏ, có báo trong khung chat | Ảnh / hình vẽ, hộp chữ, chú thích cuối trang, ghi chú lề, công thức. Ô gộp được tách ra, ô thừa để trống. Dấu `\|` trong ô đổi thành `/` |
| Chưa có phần đầu | File thiếu `name` / `description` thì plugin mở hộp thoại: **Tên** (gợi ý từ tên file) và **Mô tả** (khi nào dùng skill, 20–1024 ký tự). Bấm Hủy thì không thêm |
| Giới hạn | File tối đa 10 MB; kết quả tối đa 45.000 ký tự. File `.doc` cũ: mở bằng Word, lưu lại thành `.docx` |
| Cập nhật | Sửa file Word rồi chọn lại bằng ＋ Add skill, plugin hỏi có thay không |

- Ảnh nào chứa quy cách thì viết lại thành chữ hoặc bảng, vì skill chỉ dùng chữ.
- Muốn file Word tự mang phần đầu: gõ ở đầu file 4 đoạn `---`, `name: …`, `description: …`, `---`. Word có thể tự đổi
  `---` thành đường kẻ ngang; khi đó plugin không thấy phần đầu và sẽ hiện hộp thoại.

### 4.3 Xem và sửa skill (mới ở 0.9.8)

**Xem ⌄:** mở khung xem nội dung skill ngay trong danh sách, bấm **⌃** để thu gọn.

- Hiện Markdown đã định dạng (tiêu đề, danh sách, bảng layer), theo màu và font của khung chat.
- Khung có chiều cao giới hạn, kéo thanh cuộn để xem tiếp. Phần đầu name / description không hiện lại.
- Chỉ xem: không tạo bản sao, không ghi file. File skill đổi thì nội dung xem tự cập nhật.

**Sửa ▾:** bấm **Sửa** là mở theo cách đã chọn lần trước. Bấm **▾** cạnh nút để đổi; plugin nhớ lựa chọn.

| Cách mở | Chi tiết |
| --- | --- |
| Ứng dụng mặc định | Ứng dụng Windows đang mở file `.md` (Notepad, VS Code…) |
| Chọn ứng dụng khác… | Hộp "Mở bằng…" của Windows |
| Word | Plugin chuyển skill sang file Word, chỉ có nội dung; phần đầu name / description plugin tự giữ. Lưu trong Word thì plugin chuyển ngược lại và kiểm lại **Bảng layer chuẩn**: bảng lệch (thiếu cột, màu sai…) thì báo lỗi theo từng dòng và hỏi có lưu không. Phần Word thêm vào mà skill không giữ được (ảnh, ô gộp…) cũng được báo. Máy chưa cài Word thì mục này mờ |

**Khi lưu:**

| Skill | Lưu thì sao |
| --- | --- |
| **You** | Ghi thẳng vào skill đó. Plugin tự nạp lại, Claude dùng bản mới từ tin nhắn tiếp theo |
| **Default / Admin** | Plugin mở một **bản sao**, bản gốc không bao giờ bị ghi. Lưu bản sao xong, plugin hiện hộp **"Tạo skill mới từ skill gốc"**: Tên gợi ý `<tên>-sua`, Mô tả lấy từ bản gốc, sửa được cả hai |

- Bấm **Tạo skill**: có skill mới nhãn **You**, **đoạn chat hiện tại** chuyển sang skill mới (không dùng cả hai cùng lúc).
  Các đoạn chat khác giữ nguyên. Lần lưu sau trong cửa sổ đang mở cập nhật vào skill mới.
- Bấm **Để sau**: chưa tạo gì; lưu lần nữa thì plugin hỏi lại.
- Mở rồi đóng mà không sửa gì: không có gì xảy ra, không còn bản chép thừa.
- Bản gốc Default vẫn được làm mới khi cập nhật plugin; bản gốc Admin vẫn nhận cập nhật / Ẩn / Xóa từ cậu. Bản khách sửa
  không gửi gì về cho cậu.
- Bản sao nằm ở `%LocalAppData%\ClaudeCAD\skill-edit`, tự dọn sau 7 ngày.
- Khác 0.9.6–0.9.7: bản cũ bấm Sửa là chép ngay sang `<tên>-sua`; 0.9.8 chỉ tạo skill mới khi khách lưu và bấm Tạo skill.
- **Sửa thẳng file gốc ngoài plugin** (Explorer, Notepad…) thì vẫn như 0.9.6: phần sửa chuyển sang `<tên>-sua` (hoặc
  `<tên>-sua-2`… nếu đã có), bản gốc được khôi phục (Default lấy lại từ bản mẫu kèm plugin, Admin lấy lại từ bản đã lưu
  khi nhận).

### 4.4 Nhãn

| Nhãn | Nghĩa |
| --- | --- |
| **Admin** | Cậu gửi tới máy đó qua Google Sheet (nội dung dán thẳng hoặc link Google Docs) |
| **Default** | 3 skill mẫu đi kèm plugin |
| **You** | Khách tự thêm bằng ＋ Add skill, hoặc skill mới tạo từ bản sửa của skill Default / Admin |

### 4.5 Đẩy skill theo máy (từ 0.9.6)

- Cậu nhập skill vào trang **Skill** của Sheet, rồi gán cho từng mã máy ở trang **Gán skill** với hành động
  **Thêm / Ẩn / Xóa** (chi tiết ở mục 6).
- Máy khách tự nhận ở lần kiểm tra trạng thái kế tiếp (mở AutoCAD, khoảng 6 giờ một lần, hoặc bấm **Kiểm tra lại**),
  có toast báo. Sửa Sheet xong có thể phải chờ tới 10 phút.
- Chỉ máy **Active** nhận skill. Mỗi máy tối đa **20 skill**, mỗi skill tối đa **45.000 ký tự**, tổng tối đa **400 KB**.
- Gói skill có **chữ ký số** (cùng cặp khóa với giấy phép) và **chống gửi lại gói cũ**. Gói sai thì bỏ cả gói, chỉ ghi
  log, không ảnh hưởng việc máy có được dùng hay không.
- Skill mới gửi tới **không tự bật**: khách bấm **+** để dùng.
- **Trùng tên** với skill của khách: giữ cả hai, bản của cậu nằm ở thư mục `<tên>-admin`.
- **Khách sửa skill Admin** (0.9.8): bản sửa thành skill You riêng (mục 4.3), không nhận cập nhật, Ẩn hay Xóa từ Sheet.
  Bản Admin gốc vẫn nhận như thường.
- **Skill bị Ẩn:** biến khỏi danh sách, Claude không nạp nữa, kể cả trong đoạn chat đang dùng. Chọn lại **Thêm** là hiện
  lại ngay.
- **Soạn trên Google Docs** (mới ở 0.9.8): cậu dán link doc thay cho nội dung (mục 6.1). Máy chủ đọc doc, ký số, đẩy
  đi như skill dán thẳng. **Máy khách không bao giờ tự tải link.**
- Plugin 0.9.5 trở về trước không nhận skill, nhưng vẫn chạy bình thường.

### 4.6 Skill mẫu

| Skill | Dùng cho |
| --- | --- |
| `ve-kien-truc` | Mặt bằng kiến trúc: trục, cột, tường 220 / 110, cửa, kích thước, tên phòng |
| `ve-mep` | Điện (chiếu sáng, ổ cắm, máng cáp) và cấp thoát nước |
| `ve-ha-tang` | Đường, lý trình, cao độ, cống, ranh giới, bảng tọa độ |

- Từ 0.9.6: danh mục chuẩn bị trước khi vẽ, dựng hình bằng offset / trim và bắt điểm, tự kiểm bằng lệnh đo, lỗi hay
  gặp, mẫu báo cáo.
- **Số liệu vẫn là số mẫu**, chưa theo chuẩn công ty.
- Skill mẫu có sẵn trong thư viện nhưng **không tự dùng**.
- **Tự cập nhật:** lần mở AutoCAD đầu tiên sau khi cập nhật, skill mẫu khách **chưa sửa** được thay bằng bản mới. Đã
  sửa thẳng file thì phần sửa chuyển sang `<tên>-sua`, bản mẫu mới về chỗ cũ (bản mẫu gốc nằm ở `Contents\skills-mau`).
- Bảng chữ theo tỷ lệ in hiện chỉ có ở skill kiến trúc, nên kiểm chiều cao chữ / dim style chưa chạy với MEP và hạ tầng.

### 4.7 Skill tốt nên có

Quy trình chuẩn bị; danh mục trước khi vẽ (số đối tượng dự kiến theo layer); **Bảng layer chuẩn** đúng định dạng;
bảng **Chữ và kích thước theo tỷ lệ in**; cách dựng hình bằng offset / trim / bắt điểm; ví dụ hoàn chỉnh; mục kiểm
tra trước khi báo xong (có thể ghi `Ngưỡng khe hở: <số> mm`); lỗi hay gặp; mẫu báo cáo cuối lượt. Soạn bằng Word thì
dùng Heading cho tiêu đề và bảng thật (không vẽ bảng bằng ảnh). Chi tiết: `skills/README.md`.

## 5. Tự kiểm bản vẽ (từ 0.9.6)

**Luồng một lượt:** Claude vẽ / sửa → định báo xong → plugin chặn lại nếu chưa chạy *Tự kiểm bản vẽ*
(`verify_drawing`) sau lần ghi cuối → còn lỗi thì gửi danh sách lỗi cho Claude sửa → tối đa **2 vòng** → cuối lượt có
thẻ **Tự kiểm**, ví dụ *"Tự kiểm: 24 đối tượng · 0 lỗi ✓"*. Còn lỗi thì Claude phải liệt kê trong câu trả lời.

- Chỉ kiểm đối tượng của lượt đó và vùng lân cận, không quét cả bản vẽ.
- Không kiểm khi lượt chỉ hỏi / đo, khi bật **Chỉ xem**, hoặc khi người dùng bấm **Dừng**.
- Lỗi an toàn (nối khe hở, đóng polyline, xóa bản trùng, xóa nét dài 0) tự sửa ngay, chỉ trên đối tượng của lượt đó,
  vẫn hoàn tác bằng một lần `U`.
- Bấm **Chọn đối tượng lỗi** trên thẻ để xem chỗ lỗi trong bản vẽ.

**14 mã lỗi:**

| Mã | Dò gì | Tự sửa |
| --- | --- | --- |
| `GAP` | Hai đầu nét gần chạm mà không chạm (mặc định dưới 10 mm, skill đổi được ngưỡng) | Có |
| `OVERSHOOT` | Đầu nét vượt qua nét khác một đoạn ngắn | Không |
| `OPEN_POLY` | Polyline đầu ≈ cuối mà chưa đóng | Có |
| `ZERO_LEN` | Nét / cung dài bằng 0 | Có |
| `DUPLICATE` | Hai đối tượng trùng nhau | Có |
| `ARC_MISMATCH` | Đầu cung không chạm nét nào (cung cửa không chạm mép lỗ) | Không |
| `LAYER_0` | Đối tượng trên layer `0` hoặc `Defpoints` | Không |
| `TEXT_OVERLAP` | Chữ / dim đè nhau | Không |
| `STRAY` | Đối tượng lạc xa cụm chính | Không |
| `COUNT` | Số đối tượng theo layer khác với số Claude khai trước khi vẽ | Không |
| `LAYER_UNKNOWN` | Layer ngoài bảng chuẩn của skill | Không |
| `PROP_OVERRIDE` | Màu / nét gán riêng cho đối tượng, khác layer | Không |
| `TEXT_HEIGHT` | Chiều cao chữ sai so với bảng chữ × tỷ lệ in | Không |
| `DIM_STYLE` | Kiểu kích thước sai tỷ lệ / chiều cao chữ dim | Không |

4 mã cuối chỉ chạy khi skill đang dùng có bảng chuẩn (kể cả bảng đến từ file Word có cột Layer). Layer người dùng tự
nêu tên hoặc chọn bằng nút Layer thì không bị báo. Thiết kế đầy đủ: `docs/ke-hoach-tu-kiem.md`.

## 6. Máy chủ Google

Máy chủ kích hoạt là một **Google Apps Script gắn với Google Sheet "ClaudeCAD – Thiết bị"**, chạy trên tài khoản
Google của cậu.

### 6.1 Các trang trong Sheet

| Trang | Cột | Cậu làm gì |
| --- | --- | --- |
| **Thiết bị** | Mã máy · Thiết bị · User Windows · Ngày xin · Ngày kích hoạt · Trạng thái · Lần cuối dùng · Phiên bản · Ghi chú | Duyệt (`Active`), khóa (`Disabled`), xem máy nào còn dùng. Xóa dòng thì máy về Chờ duyệt ở lần kiểm tra sau |
| **Skill** | Tên · Phiên bản · Nội dung SKILL.md · Ghi chú | Mỗi dòng một skill. Ô Nội dung: dán **toàn bộ** file (cả phần đầu `name:` / `description:`), **hoặc** từ 0.9.8 dán **đúng một link Google Docs**. `name` trùng cột Tên. Nội dung dán thẳng: **sửa thì tăng Phiên bản** |
| **Gán skill** | Mã máy · Tên skill · Hành động · Ghi chú | Mỗi dòng một skill cho một máy. Hành động **Thêm / Ẩn / Xóa**. Để nguyên dòng trên Sheet; hai dòng trùng thì dòng dưới thắng |

- Dòng có lỗi (skill không có trong trang Skill, quá dài, thiếu description, tên sai…) không được gửi, lý do ghi ở cột
  **Ghi chú**.
- Xóa dòng gán thì máy **giữ nguyên** skill như lần nhận cuối, không xóa gì. Muốn gỡ thì chọn **Xóa**.
- ⚠️ Đừng đổi tên trang **Thiết bị**, đừng chèn / đổi thứ tự cột trước cột Ghi chú (máy chủ đọc cột theo vị trí).
- Chống spam: tối đa 30 máy mới mỗi giờ, 10 email mỗi giờ, 40 email mỗi ngày; tên máy gửi lên không chạy được như
  công thức trong bảng.
- Xem các lượt xin / kích hoạt / kiểm tra ở mục **Thực thi** (Executions) trong trình soạn Apps Script.

**Skill từ Google Docs (mới ở 0.9.8):**

| | Chi tiết |
| --- | --- |
| Cách dùng | Dán link dạng `https://docs.google.com/document/d/…/edit` vào ô Nội dung, không kèm chữ khác |
| Quyền đọc | Doc **không cần chia sẻ**: máy chủ đọc bằng tài khoản Google của cậu (quyền chỉ xem Drive) và xuất ra Markdown |
| Phần đầu | Đầu doc phải có `---`, `name: <đúng cột Tên>`, `description: …`, `---`. Google Docs đổi `---` thành đường kẻ ngang thì máy chủ vẫn nhận |
| Phiên bản | **Tự theo giờ sửa cuối của doc**; cột Phiên bản bỏ qua với dòng link |
| Khi nào tới máy | Sửa doc xong, máy được gán nhận bản mới ở lần kiểm tra kế tiếp, chậm nhất khoảng 10 phút + chu kỳ kiểm tra. Bản đang sửa dở cũng có thể đi luôn: soạn xong rồi hãy sửa doc thật |
| Ghi chú | Báo "Google Docs, bản sửa lúc …", hoặc lỗi: sai link / chưa có quyền xem, không phải Google Docs, thiếu phần đầu, quá dài |
| Tải ít | Máy chủ chỉ đọc doc của skill gán cho máy đang hỏi, chỉ tải lại khi doc có bản sửa mới |

⚠️ **Ai sửa được doc là đẩy được chỉ dẫn cho Claude** tới mọi máy được gán, không cần đụng vào Sheet. Vì vậy:

- Chỉ dùng doc do **chính tài khoản của cậu tạo và sở hữu**, không dùng doc người khác chia sẻ.
- Chia sẻ doc để **Hạn chế** (Restricted), không có người chỉnh sửa nào khác.
- Xem ai đã sửa: **Tệp → Nhật ký phiên bản**.
- Chưa rõ bản xuất có kèm **đề xuất** chưa chấp nhận hay không: đừng cấp quyền Người nhận xét / Đề xuất cho ai.

### 6.2 Các file Apps Script

| File | Chứa gì | Khi nâng cấp |
| --- | --- | --- |
| `BiMat.gs` | Phần bí mật của máy chủ. Người phát triển tạo bằng lệnh kín và gửi riêng | **Không đụng tới.** Không chia sẻ, không đưa lên GitHub: ai có file này tự tạo được key |
| `Code.gs` | Phần xử lý, không chứa bí mật | Dán đè nguyên văn bản mới từ repo |
| `appsscript.json` (từ 0.9.8) | Khóa `oauthScopes` khai 4 quyền hẹp: đọc / ghi đúng bảng tính này, gửi email, **chỉ xem** Drive, gọi dịch vụ bên ngoài | Chỉ thêm / sửa khóa `oauthScopes`, giữ nguyên các khóa khác, không dán đè cả file |

Nâng cấp máy chủ: thay `Code.gs` → Lưu → chạy hàm `kiemTra` một lần → **Triển khai → Quản lý triển khai → Phiên bản
mới**. Làm như vậy thì link máy chủ giữ nguyên. Đừng bấm "Tùy chọn triển khai mới": link sẽ đổi và bộ cài đã phát
hành không gọi được máy chủ.

- Đã khai `oauthScopes` thì Google không tự dò quyền nữa: bản `Code.gs` sau cần quyền mới mà chưa khai là máy chủ hỏng.
  Luôn chạy `kiemTra` **trước** khi tạo Phiên bản mới. Nếu có hỏng, máy khách vẫn chạy tối đa 7 ngày.
- Không khai `oauthScopes` thì Google sẽ xin quyền Drive đầy đủ (xem, sửa, xóa mọi file).

Cập nhật máy chủ cho 0.9.6 (chuyển sang 2 file) làm xong ngày 22/11. Cập nhật cho 0.9.8 (`Code.gs` mới, `oauthScopes`
hẹp, `kiemTra`, Phiên bản mới) **làm xong ngày 25/11**. Hướng dẫn từng bước: `activation/README.md`.

## 7. Bảo mật và giới hạn

### 7.1 Những gì được bảo vệ

- **Giấy phép theo máy, có chữ ký số.** Chép thư mục plugin sang máy khác hoặc cài bằng `.zip` vẫn bị hỏi kích hoạt.
- **Trạng thái máy có chữ ký**, kiểm mỗi lần mở AutoCAD và ~6 giờ một lần; mất mạng tối đa 7 ngày.
- **Link máy chủ nằm sẵn trong plugin:** xóa file cấu hình không tắt được kiểm tra; link lạ không có chữ ký của cậu.
- **Chống vặn lùi đồng hồ** để kéo dài 7 ngày.
- **Gói skill có chữ ký**, chống gửi lại gói cũ, kiểm tên, nội dung, giới hạn kích thước. Skill từ Google Docs đi cùng
  đường này.
- **Máy khách không tải link Google Docs**; chỉ máy chủ đọc, bằng quyền chỉ xem Drive.
- **File Word được kiểm trước khi chuyển:** chặn nội dung lạ trong file (DTD), giới hạn 50 MB sau giải nén cho mỗi
  phần, file tối đa 10 MB.
- **API key của khách** lưu mã hóa theo tài khoản Windows. Báo cáo lỗi che key và mã đăng nhập.
- **Dữ liệu bản vẽ không phải lệnh:** chữ có sẵn trong bản vẽ không điều khiển được Claude.
- Claude chỉ được đọc thư mục skill.

### 7.2 Giới hạn đã biết

| Giới hạn | Ý nghĩa |
| --- | --- |
| Mã nguồn đang công khai | Người biết lập trình có thể sửa mã bỏ bước kiểm tra rồi tự build lại. Key + trạng thái chỉ chặn được người dùng thông thường. Muốn chặn cả trường hợp này thì phải để repo riêng tư |
| Người rành kỹ thuật | Có thể xóa mốc chống vặn đồng hồ |
| Ai sửa được bảng tính | Đẩy được chỉ dẫn cho Claude trên máy mọi khách và xem được bí mật trong Apps Script. Chia sẻ Sheet chỉ cho quyền **Người xem** |
| Ai sửa được doc Google Docs đã gán | Đẩy được chỉ dẫn cho Claude, không cần vào Sheet. Chỉ dùng doc của cậu, chia sẻ **Hạn chế** |
| Mã máy không phải bí mật | Người biết mã máy của một máy Active tải được skill gán cho máy đó. **Không đưa mật khẩu, API key hay dữ liệu riêng của khách khác vào skill** |
| Mã cho máy không có mạng | Không khóa từ xa được; chỉ cấp khi thật cần |
| File Word | Ảnh, hộp chữ, chú thích, công thức bị bỏ; ô gộp bị tách. Quy cách nằm trong ảnh phải viết lại thành chữ |
| Cần mạng và hạn mức | Phụ thuộc hạn mức tài khoản Claude; từ 0.9.6 mỗi lượt tốn thêm ~9 % |
| Kiểm thật | Nhiều mục chỉ kiểm được trên AutoCAD / Windows thật (mục 8) |

## 8. Bộ cài 0.9.8

| | |
| --- | --- |
| Nguồn | Nhánh `claude/skill-docx-gdocs` @ 2164323, gộp vào `main` @ {{MAIN_098}} |
| CI | Run {{RUN_098}} |
| Link tải | {{LINK_098}} |
| Kích thước `Setup.exe` | {{SIZE_098}} |
| SHA-256 | `{{SHA_098}}` |
| Test tự động | Lõi 278, trợ lý 192 (đều đạt ở 2164323) |

Kiểm file tải về: mở PowerShell trong thư mục chứa file, chạy `Get-FileHash .\ClaudeCAD-Setup.exe`, so với mã trên.

Bản trước: **0.9.6** phát hành 22/11 (PR #7); **0.9.7** phát hành 22/11 (PR #8), link
<https://github.com/Tieudao-dot/Agent-to-Autocad/releases/download/v0.9.7/ClaudeCAD-Setup.exe>.

### Chưa thử trên máy thật

Các mục dưới đây mới qua test tự động và CI, chưa chạy trên AutoCAD / Windows thật:

| Bản | Mục | Hướng dẫn thử |
| --- | --- | --- |
| 0.9.6 | Đẩy skill theo máy (Thêm → nhãn Admin; tăng phiên bản; Ẩn / Thêm lại; Xóa; máy không được gán thì không nhận) | `docs/ke-hoach-day-skill.md` mục 6 |
| 0.9.6 | Tự kiểm, bắt điểm, áp chuẩn (I1–I13) | `docs/checklist-thu-that.md` phần I |
| 0.9.6 | Skill mẫu tự cập nhật; sửa thẳng file mẫu → tách sang `<tên>-sua` | `docs/test-log.md` S-6, S-7 |
| 0.9.7 | Cài đè khi AutoCAD đang mở: hộp tiếng Việt Retry / Cancel, tự dừng tiến trình trợ lý | `docs/test-log.md` I-6 |
| 0.9.8 | ＋ Add skill bằng `.docx`: bảng layer, hộp Tên + Mô tả, báo phần bị bỏ | `skills/README.md` |
| 0.9.8 | Sửa ▾: ba cách mở, nhớ lựa chọn; skill You ghi đè; Default / Admin → hộp Tạo skill mới, đoạn chat chuyển sang skill mới | `skills/README.md` mục Sửa |
| 0.9.8 | Mở bằng Word: chuyển qua lại `.md` ↔ `.docx`, ẩn phần đầu, báo lỗi bảng layer theo dòng | `skills/README.md` mục Sửa |
| 0.9.8 | Nút ⌄ xem nội dung skill | Mở ✦ Skill, bấm ⌄ |
| 0.9.8 | Skill từ Google Docs tới máy khách thật (gán, sửa doc → nhận bản mới) | `activation/README.md` |
| Chung | Lượt thử đầy đủ (~20 phút): cài + kích hoạt, đăng nhập, vẽ, xác nhận, hoàn tác, giao diện, skill / macro, AutoCAD 2026 / 2027 | `docs/checklist-thu-that.md` phần A–H |

## 9. Lịch sử phiên bản

| Phiên bản | Nội dung chính | Trạng thái |
| --- | --- | --- |
| **0.9.8** | ＋ Add skill nhận `.docx`; skill soạn trên Google Docs qua Sheet; Sửa ▾ chọn cách mở (ứng dụng mặc định / khác / Word), Default / Admin lưu thành skill mới qua hộp Tên + Mô tả; nút ⌄ xem skill; máy chủ khai quyền hẹp | Đang phát hành (25/11/2026) |
| **0.9.7** | Cài đè khi AutoCAD đang mở: hộp tiếng Việt Retry / Cancel, tự dừng tiến trình trợ lý | Phát hành 22/11/2026 (PR #8) |
| **0.9.6** | Đẩy skill theo máy, nhãn Admin / Default / You; tự kiểm bản vẽ 14 mã; bắt điểm; áp chuẩn lúc vẽ; 3 skill mẫu viết lại và tự cập nhật | Phát hành 22/11/2026 (PR #7) |
| **0.9.5** (gồm phần từng ghi 0.9.4) | Khóa key theo máy + quản lý thiết bị trên Google Sheet; ~30 bản sửa sau rà soát; skill theo từng đoạn chat; AutoCAD 2026 / 2027; giao diện kiểu palette AutoCAD, thu gọn, icon thanh trạng thái, tiến độ từng bước, gửi thêm ý; checklist thử thật | Phát hành 18/11/2026. Nâng cấp từ 0.9.3 phải kích hoạt máy |
| 0.9.3 | Đăng nhập Claude ngay trong bộ cài; bộ cài nhẹ hơn; trang phát hành ghi kích thước + SHA-256 | Phát hành 09/11/2026 |
| 0.9.2 | Toast; báo hạn mức trong chat; Gửi báo cáo lỗi; mục Tài khoản AI | Đã phát hành |
| 0.9.1 | Cài 3 bước, kèm Node.js, nút Đăng nhập Claude, thẻ Bắt đầu, link tải cố định | Đã phát hành |
| 0.9.0 | Lệnh `CLAUDECHATSELFTEST`; nhiều cải thiện tốc độ; sửa 14 lỗi | Đã phát hành |
| 0.8.0 | Chọn layer rồi chat chỉnh sửa | Đã phát hành |
| 0.7.0 | Biết phần sửa tay; phiên bản DWG 30 phút, giữ 10 bản | Đã phát hành |
| 0.6.0 | Tự nhìn bản vẽ, Chụp, gợi ý bước tiếp, macro, mẫu lệnh, tạo skill từ chat, Chỉ xem, ghi chú dự án, `@`, kế hoạch | Đã phát hành |
| 0.5.0 | **+ Add skill** và quản lý skill | Đã phát hành |
| 0.4.0 | Khung chat 20 tính năng | Đã phát hành |
| 0.3.0 | AutoCAD 2022–2025, bộ cài, cập nhật tự động | Đã phát hành |

## 10. Việc còn mở

| Việc | Tình trạng |
| --- | --- |
| Máy đã đổi `Disabled` vẫn dùng được | Đang tìm nguyên nhân. Cậu đóng hẳn rồi mở lại AutoCAD trên máy đó; nếu vẫn dùng được thì gửi mã máy `CC-…` cho thread bảo mật |
| Thử thật các mục 0.9.6–0.9.8 | Chưa làm (danh sách ở mục 8) |
| Số liệu chuẩn công ty cho 3 skill mẫu | Chờ cậu gửi tài liệu chuẩn. Kèm theo: bảng chữ theo tỷ lệ in cho MEP và hạ tầng |
| Thử key K-5 … K-16 | Chưa làm |
| Thử AutoCAD 2027 trên máy thật | Chưa làm |
| Ghi "Đã gỡ lúc" khi gỡ plugin | Để sau (nhánh `claude/device-management`) |
| Tách file `ChatControl.cs` (code khung chat quá lớn) | Để sau |
