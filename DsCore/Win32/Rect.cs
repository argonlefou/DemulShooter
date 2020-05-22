using System;
using System.Collections.Generic;
using System.Text;

namespace DsCore.Win32
{
    /// <summary>
    /// The RECT structure defines a rectangle by the coordinates of its upper-left and lower-right corners.
    /// </summary>
    public struct Rect
    {
        /// <summary>
        /// Specifies the x-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        public int Left { get; set; }
        /// <summary>
        /// Specifies the y-coordinate of the upper-left corner of the rectangle.
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Specifies the x-coordinate of the lower-right corner of the rectangle.
        /// </summary>
        public int Right { get; set; }
        /// <summary>
        /// Specifies the y-coordinate of the lower-right corner of the rectangle.
        /// </summary>
        public int Bottom { get; set; }
    }
}
