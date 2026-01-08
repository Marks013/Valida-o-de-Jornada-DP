using System;
using System.Collections.Generic;
using System.Linq;

namespace ValidadorJornada.Core.Helpers
{
    public static class HorarioFormatter
    {
        /// <summary>
        /// Formata string de horários de forma inteligente
        /// </summary>
        public static string FormatarHorarios(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var partes = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var formatados = new List<string>();

            foreach (var parte in partes)
            {
                var formatado = FormatarHorarioIndividual(parte);
                if (!string.IsNullOrEmpty(formatado))
                    formatados.Add(formatado);
            }

            return string.Join(" ", formatados);
        }

        /// <summary>
        /// Formata um horário individual
        /// </summary>
        private static string FormatarHorarioIndividual(string horario)
        {
            if (string.IsNullOrWhiteSpace(horario))
                return string.Empty;

            if (horario.Contains(":") && InputValidator.ValidarFormatoHorario(horario))
                return horario;

            var apenasDigitos = new string(horario.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(apenasDigitos))
                return string.Empty;

            return apenasDigitos.Length switch
            {
                1 => $"0{apenasDigitos}:00",
                2 => $"{apenasDigitos}:00",
                3 => $"0{apenasDigitos[0]}:{apenasDigitos.Substring(1)}",
                4 => FormatarQuatroDigitos(apenasDigitos),
                _ => string.Empty // ✅ CORRIGIDO: Retorna vazio ao invés de truncar
            };
        }

        /// <summary>
        /// Formata exatamente 4 dígitos como HH:MM
        /// </summary>
        private static string FormatarQuatroDigitos(string digitos)
        {
            var horas = digitos.Substring(0, 2);
            var minutos = digitos.Substring(2);

            if (int.TryParse(horas, out var h) && int.TryParse(minutos, out var m))
            {
                if (h >= 0 && h <= 23 && m >= 0 && m <= 59)
                    return $"{horas}:{minutos}";
            }

            return string.Empty;
        }

        /// <summary>
        /// Valida se a string precisa de formatação
        /// </summary>
        public static bool PrecisaFormatar(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var partes = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var parte in partes)
            {
                if (!InputValidator.ValidarFormatoHorario(parte))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Remove caracteres inválidos durante a digitação
        /// </summary>
        public static string FormatarDuranteDigitacao(string input, int cursorPosition)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return InputValidator.RemoverCaracteresInvalidos(input);
        }
    }
}
