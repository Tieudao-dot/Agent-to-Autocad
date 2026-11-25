# Tổng quan dự án ClaudeCAD

*Cập nhật: 23/11/2026 (giờ Việt Nam). Luồng chính cập nhật mục "Tình hình" và "Nhật ký" khi có mốc mới.*

File này là tổng quan duy nhất của dự án. Nó gộp các file cũ `plugin-tong-quan.md` và `ke-hoach-gop-nhanh.md`.

## 1. Tình hình

- **Bản đang phát hành: v0.9.8** (25/11/2026). Trước đó: v0.9.7 (22/11/2026).
  - Link tải: https://github.com/Tieudao-dot/Agent-to-Autocad/releases/download/v0.9.8/ClaudeCAD-Setup.exe
  - SHA-256: `1E294C8775953F895DA6B009940C37481C22792975D95056D5F4D574ADE6CCA0`. Trang: [Releases v0.9.8](https://github.com/Tieudao-dot/Agent-to-Autocad/releases/tag/v0.9.8).
  - `main` @6e90240 (PR [Tieudao-dot/Agent-to-Autocad#8](https://github.com/Tieudao-dot/Agent-to-Autocad/pull/8)).
- **Hai bản ra trong ngày 22/11:**
  - **v0.9.6** (PR [Tieudao-dot/Agent-to-Autocad#7](https://github.com/Tieudao-dot/Agent-to-Autocad/pull/7)):
    - đẩy skill theo máy, có nhãn Admin / Default / You;
    - sửa skill Default / Admin thì chép sang `<tên>-sua`, bản gốc giữ nguyên;
    - tự kiểm bản vẽ 14 mã lỗi, bắt điểm theo đối tượng, áp chuẩn layer / chữ lúc vẽ;
    - 3 skill mẫu viết lại và tự cập nhật.

    Chi tiết: [`tong-quan-plugin-0.9.6.md`](tong-quan-plugin-0.9.6.md).
  - **v0.9.7:** chỉ sửa bộ cài. Khi cài đè mà AutoCAD đang mở, bộ cài hỏi Thử lại / Hủy bằng tiếng Việt rồi dừng tiến trình của plugin. Plugin và trợ lý giống 0.9.6.
- **Chưa thử trên máy thật:** cậu chọn phát hành 0.9.6 ngay, bỏ qua thử thật.
  - Trên AutoCAD thật chưa thử: đẩy skill, tự kiểm, bắt điểm, skill mẫu tự cập nhật.
  - Trên Windows thật chưa thử: hộp thoại khi cài đè của 0.9.7.
- **Máy chủ Google:** đã cập nhật cho 0.9.6 ngày 22/11.
  - Bí mật nằm riêng trong `BiMat.gs`; `Code.gs` không chứa bí mật.
  - Có thêm trang Skill và Gán skill.
  - Link máy chủ không đổi; `?action=ping` trả `ready:true`.
- **Nâng cấp từ 0.9.5:** không phải kích hoạt lại. Skill, macro, lịch sử chat được giữ.
- **Sổ bộ cài:** [`bo-cai/luu-tru-bo-cai.md`](../bo-cai/luu-tru-bo-cai.md), có v0.9.7 ở đầu sổ.

## 2. Việc còn mở

| # | Việc | Ai làm / chờ gì |
| --- | --- | --- |
| 1 | **Thử trên máy thật:** 0.9.6 (`docs/ke-hoach-day-skill.md` mục 6, checklist I10–I13, `test-log.md` S-6 … S-8) và cài đè 0.9.7 khi AutoCAD đang mở | Cậu |
| 2 | **Máy đã đổi Disabled vẫn dùng được** (từ 18/11). Máy chủ trả trạng thái đúng; nghi lỗi phía máy khách. Plugin kiểm tra khi mở AutoCAD và 6 giờ một lần | Cậu: đóng hẳn rồi mở lại AutoCAD; vẫn dùng được thì gửi mã máy `CC-…` cho thread bảo mật |
| 3 | **Chuẩn vẽ công ty** để thay số liệu mẫu trong 3 skill mẫu | Cậu gửi cho thread nghiên cứu Skill |
| 4 | **Mô tả trang Release v0.9.6 / v0.9.7 / v0.9.8:** chưa có "Điểm mới" (đăng trước khi CI tự nối). Từ bản sau CI tự nối mục C của `docs/ghi-chu-phat-hanh-nhap.md` (tiêu đề file phải ghi đúng phiên bản mới). Bản cũ muốn có thì dán tay | Cậu (nếu muốn) |
| 5 | **Thử key K-5 … K-16:** mạng (K-5, K-6), chỉnh đồng hồ (K-7), chép giấy phép sang máy khác (K-11 … K-16) | Chưa có người làm |
| 6 | **AutoCAD 2027:** ghi là hỗ trợ, chưa thử trên máy thật | Khi có máy / bản dùng thử |
| 7 | **Ghi "Đã gỡ lúc"** khi khách gỡ plugin | Để sau, nằm ở `claude/device-management` (`53bb1bf`) |
| 8 | **Tách `ChatControl.cs`** cho dễ bảo trì | Để sau |
| 9 | **Hướng dẫn sử dụng:** rà lại theo giao diện thật | Sau khi thử thật |
| 10 | **Skill nhận .docx và Google Docs** (yêu cầu từ QnA 25/11) | Thread đẩy skill, nhánh `claude/skill-docx-gdocs`. Code xong @b2452fa (CI xanh ở 5df4569, run 45531742310; lõi 270, trợ lý 192), rà bảo mật xong. Máy chủ Google đã cập nhật (25/11). Bộ cài thử 0.9.8 lần 2: run 45540291856. **Chờ cậu:** thử rồi quyết phát hành |
| 11 | **Sửa skill: chọn cách mở** (yêu cầu từ QnA 25/11). Nút Sửa có menu: ứng dụng mặc định / Chọn ứng dụng khác… / Word. Skill You: lưu là ghi đè. Skill Default / Admin: lưu thì hiện popup "Tạo skill mới" (Tên + Mô tả), thay cách tự chép sang `<tên>-sua`. Không làm sửa skill trên Google Docs phía khách | Thread đẩy skill, `claude/skill-docx-gdocs`. Code xong @dd83838, CI xanh (run 45538946273; lõi 278). Có trong bộ cài thử 0.9.8 lần 2 (run 45540291856). Chờ cậu thử |

## 3. Chi phí khi dùng

- Từ 0.9.6, Claude có thêm lệnh (bắt điểm, tự kiểm), nên mỗi lượt chat tốn thêm khoảng 9 % (~2,5k token, có cache).
- Tự kiểm có thể thêm tối đa 2 vòng sửa mỗi lượt khi bản vẽ có lỗi.

## 4. Các thread (rà soát 23/11/2026)

| Thread | Phạm vi | Nhánh | Đã làm | Trạng thái | Chi phí* |
| --- | --- | --- | --- | --- | --- |
| [Luồng chính](https://claude.ai/code/session_01D56HmELvEuaLYUjB1TYfNq) | Điều phối, tổng hợp | `claude/wizardly-heisenberg-ugup5c` | Tính năng tới 0.9.3, rà soát, tổng quan, giao việc 0.9.6 / 0.9.7, hướng dẫn cập nhật máy chủ Google | Rảnh | ~$456 |
| [Gộp nhánh & phát hành](https://claude.ai/code/session_01D56HmErdvF4wWAbxpf9F5t) | Gộp, bộ cài thử, phát hành | `claude/release` (từ 25/11; tới v0.9.8 là `claude/release-0.9.5`, giữ lại không xóa) | Phát hành v0.9.5, **v0.9.6, v0.9.7** | Xong | ~$62 |
| [Bảo mật, khóa key & thiết bị](https://claude.ai/code/session_01D56HmEsRHYufkFu48qFmGR) | Key, kích hoạt, Apps Script / Sheet | `claude/device-management` | Khóa key, quản lý thiết bị, tách `BiMat.gs`, rà bảo mật đẩy skill | **Chờ cậu:** mã máy Disabled | ~$92 |
| [Đẩy skill theo máy](https://claude.ai/code/session_01D56HmERgjnrwP5MTbkGKro) | Gán skill theo mã máy | `claude/day-skill-theo-may` | Đẩy skill, nhãn, giữ bản gốc khi sửa (đã vào 0.9.6) | Xong. Dòng "chờ đồng ý" trên thread là tin cũ | ~$13 |
| [Tự kiểm trước khi báo xong](https://claude.ai/code/session_01D56HmJy4Urvum4vU5EmdwS) | Tự kiểm bản vẽ, bắt điểm, áp chuẩn | `claude/tu-kiem-tra` | 14 mã, bắt điểm, áp chuẩn lúc vẽ (đã vào 0.9.6) | Xong | ~$21 |
| [Nghiên cứu Skill](https://claude.ai/code/session_01D56HmG6vg6SvpZDrVxxKT7) | Nghiên cứu, đề xuất về Skill | `claude/nghien-cuu-skill` | `docs/nghien-cuu-skill.md`, viết lại 3 skill mẫu | **Chờ cậu:** chuẩn công ty | ~$9 |
| [Lưu trữ bộ cài](https://claude.ai/code/session_01D56HmGL5w4NCZNzEyatioe) | Sổ `bo-cai/luu-tru-bo-cai.md` | `claude/luu-tru-bo-cai` | Ghi đủ tới v0.9.7 | Xong | ~$9 |
| [QnA](https://claude.ai/code/session_01D56HmGkaq6YFfr46Xdigci) | Hỏi đáp về plugin, không sửa code | — | Chuyển lỗi cài đè (→ 0.9.7) | Rảnh | ~$19 |
| [Dùng thử & Autodesk](https://claude.ai/code/session_01D56HmGdeNRHtH8Vob4cXGr) | Bản dùng thử AutoCAD, case Autodesk 26568211 | — | Bản dùng thử từ 10/11, khoảng 30 ngày | Rảnh | ~$1 |
| Cài đặt plugin, Giao diện & UX, Kiểm thử, AutoCAD 2026/2027 | — | (đã gộp) | — | Đã lưu trữ 18/11 | ~$27 |

\* Số hệ thống ghi nhận cho từng thread, quy đổi theo giá API. Tổng cộng khoảng **$709**; luồng chính chiếm ~64 % vì ngữ cảnh dài.
Việc mới nên làm ở thread ngắn.

Project "Agent to AutoCAD Full" chạy riêng, nằm ngoài danh sách này. Quy tắc làm việc và danh bạ thread: [`CLAUDE.md`](../CLAUDE.md).
Sửa tiếp thì làm trên nhánh mới tách từ `main` (v0.9.7).

## 5. Plugin làm gì

ClaudeCAD thêm khung chat (palette) vào AutoCAD. Người dùng gõ yêu cầu bằng tiếng Việt, ví dụ "vẽ mặt bằng phòng 4×6 m, tường
220, cửa đi 900". Claude đọc bản vẽ đang mở rồi vẽ, sửa, đo hoặc trả lời ngay trên bản vẽ. Mọi thay đổi của một lượt gộp
thành một bước hoàn tác.

| Nhóm | Chi tiết |
| --- | --- |
| Vẽ và sửa | Hơn 90 lệnh cho Claude:<br>• hình học: đường, polyline, cung;<br>• chú thích: chữ, kích thước, hatch;<br>• tổ chức bản vẽ: block, layer, style;<br>• chỉnh sửa: fillet / chamfer, di chuyển / xoay / sao chép / xóa;<br>• zoom…<br><br>Có lệnh gộp nhiều thao tác một lần để vẽ nhanh.<br>• Từ 0.9.6: bắt điểm theo đối tượng (đầu, giữa, tâm, giao…), không vẽ lên layer 0 / layer ngoài chuẩn, tự tạo layer chuẩn, chiều cao chữ theo tỷ lệ in |
| Hiểu bản vẽ | Đọc layer, đối tượng, đơn vị; đo, đếm; biết người dùng đã sửa tay gì giữa các lượt |
| Theo chuẩn công ty | Skill là file `.md` mô tả quy chuẩn. Có 3 skill mẫu: kiến trúc, MEP, hạ tầng (tự cập nhật nếu khách chưa sửa). Mỗi đoạn chat chọn skill riêng. Từ 0.9.6: chủ plugin đẩy skill tới từng máy qua Sheet; nhãn Admin / Default / You |
| Tự kiểm (0.9.6) | Trước khi báo xong, Claude đo lại bản vẽ theo 14 mã lỗi (điểm không khớp, layer / thuộc tính, chữ / dim, hình học, thiếu / thừa), tự sửa tối đa 2 vòng; còn lỗi thì liệt kê. Thẻ Tự kiểm trong khung chat |
| Nhập yêu cầu | • `/` chọn mẫu lệnh, `@` gọi tên layer / block;<br>• chọn đối tượng hoặc lấy điểm trên bản vẽ;<br>• đính ảnh phác thảo, chụp vùng bản vẽ;<br>• giới hạn layer được sửa |
| Macro | Lưu một lượt vẽ ưng ý rồi chạy lại mà không cần Claude, không tốn token |
| An toàn | • Hỏi trước khi xóa / sửa nhiều đối tượng (có xem trước); việc nhiều bước thì trình kế hoạch trước.<br>• Hoàn tác cả lượt bằng một lần U.<br>• Lưu bản sao DWG 30 phút một lần, giữ 10 bản.<br>• Chế độ "Chỉ xem".<br>• Dữ liệu bản vẽ không bao giờ được coi là lệnh |
| Giao diện | • Màu và font theo theme AutoCAD.<br>• Hàng nút icon có tooltip.<br>• Thu gọn còn ô nhập; tự mở lại khi cần xác nhận.<br>• Icon và bong bóng thông báo trên thanh trạng thái.<br>• Gửi thêm ý khi Claude đang làm; Esc để dừng.<br>• Hiện tiến độ từng bước |
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
| Kích hoạt | • Mỗi máy có mã `CC-XXXX-XXXX-XXXX`, được cấp giấy phép có chữ ký số.<br>• Chủ plugin duyệt hoặc khóa từng máy trong Google Sheet "Thiết bị" (Active / Disabled / Chờ duyệt).<br>• Máy chủ: Apps Script, gồm `Code.gs` (không bí mật) + `BiMat.gs` (bí mật) |

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
- 264 test lõi, 186 test trợ lý, UI test WPF trên Windows;
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
| 0.9.5 | • Khóa key theo máy + quản lý thiết bị.<br>• ~30 bản sửa sau rà soát, rồi 3 đợt sửa tiếp.<br>• Skill theo đoạn chat.<br>• Giao diện kiểu AutoCAD.<br>• AutoCAD 2026 / 2027.<br>• Checklist thử thật + tự kiểm tra mở rộng | Đã phát hành 18/11 |
| 0.9.6 | • Đẩy skill theo máy, nhãn Admin / Default / You.<br>• Tự kiểm bản vẽ 14 mã, bắt điểm theo đối tượng, áp chuẩn lúc vẽ.<br>• 3 skill mẫu viết lại + tự cập nhật. Chi tiết: [`tong-quan-plugin-0.9.6.md`](tong-quan-plugin-0.9.6.md) | **Đã phát hành 22/11** (PR #7, `main` @8e38ee5, run 45034681870). Chưa thử trên AutoCAD thật |
| 0.9.7 | Bộ cài: khi cài đè mà AutoCAD đang mở thì hỏi Thử lại / Hủy, dừng tiến trình của plugin. Plugin và trợ lý giống 0.9.6 | Đã phát hành 22/11 (PR #8). Chưa thử trên Windows thật |
| 0.9.8 | Skill từ .docx; skill từ Google Docs qua Sheet (máy chủ chỉ xem Drive); sửa skill chọn cách mở (Word / ứng dụng khác, popup Tạo skill mới); nút ⌄ xem skill. Chi tiết: [`tong-quan-plugin-0.9.8.md`](tong-quan-plugin-0.9.8.md) | Đã phát hành 25/11 (PR #9). Chưa thử trên AutoCAD thật |

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
| 25/11 | Xong 2 cải thiện phát hành (PR #10, `main` @77713d4, CI xanh run 45564468349, không phát hành bản mới): CI tự nối "Điểm mới" vào mô tả Release (`bundle/release-highlights.mjs`, trợ lý 198 test); nhánh phát hành đổi thành `claude/release`, nhánh cũ giữ |
| 25/11 | Rà cải thiện. Cậu chọn: CI tự đưa "Điểm mới" lên trang Release + đổi tên nhánh phát hành (giao thread gộp nhánh). Để sau: kênh thử beta theo máy, tách `ChatControl.cs`, `/compact` các thread dài |
| 25/11 | **Phát hành v0.9.8** (PR #9, `main` @0f728f9, run 45556187621): skill từ .docx, skill từ Google Docs qua Sheet, sửa skill chọn cách mở, nút ⌄ xem skill. Tải: https://github.com/Tieudao-dot/Agent-to-Autocad/releases/download/v0.9.8/ClaudeCAD-Setup.exe (SHA-256 1E294C87…ADE6CCA0). Chưa thử trên AutoCAD thật. Báo cáo: [`tong-quan-plugin-0.9.8.md`](tong-quan-plugin-0.9.8.md) |
| 25/11 | Thread đẩy skill thêm nút ⌄ xem nội dung skill ngay trong danh sách (chỉ xem; yêu cầu từ QnA). `claude/skill-docx-gdocs` @2164323, CI xanh run 45546116459. Chưa có trong bộ cài thử 0.9.8 lần 2; cậu chọn gộp luôn lúc phát hành 0.9.8 (không build lại bộ cài thử) |
| 25/11 | Bộ cài thử 0.9.8 lần 2 (run 45540291856, `claude/release-0.9.5` @bca3934; lõi 278, trợ lý 192): có .docx, Google Docs, sửa skill chọn cách mở. Thay lần 1. Chờ cậu thử |
| 25/11 | Sửa skill chọn cách mở code xong (@dd83838, CI xanh run 45538946273). Cậu yêu cầu build bộ cài thử để thử; đã giao thread gộp nhánh |
| 25/11 | Cậu duyệt kế hoạch "sửa skill: chọn cách mở" (Word / ứng dụng khác; skill Default / Admin lưu thành skill mới qua popup). Thread đẩy skill làm, ~3 ngày công |
| 25/11 | Bộ cài thử 0.9.8 (run 45534974010, `claude/release-0.9.5` @dfa8fab; lõi 270, trợ lý 192). Chưa phát hành. Chờ cậu thử 5 mục (.docx, hộp thoại tên / mô tả, Google Docs, sửa doc, cài đè khi AutoCAD mở) |
| 25/11 | Cậu cập nhật máy chủ Google cho 0.9.8: dán Code.gs, khai `oauthScopes` (Drive chỉ xem), chạy `kiemTra`, tạo Phiên bản mới |
| 25/11 | Skill .docx / Google Docs code xong (`claude/skill-docx-gdocs` @b2452fa). Rà bảo mật xong: máy chủ chỉ xin quyền xem Drive (`drive.readonly`). Chờ cậu cập nhật máy chủ Google và quyết phát hành 0.9.8 |
| 25/11 | Cậu duyệt kế hoạch skill .docx / Google Docs: .docx thiếu tên / mô tả thì hỏi; ảnh bị bỏ và báo số ảnh; Google Docs chỉ qua Sheet (máy chủ ký rồi đẩy), phiên bản theo giờ sửa cuối của doc |
| 25/11 | Cậu yêu cầu (qua QnA) skill nhận file .docx và nội dung từ link Google Docs. Giao thread đẩy skill, nhánh mới `claude/skill-docx-gdocs`. Google Docs ưu tiên đi qua kênh đẩy skill có chữ ký; phần máy chủ nhờ thread bảo mật rà |
| 23/11 | Viết lại tổng quan theo bản v0.9.7. Không còn thread nào đang chạy việc; còn chờ cậu: thử thật, mã máy Disabled, chuẩn công ty |
| 22/11 | **Phát hành v0.9.7** (PR #8, `main` @6e90240): bộ cài hỏi Thử lại / Hủy khi cài đè lúc AutoCAD đang mở (lỗi QnA chuyển sang). Chưa thử trên Windows thật |
| 22/11 | **Phát hành v0.9.6** (PR #7, `main` @8e38ee5). Cậu chọn phát hành ngay, bỏ qua thử thật. Trước đó có 2 bộ cài thử (run 45011984540, rồi 45019784560 có thêm "giữ bản gốc khi sửa skill") |
| 22/11 | Cậu cập nhật máy chủ Google cho 0.9.6: tách BiMat, dán `Code.gs` mới, chạy `kiemTra`, tạo Phiên bản mới cùng link; `ping` trả `ready:true` |
| 22/11 | Code xong 3 nhóm cho 0.9.6: đẩy skill theo máy (rà bảo mật xong), tự kiểm đợt 2 (14 mã, bắt điểm, áp chuẩn), 3 skill mẫu viết lại + tự cập nhật. Gói "Công cụ vẽ & tự kiểm" giao cho thread Tự kiểm thay vì mở thread mới |
| 18/11 | **Phát hành v0.9.5**: cậu merge PR #5, CI tự phát hành (run 44319064734). Code giống hệt bộ cài 11/11 đã thử, chỉ khác số phiên bản. AutoCAD 2027 ghi là hỗ trợ (chưa thử thật). BiMat để sau |
| 17/11 | Cậu đã thử bộ cài 11/11 (run 43000757496): làm việc ổn, sẽ cài cho client. BiMat + ghi "Đã gỡ lúc" để sau, giữ ở `claude/device-management`, đang gỡ khỏi nhánh phát hành. Phát hành chính thức và các quyết định để sau |
| 15/11 | Thread bảo mật làm tính năng ghi "Đã gỡ lúc" và tách bí mật máy chủ (`53bb1bf`). Thread gộp nhánh đã gộp vào `claude/release-0.9.5` (`76f1cce`) và build bộ cài thử mới, run 43665981360. Chờ cậu cập nhật máy chủ Google rồi thử theo checklist A, H, I |
| 11/11 | Gộp xong 5 nhánh vào `claude/release-0.9.5`, đổi cặp khóa kích hoạt, có bộ cài thử cuối (run 43000757496). Mở thread cài đặt plugin. Chờ cậu thử trên AutoCAD 2026 |
