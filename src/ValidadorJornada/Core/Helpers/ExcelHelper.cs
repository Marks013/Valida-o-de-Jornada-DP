using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ExcelDataReader;

namespace ValidadorJornada.Core.Helpers
{
    public static class ExcelHelper
    {
        static ExcelHelper()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static List<(string Codigo, string Horarios)> LerArquivo(string caminhoArquivo, bool skipHeader = true)
        {
            var lista = new List<(string, string)>();

            using var stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            int linhaAtual = 0;
            while (reader.Read())
            {
                if (skipHeader && linhaAtual == 0)
                {
                    linhaAtual++;
                    continue;
                }

                var codigo = reader.GetValue(0)?.ToString()?.Trim();
                var horarios = reader.GetValue(1)?.ToString()?.Trim();

                if (!string.IsNullOrEmpty(codigo) && !string.IsNullOrEmpty(horarios))
                {
                    lista.Add((codigo, horarios));
                }
                
                linhaAtual++;
            }

            return lista;
        }

        public static bool ValidarFormato(string caminhoArquivo)
        {
            try
            {
                using var stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                return reader.FieldCount >= 2;
            }
            catch
            {
                return false;
            }
        }
    }
}