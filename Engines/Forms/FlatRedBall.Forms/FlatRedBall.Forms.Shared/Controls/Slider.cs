﻿using FlatRedBall.Forms.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace FlatRedBall.Forms.Controls
{
    public class Slider : RangeBase
    {

        #region Fields/Properties

        public double TicksFrequency { get; set; } = 1;

        public bool IsSnapToTickEnabled { get; set; } = false;

        public bool IsMoveToPointEnabled { get; set; }
        #endregion

        #region Initialize


        public Slider()
        {
            Minimum = 0;
            Maximum = 100;
            LargeChange = 25;
            SmallChange = 5;
        }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();



            Track.Push += HandleTrackPush;

        }

        #endregion

        protected override void HandleThumbPush(object sender, EventArgs e)
        {
            var leftOfThumb = this.thumb.ActualX;

            if(this.thumb.Visual.XOrigin == RenderingLibrary.Graphics.HorizontalAlignment.Center)
            {
                leftOfThumb += this.thumb.ActualWidth / 2.0f;
            }
            else if(this.thumb.Visual.XOrigin == RenderingLibrary.Graphics.HorizontalAlignment.Right)
            {
                leftOfThumb += this.thumb.ActualWidth;
            }
            var cursorScreen = GuiManager.Cursor.ScreenX;

            cursorGrabOffsetRelativeToThumb = cursorScreen - leftOfThumb;
        }
        protected override void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            base.OnMinimumChanged(oldMinimum, newMinimum);

            if(Visual != null)
            {
                UpdateThumbPositionAccordingToValue();
            }
        }

        private void HandleTrackPush(IWindow window)
        {
            if(IsMoveToPointEnabled)
            {
                var left = Track.AbsoluteX;
                var right = Track.AbsoluteX + Track.GetAbsoluteWidth();

                var screenX = GuiManager.Cursor.ScreenX;

                var ratio = (screenX - left) / (right - left);

                ratio = System.Math.Max(0, ratio);
                ratio = System.Math.Min(1, ratio);

                var value = Minimum + (Maximum - Minimum) * ratio;

                ApplyValueConsideringSnapToTicks(value);
            }
            else
            {
                double newValue;
                if (GuiManager.Cursor.ScreenX < thumb.ActualX)
                {
                    newValue = Value - LargeChange;
                    ApplyValueConsideringSnapToTicks(newValue);
                }
                else if (GuiManager.Cursor.ScreenX > thumb.ActualX + thumb.ActualWidth)
                {
                    newValue = Value + LargeChange;

                    ApplyValueConsideringSnapToTicks(newValue);
                }
            }
        }

        private double ApplyValueConsideringSnapToTicks(double newValue)
        {
            var originalValue = newValue;

            if (IsSnapToTickEnabled)
            {
                if(FlatRedBall.Input.InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.S))
                {
                    int m = 3;
                }
                newValue = Math.MathFunctions.RoundDouble(newValue, TicksFrequency, Minimum);

                var range = Maximum - Minimum;
                var lastTick = ((int)((originalValue - Minimum) / TicksFrequency)) * TicksFrequency;

                if(originalValue > lastTick)
                {
                    // see if we snap to end or not...
                    var distanceFromLastTick = System.Math.Abs(originalValue - lastTick);
                    var distanceFromMax = System.Math.Abs(Maximum - originalValue);

                    if(distanceFromMax < distanceFromLastTick)
                    {
                        newValue = Maximum;
                    }
                }

            }


            Value = newValue;
            return newValue;
        }

        protected override void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            base.OnMaximumChanged(oldMaximum, newMaximum);

            if (Visual != null)
            {
                UpdateThumbPositionAccordingToValue();
            }
        }

        protected override void OnValueChanged(double oldValue, double newValue)
        {
            base.OnValueChanged(oldValue, newValue);

            if (Visual != null)
            {
                UpdateThumbPositionAccordingToValue();
            }
        }

        private void UpdateThumbPositionAccordingToValue()
        {
            var ratioOver = (Value - Minimum) / (Maximum - Minimum);
            if (Maximum <= Minimum)
            {
                ratioOver = 0;
            }

            thumb.X = Microsoft.Xna.Framework.MathHelper.Lerp(0, Track.GetAbsoluteWidth(),
                (float)ratioOver);

        }

        protected override void UpdateThumbPositionToCursorDrag(Cursor cursor)
        {
            var cursorScreenX = cursor.ScreenX;

            var cursorXRelativeToTrack = cursorScreenX - Track.AbsoluteX;
            
            thumb.X = cursorXRelativeToTrack - cursorGrabOffsetRelativeToThumb;

            float range = Track.GetAbsoluteWidth() ;

            
            if(range != 0)
            {
                var ratio = (thumb.X) / range;

                var valueToSet = Minimum + (Maximum - Minimum) * ratio;

                ApplyValueConsideringSnapToTicks(valueToSet);
            }
            else
            {
                Value = Minimum;
            }
        }
    }
}
