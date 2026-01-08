using System;
using System.Linq;
using ValidadorJornada.Core.Models;
using ValidadorJornada.Core.Helpers;

namespace ValidadorJornada.Core.Services
{
    public class JornadaValidator
    {
        private readonly JornadaConfig _config;
        private readonly CodigoService _codigoService;

        public JornadaValidator(JornadaConfig config, CodigoService codigoService)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _codigoService = codigoService ?? throw new ArgumentNullException(nameof(codigoService));
        }

        public ValidationResult Validar(string horariosInput)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(horariosInput))
                    return CriarErro("Digite os horários");

                var horarios = horariosInput.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                return horarios.Length switch
                {
                    2 => ValidarJornadaSimples(horarios, horariosInput),
                    4 => ValidarJornadaComIntervalo(horarios, horariosInput),
                    _ => CriarErro($"Digite 2 ou 4 horários (você digitou {horarios.Length})")
                };
            }
            catch (Exception ex)
            {
                return CriarErro($"Erro na validação: {ex.Message}");
            }
        }

        public (ValidationResult jornada1, ValidationResult jornada2, string mensagemInterjornada) 
            ValidarComInterjornada(string horarios1, string horarios2, bool isModoSabado = false)
        {
            var resultado1 = Validar(horarios1);
            var resultado2 = Validar(horarios2);

            string mensagemInterjornada = string.Empty;
            
            try
            {
                var fimJ1 = TimeHelper.ExtrairUltimoHorario(horarios1);
                var inicioJ2 = TimeHelper.ExtrairPrimeiroHorario(horarios2);
                
                if (fimJ1 != TimeSpan.Zero && inicioJ2 != TimeSpan.Zero)
                {
                    var intervaloMinutos = TimeHelper.CalcularInterjornada(fimJ1, inicioJ2);
                    var horasMinimas = _config.InterjornadaMinimaMinutos / 60;
                    
                    if (intervaloMinutos >= _config.InterjornadaMinimaMinutos)
                    {
                        mensagemInterjornada = $"✅ Interjornada: {TimeHelper.FormatarDuracao(intervaloMinutos, formatoLegivel: true)}";
                    }
                    else
                    {
                        mensagemInterjornada = $"❌ Interjornada insuficiente: {TimeHelper.FormatarDuracao(intervaloMinutos, formatoLegivel: true)} (mínimo {horasMinimas}h)";
                    }
                }
            }
            catch
            {
            }

            if (!resultado1.Valido || !resultado2.Valido)
            {
                return (resultado1, resultado2, mensagemInterjornada);
            }

            if (isModoSabado)
            {
                return ValidarJornadaSabadoCompleta(resultado1, resultado2, horarios1, horarios2);
            }

            return ValidarInterjornadaNormal(resultado1, resultado2, horarios1, horarios2);
        }

        private ValidationResult ValidarJornadaSimples(string[] horarios, string horariosInput)
        {
            if (!TimeHelper.TryParseHorario(horarios[0], out var inicio) || 
                !TimeHelper.TryParseHorario(horarios[1], out var fim))
                return CriarErro("Formato inválido. Use HH:MM");

            if (inicio >= fim)
                return CriarErro("Horário inicial deve ser antes do final");

            var duracaoMin = TimeHelper.CalcularDuracaoMinutos(inicio, fim);
            
            if (!TimeHelper.ValidarLimiteDiario(duracaoMin))
                return CriarErro($"Duração ({TimeHelper.FormatarDuracao(duracaoMin)}) excede limite de 10h");

            var jornada = _config.Jornadas.FirstOrDefault(j => j.DuracaoMinutos == duracaoMin);

            if (jornada == null)
                return CriarErro($"Duração {TimeHelper.FormatarDuracao(duracaoMin)} não é válida");

            if (jornada.IntervaloMin > 0)
                return CriarErro($"Esta jornada requer intervalo. Digite 4 horários");

            return CriarSucesso(jornada, duracaoMin, null, horariosInput);
        }

        private ValidationResult ValidarJornadaComIntervalo(string[] horarios, string horariosInput)
        {
            if (!TimeHelper.TryParseHorario(horarios[0], out var h1Ini) || 
                !TimeHelper.TryParseHorario(horarios[1], out var h1Fim) ||
                !TimeHelper.TryParseHorario(horarios[2], out var h2Ini) || 
                !TimeHelper.TryParseHorario(horarios[3], out var h2Fim))
                return CriarErro("Formato inválido. Use HH:MM");

            if (!TimeHelper.ValidarOrdemCrescente(h1Ini, h1Fim, h2Ini, h2Fim))
                return CriarErro("Horários devem estar em ordem crescente");

            var duracao1 = TimeHelper.CalcularDuracaoMinutos(h1Ini, h1Fim);
            var intervalo = TimeHelper.CalcularDuracaoMinutos(h1Fim, h2Ini);
            var duracao2 = TimeHelper.CalcularDuracaoMinutos(h2Ini, h2Fim);
            var duracaoTotal = duracao1 + duracao2;

            var erros = new System.Collections.Generic.List<string>();

            if (duracao1 > _config.PeriodoMaximoSemIntervaloMinutos)
                erros.Add($"⚠️ Primeiro período ({TimeHelper.FormatarDuracao(duracao1)}) excede {TimeHelper.FormatarDuracao(_config.PeriodoMaximoSemIntervaloMinutos)}");

            if (duracao2 > _config.PeriodoMaximoSemIntervaloMinutos)
                erros.Add($"⚠️ Segundo período ({TimeHelper.FormatarDuracao(duracao2)}) excede {TimeHelper.FormatarDuracao(_config.PeriodoMaximoSemIntervaloMinutos)}");

            var periodoTotal = (duracaoTotal + intervalo) / 60.0;
            if (periodoTotal > _config.PeriodoMaximoHoras)
                erros.Add($"⚠️ Período total ({periodoTotal:F1}h) excede {_config.PeriodoMaximoHoras:F1}h");

            var jornada = _config.Jornadas.FirstOrDefault(j => j.DuracaoMinutos == duracaoTotal);
            if (jornada == null)
            {
                erros.Add($"⚠️ Duração {TimeHelper.FormatarDuracao(duracaoTotal)} não é válida");
            }

            if (jornada != null)
            {
                if (intervalo < jornada.IntervaloMin)
                    erros.Add($"⚠️ Intervalo insuficiente ({TimeHelper.FormatarDuracao(intervalo, formatoLegivel: true)}). Mínimo: {TimeHelper.FormatarDuracao(jornada.IntervaloMin, formatoLegivel: true)}");

                if (jornada.IntervaloMax > 0 && intervalo > jornada.IntervaloMax)
                    erros.Add($"⚠️ Intervalo excessivo ({TimeHelper.FormatarDuracao(intervalo, formatoLegivel: true)}). Máximo: {TimeHelper.FormatarDuracao(jornada.IntervaloMax, formatoLegivel: true)}");
            }

            if (erros.Count > 0)
                return CriarErro(string.Join("\n", erros));

            if (jornada == null)
                return CriarErro("Erro interno: jornada não encontrada");

            return CriarSucesso(jornada, duracaoTotal, intervalo, horariosInput);
        }

        private (ValidationResult, ValidationResult, string) ValidarInterjornadaNormal(
            ValidationResult resultado1, 
            ValidationResult resultado2,
            string horarios1,
            string horarios2)
        {
            var fimJ1 = TimeHelper.ExtrairUltimoHorario(horarios1);
            var inicioJ2 = TimeHelper.ExtrairPrimeiroHorario(horarios2);
            
            var intervaloMinutos = TimeHelper.CalcularInterjornada(fimJ1, inicioJ2);
            
            string mensagem;
            bool interjornadaValida = intervaloMinutos >= _config.InterjornadaMinimaMinutos;
            
            if (interjornadaValida)
            {
                mensagem = $"✅ Interjornada: {TimeHelper.FormatarDuracao(intervaloMinutos, formatoLegivel: true)}";
            }
            else
            {
                var horasMinimas = _config.InterjornadaMinimaMinutos / 60;
                mensagem = $"❌ Interjornada insuficiente: {TimeHelper.FormatarDuracao(intervaloMinutos, formatoLegivel: true)} (mínimo {horasMinimas}h)";
            }

            return (resultado1, resultado2, mensagem);
        }

        private (ValidationResult, ValidationResult, string) ValidarJornadaSabadoCompleta(
            ValidationResult jornadaPrincipal, 
            ValidationResult jornadaSabado,
            string horarios1,
            string horarios2)
        {
            if (jornadaPrincipal.DuracaoCalculada != "08:00")
            {
                var erro = CriarErro("Jornada principal deve ser 8h para modo sábado");
                return (jornadaPrincipal, erro, string.Empty);
            }

            if (jornadaSabado.DuracaoCalculada != "04:00")
            {
                var erro = CriarErro("Sábado deve ter exatamente 4 horas");
                return (jornadaPrincipal, erro, string.Empty);
            }

            var fimSexta = TimeHelper.ExtrairUltimoHorario(horarios1);
            var inicioSabado = TimeHelper.ExtrairPrimeiroHorario(horarios2);
            
            var intervaloMinutos = TimeHelper.CalcularInterjornada(fimSexta, inicioSabado);
            bool interjornadaValida = intervaloMinutos >= _config.InterjornadaMinimaMinutos;

            var horasSemanais = jornadaPrincipal.HorasSemanais + 4;
            var horasMensais = (int)(horasSemanais * 5);

            var resultadoSabadoAtualizado = new ValidationResult
            {
                Valido = interjornadaValida,
                Mensagem = interjornadaValida 
                    ? "✅ Jornada Sábado - 4h (Complemento 8h diária)"
                    : "❌ Jornada Sábado - Interjornada insuficiente",
                DuracaoCalculada = "04:00",
                TipoDia = "Sábado",
                Codigo = _codigoService.BuscarCodigo(horarios2),
                HorasSemanais = horasSemanais,
                HorasMensais = horasMensais,
                Intervalo = jornadaSabado.Intervalo
            };

            string mensagemInterjornada;
            var horasMinimas = _config.InterjornadaMinimaMinutos / 60;
            
            if (interjornadaValida)
            {
                mensagemInterjornada = $"✅Jornada Completa: {jornadaPrincipal.HorasSemanais}h (Seg-Sex) + 4h (Sáb) = {horasSemanais}h semanais\n" +
                                      $"✅ Interjornada Sexta à Sábado: {TimeHelper.FormatarDuracao(intervaloMinutos, formatoLegivel: true)}";
            }
            else
            {
                mensagemInterjornada = $"⚠️ Jornada: {jornadaPrincipal.HorasSemanais}h (Seg-Sex) + 4h (Sáb) = {horasSemanais}h semanais\n" +
                                      $"❌ Interjornada Sexta à Sábado insuficiente: {TimeHelper.FormatarDuracao(intervaloMinutos, formatoLegivel: true)} (mínimo {horasMinimas}h)";
            }

            return (jornadaPrincipal, resultadoSabadoAtualizado, mensagemInterjornada);
        }

        private ValidationResult CriarSucesso(Jornada jornada, int duracaoMin, int? intervalo, string horariosInput)
        {
            var codigo = _codigoService.BuscarCodigo(horariosInput);
            var tipoDia = DeterminarTipoDia(duracaoMin);

            var mensagem = $"✅ {jornada.Nome}" + (codigo != null ? $" (Código: {codigo})" : "");
            
            return new ValidationResult
            {
                Valido = true,
                Mensagem = mensagem,
                DuracaoCalculada = TimeHelper.FormatarDuracao(duracaoMin),
                TipoDia = tipoDia,
                Codigo = codigo,
                HorasSemanais = jornada.HorasSemanais,
                HorasMensais = jornada.HorasMensais,
                Intervalo = intervalo.HasValue ? TimeHelper.FormatarDuracao(intervalo.Value, formatoLegivel: true) : null
            };
        }

        private ValidationResult CriarErro(string mensagem)
        {
            if (!mensagem.StartsWith("⚠️") && !mensagem.StartsWith("❌") && !mensagem.StartsWith("âœ…"))
            {
                mensagem = $"⚠️ {mensagem}";
            }

            return new ValidationResult
            {
                Valido = false,
                Mensagem = mensagem,
                DuracaoCalculada = "00:00"
            };
        }

        private string DeterminarTipoDia(int duracaoMin)
        {
            return duracaoMin switch
            {
                240 => "Segunda a Sábado, ou apenas Sábado",
                350 => "Segunda a Sábado",
                440 => "Segunda a Sábado",
                480 => "Segunda a Sexta-feira",
                _ => "Não especificado"
            };
        }

        // ============================================
        // MÉTODO NOVO PARA VALIDAÇÃO EM LOTE
        // ============================================
        public ValidationResult ValidarHorariosArray(string[] horariosArray)
        {
            if (horariosArray == null || horariosArray.Length == 0)
                return CriarErro("Nenhum horário fornecido");
            
            var horariosLimpos = horariosArray
                .Where(h => !string.IsNullOrWhiteSpace(h) && h != "00:00")
                .ToArray();
            
            if (horariosLimpos.Length == 0)
                return CriarErro("Nenhum horário válido");
            
            var horariosInput = string.Join(" ", horariosLimpos);
            return Validar(horariosInput);
        }
    }
}
