using System.Collections.Generic;

namespace ValidadorJornada.Core.Models
{
    public class LinhaExcelValidacao
    {
        public int NumeroLinha { get; set; }
        public string? Matricula { get; set; }
        public string? Nome { get; set; }
        public string? Cargo { get; set; }
        public List<string> Horarios { get; set; } = new();
        public string HorariosOriginais { get; set; } = string.Empty;
        public ValidationResult? Resultado { get; set; }
        public bool TemErro => Resultado?.Valido == false;
        public bool TemAviso => Resultado?.Valido == true && !string.IsNullOrEmpty(Resultado?.Mensagem) && Resultado.Mensagem.Contains("⚠️");
        
        public string JornadaCompleta => string.Join(" - ", Horarios);
        
        public string TipoErro
        {
            get
            {
                if (Resultado == null || Resultado.Valido) return string.Empty;
                return Resultado.Mensagem ?? "Erro desconhecido";
            }
        }
    }
}
