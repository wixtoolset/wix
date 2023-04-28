// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E;

using System;
using System.IO;
using System.Xml.Linq;
using WixTestTools;
using Xunit;
using Xunit.Abstractions;

public class XmlConfigTests : MsiE2ETests
{
    public XmlConfigTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [RuntimeFact]
    public void CanModifyXmlFileWithXmlConfig()
    {
        var product = this.CreatePackageInstaller("XmlConfig");

        product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

        // Validate the expected changes in my.xml.
        var myXmlPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "XmlConfig", "my.xml");
        var content = File.ReadAllText(myXmlPath);
        var xDoc = XDocument.Parse(content);

        var xRoot = xDoc.Element("root");
        var xChild1 = xRoot.Element("child1");
        Assert.NotNull(xChild1);

        var xGrandchild1 = xChild1.Element("grandchild1");
        Assert.Null(xGrandchild1);

        var xChild2 = xRoot.Element("child2");
        Assert.NotNull(xChild2);

        var xGrandchild3 = xChild2.Element("grandchild3");
        Assert.NotNull(xGrandchild3);
        Assert.True(xGrandchild3.HasAttributes);

        var xAttribute1 = xGrandchild3.Attribute("TheAttribute1");
        Assert.NotNull(xAttribute1);
        Assert.Equal("AttributeValue1", xAttribute1.Value);

        var xAttribute2 = xGrandchild3.Attribute("TheAttribute2");
        Assert.NotNull(xAttribute2);
        Assert.Equal("AttributeValue2", xAttribute2.Value);

        product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
    }
}
