using System.Collections.Generic;

namespace ValidadorJornada.Core.Models
{
    public class Jornada
    {
        public int DuracaoMinutos { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int HorasSemanais { get; set; }
        public int HorasMensais { get; set; }
        public int IntervaloMin { get; set; }
        public int IntervaloMax { get; set; }
        public List<string> DiasValidos { get; set; } = new();
    }
}