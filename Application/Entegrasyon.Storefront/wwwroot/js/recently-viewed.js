(function() {
    var STORAGE_KEY = 'recently_viewed';
    var MAX_ITEMS = 10;

    window.RecentlyViewed = {
        add: function(product) {
            var items = JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]');
            items = items.filter(function(p) { return p.id !== product.id; });
            items.unshift(product);
            if (items.length > MAX_ITEMS) items = items.slice(0, MAX_ITEMS);
            localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
        },
        get: function() {
            return JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]');
        },
        render: function(containerId) {
            var items = this.get();
            var container = document.getElementById(containerId);
            if (!container || items.length === 0) return;
            container.style.display = '';
            var grid = container.querySelector('[data-recently-grid]');
            if (!grid) return;
            grid.textContent = '';
            items.forEach(function(p) {
                var a = document.createElement('a');
                a.href = '/urun/' + p.slug;
                a.className = 'block min-w-[160px] max-w-[160px] shrink-0';

                var img = document.createElement('img');
                img.src = p.image || '/images/placeholder.png';
                img.alt = p.title;
                img.className = 'w-full h-32 object-cover rounded';
                img.loading = 'lazy';
                a.appendChild(img);

                var title = document.createElement('p');
                title.className = 'text-xs mt-1 text-gray-700 truncate';
                title.textContent = p.title;
                a.appendChild(title);

                var price = document.createElement('p');
                price.className = 'text-xs font-bold text-primary';
                price.textContent = p.price + ' TL';
                a.appendChild(price);

                grid.appendChild(a);
            });
        }
    };
})();
