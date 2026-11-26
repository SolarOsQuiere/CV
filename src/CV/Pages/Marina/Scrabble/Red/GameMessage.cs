using System.Text.Json;
using System.Text.Json.Serialization;

namespace CV.Pages.Marina.Scrabble;

/// <summary>
/// Sobre comun de todos los mensajes entre los dos navegadores. No sabe nada del Scrabble: el contenido va en <see cref="Payload"/>.
/// </summary>
/// <param name="Type">Uno de <see cref="MessageTypes"/>.</param>
/// <param name="RoomId">Sala a la que pertenece (se descartan los de otra sala).</param>
/// <param name="MessageId">Identificador unico: el anfitrion ignora los repetidos.</param>
/// <param name="Turn">En <c>PLAYER_MOVE</c>, el turno que el cliente tenia en pantalla al mover.</param>
/// <param name="Token">Token de sesion que el anfitrion entrega al invitado en <c>JOIN_ACCEPTED</c> y este manda en todo lo demas.</param>
public record GameMessage(
    string Type,
    string RoomId,
    string MessageId,
    int Turn = 0,
    JsonElement? Payload = null,
    string? Token = null)
{
    /// <summary>Opciones JSON compartidas (camelCase, sin nulos).</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>Tamano maximo (en caracteres) de un mensaje recibido: mas que eso se descarta sin mirarlo.</summary>
    public const int MaxLongitud = 32_768;

    public static GameMessage Crear<T>(string type, string roomId, T payload, int turn = 0, string? token = null) =>
        new(type, roomId, Guid.NewGuid().ToString("N"), turn, JsonSerializer.SerializeToElement(payload, Json), token);

    public static GameMessage Crear(string type, string roomId, int turn = 0, string? token = null) =>
        new(type, roomId, Guid.NewGuid().ToString("N"), turn, null, token);

    /// <summary>Lee el contenido como <typeparamref name="T"/>; null si falta o no tiene esa forma.</summary>
    public T? Leer<T>()
    {
        if (Payload is not { } contenido)
        {
            return default;
        }

        try
        {
            return contenido.Deserialize<T>(Json);
        }
        catch (JsonException)
        {
            return default;
        }
    }
}

/// <summary>Valores de <see cref="GameMessage.Type"/>.</summary>
public static class MessageTypes
{
    // Invitado -> anfitrion
    public const string JoinRequest = "JOIN_REQUEST";
    public const string PlayerMove = "PLAYER_MOVE";
    public const string ChatSend = "CHAT_SEND";

    // Anfitrion -> invitado
    public const string JoinAccepted = "JOIN_ACCEPTED";
    public const string JoinRejected = "JOIN_REJECTED";
    public const string GameInitialState = "GAME_INITIAL_STATE";
    public const string GameStateUpdated = "GAME_STATE_UPDATED";
    public const string MoveRejected = "MOVE_REJECTED";
    public const string ChatMessage = "CHAT_MESSAGE";
}

// Contenidos (payload) de los mensajes
public record JoinRequestPayload(string Name);

public record JoinAcceptedPayload(string Token);

public record TextoPayload(string Text);
