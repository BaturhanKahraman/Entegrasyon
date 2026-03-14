window.LabelDesigner = (function () {
    let _dotNetRef = null;
    let _canvas = null;
    let _elements = [];
    let _selectedIndex = -1;
    let _zoom = 1.5;
    let _widthDots = 0;
    let _heightDots = 0;

    // Drag state
    let _dragging = false;
    let _dragStartX = 0;
    let _dragStartY = 0;
    let _dragElStartX = 0;
    let _dragElStartY = 0;

    // Resize state
    let _resizing = false;
    let _resizeHandle = '';
    let _resizeStartX = 0;
    let _resizeStartY = 0;
    let _resizeElStartW = 0;
    let _resizeElStartH = 0;
    let _resizeElStartX = 0;
    let _resizeElStartY = 0;

    function init(dotNetRef, canvasId, widthDots, heightDots, elements, zoom) {
        _dotNetRef = dotNetRef;
        _canvas = document.getElementById(canvasId);
        _widthDots = widthDots;
        _heightDots = heightDots;
        _elements = elements || [];
        _zoom = zoom || 1.5;
        _selectedIndex = -1;

        if (!_canvas) return;

        _canvas.style.width = widthDots + 'px';
        _canvas.style.height = heightDots + 'px';
        _canvas.style.position = 'relative';
        _canvas.style.border = '2px solid #ccc';
        _canvas.style.backgroundColor = '#fff';
        _canvas.style.overflow = 'hidden';
        _canvas.style.transform = 'scale(' + _zoom + ')';
        _canvas.style.transformOrigin = 'top left';

        _canvas.addEventListener('mousedown', onCanvasMouseDown);
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);

        render();
    }

    function render() {
        if (!_canvas) return;

        // Clear existing
        _canvas.innerHTML = '';

        _elements.forEach(function (el, idx) {
            var div = createElementDiv(el, idx);
            _canvas.appendChild(div);
        });
    }

    function createElementDiv(el, idx) {
        var div = document.createElement('div');
        div.dataset.index = idx;
        div.style.position = 'absolute';
        div.style.left = el.x + 'px';
        div.style.top = el.y + 'px';
        div.style.width = el.width + 'px';
        div.style.height = el.height + 'px';
        div.style.cursor = 'move';
        div.style.boxSizing = 'border-box';
        div.style.userSelect = 'none';
        div.style.overflow = 'hidden';

        if (idx === _selectedIndex) {
            div.style.outline = '2px dashed #1976D2';
            div.style.outlineOffset = '1px';
        }

        var content = renderContent(el);
        div.innerHTML = content;

        // Add resize handles if selected
        if (idx === _selectedIndex) {
            addResizeHandles(div);
        }

        return div;
    }

    function renderContent(el) {
        var fontSize = el.fontSize || 24;
        var fontWeight = el.bold ? 'bold' : 'normal';
        // Ortak stil: container'ı tamamen doldur, içeriği ortala
        var base = 'width:100%;height:100%;display:flex;align-items:center;font-family:Arial,sans-serif;box-sizing:border-box;overflow:hidden;';

        switch (el.elementType) {
            case 'Title':
                return '<div style="' + base + 'font-size:' + fontSize + 'px;font-weight:' + fontWeight + '">Ürün Başlığı</div>';
            case 'VariantInfo':
                return '<div style="' + base + 'font-size:' + fontSize + 'px;font-weight:' + fontWeight + ';color:#555">Kırmızı / M</div>';
            case 'Barcode':
                return '<div style="' + base + 'flex-direction:column;justify-content:center;align-items:center;font-family:monospace;border:1px dashed #999">'
                    + '<div style="letter-spacing:3px;font-size:' + Math.max(12, Math.floor(el.height * 0.3)) + 'px;overflow:hidden">|||||||||||||||</div>'
                    + '<div style="margin-top:4px;font-size:' + Math.max(10, Math.floor(el.height * 0.15)) + 'px">8680000000000</div>'
                    + '</div>';
            case 'SalePrice':
                return '<div style="' + base + 'font-size:' + fontSize + 'px;font-weight:' + fontWeight + '">199,90 ₺</div>';
            case 'ListPrice':
                return '<div style="' + base + 'font-size:' + fontSize + 'px;font-weight:' + fontWeight + ';text-decoration:line-through;color:#888">299,00 ₺</div>';
            case 'CustomText':
                return '<div style="' + base + 'font-size:' + fontSize + 'px;font-weight:' + fontWeight + ';color:#333">' + (el.customText || 'Özel Yazı') + '</div>';
            case 'Line':
                return '<div style="width:100%;height:100%;display:flex;align-items:center"><div style="width:100%;border-bottom:2px solid #000"></div></div>';
            default:
                return '<div style="' + base + 'font-size:12px;color:#999">' + el.elementType + '</div>';
        }
    }

    function addResizeHandles(div) {
        var handles = ['se', 'sw', 'ne', 'nw'];
        var cursors = { se: 'nwse-resize', sw: 'nesw-resize', ne: 'nesw-resize', nw: 'nwse-resize' };
        var positions = {
            se: { right: '-4px', bottom: '-4px' },
            sw: { left: '-4px', bottom: '-4px' },
            ne: { right: '-4px', top: '-4px' },
            nw: { left: '-4px', top: '-4px' }
        };

        handles.forEach(function (h) {
            var handle = document.createElement('div');
            handle.dataset.handle = h;
            handle.style.position = 'absolute';
            handle.style.width = '8px';
            handle.style.height = '8px';
            handle.style.backgroundColor = '#1976D2';
            handle.style.cursor = cursors[h];
            handle.style.zIndex = '10';
            Object.assign(handle.style, positions[h]);
            div.appendChild(handle);
        });
    }

    function onCanvasMouseDown(e) {
        var target = e.target;

        // Check resize handle
        if (target.dataset && target.dataset.handle) {
            e.preventDefault();
            e.stopPropagation();
            var parentIdx = parseInt(target.parentElement.dataset.index);
            if (isNaN(parentIdx)) return;
            _resizing = true;
            _resizeHandle = target.dataset.handle;
            _resizeStartX = e.clientX;
            _resizeStartY = e.clientY;
            _resizeElStartW = _elements[parentIdx].width;
            _resizeElStartH = _elements[parentIdx].height;
            _resizeElStartX = _elements[parentIdx].x;
            _resizeElStartY = _elements[parentIdx].y;
            _selectedIndex = parentIdx;
            render();
            return;
        }

        // Find element
        var el = target.closest('[data-index]');
        if (el) {
            var idx = parseInt(el.dataset.index);
            if (isNaN(idx)) return;

            e.preventDefault();
            _selectedIndex = idx;
            _dragging = true;

            var rect = _canvas.getBoundingClientRect();
            _dragStartX = e.clientX;
            _dragStartY = e.clientY;
            _dragElStartX = _elements[idx].x;
            _dragElStartY = _elements[idx].y;

            render();

            if (_dotNetRef) {
                _dotNetRef.invokeMethodAsync('OnElementSelected', idx);
            }
        } else {
            // Deselect
            _selectedIndex = -1;
            render();
            if (_dotNetRef) {
                _dotNetRef.invokeMethodAsync('OnElementSelected', -1);
            }
        }
    }

    function onMouseMove(e) {
        if (_dragging && _selectedIndex >= 0) {
            var dx = (e.clientX - _dragStartX) / _zoom;
            var dy = (e.clientY - _dragStartY) / _zoom;

            var newX = Math.max(0, Math.min(_widthDots - _elements[_selectedIndex].width, _dragElStartX + dx));
            var newY = Math.max(0, Math.min(_heightDots - _elements[_selectedIndex].height, _dragElStartY + dy));

            _elements[_selectedIndex].x = Math.round(newX);
            _elements[_selectedIndex].y = Math.round(newY);
            render();
        }

        if (_resizing && _selectedIndex >= 0) {
            var dx = (e.clientX - _resizeStartX) / _zoom;
            var dy = (e.clientY - _resizeStartY) / _zoom;
            var el = _elements[_selectedIndex];

            switch (_resizeHandle) {
                case 'se':
                    el.width = Math.max(20, Math.round(_resizeElStartW + dx));
                    el.height = Math.max(10, Math.round(_resizeElStartH + dy));
                    break;
                case 'sw':
                    var newW = Math.max(20, Math.round(_resizeElStartW - dx));
                    el.x = Math.round(_resizeElStartX + _resizeElStartW - newW);
                    el.width = newW;
                    el.height = Math.max(10, Math.round(_resizeElStartH + dy));
                    break;
                case 'ne':
                    el.width = Math.max(20, Math.round(_resizeElStartW + dx));
                    var newH = Math.max(10, Math.round(_resizeElStartH - dy));
                    el.y = Math.round(_resizeElStartY + _resizeElStartH - newH);
                    el.height = newH;
                    break;
                case 'nw':
                    var newW2 = Math.max(20, Math.round(_resizeElStartW - dx));
                    var newH2 = Math.max(10, Math.round(_resizeElStartH - dy));
                    el.x = Math.round(_resizeElStartX + _resizeElStartW - newW2);
                    el.y = Math.round(_resizeElStartY + _resizeElStartH - newH2);
                    el.width = newW2;
                    el.height = newH2;
                    break;
            }
            render();
        }
    }

    function onMouseUp(e) {
        if (_dragging && _selectedIndex >= 0) {
            _dragging = false;
            var el = _elements[_selectedIndex];
            if (_dotNetRef) {
                _dotNetRef.invokeMethodAsync('OnElementMoved', _selectedIndex, el.x, el.y);
            }
        }

        if (_resizing && _selectedIndex >= 0) {
            _resizing = false;
            var el = _elements[_selectedIndex];
            if (_dotNetRef) {
                _dotNetRef.invokeMethodAsync('OnElementResized', _selectedIndex, el.width, el.height);
                _dotNetRef.invokeMethodAsync('OnElementMoved', _selectedIndex, el.x, el.y);
            }
        }
    }

    function addElement(element) {
        _elements.push(element);
        render();
    }

    function updateElement(index, props) {
        if (index < 0 || index >= _elements.length) return;
        Object.assign(_elements[index], props);
        render();
    }

    function removeElement(index) {
        if (index < 0 || index >= _elements.length) return;
        _elements.splice(index, 1);
        if (_selectedIndex === index) _selectedIndex = -1;
        else if (_selectedIndex > index) _selectedIndex--;
        render();
    }

    function getElements() {
        return _elements;
    }

    function setZoom(scale) {
        _zoom = scale;
        if (_canvas) {
            _canvas.style.transform = 'scale(' + _zoom + ')';
        }
    }

    function destroy() {
        if (_canvas) {
            _canvas.removeEventListener('mousedown', onCanvasMouseDown);
        }
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        _dotNetRef = null;
        _canvas = null;
        _elements = [];
        _selectedIndex = -1;
    }

    return {
        init: init,
        addElement: addElement,
        updateElement: updateElement,
        removeElement: removeElement,
        getElements: getElements,
        setZoom: setZoom,
        destroy: destroy
    };
})();
