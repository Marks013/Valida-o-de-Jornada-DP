using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ValidadorJornada.Core.Models;

namespace ValidadorJornada.Core.Helpers
{
    public static class PdfHelper
    {
        static PdfHelper()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static byte[] CreateJornadasDocumentIndividual(List<JornadaEditavel> jornadas, DateTime dataReferencia)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContentIndividual(content, jornadas));
                    page.Footer().Element(ComposeFooter);
                });
            }).GeneratePdf();
        }

        private static void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("SOLICITAÇÃO DE ALTERAÇÃO DE JORNADA")
                        .FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
                    
                    column.Item().PaddingTop(3).Text("Validador de Jornada DP")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private static void ComposeContentIndividual(IContainer container, List<JornadaEditavel> jornadas)
        {
            container.PaddingVertical(15).Column(column =>
            {
                column.Spacing(8);

                // Agrupa jornadas idênticas
                var gruposJornadas = jornadas
                    .GroupBy(j => new { j.Jornada, j.Codigo, j.DataAlteracao })
                    .Select(g => new
                    {
                        Jornada = g.Key.Jornada,
                        Codigo = g.Key.Codigo,
                        DataAlteracao = g.Key.DataAlteracao,
                        Colaboradores = g.ToList()
                    });

                foreach (var grupo in gruposJornadas)
                {
                    column.Item().Element(c => ComposeJornadaBlockAgrupado(
                        c, 
                        grupo.Colaboradores, 
                        grupo.Jornada, 
                        grupo.Codigo, 
                        grupo.DataAlteracao
                    ));
                }

                // Assinatura
                column.Item().PaddingTop(30).AlignCenter().Column(col =>
                {
                    col.Item().Width(300).BorderBottom(1).BorderColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).AlignCenter().Text("Assinatura do Gerente / Responsável")
                        .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                });
            });
        }

        private static void ComposeJornadaBlockAgrupado(
            IContainer container, 
            List<JornadaEditavel> colaboradores,
            string jornada,
            string? codigo,
            DateTime dataAlteracao)
        {
            container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Column(column =>
            {
                column.Spacing(6);

                // Lista de Colaboradores (separados por barra)
                foreach (var colab in colaboradores)
                {
                    var isUltimo = colab == colaboradores.Last();
                    
                    // Linha 1: Matrícula + Nome
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(60).Text("Matrícula:").Bold().FontSize(9);
                        row.ConstantItem(120).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                            .PaddingLeft(4).PaddingBottom(2)
                            .Text(string.IsNullOrWhiteSpace(colab.Matricula) ? "" : colab.Matricula)
                            .FontSize(9);
                        
                        row.ConstantItem(15);
                        
                        row.ConstantItem(40).Text("Nome:").Bold().FontSize(9);
                        row.RelativeItem().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                            .PaddingLeft(4).PaddingBottom(2)
                            .Text(string.IsNullOrWhiteSpace(colab.Nome) ? "" : colab.Nome)
                            .FontSize(9);
                    });

                    // Linha 2: Cargo
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(40).Text("Cargo:").Bold().FontSize(9);
                        row.RelativeItem().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                            .PaddingLeft(4).PaddingBottom(2)
                            .Text(string.IsNullOrWhiteSpace(colab.Cargo) ? "" : colab.Cargo)
                            .FontSize(9);
                    });

                    // Barra separadora (exceto último)
                    if (!isUltimo)
                    {
                        column.Item().PaddingVertical(4).LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten2);
                    }
                }

                // Data da Alteração (única para o grupo)
                column.Item().PaddingTop(6).Row(row =>
                {
                    row.ConstantItem(100).Text("Data da Alteração:").Bold().FontSize(9);
                    row.ConstantItem(80).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                        .PaddingLeft(4).PaddingBottom(2)
                        .Text(dataAlteracao.ToString("dd/MM/yyyy"))
                        .FontSize(9);
                });

                // Jornada + Código (única para o grupo)
                column.Item().PaddingTop(4).Background(Colors.Grey.Lighten3)
                    .Padding(8).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Jornada:").Bold().FontSize(9)
                                .FontColor(Colors.Grey.Darken2);
                            
                            col.Item().PaddingTop(2).Text(jornada)
                                .FontSize(11).Bold().FontColor(Colors.Blue.Darken1);
                        });
                        
                        if (!string.IsNullOrWhiteSpace(codigo))
                        {
                            row.ConstantItem(80).Column(col =>
                            {
                                col.Item().Text("Código:").Bold().FontSize(9)
                                    .FontColor(Colors.Grey.Darken2);
                                
                                col.Item().PaddingTop(2).Text(codigo)
                                    .FontSize(11).Bold().FontColor(Colors.Green.Darken1);
                            });
                        }
                    });
            });
        }

        private static void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Column(column =>
            {
                column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten1);
                
                column.Item().PaddingTop(8).Text(text =>
                {
                    text.Span("Relatório gerado em ").FontSize(7).FontColor(Colors.Grey.Darken1);
                    text.Span($"{DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm}").FontSize(7).Bold();
                });

                column.Item().PaddingTop(2).Text("Validado com Validador Jornada DP – @Samuel-Fernandes")
                    .FontSize(7).FontColor(Colors.Blue.Darken1);
            });
        }

        public static string SanitizeFileName(string fileName)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}