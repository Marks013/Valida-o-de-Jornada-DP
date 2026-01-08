using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using ExcelDataReader;
using ValidadorJornada.Core.Models;

namespace ValidadorJornada.Core.Helpers
{
    public static class ExcelLoteHelper
    {
        private static readonly Regex RegexHorario = new(@"\b(\d{1,2}):?(\d{2})\b", RegexOptions.Compiled);
        
        public static List<LinhaExcelValidacao> LerLinhasParaValidacao(
            DataTable planilha, 
            ValidacaoLoteConfig config)
        {
            var linhas = new List<LinhaExcelValidacao>();
            
            for (int i = config.LinhaInicio - 1; i < planilha.Rows.Count; i++)
            {
                var row = planilha.Rows[i];
                var linha = new LinhaExcelValidacao { NumeroLinha = i + 1 };
                
                if (config.UsarHorariosAgrupados)
                {
                    LerHorariosAgrupados(row, linha, config);
                }
                else
                {
                    LerHorariosIndividuais(row, linha, config);
                }
                
                if (linha.Horarios.Count == 0) continue;
                
                if (config.UsarHorariosAgrupados)
                {
                    linha.Matricula = ObterValorCelula(row, 0);
                    linha.Nome = string.Empty;
                    linha.Cargo = string.Empty;
                }
                else
                {
                    linha.Matricula = ObterValorCelula(row, 0); // Coluna A
                    linha.Nome = ObterValorCelula(row, 2); // ✅ CORRIGIDO: Coluna C (índice 2)
                    linha.Cargo = ObterValorCelula(row, 4); // Coluna E
                }
                
                if (config.UsarHorariosAgrupados)
                {
                    if (string.IsNullOrWhiteSpace(linha.Matricula) || EhCabecalho(linha.Matricula))
                        continue;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(linha.Nome) || EhCabecalho(linha.Nome))
                        continue;
                    
                    if (EhLinhaTitulo(linha.Nome, linha.Cargo ?? string.Empty))
                        continue;
                }
                
                linhas.Add(linha);
            }
            
            return linhas;
        }
        
        private static void LerHorariosIndividuais(DataRow row, LinhaExcelValidacao linha, ValidacaoLoteConfig config)
        {
            var horarios = new List<string>();
            var colunasHorario = new[] { 8, 10, 11, 13 }; // I, K, L, N
            
            foreach (var col in colunasHorario)
            {
                if (col >= row.Table.Columns.Count) continue;
                
                // ✅ CRÍTICO: Usar valor RAW da célula, não ToString()
                var valorRaw = row[col];
                if (valorRaw == null || valorRaw == DBNull.Value) continue;
                
                var horario = NormalizarHorarioRaw(valorRaw);
                if (!string.IsNullOrEmpty(horario) && horario != "00:00")
                {
                    horarios.Add(horario);
                }
            }
            
            linha.Horarios = horarios;
            linha.HorariosOriginais = string.Join(" ", horarios);
        }
        
        private static void LerHorariosAgrupados(DataRow row, LinhaExcelValidacao linha, ValidacaoLoteConfig config)
        {
            var textoHorarios = ObterValorCelula(row, config.ColunaHorariosAgrupados - 1);
            if (string.IsNullOrWhiteSpace(textoHorarios)) return;
            
            linha.HorariosOriginais = textoHorarios;
            linha.Horarios = ExtrairHorariosDoTexto(textoHorarios);
        }
        
        public static List<string> ExtrairHorariosDoTexto(string texto)
        {
            var horarios = new List<string>();
            texto = LimparTextoHorarios(texto);
            
            var matches = RegexHorario.Matches(texto);
            foreach (Match match in matches)
            {
                var horas = int.Parse(match.Groups[1].Value);
                var minutos = int.Parse(match.Groups[2].Value);
                
                if (horas >= 0 && horas <= 23 && minutos >= 0 && minutos <= 59)
                {
                    horarios.Add($"{horas:D2}:{minutos:D2}");
                }
            }
            
            var horariosNumeros = Regex.Matches(texto, @"\b(\d{3,4})\b");
            foreach (Match match in horariosNumeros)
            {
                var numero = match.Value.PadLeft(4, '0');
                var h = int.Parse(numero.Substring(0, 2));
                var m = int.Parse(numero.Substring(2, 2));
                
                if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
                {
                    var horario = $"{h:D2}:{m:D2}";
                    if (!horarios.Contains(horario))
                    {
                        horarios.Add(horario);
                    }
                }
            }
            
            return horarios.Distinct().OrderBy(h => h).ToList();
        }
        
        private static string LimparTextoHorarios(string texto)
        {
            texto = texto.Replace("às", " ")
                        .Replace("Às", " ")
                        .Replace("as", " ")
                        .Replace("e", " ")
                        .Replace(",", " ")
                        .Replace(";", " ")
                        .Replace("-", " ")
                        .Replace("h", ":")
                        .Replace("H", ":");
            
            return Regex.Replace(texto, @"\s+", " ").Trim();
        }
        
        public static string NormalizarHorario(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
            
            valor = valor.Trim();
            
            // ✅ PRIORIDADE 1: Formato HH:MM já válido
            if (valor.Contains(":"))
            {
                var partes = valor.Split(':');
                if (partes.Length >= 2 && 
                    int.TryParse(partes[0], out int h) && 
                    int.TryParse(partes[1], out int m))
                {
                    if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
                    {
                        return $"{h:D2}:{m:D2}";
                    }
                }
            }
            
            // ✅ PRIORIDADE 2: Formato Excel decimal TIME
            if (double.TryParse(valor, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double fracao))
            {
                if (fracao > 0 && fracao < 1)
                {
                    var totalMinutos = fracao * 1440.0;
                    var minutosArredondados = (int)Math.Round(totalMinutos);
                    
                    if (totalMinutos - minutosArredondados > 0.5)
                        minutosArredondados++;
                    
                    if (minutosArredondados >= 1440) minutosArredondados = 1439;
                    
                    var h = minutosArredondados / 60;
                    var m = minutosArredondados % 60;
                    
                    if (h >= 0 && h <= 23)
                    {
                        return $"{h:D2}:{m:D2}";
                    }
                }
                
                // ✅ PRIORIDADE 3: Número inteiro (0-23 = horas)
                if (fracao >= 0 && fracao <= 23 && fracao == Math.Floor(fracao))
                {
                    return $"{(int)fracao:D2}:00";
                }
            }
            
            // ✅ PRIORIDADE 4: Formato numérico HHMM
            if (int.TryParse(valor, out int numero))
            {
                if (numero >= 0 && numero <= 2359)
                {
                    var h = numero / 100;
                    var m = numero % 100;
                    if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
                    {
                        return $"{h:D2}:{m:D2}";
                    }
                }
            }
            
            return string.Empty;
        }
        
        /// <summary>
        /// ✅ MÉTODO CORRIGIDO: Normaliza valor RAW da célula Excel
        /// </summary>
        private static string NormalizarHorarioRaw(object valorRaw)
        {
            // ✅ PRIORIDADE 1: DateTime
            if (valorRaw is DateTime dt)
            {
                return $"{dt.Hour:D2}:{dt.Minute:D2}";
            }
            
            // ✅ PRIORIDADE 2: Double direto
            if (valorRaw is double fracao)
            {
                if (fracao > 0 && fracao < 1)
                {
                    var totalMinutos = (int)Math.Round(fracao * 1440.0);
                    if (totalMinutos >= 1440) totalMinutos = 1439;
                    if (totalMinutos < 0) totalMinutos = 0;
                    
                    var h = totalMinutos / 60;
                    var m = totalMinutos % 60;
                    return $"{h:D2}:{m:D2}";
                }
            }
            
            // ✅ PRIORIDADE 3: String com formato decimal
            var valorStr = valorRaw.ToString()?.Trim();
            if (!string.IsNullOrEmpty(valorStr))
            {
                // Tenta parsear como double primeiro
                if (double.TryParse(valorStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double frac))
                {
                    if (frac > 0 && frac < 1)
                    {
                        var totalMin = (int)Math.Round(frac * 1440.0);
                        if (totalMin >= 1440) totalMin = 1439;
                        
                        var h = totalMin / 60;
                        var m = totalMin % 60;
                        return $"{h:D2}:{m:D2}";
                    }
                }
                
                // ✅ CRÍTICO: Detecta formato HH:MM e ajusta erros conhecidos do Excel
                if (valorStr.Contains(":"))
                {
                    var resultado = NormalizarHorario(valorStr);
                    
                    // Ajusta horários que caíram 1 minuto devido a imprecisão do Excel
                    if (!string.IsNullOrEmpty(resultado))
                    {
                        var partes = resultado.Split(':');
                        if (partes.Length == 2 && 
                            int.TryParse(partes[0], out int h) && 
                            int.TryParse(partes[1], out int m))
                        {
                            // Padrão de erro do Excel: :59, :29, :49, :19 → arredonda para cima
                            if (m == 59 || m == 29 || m == 49 || m == 19 || m == 9 || m == 39)
                            {
                                m++;
                                if (m >= 60)
                                {
                                    m = 0;
                                    h++;
                                    if (h >= 24) h = 0;
                                }
                                return $"{h:D2}:{m:D2}";
                            }
                        }
                    }
                    
                    return resultado;
                }
                
                // Fallback para outros formatos
                return NormalizarHorario(valorStr);
            }
            
            return string.Empty;
        }
        
        private static string? ObterValorCelula(DataRow row, int index)
        {
            if (index < 0 || index >= row.Table.Columns.Count) return null;
            
            var valor = row[index];
            if (valor == null || valor == DBNull.Value) return null;
            
            return valor.ToString()?.Trim();
        }
        
        private static bool EhCabecalho(string texto)
        {
            var upper = texto.ToUpperInvariant();
            return upper.Contains("NOME") || 
                   upper.Contains("FUNCIONARIO") || 
                   upper.Contains("FUNCIONÁRIO") ||
                   upper.Contains("CARGO") || 
                   upper.Contains("HORARIO") ||
                   upper.Contains("HORÁRIO") ||
                   upper.Contains("MATRICULA") ||
                   upper.Contains("MATRÍCULA") ||
                   upper.Contains("CODIGO") ||
                   upper.Contains("CÓDIGO") ||
                   upper.Contains("QUADRO");
        }
        
        private static bool EhLinhaTitulo(string nome, string cargo)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return false;
            
            var nomeUpper = nome.ToUpperInvariant();
            
            if (nomeUpper.Contains("SUPERMERCADOS") ||
                nomeUpper.Contains("LTDA") ||
                nomeUpper.Contains("S/A") ||
                nomeUpper.Contains("S.A.") ||
                nomeUpper.Contains("ME") ||
                nomeUpper.Contains("EIRELI") ||
                nomeUpper.Contains("PLANALTO") ||
                nomeUpper.Contains("PLANEJAMENTO") ||
                nomeUpper.Contains("DEPARTAMENTO") ||
                nomeUpper.Contains("SETOR") ||
                nomeUpper.Contains("SEÇÃO") ||
                nomeUpper.Contains("DIVISÃO"))
            {
                return true;
            }
            
            if (string.IsNullOrWhiteSpace(cargo) && nome.Length > 40)
                return true;
            
            return false;
        }
    }
}