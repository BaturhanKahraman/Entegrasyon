/* Zekids Bebe — Kategori listeleme: filtre, sıralama, görünüm, mobil drawer, sample ürün grid. */
(function () {
    'use strict';

    var IMG = function (id, w) { return 'https://images.unsplash.com/photo-' + id + '?auto=format&fit=crop&w=' + (w || 500) + '&q=80'; };
    function toast(m) { if (window.toast) window.toast(m); }

    /* ---- filter options ---- */
    var AGES = ['0-3 ay', '3-6 ay', '6-12 ay', '1-2 yaş', '2-4 yaş', '4-6 yaş', '6-8 yaş', '8-10 yaş', '10-12 yaş', '12-14 yaş'];
    var KSIZES = ['74', '80', '86', '92', '98', '104', '110', '116', '122', '128', '134'];
    var SEASONS = ['İlkbahar', 'Yaz', 'Sonbahar', 'Kış'];
    var COLORS = [
        { name: 'Pudra Pembe',  hex: '#FFD0DC' },
        { name: 'Bebek Mavisi', hex: '#B8E0F0' },
        { name: 'Krem',         hex: '#F3E9D8' },
        { name: 'Beyaz',        hex: '#FFFFFF' },
        { name: 'Açık Gri',     hex: '#D9DEE3' },
        { name: 'Sarı',         hex: '#FFE39E' },
        { name: 'Mint Yeşili',  hex: '#C7EBD6' },
        { name: 'Lavanta',      hex: '#E0D4F0' },
    ];
    var BRANDS = [
        { name: 'Mavi Kids', n: 24 }, { name: 'LC Waikiki', n: 18 }, { name: 'Koton Kids', n: 15 },
        { name: 'DeFacto', n: 12 }, { name: 'Panço', n: 9 }, { name: 'Zeyland', n: 7 },
        { name: 'Tuc Tuc', n: 6 }, { name: 'Mothercare', n: 5 },
    ];
    var SORT_OPTIONS = ['Önerilen', 'Yeniden Eskiye', 'Fiyat (artan)', 'Fiyat (azalan)', 'En Çok Satan', 'En Yüksek Puanlı', 'En Çok İndirim'];

    /* ---- state ---- */
    var F = {
        age: new Set(['2-4 yaş']),
        size: new Set(),
        season: new Set(),
        color: new Set(['Pudra Pembe']),
        gender: 'Hepsi',
        brands: new Set(),
        inStock: true, discounted: false, isNew: false,
        priceMin: 0, priceMax: 1000,
    };
    var currentSort = 'Önerilen';

    /* ---- helpers ---- */
    function esc(s) { return String(s).replace(/'/g, "\\'"); }

    function chipBtn(group, val, active) {
        return '<button type="button" data-toggle-val data-group="' + group + '" data-val="' + esc(val) + '" class="px-3 py-1.5 rounded-full border text-sm transition ' + (active ? 'bg-primary text-white border-primary' : 'border-cream-300 text-charcoal hover:border-primary') + '">' + val + '</button>';
    }

    function groupWrap(title, inner, open) {
        return '<details ' + (open !== false ? 'open' : '') + ' class="border-b border-cream-300 py-4 first:pt-0">' +
            '<summary class="flex items-center justify-between cursor-pointer font-semibold text-charcoal text-sm">' + title +
            '<svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" class="text-muted" aria-hidden="true"><path d="m6 9 6 6 6-6"/></svg>' +
            '</summary><div class="mt-3.5">' + inner + '</div></details>';
    }

    function activeChipsHTML() {
        var chips = [];
        F.age.forEach(function (v) { chips.push(['age', v]); });
        F.size.forEach(function (v) { chips.push(['size', 'Beden ' + v]); });
        F.season.forEach(function (v) { chips.push(['season', v]); });
        F.color.forEach(function (v) { chips.push(['color', v]); });
        F.brands.forEach(function (v) { chips.push(['brands', v]); });
        if (F.gender !== 'Hepsi') chips.push(['gender', F.gender]);
        if (F.discounted) chips.push(['discounted', 'İndirimliler']);
        if (F.isNew) chips.push(['isNew', 'Yeni gelenler']);
        if (chips.length === 0) return '';
        return '<div class="flex flex-wrap gap-2 mb-4 pb-4 border-b border-cream-300">' +
            chips.map(function (c) {
                var g = c[0], label = c[1];
                var val = label.replace(/^Beden /, '');
                return '<button type="button" data-remove-chip data-group="' + g + '" data-val="' + esc(val) + '" class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-primary/10 text-primary text-xs font-medium hover:bg-primary/20 transition">' + label +
                    '<svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" aria-hidden="true"><path d="M18 6 6 18M6 6l12 12"/></svg></button>';
            }).join('') + '</div>';
    }

    function filtersInner() {
        var age = '<div class="flex flex-wrap gap-2">' + AGES.map(function (a) { return chipBtn('age', a, F.age.has(a)); }).join('') + '</div>';
        var size = '<div class="flex flex-wrap gap-2">' + KSIZES.map(function (s) { return chipBtn('size', s, F.size.has(s)); }).join('') + '</div>';
        var gender = '<div class="flex flex-wrap gap-2">' + ['Hepsi', 'Kız', 'Erkek', 'Unisex'].map(function (g) {
            return '<button type="button" data-set-gender data-val="' + g + '" class="px-3 py-1.5 rounded-full border text-sm transition ' + (F.gender === g ? 'bg-primary text-white border-primary' : 'border-cream-300 text-charcoal hover:border-primary') + '">' + g + '</button>';
        }).join('') + '</div>';
        var season = '<div class="flex flex-wrap gap-2">' + SEASONS.map(function (s) { return chipBtn('season', s, F.season.has(s)); }).join('') + '</div>';
        var color = '<div class="flex flex-wrap gap-3">' + COLORS.map(function (c) {
            return '<button type="button" data-toggle-val data-group="color" data-val="' + esc(c.name) + '" title="' + c.name + '" aria-label="' + c.name + '" class="w-8 h-8 rounded-full border border-cream-300 transition ' + (F.color.has(c.name) ? 'ring-2 ring-primary ring-offset-2' : '') + '" style="background:' + c.hex + '"></button>';
        }).join('') + '</div>';
        var price = '<div class="priceBox">' +
            '<div class="flex items-center gap-3">' +
            '<input type="number" class="numMin w-full px-3 py-2 rounded-xl border border-cream-300 text-sm outline-none focus:border-primary" value="' + F.priceMin + '" min="0" max="1000" data-price-num="min" aria-label="En düşük fiyat" />' +
            '<span class="text-muted">-</span>' +
            '<input type="number" class="numMax w-full px-3 py-2 rounded-xl border border-cream-300 text-sm outline-none focus:border-primary" value="' + F.priceMax + '" min="0" max="1000" data-price-num="max" aria-label="En yüksek fiyat" />' +
            '</div>' +
            '<div class="range-dual mt-2">' +
            '<div class="range-track"></div>' +
            '<div class="range-fill" style="left:' + (F.priceMin / 10) + '%;width:' + ((F.priceMax - F.priceMin) / 10) + '%"></div>' +
            '<input type="range" class="rMin" min="0" max="1000" step="10" value="' + F.priceMin + '" data-price-range="min" aria-label="En düşük fiyat kaydırıcı" />' +
            '<input type="range" class="rMax" min="0" max="1000" step="10" value="' + F.priceMax + '" data-price-range="max" aria-label="En yüksek fiyat kaydırıcı" />' +
            '</div></div>';
        var brands = '<div>' +
            '<input type="text" placeholder="Marka ara…" data-brand-filter class="w-full px-3 py-2 mb-2.5 rounded-xl border border-cream-300 text-sm outline-none focus:border-primary" aria-label="Marka ara" />' +
            '<div class="brandList max-h-48 overflow-y-auto space-y-1 pr-1">' +
            BRANDS.map(function (b) {
                return '<label data-name="' + b.name.toLowerCase() + '" class="flex items-center gap-2.5 py-1.5 cursor-pointer text-sm">' +
                    '<input type="checkbox" ' + (F.brands.has(b.name) ? 'checked' : '') + ' data-toggle-val data-group="brands" data-val="' + esc(b.name) + '" class="w-4 h-4 rounded border-cream-300 accent-[#FF8FB1]" />' +
                    '<span class="text-charcoal flex-1">' + b.name + '</span>' +
                    '<span class="text-muted text-xs">(' + b.n + ')</span></label>';
            }).join('') + '</div></div>';
        var status = '<div class="space-y-3">' +
            statusRow('inStock', 'Sadece stokta olanlar') +
            statusRow('discounted', 'İndirimliler') +
            statusRow('isNew', 'Yeni gelenler') +
            '</div>';

        return activeChipsHTML() +
            groupWrap('Yaş Aralığı', age) +
            groupWrap('Beden', size) +
            groupWrap('Cinsiyet', gender) +
            groupWrap('Mevsim', season) +
            groupWrap('Renk', color) +
            groupWrap('Fiyat Aralığı', price) +
            groupWrap('Marka', brands) +
            '<div class="pt-4">' + status + '</div>';
    }

    function statusRow(key, label) {
        return '<button type="button" data-toggle-status data-key="' + key + '" class="w-full flex items-center justify-between text-sm text-charcoal">' +
            '<span>' + label + '</span>' +
            '<span class="switch" data-on="' + (F[key] ? 1 : 0) + '"></span></button>';
    }

    function renderFilters() {
        var html = filtersInner();
        var a = document.getElementById('sidebarFilters');
        var b = document.getElementById('drawerFilters');
        if (a) a.innerHTML = html;
        if (b) b.innerHTML = html;
        bindFilterEvents();
    }

    function bindFilterEvents() {
        document.querySelectorAll('[data-toggle-val]').forEach(function (el) {
            el.addEventListener(el.tagName === 'INPUT' ? 'change' : 'click', function () {
                var set = F[el.dataset.group];
                var val = el.dataset.val;
                if (set instanceof Set) {
                    if (set.has(val)) set.delete(val); else set.add(val);
                }
                renderFilters(); bump();
            });
        });
        document.querySelectorAll('[data-set-gender]').forEach(function (el) {
            el.addEventListener('click', function () { F.gender = el.dataset.val; renderFilters(); bump(); });
        });
        document.querySelectorAll('[data-toggle-status]').forEach(function (el) {
            el.addEventListener('click', function () { F[el.dataset.key] = !F[el.dataset.key]; renderFilters(); bump(); });
        });
        document.querySelectorAll('[data-remove-chip]').forEach(function (el) {
            el.addEventListener('click', function () {
                var g = el.dataset.group, val = el.dataset.val;
                if (g === 'gender') F.gender = 'Hepsi';
                else if (g === 'discounted' || g === 'isNew') F[g] = false;
                else F[g].delete(val);
                renderFilters(); bump();
            });
        });
        document.querySelectorAll('[data-price-range]').forEach(function (el) {
            el.addEventListener('input', function () { priceFromRange(el, el.dataset.priceRange); });
        });
        document.querySelectorAll('[data-price-num]').forEach(function (el) {
            el.addEventListener('input', function () { priceFromNum(el, el.dataset.priceNum); });
        });
        document.querySelectorAll('[data-brand-filter]').forEach(function (el) {
            el.addEventListener('input', function () { filterBrandList(el); });
        });
    }

    document.querySelectorAll('[data-clear-filters]').forEach(function (b) {
        b.addEventListener('click', function () {
            F.age.clear(); F.size.clear(); F.season.clear(); F.color.clear(); F.brands.clear();
            F.gender = 'Hepsi'; F.inStock = true; F.discounted = false; F.isNew = false;
            F.priceMin = 0; F.priceMax = 1000;
            renderFilters(); bump();
            toast('Filtreler temizlendi');
        });
    });

    function bump() {
        var g = document.getElementById('catGrid');
        if (!g) return;
        g.style.opacity = '0.35';
        setTimeout(function () { g.style.opacity = '1'; }, 260);
    }

    /* ---- price slider ---- */
    function priceFromRange(input, which) {
        var box = input.closest('.priceBox');
        var mn = +box.querySelector('.rMin').value;
        var mx = +box.querySelector('.rMax').value;
        if (mn > mx) {
            if (which === 'min') { mn = mx; box.querySelector('.rMin').value = mn; }
            else { mx = mn; box.querySelector('.rMax').value = mx; }
        }
        F.priceMin = mn; F.priceMax = mx;
        box.querySelector('.numMin').value = mn;
        box.querySelector('.numMax').value = mx;
        var fill = box.querySelector('.range-fill');
        fill.style.left = (mn / 10) + '%';
        fill.style.width = ((mx - mn) / 10) + '%';
    }
    function priceFromNum(input, which) {
        var box = input.closest('.priceBox');
        var mn = Math.max(0, Math.min(1000, +box.querySelector('.numMin').value || 0));
        var mx = Math.max(0, Math.min(1000, +box.querySelector('.numMax').value || 1000));
        F.priceMin = mn; F.priceMax = mx;
        box.querySelector('.rMin').value = mn;
        box.querySelector('.rMax').value = mx;
        var fill = box.querySelector('.range-fill');
        fill.style.left = (Math.min(mn, mx) / 10) + '%';
        fill.style.width = (Math.abs(mx - mn) / 10) + '%';
    }
    function filterBrandList(input) {
        var q = input.value.toLowerCase();
        var box = input.closest('div');
        box.querySelectorAll('.brandList label').forEach(function (l) {
            l.style.display = l.dataset.name.indexOf(q) >= 0 ? '' : 'none';
        });
    }

    /* ---- sort ---- */
    function renderSortMenu() {
        var menu = document.getElementById('sortMenu');
        var drawer = document.getElementById('drawerSort');
        if (menu) {
            menu.innerHTML = SORT_OPTIONS.map(function (o) {
                return '<button type="button" data-sort-select data-val="' + esc(o) + '" class="w-full text-left px-4 py-2 text-sm hover:bg-cream transition ' + (o === currentSort ? 'text-primary font-semibold' : 'text-charcoal') + '">' + o + '</button>';
            }).join('');
        }
        if (drawer) {
            drawer.innerHTML = SORT_OPTIONS.map(function (o) {
                return '<button type="button" data-sort-select data-val="' + esc(o) + '" class="px-3 py-1.5 rounded-full border text-sm transition ' + (o === currentSort ? 'bg-primary text-white border-primary' : 'border-cream-300 text-charcoal') + '">' + o + '</button>';
            }).join('');
        }
        document.querySelectorAll('[data-sort-select]').forEach(function (b) {
            b.addEventListener('click', function () {
                currentSort = b.dataset.val;
                document.getElementById('sortLabel').textContent = currentSort;
                document.getElementById('sortMenu').classList.add('hidden');
                renderSortMenu(); bump();
            });
        });
    }
    var sortBtn = document.querySelector('[data-sort-toggle]');
    if (sortBtn) {
        sortBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            document.getElementById('sortMenu').classList.toggle('hidden');
        });
    }
    document.addEventListener('click', function (e) {
        var menu = document.getElementById('sortMenu');
        var btn = document.getElementById('sortBtn');
        if (menu && !menu.classList.contains('hidden') && !menu.contains(e.target) && (!btn || !btn.contains(e.target))) {
            menu.classList.add('hidden');
        }
    });

    /* ---- view cols ---- */
    function setCols(n) {
        var g = document.getElementById('catGrid');
        if (!g) return;
        g.className = 'grid-fade grid grid-cols-2 sm:grid-cols-3 gap-4 sm:gap-6 lg:grid-cols-' + n;
        document.querySelectorAll('[data-cols]').forEach(function (b) {
            var on = +b.dataset.cols === n;
            b.classList.toggle('bg-cream', on);
            b.classList.toggle('text-charcoal', on);
            b.classList.toggle('text-muted', !on);
        });
    }
    document.querySelectorAll('[data-cols]').forEach(function (b) {
        b.addEventListener('click', function () { setCols(+b.dataset.cols); });
    });

    /* ---- pagination (mock) ---- */
    document.querySelectorAll('[data-page]').forEach(function (b) {
        b.addEventListener('click', function () {
            bump();
            window.scrollTo({ top: 0, behavior: 'smooth' });
            toast('Sayfa ' + b.dataset.page);
        });
    });

    /* ---- mobile drawer ---- */
    function openFilterDrawer() {
        var d = document.getElementById('filterDrawer');
        if (!d) return;
        d.classList.remove('hidden');
        var sheet = document.getElementById('filterSheet');
        if (sheet) sheet.classList.add('sheet-up');
        document.body.style.overflow = 'hidden';
    }
    function closeFilterDrawer() {
        var d = document.getElementById('filterDrawer');
        if (!d) return;
        d.classList.add('hidden');
        var sheet = document.getElementById('filterSheet');
        if (sheet) sheet.classList.remove('sheet-up');
        document.body.style.overflow = '';
    }
    document.querySelectorAll('[data-filter-drawer-open]').forEach(function (b) { b.addEventListener('click', openFilterDrawer); });
    document.querySelectorAll('[data-filter-drawer-close]').forEach(function (b) { b.addEventListener('click', closeFilterDrawer); });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            var sm = document.getElementById('sortMenu');
            if (sm) sm.classList.add('hidden');
            closeFilterDrawer();
        }
    });

    /* ---- product card ---- */
    function badgeHtml(type) {
        if (type === 'new') return '<span class="px-2.5 py-1 rounded-full bg-primary text-white text-[11px] font-semibold">Yeni</span>';
        if (type === 'best') return '<span class="px-2.5 py-1 rounded-full bg-accent text-charcoal text-[11px] font-semibold">Çok Satan</span>';
        if (type === 'sale') return '<span class="px-2.5 py-1 rounded-full bg-success text-white text-[11px] font-semibold">İndirim</span>';
        return '';
    }
    function discountPct(p) { return p.old ? Math.round((1 - p.price / p.old) * 100) : null; }
    function productCard(p) {
        var pct = discountPct(p);
        var dpct = pct ? '<span class="px-2.5 py-1 rounded-full bg-success text-white text-[11px] font-semibold">%' + pct + '</span>' : '';
        var oldHtml = p.old ? '<span class="text-sm text-muted line-through">' + p.old + '₺</span>' : '';
        return '<div class="group snap-start">' +
            '<a href="#" class="block" aria-label="' + p.name + ', ' + p.price + '₺">' +
            '<div class="ph relative aspect-[4/5] rounded-2xl ' + p.tint + ' overflow-hidden" data-ph="ürün görseli">' +
            '<img class="photo" loading="lazy" alt="' + p.name + '" src="' + IMG(p.img, 500) + '" />' +
            '<div class="absolute top-3 left-3 flex flex-col gap-1.5 items-start">' + badgeHtml(p.badge) + dpct + '</div>' +
            '<button type="button" data-wishlist-toggle data-on="0" aria-label="Favorilere ekle" class="absolute top-3 right-3 w-9 h-9 rounded-full bg-white/90 backdrop-blur flex items-center justify-center text-charcoal hover:text-primary transition shadow-sm">' +
            '<svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M19 14c1.5-1.5 3-3.2 3-5.5A4.5 4.5 0 0 0 12 5.5 4.5 4.5 0 0 0 2 8.5C2 10.8 3.5 12.5 5 14l7 7Z"/></svg>' +
            '</button></div></a>' +
            '<div class="mt-3"><p class="text-xs text-muted">' + p.brand + '</p>' +
            '<a href="#" class="block font-medium text-charcoal line-clamp-2 leading-snug mt-0.5 hover:text-primary transition">' + p.name + '</a>' +
            '<span class="inline-block mt-2 px-2 py-0.5 rounded-md bg-cream text-[11px] text-muted">' + p.sizes + '</span>' +
            '<div class="mt-2 flex items-baseline gap-2">' + oldHtml + '<span class="text-lg font-semibold text-primary">' + p.price + '₺</span></div>' +
            '</div></div>';
    }

    /* ---- catalog sample (24 elbise) ---- */
    var DIMG = [
        '1620774760711-caa4c94d683a', '1518831959646-742c3a14ebf7', '1560506840-ec148e82a604',
        '1604482858862-1db908a653e4', '1590995385808-7eb142fde16a', '1566454544259-f4b94c3d758c',
    ];
    var DNAMES = ['Çiçekli Yazlık Elbise', 'Fırfırlı Tül Elbise', 'Kareli Piknik Elbisesi', 'Dantel Yaka Elbise', 'Düğmeli Salopet Elbise', 'Kolsuz Pamuk Elbise', 'Nakışlı Bahar Elbisesi', 'Tütülü Parti Elbisesi', 'Puantiyeli Günlük Elbise', 'Volanlı Şifon Elbise', 'Kruvaze Keten Elbise', 'Çilek Desenli Elbise'];
    var DBRANDS = ['Mavi Kids', 'LC Waikiki', 'Koton Kids', 'DeFacto', 'Panço', 'Zeyland'];
    var DSIZES = ['2-4 yaş', '3-6 yaş', '1-5 yaş', '4-8 yaş', '2-7 yaş', '0-3 yaş'];
    var DTINTS = ['bg-primary/20', 'bg-secondary/25', 'bg-accent/25', 'bg-secondary/20', 'bg-primary/15', 'bg-accent/30'];
    var DBADGES = ['new', 'sale', 'best', ''];
    var CAT = [];
    for (var i = 0; i < 24; i++) {
        var price = 149 + ((i * 37) % 380);
        var hasOld = i % 3 === 0;
        CAT.push({
            name: DNAMES[i % DNAMES.length],
            brand: DBRANDS[i % DBRANDS.length],
            price: price,
            old: hasOld ? price + 90 + (i % 4) * 30 : null,
            sizes: DSIZES[i % DSIZES.length],
            badge: DBADGES[i % DBADGES.length],
            tint: DTINTS[i % DTINTS.length],
            img: DIMG[i % DIMG.length],
        });
    }

    /* ---- init ---- */
    renderFilters();
    renderSortMenu();
    setCols(4);
    var grid = document.getElementById('catGrid');
    if (grid) grid.innerHTML = CAT.map(productCard).join('');
    if (window.bindWishlistToggles) window.bindWishlistToggles();
})();
