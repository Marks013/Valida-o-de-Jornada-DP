using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ValidadorJornada.Core.Models;

namespace ValidadorJornada.Core.Services
{
    public class ValidacaoLoteService
    {
        private readonly ExcelValidatorService _excelValidator;
        
        public ValidacaoLoteService(ExcelValidatorService excelValidator)
        {
            _excelValidator = excelValidator;
            QuestPDF.Settings.License = LicenseType.Community;
        }
        
        public async Task<RelatorioValidacaoLote> ExecutarValidacao(
            string caminhoArquivo,
            ValidacaoLoteConfig config,
            IProgress<ProgressoValidacao>? progresso = null)
        {
            if (!File.Exists(caminhoArquivo))
                throw new FileNotFoundException("Arquivo não encontrado", caminhoArquivo);
            
            var extensao = Path.GetExtension(caminhoArquivo).ToLower();
            if (extensao != ".xlsx" && extensao != ".xls")
                throw new InvalidOperationException("Formato de arquivo não suportado");
            
            return await _excelValidator.ValidarArquivo(caminhoArquivo, config, progresso);
        }
        
        public void GerarRelatorioPDF(RelatorioValidacaoLote relatorio, string caminhoSaida)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));
                    
                    page.Header().Element(ComporCabecalho);
                    page.Content().Element(c => ComporConteudo(c, relatorio));
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf(caminhoSaida);
        }
        
        private void ComporCabecalho(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("RELATÓRIO DE VALIDAÇÃO DE JORNADAS")
                        .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text($"Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }
        
        private void ComporConteudo(IContainer container, RelatorioValidacaoLote relatorio)
        {
            container.Column(col =>
            {
                col.Item().PaddingTop(10).Element(c => ComporResumo(c, relatorio));
                
                if (relatorio.LinhasComErro.Any())
                {
                    col.Item().PaddingTop(15).Element(c => ComporErros(c, relatorio));
                }
                
                if (relatorio.JornadasRepetidas.Any())
                {
                    col.Item().PaddingTop(15).Element(c => ComporJornadasRepetidas(c, relatorio));
                }
            });
        }
        
        private void ComporResumo(IContainer container, RelatorioValidacaoLote relatorio)
        {
            container.Column(col =>
            {
                col.Item().Text("RESUMO GERAL").FontSize(12).Bold();
                col.Item().PaddingTop(5).Row(row =>
                {
                    row.RelativeItem().Text($"Arquivo: {relatorio.ArquivoOrigem}");
                    row.RelativeItem().Text($"Planilha: {relatorio.NomePlanilha}");
                });
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text($"Total: {relatorio.TotalLinhas} linhas");
                    row.RelativeItem().Text($"Tempo: {relatorio.TempoProcessamento.TotalSeconds:F1}s");
                });
                col.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text($"✅ Válidos: {relatorio.Validos}").FontColor(Colors.Green.Darken2);
                    row.RelativeItem().Text($"❌ Erros: {relatorio.Erros}").FontColor(Colors.Red.Darken1);
                    row.RelativeItem().Text($"⚠️ Avisos: {relatorio.Avisos}").FontColor(Colors.Orange.Darken1);
                });
                col.Item().PaddingTop(3).Text($"Taxa de sucesso: {relatorio.PercentualSucesso:F1}%")
                    .FontColor(relatorio.PercentualSucesso >= 90 ? Colors.Green.Darken2 : Colors.Orange.Darken2);
            });
        }
        
        private void ComporErros(IContainer container, RelatorioValidacaoLote relatorio)
        {
            bool temNomes = relatorio.LinhasComErro.Any(l => !string.IsNullOrWhiteSpace(l.Nome));
            
            container.Column(col =>
            {
                col.Item().Text($"DETALHAMENTO DOS ERROS ({relatorio.Erros})").FontSize(12).Bold();
                col.Item().PaddingTop(5).Table(table =>
                {
                    if (temNomes)
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                        });
                        
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Nome").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Jornada").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Erro").Bold();
                        });
                        
                        foreach (var erro in relatorio.LinhasComErro.Take(50))
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(erro.Nome ?? "");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(erro.JornadaCompleta);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(erro.TipoErro).FontSize(8);
                        }
                    }
                    else
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(3);
                        });
                        
                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Código").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Jornada").Bold();
                            header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Erro").Bold();
                        });
                        
                        foreach (var erro in relatorio.LinhasComErro.Take(50))
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(erro.Matricula ?? "");
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(erro.JornadaCompleta);
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(erro.TipoErro).FontSize(8);
                        }
                    }
                });
                
                if (relatorio.LinhasComErro.Count > 50)
                {
                    col.Item().PaddingTop(5).Text($"... e mais {relatorio.LinhasComErro.Count - 50} erros")
                        .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                }
            });
        }
        
        private void ComporJornadasRepetidas(IContainer container, RelatorioValidacaoLote relatorio)
        {
            container.Column(col =>
            {
                col.Item().Text("JORNADAS MAIS FREQUENTES").FontSize(12).Bold();
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(1);
                    });
                    
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Jornada").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Colaboradores").Bold();
                    });
                    
                    foreach (var jornada in relatorio.JornadasRepetidas.OrderByDescending(j => j.Value).Take(20))
                    {
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(jornada.Key);
                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(5).Text(jornada.Value.ToString());
                    }
                });
            });
        }
    }
}