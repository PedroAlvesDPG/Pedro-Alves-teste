# Decisões

Este documento explica as principais decisões de arquitetura do projeto, os trade-offs, os pontos que ainda podem ser melhorados e onde utilizei IA durante o desenvolvimento.

## Visão geral

O projeto é dividido em três partes:

- **Agente** (Console/.NET): captura a janela ativa a cada 3 segundos e envia os dados para a API.
- **API** (ASP.NET Core): recebe os dados, armazena no PostgreSQL e disponibiliza consultas e relatórios.
- **Dashboard**: interface simples que consome os dados da API.

Optei por capturar o **título da janela ativa**, em vez da lista de processos, por considerar uma informação mais útil e mais alinhada ao objetivo do desafio.

---

## Decisões e trade-offs

### Agente como Worker Service

Escolhi implementar o agente como um **Worker Service**.

Isso permite executá-lo normalmente com `dotnet run` durante o desenvolvimento e, futuramente, instalá-lo como um Serviço do Windows sem alterações na estrutura do projeto.

O custo dessa escolha foi apenas uma configuração inicial um pouco maior.

---

### Fila local em SQLite

Caso a API fique indisponível, o agente continua funcionando normalmente.

Os sinais são armazenados em uma fila local utilizando SQLite e reenviados automaticamente quando a API volta a responder.

Validei esse comportamento interrompendo a API durante a execução e verificando que todos os registros pendentes foram enviados posteriormente.

O modelo utilizado é **at-least-once**, ou seja, garante que o dado será entregue, mas existe a possibilidade de registros duplicados.

---

### Armazenamento em UTC

Todos os registros são armazenados em **UTC** (`timestamptz`).

A conversão para o horário de Brasília acontece apenas na leitura, através de:

- um campo `horaLocal` retornado pela API;
- uma view `signals_local` no PostgreSQL.

Essa decisão evita problemas de comparação e ordenação caso máquinas em fusos diferentes enviem dados para o mesmo banco.

---

### Dapper em vez de EF Core

Preferi utilizar o Dapper por oferecer maior controle sobre as consultas SQL, principalmente nos relatórios de agregação.

Como o banco possui poucas tabelas, optei por criar sua estrutura na inicialização da API usando `CREATE TABLE IF NOT EXISTS`, dispensando migrations.

---

### Índices do banco

Os índices foram criados pensando nas consultas realizadas pela aplicação:

- `(timestamp_utc, processo)` para relatórios por período;
- `(timestamp_utc, hostname)` para relatórios por máquina;
- `(hostname, timestamp_utc DESC)` para consultas filtradas por máquina.

---

### Interface para coleta

A captura da janela ativa foi isolada na interface `IActivityCollector`.

Assim, apenas essa implementação depende do sistema operacional.

Essa abordagem facilita tanto a criação de testes quanto uma futura implementação para Linux ou macOS.

---

### Docker apenas para o banco

Utilizei Docker somente para o PostgreSQL e o Adminer.

A API e o agente são executados diretamente pelo .NET.

Escolhi essa abordagem para simplificar o ambiente de desenvolvimento.

---

## Testes

Foi criado um projeto de testes (`MonitorAgent.Tests`) utilizando xUnit.

Os testes cobrem a lógica do `SignalFactory`, verificando:

- geração do timestamp em UTC;
- mapeamento correto dos campos;
- comportamento quando nenhuma janela ativa é encontrada.

Para tornar os testes determinísticos, utilizei um coletor falso e um `TimeProvider` fixo.

Não foram implementados testes para a captura Win32 nem para a comunicação HTTP.

---

## Limitações conhecidas

- A fila local não possui limite de tamanho e pode crescer caso a API permaneça indisponível por muito tempo.
- Como não existe uma chave de idempotência, alguns registros podem ser duplicados em situações específicas.
- A autenticação por API Key existe, mas está desabilitada por padrão.
- A conversão de horário está fixa para `America/Sao_Paulo`.

---

## Melhorias futuras

Com mais tempo, eu implementaria:

- limite e política de retenção para a fila local;
- chave única (`client_signal_id`) para evitar duplicidades;
- envio de múltiplos sinais por requisição;
- autenticação por API Key e HTTPS habilitados;
- testes de integração para a fila SQLite e para a API.

---

## Suporte a Linux e macOS

A estrutura atual foi pensada para que apenas a implementação de `IActivityCollector` precise mudar.

No Linux, a captura da janela ativa depende do ambiente gráfico utilizado (X11 ou Wayland).

No macOS, seria possível utilizar as APIs do sistema para obter o aplicativo em primeiro plano e o título da janela.

Toda a lógica restante do sistema permaneceria igual.

---

## Uso de IA

Utilizei principalmente o Claude para acelerar o desenvolvimento, especialmente na geração da estrutura inicial do projeto, configuração de dependências, rascunhos das consultas SQL e desta documentação.

Mesmo assim, vários trechos precisaram ser revisados e ajustados manualmente.

Alguns exemplos:

- correção do mapeamento entre `timestamptz` e `DateTime` no Dapper, que inicialmente causava erro 500;
- ajuste da formatação das datas para o padrão brasileiro, mantendo o armazenamento em UTC;
- compatibilização de versões de pacotes com o .NET 8.
