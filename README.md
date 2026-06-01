# Sistema de Controle de Tráfego Aéreo (ATC)

![ATC](./ATC.png)

## Visão geral

Este projeto é um simulador de torre de controle de tráfego aéreo com servidor SOAP em .NET e um cliente de comando em Python.

- `AtcServer/` contém um servidor web ASP.NET Core que expõe um serviço SOAP com três operações principais.
- `plane_client.py` é um cliente Python que consome o serviço SOAP para registrar voos, ordenar pousos e visualizar um radar em tempo real.

## Por que este projeto foi criado?

O objetivo é demonstrar como sistemas legados podem usar SOAP para comunicação entre serviços e clientes. O servidor simula um radar ATC que rastreia aeronaves, atualiza posições e controla pousos. O cliente Python demonstra como um console de operações pode interagir com esse servidor SOAP de forma simples.

## Como o método SOAP funciona neste projeto

O servidor SOAP é implementado em `AtcServer/Program.cs` com a biblioteca `SoapCore`.

### Serviço exposto

A interface `IAirTrafficService` define três operações SOAP:

1. `CadastrarAeronave(string voo, string modelo, int combustivel)`
   - Registra uma aeronave no radar e retorna uma confirmação.
2. `ObterMapaRadar()`
   - Retorna uma representação textual do mapa radar, incluindo posição, status e logs recentes.
3. `OrdenarPouso(string voo)`
   - Autoriza ou rejeita o pouso de um voo, com regras de distância e emergências.

### Como SOAP é usado

- O servidor publica um endpoint em `http://localhost:8000/Service.asmx`.
- Quando o cliente acessa `http://localhost:8000/Service.asmx?wsdl`, ele obtém o WSDL (descrição do serviço SOAP).
- O cliente Python usa essa descrição para montar chamadas XML SOAP automaticamente.
- Cada operação SOAP é convertida em um pedido XML que é enviado ao servidor.
- O servidor processa o pedido, executa a lógica e retorna uma resposta SOAP.

Esse fluxo é típico de integrações SOAP: contrato do serviço → WSDL → mensagens XML padronizadas.

## Funcionalidades principais

- Registro de aeronaves no radar com posição aleatória e nível de combustível.
- Simulação de movimento e redução gradual de combustível.
- Monitoramento em tempo real do radar via `ObterMapaRadar()`.
- Envio de ordem de pouso com validação de distância.
- Logs de eventos e indicadores de emergência quando o combustível está baixo.

## Pré-requisitos

### Para o servidor .NET

- .NET 10 SDK instalado
- Acesso ao terminal

### Para o cliente Python

- Python 3 instalado
- Biblioteca `zeep`
- Opcional: `sox` para sons no Linux (`play`)

## Instalação e execução

### 1. Executar o servidor SOAP

No terminal, dentro da pasta do projeto:

```bash
cd /home/ramon/Documentos/sistema-de-voo/AtcServer
dotnet restore
dotnet run
```

O servidor ficará disponível em:

```
http://localhost:8000/Service.asmx
```

### 2. Executar o cliente Python

Abra outro terminal e instale as dependências:

```bash
cd /home/ramon/Documentos/sistema-de-voo
python3 -m pip install zeep
```

Se quiser efeitos sonoros no Linux, instale o `sox`:

```bash
sudo apt install sox
```

Execute o cliente:

```bash
python3 plane_client.py
```

### 3. Usar o painel Python

O cliente oferece um menu:

- Registrar novo voo
- Enviar ordem de pouso
- Iniciar monitoramento de radar ao vivo
- Fechar console

## Observações

- O cliente espera que o servidor esteja rodando em `http://localhost:8000/Service.asmx?wsdl`.
- O servidor usa `SoapCore` para tornar a interface C# disponível como serviço SOAP.
- O radar é produzido em texto ASCII, mostrando posições, distância, combustível e estado de cada aeronave.

## Estrutura dos arquivos

- `AtcServer/Program.cs` - Servidor SOAP e lógica de simulação de aeronaves.
- `AtcServer/AtcServer.csproj` - Projeto .NET com dependência `SoapCore`.
- `plane_client.py` - Cliente Python que consome o serviço SOAP.
- `ATC.png` - Imagem de apresentação do projeto.

## Dicas rápidas

- Primeiro inicie o servidor .NET.
- Depois execute o cliente Python.
- Use o radar ao vivo para ver o status atualizado do tráfego aéreo.

---

Se quiser, posso também adicionar um tópico extra ao README explicando cada parte do código linha a linha. 