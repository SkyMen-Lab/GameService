using System;
using System.Threading.Tasks;

namespace GameService.TCP.Events
{
    public abstract class EventBase<T> : IEvent where T : class
    {
        protected T Args;
        protected EventHandler EventHandler;
        public abstract void ResolveDependencies(IServiceProvider serviceProvider);

        public abstract Task Execute();
        

        public void SetEvent(EventHandler handler)
        {
            EventHandler = handler;
        }

        public void SetArgs(object args)
        {
            if (args != null)
            {
                Args = args as T;
            }
        }
    }
}