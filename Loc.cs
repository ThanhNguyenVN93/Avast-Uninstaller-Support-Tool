using System.Collections.Generic;
using System.Globalization;

namespace frm_avast_uninstaller
{
    internal static class Loc
    {
        internal enum Lang { Vi, En }

        internal static Lang Current;

        static Loc()
        {
            Current = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi"
                ? Lang.Vi : Lang.En;
        }

        internal static void LoadSaved()
        {
            string s = Program.GetState("Lang");
            if (s == "vi") Current = Lang.Vi;
            else if (s == "en") Current = Lang.En;
        }

        internal static void Toggle()
        {
            Current = Current == Lang.Vi ? Lang.En : Lang.Vi;
            try { Program.SetState("Lang", Current == Lang.Vi ? "vi" : "en"); } catch { }
        }

        internal static string T(string key)
        {
            var table = Current == Lang.Vi ? Vi : En;
            string v;
            return table.TryGetValue(key, out v) ? v : key;
        }

        internal static ICollection<string> Keys(Lang lang)
        {
            return (lang == Lang.Vi ? Vi : En).Keys;
        }

        internal const string IssuesUrl =
            "https://github.com/ThanhNguyenVN93/Avast-Uninstaller-Support-Tool/issues/new";

        internal const string ReportIssuesUrl =
            "https://github.com/ThanhNguyenVN93/Avast-Uninstaller-Support-Tool/issues";

        private static readonly Dictionary<string, string> Vi = new Dictionary<string, string>
        {
            { "app_title",        "Công cụ gỡ cài đặt Avast" },
            { "header_subtitle",  "Gỡ bỏ hoàn toàn Avast Antivirus qua Safe Mode" },
            { "status_normal",    "Công cụ sẽ khởi động máy tính vào Safe Mode để gỡ bỏ Avast.\nVui lòng lưu tất cả công việc trước khi tiếp tục." },
            { "no_avast_normal",  "Không phát hiện Avast trên máy — không cần gỡ. Bạn vẫn có thể chạy thủ công nếu muốn." },
            { "step_label",       "BƯỚC 1 / 3  —  CHUẨN BỊ KHỞI ĐỘNG LẠI" },
            { "step_restarting",  "BƯỚC 1 / 3  —  ĐANG KHỞI ĐỘNG LẠI..." },
            { "step_error",       "LỖI  —  KHÔNG THỂ KHỞI ĐỘNG LẠI" },
            { "countdown",        "Tự động khởi động lại sau {0} giây" },
            { "btn_restart",      "Khởi động lại vào Safe Mode" },
            { "btn_cancel",       "Hủy bỏ" },
            { "btn_lang",         "English" },
            { "btn_report",       "Báo lỗi / Góp ý" },
            { "btn_about",        "Giới thiệu" },
            { "btn_nosafemode",   "Thử gỡ ngay (không cần Safe Mode)" },
            { "chk_clean",        "Dọn file/registry Avast còn sót (nâng cao)" },
            { "preflight",        "Trước khi vào Safe Mode:\n• Nếu Avast CyberCapture chặn tool này, hãy vào Quarantine khôi phục file rồi thêm vào ngoại lệ (Exclusions).\n• Wi-Fi có thể KHÔNG hoạt động — chuẩn bị mạng dây/truy cập từ xa.\n• Sao lưu dữ liệu quan trọng.\n\nTiếp tục khởi động lại vào Safe Mode?" },
            { "preflight_title",  "Kiểm tra trước khi chạy" },
            { "nosafemode_confirm","Chế độ này gỡ Avast NGAY trong Windows bình thường.\nHãy TẮT 'Self-Defense' trong Avast trước, nếu không thao tác có thể bị chặn.\n\nTiếp tục?" },
            { "nosafemode_title", "Gỡ không cần Safe Mode" },
            { "working",          "Đang gỡ Avast, vui lòng đợi..." },
            { "info_tip",         "Mẹo: chạy trong Safe Mode gỡ được Avast kể cả khi cài đặt bị khóa." },
            { "export_reminder",  "Lưu ý: hãy xuất thiết lập Avast trước khi gỡ — thiết lập sẽ KHÔNG tự khôi phục khi cài lại." },
            { "status_downloading","Đang tải phiên bản mới nhất..." },
            { "status_preparing", "Đang chuẩn bị khởi động lại vào Safe Mode..." },
            { "err_prefix",       "Lỗi: " },
            { "abort_text",       "Máy sẽ khởi động lại vào Safe Mode sau {0} giây." },
            { "abort_btn",        "Hủy khởi động lại" },
            { "aborted",          "Đã hủy. Mọi thay đổi đã được hoàn tác, máy không khởi động lại." },
            { "crash_title",      "Lỗi không mong muốn" },
            { "crash",            "Đã xảy ra lỗi không mong muốn. Chi tiết đã ghi vào log:" },
            { "need_admin",       "Công cụ cần quyền Administrator để chạy." },
            { "already_running",  "Công cụ đang chạy rồi." },
            { "help_title",       "Hướng dẫn dòng lệnh" },
            { "help_text",        "Avast Uninstaller Support Tool\n\nTham số dòng lệnh:\n  --auto            Không giao diện, tự vào Safe Mode\n  --cleanleftovers  Dọn file/service Avast còn sót\n  --nosafemode      Gỡ ngay trong Windows bình thường (không reboot)\n  --help            Hiện trợ giúp này\n\nMã thoát: 0=xong, 1=còn Avast, 2=lỗi, 3=không có Avast" },
            { "about_text",       "Avast Uninstaller Support Tool\nPhiên bản {0}\n\nTự động gỡ Avast trong Safe Mode.\nGiấy phép MIT.\n\n{1}" },
            { "tip_restart",      "Khởi động lại vào Safe Mode rồi tự gỡ Avast (khuyên dùng)." },
            { "tip_nosafemode",   "Gỡ ngay trong Windows bình thường. Cần tắt Self-Defense trước." },
            { "tip_clean",        "Nếu còn sót, xóa service + thư mục Avast. Có rủi ro — chỉ bật khi cần." },
            { "autologon_title",  "Tự động đăng nhập" },
            { "autologon_fail",   "Không bật được tự động đăng nhập, bạn sẽ cần đăng nhập thủ công trong Safe Mode.\n" },
            { "pwd_title",        "Tự động đăng nhập Safe Mode" },
            { "pwd_prompt",       "Nhập mật khẩu Windows để máy TỰ ĐỘNG đăng nhập và chạy trong Safe Mode.\nĐể trống nếu tài khoản không có mật khẩu. Bấm 'Bỏ qua' nếu muốn tự đăng nhập thủ công." },
            { "pwd_ok",           "OK" },
            { "pwd_skip",         "Bỏ qua" },
            { "result_title",     "Kết quả gỡ cài đặt Avast" },
            { "result_success",   "✓ Đã gỡ cài đặt Avast thành công." },
            { "result_warning",   "⚠ Đã chạy gỡ cài đặt nhưng vẫn còn phát hiện Avast.\nCó thể cần chạy lại công cụ một lần nữa." },
            { "result_none",      "Không phát hiện Avast trên máy. Không có gì để gỡ." },
            { "result_error",     "✕ Có lỗi xảy ra khi gỡ cài đặt trong Safe Mode.\nXem log để biết chi tiết." },
            { "result_unknown",   "Không rõ kết quả lần chạy gần nhất." },
            { "result_openlog",   "Mở log chi tiết" },
            { "result_openfolder","Mở thư mục log" },
            { "result_restorenet","Khôi phục mạng" },
            { "result_diag",      "Gom log chẩn đoán" },
            { "result_close",     "Đóng" },
            { "result_nolog",     "(Không có log.)" },
            { "restored_ok",      "Đã khôi phục cấu hình mạng." },
            { "restored_fail",    "Không tìm thấy bản sao lưu mạng để khôi phục." },
            { "diag_done",        "Đã tạo gói chẩn đoán:\n{0}" },
            { "update_available", "Có phiên bản mới: {0}" },
            { "update_btn",       "Cập nhật" },
            { "update_downloading","Đang tải bản cập nhật..." },
            { "update_confirm",   "Đã tải xong phiên bản {0}.\nỨng dụng sẽ đóng lại và tự cập nhật.\n\nTiếp tục?" },
            { "update_confirm_title","Cập nhật" },
            { "update_fail",      "Không tải được bản cập nhật." },
            { "donate_title",     "Ủng hộ tác giả" },
            { "donate_thanks",    "Cảm ơn bạn đã sử dụng tool! Nếu thấy hữu ích, hãy ủng hộ tác giả ♥" },
            { "donate_tcb",       "Techcombank" },
            { "donate_momo",      "Ví MoMo" },
            { "donate_close",     "Đóng" },
            { "uninstall_banner", "✓  Đã gỡ cài đặt Avast hoàn tất!" },
            { "os_not_supported", "Hệ điều hành này không được hỗ trợ.\nYêu cầu Windows 7 SP1 (build 7601) trở lên." },
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            { "app_title",        "Avast Uninstaller Support Tool" },
            { "header_subtitle",  "Completely remove Avast Antivirus via Safe Mode" },
            { "status_normal",    "The tool will restart your computer into Safe Mode to remove Avast.\nPlease save all work before proceeding." },
            { "no_avast_normal",  "No Avast detected on this PC — nothing to remove. You can still run it manually." },
            { "step_label",       "STEP 1 / 3  —  PREPARING RESTART" },
            { "step_restarting",  "STEP 1 / 3  —  RESTARTING..." },
            { "step_error",       "ERROR  —  RESTART FAILED" },
            { "countdown",        "Auto-restarting in {0}s" },
            { "btn_restart",      "Restart to Safe Mode" },
            { "btn_cancel",       "Cancel" },
            { "btn_lang",         "Tiếng Việt" },
            { "btn_report",       "Report issue / Feedback" },
            { "btn_about",        "About" },
            { "btn_nosafemode",   "Try removal now (no Safe Mode)" },
            { "chk_clean",        "Clean leftover Avast files/registry (advanced)" },
            { "preflight",        "Before entering Safe Mode:\n• If Avast CyberCapture blocks this tool, restore it from Quarantine and add it to Exclusions.\n• Wi-Fi may NOT work — prepare wired/remote access.\n• Back up important data.\n\nContinue restarting into Safe Mode?" },
            { "preflight_title",  "Pre-flight check" },
            { "nosafemode_confirm","This removes Avast RIGHT NOW in normal Windows.\nFirst DISABLE 'Self-Defense' in Avast, otherwise it may be blocked.\n\nContinue?" },
            { "nosafemode_title", "Remove without Safe Mode" },
            { "working",          "Uninstalling Avast, please wait..." },
            { "info_tip",         "Tip: running in Safe Mode removes Avast even if its settings are locked." },
            { "export_reminder",  "Note: export your Avast settings before removal — they are NOT restored automatically on reinstall." },
            { "status_downloading","Downloading latest version..." },
            { "status_preparing", "Preparing to restart into Safe Mode..." },
            { "err_prefix",       "Error: " },
            { "abort_text",       "The PC will restart into Safe Mode in {0}s." },
            { "abort_btn",        "Abort restart" },
            { "aborted",          "Aborted. All changes were reverted; the PC will not restart." },
            { "crash_title",      "Unexpected error" },
            { "crash",            "An unexpected error occurred. Details were written to the log:" },
            { "need_admin",       "This tool must run as Administrator." },
            { "already_running",  "The tool is already running." },
            { "help_title",       "Command-line help" },
            { "help_text",        "Avast Uninstaller Support Tool\n\nCommand-line flags:\n  --auto            No UI; arm + reboot into Safe Mode\n  --cleanleftovers  Remove leftover Avast files/services\n  --nosafemode      Uninstall now in normal Windows (no reboot)\n  --help            Show this help\n\nExit codes: 0=done, 1=still present, 2=error, 3=no Avast" },
            { "about_text",       "Avast Uninstaller Support Tool\nVersion {0}\n\nAutomatically removes Avast in Safe Mode.\nMIT License.\n\n{1}" },
            { "tip_restart",      "Restart into Safe Mode and remove Avast automatically (recommended)." },
            { "tip_nosafemode",   "Remove now in normal Windows. Disable Self-Defense first." },
            { "tip_clean",        "If leftovers remain, delete Avast services + folders. Risky — enable only if needed." },
            { "autologon_title",  "Autologon" },
            { "autologon_fail",   "Could not enable autologon; you will need to log in manually in Safe Mode.\n" },
            { "pwd_title",        "Safe Mode autologon" },
            { "pwd_prompt",       "Enter your Windows password so the PC logs in AUTOMATICALLY and runs in Safe Mode.\nLeave blank if the account has no password. Click 'Skip' to log in manually." },
            { "pwd_ok",           "OK" },
            { "pwd_skip",         "Skip" },
            { "result_title",     "Avast uninstall result" },
            { "result_success",   "✓ Avast was uninstalled successfully." },
            { "result_warning",   "⚠ The uninstaller ran but Avast is still detected.\nYou may need to run the tool again." },
            { "result_none",      "No Avast detected on this PC. Nothing to remove." },
            { "result_error",     "✕ An error occurred while uninstalling in Safe Mode.\nSee the log for details." },
            { "result_unknown",   "The outcome of the last run is unknown." },
            { "result_openlog",   "Open detailed log" },
            { "result_openfolder","Open log folder" },
            { "result_restorenet","Restore network" },
            { "result_diag",      "Collect diagnostics" },
            { "result_close",     "Close" },
            { "result_nolog",     "(No log.)" },
            { "restored_ok",      "Network configuration restored." },
            { "restored_fail",    "No network backup found to restore." },
            { "diag_done",        "Diagnostics bundle created:\n{0}" },
            { "update_available", "New version available: {0}" },
            { "update_btn",       "Update" },
            { "update_downloading","Downloading update..." },
            { "update_confirm",   "Version {0} downloaded.\nThe app will close and update itself.\n\nContinue?" },
            { "update_confirm_title","Update" },
            { "update_fail",      "Could not download the update." },
            { "donate_title",     "Donate" },
            { "donate_thanks",    "Thanks for using this tool! If it helped, consider supporting the author ♥" },
            { "donate_tcb",       "Techcombank" },
            { "donate_momo",      "MoMo Wallet" },
            { "donate_close",     "Close" },
            { "uninstall_banner", "✓  Avast has been successfully removed!" },
            { "os_not_supported", "This operating system is not supported.\nRequires Windows 7 SP1 (build 7601) or later." },
        };
    }
}
