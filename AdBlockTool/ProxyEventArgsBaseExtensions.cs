using Titanium.Web.Proxy.EventArguments;

namespace Titanium.Web.Proxy.Basic
{
    public static class ProxyEventArgsBaseExtensions
    {
        public static MyClientState GetState(this ProxyEventArgsBase args)
        {
            if (args.ClientUserData == null) args.ClientUserData = new MyClientState();

            return (MyClientState)args.ClientUserData;
        }
    }
}