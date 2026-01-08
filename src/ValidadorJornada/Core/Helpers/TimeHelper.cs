using System;
using System.Globalization;

namespace ValidadorJornada.Core.Helpers
{
    public static class TimeHelper
    {
        /// <summary>
        /// Parse horário string "HH:MM" para TimeSpan
        /// </summary>
        public static bool TryParseHorario(string horario, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            
            if (string.IsNullOrWhiteSpace(horario))
                return false;
                
            var partes = horario.Split(':');
            if (partes.Length != 2)
                return false;
                
            if (int.TryParse(partes[0], out var horas) && 
                int.TryParse(partes[1], out var minutos))
            {
                if (horas >= 0 && horas <= 23 && minutos >= 0 && minutos <= 59)
                {
                    time = new TimeSpan(horas, minutos, 0);
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Formata duração em minutos para string HH:MM ou formato legível
        /// </summary>
        /// <param name="minutos">Duração em minutos</param>
        /// <param name="formatoLegivel">Se true, retorna "Xh" ou "XhYY", se false retorna "HH:MM"</param>
        public static string FormatarDuracao(int minutos, bool formatoLegivel = false)
        {
            if (formatoLegivel)
            {
                // Formato: 65min → "1h05" | 120min → "2h" | 45min → "45min"
                if (minutos >= 60)
                {
                    var h = minutos / 60;
                    var m = minutos % 60;
                    return m > 0 ? $"{h}h{m:D2}" : $"{h}h";
                }
                return $"{minutos}min";
            }

            // Formato: HH:MM
            return $"{minutos / 60:D2}:{minutos % 60:D2}";
        }

        /// <summary>
        /// Formata tempo legível (wrapper para compatibilidade)
        /// </summary>
        public static string FormatarTempoLegivel(int minutos) => FormatarDuracao(minutos, formatoLegivel: true);

        public static int CalcularDuracaoMinutos(TimeSpan inicio, TimeSpan fim)
        {
            return (int)(fim - inicio).TotalMinutes;
        }

        public static bool ValidarOrdemCrescente(params TimeSpan[] horarios)
        {
            for (int i = 0; i < horarios.Length - 1; i++)
            {
                if (horarios[i] >= horarios[i + 1])
                    return false;
            }
            return true;
        }

        public static bool ValidarHorarioComercial(TimeSpan horario)
        {
            return horario >= TimeSpan.FromHours(6) && horario <= TimeSpan.FromHours(22);
        }

        public static bool ValidarLimiteDiario(int duracaoMinutos)
        {
            const int LIMITE_DIARIO = 600; // 10 horas
            return duracaoMinutos <= LIMITE_DIARIO;
        }

        public static bool IsHorarioNoturno(TimeSpan horario)
        {
            return horario >= TimeSpan.FromHours(22) || horario < TimeSpan.FromHours(5);
        }

        public static int CalcularInterjornada(TimeSpan fimJornada1, TimeSpan inicioJornada2)
        {
            int intervaloMinutos;
            
            if (inicioJornada2 >= fimJornada1)
            {
                intervaloMinutos = CalcularDuracaoMinutos(fimJornada1, inicioJornada2);
            }
            else
            {
                var minAteMeiaNoite = CalcularDuracaoMinutos(fimJornada1, TimeSpan.FromHours(24));
                var minDesdeMeiaNoite = CalcularDuracaoMinutos(TimeSpan.Zero, inicioJornada2);
                intervaloMinutos = minAteMeiaNoite + minDesdeMeiaNoite;
            }

            return intervaloMinutos;
        }

        public static bool ValidarInterjornada(int intervaloMinutos)
        {
            const int INTERJORNADA_MINIMA = 660; // 11 horas
            return intervaloMinutos >= INTERJORNADA_MINIMA;
        }

        public static string FormatarHorario(TimeSpan horario)
        {
            return $"{horario.Hours:D2}:{horario.Minutes:D2}";
        }

        public static TimeSpan ExtrairPrimeiroHorario(string horarios)
        {
            var lista = horarios.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (lista.Length > 0 && TryParseHorario(lista[0], out var primeiro))
                return primeiro;
            return TimeSpan.Zero;
        }

        public static TimeSpan ExtrairUltimoHorario(string horarios)
        {
            var lista = horarios.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (lista.Length > 0 && TryParseHorario(lista[lista.Length - 1], out var ultimo))
                return ultimo;
            return TimeSpan.Zero;
        }
    }
}
