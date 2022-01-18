using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SDL.Util
{
    public static class StringLock
    {

        private static readonly IDictionary<string, IDisposable> m_tickets = new Dictionary<string, IDisposable>();
        private static readonly object m_globalLock = new();

        private sealed class Ticket : IDisposable
        {

            private readonly SemaphoreSlim m_lock;
            private readonly string m_id;
            private int m_refCount;

            public Ticket(string id)
            {
                this.m_lock = new SemaphoreSlim(1);
                this.m_id = id;
                this.m_refCount = 1;
            }

            public SemaphoreSlim Lock { get => this.m_lock; }

            public void Ref()
            {
                this.m_refCount++;
            }

            void IDisposable.Dispose()
            {
                lock (StringLock.m_globalLock)
                {
                    this.m_refCount--;
                    if (this.m_refCount <= 0)
                    {
                        m_tickets.Remove(this.m_id);
                    }
                }

                this.m_lock.Release();
            }

        }

        public static async Task<IDisposable> AcquireAsync(string id)
        {
            Ticket ticket = null;
            lock (m_globalLock)
            {
                if (m_tickets.TryGetValue(id, out IDisposable _ticket))
                {
                    ticket = _ticket as Ticket;
                    ticket.Ref();
                }
                else
                {
                    ticket = new Ticket(id);
                    m_tickets.Add(id, ticket);
                }
            }

            await ticket.Lock.WaitAsync().SafeAsync();
            return ticket;
        }

    }
}
