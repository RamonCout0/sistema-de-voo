using System;
using System.ServiceModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoapCore;

namespace AtcServer
{
    // 1. O Contrato do Serviço (Interface)
    [ServiceContract(Namespace = "http://control.atc.soap")]
    public interface IAirTrafficService
    {
        [OperationContract]
        string SolicitarPouso(string voo, int combustivel);
    }

    // 2. A Implementação da Lógica da Torre
    public class AirTrafficService : IAirTrafficService
    {
        public string SolicitarPouso(string voo, int combustivel)
        {
            Console.WriteLine($"\n[LOG TORRE] Dados recebidos -> Voo: {voo} | Combustível: {combustivel}%");
            
            if (combustivel < 0)
            {
                // Simula erro estruturado (SOAP Fault)
                throw new FaultException("Dados de telemetria inválidos: combustível negativo.");
            }
            if (combustivel < 15) 
            {
                return "EMERGENCIA: Pista 02-Esquerda liberada. Vetores livres para aproximação imediata!";
            }
            
            return "AUTORIZADO: Pista 02-Direita. Vento 090 com 10 nós. Aguarde na frequência.";
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Clear();
            DesenharTorre();
            Console.WriteLine("=== SERVIDOR ATC (PROVEDOR) INICIADO ===");
            Console.WriteLine("WSDL disponível em: http://localhost:8000/Service.asmx?wsdl\n");

            var builder = WebApplication.CreateBuilder(args);

            // Adiciona a infraestrutura do SoapCore no contêiner de Injeção de Dependência
            builder.Services.AddSoapCore();
            builder.Services.AddSingleton<IAirTrafficService, AirTrafficService>();

            // Força a porta 8000 para facilitar
            builder.WebHost.UseUrls("http://localhost:8000");

            var app = builder.Build();

            // A ORDEM CORRETA: UseSoapEndpoint direto no 'app' ANTES do UseRouting
            app.UseSoapEndpoint<IAirTrafficService>("/Service.asmx", new SoapEncoderOptions());
            
            app.UseRouting();

            app.Run();
        }

        static void DesenharTorre()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"             |             ");
            Console.WriteLine(@"       ==============      ");
            Console.WriteLine(@"       |  [O]  [O]  |      ");
            Console.WriteLine(@"       \   ______   /      ");
            Console.WriteLine(@"        \_|______|_/       ");
            Console.WriteLine(@"          |      |         ");
            Console.WriteLine("===========================");
            Console.ResetColor();
        }
    }
}