namespace Licensify.Services;

public interface ILicenseParser 
{
    public string Parse(string license);
}

public class LicenseParser(CliGlobalSettings settings) 
{
    public string Parse(string license) 
    {
        return string.Empty;
    }
}