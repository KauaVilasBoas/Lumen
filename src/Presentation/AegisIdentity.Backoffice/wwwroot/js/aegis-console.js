/* ============================================================
   AegisIdentity Console — interactivity (vanilla JS)
   Page-aware: each module initialises only if its root exists.
   ============================================================ */
(function () {
  'use strict';

  const $ = (s, r = document) => r.querySelector(s);
  const $$ = (s, r = document) => [...r.querySelectorAll(s)];
  const NS = 'http://www.w3.org/2000/svg';
  const svgPath = (d, cls, stroke) => {
    const p = document.createElementNS(NS, 'path');
    p.setAttribute('d', d); p.setAttribute('class', cls);
    if (stroke) p.style.stroke = stroke;
    return p;
  };

  const METHOD_COLOR = { GET: 'var(--app)', POST: 'var(--pres)', PUT: 'var(--warn)', DELETE: 'var(--danger)' };
  const LAYER_COLORS = { pres: 'var(--pres)', app: 'var(--app)', dom: 'var(--dom)', infra: 'var(--infra)', jobs: 'var(--jobs)', cache: 'var(--cache)' };

  const USER_PALETTE = [
    'linear-gradient(135deg,#9a7dff,#6b49f0)',
    'linear-gradient(135deg,#4c8dff,#2a5fd6)',
    'linear-gradient(135deg,#2bd4a0,#159e78)',
    'linear-gradient(135deg,#f5a623,#d4830a)',
    'linear-gradient(135deg,#ff5d73,#c83a52)',
    'linear-gradient(135deg,#7b86a0,#525c74)',
    'linear-gradient(135deg,#38bdf8,#0369a1)',
    'linear-gradient(135deg,#a78bfa,#7c3aed)',
  ];

  const PROFILE_PALETTE = [
    '#8b6dff', '#5b6478', '#4c8dff', '#2bd4a0',
    '#f5a623', '#ff5d73', '#38bdf8', '#a78bfa',
  ];

  function deterministicIndex(str, length) {
    let h = 0;
    for (let i = 0; i < str.length; i++) { h = (Math.imul(31, h) + str.charCodeAt(i)) | 0; }
    return Math.abs(h) % length;
  }

  function userColor(id) { return USER_PALETTE[deterministicIndex(id, USER_PALETTE.length)]; }
  function profileColor(id) { return PROFILE_PALETTE[deterministicIndex(id, PROFILE_PALETTE.length)]; }

  function methodFromCode(code) {
    const action = (code.split('.')[1] || '').toLowerCase();
    if (action === 'create' || action === 'assign') return 'POST';
    if (action === 'update' || action === 'setpermissions') return 'PUT';
    if (action === 'delete' || action === 'remove') return 'DELETE';
    return 'GET';
  }

  function routeFor(code) {
    const [ctrl, action] = code.split('.');
    const base = '/api/' + ctrl.toLowerCase();
    const map = {
      Index: base, Get: base + '/{id}', Create: base, Update: base + '/{id}',
      Delete: base + '/{id}', SetPermissions: base + '/{id}/permissions',
      Assign: base + '/{userId}/{id}', Remove: base + '/{userId}/{id}', Ping: base + '/ping', Export: base + '/export',
    };
    return map[action] || base;
  }

  /* ---------------- Reveal Architecture ---------------- */
  function initReveal() {
    const toggle = $('[data-reveal-toggle]');
    if (!toggle) return;
    const apply = (on) => {
      document.body.classList.toggle('reveal-on', on);
      toggle.classList.toggle('on', on);
      const sw = $('.reveal-toggle-switch', toggle);
      if (sw) sw.classList.toggle('on', on);
      try { localStorage.setItem('aegis.reveal', on ? '1' : '0'); } catch (e) {}
    };
    let on = false;
    try { on = localStorage.getItem('aegis.reveal') === '1'; } catch (e) {}
    apply(on);
    toggle.addEventListener('click', () => apply(!document.body.classList.contains('reveal-on')));

    // hover popovers for reveal tags
    $$('.reveal-tag').forEach(tag => {
      const pop = $('.reveal-tag-pop', tag);
      if (!pop) return;
      pop.style.display = 'none';
      tag.addEventListener('mouseenter', () => { pop.style.display = 'block'; });
      tag.addEventListener('mouseleave', () => { pop.style.display = 'none'; });
    });
  }

  /* ---------------- Login staged animation ---------------- */
  function initLogin() {
    const form = $('[data-login-form]');
    if (!form) return;
    const stagesEl = $('.login-stages', form.closest('.login-card'));
    const btn = $('.login-submit', form);
    form.addEventListener('submit', (e) => {
      if (form.dataset.armed === '1') return; // allow real submit
      e.preventDefault();
      if (stagesEl) stagesEl.style.display = 'flex';
      btn.disabled = true; btn.textContent = 'Authenticating…';
      const steps = $$('.login-stage', stagesEl);
      let i = 0;
      const tick = () => {
        if (i > 0) { steps[i - 1].classList.remove('active'); steps[i - 1].classList.add('done'); const d = $('.login-stage-dot', steps[i - 1]); if (d) d.innerHTML = '<svg viewBox="0 0 24 24" width="1em" height="1em" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="m4 12 5 5L20 6"/></svg>'; }
        if (i < steps.length) { steps[i].classList.add('active'); i++; setTimeout(tick, 520); }
        else { form.dataset.armed = '1'; form.submit(); }
      };
      tick();
    });
  }

  /* ---------------- Profile permission matrix ---------------- */
  function initMatrix() {
    const matrix = $('[data-matrix]');
    if (!matrix) return;
    const readonly = matrix.dataset.readonly === '1';
    const footNote = $('[data-matrix-affected]');
    const affectedBase = footNote ? footNote.dataset.affected : '0';

    const updateGroup = (group) => {
      const checks = $$('.mperm', group);
      const on = checks.filter(c => c.classList.contains('on')).length;
      const head = $('.mgcheck', group.closest('.matrix-group'));
      const counter = $('.matrix-group-count', group.closest('.matrix-group'));
      if (counter) counter.textContent = on + '/' + checks.length;
      if (head) {
        head.classList.toggle('on', on === checks.length && on > 0);
        head.classList.toggle('partial', on > 0 && on < checks.length);
        head.innerHTML = on === checks.length && on > 0
          ? '<svg viewBox="0 0 24 24" width="1em" height="1em" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="m4 12 5 5L20 6"/></svg>'
          : (on > 0 ? '<span class="dash"></span>' : '');
      }
    };

    if (readonly) return;

    $$('.mperm', matrix).forEach(btn => {
      btn.addEventListener('click', () => {
        const cb = $('input[type=checkbox]', btn);
        const on = !btn.classList.contains('on');
        btn.classList.toggle('on', on);
        if (cb) cb.checked = on;
        updateGroup(btn.closest('.matrix-perms'));
      });
    });
    $$('.mgcheck', matrix).forEach(head => {
      head.addEventListener('click', () => {
        const groupBody = $('.matrix-perms', head.closest('.matrix-group'));
        const perms = $$('.mperm', groupBody);
        const allOn = perms.every(p => p.classList.contains('on'));
        perms.forEach(p => {
          p.classList.toggle('on', !allOn);
          const cb = $('input[type=checkbox]', p); if (cb) cb.checked = !allOn;
        });
        updateGroup(groupBody);
      });
    });
  }

  /* ---------------- Architecture request trace ---------------- */
  function initTrace() {
    const trace = $('[data-trace]');
    if (!trace) return;
    const steps = $$('.trace-step', trace);
    const setStep = (n) => steps.forEach((s, i) => {
      s.classList.toggle('on', i <= n);
      s.classList.toggle('cur', i === n);
      const d = $('.trace-d', s); if (d) d.style.display = i === n ? 'block' : 'none';
    });
    setStep(steps.length - 1);
    steps.forEach((s, i) => s.addEventListener('click', () => { stop(); setStep(i); }));
    let timer = null;
    const stop = () => { if (timer) clearTimeout(timer); timer = null; };
    const play = () => {
      stop(); let i = 0; setStep(0);
      const t = () => { i++; if (i < steps.length) { setStep(i); timer = setTimeout(t, 900); } };
      timer = setTimeout(t, 900);
    };
    const btn = $('[data-trace-replay]'); if (btn) btn.addEventListener('click', play);
  }

  /* ---------------- Authorization Graph ---------------- */
  function initGraph() {
    const root = $('[data-graph]');
    if (!root) return;
    let data;
    try { data = JSON.parse($('#graph-data').textContent); } catch (e) { return; }
    const { users, profiles, permissions } = data;

    const rail = $('[data-graph-rail]', root);
    const canvas = $('[data-graph-canvas]', root);
    const colUser = $('[data-col-user]', root);
    const colProfiles = $('[data-col-profiles]', root);
    const colPerms = $('[data-col-perms]', root);
    const svg = $('.graph-edges', root);
    const cacheCard = $('[data-cache]', root);
    const summary = { p: $('[data-sum-profiles]', root), e: $('[data-sum-endpoints]', root), r: $('[data-sum-revoked]', root) };

    let current = users[0]?.id;
    if (!current) return;
    let revoked = [];
    let hoverProfile = null, hoverPerm = null;
    const nodeRefs = {};

    const liveUsers = users.filter(u => u.state !== 'deleted');

    function resolve(userId) {
      const u = users.find(x => x.id === userId);
      const prs = u.profiles
        .filter(pid => profiles[pid])
        .map(pid => ({ id: pid, ...profiles[pid] }));
      const m = new Map();
      prs.forEach(pr => pr.permissions.forEach(permId => {
        const perm = permissions[permId];
        if (!perm || perm.orphan || revoked.includes(permId)) return;
        if (!m.has(permId)) m.set(permId, new Set());
        m.get(permId).add(pr.id);
      }));
      const perms = [...m.entries()].map(([permId, set]) => ({ perm: { id: permId, ...permissions[permId] }, from: [...set] }))
        .sort((a, b) => a.perm.code.localeCompare(b.perm.code));
      return { u, prs, perms };
    }

    function renderRail() {
      rail.innerHTML = '';
      liveUsers.forEach(u => {
        const b = document.createElement('button');
        b.className = 'grail-item' + (u.id === current ? ' active' : '');
        b.innerHTML = `<div class="avatar" style="background:${userColor(u.id)};width:30px;height:30px;font-size:12px">${u.username[0].toUpperCase()}</div>
          <div class="grail-meta"><div class="grail-name">${u.username}</div><div class="grail-sub mono">${u.profiles.length} profile${u.profiles.length !== 1 ? 's' : ''}</div></div>
          ${u.state !== 'active' ? `<span class="statedot ${u.state}"></span>` : ''}`;
        b.addEventListener('click', () => { current = u.id; revoked = []; setCache('miss'); renderAll(); setTimeout(() => setCache('hit'), 1000); });
        rail.appendChild(b);
      });
    }

    function badgeFor(state) {
      return { active: 'ok', locked: 'danger', pending: 'warn', deleted: 'muted' }[state] || 'muted';
    }
    function stateLabel(state) {
      return { active: 'Active', locked: 'Locked', pending: 'Pending email', deleted: 'Soft-deleted' }[state] || state;
    }

    function renderGraph() {
      const { u, prs, perms } = resolve(current);
      // col 1
      colUser.innerHTML = `<div class="gnode unode" data-ref="user">
        <div class="avatar" style="background:${userColor(u.id)};width:44px;height:44px;font-size:16px;border-radius:13px">${u.username[0].toUpperCase()}</div>
        <div class="unode-name">${u.username}</div>
        <div class="unode-mail mono">${u.email}</div>
        <div class="unode-state"><span class="badge ${badgeFor(u.state)}">${stateLabel(u.state)}</span></div></div>`;
      // col 2
      colProfiles.innerHTML = prs.length ? '' : '<div class="gcol-empty mono">no profiles assigned</div>';
      prs.forEach(pr => {
        const d = document.createElement('div');
        d.className = 'gnode pnode';
        d.dataset.ref = 'profile:' + pr.id;
        d.style.setProperty('--nc', profileColor(pr.id));
        d.innerHTML = `<span class="pnode-bar"></span><div class="pnode-main">
          <div class="pnode-name">${pr.name}${pr.isSystem ? '<span class="badge sys" style="margin-left:7px">SYS</span>' : ''}</div>
          <div class="pnode-sub mono">${pr.permissions.filter(x => permissions[x] && !permissions[x].orphan).length} permissions</div></div>`;
        d.addEventListener('mouseenter', () => { hoverProfile = pr.id; highlight(); });
        d.addEventListener('mouseleave', () => { hoverProfile = null; highlight(); });
        colProfiles.appendChild(d);
      });
      // col 3
      colPerms.innerHTML = perms.length ? '' : '<div class="gcol-empty mono">no grants — 403 on every protected route</div>';
      const wrap = document.createElement('div'); wrap.className = 'gperm-list';
      perms.forEach(({ perm }) => {
        const d = document.createElement('div');
        d.className = 'gnode knode';
        d.dataset.ref = 'perm:' + perm.id;
        const method = methodFromCode(perm.code);
        d.innerHTML = `<span class="knode-method mono" style="color:${METHOD_COLOR[method]};border-color:${METHOD_COLOR[method]}">${method}</span>
          <div class="knode-main"><div class="knode-code mono">${perm.code}</div><div class="knode-route mono">${routeFor(perm.code)}</div></div>`;
        d.addEventListener('mouseenter', () => { hoverPerm = perm.id; highlight(); });
        d.addEventListener('mouseleave', () => { hoverPerm = null; highlight(); });
        wrap.appendChild(d);
      });
      if (perms.length) colPerms.appendChild(wrap);
      // refs
      Object.keys(nodeRefs).forEach(k => delete nodeRefs[k]);
      $$('[data-ref]', canvas).forEach(el => nodeRefs[el.dataset.ref] = el);
      // summary
      if (summary.p) summary.p.textContent = prs.length;
      if (summary.e) summary.e.textContent = perms.length;
      if (summary.r) summary.r.textContent = revoked.length;
      drawEdges();
      highlight();
    }

    function currentEdges() {
      const { prs, perms } = resolve(current);
      const e = [];
      prs.forEach(pr => e.push({ from: 'user', to: 'profile:' + pr.id, profile: pr.id, color: profileColor(pr.id) }));
      perms.forEach(({ perm, from }) => from.forEach(pid => e.push({ from: 'profile:' + pid, to: 'perm:' + perm.id, profile: pid, perm: perm.id, color: profileColor(pid) })));
      return e;
    }

    function drawEdges() {
      svg.innerHTML = '';
      const wr = canvas.getBoundingClientRect();
      currentEdges().forEach(edge => {
        const a = nodeRefs[edge.from], b = nodeRefs[edge.to];
        if (!a || !b) return;
        const ar = a.getBoundingClientRect(), br = b.getBoundingClientRect();
        const x1 = ar.right - wr.left, y1 = ar.top + ar.height / 2 - wr.top;
        const x2 = br.left - wr.left, y2 = br.top + br.height / 2 - wr.top;
        const dx = Math.max(34, (x2 - x1) * 0.5);
        const d = `M${x1},${y1} C${x1 + dx},${y1} ${x2 - dx},${y2} ${x2},${y2}`;
        const g = document.createElementNS(NS, 'g');
        g.setAttribute('data-profile', edge.profile || '');
        g.setAttribute('data-perm', edge.perm || '');
        g.appendChild(svgPath(d, 'edge-base', edge.color));
        g.appendChild(svgPath(d, 'edge-flow', edge.color));
        svg.appendChild(g);
      });
    }

    function highlight() {
      const { perms } = resolve(current);
      $$('g', svg).forEach(g => {
        const prof = g.getAttribute('data-profile');
        const perm = g.getAttribute('data-perm');
        let on = true;
        if (hoverProfile) on = prof === hoverProfile;
        else if (hoverPerm) {
          const entry = perms.find(p => p.perm.id === hoverPerm);
          on = perm === hoverPerm || (entry && entry.from.includes(prof) && !perm);
        }
        g.classList.toggle('edge-on', on);
        g.classList.toggle('edge-off', !on);
        $('.edge-flow', g).style.display = on ? '' : 'none';
      });
      $$('.pnode', colProfiles).forEach(n => {
        const pid = n.dataset.ref.split(':')[1];
        let dim = false;
        if (hoverProfile && hoverProfile !== pid) dim = true;
        if (hoverPerm) { const e = perms.find(p => p.perm.id === hoverPerm); dim = !(e && e.from.includes(pid)); }
        n.classList.toggle('dim', dim);
        n.classList.toggle('hot', hoverProfile === pid);
      });
      $$('.knode', colPerms).forEach(n => {
        const permId = n.dataset.ref.split(':')[1];
        const e = perms.find(p => p.perm.id === permId);
        let dim = false;
        if (hoverProfile) dim = !(e && e.from.includes(hoverProfile));
        if (hoverPerm && hoverPerm !== permId) dim = true;
        n.classList.toggle('dim', dim);
        n.classList.toggle('hot', hoverPerm === permId);
      });
    }

    function setCache(state) {
      if (!cacheCard) return;
      cacheCard.classList.remove('hit', 'miss', 'invalidate');
      cacheCard.classList.add(state);
      const { u, perms } = resolve(current);
      const key = $('[data-cache-key]', cacheCard);
      const st = $('[data-cache-state]', cacheCard);
      if (key) key.textContent = 'user:permissions:' + u.id.slice(0, 8) + '…';
      if (st) {
        if (state === 'hit') st.innerHTML = `<span class="d ok"></span>CACHE HIT · ${perms.length} permissions`;
        if (state === 'miss') st.innerHTML = `<span class="d warn"></span>MISS → DB → repopulated`;
        if (state === 'invalidate') st.innerHTML = `<span class="d danger"></span>UserPermissionsChanged → INVALIDATE`;
      }
    }

    function renderAll() { renderRail(); renderGraph(); }

    // actions
    const revokeBtn = $('[data-graph-revoke]', root);
    const resetBtn = $('[data-graph-reset]', root);
    if (revokeBtn) revokeBtn.addEventListener('click', () => {
      const { perms } = resolve(current);
      if (!perms.length) return;
      const target = perms.find(p => p.perm.code === 'Profiles.Delete') || perms[perms.length - 1];
      setCache('invalidate');
      setTimeout(() => { revoked.push(target.perm.id); renderGraph(); setCache('miss'); setTimeout(() => setCache('hit'), 1000); }, 650);
    });
    if (resetBtn) resetBtn.addEventListener('click', () => { revoked = []; renderGraph(); setCache('miss'); setTimeout(() => setCache('hit'), 800); });

    window.addEventListener('resize', drawEdges);
    renderAll();
    setCache('hit');
    setTimeout(drawEdges, 80);
  }

  document.addEventListener('DOMContentLoaded', () => {
    initReveal(); initLogin(); initMatrix(); initTrace(); initGraph();
  });
})();
