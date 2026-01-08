using System.Linq;
using System.Text.RegularExpressions;

namespace ValidadorJornada.Core.Helpers
{
    public static class InputValidator
    {
        private static readonly Regex HorarioRegex = new Regex(@"^[\d\s:]+$", RegexOptions.Compiled);

        /// <summary>
        /// Valida se o caractere é permitido para entrada de horários
        /// </summary>
        public static bool IsCaractereValido(char c)
        {
            return char.IsDigit(c) || c == ':' || c == ' ';
        }

        /// <summary>
        /// Remove caracteres inválidos de uma string de horários
        /// </summary>
        public static string RemoverCaracteresInvalidos(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return new string(input.Where(c => IsCaractereValido(c)).ToArray());
        }

        /// <summary>
        /// Valida se a string contém apenas caracteres válidos
        /// </summary>
        public static bool ContemApenasCaracteresValidos(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            return HorarioRegex.IsMatch(input);
        }

        /// <summary>
        /// Valida se um horário individual está no formato correto
        /// </summary>
        public static bool ValidarFormatoHorario(string horario)
        {
            if (string.IsNullOrWhiteSpace(horario))
                return false;

            // Formato HH:MM
            var partes = horario.Split(':');
            if (partes.Length != 2)
                return false;

            if (partes[0].Length != 2 || partes[1].Length != 2)
                return false;

            if (!int.TryParse(partes[0], out var hora) || !int.TryParse(partes[1], out var minuto))
                return false;

            return hora >= 0 && hora <= 23 && minuto >= 0 && minuto <= 59;
        }

        /// <summary>
        /// Valida quantidade de horários (2 ou 4)
        /// </summary>
        public static (bool valido, string mensagem) ValidarQuantidadeHorarios(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return (false, "Digite os horários");

            var horarios = input.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            
            if (horarios.Length != 2 && horarios.Length != 4)
                return (false, $"Digite 2 ou 4 horários (você digitou {horarios.Length})");

            return (true, string.Empty);
        }
    }
}