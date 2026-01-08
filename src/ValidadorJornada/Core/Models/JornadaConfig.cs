using System.Collections.Generic;

namespace ValidadorJornada.Core.Models
{
    public class JornadaConfig
    {
        public List<Jornada> Jornadas { get; set; } = new();
        public double PeriodoMaximoHoras { get; set; }
        public int InterjornadaMinimaMinutos { get; set; } = 660;
        public int PeriodoMaximoSemIntervaloMinutos { get; set; } = 240;
        public int CacheHistoricoMinutos { get; set; } = 30;
        public bool SkipHeadersOnImport { get; set; } = true;
    }
}