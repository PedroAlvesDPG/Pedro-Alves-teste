namespace MonitorApi.Web;

/// <summary>
/// Dashboard (HTML) servido em "/". Busca os dados da própria API via fetch e
/// renderiza em tabelas + um gráfico de barras simples. Sem bibliotecas externas.
/// </summary>
public static class HomePage
{
    public const string Html = """
        <!doctype html>
        <html lang="pt-br">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>MonitorApi — Dashboard</title>
          <style>
            :root { color-scheme: light dark; }
            * { box-sizing: border-box; }
            body {
              font-family: system-ui, -apple-system, Segoe UI, Roboto, sans-serif;
              max-width: 1000px; margin: 32px auto; padding: 0 20px; line-height: 1.45;
            }
            h1 { margin-bottom: 2px; }
            .sub { color: #6b7280; margin-top: 0; }
            section { margin: 30px 0; }
            h2 { font-size: 1.15rem; border-bottom: 1px solid #8883; padding-bottom: 6px; display: flex; align-items: center; gap: 10px; }
            .controls { margin: 10px 0; display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
            button, select {
              font: inherit; padding: 6px 12px; border: 1px solid #8884; border-radius: 8px;
              background: transparent; cursor: pointer;
            }
            button:hover { border-color: #2563eb; }
            button.active { border-color: #2563eb; font-weight: 600; }
            table { width: 100%; border-collapse: collapse; margin-top: 8px; font-size: .92rem; }
            th, td { text-align: left; padding: 7px 10px; border-bottom: 1px solid #8882; vertical-align: top; }
            th { color: #6b7280; font-weight: 600; white-space: nowrap; }
            td.num { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
            td.title { max-width: 380px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
            .bar-wrap { display: flex; align-items: center; gap: 8px; }
            .bar { height: 14px; border-radius: 3px; background: #2563eb; min-width: 2px; }
            .muted { color: #6b7280; font-size: .85rem; }
            .status { font-size: .8rem; color: #6b7280; font-weight: 400; margin-left: auto; }
            code { background: #8882; padding: 1px 5px; border-radius: 4px; }
          </style>
        </head>
        <body>
          <h1>MonitorApi</h1>
          <p class="sub">Painel de leitura dos dados capturados.</p>

          <div class="controls">
            <button id="refresh">↻ Atualizar tudo</button>
            <label class="muted"><input type="checkbox" id="auto"> auto-atualizar (10s)</label>
          </div>

          <section>
            <h2>Últimos sinais <span id="s-status" class="status"></span></h2>
            <div class="controls">
              <span class="muted">Quantidade:</span>
              <select id="s-limit">
                <option value="50">50</option>
                <option value="100" selected>100</option>
                <option value="250">250</option>
              </select>
              <span id="rt-status" class="muted">● conectando…</span>
            </div>
            <table id="s-table"><thead><tr>
              <th>Hora (Brasília)</th><th>Máquina</th><th>Usuário</th><th>Processo</th><th>Janela</th>
            </tr></thead><tbody></tbody></table>
          </section>

          <section>
            <h2>Relatório 1 — processos por período <span id="p-status" class="status"></span></h2>
            <div class="controls">
              <button class="p-win active" data-min="60">60 min</button>
              <button class="p-win" data-min="120">120 min</button>
              <button class="p-win" data-min="1440">24 horas</button>
            </div>
            <table id="p-table"><thead><tr>
              <th>Processo</th><th>Amostras</th><th>≈ tempo</th><th style="width:40%">Proporção</th>
            </tr></thead><tbody></tbody></table>
          </section>

          <section>
            <h2>Relatório 2 — por máquina <span id="m-status" class="status"></span></h2>
            <div class="controls">
              <button class="m-win active" data-h="24">24 horas</button>
              <button class="m-win" data-h="8">8 horas</button>
              <button class="m-win" data-h="168">7 dias</button>
            </div>
            <table id="m-table"><thead><tr>
              <th>Máquina</th><th>Hora (Brasília)</th><th>Amostras</th>
            </tr></thead><tbody></tbody></table>
          </section>

          <p class="muted">Cada amostra ≈ 3 segundos em foco (20 amostras ≈ 1 minuto).</p>

        <script src="/js/signalr.min.js"></script>
        <script>
          const $ = (s) => document.querySelector(s);
          const esc = (v) => String(v ?? "").replace(/[&<>"']/g, c =>
            ({ "&":"&amp;", "<":"&lt;", ">":"&gt;", '"':"&quot;", "'":"&#39;" }[c]));
          // Remove o sufixo " (Brasília)" pois o cabeçalho da coluna já diz.
          const stripBr = (s) => String(s ?? "").replace(" (Brasília)", "");
          const tempo = (amostras) => {
            const seg = amostras * 3;
            if (seg < 60) return seg + "s";
            const m = Math.floor(seg / 60), s = seg % 60;
            return m + "min" + (s ? " " + s + "s" : "");
          };

          async function getJson(url, statusEl) {
            try {
              statusEl.textContent = "carregando…";
              const r = await fetch(url);
              if (!r.ok) throw new Error("HTTP " + r.status);
              const data = await r.json();
              statusEl.textContent = "atualizado";
              return data;
            } catch (e) {
              statusEl.textContent = "erro: " + e.message + " (a API está rodando?)";
              return null;
            }
          }

          // Monta a linha de um sinal (reutilizada na carga inicial e no tempo real).
          function signalRow(x) {
            return `<tr>
              <td>${esc(stripBr(x.timestampLocal))}</td>
              <td>${esc(x.hostname)}</td>
              <td>${esc(x.usuario)}</td>
              <td>${esc(x.processo)}</td>
              <td class="title" title="${esc(x.tituloJanela)}">${esc(x.tituloJanela)}</td>
            </tr>`;
          }

          async function loadSignals() {
            const limit = $("#s-limit").value;
            const rows = await getJson("/api/signals?limit=" + limit, $("#s-status"));
            const tb = $("#s-table tbody");
            if (!rows) { tb.innerHTML = ""; return; }
            tb.innerHTML = rows.map(signalRow).join("");
          }

          async function loadProcess(minutes) {
            const data = await getJson("/api/reports/process-counts?minutes=" + minutes, $("#p-status"));
            const tb = $("#p-table tbody");
            if (!data) { tb.innerHTML = ""; return; }
            const list = data.processos || [];
            const max = list.reduce((a, b) => Math.max(a, b.amostras), 0) || 1;
            tb.innerHTML = list.map(x => `<tr>
              <td>${esc(x.processo)}</td>
              <td class="num">${x.amostras}</td>
              <td class="num">${tempo(x.amostras)}</td>
              <td><div class="bar-wrap"><div class="bar" style="width:${(x.amostras / max * 100).toFixed(1)}%"></div></div></td>
            </tr>`).join("") || `<tr><td colspan="4" class="muted">Nenhum dado no período.</td></tr>`;
          }

          async function loadMachines(hours) {
            const data = await getJson("/api/reports/samples-by-machine-hour?hours=" + hours, $("#m-status"));
            const tb = $("#m-table tbody");
            if (!data) { tb.innerHTML = ""; return; }
            const list = data.amostras || [];
            tb.innerHTML = list.map(x => `<tr>
              <td>${esc(x.hostname)}</td>
              <td>${esc(stripBr(x.horaLocal))}</td>
              <td class="num">${x.amostras}</td>
            </tr>`).join("") || `<tr><td colspan="3" class="muted">Nenhum dado no período.</td></tr>`;
          }

          let curMin = 60, curHours = 24;
          function refreshAll() { loadSignals(); loadProcess(curMin); loadMachines(curHours); }

          $("#s-limit").addEventListener("change", loadSignals);
          $("#refresh").addEventListener("click", refreshAll);
          document.querySelectorAll(".p-win").forEach(b => b.addEventListener("click", () => {
            document.querySelectorAll(".p-win").forEach(x => x.classList.remove("active"));
            b.classList.add("active"); curMin = b.dataset.min; loadProcess(curMin);
          }));
          document.querySelectorAll(".m-win").forEach(b => b.addEventListener("click", () => {
            document.querySelectorAll(".m-win").forEach(x => x.classList.remove("active"));
            b.classList.add("active"); curHours = b.dataset.h; loadMachines(curHours);
          }));

          let autoTimer = null;
          $("#auto").addEventListener("change", (e) => {
            if (e.target.checked) autoTimer = setInterval(refreshAll, 10000);
            else clearInterval(autoTimer);
          });

          // ----- Tempo real (SignalR): novas linhas aparecem sem precisar atualizar -----
          const rt = $("#rt-status");
          const conn = new signalR.HubConnectionBuilder()
            .withUrl("/hub/signals")
            .withAutomaticReconnect()
            .build();

          conn.on("novoSinal", (s) => {
            const tb = $("#s-table tbody");
            tb.insertAdjacentHTML("afterbegin", signalRow(s));   // insere no topo
            const max = parseInt($("#s-limit").value, 10);
            while (tb.rows.length > max) tb.deleteRow(tb.rows.length - 1); // mantém o tamanho
          });

          conn.onreconnecting(() => { rt.textContent = "● reconectando…"; });
          conn.onreconnected(() => { rt.textContent = "● tempo real ligado"; });
          conn.onclose(() => { rt.textContent = "● desconectado"; });
          conn.start()
            .then(() => { rt.textContent = "● tempo real ligado"; })
            .catch(() => { rt.textContent = "● tempo real indisponível"; });

          refreshAll();
        </script>
        </body>
        </html>
        """;
}
