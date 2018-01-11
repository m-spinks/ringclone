<?xml version="1.0" encoding="utf-8"?>
<serviceModel xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" name="RingCloneService" generation="1" functional="0" release="0" Id="25be8e77-3ec1-4bef-ab98-37258dd5cfdb" dslVersion="1.2.0.0" xmlns="http://schemas.microsoft.com/dsltools/RDSM">
  <groups>
    <group name="RingCloneServiceGroup" generation="1" functional="0" release="0">
      <settings>
        <aCS name="TicketGenerator:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/RingCloneService/RingCloneServiceGroup/MapTicketGenerator:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="TicketGeneratorInstances" defaultValue="[1,1,1]">
          <maps>
            <mapMoniker name="/RingCloneService/RingCloneServiceGroup/MapTicketGeneratorInstances" />
          </maps>
        </aCS>
        <aCS name="TicketProcessor:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="">
          <maps>
            <mapMoniker name="/RingCloneService/RingCloneServiceGroup/MapTicketProcessor:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </maps>
        </aCS>
        <aCS name="TicketProcessorInstances" defaultValue="[1,1,1]">
          <maps>
            <mapMoniker name="/RingCloneService/RingCloneServiceGroup/MapTicketProcessorInstances" />
          </maps>
        </aCS>
      </settings>
      <maps>
        <map name="MapTicketGenerator:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/RingCloneService/RingCloneServiceGroup/TicketGenerator/Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </setting>
        </map>
        <map name="MapTicketGeneratorInstances" kind="Identity">
          <setting>
            <sCSPolicyIDMoniker name="/RingCloneService/RingCloneServiceGroup/TicketGeneratorInstances" />
          </setting>
        </map>
        <map name="MapTicketProcessor:Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" kind="Identity">
          <setting>
            <aCSMoniker name="/RingCloneService/RingCloneServiceGroup/TicketProcessor/Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" />
          </setting>
        </map>
        <map name="MapTicketProcessorInstances" kind="Identity">
          <setting>
            <sCSPolicyIDMoniker name="/RingCloneService/RingCloneServiceGroup/TicketProcessorInstances" />
          </setting>
        </map>
      </maps>
      <components>
        <groupHascomponents>
          <role name="TicketGenerator" generation="1" functional="0" release="0" software="D:\APD\RingClone\RingCloneService\csx\Release\roles\TicketGenerator" entryPoint="base\x64\WaHostBootstrapper.exe" parameters="base\x64\WaWorkerHost.exe " memIndex="-1" hostingEnvironment="consoleroleadmin" hostingEnvironmentVersion="2">
            <settings>
              <aCS name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="" />
              <aCS name="__ModelData" defaultValue="&lt;m role=&quot;TicketGenerator&quot; xmlns=&quot;urn:azure:m:v1&quot;&gt;&lt;r name=&quot;TicketGenerator&quot; /&gt;&lt;r name=&quot;TicketProcessor&quot; /&gt;&lt;/m&gt;" />
            </settings>
            <resourcereferences>
              <resourceReference name="DiagnosticStore" defaultAmount="[4096,4096,4096]" defaultSticky="true" kind="Directory" />
              <resourceReference name="EventStore" defaultAmount="[1000,1000,1000]" defaultSticky="false" kind="LogStore" />
            </resourcereferences>
          </role>
          <sCSPolicy>
            <sCSPolicyIDMoniker name="/RingCloneService/RingCloneServiceGroup/TicketGeneratorInstances" />
            <sCSPolicyUpdateDomainMoniker name="/RingCloneService/RingCloneServiceGroup/TicketGeneratorUpgradeDomains" />
            <sCSPolicyFaultDomainMoniker name="/RingCloneService/RingCloneServiceGroup/TicketGeneratorFaultDomains" />
          </sCSPolicy>
        </groupHascomponents>
        <groupHascomponents>
          <role name="TicketProcessor" generation="1" functional="0" release="0" software="D:\APD\RingClone\RingCloneService\csx\Release\roles\TicketProcessor" entryPoint="base\x64\WaHostBootstrapper.exe" parameters="base\x64\WaWorkerHost.exe " memIndex="-1" hostingEnvironment="consoleroleadmin" hostingEnvironmentVersion="2">
            <settings>
              <aCS name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" defaultValue="" />
              <aCS name="__ModelData" defaultValue="&lt;m role=&quot;TicketProcessor&quot; xmlns=&quot;urn:azure:m:v1&quot;&gt;&lt;r name=&quot;TicketGenerator&quot; /&gt;&lt;r name=&quot;TicketProcessor&quot; /&gt;&lt;/m&gt;" />
            </settings>
            <resourcereferences>
              <resourceReference name="DiagnosticStore" defaultAmount="[4096,4096,4096]" defaultSticky="true" kind="Directory" />
              <resourceReference name="EventStore" defaultAmount="[1000,1000,1000]" defaultSticky="false" kind="LogStore" />
            </resourcereferences>
          </role>
          <sCSPolicy>
            <sCSPolicyIDMoniker name="/RingCloneService/RingCloneServiceGroup/TicketProcessorInstances" />
            <sCSPolicyUpdateDomainMoniker name="/RingCloneService/RingCloneServiceGroup/TicketProcessorUpgradeDomains" />
            <sCSPolicyFaultDomainMoniker name="/RingCloneService/RingCloneServiceGroup/TicketProcessorFaultDomains" />
          </sCSPolicy>
        </groupHascomponents>
      </components>
      <sCSPolicy>
        <sCSPolicyUpdateDomain name="TicketGeneratorUpgradeDomains" defaultPolicy="[5,5,5]" />
        <sCSPolicyUpdateDomain name="TicketProcessorUpgradeDomains" defaultPolicy="[5,5,5]" />
        <sCSPolicyFaultDomain name="TicketGeneratorFaultDomains" defaultPolicy="[2,2,2]" />
        <sCSPolicyFaultDomain name="TicketProcessorFaultDomains" defaultPolicy="[2,2,2]" />
        <sCSPolicyID name="TicketGeneratorInstances" defaultPolicy="[1,1,1]" />
        <sCSPolicyID name="TicketProcessorInstances" defaultPolicy="[1,1,1]" />
      </sCSPolicy>
    </group>
  </groups>
</serviceModel>