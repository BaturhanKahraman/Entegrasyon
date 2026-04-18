(function () {
  'use strict';

  const DEBOUNCE_MS = 150;
  const MIN_QUERY_LEN = 2;

  let debounceTimer = null;
  let currentAbort = null;
  let activeIndex = -1;
  let flatHits = []; // [{url, el}, ...]

  const overlay = document.getElementById('cmdk-overlay');
  const input   = document.getElementById('cmdk-input');
  const results = document.getElementById('cmdk-results');

  if (!overlay || !input || !results) return;

  function open() {
    overlay.classList.add('is-open');
    input.value = '';
    showEmpty('En az 2 harf yazın…');
    activeIndex = -1;
    flatHits = [];
    setTimeout(() => input.focus(), 30);
  }

  function close() {
    overlay.classList.remove('is-open');
    if (currentAbort) { currentAbort.abort(); currentAbort = null; }
    clearTimeout(debounceTimer);
  }

  function isOpen() { return overlay.classList.contains('is-open'); }

  function clearResults() {
    while (results.firstChild) results.removeChild(results.firstChild);
  }

  function showEmpty(message) {
    clearResults();
    const div = document.createElement('div');
    div.className = 'cmdk-empty';
    div.textContent = message;
    results.appendChild(div);
  }

  function makeEl(tag, className, text) {
    const el = document.createElement(tag);
    if (className) el.className = className;
    if (text != null) el.textContent = text;
    return el;
  }

  function renderGroups(groups) {
    flatHits = [];
    activeIndex = -1;
    clearResults();

    if (!groups || groups.length === 0) {
      showEmpty('Sonuç bulunamadı.');
      return;
    }

    groups.forEach(g => {
      const label = makeEl('div', 'cmdk-group-label', g.label);
      results.appendChild(label);

      g.hits.forEach(h => {
        const hit = makeEl('div', 'cmdk-hit');
        hit.dataset.url = h.url;

        const iconWrap = makeEl('div', 'cmdk-hit-icon');
        const icon = makeEl('i', 'ti ' + (g.icon || ''));
        iconWrap.appendChild(icon);
        hit.appendChild(iconWrap);

        const body = makeEl('div', 'cmdk-hit-body');
        body.appendChild(makeEl('div', 'cmdk-hit-title', h.title));
        if (h.subtitle) body.appendChild(makeEl('div', 'cmdk-hit-subtitle', h.subtitle));
        hit.appendChild(body);

        if (h.badge) hit.appendChild(makeEl('span', 'cmdk-hit-badge', h.badge));

        results.appendChild(hit);

        const idx = flatHits.length;
        flatHits.push({ url: h.url, el: hit });

        hit.addEventListener('click', () => { window.location.href = hit.dataset.url; });
        hit.addEventListener('mouseenter', () => setActive(idx));
      });
    });

    if (flatHits.length > 0) setActive(0);
  }

  function setActive(idx) {
    flatHits.forEach((h, i) => h.el.classList.toggle('is-active', i === idx));
    activeIndex = idx;
    if (idx >= 0) flatHits[idx].el.scrollIntoView({ block: 'nearest' });
  }

  async function runSearch(q) {
    if (currentAbort) currentAbort.abort();
    currentAbort = new AbortController();
    try {
      const url = '/_/search?q=' + encodeURIComponent(q) + '&limit=5';
      const resp = await fetch(url, { signal: currentAbort.signal, credentials: 'same-origin' });
      if (!resp.ok) return;
      const data = await resp.json();
      renderGroups(data.groups);
    } catch (e) {
      if (e.name !== 'AbortError') console.error(e);
    }
  }

  input.addEventListener('input', () => {
    const q = input.value.trim();
    clearTimeout(debounceTimer);
    if (q.length < MIN_QUERY_LEN) {
      showEmpty('En az 2 harf yazın…');
      flatHits = []; activeIndex = -1;
      return;
    }
    debounceTimer = setTimeout(() => runSearch(q), DEBOUNCE_MS);
  });

  input.addEventListener('keydown', (e) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      if (flatHits.length) setActive((activeIndex + 1) % flatHits.length);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      if (flatHits.length) setActive((activeIndex - 1 + flatHits.length) % flatHits.length);
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (activeIndex >= 0) window.location.href = flatHits[activeIndex].url;
    } else if (e.key === 'Escape') {
      close();
    }
  });

  overlay.addEventListener('click', (e) => { if (e.target === overlay) close(); });

  document.addEventListener('keydown', (e) => {
    const isToggle = (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k';
    if (isToggle) { e.preventDefault(); isOpen() ? close() : open(); }
    else if (e.key === 'Escape' && isOpen()) { close(); }
  });

  document.querySelectorAll('[data-cmdk-open]').forEach(el => {
    el.addEventListener('click', (ev) => { ev.preventDefault(); open(); });
  });
})();
