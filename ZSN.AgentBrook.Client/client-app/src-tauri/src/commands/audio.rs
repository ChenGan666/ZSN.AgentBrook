use cpal::traits::{DeviceTrait, HostTrait, StreamTrait};
use std::sync::Mutex;
use tauri::{AppHandle, Emitter, State};

pub struct AudioState {
    stream: Mutex<Option<cpal::Stream>>,
}

impl Default for AudioState {
    fn default() -> Self {
        Self {
            stream: Mutex::new(None),
        }
    }
}

#[tauri::command]
pub fn start_audio_capture(app: AppHandle, state: State<'_, AudioState>) -> Result<(), String> {
    let host = cpal::default_host();
    let device = host
        .default_input_device()
        .ok_or("未找到麦克风设备")?;

    let supported = device
        .supported_input_configs()
        .map_err(|e| e.to_string())?
        .next()
        .ok_or("不支持的音频配置")?;

    let native_rate = supported.min_sample_rate() as f32;
    let config = supported.with_sample_rate(native_rate as u32);
    let channels = config.channels() as usize;

    let app_clone = app.clone();
    let stream = device
        .build_input_stream(
            &config.into(),
            move |data: &[f32], _: &cpal::InputCallbackInfo| {
                // mono downmix
                let mono: Vec<f32> = if channels > 1 {
                    data.chunks(channels)
                        .map(|ch| ch.iter().sum::<f32>() / channels as f32)
                        .collect()
                } else {
                    data.to_vec()
                };

                // downsample to 16kHz
                let ratio = native_rate / 16000.0;
                let resampled: Vec<f32> = if ratio > 1.0 {
                    let out_len = (mono.len() as f32 / ratio) as usize;
                    (0..out_len)
                        .map(|i| mono[(i as f32 * ratio) as usize])
                        .collect()
                } else {
                    mono
                };

                // float32 → int16
                let pcm: Vec<i16> = resampled
                    .iter()
                    .map(|&s| (s.clamp(-1.0, 1.0) * 32767.0) as i16)
                    .collect();

                // encode as base64 for efficient transfer
                let bytes: Vec<u8> = pcm.iter().flat_map(|&s| s.to_le_bytes()).collect();
                let b64 = base64_encode(&bytes);
                let _ = app_clone.emit("audio-pcm-data", b64);
            },
            |err| eprintln!("audio capture error: {}", err),
            None,
        )
        .map_err(|e| e.to_string())?;

    stream.play().map_err(|e| e.to_string())?;
    *state.stream.lock().unwrap() = Some(stream);

    Ok(())
}

#[tauri::command]
pub fn stop_audio_capture(state: State<'_, AudioState>) -> Result<(), String> {
    let mut guard = state.stream.lock().unwrap();
    *guard = None;
    Ok(())
}

fn base64_encode(data: &[u8]) -> String {
    const TABLE: &[u8; 64] =
        b"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    let mut out = String::with_capacity((data.len() + 2) / 3 * 4);
    let mut i = 0;
    while i + 3 <= data.len() {
        let n = ((data[i] as u32) << 16) | ((data[i + 1] as u32) << 8) | (data[i + 2] as u32);
        out.push(TABLE[((n >> 18) & 0x3F) as usize] as char);
        out.push(TABLE[((n >> 12) & 0x3F) as usize] as char);
        out.push(TABLE[((n >> 6) & 0x3F) as usize] as char);
        out.push(TABLE[(n & 0x3F) as usize] as char);
        i += 3;
    }
    if data.len() - i == 1 {
        let n = (data[i] as u32) << 16;
        out.push(TABLE[((n >> 18) & 0x3F) as usize] as char);
        out.push(TABLE[((n >> 12) & 0x3F) as usize] as char);
        out.push('=');
        out.push('=');
    } else if data.len() - i == 2 {
        let n = ((data[i] as u32) << 16) | ((data[i + 1] as u32) << 8);
        out.push(TABLE[((n >> 18) & 0x3F) as usize] as char);
        out.push(TABLE[((n >> 12) & 0x3F) as usize] as char);
        out.push(TABLE[((n >> 6) & 0x3F) as usize] as char);
        out.push('=');
    }
    out
}
