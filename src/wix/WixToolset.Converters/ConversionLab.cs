// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    internal class ConversionLab : IDisposable
    {
        private readonly XElement targetElement;
        private readonly XElement parentElement;
        private readonly List<XNode> siblingNodes;
        private int index;

        public ConversionLab(XElement targetElement)
        {
            this.targetElement = targetElement;
            this.parentElement = this.targetElement.Parent;
            this.siblingNodes = this.parentElement.Nodes().ToList();
            foreach (var siblingNode in this.siblingNodes)
            {
                siblingNode.Remove();
            }
            this.index = this.siblingNodes.IndexOf(this.targetElement);
        }

        public void RemoveOrphanTextNodes()
        {
            if (!this.targetElement.HasElements)
            {
                var childNodes = this.targetElement.Nodes().ToList();
                foreach (var childNode in childNodes)
                {
                    childNode.Remove();
                }
            }
        }

        public void RemoveTargetElement()
        {
            if (this.index + 1 < this.siblingNodes.Count
             && this.siblingNodes[this.index + 1] is XText trailingText
             && String.IsNullOrWhiteSpace(trailingText.Value))
            {
                this.siblingNodes.RemoveAt(this.index + 1);
            }
            this.siblingNodes.RemoveAt(this.index);
            if (0 < this.index
             && this.siblingNodes[this.index - 1] is XText leadingText
             && String.IsNullOrWhiteSpace(leadingText.Value))
            {
                this.siblingNodes.RemoveAt(this.index - 1);
            }
        }

        public void ReplaceTargetElement(XElement replacement)
        {
            this.siblingNodes[this.index] = replacement;
        }

        public void AddCommentsAsSiblings(IEnumerable<XNode> comments)
        {
            string leadingWhitespace = null;
            if (0 < this.index
             && this.siblingNodes[this.index - 1] is XText leadingText
             && String.IsNullOrWhiteSpace(leadingText.Value))
            {
                leadingWhitespace = leadingText.Value;
            }

            foreach (var comment in comments)
            {
                if (null != leadingWhitespace)
                {
                    this.siblingNodes.Insert(++this.index, new XText(leadingWhitespace));
                }
                this.siblingNodes.Insert(++this.index, comment);
            }
        }

        public void Dispose()
        {
            this.parentElement.Add(this.siblingNodes);
        }
    }
}
