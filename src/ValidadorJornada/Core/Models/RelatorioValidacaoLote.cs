using System;
using System.Collections.Generic;
using System.Linq;

namespace ValidadorJornada.Core.Models
{
    public class RelatorioValidacaoLote
    {
        public DateTime DataProcessamento { get; set; } = DateTime.Now;
        public string ArquivoOrigem { get; set; } = string.Empty;
        public string NomePlanilha { get; set; } = string.Empty;
        public int TotalLinhas { get; set; }
        public int Validos { get; set; }
        public int Erros { get; set; }
        public int Avisos { get; set; }
        public TimeSpan TempoProcessamento { get; set; }
        
        public List<LinhaExcelValidacao> TodasLinhas { get; set; } = new();
        public List<LinhaExcelValidacao> LinhasComErro => TodasLinhas.Where(l => l.TemErro).ToList();
        public List<LinhaExcelValidacao> LinhasComAviso => TodasLinhas.Where(l => l.TemAviso).ToList();
        
        public Dictionary<string, int> ErrosPorTipo { get; set; } = new();
        public Dictionary<string, int> JornadasRepetidas { get; set; } = new();
        
        public string ResumoTexto => $"✅ {Validos} | ❌ {Erros} | ⚠️ {Avisos} | Total: {TotalLinhas}";
        
        public double PercentualSucesso => TotalLinhas > 0 ? (Validos * 100.0 / TotalLinhas) : 0;
    }

    public class ProgressoValidacao
    {
        public int LinhaAtual { get; set; }
        public int TotalLinhas { get; set; }
        public int Validos { get; set; }
        public int Erros { get; set; }
        public int Avisos { get; set; }
        
        public double Percentual => TotalLinhas > 0 ? (LinhaAtual * 100.0 / TotalLinhas) : 0;
        public string Mensagem { get; set; } = string.Empty;
    }

    public class ValidacaoLoteConfig
    {
        public bool ValidarPeriodos { get; set; } = true;
        public bool ValidarJornada { get; set; } = true;
        public bool ValidarIntervalos { get; set; } = true;
        public bool ValidarInterjornada { get; set; } = false;
        
        public int PeriodoMaximoMinutos { get; set; } = 240;
        public int JornadaMaximaMinutos { get; set; } = 480;
        public int ToleranciaMinutos { get; set; } = 2;
        
        public int ColunaInicio { get; set; } = 8; // H
        public int LinhaInicio { get; set; } = 3;
        public bool UsarHorariosAgrupados { get; set; } = false;
        public int ColunaHorariosAgrupados { get; set; } = 2; // B
    }
}
