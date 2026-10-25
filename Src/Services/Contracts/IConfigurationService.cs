using System.Threading.Tasks;

namespace MPDCtrl.Services.Contracts;

public interface IConfigurationService
{
    void Initialize(string configFilePath);
    Task<Models.Configuration> LoadConfigurationAsync();
    Task SaveConfigurationAsync();
    Models.Configuration CurrentConfiguration { get; }
    string ConfigFilePath { get; }
}