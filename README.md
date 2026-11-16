# Avast Uninstaller Support Tool

> Removes Avast Antivirus completely using Avast's own official cleanup tool (**Avast AV Clear**), running in Safe Mode where Avast's self-protection cannot interfere. One click, fully automated.

[![Release](https://img.shields.io/github/v/release/ThanhNguyenVN93/Avast-Uninstaller-Support-Tool)](https://github.com/ThanhNguyenVN93/Avast-Uninstaller-Support-Tool/releases/latest)
[![CI](https://github.com/ThanhNguyenVN93/Avast-Uninstaller-Support-Tool/actions/workflows/ci.yml/badge.svg)](https://github.com/ThanhNguyenVN93/Avast-Uninstaller-Support-Tool/actions/workflows/ci.yml)

**Download:** grab `Avast Uninstaller Support Tool.exe` from the [latest release](https://github.com/ThanhNguyenVN93/Avast-Uninstaller-Support-Tool/releases/latest).

---

## English

### Why this tool?

Avast's self-protection blocks standard uninstallation and `avastclear` refuses to run silently. This tool reboots into **Safe Mode**, drives Avast AV Clear for you, cleans leftovers and reboots back — you only need to confirm one prompt.

### How it works

```
Normal Mode
  └─ Download latest Avast AV Clear from Avast CDN (Authenticode + signer verified)
  └─ System Restore point + network settings backup
  └─ Optional: Windows password → autologon in Safe Mode
  └─ RunOnce + AlternateShell + bcdedit safeboot → restart (20 s abort window)

Safe Mode  (automatic)
  └─ Avast AV Clear opens → tool clicks "Remove" for you
  └─ You confirm Avast's "Do you really want to uninstall?" prompt (60 s)
  └─ Tool waits until removal finishes, closes AV Clear, cleans leftover
     services / folders / registry, then restarts

Normal Mode  (automatic)
  └─ Final cleanup → result / completion screen
```

Safety: loop guard (max 3 Safe Mode attempts), stale-arm timeout (6 h), abort button, every boot trigger reverted on cancel or error. Leftover cleanup only runs after Avast AV Clear actually completed.

### Requirements

| | |
|---|---|
| OS | Windows 7 SP1 (build 7601) or later |
| Arch | x86 / x64 |
| Rights | Administrator |
| Runtime | .NET Framework 4.8 |

### Usage

1. If Avast CyberCapture quarantines the tool on first run, restore it from Quarantine (it adds itself to Avast exclusions afterwards).
2. Run **`Avast Uninstaller Support Tool.exe`** as Administrator.
3. Read the pre-flight checklist, optionally enter your Windows password for autologon, let the countdown restart the PC.
4. In Safe Mode the tool clicks **Remove**; click **Uninstall** on Avast's confirmation prompt when it appears.
5. Wait — the PC restarts to Normal Mode and shows the result.

Alternative: **Try removal now (no Safe Mode)** runs Avast AV Clear interactively without rebooting (disable Avast Self-Defense first).

### Command line

```
--auto            No UI; arm + reboot into Safe Mode (optionally --password=...)
--cleanleftovers  Remove leftover Avast files/services
--nosafemode      Uninstall now in normal Windows (no reboot)
--help            Show help
```

Exit codes: `0` done, `1` Avast still present, `2` error, `3` no Avast found.

### Logs

`C:\ProgramData\FrmAvastUninstaller\diagnostic.log` (+ `network_backup.txt`). The result screen can bundle them into a diagnostics zip for bug reports.

### Build

Visual Studio 2022+ (or MSBuild) with .NET Framework 4.8 targeting pack:

```bash
msbuild frm_avast_uninstaller.csproj -p:Configuration=Release
```

Output: a single portable `bin\Release\Avast Uninstaller Support Tool.exe`.

### Releases

Tag `vX.Y.Z` on `master` (must equal `AppVersion` in `Program.cs`). The **Release** workflow verifies the embedded Avast AV Clear signature, builds, and publishes the exe + `SHA256SUMS.txt` to GitHub Releases. The tool checks GitHub Releases for updates at startup.

---

## Tiếng Việt

### Tại sao cần tool này?

Avast tự bảo vệ nên không gỡ được theo cách thường, và `avastclear` từ chối chạy silent. Tool này tự khởi động vào **Safe Mode**, tự thao tác Avast AV Clear, dọn rác và khởi động lại — bạn chỉ cần xác nhận một hộp thoại.

### Cách hoạt động

```
Normal Mode
  └─ Tải Avast AV Clear mới nhất từ CDN của Avast (kiểm tra chữ ký)
  └─ Tạo System Restore point + sao lưu cấu hình mạng
  └─ Tuỳ chọn: nhập mật khẩu Windows → tự đăng nhập trong Safe Mode
  └─ RunOnce + AlternateShell + bcdedit safeboot → khởi động lại (20 s để huỷ)

Safe Mode  (tự động)
  └─ Avast AV Clear mở → tool tự bấm "Remove"
  └─ Bạn bấm "Uninstall" ở hộp thoại xác nhận của Avast (60 s)
  └─ Tool chờ gỡ xong, đóng AV Clear, dọn service / thư mục / registry còn sót, rồi khởi động lại

Normal Mode  (tự động)
  └─ Dọn dẹp lần cuối → màn hình kết quả / hoàn tất
```

An toàn: chống lặp (tối đa 3 lần vào Safe Mode), hết hạn sau 6 giờ, nút huỷ khởi động lại, mọi thay đổi boot được hoàn tác khi huỷ hoặc lỗi. Chỉ dọn rác khi Avast AV Clear thực sự chạy xong.

### Yêu cầu

| | |
|---|---|
| OS | Windows 7 SP1 (build 7601) trở lên |
| Kiến trúc | x86 / x64 |
| Quyền | Administrator |
| Runtime | .NET Framework 4.8 |

### Cách dùng

1. Nếu Avast CyberCapture cách ly tool ở lần chạy đầu, khôi phục từ Quarantine (tool sẽ tự thêm mình vào Exclusions).
2. Chạy **`Avast Uninstaller Support Tool.exe`** bằng quyền Administrator.
3. Đọc checklist, tuỳ chọn nhập mật khẩu Windows để tự đăng nhập, để đếm ngược khởi động lại máy.
4. Trong Safe Mode tool tự bấm **Remove**; khi hộp thoại xác nhận của Avast hiện ra, bấm **Uninstall**.
5. Chờ — máy tự về Normal Mode và hiện kết quả.

Cách khác: **Thử gỡ ngay (không cần Safe Mode)** chạy Avast AV Clear ngay trong Windows (tắt Self-Defense của Avast trước).

### Dòng lệnh

```
--auto            Không giao diện, tự vào Safe Mode (có thể kèm --password=...)
--cleanleftovers  Dọn file/service Avast còn sót
--nosafemode      Gỡ ngay trong Windows bình thường (không reboot)
--help            Hiện trợ giúp
```

Mã thoát: `0` xong, `1` còn Avast, `2` lỗi, `3` không có Avast.

### Log

`C:\ProgramData\FrmAvastUninstaller\diagnostic.log` (+ `network_backup.txt`). Màn hình kết quả có nút gom log thành file zip để báo lỗi.

### Build

Visual Studio 2022+ (hoặc MSBuild) với .NET Framework 4.8 targeting pack:

```bash
msbuild frm_avast_uninstaller.csproj -p:Configuration=Release
```

Output: một file `bin\Release\Avast Uninstaller Support Tool.exe` portable.

### Phát hành

Gắn tag `vX.Y.Z` trên `master` (phải trùng `AppVersion` trong `Program.cs`). Workflow **Release** kiểm tra chữ ký Avast AV Clear nhúng, build, và đăng exe + `SHA256SUMS.txt` lên GitHub Releases. Tool tự kiểm tra bản mới trên GitHub Releases khi khởi động.

---

## License

[MIT](LICENSE) © 2026 thanhnguyenit9x93
