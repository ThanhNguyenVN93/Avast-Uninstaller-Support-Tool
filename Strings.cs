using System.Globalization;

namespace frm_avast_uninstaller
{
    internal static class Strings
    {
        private static readonly bool Vi =
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "vi";

        // ── Form1 ────────────────────────────────────────────────────────
        public static string FormTitle =>
            Vi ? "Công cụ gỡ cài đặt Avast" : "Avast Uninstaller Support Tool";

        public static string NormalModeStatus =>
            Vi ? "Công cụ sẽ khởi động vào Safe Mode để gỡ Avast."
               : "The tool will restart into Safe Mode to remove Avast.";

        public static string RestartButton =>
            Vi ? "Khởi động lại vào Safe Mode" : "Restart to Safe Mode";

        public static string Restarting =>
            Vi ? "Đang khởi động lại vào Safe Mode..." : "Restarting into Safe Mode...";

        public static string ErrorPrefix => Vi ? "Lỗi: " : "Error: ";

        public static string Countdown(int s) =>
            Vi ? $"Tự động khởi động lại sau: {s} giây" : $"Auto-restarting in: {s} s";

        // ── Program ──────────────────────────────────────────────────────
        public static string UninstallerMissing =>
            Vi ? "Không tìm thấy avastclear.exe trong resources."
               : "avastclear.exe not found in embedded resources.";

        public static string UninstallBanner =>
            Vi ? "✓  Đã gỡ cài đặt Avast hoàn tất!"
               : "✓  Avast has been successfully removed!";

        public static string UninstallComplete =>
            Vi ? "Đã gỡ cài đặt Avast hoàn tất!\nMáy tính sẽ tự động khởi động về chế độ bình thường."
               : "Avast has been successfully removed!\nThe computer will restart normally.";

        public static string DoneTitle  => Vi ? "Hoàn Tất" : "Done";
        public static string ErrorTitle => Vi ? "Lỗi"      : "Error";

        // ── DonateForm ───────────────────────────────────────────────────
        public static string DonateTitle =>
            Vi ? "Ủng hộ tác giả" : "Donate";

        public static string DonateThanks =>
            Vi ? "Cảm ơn bạn đã sử dụng tool! Nếu thấy hữu ích, hãy ủng hộ tác giả ♥"
               : "Thanks for using this tool! If it helped, consider supporting the author ♥";

        public static string DonateTCB   => Vi ? "Techcombank" : "Techcombank";
        public static string DonateMoMo  => Vi ? "Ví MoMo"     : "MoMo Wallet";
        public static string DonateClose => Vi ? "Đóng"        : "Close";

        // ── Errors ───────────────────────────────────────────────────────
        public static string NormalCleanupError(string msg) =>
            Vi ? "Lỗi dọn dẹp sau Safe Mode:\n"  + msg
               : "Post-Safe-Mode cleanup error:\n" + msg;

        public static string SafeModeError(string msg) =>
            Vi ? "Lỗi xử lý Safe Mode:\n" + msg
               : "Safe Mode error:\n"      + msg;

        public static string OsNotSupported =>
            Vi ? "Hệ điều hành này không được hỗ trợ.\nYêu cầu Windows 7 SP1 (build 7601) trở lên."
               : "This operating system is not supported.\nRequires Windows 7 SP1 (build 7601) or later.";
    }
}
