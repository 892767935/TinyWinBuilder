using System.Text;

namespace TinyWinBuilder.Core;

/// <summary>
/// 生成 Unattended 自动应答文件
/// </summary>
public static class UnattendedGenerator
{
    public static string Generate(UnattendedSettings settings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<unattend xmlns=\"urn:schemas-microsoft-com:unattend\">");
        sb.AppendLine("  <settings pass=\"windowsPE\">");
        sb.AppendLine("    <component name=\"Microsoft-Windows-International-Core-WinPE\" processorArchitecture=\"amd64\" publicKeyToken=\"31bf3856ad364e35\" language=\"neutral\" versionScope=\"nonSxS\" xmlns:wcm=\"http://schemas.microsoft.com/WMIConfig/2002/State\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        sb.AppendLine("      <SetupUILanguage>");
        sb.AppendLine("        <UILanguage>zh-CN</UILanguage>");
        sb.AppendLine("      </SetupUILanguage>");
        sb.AppendLine("      <InputLocale>0804:{00000804}</InputLocale>");
        sb.AppendLine("      <SystemLocale>zh-CN</SystemLocale>");
        sb.AppendLine("      <UILanguage>zh-CN</UILanguage>");
        sb.AppendLine("      <UserLocale>zh-CN</UserLocale>");
        sb.AppendLine("    </component>");
        sb.AppendLine("  </settings>");
        sb.AppendLine("  <settings pass=\"specialize\">");
        sb.AppendLine("    <component name=\"Microsoft-Windows-Shell-Setup\" processorArchitecture=\"amd64\" publicKeyToken=\"31bf3856ad364e35\" language=\"neutral\" versionScope=\"nonSxS\" xmlns:wcm=\"http://schemas.microsoft.com/WMIConfig/2002/State\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        sb.AppendLine("      <AutoLogon>");
        sb.AppendLine("        <Password>");
        sb.AppendLine($"          <Value>{settings.Password}</Value>");
        sb.AppendLine("          <PlainText>true</PlainText>");
        sb.AppendLine("        </Password>");
        sb.AppendLine("        <Enabled>true</Enabled>");
        sb.AppendLine($"        <LogonCount>{settings.AutoLoginCount}</LogonCount>");
        sb.AppendLine("        <Username>{settings.Username}</Username>");
        sb.AppendLine("      </AutoLogon>");
        sb.AppendLine("      <OOBE>");
        sb.AppendLine("        <HideEULAPage>true</HideEULAPage>");
        sb.AppendLine("        <HideLocalAccountScreen>true</HideLocalAccountScreen>");
        sb.AppendLine("        <HideOEMRegistrationScreen>true</HideOEMRegistrationScreen>");
        sb.AppendLine("        <HideOnlineAccountScreens>true</HideOnlineAccountScreens>");
        sb.AppendLine("        <HideWirelessSetupInOOBE>true</HideWirelessSetupInOOBE>");
        sb.AppendLine("        <NetworkLocation>Home</NetworkLocation>");
        sb.AppendLine("        <ProtectYourPC>3</ProtectYourPC>");
        sb.AppendLine("      </OOBE>");
        sb.AppendLine("      <UserAccounts>");
        sb.AppendLine("        <LocalAccounts>");
        sb.AppendLine("          <LocalAccount wcm:action=\"add\">");
        sb.AppendLine("            <Description>Admin</Description>");
        sb.AppendLine("            <DisplayName>{settings.Username}</DisplayName>");
        sb.AppendLine("            <Group>Administrators</Group>");
        sb.AppendLine("            <Name>{settings.Username}</Name>");
        sb.AppendLine("            <Password>");
        sb.AppendLine($"              <Value>{settings.Password}</Value>");
        sb.AppendLine("              <PlainText>true</PlainText>");
        sb.AppendLine("            </Password>");
        sb.AppendLine("          </LocalAccount>");
        sb.AppendLine("        </LocalAccounts>");
        sb.AppendLine("      </UserAccounts>");
        sb.AppendLine("      <TimeZone>{settings.Timezone}</TimeZone>");
        sb.AppendLine("    </component>");
        sb.AppendLine("  </settings>");
        sb.AppendLine("</unattend>");
        return sb.ToString();
    }
}

public class UnattendedSettings
{
    public string Username { get; set; } = "User";
    public string Password { get; set; } = "123456";
    public string ProductKey { get; set; } = "";
    public string Timezone { get; set; } = "China Standard Time";
    public int AutoLoginCount { get; set; } = 1;
}
