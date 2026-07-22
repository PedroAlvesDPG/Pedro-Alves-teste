# Decisões

Anotações sobre por que fiz as coisas do jeito que fiz, o que ficou faltando e
onde usei IA. Escrevi de forma direta pra facilitar a conversa depois.

## Visão geral

O projeto tem três partes:

- **Agente** (console/.NET) — a cada 3s pega a janela ativa e manda pra API.
- **API** (ASP.NET Core) — recebe, grava no Postgres, e devolve leitura + relatórios.
- **Dashboard** — uma página simples que lê os dados da própria API.

Escolhi capturar o **título da janela ativa** (em vez da lista de processos)
porque é mais interessante e casa melhor com a parte de coleta específica de SO.

## Decisões e trade-offs

### Agente como Worker Service (e não console "puro")
- Roda como console no `dotnet run` (bom pra dev) e também dá pra instalar como
  Serviço do Windows.
- Trade-off: é um pouco mais de setup que um `Console.WriteLine` no `Main`, mas
  ganhei a capacidade de rodar como serviço "de graça".

### Fila local em SQLite (resiliência)
- Se a API estiver fora, o agente **não trava e não perde dado**: o sinal vai pra
  uma fila em SQLite e é reenviado quando a API volta.
- Testei isso na mão: derrubei a API, o agente ficou enfileirando, subi de novo e
  os pendentes foram enviados.
- Trade-off: é entrega "at-least-once" (garante que chega, mas pode duplicar — ver
  pontos fracos).

### UTC no banco, horário local só na hora de mostrar
- O banco guarda tudo em **UTC** (`timestamptz`). Não converti pra horário local no
  armazenamento de propósito.
- Motivo: se cada máquina gravasse no seu fuso, os dados ficariam impossíveis de
  comparar/ordenar.
- A conversão pra Brasília acontece só na leitura:
  - um campo `horaLocal` no JSON da API;
  - uma view `signals_local` no Postgres (pra ver bonito no Adminer).
- Foi a parte que mais tomei cuidado, porque o enunciado avisa que "derruba muita gente".

### Dapper + SQL escrito à mão (em vez de EF Core)
- Preferi ver o SQL que realmente roda, ainda mais porque o relatório é uma query
  de agregação.
- Trade-off: perco as migrations automáticas do EF. Como o schema é pequeno, criei
  a tabela no start da API (`CREATE TABLE IF NOT EXISTS`).

### Índices pensados pra query do relatório
- Em vez de índices soltos, criei compostos alinhados a cada consulta:
  - `(timestamp_utc, processo)` → relatório de processos por período;
  - `(timestamp_utc, hostname)` → relatório por máquina/hora;
  - `(hostname, timestamp_utc DESC)` → leitura filtrando por máquina.

### Coleta atrás de uma interface (`IActivityCollector`)
- A única parte que depende do SO é "qual é a janela ativa". Isolei atrás de uma
  interface, com uma implementação Windows.
- Ganhei duas coisas: (1) portar pra outro SO é trocar uma linha no registro de DI;
  (2) consigo testar a lógica com um coletor "fake", sem depender da Win32 real.

### Docker só pro Postgres
- O `docker-compose` sobe o Postgres (e o Adminer pra olhar o banco). A API e o
  agente rodam direto no .NET.
- Motivo: reduzir a parte de Docker, que é onde eu tenho menos experiência.

## Testes

- Tem um projeto de teste (`MonitorAgent.Tests`, xUnit) cobrindo o `SignalFactory`,
  que é onde mora a lógica de montar o sinal.
- Três testes: timestamp sai em UTC, campos mapeados certo, e o caso de "nenhuma
  janela ativa" (não pode quebrar).
- Usei um coletor fake e um relógio fixo (`TimeProvider`) pra o teste ser
  determinístico.
- Não testei a captura Win32 nem a fila/HTTP de verdade — isso é mais integração.

## Pontos fracos que eu sei que existem

- **Fila local sem limite.** Se a API ficar fora muito tempo, o SQLite cresce sem
  parar. Tem um `LocalRetentionDays` no config, mas ainda não está sendo aplicado.
- **Pode duplicar.** Se o POST grava no banco mas a resposta se perde, o agente
  reenvia e cria uma linha repetida. Faltou uma chave de idempotência.
- **Sem autenticação/TLS por padrão.** Tem um esquema de API Key opcional, mas vazio.
  Serve pra dev, não pra produção.
- **Privacidade.** Título de janela pode ter dado sensível (nome de arquivo, assunto
  de e-mail). Em produção precisaria de consentimento e controle de acesso (LGPD).
- **Fuso fixo.** A conversão pra local está fixa em `America/Sao_Paulo`.

## O que eu faria com mais tempo

- Aplicar de fato a retenção/limite na fila local (descartar o mais antigo).
- Adicionar `client_signal_id` único + `ON CONFLICT DO NOTHING` pra evitar duplicata.
- Envio em lote (vários sinais por request) pra escalar melhor.
- Ligar a API Key e HTTPS.
- Mais testes: a fila SQLite e um teste de integração da ingestão.
- O push em tempo real (SignalR) que ficou pra depois.

## Como eu levaria a coleta pra Linux e macOS

A ideia é manter tudo igual e só escrever uma nova implementação do
`IActivityCollector`.

- **Linux:** não existe um jeito único de pegar a "janela ativa".
  - No X11 dá pra ler a janela ativa (`_NET_ACTIVE_WINDOW`) ou usar algo como o
    `xdotool`.
  - No Wayland é mais difícil, porque por segurança não tem uma API padrão pra isso —
    dependeria do compositor (ex.: extensão no GNOME).
  - A lista de processos, se fosse por esse caminho, é fácil (ler o `/proc`).
- **macOS:** dá pra pegar o app em primeiro plano pela API do sistema
  (`NSWorkspace`), e o título da janela pela API de Acessibilidade — que exige
  permissão explícita do usuário.

Em todos os casos, hostname/usuário/timestamp continuam iguais (não dependem de SO),
então só a captura da janela muda.

## Onde usei IA (e onde precisei corrigir)

- Usei o Claude bastante pra acelerar: scaffolding dos projetos, boilerplate de DI,
  primeiro rascunho das queries e desta documentação.
- Mas não colei no escuro. Alguns pontos que precisei entender e corrigir:
  - **`timestamptz` → `DateTime` no Dapper.** A primeira versão usava
    `DateTimeOffset` nos records de leitura e a API dava erro 500. Vi no log que o
    Dapper não achava construtor compatível e troquei pra `DateTime` (que é como o
    Npgsql devolve `timestamptz`).
  - **Formato de data.** O padrão vinha ISO (AAAA/MM/DD); troquei pra DD/MM/AAAA,
    mas mantendo o valor em UTC e deixando claro com o sufixo, pra não cair na
    armadilha de confundir com horário local.
  - **Versões de pacote.** Alguns pacotes vieram numa versão de .NET mais nova que a
    minha e a restauração falhou; fixei nas versões do .NET 8.
```
