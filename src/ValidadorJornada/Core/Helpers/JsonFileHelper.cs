using System;
using System.IO;
using System.Text.Json;

namespace ValidadorJornada.Core.Helpers
{
    /// <summary>
    /// Helper centralizado para operações JSON com backup automático
    /// </summary>
    public static class JsonFileHelper
    {
        private static readonly JsonSerializerOptions _readOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        /// <summary>
        /// Carrega objeto JSON com tratamento de erros e backup
        /// </summary>
        public static T? Load<T>(string filePath) where T : class, new()
        {
            try
            {
                if (!File.Exists(filePath))
                    return new T();

                var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                
                if (string.IsNullOrWhiteSpace(json))
                    return new T();

                return JsonSerializer.Deserialize<T>(json, _readOptions) ?? new T();
            }
            catch (JsonException ex)
            {
                LogError(filePath, $"Arquivo corrompido: {ex.Message}");
                BackupCorruptedFile(filePath);
                return new T();
            }
            catch (Exception ex)
            {
                LogError(filePath, $"Erro ao ler: {ex.Message}");
                return new T();
            }
        }

        /// <summary>
        /// Salva objeto como JSON
        /// </summary>
        public static void Save<T>(string filePath, T data) where T : class
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(data, _writeOptions);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LogError(filePath, $"Erro ao salvar: {ex.Message}");
                throw new InvalidOperationException($"Não foi possível salvar o arquivo: {filePath}", ex);
            }
        }

        /// <summary>
        /// Cria backup de arquivo corrompido
        /// </summary>
        private static void BackupCorruptedFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var backupPath = $"{filePath}.corrupted_{DateTime.Now:yyyyMMddHHmmss}.bak";
                    File.Copy(filePath, backupPath);
                    File.Delete(filePath);
                }
            }
            catch
            {
                // Silencioso
            }
        }

        /// <summary>
        /// Log de erros
        /// </summary>
        private static void LogError(string filePath, string message)
        {
            try
            {
                var logPath = Path.Combine(
                    Path.GetDirectoryName(filePath) ?? "",
                    "errors.log"
                );
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {Path.GetFileName(filePath)} - {message}\n";
                File.AppendAllText(logPath, logEntry, System.Text.Encoding.UTF8);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine(message);
            }
        }
    }
}
