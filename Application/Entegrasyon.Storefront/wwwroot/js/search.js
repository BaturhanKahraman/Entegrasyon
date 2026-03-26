(function () {
    var HISTORY_KEY = 'search_history';
    var MAX_HISTORY = 10;

    function getSearchHistory() {
        return JSON.parse(localStorage.getItem(HISTORY_KEY) || '[]');
    }

    function addSearchHistory(query) {
        if (!query) return;
        var items = getSearchHistory();
        items = items.filter(function(q) { return q !== query; });
        items.unshift(query);
        if (items.length > MAX_HISTORY) items = items.slice(0, MAX_HISTORY);
        localStorage.setItem(HISTORY_KEY, JSON.stringify(items));
    }

    function renderDropdown(dropdown, items) {
        dropdown.textContent = '';
        if (!items.length) { dropdown.classList.add('hidden'); return; }
        items.forEach(function (item) {
            var a = document.createElement('a');
            a.href = item.url;
            a.className = 'flex items-center gap-3 px-4 py-2 hover:bg-gray-50 no-underline text-gray-800';

            if (item.imageUrl) {
                var img = document.createElement('img');
                img.src = item.imageUrl;
                img.className = 'w-8 h-8 object-cover rounded';
                img.alt = '';
                a.appendChild(img);
            } else {
                var icon = document.createElement('span');
                icon.className = 'w-8 h-8 flex items-center justify-center text-lg';
                if (item.type === 'history') {
                    icon.textContent = '\u{1F552}';
                } else if (item.type === 'popular') {
                    icon.textContent = '\u{1F525}';
                } else if (item.type === 'product') {
                    icon.textContent = '\u{1F6D2}';
                } else if (item.type === 'category') {
                    icon.textContent = '\u{1F4C2}';
                } else {
                    icon.textContent = '\u{1F3F7}';
                }
                a.appendChild(icon);
            }

            var text = document.createElement('span');
            text.className = 'text-sm';
            text.textContent = item.text;
            a.appendChild(text);

            if (item.type === 'history' || item.type === 'popular') {
                var badge = document.createElement('span');
                badge.className = 'ml-auto text-xs text-gray-400';
                badge.textContent = item.type === 'history' ? 'Son arama' : 'Populer';
                a.appendChild(badge);
            }

            dropdown.appendChild(a);
        });
        dropdown.classList.remove('hidden');
    }

    function showHistoryAndPopular(dropdown) {
        var historyItems = getSearchHistory().slice(0, 5).map(function(q) {
            return { text: q, url: '/arama?q=' + encodeURIComponent(q), type: 'history', imageUrl: null };
        });

        fetch('/api/arama/oneri?q=')
            .then(function(r) { return r.json(); })
            .then(function(popularItems) {
                var all = historyItems.concat(popularItems.slice(0, 5));
                renderDropdown(dropdown, all);
            })
            .catch(function() {
                renderDropdown(dropdown, historyItems);
            });
    }

    document.querySelectorAll('[data-search-input]').forEach(function (input) {
        var dropdown = document.createElement('div');
        dropdown.className = 'absolute top-full left-0 right-0 bg-white border border-gray-200 rounded-b-lg shadow-lg z-50 hidden max-h-96 overflow-y-auto';
        input.parentElement.style.position = 'relative';
        input.parentElement.appendChild(dropdown);

        var timer = null;
        input.addEventListener('input', function () {
            clearTimeout(timer);
            var q = input.value.trim();
            if (q.length < 2) {
                if (q.length === 0) {
                    showHistoryAndPopular(dropdown);
                } else {
                    dropdown.classList.add('hidden');
                }
                return;
            }
            timer = setTimeout(function () {
                fetch('/api/arama/oneri?q=' + encodeURIComponent(q))
                    .then(function (r) { return r.json(); })
                    .then(function (items) {
                        renderDropdown(dropdown, items);
                    });
            }, 300);
        });

        input.addEventListener('focus', function () {
            var q = input.value.trim();
            if (q.length < 2) {
                showHistoryAndPopular(dropdown);
            }
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                var q = input.value.trim();
                if (q) {
                    addSearchHistory(q);
                    window.location.href = '/arama?q=' + encodeURIComponent(q);
                }
            }
        });

        document.addEventListener('click', function (e) {
            if (!input.parentElement.contains(e.target)) dropdown.classList.add('hidden');
        });
    });
})();
