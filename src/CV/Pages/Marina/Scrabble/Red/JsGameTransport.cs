using System.Text.Json;
using Microsoft.JSInterop;

namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// <see cref="IGameTransport"/> que delega el envio y la recepcion en un modulo de JavaScript
/// (hoy <c>wwwroot/assets/marina/scrabble-mqtt.js</c>: broker MQTT publico por WebSocket). El modulo exporta
/// <c>crear</c>, <c>unirse</c>, <c>enviar</c> y <c>cerrar</c> y llama a <see cref="AlMensaje"/> y <see cref="AlCierre"/>.
/// Para cambiar de tecnologia (WebRTC, otro broker...) basta con otro modulo con esas cuatro funciones.
/// </summary>
public sealed class JsGameTransport(IJSRuntime js, string rutaModulo) : IGameTransport
{
    private IJSObjectReference? _modulo;
    private DotNetObjectReference<JsGameTransport>? _referencia;

    public event Func<GameMessage, Task>? MessageReceived;

    public event Func<Task>? Disconnected;

    public async Task<string?> OpenRoomAsync(string roomId)
    {
        await PrepararAsync();
        return await _modulo!.InvokeAsync<string?>("crear", _referencia, roomId);
    }

    public async Task<string?> JoinRoomAsync(string roomId)
    {
        await PrepararAsync();
        return await _modulo!.InvokeAsync<string?>("unirse", _referencia, roomId);
    }

    public async Task SendAsync(GameMessage message)
    {
        if (_modulo is null)
        {
            return;
        }

        try
        {
            await _modulo.InvokeVoidAsync("enviar", JsonSerializer.Serialize(message, GameMessage.Json));
        }
        catch (JSException)
        {
            // Canal cerrado justo ahora: llegara Disconnected.
        }
    }

    public async Task CloseAsync()
    {
        if (_modulo is null)
        {
            return;
        }

        try
        {
            await _modulo.InvokeVoidAsync("cerrar");
        }
        catch (Exception)
        {
            // Ya estaba cerrado.
        }
    }

    private async Task PrepararAsync()
    {
        _referencia ??= DotNetObjectReference.Create(this);
        _modulo ??= await js.InvokeAsync<IJSObjectReference>("import", rutaModulo);
    }

    /// <summary>Llega texto del otro navegador: se descarta lo demasiado grande, lo que no es JSON y lo que no trae tipo.</summary>
    [JSInvokable]
    public async Task AlMensaje(string json)
    {
        if (json.Length > GameMessage.MaxLongitud)
        {
            return;
        }

        GameMessage? mensaje;
        try
        {
            mensaje = JsonSerializer.Deserialize<GameMessage>(json, GameMessage.Json);
        }
        catch (JsonException)
        {
            return;
        }

        if (mensaje is null || string.IsNullOrEmpty(mensaje.Type) || string.IsNullOrEmpty(mensaje.MessageId))
        {
            return;
        }

        if (MessageReceived is { } alRecibir)
        {
            await alRecibir(mensaje);
        }
    }

    [JSInvokable]
    public async Task AlCierre()
    {
        if (Disconnected is { } alCerrar)
        {
            await alCerrar();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();

        try
        {
            if (_modulo is not null)
            {
                await _modulo.DisposeAsync();
            }
        }
        catch (Exception)
        {
            // La pagina ya se esta cerrando.
        }

        _modulo = null;
        _referencia?.Dispose();
        _referencia = null;
    }
}
