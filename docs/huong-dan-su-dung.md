# Hướng dẫn sử dụng ClaudeCAD

Tài liệu dành cho người dùng AutoCAD: cài đặt, kích hoạt, cách ra yêu cầu, các tính năng chính và cách xử lý sự cố.
Áp dụng cho bản mới của plugin. Chữ trên một vài nút có thể khác chút ít giữa các bản.

## 1. ClaudeCAD là gì

ClaudeCAD thêm một khung chat vào AutoCAD. Bạn gõ yêu cầu bằng tiếng Việt, ví dụ *"vẽ mặt bằng phòng 4×6 m, tường 220,
cửa đi 900"*. Claude đọc bản vẽ đang mở rồi vẽ, sửa, đo hoặc trả lời ngay trên bản vẽ đó. Mọi thay đổi của một lượt chat
gộp thành một bước hoàn tác: gõ `U` một lần là bản vẽ quay lại như trước lượt đó.

**Cần có:**

| | |
| --- | --- |
| Máy tính | Windows |
| AutoCAD | 2022, 2023, 2024, 2025, 2026 và 2027 (thử nghiệm). Phải là **bản đầy đủ**: plugin **không chạy trên AutoCAD LT** |
| Mạng | Cần kết nối Internet khi dùng |
| Tài khoản AI | Tài khoản Claude Pro / Max (hoặc Team), hoặc API key Anthropic |
| Giấy phép | Máy phải được chủ plugin cho phép dùng (mục 2.2) |

Bộ cài khoảng 140 MB và đã kèm mọi thứ cần thiết. Không cần quyền quản trị (admin), không cần cài thêm phần mềm khác.

## 2. Cài đặt và kích hoạt

### 2.1 Cài đặt

1. Tải file `ClaudeCAD-Setup.exe` theo link chủ plugin gửi. Link này không đổi và luôn trỏ tới bản mới nhất.
2. Đóng AutoCAD.
3. Chạy file. Nếu Windows hiện *"Windows protected your PC"*: bấm **More info → Run anyway** (bộ cài chưa ký số).
4. Đi qua các trang của bộ cài. Hai trang cần chú ý là **Kích hoạt** và **Đăng nhập Claude** (bên dưới).

### 2.2 Kích hoạt máy

Mỗi máy có một **mã máy** dạng `CC-XXXX-XXXX-XXXX`. Plugin chỉ chạy khi chủ plugin đã cho phép máy đó. Ở trang
**Kích hoạt**, bộ cài hiện mã máy và tự gửi yêu cầu kích hoạt cho chủ plugin. Bạn chọn một trong hai cách:

- **Cách A — chờ duyệt (không cần key):** để trống ô key, bấm **Next** để cài tiếp, rồi báo mã máy cho chủ plugin. Khi đã
  được duyệt, lần mở AutoCAD tiếp theo là dùng được. Muốn dùng ngay thì bấm **Kiểm tra lại** trong khung chat.
- **Cách B — nhập key:** nhận key từ chủ plugin / người cấp key, dán vào ô **Key kích hoạt**, bấm **Next**.

Lưu ý:

- Key chỉ dùng được trên đúng máy có mã máy đó. Máy đã kích hoạt thì cài đè hoặc cập nhật không hỏi lại.
- Cài lại Windows hoặc thay mainboard thì mã máy đổi; máy phải xin duyệt lại như máy mới.
- Khi cần hỗ trợ, chỉ gửi **mã máy**. Đừng gửi key qua ảnh chụp hay tin nhắn.

**Thẻ 🔒 trong khung chat.** Máy chưa được kích hoạt thì khung chat hiện thẻ **🔒 Kích hoạt ClaudeCAD** và chưa cho gửi
tin. Trên thẻ có:

- **Mã máy** và nút **Sao chép** (chép mã để gửi cho chủ plugin);
- ô dán key và nút **Kích hoạt**;
- **Gửi yêu cầu kích hoạt**: gửi mã máy và tên thiết bị cho chủ plugin;
- **Kiểm tra lại**: hỏi lại xem chủ plugin đã duyệt chưa. Đã duyệt thì dùng được ngay, không cần key.

Plugin kiểm tra giấy phép mỗi lần mở AutoCAD và khoảng 6 giờ một lần. Nếu chủ plugin tạm khóa máy, hoặc máy quá 7 ngày
không kiểm tra được (ví dụ mất mạng lâu), khung chat hiện thẻ **🔒 ClaudeCAD tạm khóa**. Cách xử lý: xem mục 9.

### 2.3 Đăng nhập Claude

Trang cuối bộ cài có ô **Đăng nhập Claude ngay**. Giữ ô này rồi bấm **Finish** → trình duyệt mở trang cấp quyền → bấm
đồng ý bằng tài khoản Claude của bạn. Một cửa sổ đen hiện lên rồi tự đóng khi xong. Không cần mở AutoCAD.

Máy đã đăng nhập sẵn thì ô này không hiện. Bỏ qua bước này cũng được: lần đầu mở AutoCAD, plugin tự mở trang đăng nhập.
Có thể đăng nhập sau ở thẻ **Bắt đầu** hoặc ⚙ ▸ **Tài khoản AI** ▸ **Đăng nhập Claude**. Dùng API key: xem mục 7.

### 2.4 Thẻ "Bắt đầu" và tự kiểm tra

Lần đầu mở AutoCAD, khung chat hiện thẻ **Bắt đầu với ClaudeCAD — 3 bước**: ① Kích hoạt máy này → ② Đăng nhập Claude →
③ Thử một yêu cầu mẫu. Bước đã xong có dấu ✓, bước cần làm có sẵn nút. Ở bước 3, nút **Điền vào ô nhập** đưa một câu mẫu
vào ô nhập; bạn xem rồi nhấn Enter. Xem lại thẻ này: ⚙ ▸ **Hướng dẫn bắt đầu**.

Sau khi cài, nên gõ lệnh `CLAUDECHATSELFTEST`. Plugin tự kiểm tra trên một bản vẽ tạm (bản vẽ của bạn không bị đụng tới)
rồi mở báo cáo ✓ / ✗ trong Notepad. Dòng cuối *"Tất cả đều đạt."* là ổn.

### 2.5 Cài cho nhiều máy (dành cho IT)

Cài ngầm, không hỏi gì (key phải đúng của máy đó); mỗi máy vẫn cần đăng nhập Claude một lần:

```
ClaudeCAD-Setup.exe /VERYSILENT /KEY=XXXX-XXXX-XXXX-XXXX
```

## 3. Mở khung chat

Có ba cách:

- gõ lệnh `CC` hoặc `CLAUDECHAT`;
- nhấn **Ctrl+Shift+Space** khi đang ở bản vẽ;
- bấm icon ClaudeCAD trên thanh trạng thái (góc dưới bên phải cửa sổ AutoCAD).

Con trỏ được đặt sẵn ở ô nhập. Khung chat là một palette giống Properties: neo trái / phải, tự ẩn (auto-hide), đổi cỡ như
palette thường. Màu và font lấy theo theme của AutoCAD; đổi theme là khung chat đổi theo ngay.

### Các vùng của khung chat

| Vùng | Có gì |
| --- | --- |
| Hàng trên cùng | Bên trái: trạng thái kết nối và dòng mức dùng. Bên phải: ■ Dừng, ⤓ Xuất, ⊞ Chat mới, ⌄ Thu gọn, ⚙ Cài đặt |
| Thanh hạn mức | Tự hiện khi đã dùng từ 50% hạn mức, ví dụ "Hạn mức 5 giờ · đã dùng 41%", kèm giờ làm mới |
| Hàng thứ hai | **Tỷ lệ** (1:20 … 1:1000), ＋ Add skill, ✦ Skill, ≡ Layer, ▷ Macro, ✎ Ghi chú, ⟲ Phiên bản, nút **🔓 Sửa được / 🔒 Chỉ xem** |
| Vùng chat | Tin của bạn, câu trả lời của Claude, các thẻ xác nhận, kế hoạch, lỗi |
| Ô nhập | Gõ yêu cầu. Bên dưới có 4 icon: ⬚ Chọn đối tượng, ✛ Lấy điểm, ▨ Ảnh, ⛶ Chụp, và nút **Gửi** |

Rê chuột lên một icon để xem tooltip *"Tên — giải thích ngắn"*. Icon Macro, Phiên bản, Ghi chú đổi sang màu nhấn khi đã có
nội dung; số lượng nằm trong tooltip.

### Icon trên thanh trạng thái

| Icon | Nghĩa |
| --- | --- |
| Xám mờ | Chưa kết nối |
| Bình thường | Sẵn sàng |
| Có chấm xanh | Claude đang làm |
| Có chấm vàng | Cần bạn xem: chờ xác nhận, kế hoạch chờ duyệt, chưa đăng nhập, lượt bị lỗi, hết hạn mức |

Khung chat đang đóng hoặc tự ẩn mà Claude làm xong thì icon hiện bong bóng *"Claude đã xong: …"* có link **Mở khung chat**.
Tắt bong bóng: ⚙ ▸ **Báo khi lượt xong**.

### Mỗi bản vẽ một cuộc chat

Chuyển bản vẽ là chuyển cuộc chat. Mở lại AutoCAD, cuộc chat vẫn còn và Claude nhớ ngữ cảnh. Bản vẽ chưa lưu (Drawing1…)
không giữ lịch sử chat cho lần mở sau, nên lưu file DWG trước khi chat. ⊞ **Chat mới** mở đoạn chat mới cho bản vẽ; đoạn cũ
vẫn xem lại được.

## 4. Cách ra yêu cầu hiệu quả

Claude làm tốt nhất khi yêu cầu nói rõ: **vẽ gì, kích thước bao nhiêu, ở đâu, layer nào**.

| Chưa tốt | Tốt hơn |
| --- | --- |
| "Vẽ cái phòng" | "Vẽ mặt bằng phòng 4000×6000, tường 220, cửa đi 900 ở cạnh dài, ghi kích thước" |
| "Sửa mấy cái tường" | (chọn 2 tường bằng ⬚) "Chuyển mấy đối tượng này sang layer A-WALL" |
| "Thêm chữ vào" | "Ghi chữ PHÒNG KHÁCH cao 250 ở giữa phòng, layer A-TEXT" |
| "Xóa rác đi" | "Xóa toàn bộ đối tượng trên layer A-TEMP" |
| "Vẽ ống" | (bật skill `ve-mep`) "Vẽ đoạn ống nước lạnh dài 3000 bắt đầu từ điểm [12400, 5600]" |

Mẹo:

- **Đơn vị:** ghi số theo mm ("tường 220", "dài 3000"). Claude đọc được đơn vị của bản vẽ, nhưng nói rõ vẫn chắc hơn.
- **Layer:** nói rõ tên layer, hoặc dùng skill của công ty để Claude tự chọn layer đúng chuẩn (mục 5.6).
- **Tỷ lệ:** chọn **Tỷ lệ** in ở hàng thứ hai. Claude chỉnh chiều cao chữ và kích thước (Dim) theo tỷ lệ đó.
- **Chỉ đúng chỗ:** chọn đối tượng, lấy điểm hoặc chụp vùng đang xem, thay vì tả vị trí bằng lời.
- **Việc lớn:** Claude trình kế hoạch trước. Đọc kỹ các bước và kích thước, sửa nếu cần rồi mới bấm **Làm**.
- **Hỏi trước khi làm:** "bản vẽ có bao nhiêu đối tượng?", "đo diện tích phòng này". Câu hỏi chỉ đọc không sửa bản vẽ.
- Thuật ngữ AutoCAD giữ tiếng Anh vẫn được: Layer, Block, Hatch, Dim, Polyline, Xref.
- Câu mơ hồ thì Claude hỏi lại một câu; bạn trả lời ngắn để Claude làm tiếp.
- **Enter** để gửi, **Shift+Enter** để xuống dòng, **↑ / ↓** trong ô nhập để gọi lại tin đã gửi.

## 5. Tính năng chính

### 5.1 Vẽ, sửa, đo

Claude vẽ đường, polyline, cung; ghi chữ, kích thước, hatch; tạo block, layer, style; fillet / chamfer, di chuyển, xoay,
sao chép, xóa; zoom. Claude cũng đọc layer, đối tượng, đơn vị, rồi đo và đếm.
Ví dụ: *"Vẽ lưới trục 5x4 bước 4000"*, *"Đếm số block cửa đi trên layer A-DOOR"*.

Dưới mỗi câu trả lời có:

- **Xem kết quả (N)**: zoom tới và tô sáng phần vừa vẽ / sửa. Link tên đối tượng trong câu trả lời cũng bấm được.
- **Thay đổi: Thêm · Sửa · Xóa**: tách theo layer; bấm một dòng để zoom và tô sáng.
- **Thao tác (N)**: từng thao tác Claude đã làm, thời gian, lỗi nếu có.
- Nút gợi ý bước tiếp theo (ví dụ "Ghi kích thước các phòng"): bấm là gửi; chuột phải để sửa trước khi gửi.

Claude biết phần bạn sửa tay giữa các lượt: khung chat hiện dòng *"✎ Bản vẽ đã được sửa ngoài khung chat…"* và Claude đọc
lại các đối tượng đó trước khi làm tiếp. Sau việc vẽ lớn, Claude có thể tự chụp vùng vừa vẽ để soát lỗi (chữ đè, ký hiệu
lệch) rồi sửa. Tắt: ⚙ ▸ **Tự kiểm tra bằng ảnh**.

### 5.2 Chọn đối tượng và lấy điểm

- ⬚ **Chọn đối tượng**: quét chọn trên bản vẽ → chip *"Kèm N đối tượng"* được gửi kèm tin nhắn. Đã chọn sẵn trên bản vẽ
  thì chip tự hiện. Bấm ✕ trên chip để không gửi kèm.
- Dưới chip là danh sách layer của vùng chọn, ví dụ ☑ A-WALL 120 · ☑ A-DOOR 8. Bỏ tick layer nào thì đối tượng trên layer
  đó không được gửi. **→ Cả layer**: dùng mọi đối tượng trên các layer đã tick.
- ✛ **Lấy điểm**: bấm một hoặc nhiều điểm trên bản vẽ, Enter để xong. Tọa độ như `[12400, 5600]` được chèn vào ô nhập.

Ví dụ: chọn 2 tường → *"đổi mấy cái này sang màu đỏ"*. Lấy 1 điểm → *"vẽ cột 400×400 tại [12400, 5600]"*.

### 5.3 Mẫu lệnh `/` và gọi tên `@`

- Gõ `/` → danh sách mẫu lệnh, ví dụ `/luoi-truc` (lưới trục), `/mat-bang` (mặt bằng phòng), `/dien-tich` (đo diện tích),
  `/bang-toa-do` (bảng tọa độ mốc), `/kiem-tra` (kiểm tra bản vẽ). Chọn bằng ↑↓, Enter để chèn. Giá trị đầu tiên được bôi
  đen để bạn sửa ngay.
- Lưu câu hay dùng thành mẫu: bấm **☆ Mẫu** trên tin nhắn của bạn.
- Gõ `@` → danh sách layer, block, kiểu chữ, kiểu kích thước, layout của bản vẽ. Enter để chèn đúng tên, không lo gõ sai.

Ví dụ: *"Chuyển các đối tượng đang chọn sang layer @A-DOOR"*.

### 5.4 Ảnh phác thảo, chụp vùng, bảng Excel

- ▨ **Ảnh**: đính kèm ảnh phác thảo, ảnh scan. Cũng dán được bằng **Ctrl+V** hoặc kéo thả file ảnh vào khung chat. Tối đa
  5 ảnh một lần.
- ⛶ **Chụp**: gửi kèm vùng bản vẽ đang xem, để chỉ cho Claude "chỗ này sai".
- **Bảng Excel**: copy vùng ô trong Excel → Ctrl+V vào ô nhập → thành bảng gửi cho Claude.

Ví dụ: đính ảnh phác tay → *"Vẽ lại mặt bằng theo ảnh này, tường 220, cửa đi 900"*.

### 5.5 Chọn layer được sửa

≡ **Layer** mở danh sách mọi layer của bản vẽ: lọc theo tên, màu, số đối tượng, trạng thái (tắt / đóng băng / khóa). Tick
layer cần sửa → **Dùng layer đã chọn** → chip *"Layer: …"* được gửi kèm tin nhắn tiếp theo. Claude hiểu theo câu:

- "đổi màu sang đỏ", "khóa lại", "tắt đi", "đổi tên thành…" → sửa **thuộc tính của layer**;
- "tăng chiều cao chữ lên 300", "dời sang phải 500", "xóa hết" → sửa **các đối tượng trên layer** (quá 20 đối tượng thì
  vẫn hỏi xác nhận).

### 5.6 Skill — chuẩn vẽ của công ty

Skill là file `.md` mô tả chuẩn vẽ: bảng layer, chiều cao chữ, quy cách, quy trình. Claude vẽ theo skill đang dùng.

- Có sẵn 3 skill mẫu: `ve-kien-truc` (mặt bằng kiến trúc), `ve-mep` (điện, cấp thoát nước), `ve-ha-tang` (đường, lý trình,
  cao độ, cống, bảng tọa độ). Skill mẫu nằm trong thư viện nhưng **không tự dùng**: bấm + khi cần.
- ✦ **Skill (N)**: phần *Đang dùng* (nút **−** để bỏ) và *Thư viện* (nút **+** để dùng); **Sửa** để mở file skill.
- Mỗi đoạn chat có bộ skill riêng. Đoạn chat mới dùng skill của đoạn chat gần nhất.
- ＋ **Add skill**: chọn một hoặc nhiều file `.md` (giữ Ctrl / Shift). Skill vào thư viện và được dùng luôn ở đoạn chat này.

Ví dụ: bật `ve-mep`, bỏ `ve-kien-truc` → *"vẽ 1 đoạn ống nước lạnh dài 3000"*.

**Viết skill cho công ty.** Soạn file `.md` bằng Notepad, lưu dạng **UTF-8**, mở đầu như sau:

```markdown
---
name: ve-ket-cau
description: Chuẩn vẽ kết cấu của công ty - layer S-*, dầm, cột, móng. Dùng khi được yêu cầu vẽ kết cấu, dầm, cột, móng, sàn, thép.
---

# Vẽ kết cấu theo chuẩn
...
```

- `name`: chữ thường không dấu, số, gạch nối. Bỏ trống thì plugin lấy theo tên file.
- `description` (bắt buộc, 20–1024 ký tự): ghi rõ **khi nào dùng** skill và các từ khóa. Claude dựa vào dòng này để chọn.
- Phần thân: bảng layer, chiều cao chữ, quy cách. Nên có mục *"Quy trình chuẩn bị"*: xem thông tin bản vẽ và danh sách
  layer trước, tạo layer / style còn thiếu theo chuẩn, rồi mới vẽ. Để Claude ít lỗi vặt, thêm các mục *"Danh mục trước
  khi vẽ"*, *"Kiểm tra trước khi báo xong"* (lệnh đo cụ thể cho layer, điểm khớp, chữ, thiếu / thừa) và *"Lỗi hay gặp"*.
  Danh sách mục đầy đủ ở `skills/README.md`; mở skill mẫu `ve-kien-truc` để tham khảo.
- Chia sẻ file cho đồng nghiệp; mỗi người thêm bằng ＋ Add skill. Trùng tên skill đã có thì plugin hỏi có thay không.
- Gõ *"tạo skill từ cuộc chat này"*: Claude soạn bản nháp, có nút **Xem / sửa** và **Thêm vào plugin**.
- Bỏ hẳn một skill khỏi thư viện: xóa thư mục mang tên skill đó trong
  `%AppData%\Autodesk\ApplicationPlugins\ClaudeCAD.bundle\Contents\skills\`.

### 5.7 Ghi chú dự án

✎ **Ghi chú** mở hai ô: **Dự án** (dùng chung cho mọi bản vẽ trong cùng thư mục, lưu thành file `ClaudeCAD-ghichu.md`
cạnh file DWG; cần lưu bản vẽ trước) và **Riêng bản vẽ này**. Ghi chú được gửi kèm mọi tin nhắn, tối đa 4000 ký tự mỗi ô.
Khi ghi chú và skill mâu thuẫn, Claude theo ghi chú.

Ví dụ: *"Tường ngoài 220, tường trong 110. Cao độ sàn tầng 1 là ±0.000. Chữ tên phòng cao 300."*

### 5.8 Macro — chạy lại không cần Claude

Sau một lượt vẽ ưng ý, bấm **💾 Lưu macro** dưới câu trả lời và đặt tên. Mở ▷ **Macro**: **▶ Chạy** (vẽ lại đúng tọa độ
cũ), **📍 Tại điểm** (chọn điểm chèn, macro được dời tới đó), **Xóa**, **Mở thư mục**. Macro chạy không cần Claude nên
không tốn hạn mức. Một lần `U` hoàn tác cả macro. Macro không chạy khi bật Chỉ xem.

Ví dụ: lưu macro "ký hiệu trục" một lần, sau đó chèn ở nhiều vị trí bằng **📍 Tại điểm**.

### 5.9 Thu gọn khung chat

Bấm **⌄** để palette co lại, chỉ còn hàng công cụ và ô nhập một dòng; Enter là gửi.

- Lượt xong hiện dòng *"✓ Đã xong: … · bấm để xem"* (lỗi thì *"⊗ …"*). Bấm dòng đó hoặc **⌃** để mở rộng; đoạn chat còn nguyên.
- Khi Claude cần bạn quyết (xác nhận, kế hoạch, đăng nhập, kích hoạt), palette tự mở rộng.
- Trạng thái thu gọn không được nhớ cho lần mở AutoCAD sau.

### 5.10 Theo dõi tiến độ, gửi thêm ý, dừng

- **Tiến độ:** khi Claude đang làm, khung chat hiện dòng *"● Đang làm · 1 phút 12 giây · 5 thao tác — Esc để dừng"* và
  danh sách bước: ✓ xong, ● đang chạy, ⚠ lỗi. Đã bấm **Làm** trên thẻ kế hoạch thì thấy *"Bước 3/7: …"*, kèm ○ cho bước
  chưa làm.
- **Gửi thêm ý:** cứ gõ rồi Enter (nút Gửi đổi thành **Gửi thêm**). Tin chờ ở hàng *"⏳ Chờ gửi khi Claude xong lượt
  này…"* (có nút **Bỏ**) và tự gửi khi lượt xong. Claude không đọc tin này giữa chừng. Nếu lượt bị lỗi, bị dừng, hết hạn
  mức hoặc Claude đang hỏi "đồng ý?", chữ được trả về ô nhập để bạn tự quyết.
- **Dừng:** nhấn **Esc** khi con trỏ đang ở ô nhập, hoặc bấm **■**. Esc trong bản vẽ vẫn là Esc của AutoCAD. Phần đã vẽ
  xong vẫn giữ; gõ `U` một lần nếu muốn bỏ.

Ví dụ: trong lúc Claude vẽ mặt bằng, gõ *"thêm cửa sổ 1200 ở tường phía nam"* rồi Enter.

### 5.11 Tiện ích khác

- **Sửa** trên tin nhắn cuối: sửa câu rồi gửi lại. Nếu lượt trước đã sửa bản vẽ, plugin tự hoàn tác lượt đó trước.
- **Copy** từng tin; ⤓ **Xuất** cuộc chat ra file `.md`. Thẻ lỗi có nút **Thử lại** gửi lại đúng yêu cầu vừa rồi.
- Cỡ chữ: **A− / A+** trong ⚙, hoặc Ctrl + lăn chuột.

## 6. An toàn bản vẽ

| Cơ chế | Cách hoạt động |
| --- | --- |
| Thẻ xác nhận | Trước khi xóa theo điều kiện hoặc sửa hơn 20 đối tượng, Claude dừng lại hỏi. Thẻ ghi to số đối tượng bị ảnh hưởng. **⌖ Xem trên bản vẽ** chọn sẵn các đối tượng đó để bạn nhìn. Bấm **Đồng ý** hoặc **Hủy** |
| Kế hoạch nhiều bước | Việc lớn (cả mặt bằng, cả hệ MEP, khoảng trên 50 đối tượng): Claude trình thẻ **Kế hoạch** với các bước, kích thước, giả định. Chọn **Làm**, **Sửa kế hoạch** hoặc **Bỏ** |
| Hoàn tác cả lượt | Mọi thay đổi của một lượt là một bước hoàn tác. Gõ `U` một lần, hoặc bấm **Hoàn tác lượt này** dưới câu trả lời mới nhất. Lượt chỉ hỏi / đo không chiếm bước U |
| Phiên bản DWG | Ngay trước khi chat sửa bản vẽ, nếu bản sao gần nhất đã cũ hơn 30 phút, plugin lưu một bản sao DWG. Giữ 10 bản gần nhất. Bản vẽ đang mở không bị đụng tới |
| Chỉ xem | Bấm **🔓 Sửa được** để chuyển thành **🔒 Chỉ xem**. Claude chỉ đọc, đo, trả lời; không sửa bản vẽ. Lưu riêng cho từng bản vẽ |
| Chữ trong bản vẽ không phải lệnh | Nội dung có sẵn trong bản vẽ (chữ, thuộc tính…) không bao giờ được coi là lệnh cho Claude |

- Nút **Hoàn tác lượt này** tự ẩn khi bạn chạy lệnh AutoCAD khác, vì lúc đó `U` không còn đúng lượt chat.
- Thẻ kế hoạch tắt được ở ⚙ ▸ **Cách Claude làm việc** ▸ **Kế hoạch trước việc lớn**. Nên để bật.

**Xem phiên bản cũ.** ⟲ **Phiên bản** → **Xem**: bản sao mở trong một tab chỉ xem, ô chat bị khóa trong lúc xem. Bấm
**Đóng — về bản hiện tại** để quay lại. Phiên bản chỉ được lưu khi **chat** sửa bản vẽ (sửa tay không tạo phiên bản) và
không có cho bản vẽ chưa lưu (Drawing1…). Bật / tắt: ⚙ ▸ **An toàn bản vẽ** ▸ **Lưu phiên bản trước khi sửa**.

**Lời khuyên trước khi lưu bản vẽ:**

1. Lưu file DWG trước khi giao việc lớn, để có phiên bản dự phòng và giữ lịch sử chat.
2. Sau mỗi lượt, xem dòng **Thay đổi** và bấm **Xem kết quả** để kiểm tra đúng chỗ, đúng layer.
3. Đọc kỹ thẻ xác nhận: số đối tượng có hợp lý không. Chưa chắc thì bấm **⌖ Xem trên bản vẽ**.
4. Kiểm tra lại các kích thước quan trọng bằng lệnh đo của AutoCAD, hoặc nhờ Claude đo lại.
5. Thấy sai thì `U` ngay, trước khi chạy lệnh khác.
6. Chỉ cần hỏi đáp trên bản vẽ quan trọng thì bật **Chỉ xem**.
7. Claude có thể nhầm. Bạn vẫn là người kiểm tra và chịu trách nhiệm cuối cùng với bản vẽ.

## 7. Tài khoản và hạn mức

**Xem hạn mức**

- Dòng nhỏ ở hàng trên cùng: mức dùng của lượt vừa rồi, chi phí ước tính của phiên, % hạn mức của gói.
- Thanh hạn mức tự hiện khi đã dùng từ 50%: *"Hạn mức 5 giờ · đã dùng 41%"* và giờ làm mới.
- Từ khoảng 80%: có thông báo nhỏ cảnh báo sắp hết.

**Khi hết hạn mức**, cuộc chat hiện một thẻ ghi loại hạn mức (5 giờ hoặc tuần), giờ làm mới và đếm ngược. Bạn có thể:

- bấm **Tự gửi lại lúc HH:mm**: plugin tự gửi lại yêu cầu vừa rồi khoảng 1 phút sau khi hạn mức làm mới (cứ để AutoCAD
  mở). Gửi tin mới hoặc bấm Chat mới thì việc tự gửi lại bị hủy;
- bấm **Dùng API key**: dùng API key trả theo lượng dùng, không phụ thuộc hạn mức gói;
- trong lúc chờ, vẫn chạy được macro đã lưu (không tốn hạn mức).

**Tiết kiệm hạn mức:** dùng macro cho việc lặp lại; chọn **Mức suy nghĩ** thấp cho việc đơn giản; tắt **Tự kiểm tra bằng
ảnh** nếu không cần (mỗi ảnh tốn thêm một ít).

**⚙ ▸ Tài khoản AI**

- *"Đang dùng: …"*: tài khoản đang dùng.
- **Đăng nhập Claude**: đăng nhập tài khoản Claude trên trình duyệt.
- **Đổi tài khoản**: đăng xuất rồi đăng nhập tài khoản Claude khác.
- **Đăng xuất**: đăng xuất tài khoản Claude trên máy này.
- **Dùng API key**: dán API key Anthropic (tạo ở console.anthropic.com, trả theo lượng dùng). Key được lưu mã hóa cho riêng
  tài khoản Windows của bạn và được ưu tiên hơn tài khoản Claude. **Xóa key đã lưu** để quay về tài khoản Claude.

Plugin chỉ dùng được tài khoản Claude hoặc API key Anthropic. Chọn model và **Mức suy nghĩ** ở ⚙ ▸ **Cách Claude làm
việc**: mức càng cao thì Claude cân nhắc càng kỹ, nhưng chậm hơn và tốn hạn mức hơn.

## 8. Cập nhật và gỡ cài đặt

**Cập nhật**

- Khung chat tự kiểm tra bản mới mỗi ngày và báo trong chat.
- Gõ `CLAUDECHATUPDATE` để tải. Bản mới được cài khi bạn đóng AutoCAD. Hoặc tải lại `ClaudeCAD-Setup.exe` và chạy đè.
- Skill bạn đã thêm / sửa, macro, mẫu lệnh, lịch sử chat đều được giữ nguyên. Không phải nhập lại key.
- Ba skill mẫu đi kèm plugin: nếu bạn chưa sửa thì được thay bằng bản mẫu mới ở lần mở AutoCAD đầu tiên sau khi cập nhật; đã sửa thì giữ nguyên bản của bạn.

**Gỡ cài đặt:** đóng AutoCAD, vào Windows **Settings → Apps → ClaudeCAD for AutoCAD → Uninstall**.

- Nếu máy có mạng lúc gỡ, chủ plugin thấy máy đã gỡ và giờ gỡ. Việc này chỉ để ghi chú, không khóa máy; cài lại thì máy
  dùng tiếp như trước. Máy không có mạng vẫn gỡ bình thường.
- Dữ liệu của bạn (lịch sử chat, macro, mẫu lệnh) vẫn nằm trong `%LocalAppData%\ClaudeCAD` và `%AppData%\ClaudeCAD`. Xóa
  tay nếu không cần nữa.

## 9. Xử lý sự cố

| Hiện tượng | Cách làm |
| --- | --- |
| Không thấy khung chat | Gõ `CC`. Khung chat đang tự ẩn hoặc đã đóng: bấm icon ClaudeCAD ở thanh trạng thái. Ctrl+Shift+Space không chạy: bật lại ở ⚙ ▸ **Phím tắt Ctrl+Shift+Space** |
| AutoCAD báo không có lệnh `CC` / `CLAUDECHAT` | Plugin chưa được nạp. Kiểm tra AutoCAD là bản đầy đủ 2022–2027 (không phải LT). Đóng AutoCAD, chạy lại bộ cài, mở lại AutoCAD |
| Windows chặn bộ cài ("Windows protected your PC") | Bấm **More info → Run anyway** |
| Bộ cài báo "The source file is corrupted" | File tải về bị hỏng. Tải lại vào ổ của máy (không chạy từ ổ mạng / USB), hoặc tạm tắt phần mềm diệt virus khi cài |
| Thẻ **🔒 Kích hoạt ClaudeCAD**, không gửi được tin | Máy chưa được duyệt. Bấm **Sao chép** mã máy, gửi cho chủ plugin. Được duyệt thì bấm **Kiểm tra lại**; hoặc dán key rồi bấm **Kích hoạt** |
| Thẻ **🔒 ClaudeCAD tạm khóa**: máy bị chủ plugin tạm khóa | Liên hệ chủ plugin / người cấp key. Khi được mở lại, bấm **Kiểm tra lại** là dùng tiếp ngay, không cần key mới |
| Thẻ 🔒: quá 7 ngày không kiểm tra được với chủ plugin | Kết nối mạng rồi bấm **Kiểm tra lại** |
| Thẻ 🔒: đồng hồ máy chậm hơn lần dùng trước | Chỉnh lại ngày giờ Windows cho đúng, kết nối mạng, bấm **Kiểm tra lại** |
| Key báo sai | Key chỉ dùng trên máy có đúng mã máy đó. Cài lại Windows / thay mainboard thì mã máy đổi: xin duyệt lại |
| Chưa đăng nhập / lỗi đăng nhập | ⚙ ▸ **Tài khoản AI** ▸ **Đăng nhập Claude**, đồng ý trên trình duyệt |
| Trợ lý không phản hồi, icon xám, đứng mãi ở "Đang khởi động..." | Kiểm tra mạng. Gõ `CLAUDECHATRESTART` để khởi động lại trợ lý, rồi gửi lại yêu cầu (hoặc bấm **Thử lại** trên thẻ lỗi) |
| Hết hạn mức | Chờ giờ làm mới (**Tự gửi lại lúc …**) hoặc **Dùng API key**. Xem mục 7 |
| Kết quả sai | Gõ `U` một lần hoặc bấm **Hoàn tác lượt này**, rồi nói lại rõ hơn (kích thước, layer, vị trí). Hoặc bấm **Sửa** trên tin nhắn cuối: plugin tự hoàn tác lượt trước rồi gửi lại. Đã lỡ chạy nhiều lệnh khác: xem bản cũ ở ⟲ **Phiên bản** |
| Claude không theo đúng chuẩn công ty | Mở ✦ **Skill**, kiểm tra skill đúng đang ở phần *Đang dùng*, bỏ skill không liên quan. Vẫn lẫn chuẩn cũ thì bấm ⊞ **Chat mới** |
| ⟲ Phiên bản trống | Bản vẽ chưa lưu (Drawing1), mới chỉ sửa tay, hoặc tính năng đang tắt ở ⚙ ▸ **An toàn bản vẽ** |
| Mở lại AutoCAD thì mất lịch sử chat | Bản vẽ chưa lưu không giữ lịch sử. Lưu bản vẽ thành file DWG trước khi chat |
| Icon trên khung chat hiện ô vuông trống | Rê chuột để xem tooltip tên nút. Báo người hỗ trợ kèm ảnh chụp |

### Gửi báo cáo lỗi

1. Gõ `CLAUDECHATSELFTEST` trước, để báo cáo có kết quả tự kiểm tra mới nhất. Có dòng ✗ thì chép lại nguyên dòng.
2. Bấm ⚙ ▸ **Trợ giúp** ▸ **Gửi báo cáo lỗi** (hoặc nút cùng tên trên thẻ lỗi). File
   `ClaudeCAD-baocao-<tên máy>-<ngày giờ>.zip` hiện trên Desktop, Explorer mở sẵn.
3. Gửi file đó cho người hỗ trợ, kèm một dòng mô tả (đã làm gì, thấy gì) và ảnh chụp cả cửa sổ AutoCAD (Win+Shift+S),
   thấy được khung chat và thanh trạng thái.
4. Khung chat không mở được: gõ `CLAUDECHATLOG`, nén cả thư mục log vừa mở rồi gửi.
5. Lỗi khi cài / kích hoạt: chụp trang bộ cài đang đứng và chữ báo lỗi. Chỉ gửi mã máy `CC-…`, không gửi key.

File báo cáo gồm: thông tin máy / AutoCAD / plugin, trạng thái trợ lý và tài khoản, log 3 ngày gần đây, kết quả tự kiểm
tra, log cài đặt. File **không** có lịch sử chat, bản vẽ hay ghi chú dự án; API key và mã đăng nhập trong log được che.

## 10. Dữ liệu và quyền riêng tư

**Lưu trên máy của bạn**

| Dữ liệu | Nơi lưu |
| --- | --- |
| Lịch sử chat, bản sao phiên bản DWG, biên bản theo dõi sửa tay | `%LocalAppData%\ClaudeCAD` |
| Cài đặt, macro, mẫu lệnh riêng | `%AppData%\ClaudeCAD` |
| Skill | `%AppData%\Autodesk\ApplicationPlugins\ClaudeCAD.bundle\Contents\skills\` |
| Ghi chú dự án (ô Dự án) | File `ClaudeCAD-ghichu.md` cạnh file DWG |
| API key (nếu dùng) | Lưu mã hóa, chỉ tài khoản Windows của bạn đọc được |

Biên bản theo dõi sửa tay không ghi gì vào file DWG và tự xóa sau 90 ngày không dùng. Nếu bật ⚙ ▸ **Biên bản cạnh bản
vẽ** (mặc định tắt), plugin lưu thêm thư mục ẩn `.claudecad` cạnh file DWG, để máy khác có cài plugin cũng biết bản vẽ đã
được sửa tay.

**Gửi cho Claude qua mạng.** Chỉ nội dung yêu cầu và dữ liệu bản vẽ cần cho lượt đó được gửi tới Claude:

- câu bạn gõ, cùng ảnh bạn đính kèm hoặc vùng bạn chụp;
- dữ liệu bản vẽ Claude cần đọc cho lượt đó: thông tin bản vẽ, layer, đối tượng đang chọn, tóm tắt phần bạn sửa tay;
- ghi chú dự án, skill đang dùng, tỷ lệ đang chọn;
- ảnh vùng vừa vẽ mà Claude tự chụp để soát lỗi (khi bật **Tự kiểm tra bằng ảnh**).

Chế độ Chỉ xem chỉ ngăn sửa bản vẽ; Claude vẫn đọc dữ liệu bản vẽ để trả lời.

**Chủ plugin thấy gì.** Để quản lý giấy phép, chủ plugin thấy: tên máy, tên người dùng Windows, mã máy, lần cuối dùng,
phiên bản plugin, và giờ gỡ cài đặt nếu có.

## 11. Bảng lệnh tóm tắt

| Lệnh / phím | Việc |
| --- | --- |
| `CC` hoặc `CLAUDECHAT` | Mở khung chat |
| **Ctrl+Shift+Space** | Mở khung chat, con trỏ ở ô nhập |
| `CLAUDECHATRESTART` | Khởi động lại trợ lý khi không phản hồi |
| `CLAUDECHATUPDATE` | Tải bản mới (cài khi đóng AutoCAD) |
| `CLAUDECHATSELFTEST` | Tự kiểm tra; báo cáo ✓ / ✗ mở trong Notepad |
| `CLAUDECHATLOG` | Mở thư mục log |
| `U` | Hoàn tác cả lượt chat gần nhất (hoặc cả một lần chạy macro) |
| Enter / Shift+Enter | Gửi (khi Claude đang làm: Gửi thêm) / xuống dòng |
| Esc (con trỏ ở ô nhập) | Dừng lượt đang chạy |
| ↑ / ↓ trong ô nhập | Gọi lại tin đã gửi |
| `/` · `@` | Chọn mẫu lệnh · chèn tên layer, block, kiểu chữ, kiểu kích thước, layout |
| Ctrl+V | Dán ảnh hoặc bảng Excel |
| Ctrl + lăn chuột | Đổi cỡ chữ |
