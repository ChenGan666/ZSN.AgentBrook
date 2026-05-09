using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AI.Functions.Common
{
    /// <summary>
    /// 文件转换为图片转换器
    /// 需要安装LibreOffice和Ghostscript
    /// </summary>
    public class FileToImageConverter
    {
        private readonly string _baseOutputDir;

        public FileToImageConverter(string baseOutputDir)
        {
            _baseOutputDir = baseOutputDir;
            Directory.CreateDirectory(_baseOutputDir);
        }

        /// <summary>
        /// 将任意文件转换为图片并存储到以文件MD5为名的目录中。
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <returns>生成的图片文件路径列表</returns>
        public List<string> ConvertToImages(string inputFilePath,string sourceFileCode = "")
        {
            if (!File.Exists(inputFilePath))
                throw new FileNotFoundException("文件不存在", inputFilePath);

            // 计算 MD5
            string md5 = sourceFileCode.IsNullOrEmpty()? GetFileMd5(inputFilePath): sourceFileCode;

            // 输出目录：base/md5
            string outputDir = Path.Combine(_baseOutputDir, md5);

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 把原始文件复制进去
            string copiedFile = Path.Combine(outputDir, Path.GetFileName(inputFilePath));
            File.Copy(inputFilePath, copiedFile, true);

            // 转换逻辑
            string ext = Path.GetExtension(inputFilePath).ToLower();
            string workingFile = copiedFile;
            string pdfFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(inputFilePath) + ".pdf");

            // Office 文档先转 PDF
            if (ext == ".doc" || ext == ".docx" || ext == ".xls" || ext == ".xlsx" ||
                ext == ".ppt" || ext == ".pptx")
            {
                ConvertOfficeToPdf(copiedFile, pdfFile);
                workingFile = pdfFile;
            }
            else if (ext == ".pdf")
            {
                workingFile = copiedFile;
            }
            else if (ext == ".txt")
            {
                File.WriteAllText(pdfFile, File.ReadAllText(copiedFile));
                workingFile = pdfFile;
            }
            else
            {
                // 直接尝试作为图片读取
                return SaveImageDirectly(copiedFile, outputDir);
            }

            // PDF 转图片
            return ConvertPdfToImages(workingFile, outputDir);
        }

        private List<string> ConvertPdfToImages(string pdfFile, string outputDir)
        {
            // 检查Ghostscript是否安装
            if (!IsGhostscriptInstalled())
                throw new InvalidOperationException("未检测到Ghostscript，请确认已安装并配置环境变量。");

            var result = new List<string>();

            try
            {// 设置密度以提高图片质量
                var settings = new MagickReadSettings
                {
                    Density = new Density(300, 300),
                    BackgroundColor = MagickColors.White
                };
                using (var images = new MagickImageCollection())
                {
                    images.Read(pdfFile, settings);

                    int index = 0;
                    foreach (var image in images)
                    {
                        image.BackgroundColor = MagickColors.White;
                        image.Alpha(AlphaOption.Remove); // 移除 alpha 通道
                        image.Opaque(MagickColors.None, MagickColors.White); // 透明 → 白色

                        image.Format = MagickFormat.Png;

                        string outPath = Path.Combine(outputDir, $"page_{index + 1}.png");
                        image.Write(outPath);
                        result.Add(outPath);
                        index++;
                    }
                }
            }
            catch (MagickDelegateErrorException ex)
            {
                // 处理Ghostscript相关的错误
                throw new InvalidOperationException($"转换PDF时出错: {ex.Message}\n请确保正确安装了Ghostscript，并已将其添加到系统PATH环境变量中。", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"转换PDF为图片时出错: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// 检查系统是否安装了Ghostscript并正确配置
        /// </summary>
        private bool IsGhostscriptInstalled()
        {
            try
            {
                string gsCommand = OperatingSystem.IsWindows() ? "gswin64c.exe" : "gs";
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = gsCommand,
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // 检查版本输出是否包含数字（如9.xx）
                return !string.IsNullOrEmpty(output) && output.Any(char.IsDigit);
            }
            catch
            {
                return false;
            }
        }

        private List<string> SaveImageDirectly(string inputFile, string outputDir)
        {
            var result = new List<string>();
            string outPath = Path.Combine(outputDir, Path.GetFileName(inputFile) + ".png");

            using (var image = new MagickImage(inputFile))
            {
                image.Format = MagickFormat.Png;
                image.Write(outPath);
            }

            result.Add(outPath);
            return result;
        }

        private void ConvertOfficeToPdf(string inputFile, string outputPdf)
        {
            string sofficePath = DetectLibreOfficePath();

            if (sofficePath == null)
                throw new InvalidOperationException("未检测到 LibreOffice，请确认已安装。");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = sofficePath,
                Arguments = $"--headless --convert-to pdf --outdir \"{Path.GetDirectoryName(outputPdf)}\" \"{inputFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new Exception($"LibreOffice 转换失败: {process.StandardError.ReadToEnd()}");
            }
        }

        private string DetectLibreOfficePath()
        {
            if (OperatingSystem.IsWindows())
            {
                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string defaultPath = Path.Combine(programFiles, "LibreOffice", "program", "soffice.exe");
                if (File.Exists(defaultPath))
                    return defaultPath;
            }
            else if (OperatingSystem.IsLinux())
            {
                return "soffice"; // 假设已在 PATH
            }
            else if (OperatingSystem.IsMacOS())
            {
                string macPath = "/Applications/LibreOffice.app/Contents/MacOS/soffice";
                if (File.Exists(macPath))
                    return macPath;
            }
            return null;
        }

        public string GetFileMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public string GetMD5HashFromFile(IFormFile file)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = file.OpenReadStream())
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                // 将字节数组转换为十六进制字符串
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
