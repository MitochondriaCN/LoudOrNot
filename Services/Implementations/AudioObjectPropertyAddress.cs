using System.Runtime.InteropServices;

namespace LoudOrNot.Services.Implementations;

[StructLayout(LayoutKind.Sequential)]
internal struct AudioObjectPropertyAddress(
    uint selector,
    uint scope,
    uint element)
{
    public uint Selector = selector;
    public uint Scope = scope;
    public uint Element = element;
}
