using System.Runtime.InteropServices;

namespace LoudOrNot.Services.Implementations;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct AudioObjectPropertyAddress(
    uint selector,
    uint scope,
    uint element)
{
    private readonly uint _selector = selector;
    private readonly uint _scope = scope;
    private readonly uint _element = element;
}
