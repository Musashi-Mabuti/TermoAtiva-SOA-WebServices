// Painel simples servido pelo próprio IIS. Usa textContent (nunca innerHTML) para evitar XSS.
const el = (tag, text, cls) => { const e = document.createElement(tag); e.textContent = text; if (cls) e.className = cls; return e; };

async function carregarAlerta() {
  const alvo = document.getElementById('alerta-conteudo');
  const bairro = document.getElementById('bairro').value;
  alvo.replaceChildren(el('p', 'Carregando…'));
  try {
    const r = await fetch(`/api/alertas/atual?bairro=${encodeURIComponent(bairro)}`);
    if (r.status === 404) { alvo.replaceChildren(el('p', 'Ainda não há leituras para este bairro.')); return; }
    if (!r.ok) throw new Error(r.status);
    const a = await r.json();
    alvo.replaceChildren(
      el('p', `${a.temperaturaC} °C · umidade ${a.umidadePct}% · sensação ${a.sensacaoTermicaC} °C`),
      el('span', a.nivel, `nivel ${a.nivel}`),
      el('p', a.orientacao));
  } catch { alvo.replaceChildren(el('p', 'Não foi possível consultar o alerta.')); }
}

async function carregarPontos() {
  const ul = document.getElementById('pontos');
  try {
    const r = await fetch('/api/pontos-acolhimento');
    const pontos = await r.json();
    ul.replaceChildren(...pontos.map(p => el('li', `${p.nome} (${p.tipo}) — ${p.endereco}, ${p.bairro}`)));
  } catch { ul.replaceChildren(el('li', 'Erro ao carregar pontos.')); }
}

async function carregarHealth() {
  const d = document.getElementById('health');
  try { const h = await (await fetch('/health')).json(); d.textContent = `✔ ${h.servico} online — ${new Date(h.utc).toLocaleString('pt-BR')}`; }
  catch { d.textContent = '✖ API indisponível'; }
}

document.getElementById('bairro').addEventListener('change', carregarAlerta);
carregarAlerta(); carregarPontos(); carregarHealth();
setInterval(carregarAlerta, 30000);
