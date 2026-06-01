using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SoapCore;

namespace AtcServer
{
    [ServiceContract(Namespace = "http://control.atc.soap")]
    public interface IAirTrafficService
    {
        [OperationContract]
        string CadastrarAeronave(string voo, string modelo, int combustivel);

        [OperationContract]
        string ObterMapaRadar();

        [OperationContract]
        string OrdenarPouso(string voo);
    }

    public class Avionics
    {
        public string Voo { get; set; }
        public string Modelo { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Combustivel { get; set; }
        public string Status { get; set; }
        public double Distancia { get; set; }
        public int CiclosVoo { get; set; } = 0; 
        public int TempoRetencao { get; set; } = 0;
    }

    public class AirTrafficService : IAirTrafficService
    {
        public static readonly ConcurrentDictionary<string, Avionics> Radar = new ConcurrentDictionary<string, Avionics>();
        public static readonly ConcurrentQueue<string> HistoricoLogs = new ConcurrentQueue<string>();
        
        public static int TotalPousosSeguros = 0;
        public static int TotalAcidentes = 0;
        public static int RadarFrame = 0;

        private const int TAM = 15;
        private const int AEROPORTO_X = 7;
        private const int AEROPORTO_Y = 7;

        public static void RegistrarLog(string mensagem)
        {
            HistoricoLogs.Enqueue($"[{DateTime.Now:HH:mm:ss}] {mensagem}");
            while (HistoricoLogs.Count > 5) HistoricoLogs.TryDequeue(out _);
        }

        public string CadastrarAeronave(string voo, string modelo, int combustivel)
        {
            var rand = new Random();
            int x = rand.Next(1, 2) == 1 ? rand.Next(1, 3) : rand.Next(12, 14);
            int y = rand.Next(0, 2) == 0 ? rand.Next(0, 3) : rand.Next(12, 14);

            var novo = new Avionics {
                Voo = voo.ToUpper(), Modelo = modelo,
                X = x, Y = y, Combustivel = combustivel,
                Status = "EM ROTA",
                Distancia = Math.Sqrt(Math.Pow(x - AEROPORTO_X, 2) + Math.Pow(y - AEROPORTO_Y, 2))
            };

            Radar[novo.Voo] = novo;
            RegistrarLog($"Aeronave {novo.Voo} ({modelo}) entrou no espaço aéreo com {combustivel}% de combustível.");
            return $"CONFIRMADO: {novo.Voo} rastreado no radar.";
        }

        public string OrdenarPouso(string voo)
        {
            string id = voo.ToUpper();
            if (Radar.TryGetValue(id, out var aero))
            {
                if (aero.Distancia > 3.0 && aero.Status != "MAYDAY 🚨")
                    throw new FaultException($"REJEITADO: {id} está a {aero.Distancia:F1} de distância. O limite é <= 3.0.");
                
                if (aero.Status == "QUEDA ❌")
                    throw new FaultException($"REJEITADO: {id} já foi perdido do radar.");

                aero.Status = "POUSANDO";
                RegistrarLog($"TORRE AUTORIZOU POUSO PARA {id}. Pista liberada.");
                return "AUTORIZADO: Pista liberada. Inicie descida.";
            }
            throw new FaultException($"ERRO: Voo {id} não encontrado.");
        }

        // CORRIGIDO: Removido o '-t alsa' e adicionado log de erro no catch
        public static void TocarSomLinux(string tipo)
        {
            try {
                if (OperatingSystem.IsLinux()) {
                    string args = tipo == "emergencia" ? "-nq synth 0.2 square 1400 vol 0.4" : "-nq synth 0.1 sine 800 vol 0.2";
                    Process.Start(new ProcessStartInfo { 
                        FileName = "play", 
                        Arguments = args, 
                        RedirectStandardOutput = true, 
                        RedirectStandardError = true, 
                        UseShellExecute = false 
                    });
                }
            } catch (Exception ex) { 
                Console.WriteLine($"[ERRO ÁUDIO BACKEND]: {ex.Message}"); 
            }
        }

        public static string GerarMapaString()
        {
            StringBuilder sb = new StringBuilder();
            string[,] grid = new string[TAM, TAM];

            for (int i = 0; i < TAM; i++)
                for (int j = 0; j < TAM; j++)
                    grid[i, j] = " . ";

            string[] icones = { " 🏛️ ", " 📡 ", " 📶 ", " 📡 " };
            grid[AEROPORTO_X, AEROPORTO_Y] = icones[RadarFrame % 4];

            bool temEmergencia = false;

            foreach (var aero in Radar.Values)
            {
                if (aero.Status != "QUEDA ❌" && aero.Status != "CONCLUIDO")
                {
                    grid[aero.X, aero.Y] = " ✈ ";
                    if (aero.X > 0) grid[aero.X - 1, aero.Y] = aero.Voo.Length >= 3 ? aero.Voo.Substring(0,3) : aero.Voo.PadRight(3);
                }
                if (aero.Status == "MAYDAY 🚨") temEmergencia = true;
            }

            if (temEmergencia) sb.AppendLine("⚠️ ⚠️  ALERTA CRÍTICO: AERONAVE EM MAYDAY (FALTA DE COMBUSTÍVEL) ⚠️ ⚠️");

            sb.AppendLine("=====================================================================");
            sb.AppendLine("||               RADAR MATRICIAL CARTESIANO CENTRAL (SOAP)         ||");
            sb.AppendLine("=====================================================================");
            for (int i = 0; i < TAM; i++)
            {
                sb.Append("||");
                for (int j = 0; j < TAM; j++) sb.Append(grid[i, j]);
                sb.AppendLine("||");
            }
            sb.AppendLine("=====================================================================");
            sb.AppendLine(string.Format("|| {0,-7} | {1,-10} | {2,-6} | {3,-5} | {4,-6} | {5,-12} ||", "VOO", "MODELO", "COORD", "DIST", "FUEL", "STATUS"));
            sb.AppendLine("---------------------------------------------------------------------");
            foreach (var aero in Radar.Values)
            {
                sb.AppendLine(string.Format("|| {0,-7} | {1,-10} | ({2,2},{3,2}) | {4,-5:F1} | {5,-6} | {6,-12} ||", 
                    aero.Voo, aero.Modelo.Length > 10 ? aero.Modelo.Substring(0,10) : aero.Modelo, 
                    aero.X, aero.Y, aero.Distancia, aero.Combustivel + "%", aero.Status));
            }
            sb.AppendLine("=====================================================================");
            sb.AppendLine("||                        LOG DE EVENTOS RECENTES                  ||");
            sb.AppendLine("---------------------------------------------------------------------");
            foreach (var log in HistoricoLogs) sb.AppendLine($" > {log}");
            sb.AppendLine("=====================================================================");
            sb.AppendLine($"|| ESTATÍSTICAS -> Ativos: {Radar.Count} | Pousos Seguros: {TotalPousosSeguros} | Quedas: {TotalAcidentes} ||");
            sb.AppendLine("=====================================================================");
            return sb.ToString();
        }

        public string ObterMapaRadar() => GerarMapaString();

        public static void IniciarSimuladorEspacial()
        {
            RegistrarLog("Sistema de Radar ATC Inicializado.");
            Task.Run(() => {
                while (true)
                {
                    Thread.Sleep(1500);
                    RadarFrame++;
                    bool isMayday = false;

                    foreach (var voo in Radar.Keys)
                    {
                        if (!Radar.TryGetValue(voo, out var aero)) continue;

                        if (aero.Status == "CONCLUIDO" || aero.Status == "QUEDA ❌")
                        {
                            aero.TempoRetencao++;
                            if (aero.TempoRetencao > 4) Radar.TryRemove(voo, out _);
                            continue;
                        }

                        if (aero.Status == "POUSANDO")
                        {
                            aero.X = AEROPORTO_X; aero.Y = AEROPORTO_Y; aero.Status = "CONCLUIDO";
                            TotalPousosSeguros++;
                            RegistrarLog($"SUCESSO: {voo} tocou o solo em segurança na pista principal.");
                            continue;
                        }

                        aero.CiclosVoo++;
                        if (aero.CiclosVoo % 5 == 0) aero.Combustivel = Math.Max(0, aero.Combustivel - 1);

                        if (aero.Combustivel <= 0)
                        {
                            aero.Status = "QUEDA ❌"; TotalAcidentes++;
                            RegistrarLog($"TRAGÉDIA: {voo} perdeu sustentação por falta de combustível e caiu.");
                            continue;
                        }

                        if (aero.Combustivel < 15 && aero.Status != "MAYDAY 🚨") 
                        {
                            aero.Status = "MAYDAY 🚨";
                            RegistrarLog($"EMERGÊNCIA DECLARADA: {voo} está com menos de 15% de combustível!");
                        }

                        if (aero.Status == "MAYDAY 🚨") isMayday = true;

                        var rand = new Random();
                        if (rand.Next(0, 2) == 0)
                        {
                            if (aero.X < AEROPORTO_X) aero.X++; else if (aero.X > AEROPORTO_X) aero.X--;
                        }
                        else
                        {
                            if (aero.Y < AEROPORTO_Y) aero.Y++; else if (aero.Y > AEROPORTO_Y) aero.Y--;
                        }

                        aero.Distancia = Math.Sqrt(Math.Pow(aero.X - AEROPORTO_X, 2) + Math.Pow(aero.Y - AEROPORTO_Y, 2));

                        if (aero.X == AEROPORTO_X && aero.Y == AEROPORTO_Y && aero.Status != "CONCLUIDO")
                        {
                            if (aero.Status != "MAYDAY 🚨") aero.Status = "ESPERA 🔄";
                            aero.X = AEROPORTO_X + rand.Next(-1, 2); aero.Y = AEROPORTO_Y + rand.Next(-1, 2);
                        }
                    }

                    RenderizarBackend(isMayday);
                }
            });
        }

        private static void RenderizarBackend(bool isMayday)
        {
            Console.Clear();
            string mapa = GerarMapaString();
            if (isMayday) {
                Console.BackgroundColor = ConsoleColor.DarkRed; Console.ForegroundColor = ConsoleColor.White;
                TocarSomLinux("emergencia");
            } else {
                TocarSomLinux("radar");
            }
            Console.WriteLine(mapa);
            Console.ResetColor();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Clear();
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSoapCore();
            builder.Services.AddSingleton<IAirTrafficService, AirTrafficService>();
            builder.WebHost.UseUrls("http://localhost:8000");
            var app = builder.Build();
            app.UseSoapEndpoint<IAirTrafficService>("/Service.asmx", new SoapEncoderOptions());
            app.UseRouting();
            AirTrafficService.IniciarSimuladorEspacial();
            app.Run();
        }
    }
}