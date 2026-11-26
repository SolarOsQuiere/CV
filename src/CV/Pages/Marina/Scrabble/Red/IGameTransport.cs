namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// Canal entre los dos navegadores. El juego solo conoce esto, no WebRTC: se puede cambiar el transporte
/// (otro servicio de senalizacion, WebSocket...) sin tocar la logica.
/// </summary>
public interface IGameTransport : IAsyncDisposable
{
    /// <summary>Anfitrion: abre la sala y espera. Devuelve el texto del error, o null.</summary>
    Task<string?> OpenRoomAsync(string roomId);

    /// <summary>Invitado: se conecta a la sala del anfitrion. Devuelve el texto del error, o null.</summary>
    Task<string?> JoinRoomAsync(string roomId);

    Task SendAsync(GameMessage message);

    /// <summary>Cierra el canal sin avisar por <see cref="Disconnected"/>.</summary>
    Task CloseAsync();

    /// <summary>Llega un mensaje valido del otro navegador.</summary>
    event Func<GameMessage, Task>? MessageReceived;

    /// <summary>El otro navegador se ha ido (o se ha perdido la conexion).</summary>
    event Func<Task>? Disconnected;
}
