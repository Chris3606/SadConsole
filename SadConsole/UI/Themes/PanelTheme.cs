﻿using System;
using SadConsole.UI.Controls;
using SadRogue.Primitives;

namespace SadConsole.UI.Themes
{
    /// <summary>
    /// A basic theme for a drawing surface that simply fills the surface based on the state.
    /// </summary>
    public class PanelTheme : ThemeBase
    {
        /// <summary>
        /// When true, only uses <see cref="ThemeStates.Normal"/> for drawing.
        /// </summary>
        public bool UseNormalStateOnly { get; set; } = true;

        /// <summary>
        /// When true, ignores all states and doesn't draw anything.
        /// </summary>
        public bool SkipDrawing { get; set; } = false;

        /// <summary>
        /// The current appearance based on the control state.
        /// </summary>
        public ColoredGlyph Appearance { get; protected set; }

        /// <inheritdoc />
        public override void Attached(ControlBase control)
        {
            control.Surface = new CellSurface(control.Width, control.Height)
            {
                DefaultBackground = Color.Transparent
            };
            control.Surface.Clear();
        }

        /// <inheritdoc />
        public override void UpdateAndDraw(ControlBase control, TimeSpan time)
        {
            if (SkipDrawing || !(control is Panel))
                return;

            RefreshTheme(control.FindThemeColors(), control);

            if (!UseNormalStateOnly)
            {
                if (Helpers.HasFlag((int)control.State, (int)ControlStates.Disabled))
                    Appearance = Disabled;
                else if (Helpers.HasFlag((int)control.State, (int)ControlStates.MouseLeftButtonDown) ||
                         Helpers.HasFlag((int)control.State, (int)ControlStates.MouseRightButtonDown))
                    Appearance = MouseDown;
                else if (Helpers.HasFlag((int)control.State, (int)ControlStates.MouseOver))
                    Appearance = MouseOver;
                else if (Helpers.HasFlag((int)control.State, (int)ControlStates.Focused))
                    Appearance = Focused;
                else
                    Appearance = Normal;
            }
            else
                Appearance = Normal;

            control.Surface.Fill(Appearance.Foreground, Appearance.Background, Appearance.Glyph);

            control.IsDirty = false;
        }

        /// <inheritdoc />
        public override ThemeBase Clone() => new PanelTheme()
        {
            Normal = Normal.Clone(),
            Disabled = Disabled.Clone(),
            MouseOver = MouseOver.Clone(),
            MouseDown = MouseDown.Clone(),
            Selected = Selected.Clone(),
            Focused = Focused.Clone(),
            UseNormalStateOnly = UseNormalStateOnly,
            SkipDrawing = SkipDrawing
        };
    }
}
