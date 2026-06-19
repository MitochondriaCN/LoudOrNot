namespace LoudOrNot.Models;

public sealed record InputAudioDevice(
    string Id,
    string Name,
    string Description,
    string Platform
);
