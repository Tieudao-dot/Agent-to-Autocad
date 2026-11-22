# Tổng quan plugin ClaudeCAD 0.9.6

*Soạn ngày 22/11/2026, theo nhánh `claude/release-0.9.5` @ 5fe6adc (tên nhánh cũ, nội dung là 0.9.6).
Viết cho chủ plugin, không cần biết lập trình.*

## 1. Tóm tắt

| | |
| --- | --- |
| Phiên bản | **0.9.6**, bộ cài thử (CI xanh, `release: false`) |
| Trạng thái | Chưa gộp vào `main`, chưa phát hành. Bản đang phát hành cho khách là **0.9.5** (18/11/2026) |
| Máy chủ Google | Đã cập nhật sẵn cho 0.9.6 ngày 22/11. Khách đang dùng 0.9.5 vẫn chạy bình thường |
| AutoCAD | 2022–2027, một bộ cài chung. 2027 có trong danh sách nhưng chưa thử trên máy thật |

**Điểm mới so với 0.9.5:**

1. **Đẩy skill theo máy:** cậu gán skill cho từng mã máy trong Google Sheet, máy khách tự nhận. Danh sách Skill có nhãn
   **Admin / Default / You**.
2. **Tự kiểm bản vẽ trước khi báo xong:** Claude đo lại bản vẽ thật (14 mã lỗi), tự sửa tối đa 2 vòng, còn lỗi thì
   liệt kê. Có thẻ **Tự kiểm ✓ / ✗**.
3. **Bắt điểm theo đối tượng** và **áp chuẩn lúc vẽ** (không vẽ lên layer 0 / layer ngoài chuẩn, tự tạo layer chuẩn,
   chiều cao chữ theo tỷ lệ in).
4. **3 skill mẫu viết lại** và **tự cập nhật** trên máy đã cài, nếu khách chưa sửa.
5. Nâng cấp từ 0.9.5 **không phải kích hoạt lại**. Mỗi lượt tốn thêm khoảng 9 % hạn mức (danh sách lệnh dài hơn).

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

### 2.2 Mới ở 0.9.6: bắt điểm và áp chuẩn lúc vẽ

- **Bắt điểm theo đối tượng:** Claude đặt điểm bằng cách bám vào hình có sẵn (đầu mút, trung điểm, tâm, giao điểm,
  vuông góc, gần nhất, điểm chèn) thay vì tự tính tọa độ. Nhờ vậy cánh cửa, cung cửa, đầu tuyến khớp đúng. Có thêm
  lệnh `get_snap_points` để xem các điểm bắt được. Tọa độ `[x, y]` kiểu cũ, macro và mẫu lệnh đã lưu vẫn chạy như trước.
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
- **Tự kiểm ✓ / ✗** (mới), nút **Chọn đối tượng lỗi**.
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

- Một file `ClaudeCAD-Setup.exe` khoảng 140 MB, kèm sẵn Node.js và Claude Code. Không cần quyền admin.
- Bộ cài chưa ký số: Windows hiện *"Windows protected your PC"* thì bấm **More info → Run anyway**.
- Hai trang cần chú ý: **Kích hoạt** và **Đăng nhập Claude ngay** (trang cuối).
- Cài ngầm nhiều máy: `ClaudeCAD-Setup.exe /VERYSILENT /KEY=<key của đúng máy đó>`.
- Cập nhật: khung chat tự kiểm tra bản mới mỗi ngày; skill, macro, mẫu lệnh, lịch sử chat được giữ.

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
| Khóa máy | Đổi thành `Disabled`: máy bị khóa ở lần kiểm tra kế tiếp, khung chat hiện thẻ **🔒 tạm khóa**. Đổi lại `Active` là dùng tiếp, không cần key mới |
| Kiểm tra định kỳ | Mỗi lần mở AutoCAD và khoảng **6 giờ** một lần. Câu trả lời có chữ ký số |
| Mất mạng | Dùng tối đa **7 ngày** từ lần kiểm tra thành công gần nhất, rồi tự khóa. Vặn lùi đồng hồ cũng bị phát hiện |
| Máy không có mạng | Cậu chạy hàm `taoKeyThuCong` trong Apps Script: email về có KEY và **mã cho máy không có mạng** (2 dòng, dán cả hai). Máy này không bị khóa từ xa, không bị giới hạn 7 ngày; chỉ cấp cho máy thật sự không có mạng |

Lưu ý:

- Ô Trạng thái chỉ nhận `Active` / `Disabled`; gõ sai gì cũng coi là Chờ duyệt (máy bị khóa), nên không mở nhầm quyền.
- Tên máy, user Windows do máy kia tự khai: xác nhận người xin qua Zalo trước khi duyệt.
- Cập nhật, cài đè, gỡ ra cài lại trên cùng máy không hỏi lại key.

## 4. Skill

Skill là file `.md` mô tả chuẩn vẽ: bảng layer, chiều cao chữ, quy cách, quy trình. Claude vẽ theo skill đang dùng.

### 4.1 Thư viện và skill theo từng đoạn chat

- **Thư viện:** mọi skill đã có trên máy. Thêm bằng **＋ Add skill** (chọn một hoặc nhiều file); skill vào thư viện và
  dùng luôn ở đoạn chat hiện tại.
- **Mỗi đoạn chat có bộ skill riêng.** Nút ✦ **Skill (N)** có phần *Đang dùng* (nút **−**) và *Thư viện* (nút **+**),
  nút **Sửa** mở file. Đoạn chat mới dùng skill của đoạn chat gần nhất.
- Có thể nhờ *"tạo skill từ cuộc chat này"*: Claude soạn bản nháp để xem / sửa rồi thêm vào plugin.

### 4.2 Nhãn (mới ở 0.9.6)

| Nhãn | Nghĩa |
| --- | --- |
| **Admin** | Cậu gửi tới máy đó qua Google Sheet |
| **Default** | 3 skill mẫu đi kèm plugin |
| **You** | Khách tự thêm. Khách bấm Sửa skill Default / Admin thì plugin chép sang `<tên>-sua` (nhãn **You**), bản gốc giữ nguyên và vẫn nhận cập nhật. Khách sửa thẳng file gốc ngoài plugin cũng được tách sang `<tên>-sua` như vậy |

### 4.3 Đẩy skill theo máy (mới ở 0.9.6)

- Cậu nhập skill vào trang **Skill** của Sheet, rồi gán cho từng mã máy ở trang **Gán skill** với hành động
  **Thêm / Ẩn / Xóa** (chi tiết ở mục 6).
- Máy khách tự nhận ở lần kiểm tra trạng thái kế tiếp (mở AutoCAD, khoảng 6 giờ một lần, hoặc bấm **Kiểm tra lại**),
  có toast báo. Sửa Sheet xong có thể phải chờ tới 10 phút.
- Chỉ máy **Active** nhận skill. Mỗi máy tối đa **20 skill**, mỗi skill tối đa **45.000 ký tự**, tổng tối đa **400 KB**.
- Gói skill có **chữ ký số** (cùng cặp khóa với giấy phép) và **chống gửi lại gói cũ**. Gói sai thì bỏ cả gói, chỉ ghi
  log, không ảnh hưởng việc máy có được dùng hay không.
- Skill mới gửi tới **không tự bật**: khách bấm **+** để dùng.
- **Trùng tên** với skill của khách: giữ cả hai, bản của cậu nằm ở thư mục `<tên>-admin`.
- **Khách sửa skill Admin:** skill thành của khách (You), từ đó không nhận cập nhật, Ẩn hay Xóa từ Sheet nữa. Bản mới
  cậu gửi sau được lưu thêm bên cạnh.
- **Skill bị Ẩn:** biến khỏi danh sách, Claude không nạp nữa, kể cả trong đoạn chat đang dùng. Chọn lại **Thêm** là hiện
  lại ngay.
- Plugin 0.9.5 trở về trước không nhận skill, nhưng vẫn chạy bình thường.

### 4.4 Skill mẫu

| Skill | Dùng cho |
| --- | --- |
| `ve-kien-truc` | Mặt bằng kiến trúc: trục, cột, tường 220 / 110, cửa, kích thước, tên phòng |
| `ve-mep` | Điện (chiếu sáng, ổ cắm, máng cáp) và cấp thoát nước |
| `ve-ha-tang` | Đường, lý trình, cao độ, cống, ranh giới, bảng tọa độ |

- 0.9.6 viết lại cả 3: danh mục chuẩn bị trước khi vẽ, dựng hình bằng offset / trim và bắt điểm, tự kiểm bằng lệnh đo,
  lỗi hay gặp, mẫu báo cáo.
- **Số liệu vẫn là số mẫu**, chưa theo chuẩn công ty.
- Skill mẫu có sẵn trong thư viện nhưng **không tự dùng**.
- **Tự cập nhật:** lần mở AutoCAD đầu tiên sau khi cập nhật, skill mẫu khách **chưa sửa** được thay bằng bản mới. Đã
  sửa dù một chữ thì giữ nguyên; bản mẫu mới nằm ở `Contents\skills-mau` để so.
- Bảng chữ theo tỷ lệ in hiện chỉ có ở skill kiến trúc, nên kiểm chiều cao chữ / dim style chưa chạy với MEP và hạ tầng.

### 4.5 Skill tốt nên có

Quy trình chuẩn bị; danh mục trước khi vẽ (số đối tượng dự kiến theo layer); **Bảng layer chuẩn** đúng định dạng;
bảng **Chữ và kích thước theo tỷ lệ in**; cách dựng hình bằng offset / trim / bắt điểm; ví dụ hoàn chỉnh; mục kiểm
tra trước khi báo xong (có thể ghi `Ngưỡng khe hở: <số> mm`); lỗi hay gặp; mẫu báo cáo cuối lượt. Chi tiết:
`skills/README.md`.

## 5. Tự kiểm bản vẽ (mới ở 0.9.6)

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

4 mã cuối chỉ chạy khi skill đang dùng có bảng chuẩn. Layer người dùng tự nêu tên hoặc chọn bằng nút Layer thì không bị
báo. Thiết kế đầy đủ: `docs/ke-hoach-tu-kiem.md`.

## 6. Máy chủ Google

Máy chủ kích hoạt là một **Google Apps Script gắn với Google Sheet "ClaudeCAD – Thiết bị"**, chạy trên tài khoản
Google của cậu.

### 6.1 Các trang trong Sheet

| Trang | Cột | Cậu làm gì |
| --- | --- | --- |
| **Thiết bị** | Mã máy · Thiết bị · User Windows · Ngày xin · Ngày kích hoạt · Trạng thái · Lần cuối dùng · Phiên bản · Ghi chú | Duyệt (`Active`), khóa (`Disabled`), xem máy nào còn dùng. Xóa dòng thì máy về Chờ duyệt ở lần kiểm tra sau |
| **Skill** (0.9.6) | Tên · Phiên bản · Nội dung SKILL.md · Ghi chú | Mỗi dòng một skill. Dán **toàn bộ** nội dung file, cả phần đầu `name:` / `description:`. `name` trùng cột Tên. **Sửa nội dung thì tăng Phiên bản** |
| **Gán skill** (0.9.6) | Mã máy · Tên skill · Hành động · Ghi chú | Mỗi dòng một skill cho một máy. Hành động **Thêm / Ẩn / Xóa**. Để nguyên dòng trên Sheet; hai dòng trùng thì dòng dưới thắng |

- Dòng có lỗi (skill không có trong trang Skill, quá dài, thiếu description, tên sai…) không được gửi, lý do ghi ở cột
  **Ghi chú**.
- Xóa dòng gán thì máy **giữ nguyên** skill như lần nhận cuối, không xóa gì. Muốn gỡ thì chọn **Xóa**.
- ⚠️ Đừng đổi tên trang **Thiết bị**, đừng chèn / đổi thứ tự cột trước cột Ghi chú (máy chủ đọc cột theo vị trí).
- Chống spam: tối đa 30 máy mới mỗi giờ, 10 email mỗi giờ, 40 email mỗi ngày; tên máy gửi lên không chạy được như
  công thức trong bảng.
- Xem các lượt xin / kích hoạt / kiểm tra ở mục **Thực thi** (Executions) trong trình soạn Apps Script.

### 6.2 Hai file Apps Script (từ 0.9.6)

| File | Chứa gì | Khi nâng cấp |
| --- | --- | --- |
| `BiMat.gs` | Phần bí mật của máy chủ. Người phát triển tạo bằng lệnh kín và gửi riêng | **Không đụng tới.** Không chia sẻ, không đưa lên GitHub: ai có file này tự tạo được key |
| `Code.gs` | Phần xử lý, không chứa bí mật | Dán đè nguyên văn bản mới từ repo |

Nâng cấp máy chủ: thay `Code.gs` → Lưu → chạy hàm `kiemTra` một lần → **Triển khai → Quản lý triển khai → Phiên bản
mới**. Làm như vậy thì link máy chủ giữ nguyên. Đừng bấm "Tùy chọn triển khai mới": link sẽ đổi và bộ cài đã phát
hành không gọi được máy chủ.

Việc chuyển từ 1 file sang 2 file và cập nhật cho 0.9.6 **đã làm xong ngày 22/11**; kiểm tra `?action=ping` trả
`ready:true`.

## 7. Bảo mật và giới hạn

### 7.1 Những gì được bảo vệ

- **Giấy phép theo máy, có chữ ký số.** Chép thư mục plugin sang máy khác hoặc cài bằng `.zip` vẫn bị hỏi kích hoạt.
- **Trạng thái máy có chữ ký**, kiểm mỗi lần mở AutoCAD và ~6 giờ một lần; mất mạng tối đa 7 ngày.
- **Link máy chủ nằm sẵn trong plugin:** xóa file cấu hình không tắt được kiểm tra; link lạ không có chữ ký của cậu.
- **Chống vặn lùi đồng hồ** để kéo dài 7 ngày.
- **Gói skill có chữ ký**, chống gửi lại gói cũ, kiểm tên, nội dung, giới hạn kích thước.
- **API key của khách** lưu mã hóa theo tài khoản Windows. Báo cáo lỗi che key và mã đăng nhập.
- **Dữ liệu bản vẽ không phải lệnh:** chữ có sẵn trong bản vẽ không điều khiển được Claude.
- Claude chỉ được đọc thư mục skill.

### 7.2 Giới hạn đã biết

| Giới hạn | Ý nghĩa |
| --- | --- |
| Mã nguồn đang công khai | Người biết lập trình có thể sửa mã bỏ bước kiểm tra rồi tự build lại. Key + trạng thái chỉ chặn được người dùng thông thường. Muốn chặn cả trường hợp này thì phải để repo riêng tư |
| Người rành kỹ thuật | Có thể xóa mốc chống vặn đồng hồ |
| Ai sửa được bảng tính | Đẩy được chỉ dẫn cho Claude trên máy mọi khách và xem được bí mật trong Apps Script. Chia sẻ Sheet chỉ cho quyền **Người xem** |
| Mã máy không phải bí mật | Người biết mã máy của một máy Active tải được skill gán cho máy đó. **Không đưa mật khẩu, API key hay dữ liệu riêng của khách khác vào skill** |
| Mã cho máy không có mạng | Không khóa từ xa được; chỉ cấp khi thật cần |
| Cần mạng và hạn mức | Phụ thuộc hạn mức tài khoản Claude; mỗi lượt 0.9.6 tốn thêm ~9 % |
| Kiểm thật | Nhiều mục chỉ kiểm được trên AutoCAD thật (theme, thanh trạng thái, chọn đối tượng, kích hoạt, tự kiểm) |

## 8. Bộ cài thử 0.9.6

| | |
| --- | --- |
| Nguồn | Nhánh `claude/release-0.9.5` @ 5fe6adc (bộ cài thử lần 2, thay run 45011984540) |
| CI | Run [45019784560](https://github.com/Tieudao-dot/Agent-to-Autocad/actions/runs/45019784560), `release: false`, xanh |
| File tải | Artifact **ClaudeCAD-0.9.6** trên trang CI, cần đăng nhập GitHub, hết hạn **20/02/2027** |
| `Setup.exe` | 141.419.468 byte |
| SHA-256 | `2EE2E290DB5E8D7E39D4FF7D4A2809E7674B736A95B4C247351C0FE66BDBB4D6` |
| Test tự động | Lõi 264, trợ lý 186, build net48 / net8 / net10 đều đạt |
| Trạng thái | Chưa gộp `main`, chưa phát hành |

Kiểm file tải về: mở PowerShell trong thư mục chứa file, chạy `Get-FileHash .\<tên file>.exe`, so với mã trên.

### Cách thử

| Thử gì | Ở đâu |
| --- | --- |
| Đẩy skill theo máy (6 bước: Thêm → thấy nhãn Admin; tăng phiên bản → nhận bản mới; Ẩn / Thêm lại; Xóa; khách sửa → You; máy không được gán thì không nhận) | `docs/ke-hoach-day-skill.md` mục 6 |
| Tự kiểm, bắt điểm, áp chuẩn (I10–I13; cả phần I là I1–I13, ~4 phút) | `docs/checklist-thu-that.md` phần I |
| Nhãn Admin / Default / You trong danh sách Skill | Mở ✦ Skill sau khi đẩy skill |
| Skill mẫu tự cập nhật trên máy đang cài 0.9.5 (máy đã sửa thẳng skill mẫu: phần sửa sang `<tên>-sua`, skill mẫu về bản mới) | `docs/test-log.md` mục S-6, S-7 |
| Bấm Sửa skill Default → ra `<tên>-sua` (You), bản gốc còn nguyên | `docs/test-log.md` mục S-8 |
| Lượt thử thật đầy đủ (~20 phút): cài + kích hoạt, đăng nhập, vẽ, xác nhận, hoàn tác, giao diện, skill / macro, AutoCAD 2026 / 2027 | `docs/checklist-thu-that.md` phần A–H |

Ước tính thời gian cậu thử: khoảng 30–45 phút. Thử xong và cậu đồng ý thì thread gộp nhánh & phát hành mới merge `main`
(CI tự phát hành).

## 9. Lịch sử phiên bản

| Phiên bản | Nội dung chính | Trạng thái |
| --- | --- | --- |
| **0.9.6** | Đẩy skill theo máy, nhãn Admin / Default / You; tự kiểm bản vẽ 14 mã; bắt điểm; áp chuẩn lúc vẽ; 3 skill mẫu viết lại và tự cập nhật | Bộ cài thử, chưa phát hành |
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
| Thử thật bộ cài 0.9.6 rồi quyết phát hành | Chờ cậu thử (mục 8) và đồng ý rõ thì mới merge `main` |
| Ghi "Đã gỡ lúc" khi gỡ plugin | Để sau, không có trong 0.9.6 (nhánh `claude/device-management`) |
| Máy đã đổi `Disabled` vẫn dùng được | Đang tìm nguyên nhân. Cậu đóng hẳn rồi mở lại AutoCAD trên máy đó; nếu vẫn dùng được thì gửi mã máy `CC-…` cho thread bảo mật |
| Số liệu chuẩn công ty cho 3 skill mẫu | Chờ cậu gửi tài liệu chuẩn. Kèm theo: bảng chữ theo tỷ lệ in cho MEP và hạ tầng |
| Thử key K-5 … K-16 | Chưa làm |
| Thử AutoCAD 2027 trên máy thật | Chưa làm |
| Tách file `ChatControl.cs` (code khung chat quá lớn) | Để sau |
