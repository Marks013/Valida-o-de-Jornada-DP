using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ValidadorJornada.Core.Models;
using ValidadorJornada.Core.Helpers;

namespace ValidadorJornada.Core.Services
{
    public class ExportService
    {
        private readonly string _exportPath;
        private readonly string _logPath;

        public ExportService()
        {
            // Salva na Área de Trabalho
            _exportPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logPath = Path.Combine(appData, "ValidadorJornada", "logs");

            Directory.CreateDirectory(_logPath);
        }

        public ExportResult ExportarJornadasIndividuais(
            List<JornadaEditavel> jornadas, 
            DateTime dataReferencia)
        {
            try
            {
                if (jornadas.Count == 0)
                    return new ExportResult 
                    { 
                        Sucesso = false, 
                        Mensagem = "Nenhuma jornada válida selecionada" 
                    };

                var pdfBytes = PdfHelper.CreateJornadasDocumentIndividual(jornadas, dataReferencia);
                var fileName = GerarNomeArquivo(dataReferencia);
                var fullPath = Path.Combine(_exportPath, fileName);

                File.WriteAllBytes(fullPath, pdfBytes);
                LogExport(jornadas.Count, fullPath);

                return new ExportResult
                {
                    Sucesso = true,
                    Mensagem = "PDF gerado com sucesso na Área de Trabalho!",
                    CaminhoArquivo = fullPath,
                    TotalJornadas = jornadas.Count
                };
            }
            catch (IOException)
            {
                return new ExportResult
                {
                    Sucesso = false,
                    Mensagem = "Falha ao gerar PDF. Verifique se o arquivo está em uso."
                };
            }
            catch (Exception ex)
            {
                LogError(ex);
                return new ExportResult
                {
                    Sucesso = false,
                    Mensagem = $"Erro inesperado: {ex.Message}"
                };
            }
        }

        private string GerarNomeArquivo(DateTime data)
        {
            var nomeBase = $"Alteração_de_Jornada_{data:dd-MM-yyyy}";
            var nome = PdfHelper.SanitizeFileName(nomeBase) + ".pdf";
            
            var fullPath = Path.Combine(_exportPath, nome);
            int contador = 1;
            
            while (File.Exists(fullPath))
            {
                nome = $"{nomeBase}_{contador}.pdf";
                fullPath = Path.Combine(_exportPath, nome);
                contador++;
            }

            return nome;
        }

        private void LogExport(int total, string caminho)
        {
            try
            {
                var logFile = Path.Combine(_logPath, "export.log");
                var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {total} jornadas exportadas → {caminho}\n";
                File.AppendAllText(logFile, entry);
            }
            catch { }
        }

        private void LogError(Exception ex)
        {
            try
            {
                var logFile = Path.Combine(_logPath, "export_errors.log");
                var entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERRO: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText(logFile, entry);
            }
            catch { }
        }

        public string GetExportPath() => _exportPath;
    }

    public class ExportResult
    {
        public bool Sucesso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string? CaminhoArquivo { get; set; }
        public int TotalJornadas { get; set; }
    }
}
