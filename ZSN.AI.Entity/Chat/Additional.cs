using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.Chat
{
    public class Additional
    {
    }

    /// <summary>
    /// 附件项数据模型
    /// </summary>
    public class AttachmentItem : INotifyPropertyChanged
    {
        private string _name;
        private string _type;
        private string _filePath;
        private string _fileCode;
        private string _fileUri;
        private bool _isUploading;
        private int _uploadProgress;

        /// <summary>
        /// 附件名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 附件类型（Image、Document、Code等）
        /// </summary>
        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        /// <summary>
        /// 文件代码（上传成功后的唯一标识）
        /// </summary>
        public string FileCode
        {
            get => _fileCode;
            set => SetProperty(ref _fileCode, value);
        }

        /// <summary>
        /// 文件服务端访问地址
        /// </summary>
        public string FileURI
        {
            get => _fileUri;
            set => SetProperty(ref _fileUri, value);
        }

        /// <summary>
        /// 是否正在上传
        /// </summary>
        public bool IsUploading
        {
            get => _isUploading;
            set => SetProperty(ref _isUploading, value);
        }

        /// <summary>
        /// 上传进度（0-100）
        /// </summary>
        public int UploadProgress
        {
            get => _uploadProgress;
            set => SetProperty(ref _uploadProgress, value);
        }

        /// <summary>
        /// 属性变更事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 设置属性值并触发属性变更通知
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 触发属性变更事件
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 录音记录项模型
    /// </summary>
    public class RecordingItem
    {
        /// <summary>
        /// 录音开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 录音时长（秒）
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// 录音文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 转写文本内容
        /// </summary>
        public string TranscriptionText { get; set; }

        /// <summary>
        /// 格式化的开始时间
        /// </summary>
        public string FormattedStartTime => StartTime.ToString("yyyy-MM-dd HH:mm:ss");

        /// <summary>
        /// 格式化的持续时间
        /// </summary>
        public string FormattedDuration
        {
            get
            {
                TimeSpan duration = TimeSpan.FromSeconds(DurationSeconds);
                return $"{duration.Minutes:00}:{duration.Seconds:00}";
            }
        }

        /// <summary>
        /// 显示标题
        /// </summary>
        public string DisplayTitle => $"录音 {FormattedStartTime} ({FormattedDuration})";
    }
    public class MeetingRecord
    { 
        public List<RecordingItem> Recordings { get; set; } = new List<RecordingItem>();
    }
}
