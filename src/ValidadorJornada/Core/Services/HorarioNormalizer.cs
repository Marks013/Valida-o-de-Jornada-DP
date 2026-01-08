using System;
using System.Linq;

namespace ValidadorJornada.Core.Helpers
{
    /// <summary>
    /// Centralizador de toda normalização de horários
    /// </summary>
    public static class HorarioNormalizer
    {
        /// <summary>
        /// Normaliza string de horários para formato padrão
        /// </summary>
        public static string Normalizar(string horarios)
        {
            if (string.IsNullOrWhiteSpace(horarios))
                return string.Empty;

            return string.Join(" ", 
                horarios.Trim()
                    .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => h.Trim())
            );
        }

        /// <summary>
        /// Normaliza e valida formato
        /// </summary>
        public static (bool valido, string normalizado) NormalizarComValidacao(string horarios)
        {
            var normalizado = Normalizar(horarios);
            
            if (string.IsNullOrEmpty(normalizado))
                return (false, normalizado);

            var partes = normalizado.Split(' ');
            
            foreach (var parte in partes)
            {
                if (!InputValidator.ValidarFormatoHorario(parte))
                    return (false, normalizado);
            }

            return (true, normalizado);
        }

        /// <summary>
        /// Compara horários ignorando formatação
        /// </summary>
        public static bool SaoIguais(string horarios1, string horarios2)
        {
            return Normalizar(horarios1) == Normalizar(horarios2);
        }
    }
}
