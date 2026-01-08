namespace ValidadorJornada.Core.Models
{
    public class ValidationResult
    {
        public bool Valido { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public string DuracaoCalculada { get; set; } = "00:00";
        public string TipoDia { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public int HorasSemanais { get; set; }
        public int HorasMensais { get; set; }
        public string? Intervalo { get; set; }
    }
} 
