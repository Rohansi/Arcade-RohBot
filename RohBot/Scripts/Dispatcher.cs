using System;
using System.Collections.Generic;

namespace Games.RohBot
{
    public static class Dispatcher
    {
        public delegate void DispatchAction();
        
        private static Queue<DispatchAction> _actions;
        
        static Dispatcher()
        {
            _actions = new Queue<DispatchAction>();
        }
        
        public static void Enqueue(DispatchAction action)
        {
            lock (_actions)
                _actions.Enqueue(action);
        }
        
        public static void RunAll()
        {
            lock (_actions)
            {
                while (_actions.Count > 0)
                {
                    var action = _actions.Dequeue();
                    action();
                }
            }
        }
    }
}
