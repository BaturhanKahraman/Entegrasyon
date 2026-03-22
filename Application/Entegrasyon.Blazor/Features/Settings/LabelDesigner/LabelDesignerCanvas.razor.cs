using Entegrasyon.Business.Abstract;
using Entegrasyon.Entity.Dtos.Label;
using Entegrasyon.Entity.Labels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Entegrasyon.Blazor.Features.Settings.LabelDesigner;

public partial class LabelDesignerCanvas : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ILabelTemplateService LabelTemplateService { get; set; } = null!;
    [Inject] private ISnackbar Snackbar { get; set; } = null!;

    [Parameter] public LabelTemplateDto? Template { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    private static readonly (string Label, int W, int H)[] Presets =
    [
        ("50 × 30 mm (Standart)", 50, 30),
        ("60 × 40 mm (Geniş)", 60, 40),
        ("80 × 50 mm (Büyük)", 80, 50)
    ];

    private DotNetObjectReference<LabelDesignerCanvas>? _dotNetRef;
    private List<LabelElement> _elements = [];
    private int _selectedIndex = -1;
    private LabelElement? _selectedElement;

    private string _name = "Yeni Şablon";
    private int _presetIndex;
    private int _widthMm = 50;
    private int _heightMm = 30;
    private int _dpi = 203;
    private double _zoom = 1.5;
    private bool _saving;
    private bool _initialized;
    private Guid? _templateId;

    private int WidthDots => (int)(_widthMm * _dpi / 25.4);
    private int HeightDots => (int)(_heightMm * _dpi / 25.4);

    private string CanvasContainerStyle =>
        $"width:{(int)(WidthDots * _zoom) + 20}px;height:{(int)(HeightDots * _zoom) + 20}px;overflow:visible";

    protected override void OnParametersSet()
    {
        if (Template is not null)
        {
            _templateId = Template.Id;
            _name = Template.Name;
            _widthMm = Template.WidthMm;
            _heightMm = Template.HeightMm;
            _dpi = Template.Dpi;
            _elements = Template.Elements.Select(e => new LabelElement
            {
                ElementType = e.ElementType,
                X = e.X, Y = e.Y,
                Width = e.Width, Height = e.Height,
                FontSize = e.FontSize,
                Bold = e.Bold,
                Strikethrough = e.Strikethrough,
                CustomText = e.CustomText
            }).ToList();

            // Preset eşleştirme
            _presetIndex = Presets.Length; // özel boyut default
            for (var i = 0; i < Presets.Length; i++)
            {
                if (Presets[i].W == _widthMm && Presets[i].H == _heightMm)
                {
                    _presetIndex = i;
                    break;
                }
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender || !_initialized)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await InitCanvas();
            _initialized = true;
        }
    }

    private async Task InitCanvas()
    {
        var jsElements = _elements.Select(e => new
        {
            elementType = e.ElementType,
            x = e.X, y = e.Y,
            width = e.Width, height = e.Height,
            fontSize = e.FontSize,
            bold = e.Bold,
            strikethrough = e.Strikethrough,
            customText = e.CustomText
        }).ToArray();

        await JS.InvokeVoidAsync("LabelDesigner.init",
            _dotNetRef, "label-designer-canvas", WidthDots, HeightDots, jsElements, _zoom);
    }

    private async Task AddElement(string elementType)
    {
        var element = elementType switch
        {
            "Title" => new LabelElement { ElementType = "Title", X = 20, Y = 10, Width = 300, Height = 30, FontSize = 28, Bold = true },
            "VariantInfo" => new LabelElement { ElementType = "VariantInfo", X = 20, Y = 45, Width = 200, Height = 22, FontSize = 18 },
            "Barcode" => new LabelElement { ElementType = "Barcode", X = 20, Y = 75, Width = 300, Height = 80, FontSize = 24 },
            "SalePrice" => new LabelElement { ElementType = "SalePrice", X = 20, Y = 170, Width = 200, Height = 40, FontSize = 36, Bold = true },
            "ListPrice" => new LabelElement { ElementType = "ListPrice", X = 220, Y = 175, Width = 200, Height = 30, FontSize = 24, Strikethrough = true },
            "CustomText" => new LabelElement { ElementType = "CustomText", X = 20, Y = 210, Width = 200, Height = 24, FontSize = 18, CustomText = "Özel Yazı" },
            "Line" => new LabelElement { ElementType = "Line", X = 20, Y = 160, Width = 300, Height = 2 },
            _ => new LabelElement { ElementType = elementType, X = 20, Y = 20, Width = 200, Height = 24, FontSize = 18 }
        };

        _elements.Add(element);

        await JS.InvokeVoidAsync("LabelDesigner.addElement", new
        {
            elementType = element.ElementType,
            x = element.X, y = element.Y,
            width = element.Width, height = element.Height,
            fontSize = element.FontSize,
            bold = element.Bold,
            strikethrough = element.Strikethrough,
            customText = element.CustomText
        });
    }

    [JSInvokable]
    public void OnElementSelected(int index)
    {
        _selectedIndex = index;
        _selectedElement = index >= 0 && index < _elements.Count ? _elements[index] : null;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnElementMoved(int index, double x, double y)
    {
        if (index < 0 || index >= _elements.Count) return;
        _elements[index].X = x;
        _elements[index].Y = y;
        if (_selectedIndex == index)
            StateHasChanged();
    }

    [JSInvokable]
    public void OnElementResized(int index, double w, double h)
    {
        if (index < 0 || index >= _elements.Count) return;
        _elements[index].Width = w;
        _elements[index].Height = h;
        if (_selectedIndex == index)
            StateHasChanged();
    }

    private async Task OnPropertyChanged(LabelElement element)
    {
        if (_selectedIndex < 0) return;

        await JS.InvokeVoidAsync("LabelDesigner.updateElement", _selectedIndex, new
        {
            elementType = element.ElementType,
            x = element.X, y = element.Y,
            width = element.Width, height = element.Height,
            fontSize = element.FontSize,
            bold = element.Bold,
            strikethrough = element.Strikethrough,
            customText = element.CustomText
        });
    }

    private async Task DeleteSelectedElement()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _elements.Count) return;

        _elements.RemoveAt(_selectedIndex);
        await JS.InvokeVoidAsync("LabelDesigner.removeElement", _selectedIndex);
        _selectedIndex = -1;
        _selectedElement = null;
    }

    private async Task OnPresetChanged(int index)
    {
        _presetIndex = index;
        if (index < Presets.Length)
        {
            _widthMm = Presets[index].W;
            _heightMm = Presets[index].H;
        }
        await ReinitCanvas();
    }

    private async Task OnZoomChanged(double zoom)
    {
        _zoom = zoom;
        await JS.InvokeVoidAsync("LabelDesigner.setZoom", _zoom);
    }

    private async Task ReinitCanvas()
    {
        await JS.InvokeVoidAsync("LabelDesigner.destroy");
        _initialized = false;
        await InitCanvas();
        _initialized = true;
    }

    private async Task Save()
    {
        _saving = true;
        try
        {
            var dto = new SaveLabelTemplateDto(
                Id: _templateId,
                Name: _name,
                Type: LabelType.ProductBarcode,
                WidthMm: _widthMm,
                HeightMm: _heightMm,
                Dpi: _dpi,
                Elements: _elements,
                IsDefault: false);

            var result = await LabelTemplateService.SaveAsync(dto);

            if (result.Success)
            {
                _templateId = result.Data?.Id;
                Snackbar.Add("Şablon kaydedildi", Severity.Success);
                await OnSaved.InvokeAsync();
            }
            else
            {
                Snackbar.Add(result.Message ?? "Kayıt başarısız", Severity.Error);
            }
        }
        finally
        {
            _saving = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("LabelDesigner.destroy");
        }
        catch (JSDisconnectedException)
        {
            // Circuit zaten kapanmış, JS çağrısı yapılamaz — güvenle yoksay
        }

        _dotNetRef?.Dispose();
    }
}
