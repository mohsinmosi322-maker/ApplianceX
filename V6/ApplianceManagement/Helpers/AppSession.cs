using ApplianceManagement.Models;

namespace ApplianceManagement.Helpers
{
    /// <summary>Authenticated session — set after login, cleared on logout.</summary>
    public static class AppSession
    {
        public static User CurrentUser { get; private set; }
        public static bool IsAuthenticated { get { return CurrentUser != null; } }
        public static int UserId { get { return CurrentUser != null ? CurrentUser.UserID : 0; } }
        public static string UserName { get { return CurrentUser != null ? CurrentUser.UserName : ""; } }
        public static string Role { get { return CurrentUser != null ? CurrentUser.Role : ""; } }
        public static bool IsAdmin { get { return string.Equals(Role, "Admin", System.StringComparison.OrdinalIgnoreCase); } }

        public static void SignIn(User user)
        {
            CurrentUser = user;
            AppLog.Info("Login: " + (user != null ? user.UserName : "?"));
        }

        public static void SignOut()
        {
            if (CurrentUser != null)
                AppLog.Info("Logout: " + CurrentUser.UserName);
            CurrentUser = null;
        }

        public static void RequireAuth()
        {
            if (!IsAuthenticated)
                throw new System.InvalidOperationException("Not authenticated.");
        }

        public static void RequirePermission(string key)
        {
            RequireAuth();
            if (!AppSettings.HasPermission(UserName, Role, key))
                throw new System.UnauthorizedAccessException("Access denied: " + key);
        }
    }
}
