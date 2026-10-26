/* WinLabeler site behaviour: code highlighter, copy buttons, burger menu, docs sections. */
(function () {
  'use strict';
  document.documentElement.classList.add('js');

  /* ---------- syntax highlighter ---------- */
  var STR = /"(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*'/.source;
  var NUM = /\b\d+(?:\.\d+)?\b/.source;
  var LANGS = {
    bash: [
      ['c', /#.*/], ['s', new RegExp(STR)], ['p', /\$\{?\w+\}?/],
      ['o', /(?:^|\s)--?[A-Za-z][\w-]*(?:=\S*)?/], ['k', /\b(?:cd|git|if|then|fi|for|do|done|export|echo)\b/],
      ['f', /\b(?:winget|dotnet|build\.bat|code|start|explorer)\b/], ['n', new RegExp(NUM)], ['x', /[|&;<>()=\\]/]
    ],
    bat: [
      ['c', /(?:^|\s)(?:rem\b|::).*/i], ['s', new RegExp(STR)], ['p', /%[^%\s]+%/],
      ['o', /(?:^|\s)\/[A-Za-z]+\b/], ['k', /\b(?:call|set|if|for|goto|echo|cd)\b/i],
      ['f', /\b(?:code|start|explorer|notepad|wt|git|npm|dotnet)\b/i], ['n', new RegExp(NUM)], ['x', /[|&<>()=]/]
    ],
    json: [
      ['p', /"(?:[^"\\\n]|\\.)*"(?=\s*:)/], ['s', /"(?:[^"\\\n]|\\.)*"/], ['n', /-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b/],
      ['k', /\b(?:true|false|null)\b/], ['x', /[{}\[\],:]/]
    ],
    js: [
      ['c', /\/\/.*|\/\*[\s\S]*?\*\//], ['s', new RegExp(STR + '|`(?:[^`\\\\]|\\\\.)*`')],
      ['k', /\b(?:const|let|var|function|return|if|else|for|while|new|await|async|import|from|export|class|try|catch|throw)\b/],
      ['n', new RegExp(NUM)], ['f', /\b[A-Za-z_$][\w$]*(?=\()/], ['x', /[{}()\[\];,.:=<>!+\-*\/&|?]/]
    ],
    py: [
      ['c', /#.*/], ['s', new RegExp(STR)], ['k', /\b(?:def|class|import|from|return|if|elif|else|for|while|in|not|and|or|None|True|False|with|as|try|except|raise|lambda)\b/],
      ['n', new RegExp(NUM)], ['f', /\b[A-Za-z_]\w*(?=\()/], ['x', /[{}()\[\],.:=<>!+\-*\/]/]
    ],
    yaml: [
      ['c', /#.*/], ['p', /^\s*-?\s*[\w.-]+(?=\s*:)/], ['s', new RegExp(STR)], ['n', new RegExp(NUM)],
      ['k', /\b(?:true|false|null|yes|no)\b/], ['x', /[:\-\[\]{},]/]
    ],
    kotlin: [
      ['c', /\/\/.*|\/\*[\s\S]*?\*\//], ['s', /"(?:[^"\\\n]|\\.)*"/], ['t', /@\w+/],
      ['k', /\b(?:val|var|fun|class|object|return|if|else|when|for|while|override|private|public|import|package|null|true|false)\b/],
      ['n', new RegExp(NUM)], ['f', /\b[A-Za-z_]\w*(?=\()/], ['x', /[{}()\[\];,.:=<>!+\-*\/]/]
    ],
    http: [
      ['c', /#.*/], ['k', /\b(?:GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)\b/], ['t', /HTTP\/\d(?:\.\d)?/],
      ['p', /^[A-Za-z-]+(?=:)/], ['s', new RegExp(STR)], ['n', new RegExp(NUM)], ['f', /\/[\w\/.\-?=&%{}]*/], ['x', /[:{}\[\],]/]
    ],
    text: []
  };
  var ALIAS = { sh: 'bash', shell: 'bash', cmd: 'bat', batch: 'bat', javascript: 'js', python: 'py', kt: 'kotlin', yml: 'yaml', plain: 'text' };
  var compiled = {};

  function esc(s) { return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }

  function compile(lang) {
    if (compiled[lang]) return compiled[lang];
    var rules = LANGS[lang] || [];
    var re = rules.length ? new RegExp(rules.map(function (r) { return '(' + r[1].source + ')'; }).join('|'), 'gm') : null;
    return (compiled[lang] = { re: re, rules: rules });
  }

  function highlight(src, lang) {
    var c = compile(lang);
    if (!c.re) return esc(src);
    var out = '', last = 0, m;
    c.re.lastIndex = 0;
    while ((m = c.re.exec(src))) {
      if (m[0] === '') { c.re.lastIndex++; continue; }
      var idx = 1;
      while (idx < m.length && m[idx] === undefined) idx++;
      var text = m[0], lead = '';
      // Rules that anchor on whitespace keep it outside the coloured span.
      var ws = /^\s+/.exec(text);
      if (ws && ws[0].length < text.length && c.rules[idx - 1][1].source.indexOf('(?:^|\\s)') === 0) { lead = ws[0]; text = text.slice(lead.length); }
      out += esc(src.slice(last, m.index)) + esc(lead) + '<span class="tok-' + c.rules[idx - 1][0] + '">' + esc(text) + '</span>';
      last = m.index + m[0].length;
    }
    return out + esc(src.slice(last));
  }

  function copyText(text, btn) {
    function done() {
      btn.classList.add('done');
      btn.lastChild.textContent = 'Copied';
      setTimeout(function () { btn.classList.remove('done'); btn.lastChild.textContent = 'Copy'; }, 1800);
    }
    function fallback() {
      var ta = document.createElement('textarea');
      ta.value = text; ta.setAttribute('readonly', ''); ta.style.cssText = 'position:fixed;opacity:0;top:0;left:0';
      document.body.appendChild(ta); ta.select();
      try { document.execCommand('copy'); done(); } catch (e) { /* ignore */ }
      document.body.removeChild(ta);
    }
    if (navigator.clipboard && window.isSecureContext) navigator.clipboard.writeText(text).then(done, fallback);
    else fallback();
  }

  function enhanceCode() {
    var blocks = document.querySelectorAll('pre > code[data-lang]');
    Array.prototype.forEach.call(blocks, function (code) {
      var pre = code.parentNode;
      var raw = code.textContent.replace(/^\n+|\s+$/g, '');
      var lang = (code.getAttribute('data-lang') || 'text').toLowerCase();
      lang = ALIAS[lang] || lang;
      try { code.innerHTML = highlight(raw, lang); } catch (e) { code.textContent = raw; }

      var wrap = document.createElement('div');
      wrap.className = 'code';
      var bar = document.createElement('div');
      bar.className = 'code-bar';
      bar.innerHTML = '<span class="code-dots" aria-hidden="true"><i></i><i></i><i></i></span>' +
        '<span class="code-title">' + esc(code.getAttribute('data-title') || lang) + '</span>' +
        '<button type="button" class="copy-btn" aria-label="Copy code"><svg class="icon" aria-hidden="true"><use href="#i-copy"/></svg><span>Copy</span></button>';
      bar.querySelector('button').addEventListener('click', function () { copyText(raw, this); });
      pre.parentNode.insertBefore(wrap, pre);
      wrap.appendChild(bar);
      wrap.appendChild(pre);
      pre.setAttribute('tabindex', '0');
    });
  }

  /* ---------- burger menu ---------- */
  var root = document.documentElement;
  var burger = document.querySelector('.burger');
  var menuLabel = 'menu';

  function setMenu(open) {
    if (!burger) return;
    root.classList.toggle('menu-open', open);
    burger.setAttribute('aria-expanded', String(open));
    burger.setAttribute('aria-label', open ? 'Close ' + menuLabel : 'Open ' + menuLabel);
    var use = burger.querySelector('use');
    if (use) use.setAttribute('href', open ? '#i-x' : '#i-menu');
  }
  window.WL_closeMenu = function () { setMenu(false); };

  function initMenu() {
    if (!burger) return;
    var target = document.getElementById(burger.getAttribute('aria-controls'));
    menuLabel = target && target.id === 'docs-nav' ? 'sections menu' : 'menu';
    setMenu(false);
    burger.addEventListener('click', function () { setMenu(!root.classList.contains('menu-open')); });
    if (target) target.addEventListener('click', function (e) { if (e.target.closest('a')) setMenu(false); });
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && root.classList.contains('menu-open')) { setMenu(false); burger.focus(); }
    });
    window.addEventListener('resize', function () { if (window.innerWidth > 820) setMenu(false); });
  }

  /* ---------- docs sections ---------- */
  function initDocs() {
    var sections = document.querySelectorAll('.doc-section');
    if (!sections.length) return;
    var links = document.querySelectorAll('.docs-nav a[data-section]');
    var siteName = document.title.split(' | ').pop();
    var first = sections[0].id;

    function exists(id) { return !!(id && document.getElementById(id) && document.getElementById(id).classList.contains('doc-section')); }

    function show(id, push) {
      if (!exists(id)) id = first;
      Array.prototype.forEach.call(sections, function (s) { s.classList.toggle('active', s.id === id); });
      Array.prototype.forEach.call(links, function (a) {
        if (a.getAttribute('data-section') === id) a.setAttribute('aria-current', 'true');
        else a.removeAttribute('aria-current');
      });
      var h1 = document.querySelector('#' + id + ' h1');
      if (h1) document.title = h1.textContent + ' | ' + siteName;
      if (push) history.pushState({ id: id }, '', '#' + id);
      window.scrollTo(0, 0);
      window.WL_closeMenu();
    }

    function fromHash() { return decodeURIComponent(location.hash.replace(/^#/, '')); }

    document.addEventListener('click', function (e) {
      var a = e.target.closest('a[data-section], [data-goto]');
      if (!a) return;
      e.preventDefault();
      show(a.getAttribute('data-section') || a.getAttribute('data-goto'), true);
    });
    window.addEventListener('popstate', function () { show(fromHash(), false); });
    show(fromHash(), false);
    // The browser's own hash-anchor scroll runs after this code.
    setTimeout(function () { window.scrollTo(0, 0); }, 0);
  }

  document.addEventListener('DOMContentLoaded', function () {
    enhanceCode();
    initMenu();
    initDocs();
  });
})();
