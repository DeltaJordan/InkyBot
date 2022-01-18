using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SDL.Util
{

    /// <summary>
    /// An alternative to Lazy<> that doesn't cache exceptions
    /// </summary>
    public sealed class SafeLazy<T>
    {

        private const int STATE_UNINITIALIZED = 0;
        private const int STATE_UPDATING = 1;
        private const int STATE_INITIALIZED = 2;

        private readonly Func<T> factory;
        private readonly LazyThreadSafetyMode mode;
        private volatile int state;
        private T value;

        public SafeLazy(
            Func<T> initializer,
            LazyThreadSafetyMode threadSafetyMode
        )
        {
            this.factory = initializer;
            this.mode = threadSafetyMode;
            this.state = STATE_UNINITIALIZED;
            this.value = default;
        }

        public SafeLazy(T value)
        {
            this.factory = null;
            this.mode = LazyThreadSafetyMode.None;
            this.state = STATE_INITIALIZED;
            this.value = value;
        }

        public T Value
        {
            get
            {
                if (this.state == STATE_INITIALIZED)
                {
                    Interlocked.MemoryBarrier();
                    return this.value;
                }

                switch (this.mode)
                {
                    case LazyThreadSafetyMode.None: this.InitUnsafe(); break;
                    case LazyThreadSafetyMode.PublicationOnly: this.InitRace(); break;
                    case LazyThreadSafetyMode.ExecutionAndPublication: this.InitAtomic(); break;
                    default: throw new InvalidEnumArgumentException();
                }

                return this.value;
            }
        }

        private void InitUnsafe()
        {
            this.value = this.factory();
            this.state = STATE_INITIALIZED;
        }

        private void InitRace()
        {
            T myValue = this.factory();
            int previousState = Interlocked.CompareExchange(
                ref this.state,
                value: STATE_UPDATING,
                comparand: STATE_UNINITIALIZED
            );

            if (previousState == STATE_UPDATING)
            {
                while (this.state == STATE_UPDATING)
                {
                    Thread.Yield();
                }
            }

            if (previousState == STATE_UNINITIALIZED)
            {
                this.value = myValue;
                this.state = STATE_INITIALIZED;
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void InitAtomic()
        {
            if (this.state == STATE_UNINITIALIZED)
            {
                this.value = this.factory();
                this.state = STATE_INITIALIZED;
            }
        }


    }

}
