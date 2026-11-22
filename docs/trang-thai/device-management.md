# Trạng thái nhánh `claude/device-management` (bảo mật, khóa key & quản lý thiết bị)

san-sang-gop: 9ea7e33


## Mới nhất: tách bí mật Apps Script ra BiMat.gs (commit `9ea7e33`, dựa trên `release-0.9.5` @5b3cedb)

- Chỉ tách bí mật, **không** kèm "báo gỡ cài đặt" (phần đó vẫn hoãn, nằm ở `53bb1bf`, đã revert trên `release-0.9.5`).
- `Code.gs.template` giữ tên, bỏ 3 dòng bí mật, thêm `secretsMissing()`; `BiMat.gs.template` mới; `make-apps-script.mjs` sinh `BiMat.gs`; test VM 137/137; `activation/README.md` (cài 2 file + cách chuyển một lần).
- Người dùng: thread đẩy skill (`claude/day-skill-theo-may`) gộp commit này trước, rồi viết hướng dẫn triển khai **một lần** cho chủ dự án (tạo `BiMat` + dán `Code.gs` mới). Gộp thử: code + test sạch, chỉ chồng `activation/README.md` đoạn "Cập nhật lên bản có đẩy skill" → lấy đoạn 2 file của nhánh này + giữ 2 ý của nhánh skill (`kiemTra` tạo trang Skill / Gán skill; làm máy chủ trước khi phát hành 0.9.6).

## Phần chênh so với lần gộp trước vào `claude/release-0.9.5`

- **Đổi cặp khóa kích hoạt** (commit `70497f6`): `plugin/ClaudeCAD.Plugin/Core/License.cs` (`LicenseLogic.PublicModulus`) và `plugin/ClaudeCAD.Core.Tests/LicenseTests.cs` (fixture `ProductionLicense`, `ProductionStatus`, ngày 2026-11-11). Chỉ 2 file, 4 dòng.
- Tài liệu: `docs/test-log.md` (dòng kiểm chứng đổi khóa trên máy chủ thật), `docs/progress.md` (đã đổi khóa).
- Không đổi `activation/activation.json` (link máy chủ giữ nguyên), không đổi `Code.gs.template`.

## Đã kiểm

- Test lõi 120/120, sidecar 127/127, build net48 + net8 0 lỗi; CI Windows (`release: false`) xanh ở commit `70497f6`.
- Máy chủ Google thật đã chạy khóa mới: `ping`, `request`, key bịa bị từ chối, `status` có chữ ký khớp khóa công khai mới, giấy phép ký bằng khóa mới.

## Lưu ý khi gộp

- **Bộ cài thử cuối phải build từ commit có khóa mới.** Bộ cài build trước `70497f6` dùng khóa cũ: máy chủ trả chữ ký không khớp, plugin sẽ khóa mọi máy.
- Khi gộp `License.cs` / `LicenseTests.cs`: lấy giá trị của nhánh này.
