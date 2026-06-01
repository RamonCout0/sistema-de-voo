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
   // 2. A Implementação da Lógica da Torre
    public class AirTrafficService : IAirTrafficService
    {
        public string SolicitarPouso(string voo, int combustivel)
        {
            if (combustivel < 0)
            {
                // Simula erro estruturado (SOAP Fault)
                throw new FaultException("Dados de telemetria inválidos: combustível negativo.");
            }

            if (combustivel < 15) 
            {
                // COLOQUE ISSO AQUI: Dispara o pânico na torre antes de responder ao cliente
                EfeitoSireneTorre(voo, combustivel);
                return "EMERGENCIA: Pista 02-Esquerda liberada. Vetores livres para aproximação imediata!";
            }
            
            if (combustivel < 40)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"\n[LOG TORRE] Voo {voo} em ESPERA (Combustível: {combustivel}%)");
                Console.ResetColor();
                return "ESPERA: Aguarde em órbita padrão a 15.000 pés.";
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[LOG TORRE] Voo {voo} APROVADO (Combustível: {combustivel}%)");
            Console.ResetColor();
            return "AUTORIZADO: Pista 02-Direita. Vento 090 com 10 nós. Aguarde na frequência.";
        }

        // ADICIONE ESTE MÉTODO NOVO AQUI DENTRO DA CLASSE:
       private void EfeitoSireneTorre(string voo, int combustivel)
        {
            for (int i = 0; i < 6; i++)
            {
                // Inverte as cores: Fundo Vermelho, Texto Branco
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.Clear();
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine("!!!                                                      !!!");
                Console.WriteLine("!!!             ALERTA DE EMERGÊNCIA GERAL               !!!");
                Console.WriteLine($"!!!      AERONAVE {voo} COM COMBUSTÍVEL CRÍTICO: {combustivel}%     !!!");
                Console.WriteLine("!!!                                                      !!!");
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                
                // CORREÇÃO PARA LINUX: Emite o som nativo do terminal
                Console.Write("\a"); 
                
                System.Threading.Thread.Sleep(150);
                
                // Inverte as cores: Fundo Preto, Texto Vermelho
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Clear();
                Console.WriteLine("------------------------------------------------------------");
                Console.WriteLine("---                                                      ---");
                Console.WriteLine("---             ALERTA DE EMBERGÊNCIA GERAL              ---");
                Console.WriteLine($"---      AERONAVE {voo} COM COMBUSTÍVEL CRÍTICO: {combustivel}%     ---");
                Console.WriteLine("---                                                      ---");
                Console.WriteLine("------------------------------------------------------------");
                
                // CORREÇÃO PARA LINUX: Segundo som nativo
                Console.Write("\a"); 
                
                System.Threading.Thread.Sleep(150);
            }
            Console.ResetColor();
            Console.Clear();
            Program.DesenharTorre(); // Redesenha a torre amarela após o pânico
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

        public static void DesenharTorre(){
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