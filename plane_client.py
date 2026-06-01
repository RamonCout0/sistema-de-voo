import time
import os
import sys
from zeep import Client

def limpar_tela():
    os.system('cls' if os.name == 'nt' else 'clear')

# Sons pelo SoX (Certifique-se de ter rodado: sudo apt install sox)
def emitir_som(tipo="radar"):
    if os.name == 'nt':
        import winsound
        if tipo == "sucesso": winsound.Beep(1000, 100); winsound.Beep(1200, 150)
        elif tipo == "erro": winsound.Beep(300, 400)
    else:
        try:
            if tipo == "emergencia": os.system("play -nq -t alsa synth 0.2 square 1400 vol 0.4 2>/dev/null")
            elif tipo == "erro": os.system("play -nq -t alsa synth 0.3 sawtooth 250 vol 0.6 2>/dev/null")
            elif tipo == "sucesso": os.system("play -nq -t alsa synth 0.1 sine 800 : synth 0.15 sine 1200 vol 0.5 2>/dev/null")
        except: pass

def animacao_pouso(voo_id):
    limpar_tela()
    frames = [
        "   ✈  [===           ] Flaps em 15°...",
        "   ✈  [======        ] Entrando no glideslope...",
        "   ✈  [=========     ] Trem de pouso travado...",
        "   🏁 [██████████████] POUSO CONCLUÍDO COM SUCESSO!"
    ]
    for i, frame in enumerate(frames):
        limpar_tela()
        print(f"=== PROTOCOLO DE DESCIDA FINAL: {voo_id} ===")
        print(frame)
        if i == len(frames) - 1: emitir_som("sucesso")
        time.sleep(0.7)
    time.sleep(1.5)

def executar_painel():
    limpar_tela()
    print("Sincronizando console com o barramento SOAP...")
    try:
        client = Client("http://localhost:8000/Service.asmx?wsdl")
        print("[OK] Conexão ativa.")
        emitir_som("sucesso")
        time.sleep(1)
    except Exception as e:
        print(f"[ERRO] Torre offline: {e}")
        return

    while True:
        limpar_tela()
        print("\033[0m") 
        print("==============================================")
        print("    CONSOLE CENTRAL DE COMANDO ATC (PYTHON)   ")
        print("==============================================")
        print(" [1] Registrar Novo Voo (Com Combustível Manual)")
        print(" [2] Enviar Ordem de Pouso (Max 3.0 passos)")
        print(" [3] INICIAR MONITORAMENTO DE RADAR AO VIVO")
        print(" [4] Fechar Console")
        print("==============================================")
        opcao = input("Selecione uma ação > ")

        if opcao == "1":
            limpar_tela()
            print("=== REGISTRO DE TRANSPONDER ===")
            voo = input("Código do Voo (ex: TAM40): ").upper()
            modelo = input("Modelo do Avião: ")
            
            try:
                comb_input = input("Combustível Inicial (1 a 100%): ")
                combustivel = int(comb_input) if comb_input else 100
            except ValueError:
                emitir_som("erro")
                continue
                
            if not voo: continue
            
            resposta = client.service.CadastrarAeronave(voo=voo, modelo=modelo, combustivel=combustivel)
            print(f"\n📡 Resposta SOAP: {resposta}")
            time.sleep(1.5)

        elif opcao == "2":
            limpar_tela()
            print("=== TRANSMISSÃO DE VETOR DE POUSO ===")
            voo = input("Informe o ID do Voo para pousar: ").upper()
            if not voo: continue

            try:
                resposta = client.service.OrdenarPouso(voo=voo)
                if "AUTORIZADO" in resposta: animacao_pouso(voo)
            except Exception as fault:
                emitir_som("erro")
                print(f"\n\033[31m❌ REJEITADO PELO SERVIDOR (SOAP FAULT):\n{fault}\033[0m")
                input("\nPressione Enter para continuar...")

        elif opcao == "3":
            try:
                while True:
                    limpar_tela()
                    mapa_atual = client.service.ObterMapaRadar()
                    
                    if "MAYDAY" in mapa_atual:
                        print("\033[41m\033[97m") # Fundo vermelho e texto branco
                        print(mapa_atual)
                        print("\033[0m") 
                        emitir_som("emergencia") 
                    else:
                        print(mapa_atual)
                    
                    print("\n[MODO AO VIVO] Atualizando via XML SOAP... (Pressione Ctrl+C para voltar)")
                    time.sleep(1.5) 
            except KeyboardInterrupt:
                time.sleep(1)
                continue

        elif opcao == "4":
            print("\nDesconectando sistema...")
            break

if __name__ == "__main__":
    executar_painel()