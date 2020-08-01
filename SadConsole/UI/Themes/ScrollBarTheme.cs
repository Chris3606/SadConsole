﻿using System;
using System.Runtime.Serialization;
using SadConsole.UI.Controls;
using SadRogue.Primitives;

namespace SadConsole.UI.Themes
{ 
    /// <summary>
    /// The theme of the slider control.
    /// </summary>
    [DataContract]
    public class ScrollBarTheme : ThemeBase
    {
        /// <summary>
        /// The theme part fot the start button.
        /// </summary>
        [DataMember]
        public int StartButtonVerticalGlyph;

        /// <summary>
        /// The theme part fot the start button.
        /// </summary>
        [DataMember]
        public int EndButtonVerticalGlyph;

        /// <summary>
        /// The theme part fot the start button.
        /// </summary>
        [DataMember]
        public int StartButtonHorizontalGlyph;

        /// <summary>
        /// The theme part fot the start button.
        /// </summary>
        [DataMember]
        public int EndButtonHorizontalGlyph;

        /// <summary>
        /// The theme part for the scroll bar bar where the slider is not located.
        /// </summary>
        [DataMember]
        public int BarGlyph;

        /// <summary>
        /// The theme part for the scroll bar icon.
        /// </summary>
        [DataMember]
        public int SliderGlyph;

        /// <summary>
        /// Creates a new theme used by the <see cref="ScrollBar"/>.
        /// </summary>
        public ScrollBarTheme()
        {
            //TODO add states for ends. Bar should use base state.
            StartButtonVerticalGlyph = 17;
            EndButtonVerticalGlyph = 16;
            StartButtonHorizontalGlyph = 30;
            EndButtonHorizontalGlyph = 31;
            SliderGlyph = 219;
            BarGlyph = 176;
        }

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
        public override void RefreshTheme(Colors themeColors, ControlBase control)
        {
            if (themeColors == null) themeColors = Library.Default.Colors;

            base.RefreshTheme(themeColors, control);

            SetForeground(Normal.Foreground);
            SetBackground(Normal.Background);

            Disabled = themeColors.Appearance_ControlDisabled.Clone();
        }

        /// <inheritdoc />
        public override void UpdateAndDraw(ControlBase control, TimeSpan time)
        {
            if (!(control is ScrollBar scrollbar))
            {
                return;
            }

            if (!scrollbar.IsDirty)
            {
                return;
            }

            RefreshTheme(control.FindThemeColors(), control);

            ColoredGlyph appearance;

            if (Helpers.HasFlag((int)scrollbar.State, (int)ControlStates.Disabled))
            {
                appearance = Disabled;
            }
            else if (Helpers.HasFlag((int)scrollbar.State, (int)ControlStates.MouseLeftButtonDown) || Helpers.HasFlag((int)scrollbar.State, (int)ControlStates.MouseRightButtonDown))
            {
                appearance = MouseDown;
            }
            else if (Helpers.HasFlag((int)scrollbar.State, (int)ControlStates.MouseOver))
            {
                appearance = MouseOver;
            }
            else if (Helpers.HasFlag((int)scrollbar.State, (int)ControlStates.Focused))
            {
                appearance = Focused;
            }
            else
            {
                appearance = Normal;
            }

            scrollbar.Surface.Clear();

            if (scrollbar.Orientation == Orientation.Horizontal)
            {
                scrollbar.Surface.SetCellAppearance(0, 0, appearance);
                scrollbar.Surface.SetGlyph(0, 0, StartButtonVerticalGlyph);

                scrollbar.Surface.SetCellAppearance(scrollbar.Width - 1, 0, appearance);
                scrollbar.Surface.SetGlyph(scrollbar.Width - 1, 0, EndButtonVerticalGlyph);

                for (int i = 1; i <= scrollbar.SliderBarSize; i++)
                {
                    scrollbar.Surface.SetCellAppearance(i, 0, appearance);
                    scrollbar.Surface.SetGlyph(i, 0, BarGlyph);
                }

                if (scrollbar.Value >= scrollbar.Minimum && scrollbar.Value <= scrollbar.Maximum && scrollbar.Minimum != scrollbar.Maximum)
                {
                    if (scrollbar.IsEnabled)
                    {
                        scrollbar.Surface.SetCellAppearance(1 + scrollbar.CurrentSliderPosition, 0, appearance);
                        scrollbar.Surface.SetGlyph(1 + scrollbar.CurrentSliderPosition, 0, SliderGlyph);
                    }
                }
            }
            else
            {
                scrollbar.Surface.SetCellAppearance(0, 0, appearance);
                scrollbar.Surface.SetGlyph(0, 0, StartButtonHorizontalGlyph);

                scrollbar.Surface.SetCellAppearance(0, scrollbar.Height - 1, appearance);
                scrollbar.Surface.SetGlyph(0, scrollbar.Height - 1, EndButtonHorizontalGlyph);

                for (int i = 0; i < scrollbar.SliderBarSize; i++)
                {
                    scrollbar.Surface.SetCellAppearance(0, i + 1, appearance);
                    scrollbar.Surface.SetGlyph(0, i + 1, BarGlyph);
                }

                if (scrollbar.Value >= scrollbar.Minimum && scrollbar.Value <= scrollbar.Maximum && scrollbar.Minimum != scrollbar.Maximum)
                {
                    if (scrollbar.IsEnabled)
                    {
                        scrollbar.Surface.SetCellAppearance(0, 1 + scrollbar.CurrentSliderPosition, appearance);
                        scrollbar.Surface.SetGlyph(0, 1 + scrollbar.CurrentSliderPosition, SliderGlyph);
                    }
                }
            }

            if (scrollbar.IsSliding)
                scrollbar.MouseArea = new Rectangle(-2, -2, scrollbar.Width + 4, scrollbar.Height + 4);
            else
                scrollbar.MouseArea = new Rectangle(0, 0, scrollbar.Width, scrollbar.Height);

            scrollbar.IsDirty = false;
        }

        /// <inheritdoc />
        public override ThemeBase Clone() => new ScrollBarTheme()
        {
            Normal = Normal.Clone(),
            Disabled = Disabled.Clone(),
            MouseOver = MouseOver.Clone(),
            MouseDown = MouseDown.Clone(),
            Selected = Selected.Clone(),
            Focused = Focused.Clone(),
            StartButtonVerticalGlyph = StartButtonVerticalGlyph,
            EndButtonVerticalGlyph = EndButtonVerticalGlyph,
            StartButtonHorizontalGlyph = StartButtonHorizontalGlyph,
            EndButtonHorizontalGlyph = EndButtonHorizontalGlyph,
            BarGlyph = BarGlyph,
            SliderGlyph = SliderGlyph
        };
    }
}
