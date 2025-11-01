using System;
using System.Management.Automation.Host;

namespace YoshinoShell
{
    internal class YoshinoRawUI : PSHostRawUserInterface
    {
        public override ConsoleColor BackgroundColor { get; set; } = ConsoleColor.Black;
        public override Size BufferSize { get; set; } = new Size(120, 50);
        public override Coordinates CursorPosition { get; set; } = new Coordinates(0, 0);
        public override int CursorSize { get; set; } = 1;
        public override ConsoleColor ForegroundColor { get; set; } = ConsoleColor.White;
        public override Coordinates WindowPosition { get; set; } = new Coordinates(0, 0);
        public override Size WindowSize { get; set; } = new Size(120, 50);
        public override string WindowTitle { get; set; } = "YoshinoShell";

        // 必要屬性（避免編譯錯誤）
        public override Size MaxPhysicalWindowSize => new Size(120, 50);
        public override Size MaxWindowSize => new Size(120, 50);
        public override bool KeyAvailable => false;

        // 空實作（避免例外）
        public override void FlushInputBuffer() { }
        public override BufferCell[,] GetBufferContents(Rectangle rectangle) => new BufferCell[0, 0];
        public override KeyInfo ReadKey(ReadKeyOptions options) => new KeyInfo();
        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill) { }
        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents) { }
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill) { }
    }
}
