<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet
   version="1.0"
   xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
   xmlns="http://www.w3.org/1999/xhtml"
   xmlns:wix="http://wixtoolset.org/schemas/v4/wxs">

  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="wix:Wix/wix:Fragment/wix:StandardDirectory/wix:Component/wix:RegistryKey/wix:RegistryValue[@Name='Classpath']">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
      <xsl:attribute name="Value"><xsl:text>%WAS_DEPS_CLASSPATH%\*;[DIR_JVM]service.jvm.web-standalone.jar</xsl:text></xsl:attribute>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>
