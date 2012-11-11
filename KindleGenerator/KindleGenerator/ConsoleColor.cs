using System;

namespace KindleGenerator
{
    /// <summary>
    /// Creates a scope for a console foreground color. When disposed, will return to  the previous Console.ForegroundColor
    /// </summary>
    public sealed class ConsoleColor : IDisposable
    {
        private readonly System.ConsoleColor _previousColor;

        public ConsoleColor(System.ConsoleColor color)
        {
            _previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public void Dispose()
        {
            Console.ForegroundColor = _previousColor;
        }
    }
}