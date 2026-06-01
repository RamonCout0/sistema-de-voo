import time
import os
import sys
from zeep import Client

def limpar_tela():
    os.system('cls' if os.name == 'nt' else 'clear')

def emitir_som(frequencia, duracao):
    if os.name == 'nt':
        import winsound
        winsound.Beep(frequencia, duracao)
    else:
        sys.stdout.write('\a')
        sys.stdout.flush()

def simular_carregamento(mensagem):
    print(f"{mensagem}")
    print("[", end="", flush=True)
    for _ in range(20):
        print("█", end="", flush=True)
        time.sleep(0.03)
    print("] OK!")

def animar_radar_espera(voo_id, voltas=3):
    frames = [
        "   ✈ [ | ] Reduzindo velocidade... Mantendo FL150 ",
        "   ✈ [ / ] Curva padrão à direita... 180°C executados ",
        "   ✈ [ — ] Alinhando no ponto de espera fixo ",
        "   ✈ [ \\ ] Entrando na perna de afastamento do circuito "
    ]
    print(f"\n📡 [SISTEMA] Iniciando procedimento de espera para {voo_id}:")
    for v in range(voltas):
        for frame in frames:
            limpar_tela()
            print("==================================================")
            print(f"         PROCEDIMENTO DE ÓRBITA: VOLTA {v+1}/{voltas}       ")
            print("==================================================")
            print(frame)
            emitir_som(900, 80)
            time.sleep(0.5)

def desenhar_aviao():
    print("                ______")
    print("                \\_____\\________")
    print("  ===✈            /     /        \\")
    print("                 /____ /__________\\")
    print("                      \\__________/")

def executar_sistema():
    clear_count = 0
    limpar_tela()
    desenhar_aviao()
    print("\n==============================================")
    print("     INICIALIZANDO COMPUTADOR DE BORDO (SOAP)  ")
    print("==============================================")
    
    # Dados locais do avião (Estado em memória no cliente)
    aeronave = {
        "id": "GLO4060",
        "modelo": "Boeing 737-800",
        "altitude": 35000,
        "status": "CRUZEIRO"
    }
    
    wsdl_url = "http://localhost:8000/Service.asmx?wsdl"
    simular_carregamento("Sincronizando barramento XML com a Torre...")
    
    try:
        client = Client(wsdl_url)
        emitir_som(800, 100)
        emitir_som(1200, 150)
        print(f"\n[OK] Telemetria vinculada à Torre C#.")
        time.sleep(1)
    except Exception as e:
        emitir_som(400, 500)
        print(f"\n[ERRO] Falha de conexão: {e}")
        return

    while True:
        limpar_tela()
        desenhar_aviao()
        print("\n==============================================")
        print(f"  AERONAVE: {aeronave['id']} ({aeronave['modelo']})")
        print(f"  ALTITUDE: {aeronave['altitude']} pés | STATUS: {aeronave['status']}")
        print("==============================================")
        print(" [1] Modificar Configurações da Aeronave (ID/Modelo)")
        print(" [2] Solicitar Autorização de Pouso à Torre")
        print(" [3] Testar Telemetria Corrompida (SOAP Fault)")
        print(" [4] Desconectar Transmissor e Sair")
        print("==============================================")
        
        opcao = input("Selecione o comando: ")

        if opcao == "1":
            limpar_tela()
            print("=== CONFIGURAÇÕES DOS INSTRUMENTOS ===")
            aeronave["id"] = input(f"Novo ID do Voo [{aeronave['id']}]: ").upper() or     aeronave["id"]
            aeronave["modelo"] = input(f"Modelo da Aeronave [{aeronave['modelo']}]: ") or     aeronave["modelo"]
            print("\n[OK] Identificadores atualizados no transponder local.")
            emitir_som(1000, 100)
            time.sleep(1.5)

        elif opcao == "2":
            limpar_tela()
            print(f"=== TRANSMISSÃO SOAP PARA A TORRE: {aeronave['id']} ===")
            try:
                combustivel = int(input("Informe o nível de combustível atual (%): "))
            except ValueError:
                emitir_som(400, 300)
                print("Valor inválido.")
                time.sleep(1.5)
                continue

            simular_carregamento("\nEmpacotando parâmetros no Body do Envelope SOAP...")
            
            # Envia os dados atuais salvos na memória do cliente
            resposta = client.service.SolicitarPouso(aeronave["id"], combustivel)
            
            print("\n----------------------------------------------")
            print(f"📡 RESPOSTA DA TORRE: {resposta}")
            print("----------------------------------------------")

            # Tomada de decisão dinâmica do cliente dependendo do que o servidor respondeu
            if "EMERGENCIA" in resposta:
                for _ in range(3): 
                    emitir_som(1500, 120); emitir_som(1100, 120)
                aeronave["status"] = "EM EMERGÊNCIA"
                confirmar = input("\n[⚠️ ] Iniciar descida imediata para pista de emergência? (S/N): ").upper()
                if confirmar == "S":
                    aeronave["altitude"] = 0
                    aeronave["status"] = "POUSADO"
                    print("\n[INFO] Trem de pouso baixado. Aeronave em solo com segurança.")
                    emitir_som(1200, 400)
                else:
                    print("\n[AVISO] Piloto optou por arremeter por conta própria!")

            elif "ESPERA" in resposta:
                emitir_som(1000, 300)
                aeronave["status"] = "EM ESPERA"
                confirmar = input("\nDeseja aceitar a instrução e entrar em órbita de espera? (S/N): ").upper()
                if confirmar == "S":
                    # Aciona a animação gráfica de rotação do avião no radar
                    animar_radar_espera(aeronave["id"], voltas=2)
                    aeronave["altitude"] = 15000
                    print("\n[INFO] Aeronave estabilizada no circuito de espera padrão.")
                else:
                    print("\n[ALERTA] Aeronave ignorou a torre e mantém rota de colisão!")
                    emitir_som(400, 600)

            else:
                emitir_som(1000, 150)
                aeronave["status"] = "APROXIMAÇÃO"
                confirmar = input("\nConfirmar recebimento e iniciar descida padrão? (S/N): ").upper()
                if confirmar == "S":
                    aeronave["altitude"] = 3000
                    aeronave["status"] = "FINAL"
                    print("\n[INFO] Alinhado com a cabeceira da pista. Procedimento normal.")

            input("\nPressione Enter para continuar...")

        elif opcao == "3":
            limpar_tela()
            print("=== SIMULADOR DE FALHA CRÍTICA ===")
            print(f"Enviando dados ilegíveis (-99% de combustível) para {aeronave['id']}...")
            time.sleep(1)
            try:
                client.service.SolicitarPouso(aeronave["id"], -99)
            except Exception as fault:
                emitir_som(400, 500)
                print("\n❌ [SOAP FAULT CAPTURADO]")
                print(f"Contrato violado! Servidor C# barrou a requisição.")
                print(f"Detalhe retornado no XML: {fault}")
            input("\nPressione Enter para continuar...")

        elif opcao == "4":
            print("\nFinalizando barramento de dados...")
            break

if __name__ == "__main__":
    executar_sistema()