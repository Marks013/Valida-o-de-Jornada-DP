using System;
using System.IO;
using System.Text.Json;
using ValidadorJornada.Core.Models;
using NJsonSchema;

namespace ValidadorJornada.Core.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        private JornadaConfig? _cachedConfig;
        private readonly object _lockObject = new object();

        public ConfigService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(baseDir, "config.json");
            
            if (!File.Exists(_configPath))
            {
                _configPath = Path.Combine(baseDir, "Resources", "config.json");
            }
        }

        public JornadaConfig LoadConfig()
        {
            lock (_lockObject)
            {
                if (_cachedConfig != null)
                    return _cachedConfig;

                try
                {
                    if (!File.Exists(_configPath))
                    {
                        throw new FileNotFoundException(
                            $"Arquivo de configuração não encontrado: {_configPath}"
                        );
                    }

                    var json = File.ReadAllText(_configPath);
                    
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidOperationException(
                            "Arquivo de configuração está vazio."
                        );
                    }

                    ValidateJsonSchema(json);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    };

                    _cachedConfig = JsonSerializer.Deserialize<JornadaConfig>(json, options);

                    if (_cachedConfig == null)
                    {
                        throw new InvalidOperationException(
                            "Não foi possível carregar a configuração."
                        );
                    }

                    ValidarConfiguracao(_cachedConfig);

                    return _cachedConfig;
                }
                catch (Exception ex) when (ex is not FileNotFoundException && ex is not InvalidOperationException)
                {
                    throw new InvalidOperationException(
                        $"Erro ao carregar configuração:\n{ex.Message}",
                        ex
                    );
                }
            }
        }

        private bool ValidateJsonSchema(string json)
        {
            try
            {
                var schema = JsonSchema.FromType<JornadaConfig>();
                var errors = schema.Validate(json);
                
                if (errors != null && errors.Count > 0)
                {
                    var errorMessages = string.Join("\n", errors);
                    System.Diagnostics.Debug.WriteLine($"Schema validation errors: {errorMessages}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Schema validation exception: {ex.Message}");
                return true;
            }
        }

        private void ValidarConfiguracao(JornadaConfig config)
        {
            if (config.Jornadas == null || config.Jornadas.Count == 0)
            {
                throw new InvalidOperationException(
                    "Configuração inválida: Nenhuma jornada foi definida."
                );
            }

            if (config.PeriodoMaximoHoras <= 0)
            {
                throw new InvalidOperationException(
                    "Configuração inválida: PeriodoMaximoHoras deve ser maior que zero."
                );
            }

            if (config.InterjornadaMinimaMinutos <= 0)
            {
                config.InterjornadaMinimaMinutos = 660;
            }

            if (config.PeriodoMaximoSemIntervaloMinutos <= 0)
            {
                config.PeriodoMaximoSemIntervaloMinutos = 240;
            }

            if (config.CacheHistoricoMinutos <= 0)
            {
                config.CacheHistoricoMinutos = 30;
            }

            foreach (var jornada in config.Jornadas)
            {
                // CORREÇÃO: Valida nome nulo/vazio/whitespace
                if (string.IsNullOrWhiteSpace(jornada.Nome))
                {
                    throw new InvalidOperationException(
                        "Configuração inválida: Todas as jornadas devem ter um nome válido."
                    );
                }

                if (jornada.DuracaoMinutos <= 0)
                {
                    throw new InvalidOperationException(
                        $"Configuração inválida: Jornada '{jornada.Nome}' tem duração inválida."
                    );
                }

                if (jornada.IntervaloMin < 0 || jornada.IntervaloMax < 0)
                {
                    throw new InvalidOperationException(
                        $"Configuração inválida: Jornada '{jornada.Nome}' tem intervalos negativos."
                    );
                }

                if (jornada.IntervaloMax > 0 && jornada.IntervaloMin > jornada.IntervaloMax)
                {
                    throw new InvalidOperationException(
                        $"Configuração inválida: Jornada '{jornada.Nome}' - intervalo mínimo é maior que o máximo."
                    );
                }
            }
        }

        public void LimparCache()
        {
            lock (_lockObject)
            {
                _cachedConfig = null;
            }
        }

        public void SaveConfig(JornadaConfig config)
        {
            lock (_lockObject)
            {
                try
                {
                    ValidarConfiguracao(config);

                    var options = new JsonSerializerOptions 
                    { 
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var json = JsonSerializer.Serialize(config, options);
                    File.WriteAllText(_configPath, json);
                    
                    _cachedConfig = config;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Erro ao salvar configuração:\n{ex.Message}",
                        ex
                    );
                }
            }
        }
    }
}
