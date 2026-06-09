using AnkiBridge.Web.Common.Dispatching;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace AnkiBridge.Web.Components.Abstractions;

public abstract class RootComponentBase : ComponentBase
{
    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    [Inject]
    protected IDialogService DialogService { get; set; } = default!;

    [Inject]
    protected IToastService ToastService { get; set; } = default!;

    [Inject]
    protected IRequestDispatcher Dispatcher { get; set; } = default!;

    [Inject] 
    protected IHttpClientFactory HttpClientFactory { get; set; } = default!;
}
