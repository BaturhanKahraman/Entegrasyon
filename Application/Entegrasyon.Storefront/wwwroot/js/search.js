(function () {
    document.querySelectorAll('[data-search-input]').forEach(function (input) {
        var dropdown = document.createElement('div');
        dropdown.className = 'absolute top-full left-0 right-0 bg-white border border-gray-200 rounded-b-lg shadow-lg z-50 hidden max-h-96 overflow-y-auto';
        input.parentElement.style.position = 'relative';
        input.parentElement.appendChild(dropdown);

        var timer = null;
        input.addEventListener('input', function () {
            clearTimeout(timer);
            var q = input.value.trim();
            if (q.length < 2) { dropdown.classList.add('hidden'); return; }
            timer = setTimeout(function () {
                fetch('/api/arama/oneri?q=' + encodeURIComponent(q))
                    .then(function (r) { return r.json(); })
                    .then(function (items) {
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
                                icon.textContent = item.type === 'product' ? '\u{1F6D2}' : item.type === 'category' ? '\u{1F4C2}' : '\u{1F3F7}';
                                a.appendChild(icon);
                            }

                            var text = document.createElement('span');
                            text.className = 'text-sm';
                            text.textContent = item.text;
                            a.appendChild(text);

                            dropdown.appendChild(a);
                        });
                        dropdown.classList.remove('hidden');
                    });
            }, 300);
        });

        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                var q = input.value.trim();
                if (q) window.location.href = '/arama?q=' + encodeURIComponent(q);
            }
        });

        document.addEventListener('click', function (e) {
            if (!input.parentElement.contains(e.target)) dropdown.classList.add('hidden');
        });
    });
})();
