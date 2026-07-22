
# Agente Monitor (Teste)

Teste prático para a vaga de Desenvolvedor Fullstack. O foco do teste é criar um mini-agente, o qual tem o dever de monitorar as ações do usuário na máquina.




## Instalação

Como instalar e usar o agente

### Requisitos:
```bash
 Docker 
 Visual Studio Code 
 Dotnet 8.0+  
 Navegador (de sua preferência)
```
Baixe o repositorio local (Imagem abaixo):
Clique em code -> Https -> Clice em copiar

Na área de trabalho, clique com o botão direito do mouse e selecione Open Git Bash here. Execute o seguinte comando:
```bash
git clone https://github.com/PedroAlvesDPG/Pedro-Alves-teste.git
```


Apos esse processo, feche o git bash. Clique com o botão direito na pasta Pedro-Alves-teste na área de trabalho, selecione Abrir com Code. (Imagem abaixo)

Com o VS Code aberto, clique em Terminal -> Novo Terminal

Execute o seguinte comando:
docker compose up -d

O banco de dados já está no ar agora.

### Comandos utils do Docker:
```bash
docker compose up -d. para iniciar pela primeira vez
docker compose up. para iniciar
docker compose ps. mostra o status. 
docker compose down. para parar (mantém dados)
docker compose down -v. para apagar os dados.
```

Para iniciarmos o agente, faltam dois passos. Abra um novo terminal e execute o comando:
```bash
cd MonitorApi
dotnet run
```

Após isso, abra um novo terminal e execute o comando:
```bash
cd MonitorAgent
dotnet run
```
O agente agora está rodando e monitorando todas as ações no dispositivo.

## Como usar

Para visualizar os dados gerados, acesse no seu navegador: 
```bash
http://localhost:8080
```

### O Login:


| Campo             | Insira                                                             |
| ----------------- | ------------------------------------------------------------------ |
| Sistema | PostgreSQL |
| Servidor | postgres |
| Usuário | monitor |
| Senha | monitor |
| Base | monitor |
