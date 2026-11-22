# Tổng quan dự án ClaudeCAD

*Cập nhật: 18/11/2026 (giờ Việt Nam). Luồng chính cập nhật mục "Tình hình" và "Nhật ký" khi có mốc mới.*

File này là tổng quan duy nhất của dự án. Nó gộp các file cũ `plugin-tong-quan.md` và `ke-hoach-gop-nhanh.md`.

## 1. Tình hình

- **Bản đang phát hành: v0.9.5** (18/11/2026, khoảng 21h giờ Việt Nam).
  - Trang tải: [Releases v0.9.5](https://github.com/Tieudao-dot/Agent-to-Autocad/releases/tag/v0.9.5). Gồm `ClaudeCAD-Setup.exe` (~141 MB) và `ClaudeCAD-0.9.5.zip`.
  - Cậu đã merge PR [Tieudao-dot/Agent-to-Autocad#5](https://github.com/Tieudao-dot/Agent-to-Autocad/pull/5). CI tự phát hành, run 44319064734 xanh.
  - Nhận AutoCAD 2022–2027. 2027 được ghi là hỗ trợ theo lựa chọn của cậu, nhưng chưa thử trên AutoCAD 2027 thật.
- **Nội dung so với v0.9.3:**
  - khóa key theo máy + quản lý thiết bị qua Google Sheet;
  - giao diện kiểu AutoCAD;
  - AutoCAD 2026 / 2027;
  - ~30 bản sửa sau rà soát;
  - sửa lỗi `install.ps1`.
- **Code khớp bản đã thử:** code của v0.9.5 giống hệt bộ cài 11/11 cậu đã thử ổn ngày 17/11. Chỉ khác số phiên bản.
- **Người dùng v0.9.3:** sẽ được báo cập nhật lên 0.9.5. Sau khi cập nhật phải kích hoạt máy, vì 0.9.3 chưa khóa máy. Cậu cần sẵn sàng duyệt máy trong Sheet "Thiết bị" hoặc gửi key.
- **Client đang dùng bộ cài 11/11 (0.9.4):** sẽ thấy thông báo cập nhật lên 0.9.5. Bản 0.9.5 giống hệt về chức năng; cập nhật không phải nhập lại key.
- **Để sau: BiMat + ghi "Đã gỡ lúc".** Lưu ở nhánh `claude/device-management` (`53bb1bf`). Không có trong v0.9.5. Máy chủ Google chưa cập nhật và không cần cập nhật.
- **Sổ bộ cài:** [`bo-cai/luu-tru-bo-cai.md`](../bo-cai/luu-tru-bo-cai.md), do thread lưu trữ bộ cài giữ.

## 2. Việc còn mở

0. **Máy đã đổi Disabled vẫn dùng được (18/11):** thread bảo mật đã kiểm, máy chủ trả trạng thái đúng. Lỗi nghi ở phía máy khách. Thread cần cậu gửi:
   - mã máy `CC-…` của máy đó;
   - nếu đóng / mở lại AutoCAD mà vẫn dùng được: bộ cài tải từ đâu, ngày nào.

   Lưu ý: plugin kiểm tra trạng thái khi mở AutoCAD và 6 giờ một lần, nên thường phải mở lại AutoCAD thì Disabled mới có hiệu lực.
1. **Các mục key ngoài 20 phút thử:** chưa thử.
   - mạng: K-5, K-6;
   - chỉnh đồng hồ: K-7;
   - chép giấy phép sang máy khác và các mục còn lại: K-11 … K-16.
2. **AutoCAD 2027:** đã ghi là hỗ trợ nhưng chưa thử trên máy thật. Nên thử khi có máy hoặc bản dùng thử.
3. **BiMat + ghi "Đã gỡ lúc":** triển khai khi cậu muốn. Cách làm: cập nhật máy chủ Google ở thread bảo mật, rồi gộp vào bản sau.
4. **Đẩy skill theo máy (22/11):** code xong ở `claude/day-skill-theo-may` @874d2e4, rà bảo mật xong, không có lỗi chặn. Bước triển khai Apps Script chờ thread bảo mật tách `BiMat.gs`. Sau đó gộp vào 0.9.6 (thread gộp nhánh). Hai commit cuối chưa chạy CI.
5. **Tự kiểm trước khi báo xong:** code xong ở `claude/tu-kiem-tra` @eecf3a9, CI xanh. Ngày 22/11 cậu giao thêm cho thread này gói "Công cụ vẽ & tự kiểm" do thread nghiên cứu Skill đề xuất: bắt điểm theo đối tượng, verify 12 mã, công cụ vẽ tự áp chuẩn. Thread sẽ báo kế hoạch và ước tính trước khi code.

## 3. Việc sau phát hành

- Hướng dẫn cho người dùng cuối đã có: [`huong-dan-su-dung.md`](huong-dan-su-dung.md). Nên rà lại theo giao diện thật.
- Tách `ChatControl.cs` cho dễ bảo trì.

## 4. Các thread (rà soát 18/11/2026)

| Thread | Phạm vi | Nhánh | Đã làm | Trạng thái | Chi phí* |
| --- | --- | --- | --- | --- | --- |
| [Luồng chính](https://claude.ai/code/session_01D56HmELvEuaLYUjB1TYfNq) | Điều phối, tổng hợp, việc chưa có thread nhận | `claude/wizardly-heisenberg-ugup5c` | Tính năng tới 0.9.3, rà soát + 30 bản sửa, kế hoạch gộp, tổng quan, hướng dẫn sử dụng, tạo repo Full | Rảnh. Chờ cậu `/compact` (~460k) | ~$423 |
| [Gộp nhánh & phát hành](https://claude.ai/code/session_01D56HmErdvF4wWAbxpf9F5t) | Gộp, bộ cài thử, nâng phiên bản, phát hành | `claude/release-0.9.5` | Gộp 5 nhánh, các bộ cài thử, gỡ BiMat, ghi chú phát hành, **phát hành v0.9.5** (18/11) | Xong | ~$24 |
| [Bảo mật, khóa key & thiết bị](https://claude.ai/code/session_01D56HmEsRHYufkFu48qFmGR) | Key, kích hoạt, Apps Script / Google Sheet | `claude/device-management` | Khóa key theo máy, quản lý thiết bị, đổi cặp khóa, BiMat + ghi "Đã gỡ lúc" (để sau) | **Chờ cậu:** gửi mã máy `CC-…` của máy đã đổi Disabled mà vẫn dùng được, và nói bộ cài tải từ đâu, ngày nào | ~$83 |
| [Lưu trữ bộ cài](https://claude.ai/code/session_01D56HmGL5w4NCZNzEyatioe) | Sổ `bo-cai/luu-tru-bo-cai.md` | `claude/luu-tru-bo-cai` | Tạo sổ; đã ghi v0.9.5 trên nhánh riêng | Xong. Dòng v0.9.5 chưa vào `main` | ~$2 |
| [QnA](https://claude.ai/code/session_01D56HmGkaq6YFfr46Xdigci) | Hỏi đáp về plugin, không sửa code | — | Trả lời câu hỏi; chuyển yêu cầu "Đã gỡ lúc" và phát hành 0.9.5 sang thread phù hợp | Rảnh | ~$13 |
| [Cài đặt plugin](https://claude.ai/code/session_01D56HmFkjV3poaV54WEEFYJ) | Hướng dẫn cài / kích hoạt, gỡ rối | — | Gửi hướng dẫn cài bản thử trên AutoCAD 2026 | Đã lưu trữ (18/11) | ~$0,5 |
| [Giao diện & UX](https://claude.ai/code/session_01D56HmHQPBueRAkYDSecN9n) | Khung chat, theme, thu gọn, icon thanh trạng thái | `claude/ux-autocad` (đã gộp) | 5 đợt giao diện, tiến độ từng bước | Đã lưu trữ (18/11) | ~$11 |
| [Kiểm thử](https://claude.ai/code/session_01D56HmG5SsqRGRPe9bHKUad) | Checklist thử thật, tự kiểm tra | `claude/test-checklist` (đã gộp) | Checklist ~20 phút, tự kiểm tra mở rộng | Đã lưu trữ (18/11) | ~$8 |
| [AutoCAD 2026/2027](https://claude.ai/code/session_01D56HmFfmat4UZDMPVZar76) | Bản AutoCAD mới, build .NET 10 | `claude/acad-2026-2027` (đã gộp) | Hỗ trợ 2026 (R25.1) + 2027 (R26.0), sửa `install.ps1` | Đã lưu trữ (18/11). 2027 chưa thử trên máy thật | ~$7 |
| [Dùng thử & Autodesk](https://claude.ai/code/session_01D56HmGdeNRHtH8Vob4cXGr) | Bản dùng thử AutoCAD, case Autodesk 26568211 | — | Bản dùng thử từ 10/11, khoảng 30 ngày | Rảnh, chờ email Autodesk | ~$1 |
| [Nghiên cứu Skill](https://claude.ai/code/session_01D56HmG6vg6SvpZDrVxxKT7) | Nghiên cứu, đề xuất về Skill | `claude/nghien-cuu-skill` | `docs/nghien-cuu-skill.md`, viết lại 3 skill mẫu (6313359) | Xong. Chờ chuẩn vẽ thật của công ty | — |
| [Tự kiểm trước khi báo xong](https://claude.ai/code/session_01D56HmJy4Urvum4vU5EmdwS) | Plugin tự kiểm bản vẽ trước khi báo xong | `claude/tu-kiem-tra` | `verify_drawing`, hook trước khi kết thúc lượt, thẻ Tự kiểm (eecf3a9, CI xanh) | Để sau | — |
| [Đẩy skill theo máy](https://claude.ai/code/session_01D56HmERgjnrwP5MTbkGKro) | Gán skill theo mã máy trong Sheet, gói ký số `CCP1` | `claude/day-skill-theo-may` | Code xong, rà bảo mật xong (874d2e4) | Chờ thread bảo mật tách `BiMat.gs`, rồi triển khai Apps Script | — |

\* Số hệ thống ghi nhận cho từng thread, quy đổi theo giá API. Tổng cộng khoảng $572; luồng chính chiếm ~74% vì ngữ cảnh dài.
Việc mới nên làm ở thread ngắn.

Thread "Agent to AutoCAD Full" tạo nhầm hôm 12/11 đã được lưu trữ. Project Full chạy riêng, nằm ngoài danh sách này.

Các nhánh đã gộp coi như đóng băng. Sửa tiếp thì làm trên nhánh mới tách từ `main` (v0.9.5). Quy tắc làm việc và danh bạ thread nằm ở
[`CLAUDE.md`](../CLAUDE.md).

## 5. Plugin làm gì

ClaudeCAD thêm khung chat (palette) vào AutoCAD. Người dùng gõ yêu cầu bằng tiếng Việt, ví dụ "vẽ mặt bằng phòng 4×6 m, tường
220, cửa đi 900". Claude đọc bản vẽ đang mở rồi vẽ, sửa, đo hoặc trả lời ngay trên bản vẽ. Mọi thay đổi của một lượt gộp
thành một bước hoàn tác.

| Nhóm | Chi tiết |
| --- | --- |
| Vẽ và sửa | 89 lệnh cho Claude:<br>• hình học: đường, polyline, cung;<br>• chú thích: chữ, kích thước, hatch;<br>• tổ chức bản vẽ: block, layer, style;<br>• chỉnh sửa: fillet / chamfer, di chuyển / xoay / sao chép / xóa;<br>• zoom…<br><br>Có lệnh gộp nhiều thao tác một lần để vẽ nhanh |
| Hiểu bản vẽ | Đọc layer, đối tượng, đơn vị; đo, đếm; biết người dùng đã sửa tay gì giữa các lượt |
| Theo chuẩn công ty | Skill là file `.md` mô tả quy chuẩn. Có 3 skill mẫu: kiến trúc, MEP, hạ tầng. Mỗi đoạn chat chọn skill riêng |
| Nhập yêu cầu | • `/` chọn mẫu lệnh, `@` gọi tên layer / block;<br>• chọn đối tượng hoặc lấy điểm trên bản vẽ;<br>• đính ảnh phác thảo, chụp vùng bản vẽ;<br>• giới hạn layer được sửa |
| Macro | Lưu một lượt vẽ ưng ý rồi chạy lại mà không cần Claude, không tốn token |
| An toàn | • Hỏi trước khi xóa / sửa nhiều đối tượng (có xem trước); việc nhiều bước thì trình kế hoạch trước.<br>• Hoàn tác cả lượt bằng một lần U.<br>• Lưu bản sao DWG 30 phút một lần, giữ 10 bản.<br>• Chế độ "Chỉ xem".<br>• Dữ liệu bản vẽ không bao giờ được coi là lệnh |
| Giao diện (bản thử) | • Màu và font theo theme AutoCAD.<br>• Hàng nút icon có tooltip.<br>• Thu gọn còn ô nhập; tự mở lại khi cần xác nhận.<br>• Icon và bong bóng thông báo trên thanh trạng thái.<br>• Gửi thêm ý khi Claude đang làm; Esc để dừng.<br>• Hiện tiến độ từng bước |
| Tài khoản AI | Claude Pro / Max hoặc API key Anthropic; xem hạn mức đã dùng |

**Cách chạy:**
1. Plugin gom ngữ cảnh: bản vẽ, đối tượng đang chọn, layer, ghi chú dự án, skill, phần đã sửa tay.
2. Plugin gửi ngữ cảnh qua named pipe cho trợ lý. Trợ lý là một tiến trình Node chạy cạnh AutoCAD, dùng Claude Agent SDK.
3. Trợ lý chuyển từng lệnh Claude chọn về cho plugin.
4. Plugin thực hiện lệnh trong AutoCAD và trả kết quả lại.

| Thành phần | Công nghệ |
| --- | --- |
| Plugin | C# / WPF. Ba bản build:<br>• .NET Framework 4.8: AutoCAD 2022–2024 (R24.1–R24.3);<br>• .NET 8: AutoCAD 2025–2026 (R25.0–R25.1);<br>• .NET 10: AutoCAD 2027 (R26.0) |
| Trợ lý | Node.js + TypeScript, Claude Agent SDK. Node đi kèm bộ cài |
| Bộ cài | `ClaudeCAD-Setup.exe` (Inno Setup), một bộ chung cho 2022–2027. Có trang kích hoạt và trang đăng nhập Claude. Cài ngầm: `/VERYSILENT /KEY=<key>` |
| Kích hoạt | • Mỗi máy có mã `CC-XXXX-XXXX-XXXX`, được cấp giấy phép có chữ ký số.<br>• Chủ plugin duyệt hoặc khóa từng máy trong Google Sheet "Thiết bị" (Active / Disabled / Chờ duyệt) |

**Lệnh:**
- `CLAUDECHAT` (tắt `CC`, hoặc `Ctrl+Shift+Space`): mở khung chat;
- `CLAUDECHATRESTART`: khởi động lại trợ lý;
- `CLAUDECHATUPDATE`: cập nhật;
- `CLAUDECHATSELFTEST`: tự kiểm tra;
- `CLAUDECHATLOG`: mở log.

Khi lỗi, vào ⚙ ▸ *Gửi báo cáo lỗi* để tạo file .zip (không chứa nội dung chat, bản vẽ hay key).

**Dữ liệu:** lịch sử chat, cài đặt, macro và các bản sao lưu nằm trên máy (`%LocalAppData%\ClaudeCAD`). API key được mã hóa
theo tài khoản Windows. Chỉ nội dung yêu cầu và dữ liệu bản vẽ cần cho lượt đó được gửi tới Claude.

**Kiểm thử tự động:**
- 164 test lõi, 136 test trợ lý, UI test WPF trên Windows;
- build cả 3 bản .NET và bộ cài mỗi lần CI chạy.

**Giới hạn:**
- Cần mạng và phụ thuộc hạn mức tài khoản Claude.
- Không chạy trên AutoCAD LT.
- AutoCAD sau 2027 sẽ không tự nạp cho tới khi được khai báo.
- Nhiều mục chỉ kiểm được trên AutoCAD thật (theme, thanh trạng thái, chọn đối tượng, phím tắt, kích hoạt). Các mục này còn
  "chưa chạy" trong `test-log.md`.

## 6. Lịch sử phiên bản

| Phiên bản | Nội dung | Trạng thái |
| --- | --- | --- |
| 0.4 – 0.9.3 | Khung chat 20+ tính năng, skill, macro, phiên bản 30 phút, ghi nhận sửa tay, chọn layer, báo cáo lỗi, tài khoản AI, đăng nhập trong bộ cài | Đã phát hành (v0.9.3 ngày 09/11) |
| 0.9.5 (bản thử, gồm phần từng ghi 0.9.4) | • Khóa key theo máy + quản lý thiết bị.<br>• ~30 bản sửa sau rà soát, rồi 3 đợt sửa tiếp.<br>• Skill theo đoạn chat.<br>• Giao diện kiểu AutoCAD.<br>• AutoCAD 2026 / 2027.<br>• Checklist thử thật + tự kiểm tra mở rộng | Đã gộp, CI xanh, chờ thử trên AutoCAD thật. csproj vẫn ghi 0.9.4 tới khi chốt tên |

## 7. Tài liệu khác

- `bo-cai/README.md`: cài đặt và lịch sử phiên bản cho người dùng.
- `huong-dan-su-dung.md`: hướng dẫn sử dụng cho người dùng cuối (bản nháp, chưa đối chiếu giao diện thật).
- `checklist-thu-that.md`: checklist thử trên AutoCAD thật.
- `test-log.md`: kết quả kiểm thử.
- `autocad-2026-2027.md`: chi tiết hỗ trợ AutoCAD 2026 / 2027.
- `ux-autocad.md`: thiết kế giao diện.
- `palette-features.md`: từng tính năng khung chat.
- `skills/README.md`: cách viết skill.
- `trang-thai/*.md`: trạng thái từng nhánh lúc gộp.

## 8. Nhật ký hằng ngày

*Dòng mới thêm lên đầu, giữ 14 ngày gần nhất.*

| Ngày | Tình hình |
| --- | --- |
| 22/11 | Thread nghiên cứu Skill viết lại cấu trúc 3 skill mẫu (`claude/nghien-cuu-skill` @6313359, giữ số liệu cũ, chưa chạy CI). Đã chuyển cho thread gộp nhánh việc bộ cài không cập nhật skill mẫu trên máy đã cài. Chờ cậu gửi chuẩn công ty |
| 22/11 | Gói "Công cụ vẽ & tự kiểm" (thiết kế ở `claude/nghien-cuu-skill` @670dfb2) giao cho thread Tự kiểm làm tiếp trên `claude/tu-kiem-tra`, không mở thread mới |
| 22/11 | Thread đẩy skill code xong (`claude/day-skill-theo-may` @874d2e4; CI xanh ở `08f4838`, run 45004919780). Rà bảo mật xong; cậu chọn tách `BiMat.gs`, việc này đã chuyển cho thread bảo mật. Triển khai Apps Script chờ commit đó, rồi gộp vào 0.9.6 |
| 18/11 | **Phát hành v0.9.5**: cậu merge PR #5, CI tự phát hành (run 44319064734). Code giống hệt bộ cài 11/11 đã thử, chỉ khác số phiên bản. AutoCAD 2027 ghi là hỗ trợ (chưa thử thật). BiMat để sau |
| 17/11 | Cậu đã thử bộ cài 11/11 (run 43000757496): làm việc ổn, sẽ cài cho client. BiMat + ghi "Đã gỡ lúc" để sau, giữ ở `claude/device-management`, đang gỡ khỏi nhánh phát hành. Phát hành chính thức và các quyết định để sau |
| 15/11 | Thread bảo mật làm tính năng ghi "Đã gỡ lúc" và tách bí mật máy chủ (`53bb1bf`). Thread gộp nhánh đã gộp vào `claude/release-0.9.5` (`76f1cce`) và build bộ cài thử mới, run 43665981360. Chờ cậu cập nhật máy chủ Google rồi thử theo checklist A, H, I |
| 11/11 | Gộp xong 5 nhánh vào `claude/release-0.9.5`, đổi cặp khóa kích hoạt, có bộ cài thử cuối (run 43000757496). Mở thread cài đặt plugin. Chờ cậu thử trên AutoCAD 2026 |
