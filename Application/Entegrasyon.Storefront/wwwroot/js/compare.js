(function() {
    var STORAGE_KEY = 'compare_products';
    var MAX_ITEMS = 4;

    window.ProductCompare = {
        get: function() {
            return JSON.parse(localStorage.getItem(STORAGE_KEY) || '[]');
        },

        add: function(product) {
            var items = this.get();
            if (items.some(function(p) { return p.id === product.id; })) return false;
            if (items.length >= MAX_ITEMS) {
                alert('En fazla ' + MAX_ITEMS + ' urun karsilastirabilirsiniz.');
                return false;
            }
            items.push(product);
            localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
            this.renderBar();
            return true;
        },

        remove: function(productId) {
            var items = this.get();
            items = items.filter(function(p) { return p.id !== productId; });
            localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
            this.renderBar();
        },

        toggle: function(product) {
            var items = this.get();
            if (items.some(function(p) { return p.id === product.id; })) {
                this.remove(product.id);
                return false;
            } else {
                return this.add(product);
            }
        },

        has: function(productId) {
            return this.get().some(function(p) { return p.id === productId; });
        },

        clear: function() {
            localStorage.removeItem(STORAGE_KEY);
            this.renderBar();
        },

        _createPillEl: function(item) {
            var pill = document.createElement('div');
            pill.className = 'flex items-center gap-1 bg-gray-100 rounded px-2 py-1 text-xs';

            var label = document.createElement('span');
            label.className = 'truncate max-w-[100px]';
            label.textContent = item.title;
            pill.appendChild(label);

            var removeBtn = document.createElement('button');
            removeBtn.className = 'text-gray-400 hover:text-red-500 ml-1';
            removeBtn.textContent = '\u00d7';
            removeBtn.addEventListener('click', function() {
                ProductCompare.remove(item.id);
            });
            pill.appendChild(removeBtn);

            return pill;
        },

        renderBar: function() {
            var items = this.get();
            var bar = document.getElementById('compare-bar');

            if (!bar) {
                bar = document.createElement('div');
                bar.id = 'compare-bar';
                bar.className = 'fixed bottom-0 left-0 right-0 bg-white border-t border-gray-200 shadow-lg z-50 transition-transform duration-300';
                document.body.appendChild(bar);
            }

            if (items.length === 0) {
                bar.style.transform = 'translateY(100%)';
                bar.textContent = '';
                return;
            }

            bar.style.transform = 'translateY(0)';
            bar.textContent = '';

            var container = document.createElement('div');
            container.className = 'max-w-7xl mx-auto px-4 py-3 flex items-center justify-between';

            var leftSection = document.createElement('div');
            leftSection.className = 'flex items-center gap-3';

            var countLabel = document.createElement('span');
            countLabel.className = 'text-sm font-medium text-gray-700';
            countLabel.textContent = items.length + ' urun karsilastiriliyor';
            leftSection.appendChild(countLabel);

            var pillsContainer = document.createElement('div');
            pillsContainer.className = 'flex gap-2';
            var self = this;
            items.forEach(function(item) {
                pillsContainer.appendChild(self._createPillEl(item));
            });
            leftSection.appendChild(pillsContainer);

            var rightSection = document.createElement('div');
            rightSection.className = 'flex items-center gap-2';

            var compareLink = document.createElement('a');
            compareLink.href = '/karsilastir?ids=' + items.map(function(p) { return p.id; }).join(',');
            compareLink.className = 'btn-primary px-4 py-2 text-sm';
            compareLink.textContent = 'Karsilastir';
            rightSection.appendChild(compareLink);

            var clearBtn = document.createElement('button');
            clearBtn.className = 'text-sm text-gray-500 hover:text-red-500 px-2';
            clearBtn.textContent = 'Temizle';
            clearBtn.addEventListener('click', function() {
                ProductCompare.clear();
            });
            rightSection.appendChild(clearBtn);

            container.appendChild(leftSection);
            container.appendChild(rightSection);
            bar.appendChild(container);

            // Update all compare buttons on page
            document.querySelectorAll('[data-compare-id]').forEach(function(btn) {
                var pid = btn.getAttribute('data-compare-id');
                var isActive = items.some(function(p) { return p.id === pid; });
                btn.classList.toggle('text-primary', isActive);
                btn.classList.toggle('text-gray-400', !isActive);
            });
        }
    };

    // Render bar on page load (karşılaştırma sayfasında gizli — orada zaten tam tablo var)
    document.addEventListener('DOMContentLoaded', function() {
        if (!document.querySelector('[data-compare-page]')) {
            ProductCompare.renderBar();
        }
    });

    // ===== Karşılaştırma sayfası (server-rendered /karsilastir?ids=...) =====
    // Sayfa server'da Model'den render edilir; ids URL'i tek doğruluk kaynağıdır.
    // Çıkar/temizle butonları ids listesini güncelleyip yeniden yükler; sepet butonu
    // (storefront genelindeki davranışla tutarlı) şimdilik toast gösterir.
    document.addEventListener('DOMContentLoaded', function() {
        var page = document.querySelector('[data-compare-page]');
        if (!page) return;

        function currentIds() {
            var raw = new URLSearchParams(window.location.search).get('ids');
            return raw ? raw.split(',').map(function(s) { return s.trim(); }).filter(Boolean) : [];
        }
        function goWith(ids) {
            window.location.href = ids.length ? '/karsilastir?ids=' + ids.join(',') : '/karsilastir';
        }

        page.querySelectorAll('[data-compare-remove]').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var id = btn.getAttribute('data-product-id');
                ProductCompare.remove(id);
                goWith(currentIds().filter(function(x) { return x !== id; }));
            });
        });

        page.querySelectorAll('[data-compare-clear]').forEach(function(btn) {
            btn.addEventListener('click', function() {
                ProductCompare.clear();
                window.location.href = '/karsilastir';
            });
        });

        page.querySelectorAll('[data-compare-add-cart]').forEach(function(btn) {
            btn.addEventListener('click', function() {
                var name = btn.getAttribute('data-product-name') || 'Ürün';
                if (window.toast) window.toast(name + ' sepete eklendi');
            });
        });
    });
})();
