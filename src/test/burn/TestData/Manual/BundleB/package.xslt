<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
    xmlns:wix="http://wixtoolset.org/schemas/v4/wxs"
>
    <xsl:output method="xml" indent="yes"/>

    <xsl:template match="@* | node()">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
    </xsl:template>

    <xsl:template match="wix:Payload" >
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
            <xsl:attribute name="Id">package_<xsl:value-of select="substring(@SourceFile, 11)" /></xsl:attribute>
            <xsl:attribute name="SourceFile">PackagePayloads<xsl:value-of select="substring(@SourceFile, 10)" /></xsl:attribute>
        </xsl:copy>
    </xsl:template>
</xsl:stylesheet>
