param (
   [switch]$quiet = $false
)

Configuration CFWindows {
  Node "localhost" {

    WindowsFeature IISWebServer {
      Ensure = "Present"
        Name = "Web-Webserver"
    }
    WindowsFeature WebSockets {
      Ensure = "Present"
        Name = "Web-WebSockets"
    }
    WindowsFeature WebServerSupport {
      Ensure = "Present"
        Name = "AS-Web-Support"
    }
    WindowsFeature DotNet {
      Ensure = "Present"
        Name = "AS-NET-Framework"
    }
    WindowsFeature HostableWebCore {
      Ensure = "Present"
        Name = "Web-WHC"
    }

    WindowsFeature ASPClassic {
      Ensure = "Present"
      Name = "Web-ASP"
    }

    Script SetupDNS {
      SetScript = {
        $externalip = ([System.Net.Dns]::GetHostEntry([System.Net.Dns]::GetHostName()).AddressList | Where { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork }).IPAddressToString
        $ifindex = (Get-WmiObject Win32_NetworkAdapterConfiguration | Where { $_.IPAddress -AND $_.IPAddress.Contains($externalip) }).Index
        $interface = (Get-WmiObject Win32_NetworkAdapter | Where { $_.DeviceID -eq $ifindex }).netconnectionid
        $currentDNS = ((Get-DnsClientServerAddress -InterfaceAlias $interface) | where { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork }).ServerAddresses
        $newDNS = @("127.0.0.1") + $currentDNS
        Set-DnsClientServerAddress -InterfaceAlias $interface -ServerAddresses ($newDNS -join ",")
      }
      GetScript = {
        $externalip = ([System.Net.Dns]::GetHostEntry([System.Net.Dns]::GetHostName()).AddressList | Where { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork }).IPAddressToString
        $ifindex = (Get-WmiObject Win32_NetworkAdapterConfiguration | Where { $_.IPAddress -AND $_.IPAddress.Contains($externalip) }).Index
        $interface = (Get-WmiObject Win32_NetworkAdapter | Where { $_.DeviceID -eq $ifindex }).netconnectionid
        Get-DnsClientServerAddress -AddressFamily ipv4 -InterfaceAlias $interface
      }
      TestScript = {
        $externalip = ([System.Net.Dns]::GetHostEntry([System.Net.Dns]::GetHostName()).AddressList | Where { $_.AddressFamily -eq [System.Net.Sockets.AddressFamily]::InterNetwork }).IPAddressToString
        $ifindex = (Get-WmiObject Win32_NetworkAdapterConfiguration | Where { $_.IPAddress -AND $_.IPAddress.Contains($externalip) }).Index
        $global:interface = (Get-WmiObject Win32_NetworkAdapter | Where { $_.DeviceID -eq $ifindex }).netconnectionid
        if((Get-DnsClientServerAddress -InterfaceAlias $interface -AddressFamily ipv4 -ErrorAction Stop).ServerAddresses[0] -eq "127.0.0.1")
        {
          Write-Verbose -Message "DNS Servers are set correctly."
          return $true
        }
        else
        {
          Write-Verbose -Message "DNS Servers not yet correct."
          return $false
        }
      }
    }

    Script ClearDNSCache
    {
        SetScript = {
            Clear-DnsClientCache
        }
        GetScript = {
            Get-DnsClientCache
        }
        TestScript = {
            @(Get-DnsClientCache).Count -eq 0
        }
    }

    Registry DisableDNSNegativeCache
    {
        Ensure = "Present"
        Key = "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters"
        ValueName = "MaxNegativeCacheTtl"
        ValueType = "DWord"
        ValueData = "0"
    }

    Script EnableDiskQuota
    {
      SetScript = {
        fsutil quota enforce C:
      }
      GetScript = {
        fsutil quota query C:
      }
      TestScript = {
        $query = "select * from Win32_QuotaSetting where VolumePath='C:\\'"
        return @(Get-WmiObject -query $query).State -eq 2
      }
    }

    Registry IncreaseDesktopHeapForServices
    {
        Ensure = "Present"
        Key = "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\SubSystems"
        ValueName = "Windows"
        ValueType = "ExpandString"
        ValueData = "%SystemRoot%\system32\csrss.exe ObjectDirectory=\Windows SharedSection=1024,20480,20480 Windows=On SubSystemType=Windows ServerDll=basesrv,1 ServerDll=winsrv:UserServerDllInitialization,3 ServerDll=sxssrv,4 ProfileControl=Off MaxRequestThreads=16"
    }

    Script SetupFirewall
    {
      TestScript = {
        $anyFirewallsDisabled = !!(Get-NetFirewallProfile -All | Where-Object { $_.Enabled -eq "False" })
        $adminRuleMissing = !(Get-NetFirewallRule -Name CFAllowAdmins -ErrorAction Ignore)
        Write-Verbose "anyFirewallsDisabled: $anyFirewallsDisabled"
        Write-Verbose "adminRuleMissing: $adminRuleMissing"
        if ($anyFirewallsDisabled -or $adminRuleMissing)
        {
          return $false
        }
        else {
          return $true
        }
      }
      SetScript = {
        $admins = New-Object System.Security.Principal.NTAccount("Administrators")
        $adminsSid = $admins.Translate([System.Security.Principal.SecurityIdentifier])

        $LocalUser = "D:(A;;CC;;;$adminsSid)"
        $otherAdmins = Get-WmiObject win32_groupuser | 
          Where-Object { $_.GroupComponent -match 'administrators' } |
          ForEach-Object { [wmi]$_.PartComponent }

        foreach($admin in $otherAdmins)
        {
          $ntAccount = New-Object System.Security.Principal.NTAccount($admin.Name)
          $sid = $ntAccount.Translate([System.Security.Principal.SecurityIdentifier]).Value
          $LocalUser = $LocalUser + "(A;;CC;;;$sid)"
        }
        New-NetFirewallRule -Name CFAllowAdmins -DisplayName "Allow admins" `
          -Description "Allow admin users" -RemotePort Any `
          -LocalPort Any -LocalAddress Any -RemoteAddress Any `
          -Enabled True -Profile Any -Action Allow -Direction Outbound `
          -LocalUser $LocalUser

        Set-NetFirewallProfile -All -DefaultInboundAction Allow -DefaultOutboundAction Block -Enabled True
      }
      GetScript = { Get-NetFirewallProfile }
    }
  }
}

if($PSVersionTable.PSVersion.Major -lt 4) {
  $shell = New-Object -ComObject Wscript.Shell
  $shell.Popup("You must be running Powershell version 4 or greater", 5, "Invalid Powershell version", 0x30)
  echo "You must be running Powershell version 4 or greater"
  exit(-1)
}

Enable-PSRemoting -Force
Install-WindowsFeature DSC-Service
CFWindows
Start-DscConfiguration -Wait -Path .\CFWindows -Force -Verbose

if ($Error) {
    Write-Host "Error summary:"
    foreach($ErrorMessage in $Error)
    {
    Write-Host $ErrorMessage
    }
    if (!$quiet) {
        Read-Host -Prompt "Setup failed. The above errors occurred. Press Enter to exit"
    }
} else {
    if (!$quiet) {
        Read-Host -Prompt "Setup completed successfully. Press Enter to exit"
    }
}
