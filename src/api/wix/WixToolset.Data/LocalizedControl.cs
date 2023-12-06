// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using SimpleJson;

    public class LocalizedControl
    {
        public LocalizedControl(string dialog, string control, int x, int y, int width, int height, bool rightToLeft, bool rightAligned, bool leftScroll, string text)
        {
            this.Dialog = dialog;
            this.Control = control;
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.RightToLeft = rightToLeft;
            this.RightAligned = rightAligned;
            this.LeftScroll = leftScroll;
            this.Text = text;
        }

        public string Dialog { get; }

        public string Control { get; }

        public int X { get; }

        public int Y { get; }

        public int Width { get; }

        public int Height { get; }

        public bool RightToLeft { get; }

        public bool RightAligned { get; }

        public bool LeftScroll { get; }

        public string Text { get; }

        /// <summary>
        /// Get key for a localized control.
        /// </summary>
        /// <returns>The localized control id.</returns>
        public string GetKey()
        {
            return LocalizedControl.GetKey(this.Dialog, this.Control);
        }

        /// <summary>
        /// Get key for a localized control.
        /// </summary>
        /// <param name="dialog">The optional id of the control's dialog.</param>
        /// <param name="control">The id of the control.</param>
        /// <returns>The localized control id.</returns>
        public static string GetKey(string dialog, string control)
        {
            return String.Concat(dialog, "/", control);
        }

        internal JsonObject Serialize()
        {
            var jsonObject = new JsonObject
            {
                { "dialog", this.Dialog },
            };

            jsonObject.AddIsNotNullOrEmpty("control", this.Control);
            jsonObject.AddNonDefaultValue("x", this.X);
            jsonObject.AddNonDefaultValue("y", this.Y);
            jsonObject.AddNonDefaultValue("width", this.Width);
            jsonObject.AddNonDefaultValue("height", this.Height);
            jsonObject.AddNonDefaultValue("rightToLeft", this.RightToLeft);
            jsonObject.AddNonDefaultValue("rightAligned", this.RightAligned);
            jsonObject.AddNonDefaultValue("leftScroll", this.LeftScroll);
            jsonObject.AddIsNotNullOrEmpty("text", this.Text);

            return jsonObject;
        }

        internal static LocalizedControl Deserialize(JsonObject jsonObject)
        {
            var dialog = jsonObject.GetValueOrDefault<string>("dialog");
            var control = jsonObject.GetValueOrDefault<string>("control");
            var x = jsonObject.GetValueOrDefault("x", 0);
            var y = jsonObject.GetValueOrDefault("y", 0);
            var width = jsonObject.GetValueOrDefault("width", 0);
            var height = jsonObject.GetValueOrDefault("height", 0);
            var rightToLeft = jsonObject.GetValueOrDefault("rightToLeft", false);
            var rightAligned = jsonObject.GetValueOrDefault("rightAligned", false);
            var leftScroll = jsonObject.GetValueOrDefault("leftScroll", false);
            var text = jsonObject.GetValueOrDefault<string>("text");

            return new LocalizedControl(dialog, control, x, y, width, height, rightToLeft, rightAligned, leftScroll, text);
        }
    }
}
