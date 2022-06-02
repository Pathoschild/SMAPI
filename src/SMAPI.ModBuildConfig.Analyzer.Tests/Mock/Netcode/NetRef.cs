// ReSharper disable CheckNamespace -- matches Stardew Valley's code
namespace Netcode;

/// <summary>A simplified version of Stardew Valley's <c>Netcode.NetRef</c> for unit testing.</summary>
public class NetRef<T> : NetFieldBase<T, NetRef<T>> where T : class { }
