# Agente Monitor (Teste)

Teste prático para a vaga de Desenvolvedor Full Stack.

O objetivo deste projeto é desenvolver um miniagente responsável por monitorar as ações realizadas pelo usuário na máquina e disponibilizar essas informações para visualização.

---

# Instalação

## Requisitos

- Docker
- Visual Studio Code
- .NET 8.0 ou superior
- Navegador de sua preferência

## 1. Clonar o repositório

Baixe o repositório para sua máquina (imagem abaixo).

No GitHub:

1. Clique em **Code**.
2. Selecione **HTTPS**.
3. Clique no ícone de copiar.

Em seguida, na Área de Trabalho, clique com o botão direito do mouse e selecione **Open Git Bash Here**.

Execute o comando abaixo:

```bash
git clone https://github.com/PedroAlvesDPG/Pedro-Alves-teste.git
```

Após concluir o download, feche o Git Bash.

Na pasta **Pedro-Alves-teste**, clique com o botão direito do mouse e selecione **Abrir com Code** (imagem abaixo).

---

## 2. Iniciar o banco de dados

Com o Visual Studio Code aberto:

1. Abra um terminal em **Terminal → Novo Terminal**.
2. Execute o comando:

```bash
docker compose up -d
```

Após a execução, o banco de dados estará disponível.

### Comandos úteis do Docker

```bash
docker compose up -d    # Inicia os containers em segundo plano.
docker compose up       # Inicia os containers exibindo os logs.
docker compose ps       # Exibe o status dos containers.
docker compose down     # Para os containers, mantendo os dados.
docker compose down -v  # Para os containers e remove todos os dados.
```

---

## 3. Iniciar a API

Abra um novo terminal e execute:

```bash
cd MonitorApi
dotnet run
```

---

## 4. Iniciar o Agente

Abra outro terminal e execute:

```bash
cd MonitorAgent
dotnet run
```

Após esses passos, o agente estará em execução e começará a monitorar as ações realizadas no dispositivo.

---

# Como usar

Para visualizar os dados coletados, acesse o endereço abaixo no navegador:

```text
http://localhost:5000
```

Na seção **Últimos sinais**, são exibidos os registros mais recentes capturados pelo sistema, com informações detalhadas sobre cada evento.

Além disso, existem duas seções de **Relatórios**:

- **Relatório por período:** exibe os processos registrados dentro de um intervalo de tempo.
- **Relatório por máquina:** exibe os processos agrupados por máquina.
