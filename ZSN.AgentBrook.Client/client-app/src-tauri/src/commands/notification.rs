use tauri::{Runtime, AppHandle};

#[tauri::command]
pub async fn send_system_notification<R: Runtime>(
    app: AppHandle<R>,
    title: String,
    body: String,
) -> Result<(), String> {
    #[cfg(any(target_os = "macos", windows, target_os = "linux"))]
    {
        #[cfg(target_os = "macos")]
        {
            let identifier = app.config().identifier.clone();
            let _ = notify_rust::set_application(&identifier);
        }

        let mut notification = notify_rust::Notification::new();
        notification.summary(&title);
        notification.body(&body);
        notification.auto_icon();

        notification
            .show()
            .map_err(|e| e.to_string())?;
    }

    Ok(())
}
