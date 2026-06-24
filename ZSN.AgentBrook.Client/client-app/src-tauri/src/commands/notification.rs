use tauri::{Runtime, AppHandle};

#[tauri::command]
pub async fn send_system_notification<R: Runtime>(
    #[allow(unused_variables)] app: AppHandle<R>,
    title: String,
    body: String,
    #[allow(unused_variables)] session_id: Option<String>,
) -> Result<(), String> {
    #[cfg(any(target_os = "macos", windows, target_os = "linux"))]
    {
        // Use the app's bundle identifier so the OS attributes the
        // notification to THIS app. Without this:
        //  - macOS dev builds show the source as "com.apple.Terminal".
        //  - Windows WinRT silently drops the toast (no registered AppUMID).
        let identifier = app.config().identifier.clone();

        #[cfg(target_os = "macos")]
        {
            let _ = notify_rust::set_application(&identifier);
        }

        let mut notification = notify_rust::Notification::new();
        notification.summary(&title);
        notification.body(&body);
        notification.auto_icon();

        #[cfg(windows)]
        {
            // notify-rust on Windows needs an explicit appname (= AppUMID hint)
            // or WinRT rejects the toast. Use the bundle identifier.
            notification.appname(&identifier);
        }

        notification
            .show()
            .map_err(|e| e.to_string())?;
    }

    Ok(())
}
