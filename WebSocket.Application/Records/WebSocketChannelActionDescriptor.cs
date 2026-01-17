
using System.Reflection;

protected sealed record WebSocketChannelActionDescriptor(string EventName, Type ChannelType, MethodInfo MethodInfo);