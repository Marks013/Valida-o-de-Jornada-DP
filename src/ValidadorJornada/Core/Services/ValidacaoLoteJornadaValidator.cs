using System;
using System.Linq;
using ValidadorJornada.Core.Models;
using ValidadorJornada.Core.Helpers;

namespace ValidadorJornada.Core.Services
{
    public class ValidacaoLoteJornadaValidator
    {
        private readonly JornadaConfig _config;
        private readonly CodigoService _codigoService;
        private ValidacaoLoteConfig _validacaoConfig;

        public ValidacaoLoteJornadaValidator(JornadaConfig config, CodigoService codigoService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _codigoService = codigoService ?? throw new ArgumentNullException(nameof(codigoService));
            
            _validacaoConfig = new ValidacaoLoteConfig
            {
                ValidarPeriodos = true,
                ValidarJornada = true,
                ValidarIntervalos = true,
                UsarHorariosAgrupados = false
            };
        }

        public void ConfigurarValidacao(ValidacaoLoteConfig validacaoConfig)
        {
            _validacaoConfig = validacaoConfig ?? throw new ArgumentNullException(nameof(validacaoConfig));
        }

        public ValidationResult ValidarHorariosArray(string[] horariosArray)
        {
            if (horariosArray == null || horariosArray.Length == 0)
                return CriarErro("Nenhum horário fornecido");
            
            var horariosLimpos = horariosArray
                .Where(h => !string.IsNullOrWhiteSpace(h) && h != "00:00")
                .ToArray();
            
            if (horariosLimpos.Length == 0)
                return CriarErro("Nenhum horário válido");
            
            return horariosLimpos.Length switch
            {
                2 => ValidarJornadaSimples(horariosLimpos),
                4 => ValidarJornadaComIntervalo(horariosLimpos),
                _ => CriarErro($"Quantidade inválida de horários: {horariosLimpos.Length}")
            };
        }

        private ValidationResult ValidarJornadaSimples(string[] horarios)
        {
            if (!TimeHelper.TryParseHorario(horarios[0], out var inicio) || 
                !TimeHelper.TryParseHorario(horarios[1], out var fim))
                return CriarErro("Formato inválido");

            if (inicio >= fim)
                return CriarErro("Horário inicial ≥ final");

            var duracaoMin = TimeHelper.CalcularDuracaoMinutos(inicio, fim);
            
            if (_validacaoConfig.ValidarJornada)
            {
                if (!TimeHelper.ValidarLimiteDiario(duracaoMin))
                    return CriarErro($"Duração excede 10h: {TimeHelper.FormatarDuracao(duracaoMin)}");

                // âœ… CORRIGIDO: Busca jornada EXATA (sem tolerância)
                var jornada = _config.Jornadas.FirstOrDefault(j => j.DuracaoMinutos == duracaoMin);

                if (jornada == null)
                    return CriarErro($"Duração não válida: {TimeHelper.FormatarDuracao(duracaoMin)}");

                if (jornada.IntervaloMin > 0)
                    return CriarErro("Jornada requer intervalo (4 horários)");

                return CriarSucesso(jornada, duracaoMin, null, string.Join(" ", horarios));
            }

            // âœ… CORRIGIDO: Se não validar jornada, busca EXATA (sem tolerância)
            var jornadaInfo = _config.Jornadas.FirstOrDefault(j => j.DuracaoMinutos == duracaoMin);
            return CriarSucesso(jornadaInfo, duracaoMin, null, string.Join(" ", horarios));
        }

        private ValidationResult ValidarJornadaComIntervalo(string[] horarios)
        {
            if (!TimeHelper.TryParseHorario(horarios[0], out var h1Ini) || 
                !TimeHelper.TryParseHorario(horarios[1], out var h1Fim) ||
                !TimeHelper.TryParseHorario(horarios[2], out var h2Ini) || 
                !TimeHelper.TryParseHorario(horarios[3], out var h2Fim))
                return CriarErro("Formato inválido");

            if (!TimeHelper.ValidarOrdemCrescente(h1Ini, h1Fim, h2Ini, h2Fim))
                return CriarErro("Horários fora de ordem");

            var duracao1 = TimeHelper.CalcularDuracaoMinutos(h1Ini, h1Fim);
            var intervalo = TimeHelper.CalcularDuracaoMinutos(h1Fim, h2Ini);
            var duracao2 = TimeHelper.CalcularDuracaoMinutos(h2Ini, h2Fim);
            var duracaoTotal = duracao1 + duracao2;

            var erros = new System.Collections.Generic.List<string>();

            if (_validacaoConfig.ValidarPeriodos)
            {
                if (duracao1 > _config.PeriodoMaximoSemIntervaloMinutos)
                    erros.Add($"1º período > {TimeHelper.FormatarDuracao(_config.PeriodoMaximoSemIntervaloMinutos)}: {TimeHelper.FormatarDuracao(duracao1)}");

                if (duracao2 > _config.PeriodoMaximoSemIntervaloMinutos)
                    erros.Add($"2º período > {TimeHelper.FormatarDuracao(_config.PeriodoMaximoSemIntervaloMinutos)}: {TimeHelper.FormatarDuracao(duracao2)}");

                var periodoTotal = (duracaoTotal + intervalo) / 60.0;
                if (periodoTotal > _config.PeriodoMaximoHoras)
                    erros.Add($"Período total > {_config.PeriodoMaximoHoras:F1}h: {periodoTotal:F1}h");
            }

            // Busca jornada para referência (sempre, independente de ValidarJornada)
            var jornada = _config.Jornadas.FirstOrDefault(j => j.DuracaoMinutos == duracaoTotal);
            
            // Valida se jornada é obrigatória
            if (_validacaoConfig.ValidarJornada && jornada == null)
            {
                erros.Add($"Duração não válida: {TimeHelper.FormatarDuracao(duracaoTotal)}");
            }

            // Valida intervalos (apenas se jornada foi encontrada)
            if (_validacaoConfig.ValidarIntervalos && jornada != null)
            {
                if (intervalo < jornada.IntervaloMin)
                    erros.Add($"Intervalo < mínimo: {TimeHelper.FormatarDuracao(intervalo)} (mín: {TimeHelper.FormatarDuracao(jornada.IntervaloMin)})");

                if (jornada.IntervaloMax > 0 && intervalo > jornada.IntervaloMax)
                    erros.Add($"Intervalo > máximo: {TimeHelper.FormatarDuracao(intervalo)} (máx: {TimeHelper.FormatarDuracao(jornada.IntervaloMax)})");
            }

            if (erros.Count > 0)
                return CriarErro(string.Join(" | ", erros));

            return CriarSucesso(jornada, duracaoTotal, intervalo, string.Join(" ", horarios));
        }

        private ValidationResult CriarSucesso(Jornada? jornada, int duracaoMin, int? intervalo, string horariosInput)
        {
            var codigo = _codigoService.BuscarCodigo(horariosInput);
            var tipoDia = DeterminarTipoDia(duracaoMin);

            var mensagem = jornada != null 
                ? $"✅ {jornada.Nome}" + (codigo != null ? $" (Código: {codigo})" : "")
                : $"✅ Duração: {TimeHelper.FormatarDuracao(duracaoMin)}" + (codigo != null ? $" (Código: {codigo})" : "");
            
            return new ValidationResult
            {
                Valido = true,
                Mensagem = mensagem,
                DuracaoCalculada = TimeHelper.FormatarDuracao(duracaoMin),
                TipoDia = tipoDia,
                Codigo = codigo,
                HorasSemanais = jornada?.HorasSemanais ?? 0,
                HorasMensais = jornada?.HorasMensais ?? 0,
                Intervalo = intervalo.HasValue ? TimeHelper.FormatarDuracao(intervalo.Value, formatoLegivel: true) : null
            };
        }

        private ValidationResult CriarErro(string mensagem)
        {
            return new ValidationResult
            {
                Valido = false,
                Mensagem = $"❌ {mensagem}",
                DuracaoCalculada = "00:00"
            };
        }

        private string DeterminarTipoDia(int duracaoMin)
        {
            return duracaoMin switch
            {
                240 => "Segunda a Sábado",
                350 => "Segunda a Sábado",
                440 => "Segunda a Sábado",
                480 => "Segunda a Sexta",
                _ => "Não especificado"
            };
        }
    }
}