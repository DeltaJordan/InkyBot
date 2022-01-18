using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace SDL.Util
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Extensions
    {

        public static ConfiguredTaskAwaitable SafeAsync(this Task task)
        {
            return task.ConfigureAwait(false);
        }

        public static ConfiguredTaskAwaitable<T> SafeAsync<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false);
        }
    }
}
