using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader;
using OfficeOpenXml;
using ValidadorJornada.Core.Helpers;
using ValidadorJornada.Core.Models;

namespace ValidadorJornada.Core.Services
{
    public class ExcelValidatorService
    {
        private readonly ValidacaoLoteJornadaValidator _validator;
        private const int COR_VERDE = 0x59F089;
        private const int COR_VERMELHO = 0x0000FF;
        
        public ExcelValidatorService(ValidacaoLoteJornadaValidator validator)
        {
            _validator = validator;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }
        
        public async Task<RelatorioValidacaoLote> ValidarArquivo(
            string caminhoArquivo,
            ValidacaoLoteConfig config,
            IProgress<ProgressoValidacao>? progresso = null)
        {
            var inicio = DateTime.Now;
            var relatorio = new RelatorioValidacaoLote
            {
                ArquivoOrigem = Path.GetFileName(caminhoArquivo)
            };
            
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            _validator.ConfigurarValidacao(config);
            
            DataTable planilha;
            using (var stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var dataset = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
                });
                
                planilha = dataset.Tables[0];
                relatorio.NomePlanilha = planilha.TableName;
            }
            
            var linhas = ExcelLoteHelper.LerLinhasParaValidacao(planilha, config);
            relatorio.TotalLinhas = linhas.Count;
            
            int contador = 0;
            foreach (var linha in linhas)
            {
                contador++;
                
                linha.Resultado = _validator.ValidarHorariosArray(linha.Horarios.ToArray());
                
                if (linha.Resultado.Valido && !linha.TemAviso)
                    relatorio.Validos++;
                else if (linha.TemErro)
                    relatorio.Erros++;
                else if (linha.TemAviso)
                    relatorio.Avisos++;
                
                relatorio.TodasLinhas.Add(linha);
                
                progresso?.Report(new ProgressoValidacao
                {
                    LinhaAtual = contador,
                    TotalLinhas = linhas.Count,
                    Validos = relatorio.Validos,
                    Erros = relatorio.Erros,
                    Avisos = relatorio.Avisos,
                    Mensagem = $"Validando linha {contador}/{linhas.Count}"
                });
                
                if (contador % 10 == 0)
                    await Task.Delay(1);
            }
            
            GerarResumoJornadas(relatorio);
            await AplicarCoresNoArquivo(caminhoArquivo, relatorio, config);
            
            relatorio.TempoProcessamento = DateTime.Now - inicio;
            return relatorio;
        }
        
        private void GerarResumoJornadas(RelatorioValidacaoLote relatorio)
        {
            foreach (var linha in relatorio.TodasLinhas.Where(l => l.Horarios.Count >= 2))
            {
                var jornada = linha.JornadaCompleta;
                if (relatorio.JornadasRepetidas.ContainsKey(jornada))
                    relatorio.JornadasRepetidas[jornada]++;
                else
                    relatorio.JornadasRepetidas[jornada] = 1;
            }
        }
        
        private async Task AplicarCoresNoArquivo(
            string caminhoArquivo,
            RelatorioValidacaoLote relatorio,
            ValidacaoLoteConfig config)
        {
            await Task.Run(() =>
            {
                byte[] fileBytes;
                using (var readStream = new FileStream(caminhoArquivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using var memStream = new MemoryStream();
                    readStream.CopyTo(memStream);
                    fileBytes = memStream.ToArray();
                }
                
                using var package = new ExcelPackage(new MemoryStream(fileBytes));
                var worksheet = package.Workbook.Worksheets[0];
                
                foreach (var linha in relatorio.TodasLinhas)
                {
                    var rowIndex = linha.NumeroLinha;
                    var colunaIndicador = 9;
                    var celula = worksheet.Cells[rowIndex, colunaIndicador];
                    
                    if (linha.TemErro)
                    {
                        celula.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        celula.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    }
                    else if (linha.Resultado?.Valido == true)
                    {
                        celula.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        celula.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(143, 240, 89));
                    }
                    
                    var celulaMensagem = worksheet.Cells[rowIndex, 15];
                    if (linha.TemErro)
                    {
                        celulaMensagem.Value = linha.TipoErro;
                        celulaMensagem.Style.Font.Bold = true;
                        celulaMensagem.Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    }
                    else if (linha.Resultado?.Valido == true)
                    {
                        celulaMensagem.Value = linha.Resultado.Mensagem;
                        celulaMensagem.Style.Font.Bold = false;
                        celulaMensagem.Style.Font.Color.SetColor(System.Drawing.Color.Green);
                    }
                }
                
                GerarAbaErros(package, relatorio);
                
                using (var saveStream = new FileStream(caminhoArquivo, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    package.SaveAs(saveStream);
                }
            });
        }
        
        private void GerarAbaErros(ExcelPackage package, RelatorioValidacaoLote relatorio)
        {
            var abaExistente = package.Workbook.Worksheets["Erros_Validacao"];
            if (abaExistente != null)
            {
                package.Workbook.Worksheets.Delete(abaExistente);
            }
            
            var abaErros = package.Workbook.Worksheets.Add("Erros_Validacao");
            
            abaErros.Cells["A1:M1"].Merge = true;
            abaErros.Cells["A1"].Value = "RELATÓRIO DE ERROS DE HORÁRIO";
            abaErros.Cells["A1"].Style.Font.Bold = true;
            abaErros.Cells["A1"].Style.Font.Size = 14;
            abaErros.Cells["A1"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            abaErros.Cells["A1"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            abaErros.Cells["A1"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            abaErros.Row(1).Height = 30;
            
            bool temNomes = relatorio.LinhasComErro.Any(l => !string.IsNullOrWhiteSpace(l.Nome));
            
            if (temNomes)
            {
                abaErros.Cells["A2"].Value = "Matrícula";
                abaErros.Cells["B2"].Value = "Nome";
                abaErros.Cells["C2"].Value = "Cargo";
                abaErros.Cells["D2"].Value = "Jornada Completa";
                abaErros.Cells["E2"].Value = "Tipo de Erro";
            }
            else
            {
                abaErros.Cells["A2"].Value = "Código";
                abaErros.Cells["B2"].Value = "Jornada Completa";
                abaErros.Cells["C2"].Value = "Tipo de Erro";
            }
            
            abaErros.Cells["A2:E2"].Style.Font.Bold = true;
            abaErros.Cells["A2:E2"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            abaErros.Cells["A2:E2"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            abaErros.Cells["A2:E2"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            abaErros.Row(2).Height = 25;
            
            int linha = 3;
            
            if (!relatorio.LinhasComErro.Any())
            {
                var colFinal = temNomes ? "E" : "C";
                abaErros.Cells[$"A3:{colFinal}3"].Merge = true;
                abaErros.Cells["A3"].Value = "Nenhum erro encontrado!";
                abaErros.Cells["A3"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                abaErros.Cells["A3"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                abaErros.Cells["A3"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(143, 240, 89));
                abaErros.Cells["A3"].Style.Font.Bold = true;
                abaErros.Row(3).Height = 25;
            }
            else
            {
                foreach (var erro in relatorio.LinhasComErro)
                {
                    if (temNomes)
                    {
                        abaErros.Cells[linha, 1].Value = erro.Matricula;
                        abaErros.Cells[linha, 2].Value = erro.Nome;
                        abaErros.Cells[linha, 3].Value = erro.Cargo;
                        abaErros.Cells[linha, 4].Value = erro.JornadaCompleta;
                        abaErros.Cells[linha, 5].Value = erro.TipoErro;
                        abaErros.Cells[linha, 5].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                        abaErros.Cells[linha, 5].Style.Font.Bold = true;
                        
                        if (linha % 2 == 1)
                        {
                            abaErros.Cells[$"A{linha}:E{linha}"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            abaErros.Cells[$"A{linha}:E{linha}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));
                        }
                    }
                    else
                    {
                        abaErros.Cells[linha, 1].Value = erro.Matricula;
                        abaErros.Cells[linha, 2].Value = erro.JornadaCompleta;
                        abaErros.Cells[linha, 3].Value = erro.TipoErro;
                        abaErros.Cells[linha, 3].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                        abaErros.Cells[linha, 3].Style.Font.Bold = true;
                        
                        if (linha % 2 == 1)
                        {
                            abaErros.Cells[$"A{linha}:C{linha}"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            abaErros.Cells[$"A{linha}:C{linha}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));
                        }
                    }
                    
                    linha++;
                }
                
                var colFinal = temNomes ? "E" : "C";
                abaErros.Cells[$"A2:{colFinal}{linha - 1}"].Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                abaErros.Cells[$"A2:{colFinal}{linha - 1}"].Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                abaErros.Cells[$"A2:{colFinal}{linha - 1}"].Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                abaErros.Cells[$"A2:{colFinal}{linha - 1}"].Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
            }
            
            GerarResumoJornadasAba(abaErros, relatorio, linha + 1, temNomes);
            
            if (temNomes)
            {
                abaErros.Column(1).Width = 12;
                abaErros.Column(2).Width = 30;
                abaErros.Column(3).Width = 25;
                abaErros.Column(4).Width = 35;
                abaErros.Column(5).Width = 50;
            }
            else
            {
                abaErros.Column(1).Width = 15;
                abaErros.Column(2).Width = 40;
                abaErros.Column(3).Width = 60;
            }
        }
        
        private void GerarResumoJornadasAba(ExcelWorksheet ws, RelatorioValidacaoLote relatorio, int linhaInicio, bool temNomes)
        {
            if (!relatorio.JornadasRepetidas.Any()) return;
            
            var linha = linhaInicio + 1;
            var colFinal = temNomes ? "E" : "C";
            
            ws.Cells[$"A{linha}:{colFinal}{linha}"].Merge = true;
            ws.Cells[$"A{linha}"].Value = "RESUMO - JORNADAS REPETIDAS";
            ws.Cells[$"A{linha}"].Style.Font.Bold = true;
            ws.Cells[$"A{linha}"].Style.Font.Size = 12;
            ws.Cells[$"A{linha}"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            ws.Cells[$"A{linha}"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            ws.Cells[$"A{linha}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            ws.Row(linha).Height = 22;
            linha++;
            
            ws.Cells[$"A{linha}"].Value = "Jornada Completa";
            
            if (temNomes)
            {
                ws.Cells[$"B{linha}:E{linha}"].Merge = true;
                ws.Cells[$"B{linha}"].Value = "Quantidade";
            }
            else
            {
                ws.Cells[$"B{linha}:C{linha}"].Merge = true;
                ws.Cells[$"B{linha}"].Value = "Quantidade";
            }
            
            ws.Cells[$"A{linha}:{colFinal}{linha}"].Style.Font.Bold = true;
            ws.Cells[$"A{linha}:{colFinal}{linha}"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            ws.Cells[$"A{linha}:{colFinal}{linha}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            ws.Row(linha).Height = 20;
            linha++;
            
            foreach (var jornada in relatorio.JornadasRepetidas.OrderByDescending(x => x.Value))
            {
                ws.Cells[linha, 1].Value = jornada.Key;
                
                if (temNomes)
                {
                    ws.Cells[$"B{linha}:E{linha}"].Merge = true;
                    ws.Cells[$"B{linha}"].Value = jornada.Value;
                    ws.Cells[$"B{linha}"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }
                else
                {
                    ws.Cells[$"B{linha}:C{linha}"].Merge = true;
                    ws.Cells[$"B{linha}"].Value = jornada.Value;
                    ws.Cells[$"B{linha}"].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                }
                
                if (linha % 2 == 0)
                {
                    ws.Cells[$"A{linha}:{colFinal}{linha}"].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    ws.Cells[$"A{linha}:{colFinal}{linha}"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(240, 240, 240));
                }
                
                linha++;
            }
        }
    }
}